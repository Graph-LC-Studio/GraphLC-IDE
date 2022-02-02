using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GraphLC_IDE.AppConfig;

using Path = System.IO.Path;

namespace GraphLC_IDE.Controls
{
    /// <summary>
    /// TaskItem.xaml 的交互逻辑
    /// </summary>
    public partial class TaskItem : UserControl
    {
        private string command = "";
        private Process process = null;
        private Thread thread = null;

        public TaskItem(Action refresh, string cmd)
        {
            InitializeComponent();
            command = cmd;

            thread = new Thread(() =>
            {
                try
                {
                    process = new Process();
                    var dir = Path.Combine(AppInfo.Path, "Bin", "GCloud");
                    process.StartInfo.Arguments = command;
                    process.StartInfo.FileName = Path.Combine(dir, "gcloud.exe");
                    process.StartInfo.WorkingDirectory = dir;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd().Replace("\n", "").Replace("\r", "");
                    process.WaitForExit();
                    process.Close();

                    this.Dispatcher.Invoke(() =>
                    {
                        Description = output == "OK" ? "完成" : "失败";
                        if(RefreshAfterCompletion)
                            refresh();
                    });
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "TaskItem.xaml.cs");
                    this.Dispatcher.Invoke(() =>
                    {
                        Description = "失败";
                    });
                }
            });
            thread.Start();
        }

        public new string Content
        {
            get => content.Text;
            set => content.Text = value;
        }

        public string Description
        {
            get => description.Text;
            set => description.Text = value;
        }

        public bool RefreshAfterCompletion
        {
            get;
            set;
        } = false;

        private void CloseButtonClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if ((Description == "完成" || Description == "失败" || Description == "取消任务") && this.Parent is StackPanel panel)
                    panel.Children.Remove(this);

                Description = "取消任务";
                process.Kill();
            }
            catch { }
        }
    }
}
