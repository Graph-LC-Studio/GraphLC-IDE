using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using GraphLC_IDE.AppConfig;

namespace GraphLC_IDE.Functions
{
    class FileInformation
    {
        public string Suffix { get; } = "";

        public string ModuleName { get; } = "";

        public string FileNameSuffix { get; } = "";

        public string FileName { get; } = "";

        public FileInformation(string srcPath)
        {
            try
            {
                FileName = srcPath;
                //此处未适配 MacOS ，Linux已适配
                FileNameSuffix = srcPath.Substring(srcPath.LastIndexOf(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/') + 1);
                Suffix = srcPath.Substring(srcPath.LastIndexOf('.'));
                if (AppInfo.ReflexToModule.ContainsKey(Suffix))
                    ModuleName = AppInfo.ReflexToModule[Suffix];
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "FileInformation.cs");
            }
        }
    }
}
