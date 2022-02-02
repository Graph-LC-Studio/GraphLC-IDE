using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Controls;
using GraphLC_IDE.Functions;
using GraphLC_IDE.Extensions;

using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using Enterwell.Clients.Wpf.Notifications;

using Path = System.IO.Path;

namespace GraphLC_IDE.Windows.GCloud
{
    /// <summary>
    /// GCloud.xaml 的交互逻辑
    /// </summary>
    public partial class GCloud : MahApps.Metro.Controls.MetroWindow
    {
        public NotificationMessageManager NotificationsManager { get; } = new NotificationMessageManager();

        public GCloud()
        {
            this.DataContext = this;
            InitializeComponent();
            this.NotificationsGrid.Visibility = Visibility.Visible;

            var theme = AppInfo.Theme["name"].ToString();
            ThemeManager.Current.ChangeTheme(this, "Dark." + theme[(theme.IndexOf('.') + 1)..]);

            AccounGrid.Visibility = Visibility.Collapsed;
            selectedItem = ButtonMyFiles;
            TabItemClick(ButtonMyFiles, null);

            Gui.Visibility = Visibility.Collapsed;

            new Thread(() =>
            {
                try
                {
                    var info = Helper.RunGCloud("info");
                    var command = info.ESplit();
                    if (command.Length == 5)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            UserName.Text = command[1];
                            Email.Text = command[2];
                            UsedSpace.Value = double.Parse(command[3]) / double.Parse(command[4]) * 100;
                            UsedSpaceInfo.Tag = UsedSpaceInfo.Text = $"已用 {Helper.ConvertFileSize(long.Parse(command[3]))}/{Helper.ConvertFileSize(long.Parse(command[4]))}";
                            Gui.Visibility = Visibility.Visible;
                        });
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            new LoginWindow() { Owner = this.Owner }.Show();
                            this.Close();
                        });
                    }

