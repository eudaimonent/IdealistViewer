using System;
using System.Collections.Generic;
using System.Text;
using IdealistViewer.Network;

namespace IdealistViewer
{
    public class ModuleManager
    {
        public static INetworkInterface GetNetworkModule(string protocol)
        {
            if (protocol == "mxp")
            {
                return new MxpNetworkModule(); 
            }
            else
            {
                return new LibOmvNetworkModule();
            }
        }

        public static ITerrainManager GetTerrainManager(Viewer viewer,IdealistViewerConfigSource configSource)
        {
            string protocol = configSource.Source.Configs["Startup"].GetString("terrain", "mesh");
            if (protocol == "mesh")
            {
                return new MeshTerrainModule(viewer);
            }
            else
            {
                return new IrrlichtTerrainModule(viewer);
            }
        }

    }
}
