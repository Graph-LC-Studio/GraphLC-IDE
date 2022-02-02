using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using GraphLC_IDE.AppConfig;

namespace GraphLC_IDE.AppConfig
{
    class Log
    {
        public static bool WriteErr(string errMessage, string fileName = "None", int outMax = 10)
        {
            try
            {
                string dir = Path.Combine(AppInfo.Path, "log");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);


                string logfile = Path.Combine(dir, DateTime.Now.ToString("yyyy_MM_dd") + ".log");
                using (StreamWriter w = new StreamWriter(logfile, true, Encoding.UTF8))
                {
                    w.WriteLine("[Error]  " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss ") + fileName);
                    w.Write("   ");
                    StackTrace st = new StackTrace();

                    for (int i = 1; i < st.FrameCount && i <= outMax; ++i)
                        w.Write((i != 1 ? "," : " ") + st.GetFrame(i).GetMethod().Name);
                    w.WriteLine();
                    w.WriteLine("    " + errMessage + "\n");
                }

                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
