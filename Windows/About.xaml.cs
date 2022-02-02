using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GraphLC_IDE.AppConfig;
using ControlzEx.Theming;

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// AboutIDE.xaml 的交互逻辑
    /// </summary>
    public partial class About : MahApps.Metro.Controls.MetroWindow
    {
        public About()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, AppInfo.Theme["name"].ToString());

            var versionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.Title = "关于 " + (TextBlockName.Text = versionInfo.ProductName);
            TextBlockVersion.Text = "版本 " + versionInfo.ProductVersion;
            TextBlockCopyright.Text = versionInfo.LegalCopyright;
        }
    }
}
