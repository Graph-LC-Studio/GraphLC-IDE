using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ControlzEx.Theming;
using ICSharpCode.AvalonEdit;
using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;

using GraphLC_IDE.AppConfig;
using GraphLC_IDE.Functions;

namespace GraphLC_IDE.Windows
{
    /// <summary>
    /// SearchBar.xaml 的交互逻辑
    /// </summary>
    public partial class SearchBar : MetroWindow
    {
        private TextEditor Editor = null;
        public SearchBar(TextEditor editor)
        {
            InitializeComponent();
            Editor = editor;
            SearchText.Focus();

            ThemeManager.Current.ChangeTheme(this, AppInfo.Theme["name"].ToString());

            if (editor.SelectedText != "")
                SearchText.Text = editor.SelectedText;
            var cache = AppInfo.MainWindowProperty["searchbar"] as JObject;
            Case.IsChecked = (bool)cache["case"];
            WholeWord.IsChecked = (bool)cache["whole"];
            UseRegex.IsChecked = (bool)cache["regex"];
        }

        private bool MatchWholeWord(int index, int length)
        {
            if (index > 0 && !char.IsPunctuation(Editor.Text[index - 1])) return false;
            if (index + length < Editor.Text.Length - 1 && !char.IsSymbol(Editor.Text[index + length + 1])) return false;
            return true;
        }

        private void SearchNext(ref int index, ref int length)
        {
            if (UseRegex.IsChecked.Value)
            {
                var result = new Regex(SearchText.Text).Match(Editor.Text, Editor.SelectionStart + Editor.SelectionLength);
                index = result.Index;
                length = result.Length;
            }
            else
            {
                length = SearchText.Text.Length;
                do
                {
                    index = Editor.Text.IndexOf(SearchText.Text, index + 1, Case.IsChecked.Value ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
                }
                while (index != -1 && WholeWord.IsChecked.Value && !MatchWholeWord(index, length));
            }
        }

        private void SearchPrevious(ref int index, ref int length)
        {
            if (UseRegex.IsChecked.Value)
            {
                var result = new Regex(SearchText.Text).Match(Editor.Text, 0, Editor.SelectionStart - Editor.SelectionLength);
                index = result.Index;
                length = result.Length;
            }
            else
            {
                length = SearchText.Text.Length;
                do
                {
                    index = Editor.Text.LastIndexOf(SearchText.Text, index - 1, Case.IsChecked.Value ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
                }
                while (index != -1 && WholeWord.IsChecked.Value && !MatchWholeWord(index, length));
            }
        }

        private void CommandBinding_Search(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                int index = Editor.SelectionStart + Editor.SelectionLength - 1, length = 0;

                SearchNext(ref index, ref length);

                if (index != -1)
                {
                    Editor.SelectionStart = index;
                    Editor.SelectionLength = length;
                    var loc = Editor.Document.GetLocation(index);
                    Editor.ScrollTo(loc.Line, loc.Column);
                }
                else
                    _ = Helper.MetroBox(this, "未能找到以下文本：" + SearchText.Text, "查找和替换", "确定");
            }
            catch (Exception ex)
            {
                _ = Helper.MetroBox(this, "搜索失败：" + SearchText.Text, "查找和替换", "确定");
                Log.WriteErr(ex.Message, "SearchBar.xaml.cs");
            }
        }

        private void SearchNext(object sender, RoutedEventArgs e)
        {
            CommandBinding_Search(null, null);
        }

        private void SearchText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CommandBinding_Search(null, null);
        }

        private void SearchPrevious(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = Editor.SelectionStart + Editor.SelectionLength - 1, length = 0;

                SearchPrevious(ref index, ref length);

                if (index != -1)
                {
                    Editor.SelectionStart = index;
                    Editor.SelectionLength = SearchText.Text.Length;
                    var loc = Editor.Document.GetLocation(index);
                    Editor.ScrollTo(loc.Line, loc.Column);
                }
                else
                    _ = Helper.MetroBox(this, "未能找到以下文本：" + SearchText.Text, "查找和替换", "确定");
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "SearchBar.xaml.cs");
            }
        }

        private void CommandBinding_Replace(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                int index = Editor.SelectionStart + Editor.SelectionLength - 1, length = 0;
                SearchNext(ref index, ref length);
                if (index != -1)
                {
                    Editor.SelectionStart = index;
                    Editor.SelectionLength = length;
                    Editor.SelectedText = ReplaceText.Text;
                    var loc = Editor.Document.GetLocation(index);
                    Editor.ScrollTo(loc.Line, loc.Column);
                }
            }
            catch(Exception ex)
            {
                Log.WriteErr(ex.Message, "SearchBar.xaml.cs");
            }
        }

        private void Replace(object sender, RoutedEventArgs e)
        {
            CommandBinding_Replace(null, null);
        }

        private void ReplaceAll(object sender, RoutedEventArgs e)
        {
            try
            {
                int cnt = 0;
                while (true)
                {
                    int index = Editor.SelectionStart + Editor.SelectionLength - 1, length = 0;
                    SearchNext(ref index, ref length);

                    if (index != -1)
                    {
                        Editor.SelectionStart = index;
                        Editor.SelectionLength = length;
                        Editor.SelectedText = ReplaceText.Text;

                        Editor.SelectionStart += length;
                        Editor.SelectionLength = 0;
                        var loc = Editor.Document.GetLocation(index);
                        Editor.ScrollTo(loc.Line, loc.Column);

                        ++cnt;
                    }
                    else break;
                }

                _ = Helper.MetroBox(this, string.Format("替换了 {0} 处搜索项", cnt), "替换结果", "确定");
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "SearchBar.xaml.cs");
            }
        }

        private void SearchBarClothing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var cache = AppInfo.MainWindowProperty["searchbar"] as JObject;
            cache["case"] = Case.IsChecked.Value;
            cache["whole"] = WholeWord.IsChecked.Value;
            cache["regex"] = UseRegex.IsChecked.Value;
            AppInfo.MainWindowProperty.Save();
        }
    }
} 