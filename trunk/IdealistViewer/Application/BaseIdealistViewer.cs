//#define DebugObjectPipeline
#define DebugTexturePipeline

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using IrrlichtNETCP;
using IrrlichtNETCP.Extensions;
using OpenMetaverse;
using Nini.Config;
using PrimMesher;

namespace IdealistViewer
{
    public class BaseIdealistViewer : conscmd_callback
    {
        public IdealistViewerConfigSource m_config = null;
        public static Dictionary<string, UUID> waitingSculptQueue = new Dictionary<string, UUID>();
        public static bool backFaceCulling = true;
        public GUIFont defaultfont = null;
        /// <summary>
        /// Irrlicht Instance.  A handle to the Irrlicht device
        /// </summary>
        static IrrlichtDevice device;

        /// <summary>
        /// Irrlicht Video Driver.  A handle to the video driver being used
        /// </summary>
        private VideoDriver driver = null;

        /// <summary>
        /// A handle to the Irrlicht ISceneManager 
        /// </summary>
        private SceneManager smgr = null;

        /// <summary>
        /// A Handle to the Irrlicht Gui manager
        /// </summary>
        private GUIEnvironment guienv = null;

        /// <summary>
        /// Standard Log4Net setup.
        /// </summary>
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Thread for running the GUI in.
        /// </summary>
        private Thread guithread;
        
        /// <summary>
        /// Bitmaps for the terrain in each active region indexed by ulong regionhandle
        /// </summary>
        private Dictionary<ulong, System.Drawing.Bitmap> terrainBitmap = new Dictionary<ulong, System.Drawing.Bitmap>();
        
        /// <summary>
        /// LibOMV Connection
        /// </summary>
        private SLProtocol avatarConnection;

        /// <summary>
        /// TerrainSceneNode Irrlicht representations of the terrain.  Indexed by regionhandle
        /// </summary>
        private Dictionary<ulong,TerrainSceneNode> terrains = new Dictionary<ulong,TerrainSceneNode>();
        
        /// <summary>
        /// Simulator patch storage Indexed by regionhandle  Contains a dictionary with the patch number as an index.
        /// </summary>
        private Dictionary<ulong, Dictionary<int, float[]>> m_landmaps = new Dictionary<ulong, Dictionary<int, float[]>>();
        
        /// <summary>
        /// Terrain Triangle Selectors indexed by ulong regionhandle.  Used for the Picker
        /// </summary>
        private Dictionary<ulong, TriangleSelector> terrainsels = new Dictionary<ulong,TriangleSelector>();
        
        /// <summary>
        /// Future water use
        /// </summary>
        private static SceneNode SNGlobalwater;

        /// <summary>
        /// Combines triangle selectors for all of the objects in the scene.  A reference to the triangles
        /// </summary>
        private static MetaTriangleSelector mts;

        /// <summary>
        /// All object modifications run through this Queue
        /// </summary>
        private static Queue<VObject> objectModQueue = new Queue<VObject>();

        /// <summary>
        /// All Meshing gets queued up int this queue.
        /// </summary>
        private static Queue<VObject> objectMeshQueue = new Queue<VObject>();

        /// <summary>
        /// foliage (trees, grass, etc. are queued in this queue.
        /// </summary>
        private static Queue<FoliageObject> foliageObjectQueue = new Queue<FoliageObject>();
        

        /// <summary>
        /// Child prim in a prim group where the children don't yet have parents 
        /// rendered get put in here to wait for the parent
        /// </summary>
        private static Queue<VObject> UnAssignedChildObjectModQueue = new Queue<VObject>();
        
        /// <summary>
        /// The texture has completed downloading, put it into this queue for assigning to linked objects
        /// </summary>
        private static Queue<TextureComplete> assignTextureQueue = new Queue<TextureComplete>();


        /// <summary>
        /// All objects that are interpolated get put into this dictionary.  Indexed by VUtil.GetHashId
        /// </summary>
        private static Dictionary<string, VObject> interpolationTargets = new Dictionary<string, VObject>();
        
        /// <summary>
        /// Tester for AV Mesh
        /// </summary>
        private static SkinnedMesh avmeshtest = null;

        /// <summary>
        /// Tester for AV Mesh 2
        /// </summary>
        private static AnimatedMeshSceneNode avmeshsntest = null;

        /// <summary>
        /// Simulator that the client think's it's currently a root agent in.
        /// Uses this to determine the offset of prim and objects in neighbor regions
        /// </summary>
        private static Simulator currentSim;

        /// <summary>
        /// Known Simulators, Indexed by ulong regionhandle
        /// </summary>
        private static Dictionary<ulong, Simulator> Simulators = new Dictionary<ulong, Simulator>();

        /// <summary>
        /// Known Entities.  Indexed by VUtil.GetHashId
        /// </summary>
        private static Dictionary<string, VObject> Entities = new Dictionary<string, VObject>();
        
        /// <summary>
        /// Known Avatars Indexed by Avatar UUID
        /// </summary>
        private static Dictionary<UUID, VObject> Avatars = new Dictionary<UUID, VObject>();

        /// <summary>
        /// This is read in from about.xml and is our mini-instruction manual
        /// </summary>
        static string AboutText = string.Empty;
        static string AboutCaption = string.Empty;
        static string StartUpModelFile = string.Empty;

        private static AvatarController AVControl = null;

        private const int modAVUpdates = 10;


        /// <summary>
        /// Held Controls
        /// </summary>
        private static bool ctrlHeld = false;
        private static bool shiftHeld = false;
        private static bool appHeld = false;
        private static bool LMheld = false;
        private static bool RMheld = false;
        private static bool MMheld = false;

        /// <summary>
        /// Configuration option to load the textures
        /// </summary>
        private static bool loadTextures = true;
        

        /// <summary>
        /// Configuration option to represent the avatar mesh.
        /// </summary>
        private static string avatarMesh = "sydney.md2";

        /// <summary>
        /// Configuration option.  Texture to apply to avatarMesh
        /// </summary>
        private static string avatarMaterial = "sydney.BMP";

        /// <summary>
        /// Stored Mouse cordinates used to determine change.
        /// </summary>
        private int OldMouseX = 0;
        private int OldMouseY = 0;

        /// <summary>
        /// Gui Window Width
        /// </summary>
        private static int WindowWidth = 1024;
        
        /// <summary>
        /// Gui Window Height
        /// </summary>
        private static int WindowHeight = 768;

        /// <summary>
        /// Helper objects to support effective Picking.
        /// </summary>
        private static float WindowWidth_DIV2 = WindowWidth * 0.5f;
        private static float WindowHeight_DIV2 = WindowHeight * 0.5f;
        private static float aspect = (float)WindowWidth / WindowHeight;
        
        /// <summary>
        /// User's Camera
        /// </summary>
        private Camera cam;
        
        /// <summary>
        /// Target Position of the camera
        /// </summary>
        private static Vector3 m_lastTargetPos = Vector3.Zero;

        /// <summary>
        /// List of held keys.  Used to process multiple keypresses simulataniously
        /// </summary>
        private static List<KeyCode> m_heldKeys = new List<KeyCode>();

        private int tickcount = 0;
        private int mscounter = 0;

        private int msreset = 100;
        private uint framecounter = 0;
        private int maxFPS = 30;  // cap frame rate at 30 fps to help keep cpu load down

        /// <summary>
        /// If the neighbor returned is a 0 ulong region handle, use this one for testing
        /// </summary>
        private ulong TESTNEIGHBOR = 1099511628032256;

        /// <summary>
        /// We loop the graphics rendering 10 times per second.
        /// </summary>
        private uint objectmods = 5; // process object queue 2 times a second

        /// <summary>
        /// Use this to ensure that meshing occurs one at a time.
        /// </summary>
        private static Object mesh_synclock = new Object();

        private uint primcount = 0;
        private uint foliageCount = 0;

        /// <summary>
        /// Cordinate Switcher Quaternion XYZ space to XZY space.
        /// </summary>
        public static IrrlichtNETCP.Quaternion Cordinate_XYZ_XZY = new IrrlichtNETCP.Quaternion();

        /// <summary>
        /// Texture manager for the client.
        /// </summary>
        private TextureManager textureMan = null;

        private MeshFactory m_MeshFactory = null;

        /// <summary>
        /// Picker
        /// </summary>
        private static TrianglePickerMapper triPicker = null;
        // experimental mesh code - only here temporarily - up top so it's visible

        /// <summary>
        /// Queue of terrain that needs to be updated.  ulong region handle.
        /// </summary>
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

