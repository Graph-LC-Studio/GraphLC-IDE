using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using GraphLC_IDE.AppConfig;

namespace GraphLC_IDE.Functions
{
    class AnimationHelper
    {
        public static void BeginAnimation(int minLevel, ThreadStart fn)
        {
            try
            {
                if ((int)AppInfo.Config["ide"]["settings"]["animation"]["level"] >= minLevel)
                {
                    new Thread(fn).Start();
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Helper.cs");
            }
        }

        public static void BeginAnimation(int minLevel, object sender, ParameterizedThreadStart fn)
        {
            try
            {
                if ((int)AppInfo.Config["ide"]["settings"]["animation"]["level"] >= minLevel)
                {
                    new Thread(fn).Start(sender);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Helper.cs");
            }
        }

        /// <summary>
        /// 控件渐入动画 (透明度)
        /// </summary>
        public static void ControlAnimationOpacityEnter(object sender)
        {
            try
            {
                var pair = sender as Tuple<Window, FrameworkElement>;
                var win = pair.Item1;
                var item = pair.Item2;

                double opa = 0.2;
                const double offsetOpa = 0.08;
                while (opa < 1.0)
                {
                    win.Dispatcher.Invoke(() =>
                    {
                        opa = item.Opacity = item.Opacity + offsetOpa > 1.0 ? 1.0 : item.Opacity + offsetOpa;
                    });
                    Thread.Sleep(50);
                }
            }
            catch { }
        }

        /// <summary>
        /// 控件淡出动画 (透明度)
        /// </summary>
        public static void ControlAnimationOpacityLeave(object sender)
        {
            try
            {
                var pair = sender as Tuple<Window, FrameworkElement, Action>;
                var win = pair.Item1;
                var item = pair.Item2;
                var end = pair.Item3;

                double opa = 1.0;
                const double offsetOpa = 0.2;
                while (opa > 0)
                {
                    win.Dispatcher.Invoke(() =>
                    {
                        opa = item.Opacity = item.Opacity - offsetOpa < 0 ? 0 : item.Opacity - offsetOpa;
                    });
                    Thread.Sleep(50);
                }

                win.Dispatcher.Invoke(() => { end(); });
            }
            catch { }
        }

        /// <summary>
        /// 控件渐入动画 (位置)
        /// </summary>
        public static void ControlAnimationEnter(object sender)
        {
            try
            {
                var pair = sender as Tuple<Window, FrameworkElement>;
                var win = pair.Item1;
                var item = pair.Item2;

                // 渐入动画
                double marleft = -8, offsetMarLeft = 1.2;
                const double offsetVelocity = 0.1, minVelocity = 0.5;
                while (marleft < 0)
                {
                    win.Dispatcher.Invoke(() =>
                    {
                        marleft = marleft + offsetMarLeft > 0 ? 0 : marleft + offsetMarLeft;
                        item.Margin = new Thickness(marleft, 0, 0, 0);
                        offsetMarLeft = offsetMarLeft - offsetVelocity < minVelocity ? minVelocity : offsetMarLeft - offsetVelocity;
                    });
                    Thread.Sleep(50);
                }
            }
            catch { }
        }

        /// <summary>
        /// 控件淡出动画 (位置)
        /// </summary>
        public static void ControlAnimationLeave(object sender)
        {
            try
            {
                var pair = sender as Tuple<Window, FrameworkElement, Action>;
                var win = pair.Item1;
                var item = pair.Item2;
                var end = pair.Item3;

                // 淡出动画
                double marleft = 0, offsetLeft = 2;
                const double offsetVelocity = 2.5, maxMarLeft = 20;
                while (marleft < maxMarLeft)
                {
                    win.Dispatcher.Invoke(() =>
                    {
                        marleft = marleft + offsetLeft > maxMarLeft ? maxMarLeft : marleft + offsetLeft;
                        item.Margin = new Thickness(marleft, 0, 0, 0);
                        offsetLeft += offsetVelocity;
                    });
                    Thread.Sleep(50);
                }

                Thread.Sleep(500);
                win.Dispatcher.Invoke(() => { end(); });
            }
            catch { }
        }
    }
}
