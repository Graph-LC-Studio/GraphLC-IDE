using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// WindowTitle.xamsl 的交互逻辑
    /// </summary>
    public partial class WindowTitle : UserControl
    {
        public WindowTitle()
        {
            InitializeComponent();
        }

        //事件
        public event ButtonCloseEvent Closed; //点击关闭按钮引发的事件

        private void CloseButtonClick(object sender, MouseButtonEventArgs e)
        {
            if (Closed != null)
                Closed(this, e); //回调
            //this.Visibility = Visibility.Hidden; //隐藏自己
        }

        //属性
        public string Title
        {
            get
            {
                return title.Text;
            }
            set
            {
                title.Text = value;
            }
        }

        public HorizontalAlignment TitleHorizontalAlignment
        {
            get
            {
                return title.HorizontalAlignment;
            }
            set
            {
                title.HorizontalAlignment = value;
            }
        }

        public VerticalAlignment TitleVerticalAlignment
        {
            get
            {
                return title.VerticalAlignment;
            }
            set
            {
                title.VerticalAlignment = value;
            }
        }

        public Thickness TitleMargin
        {
            get
            {
                return new Thickness(layoutTitle.ColumnDefinitions[0].Width.Value, layoutRoot.RowDefinitions[0].Height.Value, layoutTitle.ColumnDefinitions[3].Width.Value, layoutRoot.RowDefinitions[2].Height.Value);
            }
            set
            {
                layoutTitle.ColumnDefinitions[0].Width = new GridLength(value.Left);
                layoutTitle.ColumnDefinitions[3].Width = new GridLength(value.Right);
                layoutRoot.RowDefinitions[0].Height = new GridLength(value.Top);
                layoutRoot.RowDefinitions[2].Height = new GridLength(value.Bottom);
            }
        }

        public FontStyle TitleFontStyle
        {
            get
            {
                return title.FontStyle;
            }
            set
            {
                title.FontStyle = value;
            }
        }

        public FontWeight TitleFontWeight
        {
            get
            {
                return title.FontWeight;
            }
            set
            {
                title.FontWeight = value;
            }
        }

        public double TitleSize
        {
            get
            {
                return title.FontSize;
            }
            set
            {
                title.FontSize = value;
                closeButton.FontSize = value;
            }
        }

        public Visibility CloseButtonVisibility
        {
            get
            {
                return closeButton.Visibility;
            }
            set
            {
                closeButton.Visibility = value;
            }
        }
    }
}