        private VObject UserAvatar = null;

        private Texture GreenGrassTexture = null;

        private float TimeDilation = 0;

        private float[,] RegionHFArray = new float[256,256];

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

       
       /// <summary>
       /// Starts up Irrlicht for the GUI
       /// </summary>
       /// <param name="o"></param>
        public void startupGUI(object o)
        {

            //Create a New Irrlicht Device
            device = new IrrlichtDevice(DriverType.OpenGL,
                                                     new Dimension2D(WindowWidth, WindowHeight),
                                                    32, false, true, false, false);

            device.WindowCaption = "IdealistViewer 0.001";
            
            // Sets directory to load assets from
            device.FileSystem.WorkingDirectory = m_startupDirectory + "/" + Util.MakePath("media", "materials", "textures", "");  //We set Irrlicht's current directory to %application directory%/media

            
            driver = device.VideoDriver;
            smgr = device.SceneManager;

            GreenGrassTexture = driver.GetTexture("Green_Grass_Detailed.tga");
            
            guienv = device.GUIEnvironment;
            defaultfont = guienv.GetFont("defaultfont.png");
            GUISkin skin = guienv.Skin;
            skin.Font = defaultfont;
            // Set up event handler for the GUI window events.
            device.OnEvent += new OnEventDelegate(device_OnEvent);

            device.Resizeable = true;

            m_MeshFactory = new MeshFactory(smgr.MeshManipulator, device);

            // Set up the picker.
            triPicker = new TrianglePickerMapper(smgr.CollisionManager);
            mts = smgr.CreateMetaTriangleSelector();

            // Only create a texture manager if the user configuration option is enabled for downloading textures
            if (loadTextures)
            {
                textureMan = new TextureManager(device, driver, triPicker, mts, "IdealistCache", avatarConnection);
                textureMan.OnTextureLoaded += textureCompleteCallback;
            }

            AVControl = new AvatarController(avatarConnection, null);

            smgr.SetAmbientLight(new Colorf(0.6f, 0.6f, 0.6f, 0.6f));
            


            // Fog is on by default, this line disables it.
            smgr.VideoDriver.SetFog(new Color(0, 255, 255, 255), false, 9999, 9999, 0, false, false);

            XmlReader xml = XmlReader.Create(
                new StreamReader("../../../media/config.xml"));
            while (xml != null && xml.Read())
            {
                switch (xml.NodeType)
                {
                    case XmlNodeType.Text:
                        AboutText = xml.ReadContentAsString();
                        break;
                    case XmlNodeType.Element:
                        if (xml.Name.Equals("startUpModel"))
                            StartUpModelFile = xml.GetAttribute("file");
                        else if (xml.Name.Equals("messageText"))
                            AboutCaption = xml.GetAttribute("caption");
                        break;
                }
            }

            // Create the Skybox
            /*
            driver.SetTextureFlag(TextureCreationFlag.CreateMipMaps, false);

            smgr.AddSkyBoxSceneNode(null, new Texture[] {
                driver.GetTexture("irrlicht2_up.jpg"),
                driver.GetTexture("irrlicht2_dn.jpg"),
                driver.GetTexture("irrlicht2_rt.jpg"),
                driver.GetTexture("irrlicht2_lf.jpg"),
                driver.GetTexture("irrlicht2_ft.jpg"),
                driver.GetTexture("irrlicht2_bk.jpg")}, 0);

            driver.SetTextureFlag(TextureCreationFlag.CreateMipMaps, true);
            */
            ATMOSkySceneNode skynode = new ATMOSkySceneNode(driver.GetTexture("irrlicht2_up.jpg"), null, smgr, 20, -1);
            
            // Create User's Camera
            cam = new Camera(smgr);

            // Set up Scene Lighting.
            // This light rotates around to highlight prim meshing issues.
            //SceneNode light = smgr.AddLightSceneNode(smgr.RootSceneNode, new Vector3D(0, 0, 0), new Colorf(1, 0.2f, 0.2f, 0.2f), 90, -1);
           // Animator anim = smgr.CreateFlyCircleAnimator(new Vector3D(128, 250, 128), 250.0f, 0.0010f);
            //light.AddAnimator(anim);
            //anim.Dispose();

            // This light simulates the sun
            SceneNode light2 = smgr.AddLightSceneNode(smgr.RootSceneNode, new Vector3D(0, 255, 0), new Colorf(1, 0.25f, 0.25f, 0.25f), 250, -1);

            // Set up the water
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
            
            /*
            WaterSceneNode water = new WaterSceneNode(null, smgr, new Dimension2Df(180, 180), new Dimension2D(100, 100), new Dimension2D(512, 512));
                            water.Position = new Vector3D(0, 30, 0);
                            //water.WaveDisplacement /= 1.0f;
                            water.WaveHeight *= .5f;
                            //water.WaveSpeed *= 1;
                            water.RefractionFactor = 0.21f;
                            //water.WaveLength *= 1;
                            //water.WaveRepetition = 1;
            */
//            GUIContextMenu gcontext = guienv.AddMenu(guienv.RootElement, -1);
//            gcontext.Text = "Some Text";
//            gcontext.AddItem("SomeCooItem", -1, true, true);
//            GUIContextMenu submenu;
//            submenu = gcontext.GetSubMenu(0);
//            submenu.AddItem("Weird!", 100, true, false);

            // create menu toplevel and submenu items. No event handlers yet - ckrinke

            GUIContextMenu menu = guienv.AddMenu(guienv.RootElement, -1);
            menu.AddItem("File", -1, true, true);
            menu.AddItem("View", -1, true, true);
            menu.AddItem("Other", -1, true, true);
            menu.AddItem("Help", -1, true, true);
            
            GUIContextMenu submenu;
            submenu = menu.GetSubMenu(0);
            submenu.AddItem("Open File...", 100, true, false);
            submenu.AddSeparator();
            submenu.AddItem("Quit", 200, true, false);

            submenu = menu.GetSubMenu(1);
            submenu.AddItem("toggle sky box visibility", 300, true, false);
            submenu.AddItem("toggle debug information", 400, true, false);
            submenu.AddItem("toggle mode", -1, true, true);

            submenu = submenu.GetSubMenu(2);
            submenu.AddItem("Solid", 610, true, false);
            submenu.AddItem("Transparent", 620, true, false);
            submenu.AddItem("Reflection", 630, true, false);

            submenu = menu.GetSubMenu(3);
            submenu.AddItem("About", 500, true, false);



            //GUIToolBar gtb = guienv.AddToolBar(guienv.RootElement, 91);
            //gtb.Text = "Hi";
            //gtb.AddButton(92, "Button", "Click", null, null, true, false);

            // Test Avatar Ruth mesh
            /* AnimatedMesh av = smgr.GetMesh("Female7.x");

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
            // We need to flip the normals since it's a boned mesh.
            smgr.MeshManipulator.FlipSurfaces(av.GetMesh(0));

            avmeshsntest = smgr.AddAnimatedMeshSceneNode(av);
            avmeshsntest.JointMode = JointUpdateOnRenderMode.Control;
            //avmeshsntest.SetMaterialFlag(MaterialFlag.NormalizeNormals,true);

            // Read the Binary Asset Animation format.
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
            */
            // Main Render Loop
            int minFrameTime = (int)(1.0f / maxFPS);
            bool running = true;
            while (running)
            {
                try
                {

                    // If you close the gui window, device.Run returns false.
                    running = device.Run();
                }
                catch (AccessViolationException e)
                {
                    m_log.Error("[VIDEO]: Error in device" + e.ToString());
                }
                if (!running)
                    break;
                tickcount = System.Environment.TickCount;
                
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
                // Use this only for profiling.
                try
                {
                    smgr.DrawAll();
                }
                catch (AccessViolationException)
                {

                }
                guienv.DrawAll();

                driver.EndScene();

                mscounter += System.Environment.TickCount - tickcount;
                msreset = 55;
                //
                if (mscounter > msreset)
                {
                    // Repeat any held keys
                    processHeldKeys();
                    
                    // Update Interpolation targets
                    updateInterpolationTargets();

                    AVControl.update(System.Environment.TickCount - tickcount);

                    // Ensure that the camera is still pointing at the target object
                    cam.CheckTarget();

                    mscounter = 0;
                    framecounter++;

                    if (framecounter == uint.MaxValue)
                        framecounter = 0;
                }
                
               
              
                if ((framecounter % objectmods) == 0)
                {
                    // process avatar animation changes
                    doAnimationFrame();

                    // Process Mesh Queue.  Parameter is 'Items'
                    doProcessMesh(20);

                    // Process Object Mod Queue.  Parameter is 'Items'
                    doObjectMods(20);

                    // Check the UnAssigned Child Queue for parents that have since rezed
                    CheckAndApplyParent(5);

                    // Apply textures
                    doTextureMods(20);
                    

                    // Check for Dirty terrain Update as necessary.
                    UpdateTerrain();

                    doFoliage(3);

                    // Set the FPS in the window title.
                    device.WindowCaption = "IdealistViewer 0.001, FPS:" + driver.FPS.ToString();
                    //BoneSceneNode bcn = avmeshsntest.GetJointNode("lCollar:2");
                    //bcn.Rotation = new Vector3D(0, 36 + framecounter, 0);
                    //bcn.Position = new Vector3D(0, 0, 1 + framecounter);
                    
                    //avmeshsntest.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);

                }

                if ((framecounter % modAVUpdates) == 0)
                {
                    AVControl.UpdateRemote();
                    doSetCameraPosition();
                }
                
                // Frame Limiter
                int frameTime = System.Environment.TickCount - tickcount;
                if (frameTime < minFrameTime)
                    Thread.Sleep(minFrameTime - frameTime);
                
            }
            //In the end, delete the Irrlicht device.
            Shutdown();

        }

