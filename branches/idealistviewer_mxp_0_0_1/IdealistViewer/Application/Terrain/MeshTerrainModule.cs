using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using OpenMetaverse;
using log4net;
using System.Reflection;

namespace IdealistViewer
{
    /// <summary>
    /// Class containing algorithms, data and render state for terrain.
    /// </summary>
    public class MeshTerrainModule : ITerrainManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Reference to the viewer instance.
        /// </summary>
        private Viewer m_viewer;

        /// <summary>
        /// Queue of terrain that needs to be updated.  ulong region handle.
        /// </summary>
        private Queue<ulong> m_terrainModifications = new Queue<ulong>();
        /// <summary>
        /// Simulator patch storage Indexed by regionhandle  Contains a dictionary with the patch number as an index.
        /// </summary>
        private Dictionary<ulong, Dictionary<int, float[]>> m_simulatorHeightFields = new Dictionary<ulong, Dictionary<int, float[]>>();        
        /// <summary>
        /// TerrainSceneNode Irrlicht representations of the terrain.  Indexed by regionhandle
        /// </summary>
        private IDictionary<ulong, IList<SceneNode>> m_terrainNodes = new Dictionary<ulong, IList<SceneNode>>();
        /// <summary>
        /// Rendered height field.
        /// </summary>
        private float[,] m_heightField = new float[256, 256];
        /// <summary>
        /// Texture used for rendering the terrain.
        /// </summary>
        private Texture m_greenGrassTexture = null;

        public MeshTerrainModule(Viewer viewer)
        {
            m_viewer = viewer;

            m_greenGrassTexture = m_viewer.Renderer.Driver.GetTexture("Green_Grass_Detailed.tga");
        }

        public void Process()
        {
            if (m_terrainModifications.Count > 0)
            {
                m_terrainModifications.Clear();

                int x, y;
                int x1, y1;

                // size of each plane mesh in the grid in meters
                int xStep = 16;
                int yStep = 16;

                if (!m_terrainNodes.ContainsKey(m_viewer.SceneGraph.CurrentSimulator.Handle))
                {
                    m_terrainNodes.Add(m_viewer.SceneGraph.CurrentSimulator.Handle, new List<SceneNode>());
                }

                IList<SceneNode> sceneNodes = m_terrainNodes[m_viewer.SceneGraph.CurrentSimulator.Handle];

                foreach (SceneNode sceneNode in sceneNodes)
                {
                    m_viewer.Renderer.SceneManager.AddToDeletionQueue(sceneNode);
                }

                sceneNodes.Clear();
                //smgr.AddToDeletionQueue(

                for (y = 0; y < 256; y += yStep)
                {
                    y1 = y + yStep < 256 ? y + yStep : 255;

                    for (x = 0; x < 256; x += xStep)
                    {
                        x1 = x + xStep < 256 ? x + xStep : 255;
                        float[,] heights = GetSubHeightField(x, x1, y, y1);

                        // Calculating average height
                        float averageHeight = 0;
                        for (int i = 0; i < heights.GetLength(0); i++)
                        {
                            for (int j = 0; j < heights.GetLength(1); j++)
                            {
                                averageHeight += heights[i, j];

                            }
                        }
                        averageHeight = averageHeight / heights.Length;

                        // Canceling average height from mesh.
                        for (int i = 0; i < heights.GetLength(0); i++)
                        {
                            for (int j = 0; j < heights.GetLength(1); j++)
                            {
                                heights[i, j] -= averageHeight;
                            }
                        }

                        Mesh sampleHF = PrimMesherG.SculptIrrMesh(heights, -8, 8, -8, 8);
                        for (int i = 0; i < sampleHF.MeshBufferCount; i++)
                            sampleHF.GetMeshBuffer(i).SetColor(new Color(128, 32, 32, 32));

                        SceneNode sampleHFNode = m_viewer.Renderer.SceneManager.AddMeshSceneNode(sampleHF, m_viewer.Renderer.SceneManager.RootSceneNode, -1);
                        sampleHFNode.Position = new Vector3D(x + 8.5f, averageHeight, y + 8.5f);
                        sampleHFNode.SetMaterialTexture(0, m_greenGrassTexture);
                        sampleHFNode.SetMaterialFlag(MaterialFlag.Lighting, true);
                        sampleHFNode.SetMaterialFlag(MaterialFlag.GouraudShading, false);
                        sampleHFNode.SetMaterialFlag(MaterialFlag.BackFaceCulling, m_viewer.BackFaceCulling);
                        sampleHFNode.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                        sceneNodes.Add(sampleHFNode);
                    }
                }
            }
        }

