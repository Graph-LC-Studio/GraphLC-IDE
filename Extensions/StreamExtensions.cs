using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GraphLC_IDE.Functions;
using NChardet;

namespace GraphLC_IDE.Extensions
{
    public class MyCharsetDetectionObserver : NChardet.ICharsetDetectionObserver
    {
        public string Charset = null;

        public void Notify(string charset)
        {
            Charset = charset;
        }
    }

    internal static class StreamExtensions
    {
        public static Encoding GetEncoding(this Stream stream)
        {
            var det = new Detector();
            var cdo = new MyCharsetDetectionObserver();
            det.Init(cdo);

            var done = false;
            var isAscii = true;
            var buf = new byte[4 * 1024];
            var len = stream.Read(buf, 0, buf.Length);

            // 探测是否为Ascii编码
            if (isAscii)
                isAscii = det.isAscii(buf, len);

            // 如果不是Ascii编码，并且编码未确定，则继续探测
            if (!isAscii && !done)
                done = det.DoIt(buf, len, false);

            det.DataEnd();

            if (isAscii)
                return Encoding.ASCII;
            if (cdo.Charset != null)
                return Encoding.GetEncoding(cdo.Charset);

            foreach (var iter in det.getProbableCharsets())
            {
                var e = new EncodingHelper(iter).Encode;
                if (e != null)
                    return e;
            }

            return null;
        }
    }
}
