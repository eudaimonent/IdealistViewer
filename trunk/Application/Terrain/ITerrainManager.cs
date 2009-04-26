using System;
namespace IdealistViewer
{
    public interface ITerrainManager
    {
        void OnNetworkLandUpdate(IdealistViewer.VSimulator sim, int x, int y, int width, float[] data);
        void OnSimulatorConnected(IdealistViewer.VSimulator sim);
        void Process();
    }
}