        public void OnSimulatorConnected(VSimulator sim)
        {
            if (!m_simulatorHeightFields.ContainsKey(sim.Handle))
            {
                Dictionary<int, float[]> m_landMap = new Dictionary<int, float[]>();
                m_simulatorHeightFields.Add(sim.Handle, m_landMap);
            }

            /*lock (m_terrainModifications)
            {
                if (!m_terrainModifications.Contains(sim.Handle))
                {
                    m_terrainModifications.Enqueue(sim.Handle);
                }
            }*/
        }

        public void OnNetworkLandUpdate(VSimulator sim, int x, int y, int width, float[] data)
        {
            ulong simhandle = sim.Handle;

            if (simhandle == 0)
                simhandle = m_viewer.TestNeighbor;

            if (x < 0 || x > 15 || y < 0 || y > 15)
            {
                m_log.WarnFormat("Invalid land patch ({0}, {1}) received from server", x, y);
                return;
            }

            if (width != 16)
            {
                m_log.WarnFormat("Unsupported land patch width ({0}) received from server", width);
                return;
            }

            Dictionary<int, float[]> m_landMap;
            lock (m_simulatorHeightFields)
            {
                if (m_simulatorHeightFields.ContainsKey(simhandle))
                    m_landMap = m_simulatorHeightFields[simhandle];
                else
                {
                    m_log.Warn("[TERRAIN]: Warning landmap update XY for land that isn't found");
                    return;
                }
            }


            lock (m_landMap)
            {
                if (m_landMap.ContainsKey(y * 16 + x))
                    m_landMap[y * 16 + x] = data;
                else
                    m_landMap.Add(y * 16 + x, data);
            }

            UpdateHeightField(x, y, sim);

            lock (m_terrainModifications)
            {
                if (!m_terrainModifications.Contains(simhandle))
                {
                    m_terrainModifications.Enqueue(simhandle);
                }
            }
        }

        private void UpdateHeightField(int x, int y, VSimulator sim)
        {

            Dictionary<int, float[]> m_landMap;
            ulong simhandle = sim.Handle;

            if (simhandle == 0)
                simhandle = m_viewer.TestNeighbor;

            lock (m_simulatorHeightFields)
            {
                if (m_simulatorHeightFields.ContainsKey(simhandle))
                {
                    m_landMap = m_simulatorHeightFields[simhandle];
                }
                else
                {
                    m_log.Warn("[TERRAIN]: Warning got terrain update for unknown simulator");
                    return;
                }
            }
            if (!m_landMap.ContainsKey(y * 16 + x))
            {
                m_log.Error("Trying to update terrain on a land patch we don't have.");
                return;
            }

            float[] currentPatch = m_landMap[y * 16 + x];

            for (int cy = 0; cy < 16; cy++)
            {
                for (int cx = 0; cx < 16; cx++)
                {
                    int bitmapx = cx + x * 16;
                    int bitmapy = cy + y * 16;

                    //int col = (int)(Util.Clamp(currentPatch[cy * 16 + cx] / 255, 0.0f, 1.0f) * 255);

                    float col = 0;

                    col = currentPatch[cy * 16 + cx];
                    if (col > 1000f || col < 0)
                        col = 0f;
                    //col *= 0.00388f;

                    if (m_viewer.SceneGraph.CurrentSimulator != null)
                    {
                        if (m_viewer.SceneGraph.CurrentSimulator.Handle == simhandle)
                        {
                            m_heightField[bitmapy, bitmapx] = col;
                        }
                    }

                    col *= 0.00397f;  // looks a little closer by eyeball

                }
            }
        }

        private float[,] GetSubHeightField(int startX, int endX, int startY, int endY)
        {
            float[,] retVal = new float[endY - startY + 1, endX - startX + 1];

            for (int retY = 0, y = startY; y <= endY; retY++, y++)
                for (int retX = 0, x = startX; x <= endX; retX++, x++)
                    retVal[retY, retX] = m_heightField[y, x];

            return retVal;
        }

    }
}
