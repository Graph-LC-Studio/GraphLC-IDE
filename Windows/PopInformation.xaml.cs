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
using System.Windows.Shapes;
using System.Windows.Threading;

using ControlzEx.Theming;
using MahApps.Metro.Controls;

using GraphLC_IDE.AppConfig;

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// PopInformation.xaml 的交互逻辑
    /// </summary>
    public partial class PopInformation : Window
    {
        private Window parent = null;
        private DispatcherTimer timer = null, location = null;
        private TimeSpan timeSpan;
        private bool isLockOpacity = true;

        public new string Content
        {
            get
            {
                return content.Text;
            }
            set
            {
                content.Text = value;
            }
        }

        public new string Title
        {
            get
            {
                return title.Title;
            }
            set
            {
                title.Title = value;
            }
        }

        public PopInformation(Window p, TimeSpan closeTime)
        {
            InitializeComponent();

            try
            {
                if (AppInfo.Theme.Token != null)
                    ThemeManager.Current.ChangeTheme(this, AppInfo.Theme["name"].ToString());
                if (AppInfo.Theme.Token != null)
                {
                    var fore = AppInfo.Theme["window"]["fore"];
                    this.Foreground = new SolidColorBrush(Color.FromArgb((byte)fore[0], (byte)fore[1], (byte)fore[2], (byte)fore[3]));
                }
                else
                    this.Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));

                parent = p;
                timeSpan = closeTime;

                timer = new DispatcherTimer
                {
                    Interval = new TimeSpan(300000),
                    IsEnabled = true
                };
                timer.Tick += Timer_Tick;
                timer.Tag = closeTime.TotalMilliseconds;
                timer.Start();

                location = new DispatcherTimer
                {
                    Interval = new TimeSpan(110000),
                    IsEnabled = true
                };
                location.Tick += Location_Tick;
                location.Start();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "PopInformation.xaml.cs");
            }
        }

        private void Location_Tick(object sender, EventArgs e)
        {
            try
            {
                Point ptRightDown = parent.PointToScreen(new Point(parent.Width, parent.Height));
                if (parent.WindowState == WindowState.Maximized)
                {
                    this.Left = SystemParameters.WorkArea.Width - this.Width;
                    this.Top = SystemParameters.WorkArea.Height - this.Height;
                }
                else
                {
                    this.Left = ptRightDown.X - this.Width;
                    this.Top = ptRightDown.Y - this.Height;
                }

                this.Show();
            }
            catch
            {
                title_Closed(null, null);
            }
        }

        private void title_Closed(object sender, MouseButtonEventArgs e)
        {
            try
            {
                timer.IsEnabled = false;
                this.Close();
            }
            catch(Exception) { }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                DispatcherTimer dispatcher = (DispatcherTimer)sender;
                if (this.Opacity < 0.1) title_Closed(null, null);

                if (((double)dispatcher.Tag) <= 0)
                {
                    if (isLockOpacity)
                    {
                        this.ApplyAnimationClock(Window.OpacityProperty, null);
                        isLockOpacity = false;
                    }
                    this.Opacity = this.Opacity - 0.07 > 0 ? this.Opacity - 0.07 : 0.05;
                }

                dispatcher.Tag = ((double)dispatcher.Tag) - dispatcher.Interval.TotalMilliseconds;
                progress.Value = 100 - (double)dispatcher.Tag / timeSpan.TotalMilliseconds * 100;
            }
            catch
            {
                title_Closed(null, null);
            }
        }
    }
}
