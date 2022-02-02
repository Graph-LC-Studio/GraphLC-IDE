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
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Controls;
using GraphLC_IDE.Functions;

using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using TCPSocket;
using Encryption;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Common;

using Path = System.IO.Path;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// ModuleInstaller.xaml 的交互逻辑
    /// </summary>
    public partial class ModuleInstaller : MahApps.Metro.Controls.MetroWindow
    {
        class ModuleInfo
        {
            public string Name
            {
                get; set;
            }

            public string Title
            {
                get; set;
            }

            public string Description
            {
                get; set;
            }

            public long Size
            {
                get; set;
            }

            public long ZipSize
            {
                get; set;
            }

            public string Script
            {
                get; set;
            }
            public string Type
            {
                get; set;
            }

            public bool isInstall
            {
                get; set;
            }

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }

        private JArray modlist = AppInformation.config["ide"]["module"] as JArray, envlist = AppInformation.config["ide"]["environment"] as JArray;
        private List<ModuleInfo> installList = new List<ModuleInfo>();
        private long downloadSize = 0, totalDownloadSize = 0;

        public ModuleInstaller()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, "Dark.Blue");
            InstallProgress.Visibility = Visibility.Collapsed;

            new Thread(DownloadModuleList).Start();
        }

        /// <summary>
        /// 与服务器建立连接
        /// </summary>
        private Socket Connect()
        {
            // 连接到服务器
            Socket s = null;
            foreach (var iter in AppInformation.config["gcloud"]["ref"])
            {
                try
                {
                    string ip = new AESEncryption(AppInformation.AesKey).AESDecrypt(iter.ToString());
                    IPEndPoint ipe = null;

                    // 判断是否为ip
                    Regex rx = new Regex(@"((?:(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))\.){3}(?:25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d))))");
                    if (!rx.IsMatch(ip))
                    {
                        IPAddress[] IPs = Dns.GetHostAddresses(ip);
                        ipe = new IPEndPoint(IPs[0], int.Parse(new AESEncryption(AppInformation.AesKey).AESDecrypt(AppInformation.config["gcloud"]["link"].ToString())));
                    }
                    else
                        ipe = new IPEndPoint(IPAddress.Parse(ip), int.Parse(new AESEncryption(AppInformation.AesKey).AESDecrypt(AppInformation.config["gcloud"]["link"].ToString())));

                    s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    s.Connect(ipe);//尝试建立连接

                    if(s.Connected)
                        break;
                }
                catch { continue; }
            }

            if (s == null || !s.Connected) throw new Exception("无法连接至服务器");

            // 客户端签名
            {
                var rsa = new RSAEncryption(AppInformation.privateKey, AppInformation.publicKey);

                // 从服务端接收签名内容
                // 签名的内容随机
                var str = QSocket.Recv(s);

                // 客户端签名
                QSocket.Send(s, rsa.Sign(Encoding.UTF8.GetBytes(str)));

                var r = QSocket.Recv(s);
                if (r != "OK")
                {
                    s.Close();
                    throw new Exception(string.Format("与服务器建立连接时出错, 已自动终止连接"));
                }
            }

            return s;
        }

        private bool CloseConnection(Socket s)
        {
            try
            {
                QSocket.Send(s, "quit");
                _ = QSocket.Recv(s);
                s.Close();
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "ModuleInstaller.xaml.cs");
                return false;
            }
        }

        private void DownloadModuleList()
        {
            try
            {
                var s = Connect();

                QSocket.Send(s, "get-txt list.json");
                var list = QSocket.Recv(s);
                var obj = (JObject)JsonConvert.DeserializeObject(list);

                this.Dispatcher.Invoke(() =>
                {
                    Ring.Visibility = Visibility.Collapsed;
                    ModuleListViewer.Visibility = Visibility.Visible;
                });

                foreach (var iter in obj["list"] as JArray)
                {
                    try
                    {
                        var icon = Path.Combine(AppInformation.Path, "Cache", (string)iter["name"] + "_icon.png");
                        var down = new DownloadFileFromSocket(icon);
                        QSocket.Send(s, "get " + (string)iter["icon"]);
                        QSocket.Recv(s, down.Handle);
                        down.Dispose();

                        ModuleItem item = null;
                        this.Dispatcher.Invoke(() =>
                        {
                            item = new ModuleItem() { Height = 106, Margin = new Thickness(8, 5, 8, 5) };
                            item.Title = (string)iter["title"];
                            item.Description = (string)iter["description"];
                            item.Icon = Helper.GetBitmapImage(icon);
                            item.Opacity = (int)AppInformation.config["ide"]["settings"]["animation"]["level"] >= 2 ? 0.2 : 1.0;
                            item.CheckChanged += CountTotalSpace;

                            var tag = new ModuleInfo() { Name = (string)iter["name"], Title = (string)iter["title"], Description = (string)iter["description"], ZipSize = (long)iter["zip"], Size = (long)iter["size"], Script = (string)iter["script"], Type = (string)iter["type"] };
                            item.Tag = tag;

                            if (Search(modlist, (string)iter["name"]) || Search(envlist, (string)iter["name"]))
                                item.isCheck = true;

                            ModuleList.Children.Add(item);
                        });

                        AnimationHelper.BeginAnimation(2, new Tuple<Window, FrameworkElement>(this, item), AnimationHelper.ControlAnimationOpacityEnter);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "ModuleInstaller.xaml.cs");
                    }
                }

                CloseConnection(s);
            }
            catch (Exception ex)
            {
                try
                {
                    string message = ex.Message.Replace(new AESEncryption(AppInformation.AesKey).AESDecrypt((string)AppInformation.config["gcloud"]["link"]), "");
                    foreach (var iter in AppInformation.config["gcloud"]["ref"])
                        try
                        {
                            var ip = new AESEncryption(AppInformation.AesKey).AESDecrypt((string)iter);
                            message = message.Replace(ip, "");
                        }
                        catch { }

                    Log.WriteErr(message, "ModuleInstaller.xaml.cs");
                    this.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current.MainWindow != null)
                            new PopInformation(this, new TimeSpan(0, 0, 7))
                            {
                                Title = "获取模块列表时出错",
                                Content = message
                            }.Show();
                    });
                }
                catch { }
            }
        }

        private void OnReciveBytes(int length, object _)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    downloadSize += length;
                    TotalInstall.Text = string.Format("已下载 {0}/{1}", Helper.ConvertFileSize(downloadSize), Helper.ConvertFileSize(totalDownloadSize));
                    Progress.Value = (double)(downloadSize) / (double)(totalDownloadSize) * 100.0;
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "ModuleInstaller.xaml.cs");
                }
            });
        }

        private void UpdateDescription(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                Description.Text = message;
            });
        }

        private bool Search(JArray array, string obj)
        {
            foreach (var iter in array)
            {
                if (iter.ToString() == obj)
                    return true;
            }
            return false;
        }

        public static bool Remove(JArray array, string obj)
        {
            for (int i = 0; i < array.Count; ++i)
            {
                if (array[i].ToString() == obj)
                {
                    array.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private void Modify(object sender, RoutedEventArgs e)
        {
            if (!Helper.IsAdministrator())
            {
                _ = Helper.MetroBox(this, "模块管理器在没有获得管理员权限的情况下无法安装模块，请确保 GraphLC IDE 以管理员权限启动。", "警告");
            }
            else if(sender is Button button)
            {
                button.IsEnabled = false;
                ModuleListViewer.Visibility = Visibility.Collapsed;
                InstallProgress.Visibility = Visibility.Visible;

                new Thread(() =>
                {
                    try
                    {
                        foreach (var module in installList)
                        {
                            if (module != null)
                            {
                                // 安装
                                if (module.isInstall)
                                {
                                    var s = Connect();

                                    // 获取gsc脚本
                                    QSocket.Send(s, string.Format("get-txt \"{0}\"", module.Script));
                                    var gsc = QSocket.Recv(s).Replace("\r", "").Split('\n');

                                    foreach (var iter in gsc)
                                    {
                                        var sp = new StringSplitter(iter);

                                        if (sp[0] == "download" && sp.Count == 3)
                                        {
                                            /* 下载文件
                                             * 格式
                                             * download file1(服务器) file2(本地)
                                             */
                                            UpdateDescription("下载文件中");

                                            var down = new DownloadFileFromSocket(Path.Combine(AppInformation.Path, sp[2]), new Action<int, object>(OnReciveBytes));
                                            QSocket.Send(s, string.Format("get \"{0}\"", sp[1]));
                                            QSocket.Recv(s, down.Handle, 128 * 1024);
                                            down.Dispose();
                                        }
                                        else if (sp[0] == "unzip" && sp.Count == 3)
                                        {
                                            /* 解压压缩文件
                                             * 格式
                                             * unzip zipfile dir
                                             */
                                            UpdateDescription("解压文件中");

                                            var filename = Path.Combine(AppInformation.Path, sp[1]);
                                            var dir = Path.Combine(AppInformation.Path, sp[2]);
                                            if (!Directory.Exists(dir))
                                                Directory.CreateDirectory(dir);

                                            var archive = ArchiveFactory.Open(sp[1]);
                                            foreach (var entry in archive.Entries)
                                            {
                                                if (!entry.IsDirectory)
                                                {
                                                    Console.WriteLine(entry.Key);
                                                    entry.WriteToDirectory(dir, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                                                }
                                            }
                                        }
                                        else if (sp[0] == "delete" && sp.Count == 2)
                                        {
                                            /* 删除文件/目录
                                             * 格式
                                             * delete path
                                             */
                                            UpdateDescription("删除文件");

                                            try
                                            {
                                                var path = Path.Combine(AppInformation.Path, sp[1]);
                                                if (File.Exists(path))
                                                    new FileInfo(path).Delete();
                                                else if (Directory.Exists(path))
                                                    new DirectoryInfo(path).Delete(true);
                                            }
                                            catch { }
                                        }
                                        else if (sp[0] == "add-module" && sp.Count == 2)
                                        {
                                            /* 添加模块到模块列表
                                             * 格式
                                             * add-module name
                                             */
                                            UpdateDescription("更新列表");

                                            modlist.Add(sp[1]);
                                        }
                                        else if (sp[0] == "add-env" && sp.Count == 2)
                                        {
                                            /* 添加环境到环境列表
                                             * 格式
                                             * add-env name
                                             */
                                            UpdateDescription("更新列表");

                                            envlist.Add(sp[1]);
                                        }
                                        else if (sp[0] == "add-path" && sp.Count == 2)
                                        {
                                            /* 添加环境变量
                                             * 格式
                                             * add-path item
                                             */
                                            UpdateDescription("自动配置环境变量");

                                            try
                                            {
                                                var path = Path.Combine(AppInformation.Path, sp[1]);
                                                if (Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine).IndexOf(path) == -1)
                                                    Environment.SetEnvironmentVariable("Path", Environment.GetEnvironmentVariable("Path") + ";" + path, EnvironmentVariableTarget.Machine);
                                            }
                                            catch { }
                                        }
                                        else
                                            continue;
                                    }
                                }
                                // 卸载
                                else
                                {
                                    try
                                    {
                                        UpdateDescription("正在卸载 " + module.Name);
                                        Remove(module.Type == "Module" ? modlist : envlist, module.Name);
                                        new DirectoryInfo(Path.Combine(AppInformation.Path, "Config", module.Type, module.Name)).Delete(true);
                                    }
                                    catch { }
                                }
                            }
                        }

                        Task<MessageDialogResult> task = null;
                        this.Dispatcher.Invoke(() =>
                        {
                            Progress.Value = 100;
                            var settings = new MetroDialogSettings
                            {
                                AffirmativeButtonText = "取消",
                                NegativeButtonText = "重启",
                                ColorScheme = MetroDialogColorScheme.Theme
                            };
                            task = this.ShowMessageAsync("修改成功", "请重启 GraphLC IDE", MessageDialogStyle.AffirmativeAndNegative, settings);
                        });
                        task.Wait();

                        if (task.Result == MessageDialogResult.Negative)
                        {
                            AppInformation.config.Save();
                            this.Dispatcher.Invoke(() =>
                            {
                                Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                                Application.Current.Shutdown();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "ModuleInstaller.xaml.cs");
                        this.Dispatcher.Invoke(() =>
                        {
                            _ = Helper.MetroBox(this, "错误信息 : " + ex.Message, "下载的时候遇到了一个问题");
                        });
                    }
                }).Start();
            }
        }

        private void CountTotalSpace(object sender, RoutedEventArgs e)
        {
            try
            {
                long tot = 0;
                totalDownloadSize = 0;

                installList.Clear();
                foreach (var iter in ModuleList.Children)
                {
                    if (iter is ModuleItem item && item.Tag is ModuleInfo info)
                    {
                        bool ins = false;
                        if (ins = (Search(modlist, info.Name) || Search(envlist, info.Name)))
                            tot = item.isCheck ? tot : tot - info.Size;
                        else
                        {
                            if (item.isCheck)
                            {
                                totalDownloadSize += info.ZipSize;
                                tot += info.Size;
                            }
                        }

                        if (item.isCheck != ins)
                        {
                            var add = info.Clone() as ModuleInfo;
                            add.isInstall = !ins;
                            installList.Add(add);
                        }
                    }
                }

                TotalSpace.Text = string.Format("共需 {0}{1}", tot < 0 ? "-" : "", Helper.ConvertFileSize(Math.Abs(tot)));
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "ModuleInstaller.xaml.cs");
            }
        }
    }
}