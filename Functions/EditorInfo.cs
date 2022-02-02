using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GEditor;
using GraphLC_IDE.AppConfig;

namespace GraphLC_IDE.Functions
{
    internal class EditorInfo
    {
        public TabItem BindingTabItem { get; } = null;

        /// <summary>
        /// 评测器
        /// </summary>
        public int JudgeTimeout = 1000;
        public List<Tuple<string, string>> JudgePoints = new List<Tuple<string, string>>();

        public bool IsAutoCompile
        {
            get;
            set;
        } = true;

        public ModuleInfo Module { get; } = null;

        public EditorInfo(string file, TabItem bindingTabItem)
        {
            var suffix = new FileInfo(file).Extension;
            if (AppInfo.ReflexToModule.ContainsKey(suffix))
                Module = AppInfo.Modules[AppInfo.ReflexToModule[suffix]];

            BindingTabItem = bindingTabItem;
        }
    }
}
