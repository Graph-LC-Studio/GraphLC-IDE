using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GraphLC_IDE.AppConfig;

using Newtonsoft.Json.Linq;

namespace GraphLC_IDE.Functions
{
    class JsonHelper
    {
        public static void Add(JObject obj, string key, JToken token)
        {
            if (obj.ContainsKey(key))
                obj[key] = token;
            else
                obj.Add(key, token);
        }
    
        public static void Add(CfgLoader obj, string key, JToken token)
        {
            if (obj.Obj.ContainsKey(key))
                obj[key] = token;
            else
                obj.Obj.Add(key, token);
        }

        public static bool Remove(JObject obj, string key) => obj.Remove(key);

        public static bool Remove(CfgLoader obj, string key) => obj.Obj.Remove(key);
    }
}
