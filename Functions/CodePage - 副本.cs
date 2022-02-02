using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.Windows.Media;
using System.Xml;

using GraphLC_IDE.AppConfig;

using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Windows.Controls.Primitives;
using MahApps.Metro.Controls;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Windows.Input;
using System.Windows.Controls;

namespace GraphLC_IDE.Functions
{
    public enum CodeEditorState
    {
        Normal, Lock, Compile
    }

    class CodePage
    {
        /// <summary>
        /// 代码编辑器状态
        /// </summary>
        public CodeEditorState State { get; set; } = CodeEditorState.Normal;

        /// <summary>
        /// 代码编辑器
        /// </summary>
        public ICSharpCode.AvalonEdit.TextEditor CodeArea { get; } = null;

        /// <summary>
        /// 文件属性
        /// </summary>
        public FileInformation FilePorperty { get; } = null;
        /// <summary>
        /// 绑定的状态栏
        /// </summary>
        public StatusBarItem BindedStatus { get; } = null;
        /// <summary>
        /// 文件编码
        /// </summary>
        public Encoding Encode { get; set; } = null;

        
        /// <summary>
        /// 代码折叠
        /// </summary>
        public FoldingManager FoldingManager = null;
        public XmlFoldingStrategy FoldingStrategy = new XmlFoldingStrategy();

        /// <summary>
        /// 代码补全窗口
        /// </summary>
        public CompletionWindow CompletionTips = null;

        /// <summary>
        /// 评测器
        /// </summary>
        public int CompareTimeout = 1000;
        public List<Tuple<string, string>> CompareList = new List<Tuple<string, string>>();

        private bool isSaved = true;
        public bool IsSaved
        {
            get => isSaved;
            set
            {
                isSaved = value;
                if (!value)
                    IsAutoCompile = true;
                try
                {
                    ((MetroTabItem)CodeArea.Parent).Header = FilePorperty.FileNameSuffix.Replace("_", "__").Replace("&", "&&") + (value ? "" : "*");
                }
                catch (Exception) { }
            }
        }

        private bool canSave = true;
        public bool CanSave
        {
            get => canSave;
            set => isSaved = canSave = value;
        }

        private bool isAutoCompile = true;
        public bool IsAutoCompile
        {
            get => isAutoCompile;
            set => isAutoCompile = value;
        }

        /// <summary>
        /// 模块
        /// </summary>
        public ModuleInformation Module = null;

        public void HandleStatusContent()
        {
            try
            {
                var loc = CodeArea.Document.GetLocation(CodeArea.CaretOffset);
                BindedStatus.Content = string.Format("行: {0}    列: {1}    字符: {2}", loc.Line, loc.Column, CodeArea.Document.TextLength);
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "CodePage.cs");
                BindedStatus.Content = "";
            }
        }

