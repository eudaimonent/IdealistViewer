using System;
using System.Collections.Generic;
using System.Text;

namespace IdealistViewer
{
    public class MainConsole
    {
        private static Console instance;

        public static Console Instance
        {
            get { return instance; }
            set { instance = value; }
        }
    }
}
