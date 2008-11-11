using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using IrrlichtNETCP;
using OpenMetaverse;
using Nini.Config;
using PrimMesher;

namespace IdealistViewer
{
    public class BaseIdealistViewer : conscmd_callback
    {
        public IdealistViewerConfigSource m_config = null;
        private IrrlichtDevice device = null;
        private VideoDriver driver = null;
        private SceneManager smgr = null;
        private GUIEnvironment guienv = null;
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Thread guithread;
        private Dictionary<ulong, System.Drawing.Bitmap> terrainBitmap = new Dictionary<ulong, System.Drawing.Bitmap>();
        private SLProtocol avatarConnection;
        private Dictionary<ulong,TerrainSceneNode> terrains = new Dictionary<ulong,TerrainSceneNode>();
        
        private Dictionary<ulong, Dictionary<int, float[]>> m_landmaps = new Dictionary<ulong, Dictionary<int, float[]>>();
        
        private Dictionary<ulong, TriangleSelector> terrainsels = new Dictionary<ulong,TriangleSelector>();
        private static SceneNode SNGlobalwater;
        private static MetaTriangleSelector mts;

        private static Queue<VObject> objectModQueue = new Queue<VObject>();
        private static Queue<VObject> objectMeshQueue = new Queue<VObject>();
        public static Dictionary<string, UUID> waitingSculptQueue = new Dictionary<string, UUID>();

        private static Queue<VObject> UnAssignedChildObjectModQueue = new Queue<VObject>();
        private static Queue<TextureComplete> assignTextureQueue = new Queue<TextureComplete>();

        private static Dictionary<string, VObject> interpolationTargets = new Dictionary<string, VObject>();
        private static SkinnedMesh avmeshtest = null;
        private static AnimatedMeshSceneNode avmeshsntest = null;

        private static Simulator currentSim;

        private static Dictionary<ulong, Simulator> Simulators = new Dictionary<ulong, Simulator>();
        private static Dictionary<string, VObject> Entities = new Dictionary<string, VObject>();
        private static Dictionary<UUID, VObject> Avatars = new Dictionary<UUID, VObject>();
        
        private static bool ctrlHeld = false;
        private static bool shiftHeld = false;
        private static bool appHeld = false;
        private static bool LMheld = false;
        private static bool RMheld = false;
        private static bool MMheld = false;
        private static bool loadTextures = true;
        public static bool backFaceCulling = true;
        private static string avatarMesh = "sydney.md2";
        private static string avatarMaterial = "sydney.BMP";

        private int OldMouseX = 0;
        private int OldMouseY = 0;

        private static int WindowWidth = 1024;
        private static int WindowHeight = 768;
        private static float WindowWidth_DIV2 = WindowWidth * 0.5f;
        private static float WindowHeight_DIV2 = WindowHeight * 0.5f;
        private static float aspect = (float)WindowWidth / WindowHeight;
        private Camera cam;
        
        private static Vector3 m_lastTargetPos = Vector3.Zero;

        
        private static List<KeyCode> m_heldKeys = new List<KeyCode>();

        private int tickcount = 0;
        private int mscounter = 0;

        private int msreset = 100;
        private uint framecounter = 0;
        private int maxFPS = 30;  // cap frame rate at 30 fps to help keep cpu load down

        private ulong TESTNEIGHBOR = 1099511628032256;

        private uint objectmods = 5; // process object queue 2 times a second
        private static Object mesh_synclock = new Object();
        public static IrrlichtNETCP.Quaternion Cordinate_XYZ_XZY = new IrrlichtNETCP.Quaternion();

        private TextureManager textureMan = null;
        // experimental mesh code - only here temporarily - up top so it's visible

        public Vector2D convVect2d(UVCoord uv)
        {
            return new Vector2D(uv.U, uv.V);
        }

        public Vector3D convVect3d(Coord c)
        {// translate coordinates XYZ to XZY
            return new Vector3D(c.Y, c.Z, c.X);
        }

        # region PrimTesting

        public Mesh makeSamplePrim()
        {
            PrimMesh primMesh = new PrimMesh(24, 0.0f, 1.0f, 0.0f, 24);
            primMesh.ExtrudeCircular();
            //primMesh.CalcNormals(); // surface normals for now
            primMesh.Scale(128.0f, 128.0f, 128.0f);

            Mesh mesh = new Mesh();
            MeshBuffer mb = new MeshBuffer(VertexType.Standard);

            for (uint index = 0; index < primMesh.coords.Count; index++)
            {
                Vertex3D vert = new Vertex3D();
                vert.Position = convVect3d(primMesh.coords[(int)index]);
                //vert.Normal = convVect3d(primMesh.normals[(int)index]);
                vert.Color = new Color(255, 128, 0, 0);
                mb.SetVertex(index, vert);
            }

            uint nr = 0;
            int faceIndex = 0;
            foreach (Face f in primMesh.faces)
            {

                Vector3D surfaceNormal = convVect3d(primMesh.SurfaceNormal(faceIndex));

                mb.SetIndex(nr++, (ushort)f.v1);
                mb.SetIndex(nr++, (ushort)f.v2);
                mb.SetIndex(nr++, (ushort)f.v3);
                mb.GetVertex((ushort)f.v1).Normal = surfaceNormal;
                mb.GetVertex((ushort)f.v2).Normal = surfaceNormal;
                mb.GetVertex((ushort)f.v3).Normal = surfaceNormal;

                faceIndex++;
            }

            mesh.AddMeshBuffer(mb);
            //mb.Dispose();
            return mesh;
        }

        public Mesh makeSamplePrimNew()
        {
            PrimMesh primMesh = new PrimMesh(24, 0.0f, 1.0f, 0.0f, 24);
            primMesh.viewerMode = true;
            primMesh.ExtrudeCircular();
            primMesh.Scale(128.0f, 128.0f, 128.0f);

            Mesh mesh = new Mesh();
            MeshBuffer mb = new MeshBuffer(VertexType.Standard);

            Color color = new Color(255, 128, 0, 0);
            uint index = 0;
            for (uint vfIndex = 0; vfIndex < primMesh.viewerFaces.Count; vfIndex++)
            {
                ViewerFace vf = primMesh.viewerFaces[(int)vfIndex];
                mb.SetVertexT2(index, new Vertex3DT2(convVect3d(vf.v1), convVect3d(vf.n1), color, convVect2d(vf.uv1), convVect2d(vf.uv1)));
                mb.SetIndex(vfIndex, (ushort)index++);
                mb.SetVertexT2(index++, new Vertex3DT2(convVect3d(vf.v2), convVect3d(vf.n2), color, convVect2d(vf.uv2), convVect2d(vf.uv2)));
                mb.SetIndex(vfIndex, (ushort)index++);
                mb.SetVertexT2(index++, new Vertex3DT2(convVect3d(vf.v3), convVect3d(vf.n3), color, convVect2d(vf.uv3), convVect2d(vf.uv3)));
                mb.SetIndex(vfIndex, (ushort)index++);
            }

            mesh.AddMeshBuffer(mb);

            return mesh;
        }

        public void generateRandomPrim(int count)
        {

            Random rnd = new Random(System.Environment.TickCount);
            // dahlia's sample prim
            for (int j = 0; j < count; j++)
            {
                Mesh samplePrim = makeSamplePrim();
                if (samplePrim != null)
                {
                    SceneNode samplePrimNode = smgr.AddMeshSceneNode(samplePrim, smgr.RootSceneNode, -1);
                    samplePrimNode.Position = new Vector3D((float)(rnd.NextDouble() * 256), (float)(rnd.NextDouble() * 256), (float)(rnd.NextDouble() * 256));
                    samplePrimNode.Scale = new Vector3D(0.1f, 0.1f, 0.1f);
                    samplePrimNode.SetMaterialFlag(MaterialFlag.Lighting, true);
                }
                m_log.Debug(j);
            }
        }

        #endregion

        private static Queue<ulong> m_dirtyTerrain = new Queue<ulong>();

        /// <summary>
        /// Time at which this server was started
        /// </summary>
        protected DateTime m_startuptime;

        /// <summary>
        /// Record the initial startup directory for info purposes
        /// </summary>
        protected string m_startupDirectory = Environment.CurrentDirectory;

        /// <summary>
        /// Server version information.  Usually VersionInfo + information about svn revision, operating system, etc.
        /// </summary>
        protected string m_version;

        protected ConsoleBase m_console;

        public BaseIdealistViewer(IConfigSource iconfig)
        {

            m_config = new IdealistViewerConfigSource();
            m_config.Source = new IniConfigSource();
            

            string iniconfig = Path.Combine(Util.configDir(), "IdealistViewer.ini");
            if (File.Exists(iniconfig))
            {
                m_config.Source.Merge(new IniConfigSource(iniconfig));    
            }
            m_config.Source.Merge(iconfig);
            m_startuptime = DateTime.Now;
        }

       
       
