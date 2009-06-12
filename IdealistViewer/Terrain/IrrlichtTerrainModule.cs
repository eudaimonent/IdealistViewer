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
    public class IrrlichtTerrainModule : ITerrainManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Viewer m_viewer;

        /// <summary>
        /// Bitmaps for the terrain in each active region indexed by ulong regionhandle
        /// </summary>
        private Dictionary<ulong, System.Drawing.Bitmap> m_terrainBitmap = new Dictionary<ulong, System.Drawing.Bitmap>();
        /// <summary>
        /// Queue of terrain that needs to be updated.  ulong region handle.
        /// </summary>
        private Queue<ulong> m_terrainModifications = new Queue<ulong>();
        /// <summary>
        /// Simulator patch storage Indexed by regionhandle  Contains a dictionary with the patch number as an index.
        /// </summary>
        private Dictionary<ulong, Dictionary<int, float[]>> m_landMaps = new Dictionary<ulong, Dictionary<int, float[]>>();        
        /// <summary>
        /// TerrainSceneNode Irrlicht representations of the terrain.  Indexed by regionhandle
        /// </summary>
        private Dictionary<ulong, TerrainSceneNode> m_terrains = new Dictionary<ulong, TerrainSceneNode>();
        private IDictionary<ulong, IList<SceneNode>> m_terrainNodes = new Dictionary<ulong, IList<SceneNode>>();
        private float[,] m_heightField = new float[256, 256];
        private Texture m_greenGrassTexture = null;

        public IrrlichtTerrainModule(Viewer viewer)
        {
            m_viewer = viewer;

            m_greenGrassTexture = m_viewer.Renderer.Driver.GetTexture("Green_Grass_Detailed.png");
        }

        public void Process()
        {
            UpdateTerrain();
        }

        public void OnSimulatorConnected(VSimulator sim)
        {
            if (!m_terrainBitmap.ContainsKey(sim.Handle))
            {
                m_terrainBitmap.Add(sim.Handle, new System.Drawing.Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format24bppRgb));
                Dictionary<int, float[]> m_landMap = new Dictionary<int, float[]>();
                if (!m_landMaps.ContainsKey(sim.Handle))
                {
                    m_landMaps.Add(sim.Handle, m_landMap);
                }
                lock (m_terrainModifications)
                {
                    if (!m_terrainModifications.Contains(sim.Handle))
                    {
                        m_terrainModifications.Enqueue(sim.Handle);
                    }
                }
            }
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
            lock (m_landMaps)
            {
                if (m_landMaps.ContainsKey(simhandle))
                    m_landMap = m_landMaps[simhandle];
                else
                {
                    m_log.Warn("[TERRAIN]: Warning landmap update XY for land that isn't found (0)");
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

            UpdateTerrainBitmap(x, y, sim);

            lock (m_terrainModifications)
            {
                if (!m_terrainModifications.Contains(simhandle))
                {
                    m_terrainModifications.Enqueue(simhandle);
                }
            }
        }

        /// <summary>
        /// Update all terrain that's dirty!
        /// </summary>
        private void UpdateTerrain()
        {
            lock (m_terrainModifications)
            {
                while (m_terrainModifications.Count > 0)
                {
                    ulong regionhandle = m_terrainModifications.Dequeue();
                    //m_log.Warn("[TERRAIN]: RegionHandle:" + regionhandle.ToString());
                    string filename = "myterrain1" + regionhandle.ToString() + ".bmp";
                    string path = Util.MakePath("media", "materials", "textures", filename);
                    System.Drawing.Bitmap terrainbmp = m_terrainBitmap[regionhandle];
                    lock (terrainbmp)
                    {
                        Util.SaveBitmapToFile(terrainbmp, m_viewer.StartupDirectory + "/" + path);

                    }
                    m_viewer.Renderer.Device.FileSystem.WorkingDirectory = m_viewer.StartupDirectory + "/" + Util.MakePath("media", "materials", "textures", "");
                    TerrainSceneNode terrain = null;
                    lock (m_terrains)
                    {
                        if (m_terrains.ContainsKey(regionhandle))
                        {
                            terrain = m_terrains[regionhandle];
                            m_terrains.Remove(regionhandle);
                        }
                    }

                    lock (VSceneGraph.MeshingLock)
                    {

                        if (terrain != null)
                        {
                            // Remove old pickers
                            m_viewer.SceneGraph.TrianglePicker.RemTriangleSelector(terrain.TriangleSelector);
                            m_viewer.Renderer.SceneManager.AddToDeletionQueue(terrain);
                        }
                        Vector3 relTerrainPos = Vector3.Zero;
                        if (m_viewer.SceneGraph.CurrentSimulator != null)
                        {
                            if (m_viewer.SceneGraph.CurrentSimulator.Handle != regionhandle)
                            {
                                Vector3 Offsetcsg = Util.OffsetGobal(m_viewer.SceneGraph.CurrentSimulator.Handle, Vector3.Zero);
                                Vector3 Offsetnsg = Util.OffsetGobal(regionhandle, Vector3.Zero);
                                relTerrainPos = Offsetnsg - Offsetcsg;
                            }
                        }
                        terrain = m_viewer.Renderer.SceneManager.AddTerrainSceneNode(
                        filename, m_viewer.Renderer.SceneManager.RootSceneNode, -1,
                        new Vector3D(relTerrainPos.X - 4f, relTerrainPos.Z, relTerrainPos.Y + 16f), new Vector3D(0, 270, 0), new Vector3D(1, 1, 1), new Color(255, 255, 255, 255), 2, TerrainPatchSize.TPS17);
                        //device.FileSystem.WorkingDirectory = "./media/";
                        terrain.SetMaterialFlag(MaterialFlag.Lighting, true);
                        terrain.SetMaterialType(MaterialType.DetailMap);
                        terrain.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                        terrain.SetMaterialTexture(0, m_greenGrassTexture);
                        //terrain.SetMaterialTexture(1, driver.GetTexture("detailmap3.jpg"));
                        terrain.ScaleTexture(16, 16);
                        terrain.Scale = new Vector3D(1.0275f, 1, 1.0275f);
                    }

                    lock (m_terrains)
                    {
                        m_terrains.Add(regionhandle, terrain);
                    }

                    // Manage pickers
                    TriangleSelector terrainsel;
                    lock (m_viewer.SceneGraph.TerrainTriangleSelectors)
                    {
                        if (m_viewer.SceneGraph.TerrainTriangleSelectors.ContainsKey(regionhandle))
                        {
                            if (m_viewer.SceneGraph.TriangleSelector != null)
                            {
                                m_viewer.SceneGraph.TriangleSelector.RemoveTriangleSelector(m_viewer.SceneGraph.TerrainTriangleSelectors[regionhandle]);
                            }
                            m_viewer.SceneGraph.TerrainTriangleSelectors.Remove(regionhandle);

                        }
                    }

                    terrainsel = m_viewer.Renderer.SceneManager.CreateTerrainTriangleSelector(terrain, 1);
                    terrain.TriangleSelector = terrainsel;
                    m_viewer.SceneGraph.TrianglePicker.AddTriangleSelector(terrainsel, terrain);

                    lock (m_viewer.SceneGraph.TerrainTriangleSelectors)
                    {
                        m_viewer.SceneGraph.TerrainTriangleSelectors.Add(regionhandle, terrainsel);

                    }
                    if (m_viewer.SceneGraph.TriangleSelector != null)
                    {
                        m_viewer.SceneGraph.TriangleSelector.AddTriangleSelector(terrainsel);
                    }
                    //Vector3D terrainpos = terrain.TerrainCenter;
                    //terrainpos.Z = terrain.TerrainCenter.Z - 100f;
                    //terrain.Position = terrainpos;
                    //m_log.DebugFormat("[TERRAIN]:<{0},{1},{2}>", terrain.TerrainCenter.X, terrain.TerrainCenter.Y, terrain.TerrainCenter.Z);
                    //terrain.ScaleTexture(1f, 1f);
                    if (m_viewer.SceneGraph.CurrentSimulator != null)
                    {
                        // Update camera position
                        if (m_viewer.SceneGraph.CurrentSimulator.Handle == regionhandle)
                        {
                            m_viewer.CameraController.CameraNode.Target = terrain.TerrainCenter;
                            m_viewer.CameraController.UpdateCameraPosition();
                        }
                    }

                }
            }
        }

        private void UpdateTerrainBitmap(int x, int y, VSimulator sim)
        {

            Dictionary<int, float[]> m_landMap;
            ulong simhandle = sim.Handle;

            if (simhandle == 0)
                simhandle = m_viewer.TestNeighbor;

            lock (m_landMaps)
            {
                if (m_landMaps.ContainsKey(simhandle))
                {
                    m_landMap = m_landMaps[simhandle];
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


            System.Drawing.Bitmap terrainbitmap;

            lock (m_terrainBitmap)
            {
                if (m_terrainBitmap.ContainsKey(simhandle))
                {
                    terrainbitmap = m_terrainBitmap[simhandle];
                }
                else
                {
                    m_log.Warn("[TERRAIN]:Unable to locate terrain bitmap to write terrain update to");
                    return;
                }
            }

            lock (terrainbitmap)
            {
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


                        //m_log.Debug("[COLOR]: " + currentPatch[cy * 16 + cx].ToString());
                        //terrainbitmap.SetPixel(bitmapy, bitmapx, System.Drawing.Color.FromArgb(col, col, col));
                        terrainbitmap.SetPixel(bitmapy, bitmapx, Util.FromArgbf(1, col, col, col));
                    }
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
