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

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// PluginInstaller.xaml 的交互逻辑
    /// </summary>
    public partial class PluginInstaller : MahApps.Metro.Controls.MetroWindow
    {
        public PluginInstaller()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, "Dark.Blue");
        }
    }
}