        public void startupGUI(object o)
        {

            device = new IrrlichtDevice(DriverType.OpenGL,
                                                     new Dimension2D(WindowWidth, WindowHeight),
                                                    32, false, true, false, true);
            /*Now we set the caption of the window to some nice text. Note that there is a 'L' in front of the string: the Irrlicht Engine uses
wide character strings when displaying text.
            */
            device.WindowCaption = "IdealistViewer 0.000000000002";
            device.FileSystem.WorkingDirectory = m_startupDirectory + "\\" + Util.MakePath("media", "materials", "textures", "");  //We set Irrlicht's current directory to %application directory%/media

            //
            driver = device.VideoDriver;
            smgr = device.SceneManager;
            guienv = device.GUIEnvironment;
            device.OnEvent += new OnEventDelegate(device_OnEvent);

            if (loadTextures)
            {
                textureMan = new TextureManager(device, driver, "IdealistCache", avatarConnection);
                textureMan.OnTextureLoaded += textureCompleteCallback;
            }

            //guienv.AddStaticText("Hello World! This is the Irrlicht Software engine!",
            //    new Rect(new Position2D(10, 10), new Dimension2D(200, 22)), true, false, guienv.RootElement, -1, false);
            //Image img = 
            //
            smgr.SetAmbientLight(new Colorf(1, 0.6f, 0.6f, 0.6f));
            //smgr.VideoDriver.AmbientLight = new Colorf(1, 1f, 1f, 1f);


            //driver.
            smgr.VideoDriver.SetFog(new Color(0, 255, 255, 255), false, 9999, 9999, 0, false, false);
            driver.SetTextureFlag(TextureCreationFlag.CreateMipMaps, false);

            smgr.AddSkyBoxSceneNode(null, new Texture[] {
                driver.GetTexture("irrlicht2_up.jpg"),
                driver.GetTexture("irrlicht2_dn.jpg"),
                driver.GetTexture("irrlicht2_rt.jpg"),
                driver.GetTexture("irrlicht2_lf.jpg"),
                driver.GetTexture("irrlicht2_ft.jpg"),
                driver.GetTexture("irrlicht2_bk.jpg")}, 0);

            driver.SetTextureFlag(TextureCreationFlag.CreateMipMaps, true);
            SceneNode tree = smgr.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(128, 40, 128), new Vector3D(0, 0, 0), new Vector3D(0.25f, 0.25f, 0.25f), driver.GetTexture("OakBark.png"), driver.GetTexture("OakLeaf.png"), driver.GetTexture("OakBillboard.png"));
            tree.Position = new Vector3D(129, 22, 129);
            //tree.Scale = new Vector3D(0.25f, 0.25f, 0.25f);
            cam = new Camera(smgr);




            //AnimatedMesh mesh = smgr.GetMesh("sydney.md2");
            //AnimatedMeshSceneNode node99 = smgr.AddAnimatedMeshSceneNode(mesh);
            ///node99.

            //if (node != null)
            //{
            //    node.SetMaterialFlag(MaterialFlag.Lighting, false);
            //    node.SetFrameLoop(0, 310);
            //    node.SetMaterialTexture(0, driver.GetTexture("sydney.bmp"));
            //    node.Position = new Vector3D(128, 32, 128);
            //}
            //we add the skybox which we already used in lots of Irrlicht examples.




            SceneNode light = smgr.AddLightSceneNode(smgr.RootSceneNode, new Vector3D(0, 0, 0), new Colorf(1, 0.5f, 0.5f, 0.5f), 90, -1);
            Animator anim = smgr.CreateFlyCircleAnimator(new Vector3D(128, 250, 128), 250.0f, 0.0010f);
            light.AddAnimator(anim);
            anim.Dispose();

            SceneNode light2 = smgr.AddLightSceneNode(smgr.RootSceneNode, new Vector3D(0, 255, 0), new Colorf(0, 0.75f, 0.75f, 0.75f), 250, -1);


            // dahlia's sample prim

            //Mesh samplePrim = makeSamplePrim();
            //if (samplePrim != null)
            //{
            //    SceneNode samplePrimNode = smgr.AddMeshSceneNode(samplePrim, smgr.RootSceneNode, -1);
            //    samplePrimNode.Position = new Vector3D(128, 64, 128);
            //    samplePrimNode.SetMaterialFlag(MaterialFlag.Lighting, true);
            //}

            // dahlia's sample sculpty

            string sculptFileName = "d:\\sampleSculpty.bmp";
            try
            {
                Mesh samplePrim = null;
                System.Drawing.Image image = System.Drawing.Bitmap.FromFile(sculptFileName);

                samplePrim = PrimMesherG.SculptIrrMesh((System.Drawing.Bitmap)image);
                if (samplePrim != null)
                {
                    SceneNode samplePrimNode = smgr.AddMeshSceneNode(samplePrim, smgr.RootSceneNode, -1);
                    samplePrimNode.Position = new Vector3D(128, 32, 128);
                    samplePrimNode.SetMaterialFlag(MaterialFlag.Lighting, true);
                    samplePrimNode.SetMaterialTexture(0, driver.GetTexture("d:\\sampleSculptyTexture.bmp"));
                    }
            }
            catch (Exception e)
            {
                m_log.Error("Unable to open sample sculpty file: " + sculptFileName, e);
            }

            //generateRandomPrim(4000);
            AnimatedMesh mesh = smgr.AddHillPlaneMesh("myHill",
                new Dimension2Df(120, 120),
                new Dimension2D(40, 40), 0,
                new Dimension2Df(0, 0),
                new Dimension2Df(10, 10));

            SNGlobalwater = smgr.AddWaterSurfaceSceneNode(mesh.GetMesh(0),
                                                    0.4f, 300.0f, 12.0f, smgr.RootSceneNode, -1);
            SNGlobalwater.SetMaterialTexture(0, driver.GetTexture("water.jpg"));
            SNGlobalwater.SetMaterialTexture(1, driver.GetTexture("water2.tga"));
            SNGlobalwater.SetMaterialType(MaterialType.TransparentReflection2Layer);
            SNGlobalwater.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
            SNGlobalwater.Position = new Vector3D(0, 0, 0);

            mts = smgr.CreateMetaTriangleSelector();
            //GUIContextMenu gcontext = guienv.AddMenu(guienv.RootElement, 90);
            //gcontext.Text = "Some Text";
            //gcontext.AddItem("SomeCooItem", 93, true, true);


            //GUIToolBar gtb = guienv.AddToolBar(guienv.RootElement, 91);
            //gtb.Text = "Hi";
            //gtb.AddButton(92, "Button", "Click", null, null, true, false);

            AnimatedMesh av = smgr.GetMesh("Female7.x");

            int mbcount = av.GetMesh(0).MeshBufferCount;
            for (int j = 0; j < mbcount; j++)
            {
                Texture texDriver = driver.GetTexture(j.ToString() + "-" + avatarMaterial);
                if (texDriver != null)
                    av.GetMesh(0).GetMeshBuffer(j).Material.Texture1 = texDriver;
                else
                    av.GetMesh(0).GetMeshBuffer(j).Material.Texture1 = driver.GetTexture(avatarMaterial);

                av.GetMesh(0).GetMeshBuffer(j).Material.SpecularColor = new Color(255, 150, 150, 150);
                av.GetMesh(0).GetMeshBuffer(j).Material.AmbientColor = new Color(255, 110, 110, 110);
                av.GetMesh(0).GetMeshBuffer(j).Material.EmissiveColor = new Color(255, 125, 125, 125);
                av.GetMesh(0).GetMeshBuffer(j).Material.Shininess = 0.80f;
            }
            smgr.MeshManipulator.FlipSurfaces(av.GetMesh(0));

            avmeshsntest = smgr.AddAnimatedMeshSceneNode(av);
            avmeshsntest.JointMode = JointUpdateOnRenderMode.Control;
            //avmeshsntest.SetMaterialFlag(MaterialFlag.NormalizeNormals,true);

            System.IO.BinaryReader br = new BinaryReader(File.Open(m_startupDirectory + "\\" + Util.MakePath("media", "materials", "textures", "") + "\\coolfile.an",FileMode.Open));
            byte[] arr = new byte[(int)br.BaseStream.Length + 20];
            {
                int pos = 0;
                int length = (int)br.BaseStream.Length;
                
                arr = br.ReadBytes(((int)br.BaseStream.Length)-1);

                
            }
            br.Close();
            BinBVHAnimationReader anims = new BinBVHAnimationReader(arr);
            
            //BoneSceneNode avb = avm.GetXJointNode(1);
            avmeshsntest.Position = new Vector3D(125, 37, 125);
            avmeshsntest.Scale = new Vector3D(15, 15, 15);
            avmeshsntest.Rotation = new Vector3D(180, 0, 0);
            //avmeshsntest.UpdateAbsolutePosition();
            anims = new BinBVHAnimationReader(arr);
            for (int jointnum = 0; jointnum < anims.joints.Length; jointnum++)
            {
                binBVHJoint jointdata = anims.joints[jointnum];
                BoneSceneNode sn = avmeshsntest.GetJointNode(jointdata.Name);

                //sn.SkinningSpace = SkinningSpace.Local;
                if (sn != null)
                {
                    string name = jointdata.Name;
                    m_log.DebugFormat("[defaults]: Name:{0}, <{1},{2},{3}>", jointdata.Name, sn.Rotation.X, sn.Rotation.Y, sn.Rotation.Z);
                    if (jointdata.rotationkeys[0].key_element != Vector3.Zero)
                    {

                        m_log.DebugFormat("[ROTA]:<{0},{1},{2}>", jointdata.rotationkeys[0].key_element.Y, jointdata.rotationkeys[0].key_element.Z, jointdata.rotationkeys[0].key_element.X);
                    }
                    Vector3D jointeuler = sn.Rotation + new Vector3D(jointdata.rotationkeys[0].key_element.Z, jointdata.rotationkeys[0].key_element.Y, jointdata.rotationkeys[0].key_element.X);
                    sn.Rotation = jointeuler;// *framecounter;
                    sn.UpdateAbsolutePositionOfAllChildren();
                }
            }

            //SkinnedMesh smm = new SkinnedMesh(avm.AnimatedMesh.Raw);
            //smm.SkinMesh();

