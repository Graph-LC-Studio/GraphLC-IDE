using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.Threading.Tasks;

using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Controls;
using GraphLC_IDE.Functions;
using GraphLC_IDE.Extensions;

using ControlzEx.Theming;
using Newtonsoft.Json.Linq;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using Path = System.IO.Path;

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : MahApps.Metro.Controls.MetroWindow
    {
        private Dictionary<string, int> themeBook = new Dictionary<string, int>();
        private string[] themeRef = new string[9] { "red", "orange", "yellow", "green", "blue", "purple", "pink", "brown", "steel" };
        private Dictionary<string, int> stretchBook = new Dictionary<string, int>();
        private string[] stretchRef = new string[4] { "none", "fill", "uniform", "uniformtofill" };

        public Settings()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, AppInfo.Theme.Token == null ? "Dark.Blue" : AppInfo.Theme["name"].ToString());

            // 样式
            try
            {
                var style = ((int)AppInfo.Config["ide"]["settings"]["animation"]["level"] > 0 ?
                    TryFindResource("MahApps.Styles.TabControl.AnimatedSingleRow") :
                    TryFindResource("MahApps.Styles.TabControl")) as Style;

                if (style != null)
                    Tab.Style = style;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "MainWindow.xaml.cs - Init - 样式");
            }

            themeBook["red"] = 0;
            themeBook["orange"] = 1;
            themeBook["yellow"] = 2;
            themeBook["green"] = 3;
            themeBook["blue"] = 4;
            themeBook["purple"] = 5;
            themeBook["pink"] = 6;
            themeBook["brown"] = 7;
            themeBook["steel"] = 8;

            stretchBook["none"] = 0;
            stretchBook["fill"] = 1;
            stretchBook["uniform"] = 2;
            stretchBook["uniformtofill"] = 3;

            EditorFontName.Items.Clear();
            InstalledFontCollection fonts = new InstalledFontCollection();
            foreach (var family in fonts.Families)
            {
                EditorFontName.Items.Add(family.Name);
            }

            LoadConfig(AppInfo.Config);
        }

        private void LoadConfig(CfgLoader cfg)
        {
            // 主题
            try
            {
                if (ThemeMode.IsOn = (bool)AppInfo.Config["ide"]["use-buildin-theme"])
                {
                    ThemeDepth.SelectedItem = ThemeColor.SelectedItem = 0;
                    ThemeFileName.Text = (string)AppInfo.Config["ide"]["theme"];
                    if (!File.Exists(Path.Combine(AppInfo.Path, "Config", ThemeFileName.Text)))
                        ThemeFileName.Text = "";
                }
                else
                {
                    var themeName = ((string)AppInfo.Config["ide"]["theme"]).Replace(".cfg", "").ESplit('-');
                    ThemeDepth.SelectedIndex = themeName[0] == "dark" ? 1 : 0;
                    ThemeColor.SelectedIndex = themeBook[themeName[1]];
                }
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            // 编辑器
            try
            {
                var editor = AppInfo.Config["ide"]["settings"]["editor"] as JObject;
                var fore = editor["fore"] as JArray;
                EditorColorFore.SelectedColor = Color.FromArgb((byte)fore[0], (byte)fore[1], (byte)fore[2], (byte)fore[3]);

                var linefore = editor["linefore"] as JArray;
                EditorColorLineNumberFore.SelectedColor = Color.FromArgb((byte)linefore[0], (byte)linefore[1], (byte)linefore[2], (byte)linefore[3]);

                var font = editor["font"] as JObject;
                EditorFontName.Text = EditorFontName.Items.Contains((string)font["name"]) ? (string)font["name"] : "Consolas";
                EditorFontSize.Value = (int)font["size"];
                EditorFontWeight.SelectedIndex = (string)font["weight"] == "light" ? 0 : ((string)font["weight"] == "bold" ? 2 : 1);

                var back = editor["back"] as JObject;
                EditorBackgroundImagePath.Text = (string)back["image"];
                EditorBackgroundImageOpacity.Value = (int)((double)back["opacity"] * 100);
                EditorBackgroundFillMode.SelectedIndex = stretchBook[(string)back["stretch"]];

                if (editor.ContainsKey("encode"))
                {
                    var encode = editor["encode"] as JObject;
                    if (encode.ContainsKey("open"))
                        EditorOpenEncode.Text = encode["open"].ToString();
                    if (encode.ContainsKey("create"))
                        EditorCreateEncode.Text = encode["create"].ToString();
                }

                EditorWordWrap.IsOn = (bool)editor["wordwrap"];
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            // 补全
            try
            {
                var list = AppInfo.Config["ide"]["settings"]["completion"]["list"] as JArray;

                foreach(var iter in list)
                {
                    if (iter is JArray group && group.Count == 4)
                    {
                        var newtab = new MetroTabItem();// { Header = group[0].ToString() };
                        var panel = new CompletionItem() { BindingHeaderedControl = newtab, CompletionName = string.IsNullOrWhiteSpace(group[0].ToString()) ? "未命名补全项" : group[0].ToString(), CompletionContent = group[1].ToString(), CompletionDescription = group[2].ToString(), CompletionIconPath = group[3].ToString() };
                        newtab.Content = panel;
                        CompletionTabs.Items.Add(newtab);
                    }
                }

                CompletionTabs.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            // 文件树
            try
            {
                FileTreeSync.IsOn = (bool)AppInfo.Config["ide"]["settings"]["file-tree"]["sync"];
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            // 动画
            try
            {
                AnimationLevel.SelectedIndex = (int)AppInfo.Config["ide"]["settings"]["animation"]["level"];
                AnimationHardwareAccelerate.IsOn = (bool)AppInfo.Config["ide"]["settings"]["animation"]["accelerate"];
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }
        }

        private void SettingsClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 主题
            try
            {
                if ((bool)(AppInfo.Config["ide"]["use-buildin-theme"] = ThemeMode.IsOn))
                    AppInfo.Config["ide"]["theme"] = ThemeFileName.Text;
                else
                    AppInfo.Config["ide"]["theme"] = (ThemeDepth.SelectedIndex == 1 ? "dark" : "light") + "-" + themeRef[ThemeColor.SelectedIndex] + ".cfg";
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            // 编辑器
            try
            {
                var editor = AppInfo.Config["ide"]["settings"]["editor"] as JObject;
                var fore = editor["fore"] as JArray;
                fore[0] = EditorColorFore.SelectedColor.Value.A;
                fore[1] = EditorColorFore.SelectedColor.Value.R;
                fore[2] = EditorColorFore.SelectedColor.Value.G;
                fore[3] = EditorColorFore.SelectedColor.Value.B;

                var linefore = editor["linefore"] as JArray;
                linefore[0] = EditorColorLineNumberFore.SelectedColor.Value.A;
                linefore[1] = EditorColorLineNumberFore.SelectedColor.Value.R;
                linefore[2] = EditorColorLineNumberFore.SelectedColor.Value.G;
                linefore[3] = EditorColorLineNumberFore.SelectedColor.Value.B;

                var font = editor["font"] as JObject;
                font["name"] = string.IsNullOrWhiteSpace(EditorFontName.Text) ? "Consolas" : EditorFontName.Text;
                font["size"] = EditorFontSize.Value;
                font["weight"] = EditorFontWeight.SelectedIndex == 0 ? "light" : (EditorFontWeight.SelectedIndex == 2 ? "bold" : "normal");

                var back = editor["back"] as JObject;
                back["image"] = EditorBackgroundImagePath.Text;
                back["opacity"] = EditorBackgroundImageOpacity.Value / 100.0;
                back["stretch"] = stretchRef[EditorBackgroundFillMode.SelectedIndex];

                var encode = new JObject();
                encode["open"] = EditorOpenEncode.Text;
                encode["create"] = EditorCreateEncode.Text;
                editor["encode"] = encode;

                editor["wordwrap"] = EditorWordWrap.IsOn;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            // 代码补全
            try
            {
                var list = new JArray();
                foreach(var iter in CompletionTabs.Items)
                {
                    if(iter is TabItem item && item.Content is CompletionItem info)
                    {
                        var group = new JArray();
                        group.Add(info.CompletionName);
                        group.Add(info.CompletionContent);
                        group.Add(info.CompletionDescription);
                        group.Add(info.CompletionIconPath);
                        list.Add(group);
                    }
                }
                AppInfo.Config["ide"]["settings"]["completion"]["list"] = list;
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            // 文件树
            try
            {
                AppInfo.Config["ide"]["settings"]["file-tree"]["sync"] = FileTreeSync.IsOn;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            // 动画
            try
            {
                AppInfo.Config["ide"]["settings"]["animation"]["level"] = AnimationLevel.SelectedIndex;
                AppInfo.Config["ide"]["settings"]["animation"]["accelerate"] = AnimationHardwareAccelerate.IsOn;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "Settings.xaml.cs");
            }

            AppInfo.Config.Save();
        }

        private void AddCompletionItem(object sender, RoutedEventArgs e)
        {
            var newtab = new MetroTabItem() { Header = "未命名补全项" };
            var panel = new CompletionItem() { BindingHeaderedControl = newtab};
            newtab.Content = panel;
            CompletionTabs.Items.Add(newtab);
            CompletionTabs.SelectedIndex = CompletionTabs.Items.Count - 1; //定位到这个TabItem
        }

        private void RemoveCompletionItem(object sender, RoutedEventArgs e)
        {
            if(CompletionTabs.SelectedItem is TabItem item)
            {
                var name = item.Header;
                new Thread(() =>
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
                        this.Dispatcher.Invoke(() =>
                        {
                            CompletionTabs.Items.Remove(CompletionTabs.SelectedItem);
                        });
                }).Start();
            }
        }
    }
}
