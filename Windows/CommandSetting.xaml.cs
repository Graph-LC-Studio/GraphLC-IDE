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

using GraphLC_IDE.AppConfig;
using ControlzEx.Theming;

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// CommandSetting.xaml 的交互逻辑
    /// </summary>
    public partial class CommandSetting : MahApps.Metro.Controls.MetroWindow
    {
        ModuleInfo Module = null;

        public CommandSetting(ModuleInfo module)
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, AppInfo.Theme["name"].ToString());

            Module = module;
            AttachCommand.Text = module.AttachCommand;
            StartupParameter.Text = module.StartupParameter;
        }

        private void CommandSettingClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Module.AttachCommand = AttachCommand.Text;
            Module.StartupParameter = StartupParameter.Text;
        }
    }
}
