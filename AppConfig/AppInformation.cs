using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using GraphLC_IDE.Functions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphLC_IDE.AppConfig
{
    class AppInfo
    {
        public static string Path = AppDomain.CurrentDomain.BaseDirectory;
        public static string[] Args = null;
        public static string Version = "5.0.0";
        public static CfgLoader Config = null;
        public static CfgLoader Theme = null;
        public static CfgLoader MainWindowProperty = null;
        public static CfgLoader RecentFiles = null; //最近打开的项
        public static string ModuleFilter = "";
        public static Dictionary<string, string> ReflexToModule = new Dictionary<string, string>();
        public static Dictionary<string, ModuleInfo> Modules = new Dictionary<string, ModuleInfo>();
        public static List<EditorCompletionData> CustomizeTipsList = new List<EditorCompletionData>();
    }
}