            int minFrameTime = (int)(1.0f / maxFPS);
            bool running = true;
            while (running)
            {
                try
                {

                    running = device.Run();
                }
                catch (AccessViolationException)
                {
                    m_log.Error("[VIDEO]: Error in device");
                }
                if (!running)
                    break;
                tickcount = System.Environment.TickCount;
                UpdateTerrain();
                //cam.Position = new Vector3D(cam.Position.X , cam.Position.Y, cam.Position.Z- 0.5f);
                //cam.Target = new Vector3D(0, 0, 0);//cam.Target.X - 0.5f, cam.Target.Y, cam.Target.Z);
                //avm.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                //avm.SetMaterialTexture(0,driver.GetTexture("Green_Grass_Detailed.tga"));
                //avb
                //avm.AnimatedMesh.GetMesh(0).GetMeshBuffer(0).GetVertex(0).TCoords
                /*
                 * 
                avmeshsntest.AnimationSpeed = 8;
                avmeshsntest.SetFrameLoop(0, 1);
                avmeshsntest.AnimateJoints(true);
                avmeshsntest.AutomaticCulling = CullingType.Off;
                avmeshsntest.CurrentFrame = 1;
                //avmeshsntest.DebugDataVisible = DebugSceneType.Full;
                avmeshsntest.LoopMode = true;
                */




                driver.BeginScene(true, true, new Color(255, 100, 101, 140));
                smgr.DrawAll();
                guienv.DrawAll();
                driver.Draw3DTriangle(new Triangle3D(
                    new Vector3D(0, 0, 0),
                    new Vector3D(10, 0, 0),
                    new Vector3D(0, 10, 0)),
                    Color.Red);
                //m_log.Debug(driver.FPS);
                driver.EndScene();

                mscounter += System.Environment.TickCount - tickcount;
                msreset = 55;
                //
                if (mscounter > msreset)
                {
                    processHeldKeys();

                    updateInterpolationTargets();
                    cam.CheckTarget();

                    mscounter = 0;
                    framecounter++;

                    if (framecounter == uint.MaxValue)
                        framecounter = 0;
                }
                
               
              
                if ((framecounter % objectmods) == 0)
                {
                    doProcessMesh(5);
                    doObjectMods(5);
                    CheckAndApplyParent(5);
                    doTextureMods(1);
                    doSetCameraPosition();
                    

                    //BoneSceneNode bcn = avmeshsntest.GetJointNode("lCollar:2");
                    //bcn.Rotation = new Vector3D(0, 36 + framecounter, 0);
                    //bcn.Position = new Vector3D(0, 0, 1 + framecounter);
                    
                    //avmeshsntest.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);

                }
                //Thread.Sleep(50);
                int frameTime = System.Environment.TickCount - tickcount;
                if (frameTime < minFrameTime)
                    Thread.Sleep(minFrameTime - frameTime);
                //Thread.Sleep(50);
            }
            //In the end, delete the Irrlicht device.
            Shutdown();

        }

        private void updateInterpolationTargets()
        {
            List<string> removestr = null;
            lock (interpolationTargets)
            {
                foreach (string str in interpolationTargets.Keys)
                {
                    VObject obj = interpolationTargets[str];
                    if (obj == null)
                    {
                        if (removestr == null)
                            removestr = new List<string>();

                        removestr.Add(str);
                        continue;
                    }
                    if (obj.node == null)
                    {
                        if (removestr == null)
                            removestr = new List<string>();

                        removestr.Add(str);
                        continue;
                    }
                    if (obj.node.Raw == IntPtr.Zero)
                    {
                        if (removestr == null)
                            removestr = new List<string>();

                        removestr.Add(str);
                        continue;
                    }
                    try
                    {
                        Vector3D pos = new Vector3D(obj.node.Position.X, obj.node.Position.Y, obj.node.Position.Z);
                        if (obj.prim is Avatar)
                        {
                            Avatar av = (Avatar)obj.prim;
                            if (obj.prim.Velocity.Z < 0 && obj.prim.Velocity.Z > -2f)
                                obj.prim.Velocity.Z = 0;
                        }
                        obj.node.Position = pos + new Vector3D(obj.prim.Velocity.X * 0.055f, obj.prim.Velocity.Z * 0.055f, obj.prim.Velocity.Y * 0.055f);
                    }
                    catch (AccessViolationException)
                    {
                        if (removestr == null)
                            removestr = new List<string>();

                        removestr.Add(str);
                        continue;
                    }
                    catch (System.Runtime.InteropServices.SEHException)
                    {
                        if (removestr == null)
                            removestr = new List<string>();

                        removestr.Add(str);
                        continue;
                    }

                }
                if (removestr != null)
                {
                    foreach (string str2 in removestr)
                    {
                        if (interpolationTargets.ContainsKey(str2))
                        {
                            interpolationTargets.Remove(str2);
                        }
                    }
                }
            }

        }

        private void doTextureMods(int pCount)
        {
            lock (assignTextureQueue)
            {
                if (assignTextureQueue.Count < pCount)
                    pCount = assignTextureQueue.Count;
            }

            for (int i=0;i < pCount; i++)
            {

                TextureComplete tx;
                TextureExtended tex = null;

                lock (assignTextureQueue)
                {
                    if (i >= assignTextureQueue.Count)
                        break;

                    tx = assignTextureQueue.Dequeue();
                    // Try not to double load the texture first.
                    if (!textureMan.tryGetTexture(tx.textureID, out tex))
                    {
                        tex = new TextureExtended(driver.GetTexture(tx.texture).Raw);
                    }
                }

                if (tx.vObj != null && tex != null)
                {
                    
                        if (tx.textureID == tx.vObj.prim.Sculpt.SculptTexture)
                        {
                            tx.vObj.updateFullYN = true;
                            //tx.vObj.mesh.Dispose();
                            if (tx.vObj.node != null && tx.vObj.node.Raw != IntPtr.Zero)
                                smgr.AddToDeletionQueue(tx.vObj.node);
                            
                            //tx.vObj.mesh = null;

                            lock (objectMeshQueue)
                            {
                                m_log.Warn("[SCULPT]: Got Sculpt Callback, remeshing");
                                objectMeshQueue.Enqueue(tx.vObj);
                            }
                            continue;
                            // applyTexture will skip over textures that are not 
                            // defined in the textureentry
                        }
                    
                    textureMan.applyTexture(tex, tx.vObj, tx.textureID);
                }
            }
            
            
        }
        private void doSetCameraPosition()
        {
            Vector3[] camdata = cam.GetCameraLookAt();

            avatarConnection.SetCameraPosition(camdata[0],camdata[1]);
        }


        #region Object Management
        public void enqueueVObject(VObject newObject)
        {

            if (newObject.mesh != null)
            {
                lock (Entities)
                {
                    if (!Entities.ContainsKey(VUtil.GetHashId(newObject)))
                    {
                        Entities.Add(VUtil.GetHashId(newObject), newObject);
                    }
                    else
                    {
                        // Full object update
                        //m_log.Warn("[NEWPRIM]   ");
                    }
                }

                lock (objectModQueue)
                {
                    objectModQueue.Enqueue(newObject);
                }
            }
        }

