using System;
using System.Collections.Generic;
using System.Text;

namespace IdealistViewer.Modules
{
    public class ModuleManager
    {
        public static IProtocol GetProtocolModule()
        {
            return new SLProtocol();
        }
    }
}
