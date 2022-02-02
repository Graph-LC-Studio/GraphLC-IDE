using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GraphLC_IDE.Extensions
{
    internal static class GEditorExtensions
    {
        public static void ESave(this GEditor.CodeEditor editor, Dispatcher dispatcher = null)
        {
            editor.Save();

            Action action = () =>
            {
                if (editor.Tag is Functions.EditorInfo tag)
                    tag.BindingTabItem.Header = new FileInfo(editor.FileName).Name.Replace("_", "__").Replace("&", "&&");
            };

            if (dispatcher == null)
                action();
            else
                dispatcher.Invoke(action);
        }
    }
}