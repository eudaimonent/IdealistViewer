using System;
using System.Collections.Generic;
using System.Text;

namespace IdealistViewer.Module
{
    public class ModuleManager
    {
        public static IProtocol GetProtocolModule(IdealistViewerConfigSource configSource)
        {
            string protocol=configSource.Source.Configs["Startup"].GetString("protocol","sl");
            if (protocol == "sl")
            {
                return new SecondlifeProtocol();
            }
            else
            {
                return new MetaverseExchangeProtocol();
            }
        }
    }
}
