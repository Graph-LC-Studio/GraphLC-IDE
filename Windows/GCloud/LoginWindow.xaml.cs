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

using ControlzEx.Theming;
using Enterwell.Clients.Wpf.Notifications;

using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Functions;
using GraphLC_IDE.Extensions;

namespace GraphLC_IDE.Windows.GCloud
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : MahApps.Metro.Controls.MetroWindow
    {
        public NotificationMessageManager NotificationsManager { get; } = new NotificationMessageManager();

        public LoginWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            this.NotificationsGrid.Visibility = Visibility.Visible;

            ThemeManager.Current.ChangeTheme(this, AppInfo.Theme["name"].ToString());
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (this.Owner.WindowState == WindowState.Minimized)
                this.Owner.WindowState = WindowState.Normal;
            this.Owner.Focus();
        }

        private void Login(object sender, RoutedEventArgs e)
        {
            Ring.Visibility = Visibility.Visible;
            var name = UserName.Text;
            var password = PassWord.Password;
            new Thread(() =>
            {
                try
                {
                    var result = Helper.RunGCloud(string.Format("login \"{0}\" \"{1}\"", name, password));

                    this.Dispatcher.Invoke(() =>
                    {
                        if (result.ESplit().Length == 5)
                        {
                            if (Application.Current.MainWindow is MainWindow main)
                                main.NotificationsManager.CreateMessage()
                                                         .Animates(true)
                                                         .AnimationInDuration(0.3)
                                                         .AnimationOutDuration(0.3)
                                                         .Accent(Brushes.Green)
                                                         .Background("#363636")
                                                         .HasBadge("信息")
                                                         .HasMessage($"欢迎，{UserName.Text}")
                                                         .Dismiss().WithButton("关闭", null)
                                                         .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                         .Queue();

                            this.Close();
                        }
                        else
                            this.NotificationsManager.CreateMessage()
                                                     .Animates(true)
                                                     .AnimationInDuration(0.3)
                                                     .AnimationOutDuration(0.3)
                                                     .Accent(Brushes.Red)
                                                     .Background("#363636")
                                                     .HasBadge("错误")
                                                     .HasMessage("无法登录账户")
                                                     .Dismiss().WithButton("关闭", null)
                                                     .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                     .Queue();

                        Ring.Visibility = Visibility.Collapsed;
                    });
                }
                catch(Exception ex)
                {
                    Log.WriteErr(ex.Message, "LoginWindow.xaml.cs");
                }
            }).Start();
        }

        private void Register(object sender, RoutedEventArgs e) => new RegisterWindow() { Owner = this.Owner }.Show();

        private void Modify(object sender, MouseButtonEventArgs e) => new ModifyWindow("") { Owner = this }.Show();
    }
}
