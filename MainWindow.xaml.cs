using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Xml;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ControlzEx.Theming;
using Newtonsoft.Json.Linq;
using Enterwell.Clients.Wpf.Notifications;

using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Controls;
using GraphLC_IDE.Functions;
using GraphLC_IDE.Extensions;
using GraphLC_IDE.Windows;
using GraphLC_IDE.Windows.GCloud;
using GEditor;

using Path = System.IO.Path;

namespace GraphLC_IDE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public NotificationMessageManager NotificationsManager { get; } = new NotificationMessageManager();

        #region 主窗口

        /// <summary>
        /// 构造
        /// </summary>
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            this.NotificationsGrid.Visibility = Visibility.Visible;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            AppInfo.Config = new CfgLoader(Path.Combine(AppInfo.Path, "Config", "ide.cfg"));

            LoadCfgFile(AppInfo.Config); //加载配置文件
            Init(); //初始IDE
        }

        /// <summary>
        /// 窗口加载
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 加载最近打开的文件
            try
            {
                var OpenedHistory = Path.Combine(AppInfo.Path, "Config", "history.cfg");

                if (!File.Exists(OpenedHistory))
                    using (StreamWriter w = new StreamWriter(OpenedHistory))
                        w.Write("[]");

                // 开一个线程向列表添加文件
                new Task(() =>
                {
                    try
                    {
                        AppInfo.RecentFiles = new CfgLoader(OpenedHistory);
                        foreach (var iter in (JArray)AppInfo.RecentFiles.Token)
                        {
                            try
                            {
                                bool ContainsTag(ListBox list, string sender)
                                {
                                    foreach (var iter in list.Items)
                                    {
                                        try
                                        {
                                            var obj = (iter as ListBoxItem).Tag;
                                            if (obj.ToString().ToLower() == sender.ToLower())
                                                return true;
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                                        }
                                    }
                                    return false;
                                }

                                var file = iter.ToString();
                                bool y = false;
                                this.Dispatcher.Invoke(() => { y = ContainsTag(RecentFiles, file); });
                                if (File.Exists(file) && !y)
                                {
                                    // 动画参数
                                    // 此动画要求的最小动画级别设置 (0-2)
                                    const int animationMinLevel = 2;
                                    bool ani = false;

                                    ListBoxItem item = null;
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        ani = (int)AppInfo.Config["ide"]["settings"]["animation"]["level"] >= animationMinLevel &&
                                        (RecentFiles.Items.Count + 1) * 25 <= RecentFiles.ActualHeight;

                                        item = new ListBoxItem()
                                        {
                                            Content = file.Substring(file.LastIndexOf('\\') + 1),
                                            Tag = file,
                                            ToolTip = file,
                                            Opacity = ani ? 0.2 : 1,
                                            Margin = new Thickness(ani ? -8 : 0, 0, 0, 0)
                                        };
                                        RecentFiles.Items.Add(item);
                                    });

                                    if (ani)
                                    {
                                        // 启动动画
                                        AnimationHelper.BeginAnimation(animationMinLevel, new Tuple<Window, FrameworkElement>(this, item), AnimationHelper.ControlAnimationOpacityEnter);
                                        AnimationHelper.BeginAnimation(animationMinLevel, new Tuple<Window, FrameworkElement>(this, item), AnimationHelper.ControlAnimationEnter);
                                        Task.Delay(180).Wait();
                                    }
                                    else
                                    {
                                        Task.Delay(1).Wait();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }

            // 加载传入的文件
            try
            {
                // 遍历参数
                foreach (string arg in AppInfo.Args)
                {
                    // 如果这个参数是个文件
                    if (File.Exists(arg))
                        OpenFile(arg); // 打开这个文件
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }

            // 欢迎页面
            /*
            try
            {
                var introfile = Path.Combine(AppInformation.Path, "Config", "intro");
                if (File.Exists(introfile))
                {
                    new Introduction() { Owner = this }.Show();
                    new FileInfo(introfile).Delete();
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - Introduction");
            }
            */
        }

        /// <summary>
        /// 窗口即将关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (HasUnsavedTabs())
            {
                e.Cancel = true;
                await CloseAllTabs(true);
            }
        }

        /// <summary>
        /// 窗口关闭
        /// </summary>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            /*
            try
            {
                terminalHelper.Dispose();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
            */

            try
            {
                var list = AppInfo.RecentFiles.Token as JArray;
                list.Clear();
                foreach (ListBoxItem item in RecentFiles.Items)
                {
                    list.Add(item.Tag.ToString());
                }

                AppInfo.RecentFiles?.Save();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - 保存文件树信息");
            }

            // 保存窗口 大小 & 位置
            try
            {
                JsonHelper.Add(AppInfo.MainWindowProperty, "state", new JValue(this.WindowState == WindowState.Maximized ? "max" : "normal"));

                var obj = new JObject();
                JsonHelper.Add(obj, "left", new JValue(this.Left));
                JsonHelper.Add(obj, "top", new JValue(this.Top));
                JsonHelper.Add(obj, "width", new JValue(this.Width));
                JsonHelper.Add(obj, "height", new JValue(this.Height));

                var layout = new JObject();

                var left = new JObject();
                var leftDef = new JArray();
                foreach (var iter in LeftLayout.RowDefinitions)
                    leftDef.Add(ConversionGridLength(iter.Height));
                left.Add("width", ConversionGridLength(LayoutRoot.ColumnDefinitions[0].Width));
                left.Add("def", leftDef);
                layout.Add("left", left);

                var split1 = new JObject();
                split1.Add("width", ConversionGridLength(LayoutRoot.ColumnDefinitions[1].Width));
                layout.Add("split1", split1);

                var middle = new JObject();
                var middleDef = new JArray();
                foreach (var iter in MiddleLayout.RowDefinitions)
                    middleDef.Add(ConversionGridLength(iter.Height));
                middle.Add("width", ConversionGridLength(LayoutRoot.ColumnDefinitions[2].Width));
                middle.Add("def", middleDef);
                layout.Add("middle", middle);

                var split2 = new JObject();
                split2.Add("width", ConversionGridLength(LayoutRoot.ColumnDefinitions[3].Width));
                layout.Add("split2", split2);

                var right = new JObject();
                var rightDef = new JArray();
                foreach (var iter in RightLayout.RowDefinitions)
                    rightDef.Add(ConversionGridLength(iter.Height));
                right.Add("width", ConversionGridLength(LayoutRoot.ColumnDefinitions[4].Width));
                right.Add("def", rightDef);
                layout.Add("right", right);

                JsonHelper.Add(obj, "layout", layout);
                JsonHelper.Add(AppInfo.MainWindowProperty, "main", obj);
                AppInfo.MainWindowProperty.Save();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - 保存窗口大小位置信息");
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="cfg">配置文件</param>
        private void LoadCfgFile(CfgLoader cfg)
        {
            LoadThemeSettings(cfg); //加载主题配置文件

            if (AppInfo.Theme.Token != null)
                foreach (object i in menu.Items)
                {
                    try
                    {
                        MenuItem mi = (MenuItem)i;
                        Helper.SetControlProperty(mi, x =>
                        {
                            try
                            {
                                MenuItem item = (MenuItem)x;
                                JArray jArray = (JArray)AppInfo.Theme["menuRoot"]["fore"];
                                item.Foreground = new SolidColorBrush(Color.FromArgb((byte)jArray[0], (byte)jArray[1], (byte)jArray[2], (byte)jArray[3]));
                            }
                            catch (Exception) { }
                        });
                    }
                    catch (Exception) { }
                }

            // 主窗口属性
            AppInfo.MainWindowProperty = new CfgLoader(Path.Combine(AppInfo.Path, "Config", "property.cfg"));
            if (AppInfo.MainWindowProperty.Token == null)
                NotificationsManager.CreateMessage()
                                    .Animates(true)
                                    .AnimationInDuration(0.3)
                                    .AnimationOutDuration(0.3)
                                    .Accent(Brushes.Red)
                                    .Background("#363636")
                                    .HasBadge("错误")
                                    .HasMessage("无法加载窗口配置设置 (Config\\property.cfg)")
                                    .Dismiss().WithButton("忽略", null)
                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                    .Queue();
            else
            {
                try
                {
                    var obj = AppInfo.MainWindowProperty["main"] as JObject;
                    var layout = obj["layout"] as JObject;

                    {
                        bool startLoc = false;
                        if (obj.ContainsKey("left"))
                        {
                            startLoc = true;
                            this.Left = (double)obj["left"];
                        }
                        if (obj.ContainsKey("top"))
                        {
                            startLoc = true;
                            this.Top = (double)obj["top"];
                        }
                        if (!startLoc)
                            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }

                    if (obj.ContainsKey("width"))
                    {
                        this.Width = (double)obj["width"];
                    }
                    if (obj.ContainsKey("height"))
                    {
                        this.Height = (double)obj["height"];
                    }
                    if (AppInfo.MainWindowProperty.Obj.ContainsKey("state"))
                    {
                        this.WindowState = AppInfo.MainWindowProperty["state"].ToString() == "max" ? WindowState.Maximized : WindowState.Normal;
                    }

                    // 界面布局
                    InitLayout();
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                    NotificationsManager.CreateMessage()
                                        .Animates(true)
                                        .AnimationInDuration(0.3)
                                        .AnimationOutDuration(0.3)
                                        .Accent(Brushes.Red)
                                        .Background("#363636")
                                        .Background("#333")
                                        .HasBadge("错误")
                                        .HasMessage("加载配置文件 Config\\property.cfg 时出现错误")
                                        .Dismiss().WithButton("忽略", null)
                                        .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                        .Queue();
                }
            }
        }

        /// <summary>
        ///  初始化 GraphLC IDE
        /// </summary>
        private void Init()
        {
            // 硬件加速
            try
            {
                if ((bool)AppInfo.Config["ide"]["settings"]["animation"]["accelerate"])
                    RenderOptions.ProcessRenderMode = RenderMode.Default;
                else
                    RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - Init - 硬件加速");
            }

            // 样式
            try
            {
                var style = ((int)AppInfo.Config["ide"]["settings"]["animation"]["level"] > 0 ?
                    TryFindResource("MahApps.Styles.TabControl.AnimatedSingleRow") :
                    TryFindResource("MahApps.Styles.TabControl")) as Style;

                if (style != null)
                {
                    TabArea.Style = style;
                    TabViewArea.Style = style;
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - Init - 样式");
            }

            // 透明支持
            try
            {
                this.AllowsTransparency = (bool)AppInfo.Config["ide"]["settings"]["transparent"]["allow"];
                if (this.AllowsTransparency)
                    this.Opacity = (double)AppInfo.Config["ide"]["settings"]["transparent"]["opacity"];
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }

            try
            {
                // 修正 Menu 颜色
                TimerHandleMenu = new DispatcherTimer { Interval = new TimeSpan(800000), IsEnabled = true };
                TimerHandleMenu.Tick += TimerHandleMenu_Tick;
                TimerHandleMenu.Start();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - Init - 自动修正菜单栏");
            }

            try
            {
                // 修正工具栏的 Button
                TimerHandleButton = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1), IsEnabled = true };
                TimerHandleButton.Tick += HandleButton_Tick;
                TimerHandleButton.Start();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - Init - 自动修正工具栏按钮");
            }

            try
            {
                // 同步评测器
                TimerSynchronizeComparePanel = new DispatcherTimer { Interval = new TimeSpan(8000000), IsEnabled = true };
                TimerSynchronizeComparePanel.Tick += TimerSynchronizeComparePanel_Tick;
                TimerSynchronizeComparePanel.Start();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - Init - 自动修正同步器");
            }

            try
            {
                // 自动编译并添加错误列表
                TimerAutoCompilation = new DispatcherTimer { IsEnabled = false };
                TimerAutoCompilation.Tick += TimerAutoCompilation_Tick;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - Init - 自动修正错误列表");
            }

            // 加载模块
            try
            {
                foreach (var iter in AppInfo.Config["ide"]["module"])
                    AppInfo.Modules.Add(iter.ToString(),
                        new ModuleInfo(Path.Combine(AppInfo.Path, "Config", "Module", iter.ToString(), "module.cfg"),
                        Path.Combine(AppInfo.Path, "Config", "Module", iter.ToString(), "cache.cfg"),
                        iter.ToString()));

                AppInfo.ModuleFilter = "";
                foreach (var iter in AppInfo.Modules.ToList())
                {
                    if (iter.Value.Obj == null)
                        NotificationsManager.CreateMessage()
                                            .Animates(true)
                                            .AnimationInDuration(0.3)
                                            .AnimationOutDuration(0.3)
                                            .Accent(Brushes.Red)
                                            .Background("#363636")
                                            .HasBadge("错误")
                                            .HasMessage($"无法加载模块 {iter.Key}")
                                            .Dismiss().WithButton("忽略", null)
                                            .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                            .Queue();
                    else
                    {
                        AppInfo.ModuleFilter += iter.Key + " 代码|";
                        for (int suffix = 0; suffix < iter.Value.CodeSuffix.Length; ++suffix)
                        {
                            AppInfo.ModuleFilter += "*" + iter.Value.CodeSuffix[suffix] + (suffix == iter.Value.CodeSuffix.Length - 1 ? "|" : ";");
                            AppInfo.ReflexToModule.Add(iter.Value.CodeSuffix[suffix], iter.Key);
                        }
                    }
                }

                AppInfo.ModuleFilter += "所有文件|*.*";
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                NotificationsManager.CreateMessage()
                                    .Animates(true)
                                    .AnimationInDuration(0.3)
                                    .AnimationOutDuration(0.3)
                                    .Accent(Brushes.Red)
                                    .Background("#363636")
                                    .HasBadge("错误")
                                    .HasMessage("无法加载模块")
                                    .Dismiss().WithButton("忽略", null)
                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                    .Queue();
            }

            // 初始终端
            #region 初始终端
            /*
            try
            {
                Terminal.Foreground = new SolidColorBrush(Color.FromArgb(
                            (byte)AppInformation.config["ide"]["settings"]["terminal"]["fore"][0],
                            (byte)AppInformation.config["ide"]["settings"]["terminal"]["fore"][1],
                            (byte)AppInformation.config["ide"]["settings"]["terminal"]["fore"][2],
                            (byte)AppInformation.config["ide"]["settings"]["terminal"]["fore"][3]));
                Terminal.FontFamily = new FontFamily(AppInformation.config["ide"]["settings"]["terminal"]["font"]["name"].ToString());
                Terminal.FontSize = (double)AppInformation.config["ide"]["settings"]["terminal"]["font"]["size"];
                Terminal.FontWeight = AppInformation.config["ide"]["settings"]["terminal"]["font"]["weight"].ToString() == "bold" ?
                        FontWeights.Bold :
                        AppInformation.config["ide"]["settings"]["terminal"]["font"]["weight"].ToString() == "light" ? System.Windows.FontWeights.Light : System.Windows.FontWeights.Normal;

                // warning 
                // 使用 ${path} 而不是 $path
                string image = AppInformation.config["ide"]["settings"]["terminal"]["back"]["image"].ToString().Replace("${path}", AppInformation.Path);
                if (File.Exists(image))
                {
                    ImageBrush brush = new ImageBrush();
                    brush.ImageSource = Helper.GetBitmapImage(image);
                    brush.Opacity = (double)AppInformation.config["ide"]["settings"]["terminal"]["back"]["opacity"];
                    switch (AppInformation.config["ide"]["settings"]["terminal"]["back"]["stretch"].ToString())
                    {
                        case "none":
                            brush.Stretch = Stretch.None;
                            break;
                        case "fill":
                            brush.Stretch = Stretch.Fill;
                            break;
                        case "uniform":
                            brush.Stretch = Stretch.Uniform;
                            break;
                        case "uniformtofill":
                            brush.Stretch = Stretch.UniformToFill;
                            break;
                        default:
                            brush.Stretch = Stretch.None;
                            break;
                    }
                    Terminal.Background = brush;
                }

                // 初始化终端
                terminalHelper = new TerminalHelper(AppInformation.config["ide"]["settings"]["terminal"]["process"].ToString(), TerminalTick);

                Terminal.PreviewKeyDown += Terminal_KeyDown;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                new NotificationManager(this.Dispatcher).Show(
                    new NotificationContent
                    {
                        Title ="GraphLC IDE初始化错误",
                        Message ="初始终端时出错\n" + ex.Message,
                        Type = NotificationType.Error
                    });
            }*/
            #endregion

            // 用户模板
            AppInfo.CustomizeTipsList.Clear();
            foreach (var iter in AppInfo.Config["ide"]["settings"]["completion"]["list"])
            {
                try
                {
                    var list = iter as JArray;
                    AppInfo.CustomizeTipsList.Add(new EditorCompletionData(list[0].ToString(), list[1].ToString(), list[2].ToString(),
                        list[3].ToString() == "" ? null : Helper.GetBitmapImage(list[3].ToString().Replace("${path}", AppInfo.Path))));
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                    NotificationsManager.CreateMessage()
                                        .Animates(true)
                                        .AnimationInDuration(0.3)
                                        .AnimationOutDuration(0.3)
                                        .Accent(Brushes.Red)
                                        .Background("#363636")
                                        .HasBadge("错误")
                                        .HasMessage($"初始化模板 {iter} 时出现错误")
                                        .Dismiss().WithButton("忽略", null)
                                        .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                        .Queue();
                }
            }

            // 界面初始化
            HandleTabitemContent(); // 处理标签页（隐藏按钮等）

            // 文件树初始化
            FileTree.Tag = new FileTreeProperty();
            FileTreeOpenRootDirectory();

            // 文件树图标
            try
            {
                string dir = Path.Combine(AppInfo.Path, "Config");
                var cfg = AppInfo.Theme["window"]["filetree"]["button"];
                var undo = Path.Combine(dir, cfg["undo"].ToString());
                var redo = Path.Combine(dir, cfg["redo"].ToString());
                var parent = Path.Combine(dir, cfg["parent"].ToString());
                var refresh = Path.Combine(dir, cfg["refresh"].ToString());
                if (File.Exists(undo))
                    FileTreeButtonUndo.Source = Helper.GetBitmapImage(undo);
                if (File.Exists(redo))
                    FileTreeButtonRedo.Source = Helper.GetBitmapImage(redo);
                if (File.Exists(parent))
                    FileTreeButtonParent.Source = Helper.GetBitmapImage(parent);
                if (File.Exists(refresh))
                    FileTreeButtonRefresh.Source = Helper.GetBitmapImage(refresh);
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        /// <summary>
        /// 初始化界面布局
        /// </summary>
        private void InitLayout()
        {
            var layout = AppInfo.MainWindowProperty["main"]["layout"] as JObject;
            if (layout.ContainsKey("left"))
            {
                LayoutRoot.ColumnDefinitions[0].Width = ConversionGridLength((double)layout["left"]["width"]);

                int i = 0;
                foreach (var iter in layout["left"]["def"])
                {
                    LeftLayout.RowDefinitions[i].Height = ConversionGridLength((double)iter);
                    ++i;
                }
            }
            if (layout.ContainsKey("split1"))
                LayoutRoot.ColumnDefinitions[1].Width = ConversionGridLength((double)layout["split1"]["width"]);
            if (layout.ContainsKey("middle"))
            {
                LayoutRoot.ColumnDefinitions[2].Width = ConversionGridLength((double)layout["middle"]["width"]);

                int i = 0;
                foreach (var iter in layout["middle"]["def"])
                {
                    MiddleLayout.RowDefinitions[i].Height = ConversionGridLength((double)iter);
                    ++i;
                }
            }
            if (layout.ContainsKey("split2"))
                LayoutRoot.ColumnDefinitions[3].Width = ConversionGridLength((double)layout["split2"]["width"]);
            if (layout.ContainsKey("right"))
            {
                LayoutRoot.ColumnDefinitions[4].Width = ConversionGridLength((double)layout["right"]["width"]);

                int i = 0;
                foreach (var iter in layout["right"]["def"])
                {
                    RightLayout.RowDefinitions[i].Height = ConversionGridLength((int)iter);
                    ++i;
                }
            }
        }

        /// <summary>
        /// 根据配置文件加载主题
        /// </summary>
        /// <param name="cfg">配置文件</param>
        private void LoadThemeSettings(CfgLoader cfg)
        {
            AppInfo.Theme = new CfgLoader(Path.Combine(AppInfo.Path, "Config", cfg["ide"]["theme"].ToString()));
            if (AppInfo.Theme.Token == null)
            {
                ThemeManager.Current.ChangeTheme(this, "Dark.Blue");
                NotificationsManager.CreateMessage()
                                    .Animates(true)
                                    .AnimationInDuration(0.3)
                                    .AnimationOutDuration(0.3)
                                    .Accent(Brushes.Red)
                                    .Background("#363636")
                                    .HasBadge("错误")
                                    .HasMessage($"不存在的主题文件 {cfg["ide"]["theme"]}")
                                    .Dismiss().WithButton("忽略", null)
                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                    .Queue();
            }
            else
                ThemeManager.Current.ChangeTheme(this, AppInfo.Theme["name"].ToString());
        }

        /// <summary>
        /// 处理配置文件中的 GridLength
        /// </summary>
        /// <param name="value">如果为整数则返回 GridLength(value) ，否则返回 GridLength(Math.Abs(value), GridUnitType.Star)</param>
        /// <returns></returns>
        private GridLength ConversionGridLength(double value)
        {
            if (value >= 0)
                return new GridLength(value);
            return new GridLength(Math.Abs(value), GridUnitType.Star);
        }

        private double ConversionGridLength(GridLength value)
        {
            if (value.IsStar)
                return -1;
            return value.Value;
        }

        #endregion

        #region 终端

        /*
        /// <summary>
        /// 终端
        /// </summary>
        private TerminalHelper terminalHelper = null;

        private void TerminalTick(object x)
        {
            try
            {
                var information = (TerminalInformation)x;
                Task.Delay(1145).Wait();
                var r = "";
                while (!terminalHelper.TerminalProcess.HasExited)
                {
                    char[] readBlock = new char[1];
                    Task t = information.ReadStream.ReadAsync(readBlock, 0, 1);
                    t.Wait(1);
                    if (!t.IsCompleted) //卡住就输出
                    {
                        //错误流正在输出则等待
                        while (information.Type == TerminalType.Output && terminalHelper.isErrorOutput)
                            Task.Delay(50).Wait();
                        //terminalHelper.isErrorOutput = true;

                        if (r.Length > 0)
                        {
                            if (information.Type == TerminalType.Error)
                                terminalHelper.isErrorOutput = false;
                            this.Dispatcher.Invoke(() =>
                            {
                                TerminalScrollViewer.Visibility = Visibility.Visible;
                                TerminalLoading.Visibility = Visibility.Collapsed;
                                Terminal.Text += r;
                                Terminal.SelectionStart = Terminal.Text.Length;
                                Terminal.ScrollToEnd();
                                terminalHelper.TextStart = Terminal.Text.Length;
                                r = "";
                            });
                        }
                        //terminalHelper.isErrorOutput = false;
                    }
                    t.Wait();
                    r += readBlock[0];
                    if (information.Type == TerminalType.Error)
                        terminalHelper.isErrorOutput = true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "No process is associated with this object.")
                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }

            try
            {
                this.Dispatcher.Invoke((Action)delegate () {
                    TerminalLoading.Visibility = Visibility.Visible;
                    TerminalScrollViewer.Visibility = Visibility.Collapsed;
                    Terminal.Text = "";
                    TerminalLoading.Text = "终端链接丢失 (单击重启终端)";
                    this.Focus();
                });
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 终端按键
        /// </summary>
        /// <param name="sender">TextBox (Terminal)</param>
        /// <param name="e">参数</param>
        private void Terminal_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var _terminal = sender as TextBox;
                //Terminal.UndoLimit = -1;
                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                {
                    if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right
                        || e.Key != Key.X && e.Key != Key.Back && e.Key != Key.Delete)
                        return;
                }
                if (_terminal.SelectionStart - (e.Key == Key.Back ? 1 : 0) < terminalHelper.TextStart)
                {
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Enter)
                {
                    if (_terminal.Text.Length - terminalHelper.TextStart > 0)
                    {
                        string s = _terminal.Text.Substring(terminalHelper.TextStart);
                        _terminal.Text = _terminal.Text.Substring(0, terminalHelper.TextStart);

                        //注意：终端不支持清屏
                        //if (s.ToLower() == "cls")
                        //    _terminal.Clear();
                        terminalHelper.SendCommand(s + "\n");
                    }
                    else e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        /// <summary>
        /// 重启终端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TerminalLoading_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            terminalHelper = new TerminalHelper(AppInformation.config["ide"]["settings"]["terminal"]["process"].ToString(), TerminalTick);
        }
        */

        #endregion

        #region 自动语法检查

        /// <summary>
        /// 自动编译并添加错误列表
        /// </summary>
        private DispatcherTimer TimerAutoCompilation = null;
        private bool IsAutoCompilationExited = true;

        private void TimerAutoCompilation_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                {
                    if (!IsAutoCompilationExited) return;
                    if (tag.IsAutoCompile)
                    {
                        if (tag.Module != null)
                        {
                            if (tag.Module.ErrorAnalysisEnabled) // 如果这个模块支持识别错误
                            {
                                tag.IsAutoCompile = IsAutoCompilationExited = false;
                                var codeFile = Path.Combine(AppInfo.Path, "BuildCache", "Analysis", tag.Module.Name + DateTime.Now.ToString("_yyyyMMdd_HHmmssfffffff") + new FileInfo(area.Editor.FileName).Extension);
                                area.Editor.Save(codeFile);

                                var programDir = Path.Combine(AppInfo.Path, "BuildCache", "Analysis");
                                if (!Directory.Exists(programDir)) Directory.CreateDirectory(programDir);

                                new Task(() =>
                                {
                                    try
                                    {
                                        string message = "";
                                        Process p = GetCompilationProcess(tag.Module, tag.Module.GetHandledCommand(tag.Module.ErrorAnalysisCommand, codeFile).ESplit());
                                        p.EnableRaisingEvents = true;

                                        var standardOutput = new StringBuilder();
                                        var standardError = new StringBuilder();

                                        p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                                        {
                                            if (e.Data != null)
                                                standardOutput.AppendLine(e.Data);
                                        };
                                        p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                                        {
                                            if (e.Data != null)
                                                standardError.AppendLine(e.Data);
                                        };

                                        var task = new Task(() =>
                                        {
                                            p.Start();
                                            p.BeginErrorReadLine();
                                            p.BeginOutputReadLine();

                                            p.WaitForExit();
                                            p.Close();

                                            message = standardOutput.ToString() + (string.IsNullOrWhiteSpace(standardOutput.ToString()) ? "" : "\n") + standardError.ToString();
                                        });
                                        task.Start();

                                        if (!task.Wait(tag.Module.ErrorAnalysisTimeLimit))
                                        {
                                            try
                                            {
                                                p.Kill();
                                            }
                                            catch { }
                                        }
                                        else
                                        {
                                            this.Dispatcher.Invoke(() =>
                                            {
                                                var sel = ErrorList.SelectedIndex;
                                                var result = tag.Module.ErrorAnalysisPlugin.GetErrorList(message);

                                                var source = new List<GlcErrorAnalysisPlugin.ErrorInfo>();
                                                foreach (var iter in result)
                                                    source.Add(new GlcErrorAnalysisPlugin.ErrorInfo()
                                                    {
                                                        Line = iter.Line,
                                                        Type = iter.Type,
                                                        Description = iter.Description.Replace("\r", "").Replace("\n", "")
                                                    });

                                                ErrorList.ItemsSource = source;
                                                ErrorList.SelectedIndex = sel;
                                            });
                                        }

                                        try { File.Delete(codeFile); } catch { }

                                        this.Dispatcher.Invoke(() =>
                                        {
                                            IsAutoCompilationExited = true;
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.WriteErr(ex.Message, "MainWindow.xaml.cs - TimerAutoCompilation_Tick(thread)");
                                        this.Dispatcher.Invoke(() => TimerAutoCompilation.IsEnabled = false);
                                    }
                                }).Start();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - TimerAutoCompilation_Tick");
                TimerAutoCompilation.IsEnabled = false;
            }
        }

        private void TimerSynchronizeComparePanel_Tick(object? sender, EventArgs e)
        {
            if (TabArea.Items.Count > 0)
            {
                TextBoxCompareTimeout.Visibility = ButtonAddCompare.Visibility = ButtonClearCompare.Visibility = Visibility.Visible;
                ButtonClearCompare.IsEnabled = CompareTool.Items.Count > 0;
                SynchronizeComparePanel();
            }
            else
            {
                TextBoxCompareTimeout.Visibility = ButtonAddCompare.Visibility = ButtonClearCompare.Visibility = Visibility.Collapsed;
            }
        }

        #region 错误列表

        private void ErrorListSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (ErrorList.View is GridView gv)
                {
                    gv.Columns[2].Width = ErrorList.ActualWidth - gv.Columns[0].ActualWidth - gv.Columns[1].ActualWidth - 2;
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void ErrorListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ErrorList.SelectedItem is GlcErrorAnalysisPlugin.ErrorInfo item && TabArea.SelectedItem is TabItem tab && tab.Content is CodeArea area)
                {
                    area.Editor.CaretToLine(item.Line);
                    area.Editor.Focus();
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        #endregion

        #region 界面修正

        #region 配色修正

        /// <summary>
        /// 每1秒更新一次，修正 Menu及其子控件 的颜色
        /// </summary>
        private DispatcherTimer TimerHandleMenu = null;

        /// <summary>
        /// 修正菜单栏颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerHandleMenu_Tick(object sender, EventArgs e)
        {
            Helper.SetControlProperty(menu, HandleMenuFunciton);
        }

        /// <summary>
        /// 遍历 MenuItem 的回调函数
        /// </summary>
        /// <param name="x">MenuItem</param>
        void HandleMenuFunciton(object x)
        {
            try
            {
                if (AppInfo.Theme.Token == null) return;
                MenuItem item = (MenuItem)x;
                var jArray = (JObject)AppInfo.Theme["menuChild"];
                if (item.IsEnabled)
                {
                    item.Foreground = new SolidColorBrush(Color.FromArgb((byte)jArray["fore"][0], (byte)jArray["fore"][1], (byte)jArray["fore"][2], (byte)jArray["fore"][3]));
                }
                else
                {
                    item.Foreground = new SolidColorBrush(Color.FromArgb((byte)jArray["lock"][0], (byte)jArray["lock"][1], (byte)jArray["lock"][2], (byte)jArray["lock"][3]));
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        #region 修正内容

        /// <summary>
        /// 每1秒更新一次，修正工具栏和文件树中的 Button
        /// </summary>
        private DispatcherTimer TimerHandleButton = null;

        private void HandleButton_Tick(object? sender, EventArgs e)
        {
            try
            {
                try
                {
                    if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    {
                        // 工具栏
                        if (ButtonBuildProject.Visibility == Visibility.Visible)
                            ButtonBuildProject.IsEnabled = CanBuild(area.Editor);
                        if (ButtonRunProject.Visibility == Visibility.Visible)
                            ButtonRunProject.IsEnabled = CanRun(area.Editor);
                        if (ButtonJudge.Visibility == Visibility.Visible)
                            ButtonJudge.IsEnabled = CanJudge(area.Editor);
                    }
                }
                catch { }

                var tag = FileTree.Tag as FileTreeProperty;
                FileTreeButtonUndo.Opacity = tag.Position == 0 ? 0.4 : 1.0;
                FileTreeButtonRedo.Opacity = tag.Position == tag.Stack.Count - 1 ? 0.4 : 1.0;
                FileTreeButtonParent.Opacity = tag.Path == "我的电脑" ? 0.4 : 1.0;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        #endregion

        #region 文件操作

        CodeArea CreateCodeArea(string file)
        {
            var area = new CodeArea();
            var editor = area.Editor;

            editor.AllowDrop = false;
            editor.ContextMenu = TryFindResource("CodePageContextMenu") as ContextMenu;
            editor.Focusable = true;
            editor.ShowLineNumbers = true;
            editor.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            editor.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            editor.WordWrap = (bool)AppInfo.Config["ide"]["settings"]["editor"]["wordwrap"];
            editor.Foreground = new SolidColorBrush(Color.FromArgb(
                        (byte)AppInfo.Config["ide"]["settings"]["editor"]["fore"][0],
                        (byte)AppInfo.Config["ide"]["settings"]["editor"]["fore"][1],
                        (byte)AppInfo.Config["ide"]["settings"]["editor"]["fore"][2],
                        (byte)AppInfo.Config["ide"]["settings"]["editor"]["fore"][3]));
            editor.LineNumbersForeground = new SolidColorBrush(Color.FromArgb(
                        (byte)AppInfo.Config["ide"]["settings"]["editor"]["linefore"][0],
                        (byte)AppInfo.Config["ide"]["settings"]["editor"]["linefore"][1],
                        (byte)AppInfo.Config["ide"]["settings"]["editor"]["linefore"][2],
                        (byte)AppInfo.Config["ide"]["settings"]["editor"]["linefore"][3]));
            editor.FontFamily = new FontFamily(AppInfo.Config["ide"]["settings"]["editor"]["font"]["name"].ToString());
            editor.FontSize = (double)AppInfo.Config["ide"]["settings"]["editor"]["font"]["size"];
            editor.FontWeight = AppInfo.Config["ide"]["settings"]["editor"]["font"]["weight"].ToString() == "bold" ?
                    FontWeights.Bold :
                    AppInfo.Config["ide"]["settings"]["editor"]["font"]["weight"].ToString() == "light" ? System.Windows.FontWeights.Light : System.Windows.FontWeights.Normal;

            // 背景
            string image = AppInfo.Config["ide"]["settings"]["editor"]["back"]["image"].ToString().Replace("${path}", AppInfo.Path);
            if (File.Exists(image))
            {
                ImageBrush brush = new ImageBrush();
                brush.ImageSource = Helper.GetBitmapImage(image);
                brush.Opacity = (double)AppInfo.Config["ide"]["settings"]["editor"]["back"]["opacity"];
                switch (AppInfo.Config["ide"]["settings"]["editor"]["back"]["stretch"].ToString())
                {
                    case "none":
                        brush.Stretch = Stretch.None;
                        break;
                    case "fill":
                        brush.Stretch = Stretch.Fill;
                        break;
                    case "uniform":
                        brush.Stretch = Stretch.Uniform;
                        break;
                    case "uniformtofill":
                        brush.Stretch = Stretch.UniformToFill;
                        break;
                    default:
                        brush.Stretch = Stretch.None;
                        break;
                }

                editor.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                area.Background = brush;

                // 代码编辑区
                // 底部遮罩效果
                var bottomControlColor = (TryFindResource("MahApps.Brushes.Control.Background") as SolidColorBrush).Color;
                bottomControlColor.A = 220;
                area.BottomControlCollection.Background = new SolidColorBrush(bottomControlColor);
            }

            // 高亮
            if (AppInfo.ReflexToModule.ContainsKey(new FileInfo(file).Extension))
            {
                var module = AppInfo.Modules[new FileInformation(file).ModuleName];
                var hname = "";

                if (AppInfo.Theme["name"].ToString().Split('.')[0].ToLower() == "light")
                    hname = module.LightHighlight;
                else if (AppInfo.Theme["name"].ToString().Split('.')[0].ToLower() == "dark")
                    hname = module.DarkHighlight;

                if (module.HighlightEnabled)
                {
                    var highlight = Path.Combine(AppInfo.Path, "Config", "Module", module.Name, hname);
                    if (File.Exists(highlight))
                        using (StreamReader s = new StreamReader(highlight, Encoding.UTF8))
                        {
                            using (XmlTextReader reader = new XmlTextReader(s))
                            {
                                editor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
                            }
                        }
                }
            }

            // 折叠
            editor.FoldingManager = ICSharpCode.AvalonEdit.Folding.FoldingManager.Install(editor.TextArea);
            editor.FoldingStrategy.UpdateFoldings(editor.FoldingManager, editor.Document);

            // 搜素
            // SearchBar = SearchPanel.Install(this);
            // SearchBar.MarkerBrush = (Application.Current.MainWindow as MetroWindow).TryFindResource("MahApps.Brushes.DataGrid.Selection.Background.Inactive") as Brush;

            foreach (var iter in editor.TextArea.LeftMargins)
            {
                if (iter is ICSharpCode.AvalonEdit.Folding.FoldingMargin foldingMargin)
                {
                    var color = (Foreground as SolidColorBrush).Color;
                    foldingMargin.FoldingMarkerBrush = new SolidColorBrush(Color.FromArgb((byte)(color.A * 0.5), color.R, color.G, color.B));
                    foldingMargin.SelectedFoldingMarkerBrush = Foreground;
                    foldingMargin.FoldingMarkerBackgroundBrush = foldingMargin.SelectedFoldingMarkerBackgroundBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                }
                else if (iter is ICSharpCode.AvalonEdit.Editing.LineNumberMargin lineMargin)
                {
                    var margin = lineMargin.Margin;
                    lineMargin.Margin = new Thickness(20, margin.Top, 20, margin.Bottom);
                }
            }

            editor.PreviewKeyDown += (sender, e) =>
            {
                try
                {
                    if (sender is CodeEditor editor)
                        if (editor.CompletionTips != null)
                        {
                            if (e.Key == Key.Tab && (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift)))
                            {
                                editor.CompletionTips.Close();
                                e.Handled = true;
                            }
                            else if ((e.Key == Key.Up || e.Key == Key.Down) && editor.CompletionTips.CompletionList.ListBox.Items.Count == 0)
                            {
                                editor.CompletionTips.Close();
                            }
                        }
                }
                catch { }
            };
            editor.TextArea.TextEntering += (sender, e) =>
            {
                bool CheckSymbol(char c) => char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c);
                bool Check(char c) => CheckSymbol(c) || char.IsDigit(c);

                try
                {
                    if (editor.Tag is EditorInfo tag)
                    {
                        try
                        {
                            if ((tag.Module?.SupportCompletion).GetValueOrDefault())
                            {
                                // 检查光标位置前一个字符是否为空字符或标点符号
                                bool y = true;
                                if (editor.CaretOffset > 0)
                                    y = Check(editor.Text[editor.CaretOffset - 1]);
                                if (!Check(e.Text[0]) && y)
                                {
                                    // 新建一个提示框
                                    editor.CompletionTips = new ICSharpCode.AvalonEdit.CodeCompletion.CompletionWindow(editor.TextArea);

                                    // 初始化样式
                                    ThemeManager.Current.ChangeTheme(editor.CompletionTips, AppInfo.Theme["name"].ToString());
                                    editor.CompletionTips.Foreground = new SolidColorBrush(Color.FromArgb((byte)AppInfo.Theme["window"]["fore"][0],
                                                                                                   (byte)AppInfo.Theme["window"]["fore"][1],
                                                                                                   (byte)AppInfo.Theme["window"]["fore"][2],
                                                                                                   (byte)AppInfo.Theme["window"]["fore"][3]));
                                    editor.CompletionTips.Background = new SolidColorBrush(Color.FromArgb((byte)AppInfo.Theme["window"]["back"][0],
                                                                                                   (byte)AppInfo.Theme["window"]["back"][1],
                                                                                                   (byte)AppInfo.Theme["window"]["back"][2],
                                                                                                   (byte)AppInfo.Theme["window"]["back"][3]));

                                    // 临时列表
                                    var list = new List<EditorCompletionData>();

                                    // 合并 模块补全列表 和 用户定义补全列表
                                    foreach (var iter in tag.Module.CompletionList) list.Add(iter);
                                    foreach (var iter in AppInfo.CustomizeTipsList) list.Add(iter);

                                    // 排序
                                    list.Sort((EditorCompletionData x, EditorCompletionData y) =>
                                    {
                                        return x.Content.ToString().CompareTo(y.Content.ToString());
                                    });

                                    foreach (var iter in list)
                                        editor.CompletionTips.CompletionList.CompletionData.Add(iter.Clone() as EditorCompletionData);

                                    editor.CompletionTips.Show();
                                    editor.CompletionTips.Closed += (sender, e) => editor.CompletionTips = null;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteErr(ex.Message, "MainWindows.xaml.cs");
                        }

                        try
                        {
                            if (editor.CompletionTips != null)
                            {
                                if ((tag.Module?.SupportCompletion).GetValueOrDefault() && e.Text.Length > 0 && CheckSymbol(e.Text[0]))
                                {
                                    bool y = false;
                                    foreach (string iter in tag.Module.CompletionKey)
                                        if (e.Text == iter)
                                        {
                                            y = true;
                                            break;
                                        }
                                    if (y)
                                        editor.CompletionTips.CompletionList.RequestInsertion(e);
                                    else
                                        editor.CompletionTips.Close();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteErr(ex.Message, "MainWindows.xaml.cs");
                        }
                    }
                }
                catch { }
            };
            editor.TextChanged += (sender, e) =>
            {
                try
                {
                    if (sender is CodeEditor editor && editor.Tag is EditorInfo tag)
                    {
                        tag.BindingTabItem.Header = new FileInfo(editor.FileName).Name.Replace("_", "__").Replace("&", "&&") + "*";
                        tag.IsAutoCompile = true;
                    }
                }
                catch { }
            };
            editor.TextSaved += (sender, e) =>
            {
                try
                {
                    if (sender is CodeEditor editor && editor.Tag is EditorInfo tag)
                        tag.BindingTabItem.Header = new FileInfo(editor.FileName).Name.Replace("_", "__").Replace("&", "&&");
                }
                catch { }
            };

            return area;
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="srcPath">文件路径</param>
        /// <param name="supportSave">文件默认保存</param>
        /// <param name="addToRecentFile">是否添加至最近的项</param>
        /// <param name="syncFileTree">是否同步文件树</param>
        void OpenFile(string srcPath, bool supportSave = true, bool addToRecentFile = true, bool syncFileTree = true)
        {
            try
            {
                if ((AppInfo.Config["ide"]["settings"]["editor"] as JObject).ContainsKey("encode"))
                {
                    var encode = AppInfo.Config["ide"]["settings"]["editor"]["encode"] as JObject;
                    if (encode["open"].ToString() == "Auto")
                        OpenFile(srcPath, null, supportSave, addToRecentFile, syncFileTree);
                    else
                        OpenFile(srcPath, new EncodingHelper(encode["open"].ToString()).Encode, supportSave, addToRecentFile, syncFileTree);
                }
                else OpenFile(srcPath, null, supportSave, addToRecentFile, syncFileTree);
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }


        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="srcPath">文件路径</param>
        /// <param name="encode">文件编码</param>
        /// <param name="supportSave">文件默认保存</param>
        /// <param name="addToRecentFile">是否添加至最近的项</param>
        /// <param name="syncFileTree">是否同步文件树</param>
        void OpenFile(string srcPath, Encoding encode, bool supportSave = true, bool addToRecentFile = true, bool syncFileTree = true)
        {
            try
            {
                var area = CreateCodeArea(srcPath);
                area.Editor.SupportSave = supportSave;

                if (encode == null)
                    using (var stream = new FileStream(srcPath, FileMode.Open, FileAccess.Read))
                        area.Editor.Encoding = stream.GetEncoding();
                else
                    area.Editor.Encoding = encode;

                area.Editor.Load(srcPath);
                area.EditorEncoding.Text = new EncodingHelper(area.Editor.Encoding).Name ?? "UTF-8";
                area.EditorEncoding.Tag = false;

                area.EditorEncoding.SelectionChanged += async (sender, e) =>
                {
                    try
                    {
                        if (sender is ComboBox combobox)
                        {
                            if (combobox.Tag is bool tag && tag)
                                combobox.Tag = false;
                            else
                            {
                                var text = combobox.Text;
                                var dialogSettings = new MetroDialogSettings()
                                {
                                    AffirmativeButtonText = "重新打开",
                                    NegativeButtonText = "保存",
                                    FirstAuxiliaryButtonText = "取消",
                                    ColorScheme = MetroDialogColorScheme.Theme
                                };
                                var result = await this.ShowMessageAsync("更改编码方式", "您修改了编码方式，是否以此编码重新打开文件或保存文件？", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);
                                if (result == MessageDialogResult.FirstAuxiliary)
                                {
                                    combobox.Tag = true;
                                    combobox.Text = text;
                                    combobox.Tag = false;
                                }
                                else if (result == MessageDialogResult.Affirmative) // 重新打开
                                {
                                    area.Editor.Encoding = new EncodingHelper(combobox.Text).Encode;
                                    area.Editor.Load(area.Editor.FileName);
                                }
                                else if (result == MessageDialogResult.Negative) // 保存
                                {
                                    area.Editor.Encoding = new EncodingHelper(combobox.Text).Encode;
                                    area.Editor.ESave();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                    }
                };

                var item = new MetroTabItem
                {
                    Content = area,
                    Header = new FileInfo(srcPath).Name.Replace("_", "__").Replace("&", "&&"),
                    CloseButtonEnabled = true,
                    CloseButtonMargin = new Thickness(0, 7, 0, 0)
                };

                var etag = new EditorInfo(srcPath, item);
                area.Editor.Tag = etag;

                // 待修改
                // 插件接口完善后，此处替换为插件初始化
                etag.Module?.EditorPlugin.EditorLoaded(area.Editor);

                TabArea.Items.Add(item);
                TabArea.SelectedIndex = TabArea.Items.Count - 1; //定位到这个TabItem

                // 文件树同步
                if ((bool)AppInfo.Config["ide"]["settings"]["file-tree"]["sync"] && syncFileTree)
                {
                    var dir = srcPath.Substring(0, srcPath.LastIndexOf("\\"));
                    FileTreeOpenDirectory(dir);
                    (FileTree.Tag as FileTreeProperty).Path = dir;
                    HandleButton_Tick(null, null);
                }

                if (addToRecentFile)
                {
                    for (int i = RecentFiles.Items.Count - 1; i >= 0; --i)
                    {
                        try
                        {
                            var listitem = RecentFiles.Items[i] as ListBoxItem;
                            if (listitem.Tag.ToString().ToLower() == srcPath.ToLower())
                                RecentFiles.Items.RemoveAt(i);
                        }
                        catch (Exception ex)
                        {
                            Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                        }
                    }
                    // 向 "最近打开的项" 中添加项目
                    // 动画设置
                    // 此动画所需的最小动画等级 0-2
                    var animationMinLevel = 2;

                    ListBoxItem listboxitem = new ListBoxItem() { Content = srcPath.Substring(srcPath.LastIndexOf('\\') + 1), Tag = srcPath, ToolTip = srcPath };
                    if ((int)AppInfo.Config["ide"]["settings"]["animation"]["level"] >= animationMinLevel)
                    {
                        listboxitem.Opacity = 0.0;
                        listboxitem.Margin = new Thickness(-12, 0, 0, 0);
                    }
                    RecentFiles.Items.Insert(0, listboxitem);

                    // 启动动画
                    AnimationHelper.BeginAnimation(animationMinLevel, new Tuple<Window, FrameworkElement>(this, listboxitem), AnimationHelper.ControlAnimationOpacityEnter);
                    AnimationHelper.BeginAnimation(animationMinLevel, new Tuple<Window, FrameworkElement>(this, listboxitem), AnimationHelper.ControlAnimationEnter);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                NotificationsManager.CreateMessage()
                                    .Animates(true)
                                    .AnimationInDuration(0.3)
                                    .AnimationOutDuration(0.3)
                                    .Accent(Brushes.Red)
                                    .Background("#363636")
                                    .HasBadge("错误")
                                    .HasMessage("无法打开文件")
                                    .Dismiss().WithButton("忽略", null)
                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                    .Queue();
            }
        }

        #endregion

        #region 标签页

        /// <summary>
        /// 判断是否有没保存的标签页
        /// </summary>
        /// <returns></returns>
        private bool HasUnsavedTabs()
        {
            //遍历所有标签页，判断是否保存
            foreach (var tab in TabArea.Items)
            {
                try //加 try-catch 只是为了防止标签页混进奇怪的东西（虽然不太可能）
                {
                    if (tab is TabItem item && item.Content is CodeArea area && !area.Editor.IsSaved)
                        return true;
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                }
            }

            return false;
        }

        /// <summary>
        /// 关闭所有标签页
        /// </summary>
        /// <param name="closeWindow">是否关闭窗体</param>
        private async Task CloseAllTabs(bool closeWindow)
        {
            try
            {
                // 是否退出
                bool r = true;
                for (int i = TabArea.Items.Count - 1; i >= 0; --i)
                    if (TabArea.Items[i] is TabItem item && item.Content is CodeArea area)
                    {
                        if (area.Editor.IsSaved)
                            TabArea.Items.RemoveAt(i);
                        else
                        {
                            var dialogSettings = new MetroDialogSettings()
                            {
                                AffirmativeButtonText = "放弃",
                                NegativeButtonText = "保存",
                                FirstAuxiliaryButtonText = "取消",
                                ColorScheme = MetroDialogColorScheme.Theme
                            };

                            var result = await this.ShowMessageAsync("保存文件", "是否保存此文件\n" + area.Editor.FileName, MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);

                            if (result == MessageDialogResult.FirstAuxiliary)
                            {
                                r = false;
                                continue;
                            }
                            else if (result == MessageDialogResult.Negative) // 按下 "保存" 按钮
                                area.Editor.ESave();

                            TabArea.Items.RemoveAt(i);
                        }
                    }

                if (closeWindow && r)
                    this.Close();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        /// <summary>
        /// 处理标签页
        /// </summary>
        void HandleTabitemContent()
        {
            try
            {
                if (TabArea.SelectedItem is TabItem tabitem && tabitem.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                {
                    HandleButton_Tick(null, null);

                    ComboBoxCompileBits.Items.Clear();
                    ComboBoxCompileModes.Items.Clear();
                    ComboBoxCompileOptions.Items.Clear();

                    CompareTool.Items.Clear();
                    foreach (var iter in tag.JudgePoints)
                        AddComparePoint(iter.Item1, iter.Item2);

                    TextBoxCompareTimeout.Text = tag.JudgeTimeout.ToString();

                    if (tag.Module != null)
                    {
                        ButtonBuildProject.Visibility = Visibility.Visible;
                        ButtonRunProject.Visibility = Visibility.Visible;
                        ButtonJudge.Visibility = Visibility.Visible;

                        foreach (string s in tag.Module.Bits) ComboBoxCompileBits.Items.Add(s);
                        foreach (string s in tag.Module.Modes) ComboBoxCompileModes.Items.Add(s);
                        foreach (string s in tag.Module.Options) ComboBoxCompileOptions.Items.Add(s);

                        ComboBoxCompileBits.Visibility = Visibility.Visible;
                        ComboBoxCompileModes.Visibility = Visibility.Visible;
                        ComboBoxCompileOptions.Visibility = Visibility.Visible;
                        ComboBoxCompileBits.Text = tag.Module.Bit;
                        ComboBoxCompileModes.Text = tag.Module.Mode;
                        ComboBoxCompileOptions.Text = tag.Module.Option;

                        if (TimerAutoCompilation.IsEnabled = tag.Module.ErrorAnalysisEnabled)
                        {
                            TimerAutoCompilation.Interval = TimeSpan.FromMilliseconds(tag.Module.ErrorAnalysisIntervals);
                            TimerAutoCompilation.IsEnabled = true;
                            TimerAutoCompilation.Start();
                        }
                    }
                    else
                    {
                        ButtonBuildProject.Visibility = Visibility.Collapsed;
                        ButtonRunProject.Visibility = Visibility.Collapsed;
                        ButtonJudge.Visibility = Visibility.Collapsed;
                        ComboBoxCompileBits.Visibility = Visibility.Collapsed;
                        ComboBoxCompileModes.Visibility = Visibility.Collapsed;
                        ComboBoxCompileOptions.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    ButtonBuildProject.Visibility = Visibility.Collapsed;
                    ButtonRunProject.Visibility = Visibility.Collapsed;
                    ButtonJudge.Visibility = Visibility.Collapsed;
                    ComboBoxCompileBits.Visibility = Visibility.Collapsed;
                    ComboBoxCompileModes.Visibility = Visibility.Collapsed;
                    ComboBoxCompileOptions.Visibility = Visibility.Collapsed;
                    CompareTool.Items.Clear();

                    TimerAutoCompilation.IsEnabled = false;
                }
            }
            catch { }
        }

        private async void TabArea_TabItemClosingEvent(object sender, BaseMetroTabControl.TabItemClosingEventArgs e)
        {
            try
            {
                if (sender is MetroTabControl tabControl)
                {
                    e.Cancel = true;

                    if (e.ClosingTabItem.Content is CodeArea area && !area.Editor.IsSaved)
                    {
                        var dialogSettings = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = "放弃",
                            NegativeButtonText = "保存",
                            FirstAuxiliaryButtonText = "取消",
                            ColorScheme = MetroDialogColorScheme.Theme
                        };

                        var result = await this.ShowMessageAsync("保存文件", "是否需要保存此文件？", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);
                        if (result == MessageDialogResult.FirstAuxiliary)
                            return;
                        else
                        {
                            if (result == MessageDialogResult.Negative) //按下 "保存" 按钮
                                area.Editor.ESave();
                        }
                    }

                    tabControl.Items.Remove(e.ClosingTabItem);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindows.xaml.cs");
            }
        }

        /// <summary>
        /// 切换标签页
        /// </summary>
        private void TabAreaSelectionChanged(object sender, SelectionChangedEventArgs e) => HandleTabitemContent();

        #endregion

        #region Binding

        //新建文件
        private void CommandBinding_RoutedCreateFile_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CommandBinding_RoutedCreateFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = AppInfo.ModuleFilter,
                    Title = "选择文件保存位置"
                };

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var encode = Encoding.UTF8;

                    if ((AppInfo.Config["ide"]["settings"]["editor"] as JObject).ContainsKey("encode"))
                    {
                        var obj = AppInfo.Config["ide"]["settings"]["editor"]["encode"] as JObject;
                        if (obj.ContainsKey("create"))
                            encode = new EncodingHelper(obj["create"].ToString()).Encode;
                    }

                    StreamWriter w = new StreamWriter(fileDialog.FileName, false, encode);
                    w.Close();
                    OpenFile(fileDialog.FileName, encode);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        //打开文件
        private void CommandBinding_RoutedOpenFile_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CommandBinding_RoutedOpenFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
                fileDialog.Filter = AppInfo.ModuleFilter;
                fileDialog.CheckFileExists = true;
                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OpenFile(fileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedHandleFile_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = TabArea.Items.Count > 0;

        private void CommandBinding_RoutedSaveFile_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem tabitem && tabitem.Content is CodeArea area)
                    e.CanExecute = !area.Editor.IsSaved;
            }
            catch (Exception) { e.CanExecute = false; }
        }

        private void CommandBinding_RoutedSaveFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem tabitem && tabitem.Content is CodeArea area)
                {
                    if (area.Editor.SupportSave)
                    {
                        area.Editor.ESave();
                    }
                    else
                    {
                        System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog
                        {
                            Filter = AppInfo.ModuleFilter,
                            Title = "选择文件保存位置"
                        };
                        if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            area.Editor.FileName = fileDialog.FileName;
                            area.Editor.ESave();
                            area.Editor.SupportSave = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedSaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // 注意
            // 此处未使用异步，保存大文件可能导致卡顿
            try
            {
                if (TabArea.SelectedItem is TabItem tab && tab.Content is CodeArea area)
                {
                    System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog
                    {
                        Filter = AppInfo.ModuleFilter,
                        Title = "选择文件保存位置"
                    };

                    if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        area.Editor.Save(fileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedSaveAll_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = HasUnsavedTabs();

        private void CommandBinding_RoutedSaveAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //遍历所有标签页，判断是否保存，如果没有则保存
            foreach (var tab in TabArea.Items)
            {
                try
                {
                    if (tab is TabItem item && item.Content is CodeArea area && !area.Editor.IsSaved)
                        area.Editor.ESave();
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                }
            }
        }

        private async void CommandBinding_RoutedCloseFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area && !area.Editor.IsSaved)
                {
                    var dialogSettings = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "放弃",
                        NegativeButtonText = "保存",
                        FirstAuxiliaryButtonText = "取消",
                        ColorScheme = MetroDialogColorScheme.Theme
                    };
                    var result = await this.ShowMessageAsync("保存文件", "是否需要保存此文件？", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, dialogSettings);

                    if (result == MessageDialogResult.FirstAuxiliary)
                        return;
                    else if (result == MessageDialogResult.Negative) // 按下 "保存" 按钮
                        area.Editor.ESave();
                }

                // 删除此标签页
                TabArea.Items.Remove(TabArea.SelectedItem);
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private async void CommandBinding_RoutedCloseAllTabs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await CloseAllTabs(false);
        }

        private void CommandBinding_RoutedQuit_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private async void CommandBinding_RoutedQuit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await CloseAllTabs(true);
        }

        private void CommandBinding_RoutedUndo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    e.CanExecute = area.Editor.CanUndo;
            }
            catch (Exception ex)
            {
                e.CanExecute = false;
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedUndo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    area.Editor.Undo();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedRedo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    e.CanExecute = area.Editor.CanRedo;
            }
            catch (Exception ex)
            {
                e.CanExecute = false;
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedRedo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    area.Editor.Redo();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedEdit_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = TabArea.Items.Count > 0;

        private void CommandBinding_RoutedCut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    area.Editor.Cut();
            }
            catch { }
        }

        private void CommandBinding_RoutedCopy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    area.Editor.Copy();
            }
            catch { }
        }

        private void CommandBinding_RoutedPaste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    area.Editor.Paste();
            }
            catch { }
        }

        private void CommandBinding_RoutedSelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    area.Editor.SelectAll();
            }
            catch { }
        }

        /// <summary>
        /// 弹出搜索框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBinding_RoutedFind_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    new SearchBar(area.Editor) { Owner = this }.Show();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        /// <summary>
        /// 获取给定的 CodeEditor 的代码是否能构建
        /// </summary>
        private bool CanBuild(CodeEditor editor)
        {
            try
            {
                if (editor.Tag is EditorInfo tag)
                    return tag.Module != null && editor.State == CodeEditorState.Normal && tag.Module.BuildEnabled;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 获取给定的 CodeEditor 的代码能否运行
        /// </summary>
        /// <param name="editor"></param>
        /// <returns></returns>
        private bool CanRun(CodeEditor editor)
        {
            try
            {
                if (editor.Tag is EditorInfo tag)
                    return tag.Module != null && editor.State != CodeEditorState.Lock && tag.Module.RunEnabled && File.Exists(tag.Module.GetHandledCommand(tag.Module.BuildProgramPath, editor.FileName));
            }
            catch { }
            return false;
        }

        // 注意：评测机将在后面的版本被移出到 OIHelper 插件
        /// <summary>
        /// 获取给定的 CodeEditor 的代码能否评测
        /// </summary>
        private bool CanJudge(CodeEditor editor)
        {
            try
            {
                if (editor.Tag is EditorInfo tag)
                    return CompareTool.Items.Count > 0 && tag.Module != null && editor.State != CodeEditorState.Lock && tag.Module.JudgeEnabled;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 从模块返回编译器进程（未启动）
        /// </summary>
        private Process GetCompilationProcess(ModuleInfo module, string[] command)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;


            p.StartInfo.FileName = command[0];
            for (int i = 1; i < command.Length; ++i)
            {
                if (command[i].Length > 0 && command[i][0] != '$')
                    p.StartInfo.Arguments += " " + command[i].MakeCommand();
                else
                {
                    if (command[i] == "${if}")
                    {
                        //if (i + 1 >= s.Count) 

                        List<string> condition = new List<string>();
                        while (command[++i] != "${then}")
                        {
                            if (i >= command.Length)
                                throw new Exception("在第 " + i.ToString() + " 个命令块，命令长度错误");
                            condition.Add(command[i]);
                        }

                        while (condition.Count != 1)
                        {
                            if (condition.Count < 3)
                                throw new Exception("在第 " + i.ToString() + " 个命令块，无法识别的 ${if} 条件");

                            //值 运算符 值
                            string value1 = condition[0], compare = condition[1], value2 = condition[2];
                            for (int k = 0; k < 3; ++k) condition.RemoveAt(0);

                            if (value1.Contains('.'))
                                this.Dispatcher.Invoke(() =>
                                {
                                    value1 = value1.GetObjectFromFramewrokElement(this).ToString();
                                });
                            if (value2.Contains('.'))
                                this.Dispatcher.Invoke(() =>
                                {
                                    value2 = value2.GetObjectFromFramewrokElement(this).ToString();
                                });
                            if (compare == "&&")
                            {
                                if (value1 != "0" && value1.ToLower() != "false" && value2 != "0" && value2.ToLower() != "false")
                                    condition.Insert(0, "true");
                                else
                                    condition.Insert(0, "false");
                            }
                            if (compare == "||")
                            {
                                if ((value1 != "0" && value1.ToLower() != "false") || (value2 != "0" && value2.ToLower() != "false"))
                                    condition.Insert(0, "true");
                                else
                                    condition.Insert(0, "false");
                            }
                            else if (compare == "==")
                                condition.Insert(0, (value1 == value2 ? "true" : "false"));
                            else if (compare == "!=")
                                condition.Insert(0, (value1 == value2 ? "false" : "true"));
                            else
                                throw new Exception("在第 " + i.ToString() + " 个命令块，无法识别的比较符 " + compare);
                        }

                        bool conditionTrue = condition[0] != "0" && condition[0].ToLower() != "false";
                        bool trueBlock = false;
                        if (!conditionTrue)
                        {
                            int k = i;
                            while (command[++k] != "${else}" && command[k] != "${end}")
                            {
                                if (k >= command.Length)
                                    throw new Exception("在第 " + k.ToString() + " 个命令块，命令长度错误");
                            }
                            if (command[k] != "${else}")
                                i = k - 1;
                        }
                        while (command[++i] != "${end}")
                        {
                            if (i >= command.Length)
                                throw new Exception("在第 " + i.ToString() + " 个命令块，命令长度错误");
                            if (command[i] == "${else}") trueBlock = true;
                            else if ((conditionTrue && !trueBlock) || (!conditionTrue && trueBlock))
                                p.StartInfo.Arguments += " " + command[i].Replace("$$", "$").MakeCommand();
                        }
                    }
                    else
                        p.StartInfo.Arguments += " " + command[i].Replace("$$", "$").MakeCommand();
                }
            }

            return p;
        }

        /// <summary>
        /// 从给定的 CodeEditor 构建源代码
        /// </summary>
        /// <param name="editor">代码编辑器</param>
        /// <param name="function">构建完成后调用的函数</param>
        /// <returns>构建的结果, null 为构建失败</returns>
        private async Task BuildSolution(CodeEditor editor, Action function = null)
        {
            if (!CanBuild(editor)) return;
            if (editor.Tag is EditorInfo tag)
            {
                // note : 新版本准备删除这段代码
                // 通过状态栏显示构建结果
                MenuViewViewerClick(null, null);
                TabViewArea.SelectedIndex = 0; //查看器标签页定位到 "构建"

                Output.Text = "开始构建";
                editor.State = CodeEditorState.Compile;

                var codeFile = editor.FileName;
                var programFile = tag.Module.GetHandledCommand(tag.Module.BuildProgramPath, editor.FileName);
                var cacheFile = "";

                var p = GetCompilationProcess(tag.Module, tag.Module.GetHandledCommand(tag.Module.BuildCommand, codeFile, tag.Module.AttachCommand).ESplit());
                p.EnableRaisingEvents = true;

                Output.Text += "\n\n向 " + p.StartInfo.FileName + " 发送命令 : " + p.StartInfo.FileName + p.StartInfo.Arguments;
                Output.Text += "\n代码文件 : " + codeFile;
                Output.Text += "\n解决方案 : " + programFile;

                try
                {
                    if (File.Exists(programFile))
                    {
                        cacheFile = Path.Combine(AppInfo.Path, "BuildCache", programFile.Replace(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/", "_").Replace(":", "_").Replace(" ", "_"));
                        if (!Directory.Exists(Path.Combine(AppInfo.Path, "BuildCache")))
                            Directory.CreateDirectory(Path.Combine(AppInfo.Path, "BuildCache"));

                        if (File.Exists(cacheFile))
                            File.Delete(cacheFile);

                        File.Move(programFile, cacheFile);
                        Output.Text += "\n\n已将解决方案移至缓存文件夹";
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                    Output.Text += "\n\n移动解决方案时出现错误 : " + ex.Message;
                }

                var standardOutput = new StringBuilder();
                var standardError = new StringBuilder();

                p.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        standardOutput.AppendLine(e.Data);
                };
                p.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        standardError.AppendLine(e.Data);
                };

                Output.Text += "\n\n已提交构建";

                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                await Task.Run(() => p.WaitForExit());
                p.Close();

                var message = standardOutput.ToString() + (string.IsNullOrWhiteSpace(standardOutput.ToString()) ? "" : "\n") + standardError.ToString();
                var success = File.Exists(programFile);

                // 如果构建失败就回移缓存中的解决方案
                if (!success)
                {
                    try
                    {
                        File.Move(cacheFile, programFile);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                    }
                }

                Output.Text += "\n\n构建" + (success ? "成功" : "失败");
                if (!string.IsNullOrWhiteSpace(message) || !success)
                    Output.Text += "\n\n" + message;

                if (success)
                {
                    ButtonRunProject.IsEnabled = true;
                    if (function != null)
                        function();
                }

                if (editor.State == CodeEditorState.Compile)
                    editor.State = CodeEditorState.Normal;
            }
        }

        /// <summary>
        /// 运行解决方案
        /// </summary>
        /// <param name="editor">代码编辑器</param>
        /// <param name="useShell">是否允许通过 SimpleShell 运行解决方案</param>
        /// <param name="function">运行</param>
        private async Task RunSolution(CodeEditor editor, bool allowShell = true, Action function = null)
        {
            if (!CanRun(editor)) return;
            if (editor.Tag is EditorInfo tag)
            {
                var command = tag.Module.GetHandledCommand(tag.Module.RunCommand, editor.FileName, tag.Module.StartupParameter);
                var programDirectory = editor.FileName.Substring(0, editor.FileName.LastIndexOf('\\'));

                Process p = null;
                if (tag.Module.RunWithSimpleShell && allowShell)
                {
                    p = new Process();
                    p.StartInfo.FileName = Path.Combine(AppInfo.Path, "Bin", "SimpleShell", "SimpleShell.exe");
                    p.StartInfo.Arguments = command;
                    p.StartInfo.WorkingDirectory = programDirectory;
                    p.Start();
                }
                else
                {
                    var split = command.ESplit();
                    var arg = "";

                    for (int i = 1; i < split.Length; ++i)
                        arg += split[i].MakeCommand() + " ";

                    p = Process.Start(split[0], arg);
                }

                await Task.Run(() => p.WaitForExit());
                p.Close();
                function();
            }
        }

        private async void ButtonBuildProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                {
                    if (!area.Editor.IsSaved)
                        area.Editor.ESave();

                    try
                    {
                        await BuildSolution(area.Editor);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                        Output.Text = "构建时遇到错误，终止构建：" + ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedRunWithBuild_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    e.CanExecute = CanBuild(area.Editor) && CanRun(area.Editor);
            }
            catch { e.CanExecute = false;}
        }

        private async void CommandBinding_RoutedRunWithBuild_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                {
                    if (!area.Editor.IsSaved)
                        area.Editor.ESave();

                    try
                    {
                        await BuildSolution(area.Editor, () => _ = RunSolution(area.Editor));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                        Output.Text = "构建时遇到错误，终止构建：" + ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void CommandBinding_RoutedBuild_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    e.CanExecute = CanBuild(area.Editor);
            }
            catch { e.CanExecute = false; }
        }

        private void CommandBinding_RoutedBuild_Execute(object sender, ExecutedRoutedEventArgs e) => ButtonBuildProject_Click(null, null);

        /// <summary>
        /// 运行解决方案
        /// </summary>
        private async void ButtonRunProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                {
                    // 如果不存在解决方案 (或解决方案不是最新的)，则询问是否构建
                    if (tag.Module.BuildEnabled)
                    {
                        if (!area.Editor.IsSaved || !File.Exists(tag.Module.GetHandledCommand(tag.Module.BuildProgramPath, area.Editor.FileName)))
                        {
                            var result = await this.ShowMessageAsync("构建解决方案", "解决方案不是最新的，是否构建？", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "取消", NegativeButtonText = "立即构建" });
                            if (result == MessageDialogResult.Negative)
                            {
                                try
                                {
                                    area.Editor.ESave();
                                    await BuildSolution(area.Editor, () => _ = RunSolution(area.Editor));
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                                    Output.Text = "构建时遇到错误，终止构建：" + ex.Message;
                                }
                                return;
                            }
                        }
                    }
                    else if (!area.Editor.IsSaved)
                        area.Editor.ESave();

                    _ = RunSolution(area.Editor);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                NotificationsManager.CreateMessage()
                                    .Animates(true)
                                    .AnimationInDuration(0.3)
                                    .AnimationOutDuration(0.3)
                                    .Accent(Brushes.Red)
                                    .Background("#363636")
                                    .HasBadge("错误")
                                    .HasMessage("运行解决方案时出现错误")
                                    .Dismiss().WithButton("忽略", null)
                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                    .Queue();
            }
        }

        private void CommandBinding_RoutedRun_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    e.CanExecute = CanRun(area.Editor);
            }
            catch { e.CanExecute = false; }
        }
        private void CommandBinding_RoutedRun_Execute(object sender, ExecutedRoutedEventArgs e) => ButtonRunProject_Click(null, null);

        private void CommandBinding_RoutedCompare_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                    e.CanExecute = CanJudge(area.Editor);
            }
            catch { e.CanExecute = false; }
        }
        private void CommandBinding_RoutedCompare_Execute(object sender, ExecutedRoutedEventArgs e) => ButtonJudge_Click(null, null);

        private void CommandBinding_RoutedRunWithoutCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                {
                    e.CanExecute = CanRun(area.Editor);
                }
            }
            catch { }
        }

        private async void CommandBinding_RoutedRunWithoutCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                {
                    // 如果不存在解决方案 (或解决方案不是最新的)，则询问是否构建
                    if (tag.Module.BuildEnabled)
                    {
                        if (!area.Editor.IsSaved || !File.Exists(tag.Module.GetHandledCommand(tag.Module.BuildProgramPath, area.Editor.FileName)))
                        {
                            var result = await this.ShowMessageAsync("构建解决方案", "解决方案不是最新的，是否构建？", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "取消", NegativeButtonText = "立即构建" });
                            if (result == MessageDialogResult.Negative)
                            {
                                try
                                {
                                    area.Editor.ESave();
                                    await BuildSolution(area.Editor, () => _ = RunSolution(area.Editor));
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                                    Output.Text = "构建时遇到错误，终止构建：" + ex.Message;
                                }
                                return;
                            }
                        }
                    }
                    else if (!area.Editor.IsSaved)
                        area.Editor.ESave();

                    _ = RunSolution(area.Editor, false);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                NotificationsManager.CreateMessage()
                                    .Animates(true)
                                    .AnimationInDuration(0.3)
                                    .AnimationOutDuration(0.3)
                                    .Accent(Brushes.Red)
                                    .Background("#363636")
                                    .HasBadge("错误")
                                    .HasMessage("运行解决方案时出现错误")
                                    .Dismiss().WithButton("忽略", null)
                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                    .Queue();
            }
        }

        #region 评测器

        /// <summary>
        /// 同步评测器
        /// </summary>
        private DispatcherTimer TimerSynchronizeComparePanel = null;

        /// <summary>
        /// 添加评测点
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        private void AddComparePoint(string input = "", string output = "")
        {
            var block1 = new TextBlock() { Text = "评测点 " + (CompareTool.Items.Count + 1).ToString() };
            var block2 = new TextBlock();
            var block3 = new TextBlock() { Text = "×", Cursor = Cursors.Hand };
            block3.PreviewMouseLeftButtonUp += ComparePanelClosed;

            var dockPanel = new DockPanel() { Margin = new Thickness(0, 0, 10, 0) };
            dockPanel.Children.Add(block1);
            dockPanel.Children.Add(block3);
            dockPanel.Children.Add(block2);
            DockPanel.SetDock(block1, Dock.Left);
            DockPanel.SetDock(block3, Dock.Right);

            TreeViewItem item = new TreeViewItem();
            item.Items.Add(new ComparePanel(input, output) { Margin = new Thickness(-18, 3, 14, 3), Height = 200 });
            item.Header = dockPanel;
            block3.Tag = item;
            CompareTool.Items.Add(item);
        }

        /// <summary>
        /// 添加评测点
        /// </summary>
        private void ButtonAddComparePoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TabArea.Items.Count == 0)
                    return;

                // 布局
                AddComparePoint();

                SynchronizeComparePanel(); // 同步评测器
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        /// <summary>
        /// 清空评测点
        /// </summary>
        private void ButtonClearComparePoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CompareTool.Items.Clear();
                SynchronizeComparePanel();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        /// <summary>
        /// 同步 CodePage.CompareList 和 MainWindow.ComparePanel.Items
        /// </summary>
        private void SynchronizeComparePanel()
        {
            try
            {
                if (TabArea.SelectedItem is TabItem tabitem && tabitem.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                {
                    tag.JudgePoints.Clear();

                    foreach (TreeViewItem iter in CompareTool.Items)
                    {
                        var panel = iter.Items[0] as ComparePanel;
                        tag.JudgePoints.Add(new Tuple<string, string>(panel.InputBox.Text, panel.OutputBox.Text));
                    }

                    tag.JudgeTimeout = int.Parse(this.TextBoxCompareTimeout.Text);
                }
            }
            catch { }
        }

        /// <summary>
        /// 评测点被删除
        /// </summary>
        private void ComparePanelClosed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is TextBlock button && button.Tag is TreeViewItem item)
                    if (CompareTool.Items.Contains(item))
                        CompareTool.Items.Remove(item);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 执行评测
        /// </summary>
        private async void ButtonJudge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CompareTool.Items.Count == 0) return;

                if (TabArea.SelectedItem is TabItem tabitem && tabitem.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                {
                    if (!tag.Module.JudgeEnabled) return;

                    Action judge = () =>
                    {
                        SynchronizeComparePanel();

                        CompareResult dialog = new CompareResult(tag.Module.GetHandledCommand(tag.Module.RunCommand, area.Editor.FileName, tag.Module.StartupParameter), int.Parse(TextBoxCompareTimeout.Text), tag.JudgePoints) { Owner = this };
                        dialog.Show();
                    };

                    // 如果不存在解决方案 (或解决方案不是最新的)，则询问是否构建
                    if (tag.Module.BuildEnabled)
                    {
                        if (!area.Editor.IsSaved || !File.Exists(tag.Module.GetHandledCommand(tag.Module.BuildProgramPath, area.Editor.FileName)))
                        {
                            var result = await this.ShowMessageAsync("构建解决方案", "解决方案不是最新的，是否构建？", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "取消", NegativeButtonText = "立即构建" });
                            if (result == MessageDialogResult.Negative)
                            {
                                try
                                {
                                    area.Editor.ESave();
                                    await BuildSolution(area.Editor, judge);
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
                                    Output.Text = "构建时遇到错误，终止构建：" + ex.Message;
                                }
                                return;
                            }
                        }
                    }
                    else if (!area.Editor.IsSaved)
                        area.Editor.ESave();

                    judge();
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        private void ComboBoxCompileBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox box && box.SelectedItem != null && TabArea.SelectedItem is TabItem item && item.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                    tag.Module.Bit = box.SelectedItem.ToString();
            }
            catch { }
        }

        private void ComboBoxCompileModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox box && box.SelectedItem != null && TabArea.SelectedItem is TabItem item && item.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                    tag.Module.Mode = box.SelectedItem.ToString();
            }
            catch { }
        }

        private void ComboBoxCompileOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox box && box.SelectedItem != null && TabArea.SelectedItem is TabItem item && item.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                    tag.Module.Option = box.SelectedItem.ToString();
            }
            catch { }
        }


        #region 最近打开的项

        private void OpenedListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenedListContextMenuOpenClick(null, null);
        }

        private void OpenedListKeyDown(object sender, KeyEventArgs e)
        {
            if (RecentFiles.SelectedItem == null) return;

            if (e.Key == Key.Delete)
                OpenedListContextMenuDelete_Click(null, null);
            else if (e.Key == Key.Enter)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) || e.KeyboardDevice.IsKeyDown(Key.RightAlt))
                    OpenedListContextMenuOpenInNewWindow_Click(null, null);
                else
                    OpenedListContextMenuOpenClick(null, null);
            }
        }

        /// <summary>
        /// 当 “最近打开的项” 列表的右键菜单将要打开时，检查其是否应该打开
        /// </summary>
        private void OpenedList_ContextMenuOpening(object sender, ContextMenuEventArgs e) => e.Handled = RecentFiles.SelectedItem == null;

        private void OpenedListContextMenuOpenClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var file = (RecentFiles.SelectedItem as ListBoxItem).Tag.ToString();
                if (File.Exists(file))
                    OpenFile(file);
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void OpenedListContextMenuOpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var file = (RecentFiles.SelectedItem as ListBoxItem).Tag.ToString();
                if (File.Exists(file))
                    Process.Start(Process.GetCurrentProcess().MainModule.FileName, String.Format("{0}{1}{0}", file.Contains(' ') ? "\"" : "", file));
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        /// <summary>
        /// 将某个项从 "最近打开的项" 列表中删除 (同时显示移除动画)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenedListContextMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 此动画所需的最小动画级别
                const int animationMinLevel = 2;
                var item = RecentFiles.Items[RecentFiles.SelectedIndex];

                // 启动动画
                if ((int)AppInfo.Config["ide"]["settings"]["animation"]["level"] >= 2) // 检查动画级别
                {
                    // 满足动画级别，在动画播放完成后删除
                    AnimationHelper.BeginAnimation(animationMinLevel, new Tuple<Window, FrameworkElement, Action>(this, item as ListBoxItem, () => { }), AnimationHelper.ControlAnimationOpacityLeave);
                    AnimationHelper.BeginAnimation(animationMinLevel, new Tuple<Window, FrameworkElement, Action>(this, item as ListBoxItem, () => RecentFiles.Items.Remove(item)), AnimationHelper.ControlAnimationLeave);
                }
                else // 如果不满足动画级别，直接删除
                {
                    RecentFiles.Items.Remove(item);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        #region 文件树

        private void FileTreeOpenRootDirectory()
        {
            try
            {
                FileTree.Items.Clear();
                DriveInfo[] driveInfo = DriveInfo.GetDrives();

                //遍历数组
                foreach (DriveInfo d in driveInfo)
                {
                    //指示驱动器是否准备好
                    if (d.IsReady)
                    {
                        ListBoxItem item = new ListBoxItem()
                        {
                            Content = (string.IsNullOrWhiteSpace(d.VolumeLabel) ? "默认磁盘" : d.VolumeLabel) + " (" + d.Name + ")",
                            Tag = d.Name
                        };
                        FileTree.Items.Add(item);
                    }
                }

                FileTreeOpenPath.Text = "我的电脑";
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void FileTreeOpenDirectory(string srcPath)
        {
            try
            {
                if (srcPath == "我的电脑")
                {
                    FileTreeOpenRootDirectory();
                    return;
                }
                if (!Directory.Exists(srcPath))
                    return;

                FileTree.Items.Clear();
                FileTreeOpenPath.Text = srcPath;

                string[] dirs = Directory.GetDirectories(srcPath);
                Array.Sort(dirs);
                foreach (string file in dirs)
                {
                    FileInfo info = new FileInfo(file);
                    ListBoxItem item = new ListBoxItem()
                    {
                        Content = info.Name,
                        Tag = info.FullName
                    };
                    FileTree.Items.Add(item);
                }

                string[] files = Directory.GetFiles(srcPath);
                Array.Sort(files);
                foreach (string file in files)
                {
                    FileInfo info = new FileInfo(file);
                    ListBoxItem item = new ListBoxItem()
                    {
                        Content = info.Name,
                        Tag = info.FullName
                    };
                    FileTree.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void FileTreeMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is ListBox list)
                {
                    var path = (list.SelectedItem as ListBoxItem).Tag.ToString();
                    if (Directory.Exists(path))
                    {
                        FileTreeOpenDirectory(path);
                        (FileTree.Tag as FileTreeProperty).Path = path;
                    }
                    else if (File.Exists(path))
                        OpenFile(path);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void FileTreeKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                FileTreeMouseDoubleClick(FileTree, null);
            else if (e.Key == Key.F5)
                FileTreeButtonRefreshClick(FileTree, null);
        }

        private void FileTreeButtonUndoClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (FileTree.Tag is FileTreeProperty tag)
                {
                    if (tag.Position > 0)
                        --tag.Position;
                    FileTreeOpenDirectory(tag.Path);
                    HandleButton_Tick(null, null);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void FileTreeButtonRedoClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (FileTree.Tag is FileTreeProperty tag)
                {
                    if (tag.Position != tag.Stack.Count - 1)
                        ++tag.Position;
                    FileTreeOpenDirectory(tag.Path);
                    HandleButton_Tick(null, null);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void FileTreeButtonParentClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (FileTree.Tag is FileTreeProperty tag)
                {
                    if (tag.Path == "我的电脑")
                        return;

                    if (tag.Path.Length == 3)
                    {
                        tag.Path = "我的电脑";
                        FileTreeOpenRootDirectory();
                    }
                    else
                    {
                        DirectoryInfo info = new DirectoryInfo(tag.Path);
                        FileTreeOpenDirectory(tag.Path = info.Parent.FullName);
                        HandleButton_Tick(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void FileTreeButtonRefreshClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (FileTree.Tag is FileTreeProperty tag)
                {
                    FileTreeOpenDirectory(tag.Path);
                    HandleButton_Tick(null, null);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        #region 布局

        #region 子窗口关闭
        private void FileTreeTitleClosed(object sender, MouseButtonEventArgs e)
        {
            LeftLayout.RowDefinitions[0].Height = new GridLength(0);
            if (LeftLayout.RowDefinitions[1].Height.Value == 0)
            {
                LayoutRoot.ColumnDefinitions[0].Width = new GridLength(0);
                LayoutRoot.ColumnDefinitions[1].Width = new GridLength(0);
            }
            else
            {
                LeftLayout.RowDefinitions[1].Height = new GridLength(0);
            }
        }

        private void RecentFilesTitleClosed(object sender, MouseButtonEventArgs e)
        {
            LeftLayout.RowDefinitions[2].Height = new GridLength(0);
            if (LeftLayout.RowDefinitions[1].Height.Value == 0)
            {
                LayoutRoot.ColumnDefinitions[0].Width = new GridLength(0);
                LayoutRoot.ColumnDefinitions[1].Width = new GridLength(0);
            }
            else
            {
                LeftLayout.RowDefinitions[1].Height = new GridLength(0);
            }
        }

        private void ViewerTitleClosed(object sender, MouseButtonEventArgs e)
        {
            MiddleLayout.RowDefinitions[3].Height = new GridLength(0);
            MiddleLayout.RowDefinitions[4].Height = new GridLength(0);
        }

        private void CompareToolTitleClosed(object sender, MouseButtonEventArgs e)
        {
            LayoutRoot.ColumnDefinitions[3].Width = new GridLength(0);
            LayoutRoot.ColumnDefinitions[4].Width = new GridLength(0);
        }
        #endregion

        #region 菜单栏按钮响应
        private void MenuViewFileTreeClick(object sender, RoutedEventArgs e)
        {
            if (LeftLayout.RowDefinitions[0].Height.Value > 5 && LayoutRoot.ColumnDefinitions[0].Width.Value > 5) return;

            LeftLayout.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            if (LeftLayout.RowDefinitions[2].Height.Value != 0)
                LeftLayout.RowDefinitions[1].Height = new GridLength(2);
            if (LayoutRoot.ColumnDefinitions[0].Width.Value <= 5 || LayoutRoot.ColumnDefinitions[1].Width.Value == 0)
            {
                LayoutRoot.ColumnDefinitions[0].Width = new GridLength(220);
                LayoutRoot.ColumnDefinitions[1].Width = new GridLength(2);
            }
        }

        private void MenuViewRecentFilesClick(object sender, RoutedEventArgs e)
        {
            if (LeftLayout.RowDefinitions[2].Height.Value > 5 && LayoutRoot.ColumnDefinitions[0].Width.Value > 5) return;

            LeftLayout.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
            if (LeftLayout.RowDefinitions[0].Height.Value != 0)
                LeftLayout.RowDefinitions[1].Height = new GridLength(2);
            if (LayoutRoot.ColumnDefinitions[0].Width.Value <= 5 || LayoutRoot.ColumnDefinitions[1].Width.Value == 0)
            {
                LayoutRoot.ColumnDefinitions[0].Width = new GridLength(220);
                LayoutRoot.ColumnDefinitions[1].Width = new GridLength(2);
            }
        }

        private void MenuViewViewerClick(object sender, RoutedEventArgs e)
        {
            if (MiddleLayout.RowDefinitions[4].Height.Value > 5) return;

            MiddleLayout.RowDefinitions[3].Height = new GridLength(2);
            MiddleLayout.RowDefinitions[4].Height = new GridLength(200);
        }

        private void MenuViewCompareToolClick(object sender, RoutedEventArgs e)
        {
            if (LayoutRoot.ColumnDefinitions[4].Width.Value > 5) return;

            LayoutRoot.ColumnDefinitions[3].Width = new GridLength(2);
            LayoutRoot.ColumnDefinitions[4].Width = new GridLength(200);
        }

        private void MenuSettingReSettingLayoutClick(object sender, RoutedEventArgs e)
        {
            MenuViewFileTreeClick(null, null);
            MenuViewRecentFilesClick(null, null);
            MenuViewViewerClick(null, null);
            MenuViewCompareToolClick(null, null);
        }
        #endregion

        #endregion

        #region 设置模块命令

        private void CommandBinding_RoutedCommandSetting_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                {
                    e.CanExecute = tag.Module != null;
                }
            }
            catch { e.CanExecute = false; }
        }

        private void CommandBinding_RoutedCommandSetting_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem tabitem && tabitem.Content is CodeArea area && area.Editor.Tag is EditorInfo tag)
                {
                    new CommandSetting(tag.Module) { Owner = this }.Show();
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        private void MenuSettingOptionClick(object sender, RoutedEventArgs e)
        {
            new Settings() { Owner = this }.Show();
        }

        private void MenuPluginInstallModuleClick(object sender, RoutedEventArgs e)
        {
            try
            {
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = Path.Combine(AppInfo.Path, "Bin", "Installer", "ModuleInstaller.exe"),
                        CreateNoWindow = false,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "MainWindows.xaml.cs");
                }
            }
            catch { }
        }

        private void MenuPluginInstallClick(object sender, RoutedEventArgs e)
        {
            new PluginInstaller() { Owner = this }.Show();
        }

        #region GCloud

        private void OpenGCloud(object sender, RoutedEventArgs e)
        {
            new GCloud() { Owner = this }.Show();
        }

        #region 上传

        private void TabAreaContentMenuCloseItemClick(object sender, RoutedEventArgs e)
        {
            if (TryFindResource("CodePageContextMenu") is ContextMenu menu)
            {
                menu.IsOpen = false;
                CommandBinding_RoutedCloseFile_Executed(null, null);
            }
        }

        private void TabAreaContentMenuUploadToGCloudClick(object sender, RoutedEventArgs e)
        {
            FlyoutGCloudUploadPanel.IsOpen = true;
        }

        private void FlyoutGCloudUploadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TabArea.SelectedItem is TabItem item && item.Content is CodeArea area)
                {
                    var gcloud = Path.Combine(AppInfo.Path, "Bin", "GCloud", "gcloud.exe");
                    if (!File.Exists(gcloud))
                        throw new Exception("GCloud不存在，请重新安装");

                    var url = FlyoutGCloudUploadUrl.Text;
                    var path = Path.Combine(AppInfo.Path, "Cache", "gcloud_upload_file_" + DateTime.Now.ToString("yyyyMMddHHmmss") + area.Editor.FileName);
                    area.Editor.Save(path);
                    FlyoutGCloudUploadRing.IsActive = true;

                    new Task(() =>
                    {
                        var result = Helper.RunGCloud("upload \"" + path + "\" \"" + url + "\"");
                        this.Dispatcher.Invoke(() =>
                        {
                            if (File.Exists(path))
                                FlyoutGCloudUploadPanel.IsOpen = false;
                            else
                                NotificationsManager.CreateMessage()
                                                    .Animates(true)
                                                    .AnimationInDuration(0.3)
                                                    .AnimationOutDuration(0.3)
                                                    .Accent(Brushes.Red)
                                                    .Background("#363636")
                                                    .HasBadge("错误")
                                                    .HasMessage("无法上传文件")
                                                    .Dismiss().WithButton("忽略", null)
                                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                    .Queue();

                            FlyoutGCloudUploadRing.IsActive = false;

                        });
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");

                NotificationsManager.CreateMessage()
                                    .Animates(true)
                                    .AnimationInDuration(0.3)
                                    .AnimationOutDuration(0.3)
                                    .Accent(Brushes.Red)
                                    .Background("#363636")
                                    .HasBadge("错误")
                                    .HasMessage("无法上传文件")
                                    .Dismiss().WithButton("忽略", null)
                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                    .Queue();
            }
        }

        #endregion

        #region 下载

        private void DownloadFileFromGCloud(object sender, RoutedEventArgs e)
        {
            FlyoutGCloudDownloadPanel.IsOpen = true;
        }

        private void FlyoutGCloudSaveFileClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "文件|*.*",
                Title = "选择文件保存位置"
            };
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                FlyoutGCloudDownloadPath.Text = fileDialog.FileName;
        }

        private void FlyoutGCloudDownloadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var gcloud = Path.Combine(AppInfo.Path, "Bin", "GCloud", "gcloud.exe");
                if (!File.Exists(gcloud))
                    throw new Exception("GCloud不存在，请重新安装");

                var url = FlyoutGCloudDownloadUrl.Text;
                var path = FlyoutGCloudDownloadChooseFilePath.IsOn ? FlyoutGCloudDownloadPath.Text : Path.Combine(AppInfo.Path, "Cache", "gcloud_file_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + url.Substring(url.LastIndexOf('.')));
                FlyoutGCloudDownloadRing.IsActive = true;

                new Task(() =>
                {
                    var result = Helper.RunGCloud("download \"" + url + "\" \"" + path + "\"");
                    this.Dispatcher.Invoke(() =>
                    {
                        if (File.Exists(path))
                        {
                            FlyoutGCloudDownloadPanel.IsOpen = false;
                            OpenFile(path, this.FlyoutGCloudDownloadChooseFilePath.IsOn, false, false);
                        }
                        else
                            NotificationsManager.CreateMessage()
                                                .Animates(true)
                                                .AnimationInDuration(0.3)
                                                .AnimationOutDuration(0.3)
                                                .Accent(Brushes.Red)
                                                .Background("#363636")
                                                .HasBadge("错误")
                                                .HasMessage("无法下载文件")
                                                .Dismiss().WithButton("忽略", null)
                                                .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                .Queue();

                        FlyoutGCloudDownloadRing.IsActive = false;
                    });
                }).Start();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");

                NotificationsManager.CreateMessage()
                                    .Animates(true)
                                    .AnimationInDuration(0.3)
                                    .AnimationOutDuration(0.3)
                                    .Accent(Brushes.Red)
                                    .Background("#363636")
                                    .HasBadge("错误")
                                    .HasMessage("无法下载文件")
                                    .Dismiss().WithButton("忽略", null)
                                    .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                    .Queue();
            }
        }

        #endregion

        #endregion

        private async void MenuAboutSendingFeedBackClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = "";
                await Task.Run(() => result = Helper.RunGCloud("info"));
                var command = result.ESplit();
                if (command.Length == 5)
                    Process.Start(Path.Combine(AppInfo.Path, "Bin", "Feedbacker", "Feedbacker.exe"), $"{command[1].MakeCommand()} {command[2].MakeCommand()}");
                else
                    new LoginWindow() { Owner = this }.Show();
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        private void MenuAboutUpdateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.Combine(AppInfo.Path, "Bin", "Update", "Update.exe"),
                    CreateNoWindow = false,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindows.xaml.cs");
            }
        }

        private void MenuAboutIDEClick(object sender, RoutedEventArgs e)
        {
            new About() { Owner = this }.Show();
        }

        #region 拖入文件

        /// <summary>
        /// 抛弃文件夹目标
        /// </summary>
        private void FilesDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Link;
            else e.Effects = DragDropEffects.None;
        }

        private void FilesDrop(object sender, DragEventArgs e)
        {
            try
            {
                var drops = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string iter in drops)
                {
                    if (File.Exists(iter))
                        OpenFile(iter);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");
            }
        }

        #endregion

        #region 起始页

        private void StartPageCreateFile_Click(object sender, MouseButtonEventArgs e) => CommandBinding_RoutedCreateFile_Executed(null, null);
        private void StartPageOpenFile_Click(object sender, MouseButtonEventArgs e) => CommandBinding_RoutedOpenFile_Executed(null, null);
        private void StartPageOpenFileFromGCloud_Click(object sender, MouseButtonEventArgs e) => DownloadFileFromGCloud(null, null);

        private void StartPageSettings_Click(object sender, MouseButtonEventArgs e) => MenuSettingOptionClick(null, null);
        private void StartPageInstallModules_Click(object sender, MouseButtonEventArgs e) => MenuPluginInstallModuleClick(null, null);
        private void StartPageInstallPlugins_Click(object sender, MouseButtonEventArgs e) { }

        private void StartPageFeedback_Click(object sender, MouseButtonEventArgs e) => MenuAboutSendingFeedBackClick(null, null);
        private void StartPageAbout_Click(object sender, MouseButtonEventArgs e) => MenuAboutIDEClick(null, null);

        private void StartPageWebsite_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "http://return25.github.io/",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        #endregion
    }
}