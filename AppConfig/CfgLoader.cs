using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphLC_IDE.AppConfig
{
    public class CfgLoader
    {
        private string fileName = "";

        public JToken Token
        {
            get;
        }

        public JObject Obj
        {
            get => Token as JObject;
        }

        public CfgLoader(string srcPath, bool createFile = false)
        {
            try
            {
                fileName = srcPath;

                if (createFile && !File.Exists(fileName))
                    File.Create(fileName).Close();

                using (StreamReader r = new StreamReader(fileName, Encoding.UTF8))
                {
                    string s = r.ReadToEnd();
                    Token = (JToken)JsonConvert.DeserializeObject(s);
                }
            }
            catch (Exception e)
            {
                Log.WriteErr(e.Message, "CfgLoader.cs");
            }
        }

        public JToken this[object key]
        {
            get
            {
                try
                {
                    return Token[key];
                }
                catch (Exception e)
                {
                    Log.WriteErr(e.Message, "CfgLoader.cs");
                    return null;
                }
            }
            set
            {
                try
                {
                    Token[key] = value;
                }
                catch (Exception e)
                {
                    Log.WriteErr(e.Message, "CfgLoader.cs");
                }
            }
        }

        public bool Save()
        {
            if (fileName == "" || fileName == string.Empty)
                return false;

            try
            {
                using (StreamWriter w = new StreamWriter(fileName, false, Encoding.UTF8))
                    w.Write(Token.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteErr(ex.Message, "CfgLoader.cs");
                return false;
            }
        }
    }
}
