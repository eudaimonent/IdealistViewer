using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Text;
using log4net;
using log4net.Config;
using Nini.Config;



namespace IdealistViewer
{
    class IdealistViewer 
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            
            
            ArgvConfigSource configSource = new ArgvConfigSource(args);
            configSource.Alias.AddAlias("On", true);
            configSource.Alias.AddAlias("Off", false);
            configSource.Alias.AddAlias("True", true);
            configSource.Alias.AddAlias("False", false);

            BaseIdealistViewer IV = new BaseIdealistViewer(configSource);
            IV.Startup();
            while (true)
            {
                MainConsole.Instance.Prompt();
            }
        }
    }
}
