using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using GraphLC_IDE.AppConfig;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace GraphLC_IDE.Functions
{
    class Helper
    {
        /// <summary>
        /// 返回不占用文件的BitmapImage
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns></returns>
        public static BitmapImage GetBitmapImage(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imagePath);
            bitmap.EndInit();
            return bitmap.Clone();
        }

        public static string RunGCloud(string command)
        {
            try
            {
                var output = "";
                var p = new Process();
                var dir = Path.Combine(AppInfo.Path, "Bin", "GCloud");

                p.StartInfo.Arguments = command;
                p.StartInfo.FileName = Path.Combine(dir, "gcloud.exe");
                p.StartInfo.WorkingDirectory = dir;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output += e.Data + Environment.NewLine;
                };
                
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();
                p.Close();
                return output.Replace("\r", "").Trim();
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Helper.cs");
                return "UnknownError";
            }
        }

        public static async Task<MessageDialogResult> MetroBox(MetroWindow win, string message, string title = "", string button = "关闭")
        {
            var Settings = new MetroDialogSettings()
            {
                AffirmativeButtonText = button,
                ColorScheme = MetroDialogColorScheme.Theme
            };
            return await win.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, Settings);
        }

        /// <summary>
        /// 运行环境是否为Linux
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        /// <summary>
        /// 确定当前主体是否属于具有指定 Administrator 的 Windows 用户组
        /// </summary>
        /// <returns>如果当前主体是指定的 Administrator 用户组的成员，则为 true；否则为 false。</returns>
        public static bool IsAdministrator()
        {
            bool result;
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                result = principal.IsInRole(WindowsBuiltInRole.Administrator);

                //http://www.cnblogs.com/Interkey/p/RunAsAdmin.html
                //AppDomain domain = Thread.GetDomain();
                //domain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                //WindowsPrincipal windowsPrincipal = (WindowsPrincipal)Thread.CurrentPrincipal;
                //result = windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                result = false;
            }
            return result;
        }

        //函数指针
        public delegate void HandleControlPropertyFunctionWithObject(object control);
        
        /// <summary>
        /// 遍历 Menu 
        /// </summary>
        /// <param name="menu">Menu</param>
        /// <param name="handleControl">执行的操作</param>
        /// <param name="tierCount">递归层数</param>
        public static void SetControlProperty<T>(T menu, HandleControlPropertyFunctionWithObject handleControl, int tierCount = -1) where T : ItemsControl
        {
            if (AppInfo.Theme.Token == null) return;
            foreach (object item in menu.Items)
            {
                try
                {
                    SetAllControlProperty<MenuItem>((MenuItem)item, handleControl, tierCount > 0 ? tierCount - 1 : tierCount);
                }
                catch(Exception) { }
            }
        }

        /// <summary>
        /// 遍历 control 以及其子项并执行操作
        /// </summary>
        /// <param name="control">要遍历的控件</param>
        /// <param name="handleControl">对遍历的对象执行的操作</param>
        /// <param name="tierCount">递归层数</param>
        public static void SetAllControlProperty<T>(object control, HandleControlPropertyFunctionWithObject handleControl, int tierCount = -1) where T : ItemsControl
        {
            if (tierCount == 0) return;

            T item = (T)control;
            if (item.Items.Count == 0) return;
            foreach (object each in item.Items)
            {
                try
                {
                    MenuItem child = (MenuItem)each;
                    handleControl(each);
                    SetAllControlProperty<T>(child, handleControl, tierCount > 0 ? tierCount - 1 : tierCount);
                }
                catch(Exception) { }
            }
        }

        /// <summary>
        /// 换算空间
        /// </summary>
        /// <param name="size">空间大小</param>
        /// <returns></returns>
        public static string ConvertFileSize(long size)
        {
            string result = "0KB";
            int filelength = size.ToString().Length;
            if (filelength < 4)
                result = size + "B";
            else if (filelength < 7)
                result = Math.Round(Convert.ToDouble(size / 1024d), 2) + "KB";
            else if (filelength < 10)
                result = Math.Round(Convert.ToDouble(size / 1024d / 1024), 2) + "MB";
            else if (filelength < 13)
                result = Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024), 2) + "GB";
            else
                result = Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024 / 1024), 2) + "TB";
            return result;
        }
    }
}
