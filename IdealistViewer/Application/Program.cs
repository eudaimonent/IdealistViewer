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
    // Main program class which is first executed when IdealistViewer.exe is started.
    public class Program 
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            
            
            ArgvConfigSource configSource = new ArgvConfigSource(args);
            configSource.Alias.AddAlias("On", true);
            configSource.Alias.AddAlias("Off", false);
            configSource.Alias.AddAlias("True", true);
            configSource.Alias.AddAlias("False", false);

            Viewer IV = new Viewer(configSource);
            IV.Startup();
            while (true)
            {
                if (MainConsole.Instance != null)
                {
                    MainConsole.Instance.Prompt();
                    Thread.Sleep(100);
                }
            }
        }

    }
}
