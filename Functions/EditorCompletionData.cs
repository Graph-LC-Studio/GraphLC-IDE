using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace GraphLC_IDE.Functions
{
    public class EditorCompletionData : ICompletionData
    {
        public EditorCompletionData(string content, string text, object description, ImageSource imgSource = null, double priority = 0)
        {
            Content = content;
            CompletedText = text;
            Description = description;
            Image = imgSource;
            Priority = priority;
        }

        public ImageSource Image
        {
            get;
            private set;
        }

        public object Content // 条目显示的内容
        {
            get;
            private set;
        }

        public string Text // 解决排序问题
        {
            get => Content.ToString();
        }

        public string CompletedText // 补全的内容
        {
            get;
            private set;
        }

        public object Description
        {
            get;
            private set;
        }

        public double Priority
        {
            get;
            private set;
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.CompletedText);
        }

        public object Clone() => MemberwiseClone();
    }
}