        private void doObjectMods(int pObjects)
        {
            for (int i = 0; i < pObjects; i++)
            {
                VObject vObj = null;
                lock (objectModQueue)
                {
                    if (objectModQueue.Count == 0)
                        break;
                    vObj = objectModQueue.Dequeue();
                }
                if (vObj.prim != null)
                {



                    ulong simhandle = vObj.prim.RegionHandle;

                    if (simhandle == 0)
                        simhandle = TESTNEIGHBOR;

                    Vector3 WorldoffsetPos = Vector3.Zero;

                    if (currentSim != null)
                    {
                        if (simhandle != currentSim.Handle)
                        {
                            Vector3 gposr = Util.OffsetGobal(simhandle, Vector3.Zero);
                            Vector3 gposc = Util.OffsetGobal(currentSim.Handle, Vector3.Zero);
                            
                            WorldoffsetPos =  gposr - gposc;
                        }
                    }
                    
                    VObject parentObj = null;
                    SceneNode parentNode = smgr.RootSceneNode;
                    //VObject vObj = UnAssignedChildObjectModQueue.Dequeue();
                    //if (Entities.ContainsKey(vObj.prim.RegionHandle.ToString() + vObj.prim.ParentID.ToString()))
                    //{
                    
                    if (vObj.prim.ParentID != 0)
                    {
                        lock (Entities)
                        {
                            if (Entities.ContainsKey(simhandle.ToString() + vObj.prim.ParentID.ToString()))
                            {
                                parentObj = Entities[simhandle.ToString() + vObj.prim.ParentID.ToString()];
                                if (parentObj.node != null)
                                {
                                    //parentNode = parentObj.node;
                                    //pscalex = parentObj.prim.Scale.X;
                                    //pscaley = parentObj.prim.Scale.Y;
                                    //pscalez = parentObj.prim.Scale.Z;
                                }
                                else
                                {
                                    UnAssignedChildObjectModQueue.Enqueue(vObj);
                                    continue;
                                }

                            }
                        }

                    }
                    //}
                    bool creatednode = false;
#region Avatar 

                    SceneNode node = null;
                    if (vObj.prim is Avatar)
                    {
                        if (((Avatar)vObj.prim).ID.ToString().Contains("dead"))
                            continue;
                        if (vObj.node == null && vObj.updateFullYN)
                        {
                        
                            AnimatedMesh avmesh = smgr.GetMesh(avatarMesh);

                            bool isTextured = false;
                            int numTextures = 0;
                            int mbcount = avmesh.GetMesh(0).MeshBufferCount;
                            for (int j = 0; j < mbcount; j++)
                            {
                                Texture texDriver = driver.GetTexture(j.ToString() + "-" + avatarMaterial);
                                numTextures += texDriver == null ? 0 : 1;
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.Texture1 = texDriver;
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.SpecularColor = new Color(255, 128, 128, 128);
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.AmbientColor = new Color(255, 128, 128, 128);
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.EmissiveColor = new Color(255, 128, 128, 128);
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.Shininess = 0;
                            }
                            if (numTextures == mbcount)
                                isTextured = true;

                            lock (Entities)
                            {
                                vObj.node = smgr.AddAnimatedMeshSceneNode(avmesh);
                                node = vObj.node;
                            }

                            node.Scale = new Vector3D(0.035f, 0.035f, 0.035f);
                            //node.Scale = new Vector3D(15f, 15f, 15f);
                            if (!isTextured)
                                node.SetMaterialTexture(0, driver.GetTexture(avatarMaterial));
                            node.SetMaterialFlag(MaterialFlag.Lighting, true);
                            lock (interpolationTargets)
                            {
                                if (interpolationTargets.ContainsKey(simhandle.ToString() + vObj.prim.LocalID.ToString()))
                                {
                                    interpolationTargets[simhandle.ToString() + vObj.prim.LocalID.ToString()] = vObj;
                                }
                                else
                                {
                                    interpolationTargets.Add(simhandle.ToString() + vObj.prim.LocalID.ToString(), vObj);
                                }
                            }
                            SceneNode trans = smgr.AddEmptySceneNode(node, -1);
                            node.AddChild(trans);
                            trans.Position = new Vector3D(0, 50, 0);

                            SceneNode trans2 = smgr.AddEmptySceneNode(node, -1);
                            node.AddChild(trans2);
                            trans2.Position = new Vector3D(0.0f, 49.5f, 0.5f);
                            
                            smgr.AddTextSceneNode(guienv.BuiltInFont, ((Avatar)vObj.prim).Name, new Color(255, 255, 255, 255), trans);
                            smgr.AddTextSceneNode(guienv.BuiltInFont, ((Avatar)vObj.prim).Name, new Color(255, 0, 0, 0), trans2);
                            
                            //node
                        }
                        else
                        {
                            node = vObj.node;
                        }

                    }
#endregion
                    else
                    {
                        if (vObj.mesh == null)
                            continue;
                        if (vObj.updateFullYN)
                        {
                            if (vObj.prim.Sculpt.SculptTexture != UUID.Zero)
                                m_log.Warn("[SCULPT]: Sending sculpt to the scene....");

                            //Vertex3D vtest = vObj.mesh.GetMeshBuffer(0).GetVertex(0);
                            //System.Console.WriteLine(" X:" + vtest.Position.X + " Y:" + vtest.Position.Y + " Z:" + vtest.Position.Z);
                            node = smgr.AddMeshSceneNode(vObj.mesh, parentNode, (int)vObj.prim.LocalID);

                            creatednode = true;
                            vObj.node = node;
                        }
                        else
                        {
                            node = vObj.node;
                        }
                        if (node == null)
                            continue;
                    }

                    if (node == null && vObj.prim is Avatar)
                    {
                        // why would node = null?
                        continue;
                    }
                    if (vObj.prim is Avatar)
                    {
                        
                        vObj.prim.Position.Z -= 0.2f;
                    }
                    else
                    {
                        node.Scale = new Vector3D(vObj.prim.Scale.X, vObj.prim.Scale.Z, vObj.prim.Scale.Y);
                    }
                    
                   // m_log.WarnFormat("[SCALE]: <{0},{1},{2}> = <{3},{4},{5}>", vObj.prim.Scale.X, vObj.prim.Scale.Z, vObj.prim.Scale.Y, pscalex, pscaley, pscalez);
                    if (vObj.prim.ParentID == 0)
                    {
                        if (vObj.prim is Avatar)
                        {
                            //m_log.WarnFormat("[AVATAR]: W:<{0},{1},{2}> R:<{3},{4},{5}>",WorldoffsetPos.X,WorldoffsetPos.Y,WorldoffsetPos.Z,vObj.prim.Position.X,vObj.prim.Position.Y,vObj.prim.Position.Z);
                            WorldoffsetPos = Vector3.Zero;
                        }

                        try
                        {
                            if (node.Raw == IntPtr.Zero)
                                continue;
                            node.Position = new Vector3D(WorldoffsetPos.X + vObj.prim.Position.X, WorldoffsetPos.Z + vObj.prim.Position.Z, WorldoffsetPos.Y + vObj.prim.Position.Y);
                        }
                        catch (System.Runtime.InteropServices.SEHException)
                        {
                            continue;
                        }
                        catch (AccessViolationException)
                        {
                            continue;
                        }
                        
                    }
                    else
                    {
                        if (node.Raw == IntPtr.Zero)
                            continue;
                        // ROTATION
                        if (vObj == null || parentObj == null)
                            continue;
                        if (vObj.prim == null || parentObj.prim == null)
                            continue;
                        vObj.prim.Position = vObj.prim.Position * parentObj.prim.Rotation;
                        vObj.prim.Rotation = parentObj.prim.Rotation * vObj.prim.Rotation;

                        node.Position = new Vector3D(WorldoffsetPos.X + parentObj.prim.Position.X + vObj.prim.Position.X, WorldoffsetPos.Z + parentObj.prim.Position.Z + vObj.prim.Position.Z, WorldoffsetPos.Y + parentObj.prim.Position.Y + vObj.prim.Position.Y);
                    }

                    if (vObj.updateFullYN)
                    {
                        if ((vObj.prim.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                        {
                            lock (interpolationTargets)
                            {
                                if (!interpolationTargets.ContainsKey(simhandle.ToString() + vObj.prim.LocalID.ToString()))
                                    interpolationTargets.Add(simhandle.ToString() + vObj.prim.LocalID.ToString(), vObj);
                            }
                        }
                        else
                        {
                            lock (interpolationTargets)
                            {
                                if (interpolationTargets.ContainsKey(simhandle.ToString() + vObj.prim.LocalID.ToString()))
                                    interpolationTargets.Remove(simhandle.ToString() + vObj.prim.LocalID.ToString());
                            }
                        }

                    }

                    //m_log.Warn(vObj.prim.Rotation.ToString());
                    IrrlichtNETCP.Quaternion iqu = new IrrlichtNETCP.Quaternion(vObj.prim.Rotation.X, vObj.prim.Rotation.Z, vObj.prim.Rotation.Y, vObj.prim.Rotation.W);
                    
                    iqu.makeInverse();

                    IrrlichtNETCP.Quaternion finalpos = iqu;
                    if (vObj.prim.ParentID != 0)
                    {
                        //IrrlichtNETCP.Quaternion parentrot = new IrrlichtNETCP.Quaternion(parentObj.node.Rotation.X, parentObj.node.Rotation.Y, parentObj.node.Rotation.Z);
                        //parentrot.makeInverse();
                        //parentrot = Cordinate_XYZ_XZY * parentrot;

                        //finalpos = parentrot * iqu;
                    }
                    finalpos = Cordinate_XYZ_XZY * finalpos;


                    if (node.Raw == IntPtr.Zero)
                        continue;
                    node.Rotation  = finalpos.Matrix.RotationDegrees;
                    if (creatednode)
                    {   
                        //node.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                        //node.SetMaterialFlag(MaterialFlag.BackFaceCulling, backFaceCulling);
                        //node.SetMaterialFlag(MaterialFlag.GouraudShading, true);
                        //node.SetMaterialFlag(MaterialFlag.Lighting, true);
                        //node.SetMaterialTexture(0, driver.GetTexture("red_stained_wood.tga"));

                        TriangleSelector trisel = smgr.CreateTriangleSelector(vObj.mesh, node);
                        node.TriangleSelector = trisel;
                        lock (mts)
                        {
                            mts.AddTriangleSelector(trisel);
                        }
                        if (vObj.prim.Textures != null)
                        {
                            if (vObj.prim.Textures.DefaultTexture != null)
                            {
                                if (vObj.prim.Textures.DefaultTexture.TextureID != UUID.Zero)
                                {
                                    UUID textureID = vObj.prim.Textures.DefaultTexture.TextureID;
                                    if (textureMan != null)
                                        textureMan.RequestImage(textureID, vObj);

                                }
                            }
                            
                            if (vObj.prim.Textures.FaceTextures != null)
                            {
                                
                                Primitive.TextureEntryFace[] objfaces = vObj.prim.Textures.FaceTextures;
                                for (int i2 = 0; i2 < objfaces.Length; i2++)
                                {
                                    if (objfaces[i2] == null)
                                        continue;

                                    UUID textureID = objfaces[i2].TextureID;

                                    if (textureID != UUID.Zero)
                                    {
                                        if (textureMan != null)
                                            textureMan.RequestImage(textureID, vObj);
                                    }
                                }
                            }
                        }
                    }
                    if (node.Raw == IntPtr.Zero)
                        continue;
                    node.UpdateAbsolutePosition();
                    
                }
            }
        }

        private void CheckAndApplyParent(int pObjects)
        {
            if (UnAssignedChildObjectModQueue.Count < pObjects)
                pObjects = UnAssignedChildObjectModQueue.Count;

            for (int i = 0; i < pObjects; i++)
            {
                VObject vObj = null;
                Vector3 WorldoffsetPos = Vector3.Zero;
                lock (UnAssignedChildObjectModQueue)
                {
                    if (UnAssignedChildObjectModQueue.Count == 0)
                        break;
                    
                    
                
                    vObj = UnAssignedChildObjectModQueue.Dequeue();
                }
                ulong simhandle = vObj.prim.RegionHandle;
                
                if (simhandle == 0)
                    simhandle = TESTNEIGHBOR;

                if (Entities.ContainsKey(simhandle.ToString() + vObj.prim.ParentID.ToString()))
                {
                    VObject parentObj = Entities[simhandle.ToString() + vObj.prim.ParentID.ToString()];

                    if (parentObj.node != null)
                    {
                        if (currentSim != null)
                        {
                            if (simhandle != currentSim.Handle)
                            {
                                Vector3 gposr = Util.OffsetGobal(simhandle, Vector3.Zero);
                                Vector3 gposc = Util.OffsetGobal(currentSim.Handle, Vector3.Zero);
                                WorldoffsetPos = gposr - gposc;
                            }
                        }
                        bool creatednode = false;

                        SceneNode node = null;
                        if (vObj.node == null)
                        {
                            if (vObj.prim is Avatar)
                            {
                                
                                AnimatedMesh avmesh = smgr.GetMesh("sydney.md2");

                                AnimatedMeshSceneNode node2 = smgr.AddAnimatedMeshSceneNode(avmesh);
                                node = node2;
                                node.Scale = new Vector3D(0.035f, 0.035f, 0.035f);
                                vObj.node = node2;

                                lock (interpolationTargets)
                                {
                                    if (interpolationTargets.ContainsKey(simhandle.ToString() + vObj.prim.LocalID.ToString()))
                                    {
                                        interpolationTargets[simhandle.ToString() + vObj.prim.LocalID.ToString()] = vObj;
                                    }
                                    else
                                    {
                                        interpolationTargets.Add(simhandle.ToString() + vObj.prim.LocalID.ToString(), vObj);
                                    }
                                }
                            }
                            else
                            {
                                node = smgr.AddMeshSceneNode(vObj.mesh, smgr.RootSceneNode, (int)vObj.prim.LocalID);
                                creatednode = true;
                                vObj.node = node;
                            }
                        }
                        else
                        {
                            node = vObj.node;
                        }

                        //parentObj.node.AddChild(node);
                        node.Scale = new Vector3D(vObj.prim.Scale.X, vObj.prim.Scale.Z, vObj.prim.Scale.Y);

                        //m_log.WarnFormat("[SCALE]: <{0},{1},{2}> = <{3},{4},{5}>", vObj.prim.Scale.X, vObj.prim.Scale.Z, vObj.prim.Scale.Y, parentObj.node.Scale.X, parentObj.node.Scale.Y, parentObj.node.Scale.Z);
                        
                        vObj.prim.Position = vObj.prim.Position * parentObj.prim.Rotation;
                        vObj.prim.Rotation = parentObj.prim.Rotation * vObj.prim.Rotation;

                        node.Position = new Vector3D(WorldoffsetPos.X + parentObj.prim.Position.X + vObj.prim.Position.X, WorldoffsetPos.Z + parentObj.prim.Position.Z + vObj.prim.Position.Z, WorldoffsetPos.Y + parentObj.prim.Position.Y + vObj.prim.Position.Y);
                        
                        //m_log.Warn(vObj.prim.Rotation.ToString());
                        IrrlichtNETCP.Quaternion iqu = new IrrlichtNETCP.Quaternion(vObj.prim.Rotation.X, vObj.prim.Rotation.Z, vObj.prim.Rotation.Y, vObj.prim.Rotation.W);
                        iqu.makeInverse();

                        //IrrlichtNETCP.Quaternion parentrot = new IrrlichtNETCP.Quaternion(parentObj.node.Rotation.X, parentObj.node.Rotation.Y, parentObj.node.Rotation.Z);
                        //parentrot.makeInverse();

                        //parentrot = Cordinate_XYZ_XZY * parentrot;

                        IrrlichtNETCP.Quaternion finalpos = iqu;
                        //IrrlichtNETCP.Quaternion finalpos = parentrot * iqu;

                        finalpos = Cordinate_XYZ_XZY * finalpos;

                        node.Rotation = finalpos.Matrix.RotationDegrees;

                        if (creatednode)
                        {
                            //node.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                            //node.SetMaterialFlag(MaterialFlag.BackFaceCulling, backFaceCulling);
                            //node.SetMaterialFlag(MaterialFlag.GouraudShading, true);
                            //node.SetMaterialFlag(MaterialFlag.Lighting, true);
                            //node.SetMaterialTexture(0, driver.GetTexture("red_stained_wood.tga"));


                            TriangleSelector trisel = smgr.CreateTriangleSelector(vObj.mesh, node);
                            node.TriangleSelector = trisel;
                            lock (mts)
                            {
                                mts.AddTriangleSelector(trisel);
                            }
                            if (vObj.prim.Textures != null)
                            {
                                if (vObj.prim.Textures.DefaultTexture != null)
                                {
                                    if (vObj.prim.Textures.DefaultTexture.TextureID != UUID.Zero)
                                    {
                                        UUID textureID = vObj.prim.Textures.DefaultTexture.TextureID;
                                        if (textureMan != null)
                                            textureMan.RequestImage(textureID, vObj);

                                    }
                                }
                                if (vObj.prim.Textures.FaceTextures != null)
                                {
                                    Primitive.TextureEntryFace[] objfaces = vObj.prim.Textures.FaceTextures;
                                    for (int i2 = 0; i2 < objfaces.Length; i2++)
                                    {
                                        if (objfaces[i2] == null)
                                            continue;
                                        UUID textureID = objfaces[i2].TextureID;

                                        if (textureID != UUID.Zero)
                                        {
                                            if (textureMan != null)
                                                textureMan.RequestImage(textureID, vObj);
                                        }
                                    }
                                }
                            }
                        }

                        node.UpdateAbsolutePosition();
                    }
                    else
                    {
                        m_log.Warn("[CHILDOBJ]: Found Parent Object but it doesn't have a SceneNode, Skipping");
                        UnAssignedChildObjectModQueue.Enqueue(vObj);
                    }
                }
                else
                {
                    UnAssignedChildObjectModQueue.Enqueue(vObj);
                }
            }
        }

        public void doProcessMesh(int pObjects)
        {
            bool sculptYN = false;
            TextureExtended sculpttex = null;

            for (int i = 0; i < pObjects; i++)
            {
                VObject vobj = null;
                lock (objectMeshQueue)
                {
                    if (objectMeshQueue.Count == 0)
                        break;
                    vobj = objectMeshQueue.Dequeue();
                }

                if (textureMan != null)
                {
                    if (vobj.prim.Sculpt.SculptTexture != UUID.Zero)
                    {
                        m_log.Warn("[SCULPT]: Got Sculpt");
                        if (!textureMan.tryGetTexture(vobj.prim.Sculpt.SculptTexture, out sculpttex))
                        {
                            m_log.Warn("[SCULPT]: Didn't have texture, requesting it");
                            textureMan.RequestImage(vobj.prim.Sculpt.SculptTexture, vobj);
                            //Sculpt textures will cause the prim to get put back into the Mesh objects queue
                            continue;
                        }
                        else
                        {
                            m_log.Warn("[SCULPT]: have texture, setting sculpt to true");
                            sculptYN = true;
                        }
                    }
                }
                else
                {
                    sculptYN = false;
                }

                if (sculptYN == false || sculpttex == null)
                {
                    vobj.mesh = PrimMesherG.PrimitiveToIrrMesh(vobj.prim);
                }
                else
                {
                    float LOD = 32f;
                    
                    if (sculpttex.DOTNETImage.Width < 32f) LOD = sculpttex.DOTNETImage.Width;
                    if (sculpttex.DOTNETImage.Height < 32f && sculpttex.DOTNETImage.Height < LOD ) LOD = sculpttex.DOTNETImage.Height;
                    if (LOD < 32f && LOD > 16f) LOD = 32;
                    if (LOD < 16f && LOD > 8f) LOD = 16;
                    if (LOD < 8f && LOD > 4f) LOD = 8;
                    if (LOD < 4 && LOD > 2) LOD = 4;
                    if (LOD < 2) LOD = 2;

                    m_log.Warn("[SCULPT]: Resizing Sculptie......");
                    SculptMeshLOD smLOD = new SculptMeshLOD(sculpttex.DOTNETImage,LOD);
                    m_log.Warn("[SCULPT]: Meshing Sculptie......");
                    vobj.mesh = PrimMesherG.SculptIrrMesh(smLOD.ResultBitmap);
                    smLOD.Dispose();
                    m_log.Warn("[SCULPT]: Sculptie Meshed");
                    
                }
      
                ulong regionHandle = vobj.prim.RegionHandle;

                if (vobj.prim.ParentID != 0)
                {
                    bool foundEntity = false;

                    lock (Entities)
                    {
                        if (!Entities.ContainsKey(regionHandle.ToString() + vobj.prim.ParentID.ToString()))
                        {
                            UnAssignedChildObjectModQueue.Enqueue(vobj);
                        }
                        else
                        {
                            foundEntity = true;
                        }
                    }

                    if (foundEntity)
                    {
                        vobj.updateFullYN = true;
                        enqueueVObject(vobj);
                    }
                }
                else
                {
                    
                    vobj.updateFullYN = true;
                    enqueueVObject(vobj);
                }
            }
        }

        public void doAnimationFrame()
        {
            lock (Avatars)
            {
                foreach (UUID avatarID in Avatars.Keys)
                {
                    VObject avobj = Avatars[avatarID];
                    
                    if (avobj.prim.ID.ToString().Contains("dead"))
                        continue;

                    if (avobj.mesh != null)
                    {

                    }
                }
            }
        }

        #endregion

       
        #region Console
        /// <summary>
        /// Set the level of log notices being echoed to the console
        /// </summary>
        /// <param name="setParams"></param>
        private void SetConsoleLogLevel(string[] setParams)
        {
            ILoggerRepository repository = LogManager.GetRepository();
            IAppender[] appenders = repository.GetAppenders();
            IdealistViewerAppender consoleAppender = null;

            foreach (IAppender appender in appenders)
            {
                if (appender.Name == "Console")
                {
                    consoleAppender = (IdealistViewerAppender)appender;
                    break;
                }
            }

            if (null == consoleAppender)
            {
                Notice("No appender named Console found (see the log4net config file for this executable)!");
                return;
            }

            if (setParams.Length > 0)
            {
                Level consoleLevel = repository.LevelMap[setParams[0]];
                if (consoleLevel != null)
                    consoleAppender.Threshold = consoleLevel;
                else
                    Notice(
                        String.Format(
                            "{0} is not a valid logging level.  Valid logging levels are ALL, DEBUG, INFO, WARN, ERROR, FATAL, OFF",
                            setParams[0]));
            }

            // If there is no threshold set then the threshold is effectively everything.
            Level thresholdLevel
                = (null != consoleAppender.Threshold ? consoleAppender.Threshold : log4net.Core.Level.All);

            Notice(String.Format("Console log level is {0}", thresholdLevel));
        }


        /// <summary>
        /// Runs commands issued by the server console from the operator
        /// </summary>
        /// <param name="command">The first argument of the parameter (the command)</param>
        /// <param name="cmdparams">Additional arguments passed to the command</param>
        public virtual void RunCmd(string command, string[] cmdparams)
        {
            switch (command)
            {
                case "help":
                    ShowHelp(cmdparams);
                    Notice("");
                    break;

                case "set":
                    Set(cmdparams);
                    break;

                case "show":
                    if (cmdparams.Length > 0)
                    {
                        Show(cmdparams);
                    }
                    break;

                case "quit":
                case "shutdown":
                    Shutdown();
                    break;
            }
        }

        /// <summary>
        /// Set an OpenSim parameter
        /// </summary>
        /// <param name="setArgs">
        /// The arguments given to the set command.
        /// </param>
        public virtual void Set(string[] setArgs)
        {
            // Temporary while we only have one command which takes at least two parameters
            if (setArgs.Length < 2)
                return;

            if (setArgs[0] == "log" && setArgs[1] == "level")
            {
                string[] setParams = new string[setArgs.Length - 2];
                Array.Copy(setArgs, 2, setParams, 0, setArgs.Length - 2);

                SetConsoleLogLevel(setParams);
            }
        }

        /// <summary>
        /// Show help information
        /// </summary>
        /// <param name="helpArgs"></param>
        protected virtual void ShowHelp(string[] helpArgs)
        {
            if (helpArgs.Length == 0)
            {
                Notice("");
                // TODO: not yet implemented
                //Notice("help [command] - display general help or specific command help.  Try help help for more info.");
                Notice("quit - equivalent to shutdown.");

                Notice("set log level [level] - change the console logging level only.  For example, off or debug.");
                Notice("show info - show server information (e.g. startup path).");

                //if (m_stats != null)
                //    Notice("show stats - show statistical information for this server");

                Notice("show threads - list tracked threads");
                Notice("show uptime - show server startup time and uptime.");
                Notice("show version - show server version.");
                Notice("shutdown - shutdown the server.\n");

                return;
            }
        }

        /// <summary>
        /// Outputs to the console information about the region
        /// </summary>
        /// <param name="showParams">
        /// What information to display (valid arguments are "uptime", "users", ...)
        /// </param>
        public virtual void Show(string[] showParams)
        {
            switch (showParams[0])
            {
                case "info":
                    Notice("Version: " + m_version);
                    Notice("Startup directory: " + m_startupDirectory);
                    break;



                case "version":
                    Notice("Version: " + m_version);
                    break;
            }
        }

        /// <summary>
        /// Console output is only possible if a console has been established.
        /// That is something that cannot be determined within this class. So
        /// all attempts to use the console MUST be verified.
        /// </summary>
        private void Notice(string msg)
        {
            if (m_console != null)
            {
                m_console.Notice(msg);
            }
        }

        #endregion

        #region Startup
       
        /// <summary>
        /// Performs initialisation of the scene, such as loading configuration from disk.
        /// </summary>
        public virtual void Startup()
        {
            m_log.Info("[STARTUP]: Beginning startup processing");

            m_version = Util.EnhanceVersionInformation();

            m_log.Info("[STARTUP]: Version: " + m_version + "\n");

            StartupSpecific();

            TimeSpan timeTaken = DateTime.Now - m_startuptime;

            m_log.InfoFormat("[STARTUP]: Startup took {0}m {1}s", timeTaken.Minutes, timeTaken.Seconds);
        }

        /// <summary>
        /// Must be overriden by child classes for their own server specific startup behaviour.
        /// </summary>
        protected void StartupSpecific()
        {

            m_console = new ConsoleBase("Region", this);
            IConfig cnf = m_config.Source.Configs["Startup"];
            string loginURI = "http://127.0.0.1:9000/";
            string firstName = string.Empty;
            string lastName = string.Empty;
            string password = string.Empty;
            string startlocation = "";
            bool loadtextures = true;

            if (cnf != null)
            {
                loginURI = cnf.GetString("login_uri", "");
                firstName = cnf.GetString("first_name", "test");
                lastName = cnf.GetString("last_name", "user");
                password = cnf.GetString("pass_word", "nopassword");
                loadtextures = cnf.GetBoolean("load_textures", true);
                backFaceCulling = cnf.GetBoolean("backface_culling", backFaceCulling);
                avatarMesh = cnf.GetString("avatar_mesh", avatarMesh);
                avatarMaterial = cnf.GetString("avatar_material", avatarMaterial);
                startlocation = cnf.GetString("start_location", "");
            }
            loadTextures = loadtextures;
            MainConsole.Instance = m_console;

            avatarConnection = new SLProtocol();
            avatarConnection.OnLandPatch += landPatchCallback;
            avatarConnection.OnGridConnected += connectedCallback;
            avatarConnection.OnNewPrim += newPrimCallback;
            avatarConnection.OnSimConnected += SimConnectedCallback;
            avatarConnection.OnObjectUpdated += objectUpdatedCallback;
            avatarConnection.OnObjectKilled += objectKilledCallback;
            avatarConnection.OnNewAvatar += newAvatarCallback;

            guithread = new Thread(new ParameterizedThreadStart(startupGUI));
            guithread.Start();

            


            IrrlichtNETCP.Matrix4 m4 = new IrrlichtNETCP.Matrix4();
            m4.SetM(0, 0, 1);
            m4.SetM(1, 0, 0);
            m4.SetM(2, 0, 0);
            m4.SetM(3, 0, 0);
            m4.SetM(0, 1, 0);
            m4.SetM(1, 1, 0);
            m4.SetM(2, 1, 1);
            m4.SetM(3, 1, 0);
            m4.SetM(0, 2, 0);
            m4.SetM(1, 2, 1);
            m4.SetM(2, 2, 0);
            m4.SetM(3, 2, 0);
            m4.SetM(0, 3, 0);
            m4.SetM(1, 3, 0);
            m4.SetM(2, 3, 0);
            m4.SetM(3, 3, 1);


            Cordinate_XYZ_XZY = new IrrlichtNETCP.Quaternion(m4);
            Cordinate_XYZ_XZY.makeInverse();
            //Cordinate_XYZ_XZY = (CoordinateConversion.

            
            avatarConnection.BeginLogin(loginURI, firstName + " " + lastName, password, startlocation);
            //base.StartupSpecific();
        }

        #endregion

        #region ShutDown
        /// <summary>
        /// Should be overriden and referenced by descendents if they need to perform extra shutdown processing
        /// </summary>      
        public virtual void Shutdown()
        {
            ShutdownSpecific();

            m_log.Info("[SHUTDOWN]: Shutdown processing on main thread complete.  Exiting...");

            Environment.Exit(0);
        }
        /// <summary>
        /// Should be overriden and referenced by descendents if they need to perform extra shutdown processing
        /// </summary>      
        protected void ShutdownSpecific()
        {
            device.Close();
            device.Dispose();
            avatarConnection.Logout();
            //base.ShutdownSpecific();
        }

        #endregion


        public void UpdateTerrain()
        {
            lock (m_dirtyTerrain)
            {
                while (m_dirtyTerrain.Count > 0)
                {
                    ulong regionhandle = m_dirtyTerrain.Dequeue();
                    //m_log.Warn("[TERRAIN]: RegionHandle:" + regionhandle.ToString());
                    string filename = "myterrain1" + regionhandle.ToString() + ".bmp";
                    string path = Util.MakePath("media", "materials", "textures", filename);
                    System.Drawing.Bitmap terrainbmp = terrainBitmap[regionhandle];
                    lock (terrainbmp)
                    {
                        Util.SaveBitmapToFile(terrainbmp, m_startupDirectory + "\\" + path);

                    }
                    device.FileSystem.WorkingDirectory = m_startupDirectory + "\\" + Util.MakePath("media", "materials", "textures", "");
                    TerrainSceneNode terrain = null;
                    lock (terrains)
                    {
                        if (terrains.ContainsKey(regionhandle))
                        {
                            terrain = terrains[regionhandle];
                            terrains.Remove(regionhandle);
                        }
                    }
                    
                    lock (mesh_synclock)
                    {

                        if (terrain != null)
                        {
                            smgr.AddToDeletionQueue(terrain);

                        }
                        Vector3 relTerrainPos = Vector3.Zero;
                        if (currentSim != null)
                        {
                            if (currentSim.Handle != regionhandle)
                            {
                                Vector3 Offsetcsg = Util.OffsetGobal(currentSim.Handle, Vector3.Zero);
                                Vector3 Offsetnsg = Util.OffsetGobal(regionhandle, Vector3.Zero);
                                relTerrainPos = Offsetnsg - Offsetcsg;
                            }
                        }
                        terrain = smgr.AddTerrainSceneNode(
                        filename, smgr.RootSceneNode, -1,
                        new Vector3D(relTerrainPos.X - 4f, relTerrainPos.Z, relTerrainPos.Y + 16f), new Vector3D(0, 270, 0), new Vector3D(1, 1, 1), new Color(255, 255, 255, 255), 2, TerrainPatchSize.TPS17);
                        //device.FileSystem.WorkingDirectory = "./media/";
                        terrain.SetMaterialFlag(MaterialFlag.Lighting, true);
                        terrain.SetMaterialType(MaterialType.DetailMap);
                        terrain.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                        terrain.SetMaterialTexture(0, driver.GetTexture("Green_Grass_Detailed.tga"));
                        //terrain.SetMaterialTexture(1, driver.GetTexture("detailmap3.jpg"));
                        terrain.ScaleTexture(16, 16);
                        terrain.Scale = new Vector3D(1.0275f, 1, 1.0275f);
                    }

                    lock (terrains)
                    {
                        terrains.Add(regionhandle, terrain);
                    }

                    TriangleSelector terrainsel;
                    lock (terrainsels)
                    {
                        if (terrainsels.ContainsKey(regionhandle))
                        {
                            mts.RemoveTriangleSelector(terrainsels[regionhandle]);
                            terrainsels.Remove(regionhandle);
                        }
                    }

                    terrainsel = smgr.CreateTerrainTriangleSelector(terrain, 1);
                    terrain.TriangleSelector = terrainsel;
                    lock (terrainsels)
                    {
                        terrainsels.Add(regionhandle, terrainsel);

                    }
                    mts.AddTriangleSelector(terrainsel);
                    //Vector3D terrainpos = terrain.TerrainCenter;
                    //terrainpos.Z = terrain.TerrainCenter.Z - 100f;
                    //terrain.Position = terrainpos;
                    //m_log.DebugFormat("[TERRAIN]:<{0},{1},{2}>", terrain.TerrainCenter.X, terrain.TerrainCenter.Y, terrain.TerrainCenter.Z);
                    //terrain.ScaleTexture(1f, 1f);
                    if (currentSim != null)
                    {
                        if (currentSim.Handle == regionhandle)
                        {
                            cam.SNCamera.Target = terrain.TerrainCenter;
                            cam.UpdateCameraPosition();
                        }
                    }

                }
            }
        }

        #region LibOMV Callbacks

        


        public void newPrimCallback(Simulator sim, Primitive prim, ulong regionHandle,
                                      ushort timeDilation)
        {
            //System.Console.WriteLine(prim.ToString());
            //return;
            VObject newObject = null;

            //bool foundEntity = false;

            lock (Entities)
            {
                if (Entities.ContainsKey(regionHandle.ToString() + prim.LocalID.ToString()))
                {
                    //foundEntity = true;
                    newObject = Entities[regionHandle.ToString() + prim.LocalID.ToString()];
                }
            }
            if (newObject != null)
            {
                if (newObject.node != null)
                {
                    smgr.AddToDeletionQueue(newObject.node);
                    newObject.node = null;
                }

            }

            
            newObject = VUtil.NewVObject(prim,newObject);
            

                
            
            lock (objectMeshQueue)
            {
                objectMeshQueue.Enqueue(newObject);
            }

        }

        private void landPatchCallback(Simulator sim, int x, int y, int width, float[] data)
        {
            ulong simhandle = sim.Handle;

            if (simhandle == 0)
                simhandle = TESTNEIGHBOR;
            
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
            lock (m_landmaps)
            {
                if (m_landmaps.ContainsKey(simhandle))
                    m_landMap = m_landmaps[simhandle];
                else
                {
                    m_log.Warn("[TERRAIN]: Warning landmap update XY for land that isn't found");
                    return;
                }
            }


            lock (m_landMap)
            {
                if (!m_landMap.ContainsKey(y * 16 + x))
                {
                    m_landMap.Add(y * 16 + x, data);
                }
                else
                {
                    m_landMap[y * 16 + x] = data;
                }
            }

            updateTerrainBitmap(x, y, sim);

            lock (m_dirtyTerrain)
            {
                if (!m_dirtyTerrain.Contains(simhandle))
                {
                    m_dirtyTerrain.Enqueue(simhandle);
                }
            }
            
        }

        private void updateTerrainBitmap(int x, int y, Simulator sim)
        {
            
            Dictionary<int, float[]> m_landMap;
            ulong simhandle = sim.Handle;

            if (simhandle == 0)
                simhandle = TESTNEIGHBOR;
            
            lock (m_landmaps)
            {
                if (m_landmaps.ContainsKey(simhandle))
                {
                    m_landMap = m_landmaps[simhandle];
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

            lock (terrainBitmap)
            {
                if (terrainBitmap.ContainsKey(simhandle))
                {
                    terrainbitmap = terrainBitmap[simhandle];
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
                        col *= 0.00388f;
                        
                        
                        //m_log.Debug("[COLOR]: " + currentPatch[cy * 16 + cx].ToString());
                        //terrainbitmap.SetPixel(bitmapy, bitmapx, System.Drawing.Color.FromArgb(col, col, col));
                        terrainbitmap.SetPixel(bitmapy, bitmapx, Util.FromArgbf(1,col,col,col));
                    }
                }
            }
        }
        protected void connectedCallback()
        {

        }
        protected void SimConnectedCallback(Simulator sim)
        {
            bool isCurrentSim = false;
            ulong simhandle = sim.Handle;
            m_log.Warn("Connected to sim with:" + simhandle);

            if (simhandle == 0)
                simhandle = TESTNEIGHBOR;

            

            if (currentSim == null)
            {
                currentSim = sim;
                isCurrentSim = true;
            }
            else
            {
                if (currentSim.Handle == simhandle)
                {
                    isCurrentSim = true;
                }
            }
            

            lock (Simulators)
            {
                if (!Simulators.ContainsKey(simhandle))
                {
                    Simulators.Add(simhandle, sim);
                    if (!terrainBitmap.ContainsKey(simhandle))
                    {
                        terrainBitmap.Add(simhandle, new System.Drawing.Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format24bppRgb));
                        Dictionary<int, float[]> m_landMap = new Dictionary<int, float[]>();
                        if (!m_landmaps.ContainsKey(simhandle))
                        {
                            m_landmaps.Add(simhandle, m_landMap);
                        }
                        lock (m_dirtyTerrain)
                        {
                            if (!m_dirtyTerrain.Contains(simhandle))
                            {
                                m_dirtyTerrain.Enqueue(simhandle);
                            }
                        }
                    }
                }
            }
            if (isCurrentSim)
            {
                SNGlobalwater.Position = new Vector3D(0, sim.WaterHeight-0.5f, 0);
                //SNGlobalwater.Position = new Vector3D(0, sim.WaterHeight - 50.5f, 0);
                // TODO REFIX!
            }
        }

        private void objectUpdatedCallback(Simulator simulator, ObjectUpdate update, ulong regionHandle, 
            ushort timeDilation)
        {
            VObject obj = null;
            //if (!update.Avatar)
            //{
                lock (Entities)
                {
                    if (Entities.ContainsKey(regionHandle.ToString() + update.LocalID.ToString()))
                    {
                        obj = Entities[regionHandle.ToString() + update.LocalID.ToString()];
                        if (obj.prim is Avatar)
                        {
                            if (obj.node != null)
                            {
                                obj.updateFullYN = false;
                            }
                            
                        }
                        else
                        {
                            obj.updateFullYN = false;
                        }

                        obj.prim.Acceleration = update.Acceleration;
                        obj.prim.AngularVelocity = update.AngularVelocity;
                        obj.prim.CollisionPlane = update.CollisionPlane;
                        obj.prim.Position = update.Position;
                        obj.prim.Rotation = update.Rotation;
                        obj.prim.PrimData.State = update.State;
                        obj.prim.Textures = update.Textures;
                        obj.prim.Velocity = update.Velocity;
                        
                        Entities[regionHandle.ToString() + update.LocalID.ToString()] = obj;
                    }
                }
                if (obj != null)
                {
                    if (obj.prim is Avatar)
                    {
                        if (obj.node != null)
                        {
                            lock (objectModQueue)
                            {
                                objectModQueue.Enqueue(obj);
                            }
                        }
                    }
                    else
                    {
                        enqueueVObject(obj);
                    }
                }
            //}
        }
        private void objectKilledCallback(Simulator psim, uint pLocalID)
        {
            ulong regionHandle = psim.Handle;
            m_log.Debug("[DELETE]: obj " + regionHandle.ToString() + ":" + pLocalID.ToString());
            VObject obj = null;

            
            lock (Entities)
            {

                if (Entities.ContainsKey(regionHandle.ToString() + pLocalID.ToString()))
                {
                    obj = Entities[regionHandle.ToString() + pLocalID.ToString()];

                    

                    if (obj.node != null)
                    {
                        lock (interpolationTargets)
                        {
                            if (interpolationTargets.ContainsKey(regionHandle.ToString() + obj.prim.LocalID.ToString()))
                            {
                                interpolationTargets.Remove(regionHandle.ToString() + obj.prim.LocalID.ToString());
                            }

                        }
                        if (cam.SNtarget == obj.node)
                            cam.SNtarget = null;
                        
                        smgr.AddToDeletionQueue(obj.node);
                        obj.node = null;
                        
                    }
                    Entities.Remove(regionHandle.ToString() + pLocalID.ToString());
                }
            }
            if (obj != null)
            {
                if (obj.prim is Avatar)
                {
                    lock (Avatars)
                    {
                        if (Avatars.ContainsKey(obj.prim.ID))
                        {
                            Avatars.Remove(obj.prim.ID);
                        }
                    }
                }
            }
        }

        private void newAvatarCallback(Simulator sim, Avatar avatar, ulong regionHandle,
                                       ushort timeDilation)
        {
            VObject avob = null; 
            
            lock (Avatars)
            {
                if (Avatars.ContainsKey(avatar.ID))
                {
                    Avatars[avatar.ID] = avob;
                }
                else
                {
                    Avatars.Add(avatar.ID, avob);
                }
            }
            lock (Entities)
            {
                if (Entities.ContainsKey(regionHandle.ToString() + avatar.LocalID.ToString()))
                {
                    VObject existingob = Entities[regionHandle.ToString() + avatar.LocalID.ToString()];
                    existingob.prim = avatar;
                    Entities[regionHandle.ToString() + avatar.LocalID.ToString()] = existingob;
                    avob = existingob;
                }
                else
                {
                    avob = new VObject();
                    avob.prim = avatar;
                    avob.mesh = null;
                    avob.node = null;
                    Entities.Add(regionHandle.ToString() + avatar.LocalID.ToString(), avob);
                    
                }
            }
            lock (objectModQueue)
            {
                avob.updateFullYN = true;
                objectModQueue.Enqueue(avob);
            }
        }

        #endregion

        #region KeyActions

        private void processHeldKeys()
        {
            lock (m_heldKeys)
            {
                foreach (KeyCode ky in m_heldKeys)
                {
                    doHeldKeyActions(ky);
                }

            }

        }

        private void doHeldKeyActions(KeyCode ky)
        {
            switch (ky)
            {
                case KeyCode.Up:
                    if (!shiftHeld && !ctrlHeld)
                    {

                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

                case KeyCode.Down:
                    if (!shiftHeld && !ctrlHeld)
                    {

                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

                case KeyCode.Left:
                    if (!shiftHeld && !ctrlHeld)
                    {

                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            //vOrbit.X -= 2f;
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

                case KeyCode.Right:
                    if (!shiftHeld && !ctrlHeld)
                    {

                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            //vOrbit.X += 2f;
                            //vOrbit.X -= 2f;
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;
                case KeyCode.Prior:
                    if (!shiftHeld && !ctrlHeld)
                    {

                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            //vOrbit.Y -= 2f;
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

                case KeyCode.Next:
                    if (!shiftHeld && !ctrlHeld)
                    {

                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

            }

        }

        public void doKeyHeldStore(KeyCode ky, bool held)
        {
            lock (m_heldKeys)
            {
                if (held)
                {
                    if (!m_heldKeys.Contains(ky))
                    {
                        m_heldKeys.Add(ky);
                    }
                }
                else
                {
                    if (m_heldKeys.Contains(ky))
                    {
                        m_heldKeys.Remove(ky);
                    }
                }

            }
        }

        #endregion

        public bool device_OnEvent(Event p_event)
        {
            processHeldKeys();

            if (p_event.Type != EventType.MouseInputEvent)
            {
                if (p_event.Type == EventType.KeyInputEvent)
                {
                    
                    switch (p_event.KeyCode)
                    {
                        case KeyCode.Control:
                            ctrlHeld = p_event.KeyPressedDown;
                            if (ctrlHeld)
                            {
                                cam.ResetMouseOffsets();
                            }
                            else
                            {
                                cam.ApplyMouseOffsets();
                            }
                            break;
                        case KeyCode.Shift:
                            shiftHeld = p_event.KeyPressedDown;
                            break;
                        case KeyCode.Up:
                        case KeyCode.Down:
                        case KeyCode.Left:
                        case KeyCode.Right:
                        case KeyCode.Prior:
                        case KeyCode.Next:
                            doKeyHeldStore(p_event.KeyCode,p_event.KeyPressedDown);
                            break;
                    }
                }
            }

            if (p_event.Type == EventType.MouseInputEvent)
            {
                return MouseEventProcessor(p_event);
            }

            return true;
        }
        #region Mouse Handler
        public bool MouseEventProcessor(Event p_event)
        {
            //m_log.DebugFormat("[MOOSE]:<{0},{1}>",p_event.MousePosition.X,p_event.MousePosition.Y);
            if (p_event.MouseInputEvent == MouseInputEvent.MouseWheel)
            {
                //KeyCode.RButton
                cam.MouseWheelAction(p_event.MouseWheelDelta);
                

            }
            if (p_event.MouseInputEvent == MouseInputEvent.LMouseLeftUp)
            {
                if (ctrlHeld)
                {
                    //if (loMouseOffsetPHI != 0 || loMouseOffsetTHETA != 0)
                    //{

                    cam.ApplyMouseOffsets();
                    //}
                }
                LMheld = false;
            }
            if (p_event.MouseInputEvent == MouseInputEvent.LMousePressedDown)
            {

                LMheld = true;
                if (ctrlHeld)
                {
                    //OldMouseX = 0;
                    // OldMouseY = 0;
                    cam.ResetMouseOffsets();
                    Vector3D[] projection = cam.ProjectRayPoints(p_event.MousePosition, WindowWidth_DIV2,WindowHeight_DIV2, aspect);
                    Line3D projectedray = new Line3D(projection[0], projection[1]);

                    Vector3D collisionpoint = new Vector3D(0, 0, 0);
                    Triangle3D tri = new Triangle3D(0, 0, 0, 0, 0, 0, 0, 0, 0);
                    SceneNode node = smgr.CollisionManager.GetSceneNodeFromRay(projectedray, 0x0128, true); //smgr.CollisionManager.GetSceneNodeFromScreenCoordinates(new Position2D(p_event.MousePosition.X, p_event.MousePosition.Y), 0, false);
                    if (node == null)
                    {
                        m_log.Warn("[PICKER]: Picked null");
                    }
                    else
                    {
                        m_log.WarnFormat("[PICK]: Picked <{0},{1},{2}>",node.Position.X,node.Position.Y,node.Position.Z);
                        if (node.Position.X == 0 && node.Position.Z == 0)
                        {
                            if (smgr.CollisionManager.GetCollisionPoint(projectedray, mts, out collisionpoint, out tri))
                            {
                                
                                //if (collisionpoint != null)
                                //{
                                //m_log.DebugFormat("Found point: <{0},{1},{2}>", collisionpoint.X, collisionpoint.Y, collisionpoint.Z);
                                //}
                                cam.SetTarget(collisionpoint);
                                cam.SNtarget = null;
                            }
                        }
                        else
                        {
                            
                            cam.SetTarget(node.Position);
                            cam.SNtarget = node;
                        }
                    }
                    
                    
                    //else
                    //{
                        //if (smgr.CollisionManager.GetCollisionPoint(projectedray, terrainsel, out collisionpoint, out tri))
                        //{
                            //if (collisionpoint != null)
                            //{
                            //m_log.DebugFormat("Found point: <{0},{1},{2}>", collisionpoint.X, collisionpoint.Y, collisionpoint.Z);
                            //}
                          //  cam.SetTarget(collisionpoint);
                        //}
                    //}
                }
            }
            if (p_event.MouseInputEvent == MouseInputEvent.RMouseLeftUp)
            {
                RMheld = false;
            }
            if (p_event.MouseInputEvent == MouseInputEvent.RMousePressedDown)
            {
                RMheld = true;
            }

            if (p_event.MouseInputEvent == MouseInputEvent.MouseMoved)
            {

                //float pos1 = (float)((p_event.MousePosition.X / WindowWidth_DIV2 - 1.0f) / aspect);
                //float pos2 = (float)(1.0f - p_event.MousePosition.Y / WindowHeight_DIV2);

                if (LMheld && ctrlHeld)
                {

                    int deltaX = p_event.MousePosition.X - OldMouseX;
                    int deltaY = p_event.MousePosition.Y - OldMouseY;

                    //loMouseOffsetTHETA = loMouseOffsetTHETA + (deltaX * CAMERASPEED);
                    //loMouseOffsetPHI = loMouseOffsetPHI + (deltaY * CAMERASPEED);
                    cam.SetDeltaFromMouse(deltaX, deltaY);

                    
                    // m_log.DebugFormat("pos1:{0}, pos2{1}", deltaX, deltaY);

                }
                OldMouseX = p_event.MousePosition.X;
                OldMouseY = p_event.MousePosition.Y;

            }
            return true;

        }

        #endregion

        public void textureCompleteCallback(string tex, VObject vObj, UUID AssetID)
        {
            TextureComplete tx = new TextureComplete();
            tx.texture = tex;
            tx.vObj = vObj;
            tx.textureID = AssetID;
            assignTextureQueue.Enqueue(tx);
        }
    }
    
    public struct TextureComplete 
    {
        public VObject vObj;
        public string texture;
        public UUID textureID;
        
    }
}
