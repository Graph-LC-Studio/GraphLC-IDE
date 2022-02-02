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

using GraphLC_IDE.AppConfig;
using ICSharpCode.AvalonEdit.Folding;

namespace GraphLC_IDE.Controls
{
    /// <summary>
    /// CompletiionItem.xaml 的交互逻辑
    /// </summary>
    public partial class CompletionItem : UserControl
    {
        public CompletionItem()
        {
            InitializeComponent();

            CompletionContentBox.AllowDrop = false;
            CompletionContentBox.Focusable = true;
            CompletionContentBox.ShowLineNumbers = true;
            CompletionContentBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            CompletionContentBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            CompletionContentBox.WordWrap = (bool)AppInfo.Config["ide"]["settings"]["editor"]["wordwrap"];
            CompletionContentBox.Foreground = new SolidColorBrush(Color.FromArgb(
                            (byte)AppInfo.Config["ide"]["settings"]["editor"]["fore"][0],
                            (byte)AppInfo.Config["ide"]["settings"]["editor"]["fore"][1],
                            (byte)AppInfo.Config["ide"]["settings"]["editor"]["fore"][2],
                            (byte)AppInfo.Config["ide"]["settings"]["editor"]["fore"][3]));
            CompletionContentBox.LineNumbersForeground = new SolidColorBrush(Color.FromArgb(
                            (byte)AppInfo.Config["ide"]["settings"]["editor"]["linefore"][0],
                            (byte)AppInfo.Config["ide"]["settings"]["editor"]["linefore"][1],
                            (byte)AppInfo.Config["ide"]["settings"]["editor"]["linefore"][2],
                            (byte)AppInfo.Config["ide"]["settings"]["editor"]["linefore"][3]));
            CompletionContentBox.FontFamily = new System.Windows.Media.FontFamily(AppInfo.Config["ide"]["settings"]["editor"]["font"]["name"].ToString());
            CompletionContentBox.FontSize = (double)AppInfo.Config["ide"]["settings"]["editor"]["font"]["size"];
            CompletionContentBox.FontWeight = AppInfo.Config["ide"]["settings"]["editor"]["font"]["weight"].ToString() == "bold" ?
                        System.Windows.FontWeights.Bold :
                        AppInfo.Config["ide"]["settings"]["editor"]["font"]["weight"].ToString() == "light" ? System.Windows.FontWeights.Light : System.Windows.FontWeights.Normal;

            // 折叠
            FoldingManager _ = FoldingManager.Install(CompletionContentBox.TextArea);
        }

        public HeaderedContentControl BindingHeaderedControl
        {
            get;
            set;
        } = null;

        public string CompletionName
        {
            get => CompletionNameBox.Text;
            set => CompletionNameBox.Text = value;
        }

        public string CompletionDescription
        {
            get => CompletionDescriptionBox.Text;
            set => CompletionDescriptionBox.Text = value;
        }

        public string CompletionIconPath
        {
            get => CompletionIconPathBox.Text;
            set => CompletionIconPathBox.Text = value;
        }

        public string CompletionContent
        {
            get => CompletionContentBox.Text;
            set => CompletionContentBox.Text = value;
        }

        private void CompletionNameBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (BindingHeaderedControl != null && sender is TextBox textbox)
                {
                    BindingHeaderedControl.Header = string.IsNullOrWhiteSpace(textbox.Text) ? "未命名补全项" : textbox.Text;
                }
            }
            catch { }
        }
    }
}
