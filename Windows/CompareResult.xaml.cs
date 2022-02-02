using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using ControlzEx.Theming;
using Enterwell.Clients.Wpf.Notifications;

using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Functions;
using GraphLC_IDE.Extensions;

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// CompareResult.xaml 的交互逻辑
    /// </summary>
    public partial class CompareResult : MahApps.Metro.Controls.MetroWindow
    {
        enum Result
        {
            AC, WA, TLE, RE
        }

        public NotificationMessageManager NotificationsManager { get; } = new NotificationMessageManager();

        private string[] RunCommand = null;
        private int Timeout = 1000;
        private List<Tuple<string, string>> Points = null;

        public CompareResult(string command, int timeout, List<Tuple<string, string>> points)
        {
            RunCommand = command.ESplit();
            Timeout = timeout;
            Points = new List<Tuple<string, string>>(points.ToArray());

            this.DataContext = this;
            InitializeComponent();
            this.NotificationsGrid.Visibility = Visibility.Visible;

            InfoPanel.Visibility = Visibility.Collapsed;
            ThemeManager.Current.ChangeTheme(this, AppInfo.Theme["name"].ToString());

            _ = Compare();
        }

        private async Task Compare()
        {
            await Task.Delay(400);
            try
            {
                // 是否满足动画的最低级别
                bool ani = (int)AppInfo.Config["ide"]["settings"]["animation"]["level"] >= 1;

                int cnt = 0, pass = 0;
                foreach (var iter in Points)
                {
                    // 匹配结果
                    Result result = Result.AC;

                    // 匹配输出
                    var tot = 0;
                    var lines = iter.Item2.Split('\n');
                    var standardOutput = new StringBuilder();

                    Process p = new Process();
                    var task = new Task(() =>
                    {
                        try
                        {
                            p.StartInfo.FileName = RunCommand[0];
                            for (int i = 1; i < RunCommand.Length; i++)
                                p.StartInfo.Arguments += RunCommand[i] + " ";
                            p.StartInfo.WorkingDirectory = RunCommand[0].Substring(0, RunCommand[0].LastIndexOf("\\"));
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.RedirectStandardInput = p.StartInfo.RedirectStandardOutput = true;

                            p.OutputDataReceived += (sender, e) =>
                            {
                                if (e.Data != null)
                                {
                                    var output = e.Data;
                                    standardOutput.AppendLine(e.Data);

                                    if (tot >= lines.Length)
                                    {
                                        if (!string.IsNullOrWhiteSpace(output))
                                            result = Result.WA;
                                    }

                                    if (tot >= lines.Length || output.TrimEnd() != lines[tot].TrimEnd())
                                        result = Result.WA;

                                    ++tot;
                                }
                            };

                            p.Start();
                            p.BeginOutputReadLine();

                            // 输入样例
                            p.StandardInput.WriteLine(iter.Item1);
                            p.WaitForExit();

                            if (lines.Length - tot > 0)
                            {
                                if (lines.Length - tot != 1 || (lines.Length - tot == 1 && !string.IsNullOrWhiteSpace(lines[lines.Length - 1])))
                                    result = Result.WA;
                            }

                            if (p.ExitCode != 0)
                                result = Result.RE;
                            p.Close();
                        }
                        catch { }
                    });

                    task.Start();

                    bool finish = false;

                    await Task.Run(() => finish = task.Wait(Timeout));

                    // 超时就干掉进程
                    if (!finish)
                    {
                        result = Result.TLE;
                        try
                        {
                            p.Kill();
                            p.Close();
                        }
                        catch { }
                    }

                    // 往评测列表添加已经评测过的测试点
                    var block1 = new TextBlock() { Text = "评测点 " + (cnt + 1).ToString() };
                    var block2 = new TextBlock();
                    var block3 = new TextBlock() { Text = result == Result.AC ? "" : result.ToString() };
                    var dockPanel = new DockPanel() { Margin = new Thickness(0, 0, 4, 0) };
                    dockPanel.Children.Add(block1);
                    dockPanel.Children.Add(block3);
                    dockPanel.Children.Add(block2);
                    DockPanel.SetDock(block1, Dock.Left);
                    DockPanel.SetDock(block3, Dock.Right);

                    var item = new ListBoxItem()
                    {
                        Content = dockPanel,
                        Opacity = ani ? 0.2 : 1,
                        Margin = new Thickness(ani ? -8 : 0, 0, 0, 0),
                        Tag = new Tuple<string, Tuple<string, string>>(iter.Item1, new Tuple<string, string>(iter.Item2, standardOutput.ToString()))
                    };

                    List.Items.Add(item);

                    // 更新标题
                    this.Title = $"评测结果 [ 已评测 {++cnt} 通过 {(result == Result.AC ? ++pass : pass)} ]";

                    // 动画
                    if (ani)
                    {
                        AnimationHelper.BeginAnimation(1, new Tuple<Window, FrameworkElement>(this, item), AnimationHelper.ControlAnimationOpacityEnter);
                        AnimationHelper.BeginAnimation(1, new Tuple<Window, FrameworkElement>(this, item), AnimationHelper.ControlAnimationEnter);
                        await Task.Delay(180);
                    }
                    else await Task.Delay(1);
                }

                this.Dispatcher.Invoke(() =>
                {
                    LoadPanel.Visibility = Visibility.Collapsed;
                    InfoPanel.Opacity = ani ? 0.2 : 1;
                    InfoPanel.Visibility = Visibility.Visible;

                    List.SelectedIndex = 0;
                });

                AnimationHelper.BeginAnimation(1, new Tuple<Window, FrameworkElement>(this, InfoPanel), AnimationHelper.ControlAnimationOpacityEnter);
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Windows\\CompareResult.xaml.cs");

                NotificationsManager.CreateMessage()
                                   .Animates(true)
                                   .AnimationInDuration(0.3)
                                   .AnimationOutDuration(0.3)
                                   .Accent(Brushes.Red)
                                   .Background("#363636")
                                   .HasBadge("错误")
                                   .HasMessage("评测时遇到意外错误")
                                   .Dismiss().WithButton("忽略", null)
                                   .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                   .Queue();
            }
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (LoadPanel.Visibility == Visibility.Visible || List.SelectedItem == null) return;

                var tag = (List.SelectedItem as ListBoxItem).Tag as Tuple<string, Tuple<string, string>>;
                StandardInput.Text = tag.Item1;
                StandardOutput.Text = tag.Item2.Item1;
                ProgramOutput.Text = tag.Item2.Item2;
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "Windows\\CompareResult.xaml.cs");
            }
        }
    }
}