        /// <summary>
        /// Updates all interpolation targets
        /// </summary>
        private void updateInterpolationTargets()
        {
            List<string> removestr = null;
            lock (interpolationTargets)
            {
                foreach (string str in interpolationTargets.Keys)
                {
                    VObject obj = interpolationTargets[str];

                    // Check if the target is dead.
                    if (obj == null)
                    {
                        //if (removestr == null)
                            //removestr = new List<string>();

                        //removestr.Add(str);
                        continue;
                    }
                    if (obj.node == null)
                    {
                        //if (removestr == null)
                        //    removestr = new List<string>();

                        //removestr.Add(str);
                        continue;
                    }
                    if (obj.node.Raw == IntPtr.Zero)
                    {
                        //if (removestr == null)
                        //    removestr = new List<string>();

                        //removestr.Add(str);
                        continue;
                    }

                    // Interpolate
                    try
                    {
                        if (UserAvatar != null && UserAvatar.prim != null)
                        {
                            if (obj.prim.ID != UserAvatar.prim.ID)
                            {
                                // If this is our avatar and the update came less then 5 seconds 
                                // after we last rotated, it'll just confuse the user
                                if (System.Environment.TickCount - AVControl.userRotated < 5000)
                                {
                                    continue;
                                }
                            }
                        }

                        bool againstground = false;
                        if (obj.prim != null && UserAvatar != null && UserAvatar.prim != null && currentSim != null)
                        {
                            if (UserAvatar.prim.ID == obj.prim.ID)
                            {
                                if (obj.prim.Position.Z >= 0)
                                //terrainBitmap lower then avatar byte 2.3
                                if (RegionHFArray[(int)(Util.Clamp<float>(obj.prim.Position.Y, 0, 255)), (int)(Util.Clamp<float>(obj.prim.Position.X, 0, 255))] + 1.5f >= obj.prim.Position.Z)
                                {
                                    
                                    againstground = true;
                                }
                                //m_log.InfoFormat("[INTERPOLATION]: TerrainHeight:{0}-{1}-{2}-<{3},{4},{5}>", RegionHFArray[(int)(Util.Clamp<float>(obj.prim.Position.Y, 0, 255)),(int)(Util.Clamp<float>(obj.prim.Position.X, 0, 255))], obj.prim.Position.Z, (int)(Util.Clamp<float>(obj.prim.Position.Y, 0, 255) * 256 + Util.Clamp<float>(obj.prim.Position.X, 0, 255)), obj.prim.Position.X, obj.prim.Position.Y, obj.prim.Position.Z);
                            }
                        }
                        if (againstground)
                        {
                            obj.prim.Velocity.Z = 0;
                        }
                        Vector3D pos = new Vector3D(obj.node.Position.X, (againstground ? RegionHFArray[(int)(Util.Clamp<float>(obj.prim.Position.Y, 0, 255)), (int)(Util.Clamp<float>(obj.prim.Position.X, 0, 255))] + 0.9f : obj.node.Position.Y), obj.node.Position.Z);
                        Vector3D interpolatedpos = ((new Vector3D(obj.prim.Velocity.X, obj.prim.Velocity.Z, obj.prim.Velocity.Y) * 0.073f) * TimeDilation);
                        if (againstground)
                        {
                            interpolatedpos.Y = 0;
                        }

                        //if (obj.prim is Avatar)
                        //{
                            //Avatar av = (Avatar)obj.prim;
                            //if (obj.prim.Velocity.Z < 0 && obj.prim.Velocity.Z > -2f)
                            //    obj.prim.Velocity.Z = 0;
                       // }
                        obj.node.Position = pos + interpolatedpos;
                        
                    }
                    catch (AccessViolationException)
                    {
                        //if (removestr == null)
                        //    removestr = new List<string>();

                        //removestr.Add(str);
                        continue;
                    }
                    catch (System.Runtime.InteropServices.SEHException)
                    {
                        
                       // if (removestr == null)
                        //    removestr = new List<string>();

                       // removestr.Add(str);
                        continue;
                    }

                }

                // Remove dead Interpolation targets
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

        /// <summary>
        /// Assign Textures to objects that requested them
        /// </summary>
        /// <param name="pCount">Number of textures to process this round</param>
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
                        // Nope, we really don't have that texture loaded yet.  Load it now.
                        tex = new TextureExtended(driver.GetTexture(tx.texture).Raw);
                    }
                }

                if (tx.vObj != null && tex != null)
                {
                    
                        if (tx.textureID == tx.vObj.prim.Sculpt.SculptTexture)
                        {
                            tx.vObj.updateFullYN = true;
                            //tx.vObj.mesh.Dispose();

                            if (tx.vObj.node != null && tx.vObj.node.TriangleSelector != null)
                            {
                                if (mts != null)
                                {
                                    mts.RemoveTriangleSelector(tx.vObj.node.TriangleSelector);
                                }

                            }
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

        /// <summary>
        /// Updates camera position reported by LibOMV
        /// </summary>
        private void doSetCameraPosition()
        {
            Vector3[] camdata = cam.GetCameraLookAt();

            avatarConnection.SetCameraPosition(camdata);
        }


        #region Object Management

        /// <summary>
        /// Enqueues an object for processing.  This is the beginning of the object pipeline.
        /// </summary>
        /// <param name="newObject"></param>
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

        /// <summary>
        /// After the prim are meshed, here to be placed in the scene.  Linked object textures are requested
        /// </summary>
        /// <param name="pObjects"></param>
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
#if DebugObjectPipeline
                        m_log.DebugFormat("[OBJ]: NonRootPrim ID: {0}", vObj.prim.ID);
#endif
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
#if DebugObjectPipeline
                                    m_log.DebugFormat("[OBJ]: No Parent Yet for ID: {0}", vObj.prim.ID);
#endif
                                    // No parent yet...    Stick it in the child prim wait queue.
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
#if DebugObjectPipeline
                        m_log.DebugFormat("[OBJ]: Avatar ID: {0}", vObj.prim.ID);
#endif
                        // Little known fact.  Dead avatar in LibOMV have the word 'dead' in their UUID
                        // Skip over this one and move on to the next one if it's dead.
                        if (((Avatar)vObj.prim).ID.ToString().Contains("dead"))
                            continue;

                        // If we don't have an avatar representation yet for this avatar or it's a full update
                        if (vObj.node == null && vObj.updateFullYN)
                        {
#if DebugObjectPipeline
                            m_log.DebugFormat("[OBJ]: Created Avatar ID: {0}", vObj.prim.ID);
#endif
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

                            // TODO: FIXME - this depends on the mesh being loaded. A good candidate for a config item.
                            node.Scale = new Vector3D(0.035f, 0.035f, 0.035f);
                            //node.Scale = new Vector3D(15f, 15f, 15f);
                           
                            if (!isTextured)
                                node.SetMaterialTexture(0, driver.GetTexture(avatarMaterial));
                            
                            // Light avatar
                            node.SetMaterialFlag(MaterialFlag.Lighting, true);

                            if (node is AnimatedMeshSceneNode)
                            {
                                ((AnimatedMeshSceneNode) node).SetFrameLoop(0, 6);
                                // cast and do extra cool stuff 
                            }
#if DebugObjectPipeline
                            m_log.DebugFormat("[OBJ]: Added Interpolation Target for Avatar ID: {0}", vObj.prim.ID);
#endif
                            // Add to Interpolation targets
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

                            // Is this an update about us?
                            if (vObj.prim.ID == avatarConnection.GetSelfUUID)
                            {
                                if (UserAvatar == null)
                                    SetSelfVObj(vObj);

                            }

                            // Display the avatar's name over their head.
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
#if DebugObjectPipeline
                            m_log.DebugFormat("[OBJ]: update for existing avatar ID: {0}", vObj.prim.ID);
#endif
                            // Set the current working node to the already existing node.
                            node = vObj.node;
                        }

                    }
#endregion
                    else
                    {
#if DebugObjectPipeline
                        m_log.DebugFormat("[OBJ]: Update for Prim ID: {0}", vObj.prim.ID);
#endif
                        // No mesh yet, skip over it.
                        if (vObj.mesh == null)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: No Mesh for Prim ID: {0}.  This prim won't be displayed", vObj.prim.ID);
#endif
                            continue;
                        }

                        // Full Update
                        if (vObj.updateFullYN)
                        {
                            // Check if it's a sculptie and we've got it's texture.
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
                            // Set the working node to the pre-existing node for this object
                            node = vObj.node;
                        }

#if DebugObjectPipeline
                        m_log.DebugFormat("[OBJ]: Update Data Prim ID: {0}, FULL:{1}, CREATED:{2}", vObj.prim.ID, vObj.updateFullYN ,creatednode);
#endif

                        if (node == null)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: Node was Null for Prim ID: {0}.  This prim won't be displayed", vObj.prim.ID);
#endif
                            continue;
                        }
                    }

