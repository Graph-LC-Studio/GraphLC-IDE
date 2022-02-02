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
using static GraphLC_IDE.Functions.Event;

namespace GraphLC_IDE.Controls
{
    /// <summary>
    /// ComparePanel.xaml 的交互逻辑
    /// </summary>
    public partial class ComparePanel : UserControl
    {
        public ComparePanel(string input = "", string output = "")
        {
            InitializeComponent();
            InputBox.Text = input;
            OutputBox.Text = output;
        }

        public event ButtonCloseEvent Closed; //点击关闭按钮引发的事件
        private void CloseButtonClick(object sender, MouseButtonEventArgs e)
        {
            if(Closed != null)
                Closed(this, e);
        }
    }
}