        public CodePage(string srcPath, ContextMenu menu, StatusBarItem status, Encoding encode)
        {
            BindedStatus = status;
            FilePorperty = new FileInformation(srcPath);

            CodeArea = new ICSharpCode.AvalonEdit.TextEditor()
            {
                Name = "CodeArea",
                AllowDrop = false,
                ContextMenu = menu,
                Focusable = true,
                ShowLineNumbers = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,

                WordWrap = (bool)AppInformation.config["ide"]["settings"]["editor"]["wordwrap"],
                Foreground = new SolidColorBrush(Color.FromArgb(
                            (byte)AppInformation.config["ide"]["settings"]["editor"]["fore"][0],
                            (byte)AppInformation.config["ide"]["settings"]["editor"]["fore"][1],
                            (byte)AppInformation.config["ide"]["settings"]["editor"]["fore"][2],
                            (byte)AppInformation.config["ide"]["settings"]["editor"]["fore"][3])),
                LineNumbersForeground = new SolidColorBrush(Color.FromArgb(
                            (byte)AppInformation.config["ide"]["settings"]["editor"]["linefore"][0],
                            (byte)AppInformation.config["ide"]["settings"]["editor"]["linefore"][1],
                            (byte)AppInformation.config["ide"]["settings"]["editor"]["linefore"][2],
                            (byte)AppInformation.config["ide"]["settings"]["editor"]["linefore"][3])),
                FontFamily = new System.Windows.Media.FontFamily(AppInformation.config["ide"]["settings"]["editor"]["font"]["name"].ToString()),
                FontSize = (double)AppInformation.config["ide"]["settings"]["editor"]["font"]["size"],
                FontWeight = AppInformation.config["ide"]["settings"]["editor"]["font"]["weight"].ToString() == "bold" ?
                        System.Windows.FontWeights.Bold :
                        AppInformation.config["ide"]["settings"]["editor"]["font"]["weight"].ToString() == "light" ? System.Windows.FontWeights.Light : System.Windows.FontWeights.Normal
            };

            // 背景
            string image = AppInformation.config["ide"]["settings"]["editor"]["back"]["image"].ToString().Replace("$path", AppInformation.Path);
            if (File.Exists(image))
            {
                ImageBrush brush = new ImageBrush();
                brush.ImageSource = Helper.GetBitmapImage(image);
                brush.Opacity = (double)AppInformation.config["ide"]["settings"]["editor"]["back"]["opacity"];
                switch (AppInformation.config["ide"]["settings"]["editor"]["back"]["stretch"].ToString())
                {
                    case "none":
                        brush.Stretch = Stretch.None;
                        break;
                    case "fill":
                        brush.Stretch = Stretch.Fill;
                        break;
                    case "uniform":
                        brush.Stretch = Stretch.Uniform;
                        break;
                    case "uniformtofill":
                        brush.Stretch = Stretch.UniformToFill;
                        break;
                    default:
                        brush.Stretch = Stretch.None;
                        break;
                }
                CodeArea.Background = brush;
            }

            // 插入折叠
            FoldingManager = FoldingManager.Install(CodeArea.TextArea);
            FoldingStrategy.UpdateFoldings(FoldingManager, CodeArea.Document);

            // 高亮
            if (AppInformation.ReflexToModule.ContainsKey(FilePorperty.Suffix))
            {
                var m = AppInformation.ReflexToModule[FilePorperty.Suffix];
                using (StreamReader s = new StreamReader(System.IO.Path.Combine(AppInformation.Path, "Config", "Module", m, AppInformation.module[m].Highlight), Encoding.UTF8))
                {
                    using (XmlTextReader reader = new XmlTextReader(s))
                    {
                        CodeArea.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }

            if (encode != null) CodeArea.Encoding = Encode = encode;
            CodeArea.Load(new FileStream(srcPath, FileMode.Open, FileAccess.Read));
            if (encode == null) Encode = CodeArea.Encoding;

            CodeArea.PreviewKeyDown += CodeArea_PreviewKeyDown;
            CodeArea.PreviewKeyUp += CodeArea_PreviewKeyUp;
            CodeArea.TextArea.TextEntering += TextArea_TextEntering;
            CodeArea.TextChanged += CodeArea_TextChanged;
            CodeArea.PreviewMouseUp += CodeArea_PreviewMouseUp;
            CodeArea.PreviewMouseDown += CodeArea_PreviewMouseDown;
            CodeArea.MouseEnter += CodeArea_MouseEnter;
            CodeArea.MouseHover += CodeArea_MouseHover;
            CodeArea.PreviewMouseWheel += CodeArea_MouseWheel;

            try
            {
                if (AppInformation.module.ContainsKey(FilePorperty.ModuleName))
                    Module = AppInformation.module[FilePorperty.ModuleName];
            }
            catch { }
        }

        private void CodeArea_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (CompletionTips != null)
                {
                    if (e.Key == Key.Tab && (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift)))
                    {
                        CompletionTips.Close();
                        e.Handled = true;
                    }
                    else if((e.Key == Key.Up || e.Key == Key.Down) && CompletionTips.CompletionList.ListBox.Items.Count == 0)
                    {
                        CompletionTips.Close();
                    }
                }

                Module?.EditorPlugin.EditorKeyDown(new GlcEditorPlugin.EditorProperty(CodeArea, FoldingManager), e);
            }
            catch { }
        }

        private void CodeArea_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                HandleStatusContent();

                Module?.EditorPlugin.EditorKeyUp(new GlcEditorPlugin.EditorProperty(CodeArea, FoldingManager), e);
            }
            catch { }
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            try
            {
                bool CheckSymbol(char c) => char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c);
                bool Check(char c) => CheckSymbol(c) || char.IsDigit(c);

                try
                {
                    if ((AppInformation.module[FilePorperty.ModuleName]?.SupportCompletion).GetValueOrDefault())
                    {
                        // 检查光标位置前一个字符是否为空字符或标点符号
                        bool y = true;
                        if (CodeArea.CaretOffset > 0)
                            y = Check(CodeArea.Text[CodeArea.CaretOffset - 1]);
                        if (!Check(e.Text[0]) && y)
                        {
                            // 新建一个提示框
                            CompletionTips = new CompletionWindow(CodeArea.TextArea);
                            // 初始化样式
                            CompletionTips.Foreground = new SolidColorBrush(Color.FromArgb((byte)AppInformation.theme["window"]["fore"][0],
                                                                                           (byte)AppInformation.theme["window"]["fore"][1],
                                                                                           (byte)AppInformation.theme["window"]["fore"][2],
                                                                                           (byte)AppInformation.theme["window"]["fore"][3]));
                            CompletionTips.Background = new SolidColorBrush(Color.FromArgb((byte)AppInformation.theme["window"]["back"][0],
                                                                                           (byte)AppInformation.theme["window"]["back"][1],
                                                                                           (byte)AppInformation.theme["window"]["back"][2],
                                                                                           (byte)AppInformation.theme["window"]["back"][3]));

                            // 临时列表
                            var list = new List<EditorCompletionData>();
                            // 合并 模块补全列表 和 用户定义补全列表
                            foreach (var iter in AppInformation.module[FilePorperty.ModuleName].Completion)
                            {
                                list.Add(iter);
                            }
                            foreach (var iter in AppInformation.CustomizeTipsList)
                                list.Add(iter);

                            // 排序
                            /*list.Sort((EditorCompletionData x, EditorCompletionData y) =>
                            {
                                return x.Content.ToString().CompareTo(y.Content.ToString());
                            });*/

                            foreach (var iter in list)
                                CompletionTips.CompletionList.CompletionData.Add(iter.Clone() as EditorCompletionData);

                            CompletionTips.Show();
                            CompletionTips.Closed += (sender, e) => CompletionTips = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "CodePage.cs");
                }

                try
                {
                    if (CompletionTips != null)
                    {
                        if ((AppInformation.module[FilePorperty.ModuleName]?.SupportCompletion).GetValueOrDefault() && e.Text.Length > 0 && CheckSymbol(e.Text[0]))
                        {
                            bool y = false;
                            foreach (string iter in AppInformation.module[FilePorperty.ModuleName].InsertCompletionKey)
                                if (e.Text == iter)
                                {
                                    y = true;
                                    break;
                                }
                            if (y)
                                CompletionTips.CompletionList.RequestInsertion(e);
                            else
                                CompletionTips.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteErr(ex.Message, "CodePage.cs");
                }
                Module?.EditorPlugin.EditorTextEnter(new GlcEditorPlugin.EditorProperty(CodeArea, FoldingManager), e);
            }
            catch { }
        }

        private void CodeArea_TextChanged(object? sender, EventArgs e)
        {
            try
            {
                HandleStatusContent();
                IsSaved = false;

                Module?.EditorPlugin.EditorTextChanged(new GlcEditorPlugin.EditorProperty(CodeArea, FoldingManager), e);
            }
            catch { }
        }

        private void CodeArea_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Module?.EditorPlugin.EditorMouseDown(new GlcEditorPlugin.EditorProperty(CodeArea, FoldingManager), e);
            }
            catch { }
        }

        private void CodeArea_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                HandleStatusContent();

                Module?.EditorPlugin.EditorMouseUp(new GlcEditorPlugin.EditorProperty(CodeArea, FoldingManager), e);
            }
            catch { }
        }

        private void CodeArea_MouseHover(object sender, MouseEventArgs e)
        {
            try
            {
                Module?.EditorPlugin.EditorMouseHover(new GlcEditorPlugin.EditorProperty(CodeArea, FoldingManager), e);
            }
            catch { }
        }

        private void CodeArea_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                Module?.EditorPlugin.EditorMouseEnter(new GlcEditorPlugin.EditorProperty(CodeArea, FoldingManager), e);
            }
            catch { }
        }

        private void CodeArea_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    CodeArea.FontSize += e.Delta / 50;
                }
            }
            catch { }
        }
    }
}
