using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GraphLC_IDE.Functions
{
    public class DownloadFileFromSocket : IDisposable
    {
        private FileStream file = null;
        private Action<int, object> onWriteByte = null;
        public long TotalDownloadSize
        {
            get; private set;
        }

        public DownloadFileFromSocket(string filename, Action<int, object> writeByte = null)
        {
            onWriteByte = writeByte;
            TotalDownloadSize = 0;
            file = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        }
        ~DownloadFileFromSocket() => Dispose();

        public void Dispose()
        {
            file?.Close();
        }

        public void Handle(byte[] message)
        {
            file.Write(message, 0, message.Length);
            TotalDownloadSize += message.Length;

            if(onWriteByte != null)
                onWriteByte(message.Length, this);
        }

        public long Length
        {
            get
            {
                return file.Length;
            }
        }
    }
}