                    if (node == null && vObj.prim is Avatar)
                    {
                        // why would node = null?  Race Condition?
                        continue;
                    }

                    if (vObj.prim is Avatar)
                    {
                        // TODO: FIXME - This is dependant on the avatar mesh loaded. a good candidate for a config option.
                        //vObj.prim.Position.Z -= 0.2f;
                        if (vObj.prim.Position.Z >=0)
                            if (RegionHFArray[(int)(Util.Clamp<float>(vObj.prim.Position.Y, 0, 255)), (int)(Util.Clamp<float>(vObj.prim.Position.X, 0, 255))] + 2.5f >= vObj.prim.Position.Z)
                            {
                                vObj.prim.Position.Z = RegionHFArray[(int)(Util.Clamp<float>(vObj.prim.Position.Y, 0, 255)), (int)(Util.Clamp<float>(vObj.prim.Position.X, 0, 255))] + 0.9f;
                            }
                    
                    }
                    else
                    {
                        // Set the scale of the prim to the libomv reported scale.
                        node.Scale = new Vector3D(vObj.prim.Scale.X, vObj.prim.Scale.Z, vObj.prim.Scale.Y);
                    }
                    
                   // m_log.WarnFormat("[SCALE]: <{0},{1},{2}> = <{3},{4},{5}>", vObj.prim.Scale.X, vObj.prim.Scale.Z, vObj.prim.Scale.Y, pscalex, pscaley, pscalez);
                    
