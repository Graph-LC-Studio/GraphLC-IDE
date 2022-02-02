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
using ControlzEx.Theming;
using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Windows.GCloud;

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// Introduction.xaml 的交互逻辑
    /// </summary>
    public partial class Introduction : MahApps.Metro.Controls.MetroWindow
    {
        public Introduction()
        {
            InitializeComponent();
            var theme = AppInfo.Theme["name"].ToString();
            ThemeManager.Current.ChangeTheme(this, "Light." + theme.Substring(theme.IndexOf('.') + 1));
        }

        private void Reg(object sender, RoutedEventArgs e)
        {
            new RegisterWindow() { Owner = this.Owner }.Show();
        }

        private void Login(object sender, RoutedEventArgs e)
        {
            new LoginWindow() { Owner = this.Owner }.Show();
        }
    }
}
