using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace GraphLC_IDE.Extensions
{
    internal static class StringExtensions
    {
        public static string[] ESplit(this string text, char split = ' ')
        {
            text += split;
            List<string> list = new List<string>();
            int len = text.Length, subpos = 0;
            bool special = true, vaild = false;
            for (int i = 0; i < len; ++i)
            {
                if (text[i] == '\"' && (i == 0 || text[i - 1] != '\\'))
                    special = !special;
                else if (text[i] == split)
                {
                    if (vaild && special)
                    {
                        list.Add(text.Substring(subpos, i - subpos).Replace("\"", ""));
                    }
                    if (special) subpos = i + 1;
                    vaild = false;
                }
                else vaild = true;
            }
            return list.ToArray();
        }

        public static object GetObjectFromFramewrokElement(this string objName, FrameworkElement element)
        {
            var array = objName.ESplit('.');
            object obj = element.FindName(array[0]);
            for (int i = 1; i < array.Length; ++i)
                obj = obj.GetType().GetProperty(array[i]).GetValue(obj, null);
            return obj;
        }

        /// <summary>
        /// 如果字符串包含空格，返回加了引号的字符串
        /// </summary>
        public static string MakeCommand(this string text)
        {
            return text.Contains(' ') ? $"\"{text}\"" : text;
        }
    }
}