                    new Thread(new ParameterizedThreadStart(LoadFiles)).Start("");
                }
                catch(Exception ex)
                {
                    Log.WriteErr(ex.Message, "GCloud.xaml.cs");
                }
            }).Start();
        }

        private void FilesSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (Files.View is GridView gv)
                {
                    gv.Columns[0].Width = Files.ActualWidth - gv.Columns[3].ActualWidth - gv.Columns[2].ActualWidth - gv.Columns[1].ActualWidth - 2;
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "GCloud.xaml.cs");
            }
        }

        public static void BringToFront(FrameworkElement element)
        {
            try
            {
                if (element.Parent is Canvas parent)
                {
                    var maxZ = parent.Children.OfType<UIElement>()
                                              .Where((x) => x != element)
                                              .Select(x => Canvas.GetZIndex(x))
                                              .Max();
                    Canvas.SetZIndex(element, maxZ + 1);
                }
            }
            catch { }
        }

        class FileItem
        {
            public string Name
            {
                get;
                set;
            }
            public string Date
            {
                get;
                set;
            }
            public string Size
            {
                get;
                set;
            }
            public string Shared
            {
                get;
                set;
            }

            public FileItem(string name, string date, string size, string shared)
            {
                Name = name;
                Date = date;
                Size = size;
                Shared = shared;
            }
        }

        private void LoadFiles(object sender)
        {
            try
            {
                if (sender is string dir)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Files.Visibility = Visibility.Collapsed;
                        Ring.Visibility = Visibility.Visible;
                        Files.Items.Clear();
                    });

                    var result = Helper.RunGCloud("dir \"" + dir + "\"");
                    if (result.Replace("\n", "").Trim() == "MissingFile")
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Ring.Visibility = Visibility.Collapsed;
                            Files.Visibility = Visibility.Visible;

                            Files.Tag = dir;
                        });
                        return;
                    }

                    var files = result.Replace("\r", "").Split('\n');
                    foreach(var iter in files)
                    {
                        try
                        {
                            if (iter == "MissingFile")
                                throw new Exception("目录不存在");
                            
                            var command = iter.ESplit();
                            if (command.Length != 4) break;

                            this.Dispatcher.Invoke(() =>
                            {
                                int p = command[0].LastIndexOf('\\');
                                var item = new FileItem(command[0].Substring(p == -1 ? 0 : p + 1), command[1], command[2] == "none" ? "" : Helper.ConvertFileSize(long.Parse(command[2])), command[3] == "true" ? "已共享" : "");
                                Files.Items.Add(item);
                            });

                            Thread.Sleep(1);
                        }
                        catch (Exception ex)
                        {
                            Log.WriteErr(ex.Message, "GCloud.xaml.cs");
                        }
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        Ring.Visibility = Visibility.Collapsed;
                        Files.Visibility = Visibility.Visible;

                        Files.Tag = dir;
                    });
                }
            }
            catch(Exception ex)
            {
                try
                {
                    Log.WriteErr(ex.Message, "GCloud.xaml.cs");
                    this.Dispatcher.Invoke(() =>
                    {
                        NotificationsManager.CreateMessage()
                                            .Animates(true)
                                            .AnimationInDuration(0.3)
                                            .AnimationOutDuration(0.3)
                                            .Accent(Brushes.Red)
                                            .Background("#363636")
                                            .HasBadge("错误")
                                            .HasMessage("无法连接到服务器")
                                            .Dismiss().WithButton("忽略", null)
                                            .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                            .Queue();
                    });
                }
                catch { }
            }
        }

        private bool IsDirectory(FileItem item) => string.IsNullOrWhiteSpace(item.Size);

        private void FilesMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Files.SelectedItem is FileItem item && Files.Tag is string dir && IsDirectory(item))
                {
                    new Thread(new ParameterizedThreadStart(LoadFiles)).Start(Path.Combine(dir, item.Name));
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Log.WriteErr(ex.Message, "GCloud.xaml.cs");
                    this.Dispatcher.Invoke(() =>
                    {
                        NotificationsManager.CreateMessage()
                                           .Animates(true)
                                           .AnimationInDuration(0.3)
                                           .AnimationOutDuration(0.3)
                                           .Accent(Brushes.Red)
                                           .Background("#363636")
                                           .HasBadge("错误")
                                           .HasMessage("无法打开文件夹")
                                           .Dismiss().WithButton("忽略", null)
                                           .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                           .Queue();
                    });
                }
                catch { }
            }
        }

        private TextBlock selectedItem = null;

        private void TabItemMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock item && selectedItem != item)
                item.Background = new SolidColorBrush(Color.FromRgb(62, 62, 62));
        }

        private void TabItemMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock item && selectedItem != item)
                item.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void TabItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock item)
            {
                {
                    selectedItem.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    if (selectedItem.Tag is Grid grid)
                    {
                        grid.Visibility = Visibility.Collapsed;
                    }
                }

                selectedItem = item;

                {
                    selectedItem.Background = (Brush)(TryFindResource("Theme.ShowcaseBrush") ?? new SolidColorBrush(Color.FromRgb(122, 122, 122)));
                    if (selectedItem.Tag is Grid grid)
                    {
                        grid.Visibility = Visibility.Visible;
                        BringToFront(grid);
                    }
                }
            }
        }

        private void ShowTasks(object sender, RoutedEventArgs e) => TaskFlyout.IsOpen = true;

        /// <summary>
        /// 菜单栏弹出
        /// </summary>
        private void FileViewContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (Files.SelectedItem == null)
            {
                MenuMakeDir.Visibility = Visibility.Visible;
                MenuCopy.Visibility = MenuDownload.Visibility = MenuShare.Visibility = MenuMoveTo.Visibility = MenuRename.Visibility = MenuDelete.Visibility = Visibility.Collapsed;
            }
            else
            {
                MenuMakeDir.Visibility = Visibility.Visible;

                if (Files.SelectedItems.Count == 1 && Files.SelectedItem is FileItem item)
                {
                    MenuMakeDir.Visibility = MenuMoveTo.Visibility = MenuRename.Visibility = MenuDelete.Visibility = Visibility.Visible;
                    bool y = !IsDirectory(item);
                    if (y)
                        MenuShare.Header = string.IsNullOrWhiteSpace(item.Shared) ? "共享" : "取消共享";
                    MenuShare.Visibility = y ? Visibility.Visible : Visibility.Collapsed;

                    MenuDownload.Visibility = MenuCopy.Visibility = IsDirectory(item) ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    MenuMakeDir.Visibility = MenuMoveTo.Visibility = MenuRename.Visibility = MenuDelete.Visibility = MenuDownload.Visibility = MenuShare.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void MenuMakeDirClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var tag = Files.Tag.ToString();
                new Thread(() =>
                {
                    try
                    {
                        Task<string> task = null;
                        this.Dispatcher.Invoke(() =>
                        {
                            var Settings = new MetroDialogSettings()
                            {
                                AffirmativeButtonText = "确定",
                                NegativeButtonText = "取消",
                                ColorScheme = MetroDialogColorScheme.Theme
                            };
                            task = this.ShowInputAsync("输入文件夹名称", "", Settings);
                        });
                        task.Wait();

                        if (!string.IsNullOrWhiteSpace(task.Result))
                        {
                            var result = Helper.RunGCloud($"mkdir \"{Path.Combine(tag, task.Result)}\"").Replace("\n","");
                            if (result == "OK")
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    Refresh();
                                });
                            }
                            else
                                this.Dispatcher.Invoke(() =>
                                {
                                    NotificationsManager.CreateMessage()
                                                       .Animates(true)
                                                       .AnimationInDuration(0.3)
                                                       .AnimationOutDuration(0.3)
                                                       .Accent(Brushes.Red)
                                                       .Background("#363636")
                                                       .HasBadge("错误")
                                                       .HasMessage("无法创建目录")
                                                       .Dismiss().WithButton("忽略", null)
                                                       .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                       .Queue();
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "GCloud.xaml.cs - thread");
                    }
                }).Start();
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "GCloud.xaml.cs");
            }
        }

        private void MenuCopyClick(object sender, RoutedEventArgs e)
        {
            if (Files.SelectedItem is FileItem item && Files.Tag is string tag)
            {
                var name = item.Name;
                var url = Path.Combine(tag, item.Name);
                new Thread(() =>
                {
                    try
                    {
                        Task<string> task = null;
                        this.Dispatcher.Invoke(() =>
                        {
                            var Settings = new MetroDialogSettings()
                            {
                                AffirmativeButtonText = "确定",
                                NegativeButtonText = "取消",
                                DefaultText = url[(url.LastIndexOf('\\') + 1)..],
                                ColorScheme = MetroDialogColorScheme.Theme
                            };
                            task = this.ShowInputAsync("输入复制目标", $"要将 {url} 复制到哪里？", Settings);
                        });
                        task.Wait();

                        if (!string.IsNullOrWhiteSpace(task.Result))
                        {
                            var result = Helper.RunGCloud($"copy \"{url}\" \"{Path.Combine(tag, task.Result)}\"").Replace("\n", "");
                            if (result == "OK")
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    Refresh();
                                });
                            }
                            else
                                this.Dispatcher.Invoke(() =>
                                {
                                    NotificationsManager.CreateMessage()
                                                       .Animates(true)
                                                       .AnimationInDuration(0.3)
                                                       .AnimationOutDuration(0.3)
                                                       .Accent(Brushes.Red)
                                                       .Background("#363636")
                                                       .HasBadge("错误")
                                                       .HasMessage("无法复制文件")
                                                       .Dismiss().WithButton("忽略", null)
                                                       .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                       .Queue();
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "GCloud.xaml.cs - thread");
                    }
                }).Start();
            }
        }

        private void MenuUploadClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "文件|*.*",
                Title = "选择上传的文件"
            };
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var file = fileDialog.FileName;
                var suffixName = fileDialog.SafeFileName;

                ShowTasks(null, null);
                var task = new TaskItem(Refresh, $"upload \"{file}\" \"{Path.Combine(Files.Tag.ToString(), suffixName)}\"")
                {
                    Content = suffixName,
                    Description = "上传中",
                    RefreshAfterCompletion = true
                };
                TaskPanel.Children.Add(task);
            }
        }

        private void MenuDownloadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Files.SelectedItem is FileItem item)
                {
                    var name = item.Name;
                    var url = Path.Combine(Files.Tag.ToString(), name);
                    var path = "";

                    System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog
                    {
                        Filter = string.Format("{0} 文件|*.{0}|文件|*.*", new FileInformation(name).Suffix[1..]),
                        Title = "选择文件保存位置",
                        FileName = item.Name
                    };
                    if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        path = fileDialog.FileName;
                        ShowTasks(null, null);
                        TaskItem task = new TaskItem(Refresh, string.Format("download \"{0}\" \"{1}\"", url, path))
                        {
                            Content = name,
                            Description = "下载中"
                        };
                        TaskPanel.Children.Add(task);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "GCloud.xaml.cs");
            }
        }

        private void Refresh()
        {
            if (Files.Tag is string tag)
            {
                new Thread(new ParameterizedThreadStart(LoadFiles)).Start(tag);
                new Thread(() =>
                {
                    var info = Helper.RunGCloud("info").Replace("\n","");
                    var command = info.ESplit();
                    if (command.Length == 5)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            UsedSpace.Value = double.Parse(command[3]) / double.Parse(command[4]) * 100;
                            UsedSpaceInfo.Text = $"已用 {Helper.ConvertFileSize(long.Parse(command[3]))}/{Helper.ConvertFileSize(long.Parse(command[4]))}";
                        });
                    }
                }).Start();
            }
        }

        private void Refresh(object sender, RoutedEventArgs e) => Refresh();

        private void GoParent(object sender, RoutedEventArgs e)
        {
            if (Files.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            {
                int p = tag.LastIndexOf('\\');
                new Thread(new ParameterizedThreadStart(LoadFiles)).Start(p == -1 ? "" : tag.Substring(0, p));
            }
        }

        private void MenuMoveToClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Files.SelectedItem is FileItem item && Files.Tag is string tag)
                {
                    var name = item.Name;
                    var url = Path.Combine(tag, name);
                    new Thread(() =>
                    {
                        try
                        {
                            Task<string> task = null;
                            this.Dispatcher.Invoke(() =>
                            {
                                var p = url.LastIndexOf('\\');
                                var Settings = new MetroDialogSettings()
                                {
                                    AffirmativeButtonText = "确定",
                                    NegativeButtonText = "取消",
                                    DefaultText = url[..(p == -1 ? 0 : p)], // 待修改: 采用相对路径，而不是绝对路径
                                    ColorScheme = MetroDialogColorScheme.Theme
                                };
                                task = this.ShowInputAsync("输入新路径", $"要将 {url} 移动到哪？", Settings);
                            });
                            task.Wait();

                            if (!string.IsNullOrEmpty(task.Result))
                            {
                                var result = Helper.RunGCloud(string.Format("move \"{0}\" \"{1}\"", url, Path.Combine(task.Result, name))).Replace("\n", "");
                                this.Dispatcher.Invoke(() =>
                                {
                                    if (result == "OK")
                                    {

                                        Refresh();
                                    }
                                    else
                                        NotificationsManager.CreateMessage()
                                                           .Animates(true)
                                                           .AnimationInDuration(0.3)
                                                           .AnimationOutDuration(0.3)
                                                           .Accent(Brushes.Red)
                                                           .Background("#363636")
                                                           .HasBadge("错误")
                                                           .HasMessage("无法移动文件")
                                                           .Dismiss().WithButton("忽略", null)
                                                           .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                           .Queue();
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteErr(ex.Message, "GCloud.xaml.cs - thread");
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "GCloud.xaml.cs");
            }
        }

        private void MenuRenameClick(object sender, RoutedEventArgs e)
        {
            if (Files.SelectedItem is FileItem item && Files.Tag is string tag)
            {
                var name = item.Name;
                var url = Path.Combine(tag, item.Name);
                new Thread(() =>
                {
                    try
                    {
                        Task<string> task = null;
                        this.Dispatcher.Invoke(() =>
                        {
                            var Settings = new MetroDialogSettings()
                            {
                                AffirmativeButtonText = "确定",
                                NegativeButtonText = "取消",
                                DefaultText = url[(url.LastIndexOf('\\') + 1)..],
                                ColorScheme = MetroDialogColorScheme.Theme
                            };
                            task = this.ShowInputAsync("输入新名称", string.Format("要将 {0} 重命名为？", url), Settings);
                        });
                        task.Wait();

                        if (!string.IsNullOrEmpty(task.Result))
                        {
                            var result = Helper.RunGCloud(string.Format("rename \"{0}\" \"{1}\"", url, Path.Combine(tag, task.Result))).Replace("\n", "");
                            if (result == "OK")
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    Refresh();
                                });
                            }
                            else
                                this.Dispatcher.Invoke(() =>
                                {
                                    NotificationsManager.CreateMessage()
                                                       .Animates(true)
                                                       .AnimationInDuration(0.3)
                                                       .AnimationOutDuration(0.3)
                                                       .Accent(Brushes.Red)
                                                       .Background("#363636")
                                                       .HasBadge("错误")
                                                       .HasMessage("无法重命名文件")
                                                       .Dismiss().WithButton("忽略", null)
                                                       .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                       .Queue();
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteErr(ex.Message, "GCloud.xaml.cs - thread");
                    }
                }).Start();
            }
        }

        private void MenuShareClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Files.SelectedItem is FileItem item && !string.IsNullOrWhiteSpace(item.Size) && Files.Tag is string tag)
                {
                    var name = item.Name;
                    var url = Path.Combine(tag, item.Name);

                    var command = string.Format("share {0}", url);
                    if (item.Shared == "已共享")
                        command = "un" + command;

                    new Thread(() =>
                    {
                        var result = Helper.RunGCloud(command).Replace("\n","");
                        if (result == "OK")
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                Refresh();
                            });
                        }
                        else
                            this.Dispatcher.Invoke(() =>
                            {
                                NotificationsManager.CreateMessage()
                                                   .Animates(true)
                                                   .AnimationInDuration(0.3)
                                                   .AnimationOutDuration(0.3)
                                                   .Accent(Brushes.Red)
                                                   .Background("#363636")
                                                   .HasBadge("错误")
                                                   .HasMessage("无法共享文件")
                                                   .Dismiss().WithButton("忽略", null)
                                                   .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                   .Queue();
                            });
                    }).Start();
                }
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "GCloud.xaml.cs");
            }
        }

        private void MenuDeleteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Files.SelectedItem is FileItem item && Files.Tag is string tag) 
                {
                    var name = item.Name;
                    var url = Path.Combine(tag, item.Name);
                    new Thread(() =>
                    {
                        try
                        {
                            Task<MessageDialogResult> task = null;
                            this.Dispatcher.Invoke(() =>
                            {
                                var Settings = new MetroDialogSettings()
                                {
                                    AffirmativeButtonText = "取消",
                                    NegativeButtonText = "确定",
                                    ColorScheme = MetroDialogColorScheme.Theme
                                };
                                task = this.ShowMessageAsync("", "确定删除 " + name + " ?", MessageDialogStyle.AffirmativeAndNegative, Settings);
                            });

                            task.Wait();
                            if (task.Result == MessageDialogResult.Negative)
                            {
                                var result = Helper.RunGCloud("del \"" + url + "\"").Replace("\n", "");
                                if (result == "OK")
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        Refresh();
                                    });
                                }
                                else
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        NotificationsManager.CreateMessage()
                                                           .Animates(true)
                                                           .AnimationInDuration(0.3)
                                                           .AnimationOutDuration(0.3)
                                                           .Accent(Brushes.Red)
                                                           .Background("#363636")
                                                           .HasBadge("错误")
                                                           .HasMessage("无法删除此文件")
                                                           .Dismiss().WithButton("忽略", null)
                                                           .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                                           .Queue();
                                    });
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteErr(ex.Message, "GCloud.xaml.cs - thread");
                        }
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "GCloud.xaml.cs");
            }
        }

        /// <summary>
        /// 抛弃文件夹目标
        /// </summary>
        private void FilesDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Link;
            else e.Effects = DragDropEffects.None;
        }

        /// <summary>
        /// 拖动文件夹上传
        /// </summary>
        private void FilesDrop(object sender, DragEventArgs e)
        {
            try
            {
                var drops = (string[])e.Data.GetData(DataFormats.FileDrop);

                ShowTasks(null, null);
                foreach (string iter in drops)
                {
                    if (File.Exists(iter))
                    {
                        var file = iter[(iter.LastIndexOf('\\') + 1)..];
                        TaskItem task = new TaskItem(Refresh, string.Format("upload \"{0}\" \"{1}\"", iter, Path.Combine(Files.Tag.ToString(), file)))
                        {
                            Content = file,
                            Description = "上传中",
                            RefreshAfterCompletion = true
                        };
                        TaskPanel.Children.Add(task);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "GCloud.xaml.cs");
            }
        }

        private void LogOut(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                var result = Helper.RunGCloud("quit");
                this.Dispatcher.Invoke(() => { this.Close(); });
            }).Start();
        }

        private void DownloadFile(object sender, MouseButtonEventArgs e) => MenuDownloadClick(null, null);

        private void DownloadFileFromShareSpace(object sender, RoutedEventArgs e) => DownloadFlyout.IsOpen = true;

        private void CopyFileFromShareSpace(object sender, RoutedEventArgs e) => CopyFlyout.IsOpen = true;

        private void FlyoutGCloudDownloadChoosePathClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "文件|*.*",
                Title = "选择文件保存位置"
            };
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                FlyoutGCloudDownloadPath.Text = fileDialog.FileName;
        }

        private void FlyoutGCloudDownloadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var gcloud = Path.Combine(AppInfo.Path, "Bin", "GCloud", "gcloud.exe");
                if (!File.Exists(gcloud))
                    throw new Exception("GCloud不存在，请重新安装");

                var url = FlyoutGCloudDownloadUrl.Text;
                var path = FlyoutGCloudDownloadPath.Text;

                TaskItem task = new TaskItem(Refresh, "download \"" + url + "\" \"" + path + "\"")
                {
                    Content = path[(path.LastIndexOf("\\") + 1)..],
                    Description = "下载中"
                };
                TaskPanel.Children.Add(task);

                DownloadFlyout.IsOpen = false;
                ShowTasks(null, null);
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");

                NotificationsManager.CreateMessage()
                                   .Animates(true)
                                   .AnimationInDuration(0.3)
                                   .AnimationOutDuration(0.3)
                                   .Accent(Brushes.Red)
                                   .Background("#363636")
                                   .HasBadge("错误")
                                   .HasMessage("下载文件时遇到意外错误")
                                   .Dismiss().WithButton("忽略", null)
                                   .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                   .Queue();
            }
        }

        private void FlyoutGCloudCopyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var gcloud = Path.Combine(AppInfo.Path, "Bin", "GCloud", "gcloud.exe");
                if (!File.Exists(gcloud))
                    throw new Exception("GCloud不存在，请重新安装");

                var url = FlyoutGCloudCopyUrl.Text;
                var path = FlyoutGCloudCopyPath.Text;

                TaskItem task = new TaskItem(Refresh, "copy \"" + url + "\" \"" + path + "\"")
                {
                    Content = path[(path.LastIndexOf("\\") + 1)..],
                    Description = "转存中"
                };
                TaskPanel.Children.Add(task);

                CopyFlyout.IsOpen = false;
                TaskFlyout.IsOpen = true;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs");

                NotificationsManager.CreateMessage()
                                   .Animates(true)
                                   .AnimationInDuration(0.3)
                                   .AnimationOutDuration(0.3)
                                   .Accent(Brushes.Red)
                                   .Background("#363636")
                                   .HasBadge("错误")
                                   .HasMessage("无法转存文件")
                                   .Dismiss().WithButton("忽略", null)
                                   .Dismiss().WithDelay(TimeSpan.FromSeconds(8))
                                   .Queue();
            }
        }

        private void Modify(object sender, RoutedEventArgs e) => new ModifyWindow(UserName.Text) { Owner = this }.Show();

        private void LeaseSpace(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "http://return25.github.io/lease.html",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void Files_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                FilesMouseDoubleClick(null, null);
            else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.M)
                MenuMakeDirClick(null, null);
            else if (e.Key == Key.Delete)
                MenuDeleteClick(null, null);
        }
    }
}
