using System;
using System.Collections.Generic;
using System.Text;
using Nini.Config;

namespace IdealistViewer
{
    public class IdealistViewerConfigSource
    {
        public IConfigSource Source;

        public void Save(string path)
        {
            if (Source is IniConfigSource)
            {
                IniConfigSource iniCon = (IniConfigSource)Source;
                iniCon.Save(path);
            }
        }
    }
}
