using System;
using System.Collections.Generic;
using System.Text;

namespace GraphLC_IDE.Functions
{
    class FileTreeProperty
    {
        public string Path
        {
            get => Stack[Position];
            set
            {
                ++Position;
                while (Position != Stack.Count)
                    Stack.RemoveAt(Stack.Count - 1);

                Stack.Add(value);
            }
        }

        public int Position = 0;
        public List<string> Stack = new List<string>();

        public FileTreeProperty()
        {
            Stack.Add("我的电脑");
        }
    }
}
