using System;
using System.IO;
using System.Text;
using System.Windows;

using GraphLC_IDE.AppConfig;

using NChardet;

namespace GraphLC_IDE.Functions
{
    public class EncodingHelper
    {
        public Encoding Encode { get; private set; } = null;

        public string Name
        {
            get
            {
                if (Encode.WebName == "us-ascii")
                    return "ASCII";
                else if (Encode.WebName == "utf-8")
                    return Encode.Preamble.Length == 3 ? "UTF-8 with BOM" : "UTF-8";
                else if (Encode.WebName == "utf-16" || Encode.WebName == "utf-16le")
                    return "UTF-16";
                else if (Encode.WebName == "utf-16be")
                    return "UTF-16BE";
                else if (Encode.WebName == "gb18030" || Encode.WebName == "gb2312" || Encode.WebName == "gbk")
                    return "GB2312";
                else if (Encode.WebName == "shift_jis")
                    return "Shift-JIS";
                else if (Encode.WebName == "windows-1252")
                    return "Windows-1252";
                else
                    return String.Empty;
            }
        }

        public EncodingHelper(Encoding e)
        {
            Encode = e;
        }

        public EncodingHelper(string name)
        {
            name = name.ToLower().Replace("_", "-");
            if (name == "ascii")
                Encode = Encoding.ASCII;
            else if (name == "utf-8")
                Encode = new UTF8Encoding(false);
            else if (name == "utf-8 with bom")
                Encode = new UTF8Encoding(true);
            else if (name == "utf-16" || name == "utf-16le")
                Encode = Encoding.Unicode;
            else if (name == "gb18030" || name == "gb2312" || name == "gbk")
                Encode = Encoding.GetEncoding("GB2312");
            else if (name == "shift-jis")
                Encode = Encoding.GetEncoding("Shift-JIS");
            else if (name == "windows-1252")
                Encode = Encoding.GetEncoding("Windows-1252");
            else
                Encode = null;
        }
    }
}
