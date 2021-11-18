using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Input;

using GlcEditorPlugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CppEditorPlugin
{
    public class Class : IEditorPlugin
    {
        private JObject config = null;

        private bool MoveSemBack = false;
        private int ReplaceSpaces = 0;
        private bool AutoComplete = false;
        private bool AutoDelete = false;
        private bool AutoDeleteInterbank = false;
        private bool WrapBraces = false;
        private bool WrapBracesNewLine = false;

        // 模块插件被加载时触发
        // 加载配置文件
        public void PluginLoaded()
        {
            // 读取配置文件
            using (StreamReader r = new StreamReader(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.txt"), Encoding.UTF8))
                config = (JObject)JsonConvert.DeserializeObject(r.ReadToEnd());

            MoveSemBack = (bool)(config[nameof(MoveSemBack)] ?? false);
            ReplaceSpaces = (int)(config[nameof(ReplaceSpaces)] ?? 0);
            AutoComplete = (bool)(config[nameof(AutoComplete)] ?? false);
            AutoDelete = (bool)(config[nameof(AutoDelete)] ?? false);
            AutoDeleteInterbank = (bool)(config[nameof(AutoDeleteInterbank)] ?? false);
            WrapBraces = (bool)(config[nameof(WrapBraces)] ?? false);
            WrapBracesNewLine = (bool)(config[nameof(WrapBracesNewLine)] ?? false);
        }

        public void Dispose() { }

        public PluginInfo GetPluginInfo() => new PluginInfo("CppEditorPlugin", "2.0.0.0", "Return", "括号补全");

        // 自定义补全列表
        public List<EditorCompletionData> CompletionList => new List<EditorCompletionData>();

        private Tuple<char, char>[] pair = new Tuple<char, char>[6] {
            new Tuple<char, char>('(',')'),
            new Tuple<char, char>('{', '}'),
            new Tuple<char, char>('[', ']'),
            new Tuple<char, char>('<', '>'),
            new Tuple<char, char>('\"', '\"'),
            new Tuple<char, char>('\'', '\'')};

        /// <summary>
        /// 用户按下按键时触发
        /// </summary>
        /// <param name="sender">传入的编辑器</param>
        /// <param name="e">按键参数</param>
        public void EditorKeyDown(EditorProperty sender, KeyEventArgs e)
        {
            var editor = sender.Editor;
            if (editor.SelectionLength == 0)
            {
                // 分号后移 (Remove)
                if (MoveSemBack)
                    if (e.Key == Key.Oem1 && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                    {
                        editor.BeginChange();
                        int p = editor.CaretOffset;
                        while (p < editor.Document.TextLength - 1 && editor.Text[p] == ')') ++p;
                        editor.Document.Insert(editor.CaretOffset = p, ";");
                        editor.EndChange();
                        e.Handled = true;
                    }

                // 将 Tab 替换成空格
                if (ReplaceSpaces > 0)
                    if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift) && !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        if (e.Key == Key.Tab) // 拦截 Tab
                        {
                            editor.BeginChange();
                            editor.Document.Insert(editor.CaretOffset, new string(' ', ReplaceSpaces));
                            editor.EndChange();
                            e.Handled = true;
                        }
                    }

                // 自动补全
                if (AutoComplete)
                {
                    // ()
                    if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && e.Key == Key.D9)
                    {
                        editor.BeginChange();
                        editor.Document.Insert(editor.CaretOffset, "()");
                        editor.CaretOffset--;
                        e.Handled = true;
                        editor.EndChange();
                    }
                    // {}
                    if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && e.Key == Key.Oem4)
                    {
                        editor.BeginChange();
                        editor.Document.Insert(editor.CaretOffset, "{}");
                        editor.CaretOffset--;
                        editor.EndChange();
                        e.Handled = true;
                    }
                    // []
                    if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift) && e.Key == Key.Oem4)
                    {
                        editor.BeginChange();
                        editor.Document.Insert(editor.CaretOffset, "[]");
                        editor.CaretOffset--;
                        editor.EndChange();
                        e.Handled = true;
                    }
                    // ""
                    if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && e.Key == Key.Oem7)
                    {
                        editor.BeginChange();
                        editor.Document.Insert(editor.CaretOffset, "\"\"");
                        editor.CaretOffset--;
                        editor.EndChange();
                        e.Handled = true;
                    }
                    // ''
                    if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift) && e.Key == Key.Oem7)
                    {
                        editor.BeginChange();
                        editor.Document.Insert(editor.CaretOffset, "\'\'");
                        editor.CaretOffset--;
                        editor.EndChange();
                        e.Handled = true;
                    }
                }

                // 自动删除
                if (AutoDelete)
                    if (e.Key == Key.Back && editor.CaretOffset > 0 && editor.CaretOffset <= editor.Document.TextLength - 1)
                    {
                        bool check(char ch) => ch == '\r' || ch == '\n' || ch == '\t' || ch == ' ';
                        int csuf = editor.CaretOffset, len = editor.Document.TextLength;

                        // 跨行删除
                        if (AutoDeleteInterbank)
                            while (csuf < len - 1 && check(editor.Text[csuf]))
                                ++csuf;

                        char pre = editor.Text[editor.CaretOffset - 1], suf = editor.Text[csuf];
                        foreach (var iter in pair)
                        {
                            if (pre == iter.Item1 && suf == iter.Item2)
                            {
                                editor.BeginChange();
                                editor.Document.Remove(editor.CaretOffset, csuf - editor.CaretOffset + 1);
                                editor.EndChange();
                            }
                        }
                    }

                // 大括号换行
                if (WrapBraces)
                    if (e.Key == Key.Enter && editor.CaretOffset <= editor.Document.TextLength - 1)
                    {
                        int cpre = editor.CaretOffset - 1, csuf = editor.CaretOffset, len = editor.Document.TextLength;

                        // 性能优化，最多扫描光标前后64个字符
                        while (Math.Abs(editor.CaretOffset - cpre) < 64 && cpre > 0 && editor.Text[cpre] == ' ') --cpre;
                        while (Math.Abs(editor.CaretOffset - csuf) < 64 && csuf < len - 1 && editor.Text[csuf] == ' ') ++csuf;

                        char pre = editor.Text[cpre], suf = csuf <= len - 1 ? editor.Text[csuf] : (char)0;
                        if (pre == '{')
                        {
                            int tablen = 0, ctab;
                            var loc = editor.Document.GetLocation(editor.CaretOffset);
                            ctab = editor.Document.Lines[loc.Line - 1].Offset;

                            while (true)
                            {
                                if (editor.Text[ctab] == ' ')
                                {
                                    ++ctab;
                                    ++tablen;
                                }
                                else if (editor.Text[ctab] == '\t')
                                {
                                    ++ctab;
                                    tablen += 4;
                                }
                                else break;
                            }

                            string tab = "";
                            while (tablen >= 4) { tab += '\t'; tablen -= 4; }
                            if (tablen > 0) tab += new string(' ', tablen);

                            editor.BeginChange();
                            if (suf == '}') // { |<Enter }
                            {
                                // 在 '{' 前面插入换行
                                if (WrapBracesNewLine && editor.Text[ctab] != '{')
                                    editor.Document.Insert(cpre, "\r\n" + tab); 
                                editor.Document.Insert(editor.CaretOffset, "\r\n" + tab + "\t\n" + tab);
                                editor.CaretOffset -= tab.Length + 1;
                            }
                            else // { |<Enter
                            {
                                editor.Document.Insert(editor.CaretOffset, "\r\n" + tab + "\t");
                            }
                            editor.EndChange();

                            e.Handled = true;
                        }
                    }
            }
        }

        public void EditorKeyUp(EditorProperty sender, KeyEventArgs e) { }

        public void EditorTextEnter(EditorProperty sender, TextCompositionEventArgs e) { }

        public void EditorTextChanged(EditorProperty sender, EventArgs e) { }

        public void EditorMouseEnter(EditorProperty sender, MouseEventArgs e) { }

        public void EditorMouseHover(EditorProperty sender, MouseEventArgs e) { }

        public void EditorMouseDown(EditorProperty sender, MouseButtonEventArgs e) { }

        public void EditorMouseUp(EditorProperty sender, MouseButtonEventArgs e) { }
    }
}
