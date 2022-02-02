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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GraphLC_IDE.Controls
{
    /// <summary>
    /// ModuleItem.xaml 的交互逻辑
    /// </summary>
    public partial class ModuleItem : UserControl
    {
        public ModuleItem()
        {
            InitializeComponent();
        }

        public bool isCheck
        {
            get => ControlIsCheck.IsChecked.HasValue ? ControlIsCheck.IsChecked.Value : false;
            set => ControlIsCheck.IsChecked = value;
        }

        public string Title
        {
            get => ControlTitle.Text;
            set => ControlTitle.Text = value;
        }

        public string Description
        {
            get => ControlDescription.Text;
            set => ControlDescription.Text = value;
        }

        public ImageSource Icon
        {
            get => ControlIcon.Source;
            set => ControlIcon.Source = value;
        }

        public event RoutedEventHandler CheckChanged = null;
        private void ControlIsCheckChecked(object sender, RoutedEventArgs e) => CheckChanged(sender, e);
        private void ControlIsCheckUnchecked(object sender, RoutedEventArgs e) => CheckChanged(sender, e);

        private void ModuleItemMouseEnter(object sender, MouseEventArgs e)
        {
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204));
        }

        private void ModuleItemMouseLeave(object sender, MouseEventArgs e)
        {
            this.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }
    }
}
