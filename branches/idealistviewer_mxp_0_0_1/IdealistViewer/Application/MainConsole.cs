using System;
using System.Collections.Generic;
using System.Text;

namespace IdealistViewer
{
    public class MainConsole
    {
        private static ConsoleBase instance;

        public static ConsoleBase Instance
        {
            get { return instance; }
            set { instance = value; }
        }
    }
}
