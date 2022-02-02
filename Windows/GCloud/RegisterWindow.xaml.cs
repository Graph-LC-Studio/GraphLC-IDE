using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ControlzEx.Theming;
using Enterwell.Clients.Wpf.Notifications;

using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Functions;

namespace GraphLC_IDE.Windows.GCloud
{
    /// <summary>
    /// RegisterWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RegisterWindow : MahApps.Metro.Controls.MetroWindow
    {
        public RegisterWindow()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, AppInfo.Theme["name"].ToString());
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if(this.Owner.WindowState == WindowState.Minimized)
                this.Owner.WindowState = WindowState.Normal;
            this.Owner.Focus();
        }

        private void Verify(object sender, RoutedEventArgs e)
        {
            var email = Email.Text;
            new Thread(() =>
            {
                try
                {
                    _ = Helper.RunGCloud(string.Format("mail \"{0}\"", email)).Replace("\n","\n");
                }
                catch(Exception ex)
                {
                    Log.WriteErr(ex.Message, "RegisterWindow.xaml.cs");
                }
            }).Start();
        }

        private void Reg(object sender, RoutedEventArgs e)
        {
            if (PassWord.Password != VerifyPassWord.Password)
            {
                _ = Helper.MetroBox(this, "两次输入的密码不一致", "警告", "确定");
                return;
            }

            var name = UserName.Text;
            var password = PassWord.Password;
            var code = Code.Text;
            var email = Email.Text;
            new Thread(() =>
            {
                try
                {
                    var result = Helper.RunGCloud(string.Format("verify \"{0}\" \"{1}\"", email, code)).Replace("\n", "");
                    if (result != "OK")
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            _ = Helper.MetroBox(this, result, "无法注册账户");
                        });
                    }
                    else
                    {
                        var result2 = Helper.RunGCloud(string.Format("reg \"{0}\" \"{1}\" \"{2}\"", name, password, email)).Replace("\n","");
                        if (result2 == "OK")
                            this.Dispatcher.Invoke(() =>
                            {
                                if (Application.Current.MainWindow is MainWindow main)
                                    main.NotificationsManager.CreateMessage()
                                                             .Animates(true)
                                                             .AnimationInDuration(0.3)
                                                             .AnimationOutDuration(0.3)
                                                             .Accent(Brushes.Red)
                                                             .Background("#363636")
                                                             .HasBadge("信息")
                                                             .HasMessage($"注册成功")
                                                             .Dismiss().WithButton("忽略", null)
                                                             .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                             .Queue();
                                this.Close();
                            });
                        else
                            this.Dispatcher.Invoke(() =>
                            {
                                _ = Helper.MetroBox(this, result, "注册失败");
                            });
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "RegisterWindow.xaml.cs");
                }
            }).Start();
        }
    }
}