                    // If this prim is either the parent prim or an individual prim
                    if (vObj.prim.ParentID == 0)
                    {
                        if (vObj.prim is Avatar)
                        {
                            //m_log.WarnFormat("[AVATAR]: W:<{0},{1},{2}> R:<{3},{4},{5}>",WorldoffsetPos.X,WorldoffsetPos.Y,WorldoffsetPos.Z,vObj.prim.Position.X,vObj.prim.Position.Y,vObj.prim.Position.Z);
                            WorldoffsetPos = Vector3.Zero;
                            // The world offset for avatar doesn't work for some reason yet in LibOMV.  
                            // It's offset, so don't offset them by their world position yet.
                        }

                        try
                        {
                            if (node.Raw == IntPtr.Zero)
                                continue;
                            // Offset the node by it's world position
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
                        // Check if the node died
                        if (node.Raw == IntPtr.Zero)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: Prim ID: {0} Missing Node, IntPtr.Zero", vObj.prim.ID);
#endif
                            continue;
                        }
                        if (vObj == null || parentObj == null)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: Prim ID: {0} Missing Node, vObj == null || parentVObj == null", vObj.prim.ID);
#endif
                            continue;
                        }
                        if (vObj.prim == null || parentObj.prim == null)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: Prim ID: {0} Missing prim, vObj.prim == null || parentObj.prim == null", vObj.prim.ID);
#endif
                            continue;
                        }

                        // apply rotation and position reported form LibOMV
                        vObj.prim.Position = vObj.prim.Position * parentObj.prim.Rotation;
                        vObj.prim.Rotation = parentObj.prim.Rotation * vObj.prim.Rotation;

                        node.Position = new Vector3D(WorldoffsetPos.X + parentObj.prim.Position.X + vObj.prim.Position.X, WorldoffsetPos.Z + parentObj.prim.Position.Z + vObj.prim.Position.Z, WorldoffsetPos.Y + parentObj.prim.Position.Y + vObj.prim.Position.Y);
                    }

                    if (vObj.updateFullYN)
                    {
                        // If the prim is physical, add it to the interpolation targets.
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
                                if (! (vObj.prim is Avatar))
                                if (interpolationTargets.ContainsKey(simhandle.ToString() + vObj.prim.LocalID.ToString()))
                                    interpolationTargets.Remove(simhandle.ToString() + vObj.prim.LocalID.ToString());
                            }
                        }

                    }

                    if (node.Raw == IntPtr.Zero)
                    {
#if DebugObjectPipeline
                        m_log.WarnFormat("[OBJ]: Prim ID: {0} Node IntPtr.Zero, node.Raw == IntPtr.Zero", vObj.prim.ID);
#endif
                        continue;
                    }

                    bool ApplyRotationYN = true;

                    if (vObj.prim is Avatar)
                    {
                        if (UserAvatar != null && UserAvatar.prim != null && AVControl != null)
                        {
                            if (vObj.prim.ID == UserAvatar.prim.ID)
                            {
                                // If this is our avatar and the update came less then 5 seconds 
                                // after we last rotated, it'll just confuse the user
                                if (System.Environment.TickCount - AVControl.userRotated < 5000)
                                {
                                    ApplyRotationYN = false;
                                }
                            }
                        }
                    }

                    //m_log.Warn(vObj.prim.Rotation.ToString());
                    if (ApplyRotationYN)
                    {
                        // Convert Cordinate space
                        IrrlichtNETCP.Quaternion iqu = new IrrlichtNETCP.Quaternion(vObj.prim.Rotation.X, vObj.prim.Rotation.Z, vObj.prim.Rotation.Y, vObj.prim.Rotation.W);

                        iqu.makeInverse();

                        IrrlichtNETCP.Quaternion finalpos = iqu;

                        finalpos = Cordinate_XYZ_XZY * finalpos;
                        node.Rotation = finalpos.Matrix.RotationDegrees;
                    }

                    
                    if (creatednode)
                    {   
                        // If we created this node, then we need to add it to the 
                        // picker targets and request it's textures

                        TriangleSelector trisel = smgr.CreateTriangleSelector(vObj.mesh, node);
                        node.TriangleSelector = trisel;
                        triPicker.AddTriangleSelector(trisel, node);
                        if (mts != null)
                        {
                            lock (mts)
                            {
                                mts.AddTriangleSelector(trisel);
                            }
                        }
                        if (vObj.prim.Textures != null)
                        {
                            if (vObj.prim.Textures.DefaultTexture != null)
                            {
                                if (vObj.prim.Textures.DefaultTexture.TextureID != UUID.Zero)
                                {
                                    UUID textureID = vObj.prim.Textures.DefaultTexture.TextureID;
                                    
                                    // Only request texture if texture downloading is enabled.
                                    if (textureMan != null)
                                        textureMan.RequestImage(textureID, vObj);

                                }
                            }
                            
                            // If we have individual face texture settings
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
                    // Check for dead nodes
                    if (node.Raw == IntPtr.Zero)
                        continue;
                    node.UpdateAbsolutePosition();
                    
                }
            }
        }

        /// <summary>
        /// This is mostly a duplication of doObjectMods.  A good candidate for abstraction
        /// Acts on child objects
        /// </summary>
        /// <param name="pObjects"></param>
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
                                
                                //AnimatedMesh avmesh = smgr.GetMesh("sydney.md2");
                                AnimatedMesh avmesh = smgr.GetMesh(avatarMesh);

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
                                try
                                {
                                    node = smgr.AddMeshSceneNode(vObj.mesh, smgr.RootSceneNode, (int)vObj.prim.LocalID);
                                    creatednode = true;
                                    vObj.node = node;
                                }
                                catch (AccessViolationException)
                                {

                                    continue;
                                }
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
                            triPicker.AddTriangleSelector(trisel, node);
                            if (mts != null)
                            {
                                lock (mts)
                                {
                                    mts.AddTriangleSelector(trisel);
                                }
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

        /// <summary>
        /// Mesh pObjects count prim in the ObjectMeshQueue
        /// </summary>
        /// <param name="pObjects">number of prim to mesh this time around</param>
        public void doProcessMesh(int pObjects)
        {
            bool sculptYN = false;
            TextureExtended sculpttex = null;

            for (int i = 0; i < pObjects; i++)
            {
                sculptYN = false;
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
                            // Skipping it for now.
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
                    // Mesh a regular prim.
                    vobj.mesh = m_MeshFactory.GetMeshInstance(vobj.prim);
                }
                else
                {
                    // Mesh a scupted prim.
                    m_log.Warn("[SCULPT]: Meshing Sculptie......");
                    vobj.mesh = m_MeshFactory.GetSculptMesh(vobj.prim.Sculpt.SculptTexture, sculpttex, vobj.prim.Sculpt.Type, vobj.prim);
                    m_log.Warn("[SCULPT]: Sculptie Meshed");

                }

                // Add the newly meshed object ot the objectModQueue
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

        // for animation debugging...
        //public int myStartFrame = 0;
        //public int myStopFrame = 90;
        //public bool myFramesDirty = true;

        /// <summary>
        /// Animations that are received are stored in a dictionary in the protocol module and associated
        /// with an avatar. They are removed from that dictionary here and applied to the proper avatars
        /// in the scene.
        /// </summary>
        public void doAnimationFrame()
        {
            lock (Avatars)
            {
                foreach (UUID avatarID in Avatars.Keys)
                {
                    VObject avobj = Avatars[avatarID];
                    if (avobj != null)
                    {
                        if (avobj.prim != null)
                            if (avobj.prim.ID.ToString().Contains("dead"))
                                continue;


                        if (avobj.mesh != null)
                        {
                        }


                        if (avobj.node != null) // this is the scenenode for an animated mesh
                        {
                            List<UUID> newAnims = null;
                            lock (avatarConnection.AvatarAnimations)
                            {
                                // fetch any pending animations from the dictionary and then
                                // delete them from the dictionary
                                if (avatarConnection.AvatarAnimations.ContainsKey(avatarID))
                                {
                                    newAnims = avatarConnection.AvatarAnimations[avatarID];
                                    avatarConnection.AvatarAnimations.Remove(avatarID);
                                }
                            }
                            if (newAnims != null)
                            {
                                MD2Animation md2Anim = MD2Animation.Stand;
                                foreach (UUID animID in newAnims)
                                {
                                    //m_log.Debug("[ANIMATION] - got animID: " + animID.ToString());

                                    if (animID == Animations.STAND
                                        || animID == Animations.STAND_1
                                        || animID == Animations.STAND_2
                                        || animID == Animations.STAND_3
                                        || animID == Animations.STAND_4)
                                    {
                                        m_log.Debug("[ANIMAION] - standing");
                                        md2Anim = MD2Animation.Stand;

                                    }
                                    if (animID == Animations.CROUCHWALK)
                                    {
                                        m_log.Debug("[ANIMAION] - crouchwalk");
                                        md2Anim = MD2Animation.CrouchWalk;
                                    }
                                    if (animID == Animations.WALK
                                        || animID == Animations.CROUCHWALK
                                        || animID == Animations.FEMALE_WALK)
                                    {
                                        m_log.Debug("[ANIMAION] - walking");
                                        md2Anim = MD2Animation.Run;
                                    }
                                    if (animID == Animations.SIT
                                        || animID == Animations.SIT_FEMALE
                                        || animID == Animations.SIT_GENERIC
                                        || animID == Animations.SIT_GROUND
                                        || animID == Animations.SIT_GROUND_staticRAINED
                                        || animID == Animations.SIT_TO_STAND)
                                    {
                                        m_log.Debug("[ANIMAION] - sitting");
                                        md2Anim = MD2Animation.Pain3;
                                    }
                                    if (animID == Animations.FLY
                                        || animID == Animations.FLYSLOW)
                                    {
                                        m_log.Debug("[ANIMAION] - flying");
                                        md2Anim = MD2Animation.Jump;
                                    }
                                    if (animID == Animations.CROUCH)
                                    {
                                        m_log.Debug("[ANIMAION] - crouching");
                                        md2Anim = MD2Animation.CrouchPain;
                                    }
                                    else md2Anim = MD2Animation.Stand;
                                }
                                if (avobj.node is AnimatedMeshSceneNode)
                                {
                                    ((AnimatedMeshSceneNode)avobj.node).SetMD2Animation(md2Anim);
                                }
                            }


                            //if (avatarID == avatarConnection.GetSelfUUID && myFramesDirty)
                            //{
                            //    if (avobj.node is AnimatedMeshSceneNode)
                            //    {
                            //        myFramesDirty = false;
                            //        ((AnimatedMeshSceneNode)avobj.node).SetFrameLoop(myStartFrame, myStopFrame);
                            //        m_log.Debug("setting frames to " + myStartFrame.ToString() + " " + myStopFrame.ToString());
                            //    }
                            //}

                        }

                    }
                }
            }
        }

        public void doFoliage(uint max)
        {
            int i = 0;
            bool done = false;
            while (!done)
            {
                /*
                    // Pine 1 -0
                    // Oak 1 
                    // Tropical Bush 1 - 2
                    // Palm 1 -3
                    // Dogwood - 4    
                    // Tropical Bush 2 - 5
                    // Palm 2 - 6
                    // Cypress 1 - 7
                    // Cypress 2 - 8
                    // Pine 2 - 9
                    // Plumeria - 10
                    // Winter Pine 1 - 11
                    // Winter Aspen - 12
                    // Winter Pine 2 - 13
                    // Eucalyptus - 14
                    // Fern - 15
                    // Eelgrass - 16
                    // Sea Sword - 17
                    // Kelp 1 - 18
                    // Beach Grass 1 - 19
                    // Kelp 2 - 20
                 */

                if (foliageObjectQueue.Count > 0)
                {
                    FoliageObject foliage = foliageObjectQueue.Dequeue();
                    Primitive prim = foliage.prim;
                    ulong handle = 0;
                    float scaleScalar = 0.1f;
                    if (currentSim != null)
                    {
                        handle = currentSim.Handle;
                    }
                    Vector3 globalPositionToRez = Util.OffsetGobal(prim.RegionHandle, Vector3.Zero);
                    Vector3 currentGlobalPosition = Util.OffsetGobal(handle, Vector3.Zero);
                    Vector3 worldOffsetPosition = globalPositionToRez - currentGlobalPosition;
                    Vector3 position = prim.Position;
                    Vector3 scale = prim.Scale;

                    SceneNode tree;

                    int type = foliage.prim.PrimData.State;
                    
                    switch (type)
                    {
                        case 0: // Pine 1 -0
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("PineBark.png"), driver.GetTexture("PineLeaf.png"), driver.GetTexture("PineBillboard.png"));
                            break;
                        case 1: // Oak 1
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("OakBark.png"), driver.GetTexture("OakLeaf.png"), driver.GetTexture("OakBillboard.png"));
                            break;
                        case 2: // Tropical Bush 1 - 2
                            scaleScalar = 0.025f;
                            tree = smgr.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("PineBark.png"), driver.GetTexture("PineLeaf.png"), driver.GetTexture("PineBillboard.png"));
                            break;
                        case 3: // Palm 1 -3
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("OakBark.png"), driver.GetTexture("OakLeaf.png"), driver.GetTexture("OakBillboard.png"));
                            break;
                        case 4: // Dogwood - 4
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("OakBark.png"), driver.GetTexture("OakLeaf.png"), driver.GetTexture("OakBillboard.png"));
                            break;
                        case 5: // Tropical Bush 2 - 5
                            scaleScalar = 0.025f;
                            tree = smgr.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("OakBark.png"), driver.GetTexture("OakLeaf.png"), driver.GetTexture("OakBillboard.png"));
                            break;
                        case 6: // Palm 2 - 6
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("PineBark.png"), driver.GetTexture("PineLeaf.png"), driver.GetTexture("PineBillboard.png"));
                            break;
                        case 7: // Cypress 1 - 7
                        case 8: // Cypress 2 - 8
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("OakBark.png"), driver.GetTexture("OakLeaf.png"), driver.GetTexture("OakBillboard.png"));
                            break;
                        case 9: // Pine 2 - 9
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("PineBark.png"), driver.GetTexture("PineLeaf.png"), driver.GetTexture("PineBillboard.png"));
                            break;
                        case 10: // Plumeria - 10
                        case 11: // Winter Pine 1 - 11
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("PineBark.png"), driver.GetTexture("PineLeaf.png"), driver.GetTexture("PineBillboard.png"));
                            break;
                        case 12: // Winter Aspen - 12
                            scaleScalar = 0.01f;
                            tree = smgr.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("OakBark.png"), driver.GetTexture("OakLeaf.png"), driver.GetTexture("OakBillboard.png"));
                            break;
                        case 13: // Winter Pine 2 - 13
                            scaleScalar = 0.1f;
                            tree = smgr.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("PineBark.png"), driver.GetTexture("PineLeaf.png"), driver.GetTexture("PineBillboard.png"));
                            break;
                        case 15: // Fern - 15
                        case 16: // Eelgrass - 16
                        case 17: // Sea Sword - 17
                        case 18: // Kelp 1 - 18
                        case 19: // Beach Grass 1 - 19
                        case 20: // Kelp 2 - 20
                        default:
                            scaleScalar = 0.01f;
                            tree = smgr.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), driver.GetTexture("PineBark.png"), driver.GetTexture("PineLeaf.png"), driver.GetTexture("PineBillboard.png"));
                            break;
                    }

                    if (tree == null)
                    {
                        m_log.Warn("[FOLIAGE]: Couldn't make tree, shaders not supported by hardware");
                    }

                    if (tree != null)
                    {

                       

                        m_log.Debug("[FOLIAGE]: got foliage, location: " + prim.Position.ToString() + " type: " + type.ToString());

                        tree.Position = new Vector3D(position.X + worldOffsetPosition.X,
                            position.Z + worldOffsetPosition.Z,
                            position.Y + worldOffsetPosition.Y);
                        tree.Scale = new Vector3D(scale.X * scaleScalar, scale.Z * scaleScalar, scale.Y * scaleScalar);

                    }
                    else
                    {
                        m_log.Warn("[FOLIAGE]: Couldn't make tree, shaders not supported by hardware");
                    }
                }
                else
                    done = true;

                if (++i >= max)
                    done = true;
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
                //case "a":  // experimental for animation debugging
                //    try
                //    {
                //        int.TryParse(cmdparams[0], out myStartFrame);
                //        int.TryParse(cmdparams[1], out myStopFrame);
                //        myFramesDirty = true;
                //    }
                //    catch
                //    {
                //        m_log.Warn("usage: a <startFrame> <endFrame> - where startFrame and endFrame are integers");
                //    }
                //    break;
                case "goto":
                    float x = 128f;
                    float y = 128f;
                    float z = 22f;
                    string cmdStr = "";
                    foreach (string arg in cmdparams)
                        cmdStr += arg + " ";
                    cmdStr = cmdStr.Trim();
                    string[] dest = cmdStr.Split(new char[] { '/' });
                    if (float.TryParse(dest[1], out x) &&
                        float.TryParse(dest[2], out y) &&
                        float.TryParse(dest[3], out z))
                    {
                        avatarConnection.Teleport(dest[0], x, y, z);
                    }
                    else
                        m_log.Warn("Usage: goto simname x y z");
                    break;
                case "help":
                    ShowHelp(cmdparams);
                    Notice("");
                    break;
                case "relog":
                    avatarConnection.BeginLogin(avatarConnection.loginURI, avatarConnection.firstName + " " + avatarConnection.lastName, avatarConnection.password, avatarConnection.startlocation);
                    break;
                case "say":
                    string message = "";
                    foreach (string word in cmdparams)
                        message += word + " ";
                    avatarConnection.Say(message);
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


                Notice("goto region/x/y/z - teleport to a location");
                Notice("say [message] - says a message over chat");

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

            // Initialize LibOMV
            avatarConnection = new SLProtocol();
            avatarConnection.OnLandPatch += landPatchCallback;
            avatarConnection.OnGridConnected += connectedCallback;
            avatarConnection.OnNewPrim += newPrimCallback;
            avatarConnection.OnSimConnected += SimConnectedCallback;
            avatarConnection.OnObjectUpdated += objectUpdatedCallback;
            avatarConnection.OnObjectKilled += objectKilledCallback;
            avatarConnection.OnNewAvatar += newAvatarCallback;
            avatarConnection.OnNewFoliage += newFoliageCallback;

            // Startup the GUI
            guithread = new Thread(new ParameterizedThreadStart(startupGUI));
            guithread.Start();

            

            // Compose Coordinate space converter quaternion
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

            // Begin Login!
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
            // Shutdown Irrlicht
            device.Close();
            device.Dispose();

            // Shutdown LibOMV
            avatarConnection.Logout();
            //base.ShutdownSpecific();
        }

        #endregion

        /// <summary>
        /// Update all terrain that's dirty!
        /// </summary>
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
                        Util.SaveBitmapToFile(terrainbmp, m_startupDirectory + "/" + path);

                    }
                    device.FileSystem.WorkingDirectory = m_startupDirectory + "/" + Util.MakePath("media", "materials", "textures", "");
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
                            // Remove old pickers
                            triPicker.RemTriangleSelector(terrain.TriangleSelector);
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
                        terrain.SetMaterialTexture(0, GreenGrassTexture);
                        //terrain.SetMaterialTexture(1, driver.GetTexture("detailmap3.jpg"));
                        terrain.ScaleTexture(16, 16);
                        terrain.Scale = new Vector3D(1.0275f, 1, 1.0275f);
                    }

                    lock (terrains)
                    {
                        terrains.Add(regionhandle, terrain);
                    }

                    // Manage pickers
                    TriangleSelector terrainsel;
                    lock (terrainsels)
                    {
                        if (terrainsels.ContainsKey(regionhandle))
                        {
                            if (mts != null)
                            {
                                mts.RemoveTriangleSelector(terrainsels[regionhandle]);
                            }
                            terrainsels.Remove(regionhandle);
                            
                        }
                    }

                    terrainsel = smgr.CreateTerrainTriangleSelector(terrain, 1);
                    terrain.TriangleSelector = terrainsel;
                    triPicker.AddTriangleSelector(terrainsel, terrain);

                    lock (terrainsels)
                    {
                        terrainsels.Add(regionhandle, terrainsel);

                    }
                    if (mts != null)
                    {
                        mts.AddTriangleSelector(terrainsel);
                    }
                    //Vector3D terrainpos = terrain.TerrainCenter;
                    //terrainpos.Z = terrain.TerrainCenter.Z - 100f;
                    //terrain.Position = terrainpos;
                    //m_log.DebugFormat("[TERRAIN]:<{0},{1},{2}>", terrain.TerrainCenter.X, terrain.TerrainCenter.Y, terrain.TerrainCenter.Z);
                    //terrain.ScaleTexture(1f, 1f);
                    if (currentSim != null)
                    {
                        // Update camera position
                        if (currentSim.Handle == regionhandle)
                        {
                            cam.SNCamera.Target = terrain.TerrainCenter;
                            cam.UpdateCameraPosition();
                        }
                    }

                }
            }
        }


        /// LibOMV Callbacks.  These execute in the threadcontext of LIBOMV, so be careful!
        /// Stick the result in queues so we can process them in our own threads later.
        #region LibOMV Callbacks

        public void newFoliageCallback(Simulator simulator, Primitive foliage, ulong regionHandle, ushort timeDilation)
        {
            TimeDilation = (float)(timeDilation / ushort.MaxValue);
            foliageCount++;
            //m_log.Debug("[FOLIAGE]: got foliage, location: " + foliage.Position.ToString());

            FoliageObject newFoliageObject = new FoliageObject();
            
            // add to the foliage queue
            newFoliageObject.prim = foliage;
            lock (foliageObjectQueue)
            {
                foliageObjectQueue.Enqueue(newFoliageObject);
            }

            
        }

        /// <summary>
        /// LibOMV gave us a Full Object Update
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="prim"></param>
        /// <param name="regionHandle"></param>
        /// <param name="timeDilation"></param>
        public void newPrimCallback(Simulator sim, Primitive prim, ulong regionHandle,
                                      ushort timeDilation)
        {
            TimeDilation = (float)(timeDilation / ushort.MaxValue);
            //System.Console.WriteLine(prim.ToString());
            //return;
            primcount++;
            VObject newObject = null;

            //bool foundEntity = false;
#if DebugObjectPipeline
            m_log.DebugFormat("[OBJ]: Got New Prim ID: {0}", prim.ID);
#endif
            lock (Entities)
            {
                if (Entities.ContainsKey(regionHandle.ToString() + prim.LocalID.ToString()))
                {
                    //foundEntity = true;
                    newObject = Entities[regionHandle.ToString() + prim.LocalID.ToString()];
#if DebugObjectPipeline
                    m_log.DebugFormat("[OBJ]: Reusing Entitity ID: {0}", prim.ID);
#endif
                }
                else
                {
#if DebugObjectPipeline
    m_log.DebugFormat("[OBJ]: New Entitity ID: {0}", prim.ID);
#endif
                }
            }

            if (newObject != null)
            {
                if (newObject.node != null)
                {
                    if (newObject.node.TriangleSelector != null)
                    {
                        if (mts != null)
                        {
                            mts.RemoveTriangleSelector(newObject.node.TriangleSelector);
                        }
                    }

                    for (uint i = 0; i < newObject.node.MaterialCount; i++)
                    {
                        IrrlichtNETCP.Material objmaterial = newObject.node.GetMaterial((int)i);
                        //objmaterial.Texture1.Dispose();
                        if (objmaterial.Layer1 != null)
                        {
                            if (objmaterial.Layer1.Texture != null)
                            {
                                objmaterial.Layer1.Texture.Dispose();
                            }
                            objmaterial.Layer1.Dispose();
                        }
                        objmaterial.Dispose();
                    }

                    smgr.AddToDeletionQueue(newObject.node);
#if DebugObjectPipeline
                    m_log.DebugFormat("[OBJ]: Deleted Node for ID: {0}", prim.ID);
#endif

                    newObject.node = null;
                    Mesh objmesh = newObject.mesh;
                    for (int i = 0; i < objmesh.MeshBufferCount; i++)
                    {
                        MeshBuffer mb = objmesh.GetMeshBuffer(i);
                        mb.Dispose();

                    }
                    newObject.mesh.Dispose();
                    newObject.prim = null;
                }

            }

            
            // Box the object and node
            newObject = VUtil.NewVObject(prim,newObject);
            

                
            // Add to the mesh queue
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
                if (m_landMap.ContainsKey(y * 16 + x))
                    m_landMap[y * 16 + x] = data;
                else
                    m_landMap.Add(y * 16 + x, data);
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
                        //col *= 0.00388f;

                        if (currentSim != null)
                        {
                            if (currentSim.Handle == simhandle)
                            {
                                RegionHFArray[bitmapy,bitmapx] = col;
                            }
                        }

                        col *= 0.00397f;  // looks a little closer by eyeball
                        
                        
                        //m_log.Debug("[COLOR]: " + currentPatch[cy * 16 + cx].ToString());
                        //terrainbitmap.SetPixel(bitmapy, bitmapx, System.Drawing.Color.FromArgb(col, col, col));
                        terrainbitmap.SetPixel(bitmapy, bitmapx, Util.FromArgbf(1,col,col,col));
                    }
                }
            }
        }

        /// <summary>
        /// LibOMV has informed us that it's connected
        /// </summary>
        protected void connectedCallback()
        {

        }

        /// <summary>
        /// LibOMV has informed us that it's connected to a simulator.
        /// </summary>
        /// <param name="sim"></param>
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
            
            // Add the simulators to our known simulators and initialize the terrain constructs
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
                // Set the water position
                SNGlobalwater.Position = new Vector3D(0, sim.WaterHeight-0.5f, 0);
                //SNGlobalwater.Position = new Vector3D(0, sim.WaterHeight - 50.5f, 0);
                // TODO REFIX!
            }
        }

        /// <summary>
        /// LibOMV has informed us that an object has moved.  This is a terse update.
        /// </summary>
        /// <param name="simulator"></param>
        /// <param name="update"></param>
        /// <param name="regionHandle"></param>
        /// <param name="timeDilation"></param>
        private void objectUpdatedCallback(Simulator simulator, ObjectUpdate update, ulong regionHandle, 
            ushort timeDilation)
        {
            TimeDilation = (float)(timeDilation / ushort.MaxValue);
            
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
                        // Update the primitive properties for this object.
                        obj.prim.Acceleration = update.Acceleration;
                        obj.prim.AngularVelocity = update.AngularVelocity;
                        obj.prim.CollisionPlane = update.CollisionPlane;
                        obj.prim.Position = update.Position;
                        obj.prim.Rotation = update.Rotation;
                        obj.prim.PrimData.State = update.State;
                        obj.prim.Textures = update.Textures;
                        obj.prim.Velocity = update.Velocity;
                        
                        // Save back to the Entities.  vObject used to be a value type, so this was neccessary.
                        // it may not be anymore.

                        Entities[regionHandle.ToString() + update.LocalID.ToString()] = obj;
                    }
                }

                // Enqueue this object into the modification queue.
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

        /// <summary>
        ///  LibOMV has informed us that an object was deleted.
        /// </summary>
        /// <param name="psim"></param>
        /// <param name="pLocalID"></param>
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
                        // If we're interpolating this object, stop
                        lock (interpolationTargets)
                        {
                            if (interpolationTargets.ContainsKey(regionHandle.ToString() + obj.prim.LocalID.ToString()))
                            {
                                interpolationTargets.Remove(regionHandle.ToString() + obj.prim.LocalID.ToString());
                            }

                        }
                        // If the camera is targetting this object, stop targeting this object
                        if (cam.SNtarget == obj.node)
                            cam.SNtarget = null;

                        // Remove this object from our picker.
                        if (obj.node.TriangleSelector != null)
                        {
                            if (mts != null)
                            {
                                mts.RemoveTriangleSelector(obj.node.TriangleSelector);
                            }
                        }

                        smgr.AddToDeletionQueue(obj.node);
                        obj.node = null;
                        
                    }
                    // Remove this object from the known entities.
                    Entities.Remove(regionHandle.ToString() + pLocalID.ToString());
                }
            }

            // If it's an avatar, remove it from known avatars
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
                    // remove any pending animations for the avatar
                    lock (avatarConnection.AvatarAnimations)
                    {
                        if (avatarConnection.AvatarAnimations.ContainsKey(obj.prim.ID))
                            avatarConnection.AvatarAnimations.Remove(obj.prim.ID);
                    }
                    if (obj.prim.ID == avatarConnection.GetSelfUUID)
                    {
                        UserAvatar = null;
                    }
                }
            }
        }

        /// <summary>
        /// LibOMV has informed us of a new avatar
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="avatar"></param>
        /// <param name="regionHandle"></param>
        /// <param name="timeDilation"></param>
        private void newAvatarCallback(Simulator sim, Avatar avatar, ulong regionHandle,
                                       ushort timeDilation)
        {
            TimeDilation = (float)(timeDilation / ushort.MaxValue);
            VObject avob = null; 
            
            lock (Entities)
            {
                // If we've got an entitiy for this avatar, then this is a full object update
                // not a new avatar
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

            // Add to the Object Modification queue.
            lock (objectModQueue)
            {
                avob.updateFullYN = true;
                objectModQueue.Enqueue(avob);
            }

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
        }

        #endregion

        #region KeyActions

        /// <summary>
        /// Processes held keys.  This allows us to do multiple keypresses.
        /// </summary>
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

        /// <summary>
        /// Does an avatar movement based on provided key
        /// </summary>
        /// <param name="ky"></param>
        private void DoMotorAction(KeyCode ky, bool kydown, bool held)
        {
            //m_log.DebugFormat("{0},{1},{2}", ky.ToString(), kydown, held);
            switch (ky)
            {
                case KeyCode.Up:
                    
                    AVControl.Forward = kydown;
                    break;

                case KeyCode.Down:
                    AVControl.Back = kydown;
                    break;

                case KeyCode.Left:
                    AVControl.TurnLeft = kydown;
                    break;

                case KeyCode.Right:
                    AVControl.TurnRight = kydown;
                    break;

                case KeyCode.Prior:
                    if (AVControl.Fly)
                        AVControl.Up = kydown;
                    else
                        if (kydown)
                            AVControl.Jump = true;
                        else
                            AVControl.Jump = false;

                    break;

                case KeyCode.Next:
                    AVControl.Down = kydown;
                    break;

                case KeyCode.Home:
                    if (!held && !kydown) 
                        AVControl.Fly = !AVControl.Fly;
                    break;

            }
        }

        /// <summary>
        /// does an action for a held key
        /// </summary>
        /// <param name="ky"></param>
        private void doHeldKeyActions(KeyCode ky)
        {
            switch (ky)
            {
                case KeyCode.Up:
                    if (!shiftHeld && !ctrlHeld)
                    {
                        if (cam.CameraMode == ECameraMode.Build)
                        {
                            if (UserAvatar != null)
                            {
                                cam.SetTarget(UserAvatar.node);
                                cam.SwitchMode(ECameraMode.Third);
                            }
                        }

                        DoMotorAction(ky, true, true);
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
                        if (cam.CameraMode == ECameraMode.Build)
                        {
                            if (UserAvatar != null)
                            {
                                cam.SetTarget(UserAvatar.node);
                                cam.SwitchMode(ECameraMode.Third);
                            }
                        }

                        DoMotorAction(ky, true, true);
                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            cam.SwitchMode(ECameraMode.Build);
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

                case KeyCode.Left:
                    if (!shiftHeld && !ctrlHeld)
                    {
                        if (cam.CameraMode == ECameraMode.Build)
                        {
                            if (UserAvatar != null)
                            {
                                cam.SetTarget(UserAvatar.node);
                                cam.SwitchMode(ECameraMode.Third);
                            }
                        }

                        DoMotorAction(ky, true, true);
                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            //vOrbit.X -= 2f;
                            cam.SwitchMode(ECameraMode.Build);
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

                case KeyCode.Right:
                    if (!shiftHeld && !ctrlHeld)
                    {
                        if (cam.CameraMode == ECameraMode.Build)
                        {
                            if (UserAvatar != null)
                            {
                                cam.SetTarget(UserAvatar.node);
                                cam.SwitchMode(ECameraMode.Third);
                            }
                        }

                        DoMotorAction(ky, true, true);
                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            //vOrbit.X += 2f;
                            //vOrbit.X -= 2f;
                            cam.SwitchMode(ECameraMode.Build);
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;
                case KeyCode.Prior:
                    if (!shiftHeld && !ctrlHeld)
                    {
                        if (cam.CameraMode == ECameraMode.Build)
                        {
                            if (UserAvatar != null)
                            {
                                cam.SetTarget(UserAvatar.node);
                                cam.SwitchMode(ECameraMode.Third);
                            }
                        }

                        DoMotorAction(ky, true, true);
                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            //vOrbit.Y -= 2f;
                            cam.SwitchMode(ECameraMode.Build);
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

                case KeyCode.Next:
                    if (!shiftHeld && !ctrlHeld)
                    {
                        if (cam.CameraMode == ECameraMode.Build)
                        {
                            if (UserAvatar != null)
                            {
                                cam.SetTarget(UserAvatar.node);
                                cam.SwitchMode(ECameraMode.Third);
                            }
                        }

                        DoMotorAction(ky, true, true);
                    }
                    else
                    {
                        if (ctrlHeld)
                        {
                            cam.SwitchMode(ECameraMode.Build);
                            cam.DoKeyAction(ky);
                        }

                    }
                    break;

            }

        }

        /// <summary>
        /// Manage held keys
        /// </summary>
        /// <param name="ky"></param>
        /// <param name="held"></param>
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


        /*This function displays a messagebox with messageText that has been 
         * previously read with the xmlreader earlier.
         */
        static void showAboutText()
        {
            // create modal message box with the text
            // loaded from the xml file.
            device.GUIEnvironment.AddMessageBox(
                AboutCaption, AboutText, true, MessageBoxFlag.OK, device.GUIEnvironment.RootElement, 0);
        }


        /// <summary>
        /// The Irrlicht window has had an event.
        /// </summary>
        /// <param name="p_event"></param>
        /// <returns></returns>
        public bool device_OnEvent(Event p_event)
        {

            
            // !Mouse event  (we do this so that we don't process the rest of this each mouse move
            if (p_event.Type != EventType.MouseInputEvent)
            {
                //Keyboard event
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
                        case KeyCode.Home:
                            if (!ctrlHeld)
                                DoMotorAction(p_event.KeyCode, p_event.KeyPressedDown, false);

                            doKeyHeldStore(p_event.KeyCode,p_event.KeyPressedDown);
                            break;
                        case KeyCode.Key_P:
                            if (p_event.KeyPressedDown)
                            {
                                uint texcount = 0;
                                if (textureMan != null)
                                    texcount = textureMan.TextureCacheCount;
                                m_log.DebugFormat("FullUpdateCount:{0}, PrimCount:{1}, TextureCount:{2}, UniquePrim:{3}", primcount, Entities.Count, texcount,m_MeshFactory.UniqueObjects);
                            }
                            break;
                        case KeyCode.Key_C:
                            if (p_event.KeyPressedDown)
                            {
                                if (textureMan != null)
                                {
                                    textureMan.ClearMemoryCache();

                                }
                            }
                            break;
                    }
                }
                processHeldKeys();
            }

            if (p_event.Type == EventType.MouseInputEvent)
            {
                return MouseEventProcessor(p_event);
            }

            return false;
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
                    //cam.SwitchMode(ECameraMode.Build);
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
                    cam.SwitchMode(ECameraMode.Build);
                    // Pick!

                    cam.ResetMouseOffsets();
                    Vector3D[] projection = cam.ProjectRayPoints(p_event.MousePosition, WindowWidth_DIV2,WindowHeight_DIV2, aspect);
                    Line3D projectedray = new Line3D(projection[0], projection[1]);

                    Vector3D collisionpoint = new Vector3D(0, 0, 0);
                    Triangle3D tri = new Triangle3D(0, 0, 0, 0, 0, 0, 0, 0, 0);
                    
                    // Check if we have a node under the mouse
                    SceneNode node = triPicker.GetSceneNodeFromRay(projectedray, 0x0128, true, cam.SNCamera.Position); //smgr.CollisionManager.GetSceneNodeFromScreenCoordinates(new Position2D(p_event.MousePosition.X, p_event.MousePosition.Y), 0, false);
                    if (node == null)
                    {
                        if (mts != null)
                        {
                            // Collide test against the terrain
                            if (smgr.CollisionManager.GetCollisionPoint(projectedray, mts, out collisionpoint, out tri))
                            {

                                //if (collisionpoint != null)
                                //{
                                //m_log.DebugFormat("Found point: <{0},{1},{2}>", collisionpoint.X, collisionpoint.Y, collisionpoint.Z);
                                //}
                                if (cam.CameraMode == ECameraMode.Build)
                                {
                                    cam.SetTarget(collisionpoint);
                                    cam.SNtarget = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Sometimes the terrain picker returns weird values.
                        // If it's weird try the general 'everything' triangle picker.
                        m_log.WarnFormat("[PICK]: Picked <{0},{1},{2}>",node.Position.X,node.Position.Y,node.Position.Z);
                        if (node.Position.X == 0 && node.Position.Z == 0)
                        {
                            if (mts != null)
                            {
                                if (smgr.CollisionManager.GetCollisionPoint(projectedray, mts, out collisionpoint, out tri))
                                {

                                    //if (collisionpoint != nuYesll)
                                    //{
                                    //m_log.DebugFormat("Found point: <{0},{1},{2}>", collisionpoint.X, collisionpoint.Y, collisionpoint.Z);
                                    //}
                                    if (cam.CameraMode == ECameraMode.Build)
                                    {
                                        cam.SetTarget(collisionpoint);
                                        cam.SNtarget = null;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Target the node
                            if (cam.CameraMode == ECameraMode.Build)
                            {
                                cam.SetTarget(node.Position);
                                cam.SNtarget = node;
                            }
                        }
                    }
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

                // Handles Orbiting
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
            return false;

        }

        #endregion
        /// <summary>
        /// Callback from the texture Manager
        /// Enqueues an object to be textured
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="vObj"></param>
        /// <param name="AssetID"></param>
        public void textureCompleteCallback(string tex, VObject vObj, UUID AssetID)
        {
            TextureComplete tx = new TextureComplete();
            tx.texture = tex;
            tx.vObj = vObj;
            tx.textureID = AssetID;
            assignTextureQueue.Enqueue(tx);
        }

        public void SetSelfVObj(VObject self)
        {
            if (UserAvatar == null)
            {
                UserAvatar = self;
                AVControl.AvatarNode = UserAvatar.node;
            }
        }

    }
    
    /// <summary>
    /// embedded struct for texture complete object.
    /// </summary>
    public struct TextureComplete 
    {
        public VObject vObj;
        public string texture;
        public UUID textureID;
        
    }
}
