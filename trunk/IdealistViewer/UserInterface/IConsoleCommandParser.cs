using System;
using System.Collections.Generic;
using System.Text;

namespace IdealistViewer
{
    public interface IConsoleCommandParser
    {
        void OnConsoleCommand(string cmd, string[] cmdparams);
    }
}
