using System;
using System.IO;
using System.Xml;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial09
{
    class Tutorial09
    {
        static IrrlichtDevice device;
        static string StartUpModelFile = string.Empty;
        static string MessageText = string.Empty;
        static string Caption = string.Empty;
        static AnimatedMeshSceneNode Model;
        static SceneNode SkyBox = null;
        static bool programClosing = false;

        static void Main(string[] args)
        {
            // ask user for driver
            DriverType driverType = DriverType.Direct3D9;

            // Ask user to select driver:
            StringBuilder sb = new StringBuilder();
            sb.Append("Please select the driver you want for this example:\n");
            sb.Append("(a) Direct3D 9.0c\n(b) Direct3D 8.1\n(c) OpenGL 1.5\n");
            sb.Append("(d) Software Renderer\n(e) Apfelbaum Software Renderer\n");
            sb.Append("(f) Null Device\n(otherKey) exit\n\n");

            // Get the user's input:
            TextReader tIn = Console.In;
            TextWriter tOut = Console.Out;
            tOut.Write(sb.ToString());
            string input = tIn.ReadLine();

            // Select device based on user's input:
            switch (input)
            {
                case "a":
                    driverType = DriverType.Direct3D9;
                    break;
                case "b":
                    driverType = DriverType.Direct3D8;
                    break;
                case "c":
                    driverType = DriverType.OpenGL;
                    break;
                case "d":
                    driverType = DriverType.Software;
                    break;
                case "e":
                    driverType = DriverType.Software2;
                    break;
                case "f":
                    driverType = DriverType.Null;
                    break;
                default:
                    return;
            }

            // Create device and exit if creation fails:
            device = new IrrlichtDevice(driverType, new Dimension2D(640, 480),
                32, false, false, false, false);

            if (device == null)
            {
                tOut.Write("Device creation failed.");
                return;
            }

            /* set the event receiver. We add a simple delegate that will handle every event
             */
            device.OnEvent += new OnEventDelegate(device_OnEvent);

            device.Resizeable = true;
            device.WindowCaption = "Irrlicht Engine - Loading...";
            VideoDriver driver = device.VideoDriver;
            GUIEnvironment env = device.GUIEnvironment;
            SceneManager smgr = device.SceneManager;
            GUIElement guiRoot = env.RootElement;
            SceneNode sceneRoot = smgr.RootSceneNode;

            XmlReader xml = XmlReader.Create(
                new StreamReader("../../medias/config.xml"));
            while (xml != null && xml.Read())
            {
                switch (xml.NodeType)
                {
                    case XmlNodeType.Text:
                        MessageText = xml.ReadContentAsString();
                        break;
                    case XmlNodeType.Element:
                        if (xml.Name.Equals("startUpModel"))
                            StartUpModelFile = xml.GetAttribute("file");
                        else if (xml.Name.Equals("messageText"))
                            Caption = xml.GetAttribute("caption");
                        break;
                }
            }

            GUISkin skin = env.Skin;
            //We set the skin as a metallic windows skin
            skin = env.CreateSkin(GUISkinTypes.WindowsMetallic);

            // set a nicer font
            GUIFont font = env.GetFont("../../medias/fonthaettenschweiler.bmp");
            //if (font) skin.Font=(font;

            // create menu
            GUIContextMenu menu = env.AddMenu(guiRoot, -1);
            menu.AddItem("File", -1, true, true);
            menu.AddItem("View", -1, true, true);
            menu.AddItem("Help", -1, true, true);
 
            GUIContextMenu submenu;
            submenu = menu.GetSubMenu(0);
            submenu.AddItem("Open Model File...", 100, true, false);
            submenu.AddSeparator();
            submenu.AddItem("Quit", 200, true, false);

            submenu = menu.GetSubMenu(1);
            submenu.AddItem("toggle sky box visibility", 300, true, false);
            submenu.AddItem("toggle model debug information", 400, true, false);
            submenu.AddItem("model material", -1, true, true);

            submenu = submenu.GetSubMenu(2);
            submenu.AddItem("Solid", 610, true, false);
            submenu.AddItem("Transparent", 620, true, false);
            submenu.AddItem("Reflection", 630, true, false);

            submenu = menu.GetSubMenu(2);
            submenu.AddItem("About", 500, true, false);

            //We want a toolbar, onto which we can place colored buttons and important looking stuff like a senseless combobox.

            // create toolbar
            GUIToolBar bar = env.AddToolBar(guiRoot, -1);
            bar.AddButton(1102, "", "", driver.GetTexture("../../medias/open.bmp"), null, false, false);
            bar.AddButton(1103, "", "", driver.GetTexture("../../medias/help.bmp"), null, false, false);
            bar.AddButton(1104, "", "", driver.GetTexture("../../medias/tools.bmp"), null, false, false);

            // create a combobox with some senseless texts
            GUIComboBox box = env.AddComboBox(new Rect(new Position2D(100, 5), new Position2D(200, 25)), bar, -1);
            box.AddItem("Bilinear");
            box.AddItem("Trilinear");
            box.AddItem("Anisotropic");
            box.AddItem("Isotropic");
            box.AddItem("Psychedelic");
            box.AddItem("No filtering");

            //To make the editor look a little bit better, we disable transparent gui elements,
            //and add a Irrlicht Engine logo. In addition, a text, which will show the current
            //frame per second value is created, and the window caption changed.

            // disable alpha
            /*for (int i=0; i<EGDC_COUNT ; ++i)
            {
                Color col = env.getSkin().getColor((gui::EGUI_DEFAULT_COLOR)i);
                col.SetAlpha(255);
                env.Skin.SetColor((gui::EGUI_DEFAULT_COLOR)i, col);
            }*/
            // add the irrlicht engine logo
            GUIImage img = env.AddImage(new Rect(new Position2D(22, 429), new Position2D(108, 460)), guiRoot, 666, "");
            img.Image = driver.GetTexture("../../medias/irrlichtlogoaligned.jpg");

            // add a tabcontrol
            createToolBox();

            // create fps text
            GUIStaticText fpstext = env.AddStaticText(string.Empty,
                new Rect(new Position2D(210, 26), new Position2D(270, 41)),
                true, false, guiRoot,777, true);

            device.WindowCaption = Caption;

            showAboutText();
            loadModel(StartUpModelFile);

            // add skybox
            SkyBox = smgr.AddSkyBoxSceneNode(sceneRoot,
                new Texture[] {
                driver.GetTexture("../../medias/irrlicht2_up.jpg"),
                driver.GetTexture("../../medias/irrlicht2_dn.jpg"),
                driver.GetTexture("../../medias/irrlicht2_lf.jpg"),
                driver.GetTexture("../../medias/irrlicht2_rt.jpg"),
                driver.GetTexture("../../medias/irrlicht2_ft.jpg"),
                driver.GetTexture("../../medias/irrlicht2_bk.jpg")
                }, -1);

            // add a camera scene node
            CameraSceneNode camera = smgr.AddCameraSceneNodeMaya(sceneRoot, 100, 100, 100, -1);

            /*Finally we simply have to draw everything, that's all.*/
            //CameraSceneNode camera = smgr.AddCameraSceneNodeFPS(sceneRoot,100,100,false);
            camera.Position = new Vector3D(-50, 50, -150);
            int lastFPS = -1;

            while (device.Run() && !programClosing)
            {
                if (device.WindowActive)
                {
                    driver.BeginScene(true, true, new Color(0, 200, 200, 200));
                    smgr.DrawAll();
                    env.DrawAll();
                    driver.EndScene();

                    int fps = device.VideoDriver.FPS;
                    if (lastFPS != fps)
                    {
                        device.WindowCaption = "Irrlicht Engine - Quake 3 Map example" +
                            " FPS:" + fps.ToString();
                        fpstext.Text = fps.ToString();
                        lastFPS = fps;

                    }
                }
            }
            /*
            In the end, delete the Irrlicht device.
            */
            device.Dispose();
        }


        /*The three following functions do several stuff used by the mesh viewer.
         * The first function showAboutText() simply displays a messagebox with a caption
         * and a message text. The texts will be stored in the MessageText and Caption
         * variables at startup.*/
        static void showAboutText()
        {
            // create modal message box with the text
            // loaded from the xml file.
            device.GUIEnvironment.AddMessageBox(
                Caption, MessageText, true, MessageBoxFlag.OK, device.GUIEnvironment.RootElement, 0);
        }

        /* The second function loadModel() loads a model and displays it using an
         * addAnimatedMeshSceneNode and the scene manager. Nothing difficult. It also
         * displays a short message box, if the model could not be loaded.*/
        static void loadModel(string filename)
        {
            // load a model into the engine
            if (Model != null)
                Model.Remove();
            Model = null;

            AnimatedMesh m = device.SceneManager.GetMesh(filename);
            if (m == null)
            {
                // model could not be loaded
                if (StartUpModelFile != filename)
                    device.GUIEnvironment.AddMessageBox(
                        Caption, "The model could not be loaded." +
                        " Maybe it is not a supported file format.",
                        false, MessageBoxFlag.OK, device.GUIEnvironment.RootElement, 0);
                return;
            }

            // set default material properties
            Model = device.SceneManager.AddAnimatedMeshSceneNode(m);
            Model.SetMaterialType(MaterialType.TransparentAddColor);
            Model.SetMaterialFlag(MaterialFlag.Lighting, false);
            Model.DebugDataVisible = DebugSceneType.Full;
        }

        /* Finally, the third function creates a toolbox window. In this simple mesh viewer,
         * this toolbox only contains a tab control with three edit boxes for changing
         * the scale of the displayed model.*/
        static void createToolBox()
        {
            // remove tool box if already there
            GUIEnvironment env = device.GUIEnvironment;
            GUIElement root = env.RootElement;
            GUIElement e = root.GetElementFromID(5000, true);
            if (e != null) e.Remove();

            // create the toolbox window
            GUIWindow wnd = env.AddWindow(
                new Rect(new Position2D(450, 25), new Position2D(640, 480)),
                false, "Toolset", root, 5000);

            // create tab control and tabs
            GUITabControl tab = env.AddTabControl(
            new Rect(new Position2D(2, 20), new Position2D(640 - 452, 480 - 7)),
            wnd, true, true, -1);
            GUITab t1 = tab.AddTab("Scale", -1);
            GUITab t2 = tab.AddTab("Empty Tab", -1);

            // add some edit boxes and a button to tab one
            //env.AddEditBox("1.0", new Rect(40,50,130,70), true, t1, 901);
            env.AddEditBox("1.0",
                new Rect(new Position2D(40, 50), new Position2D(130, 70)), true, t1, 901);
            env.AddEditBox("1.0",
                new Rect(new Position2D(40, 80), new Position2D(130, 100)), true, t1, 902);
            env.AddEditBox("1.0",
                new Rect(new Position2D(40, 110), new Position2D(130, 130)), true, t1, 903);
            env.AddButton(new
                Rect(new Position2D(10, 150), new Position2D(100, 190)), t1, 1101, "set");

            // bring irrlicht engine logo to front, because it
            // now may be below the newly created toolbox
            root.BringToFront(root.GetElementFromID(666, true));
        }

        /*To get all the events sent by the GUI Elements, we need to create an event receiver.
          This one is really simple. If an event occurs, it checks the id of the caller and
          the event type, and starts an action based on these values. For example, if a menu
          item with id 100 was selected, if opens a file-open-dialog.*/
        static bool device_OnEvent(Event p_event)
        {
            if (p_event.Type == EventType.GUIEvent)
            {
                int id = p_event.Caller.ID;
                GUIEnvironment env = device.GUIEnvironment;
                switch (p_event.GUIEvent)
                {
                    case GUIEventType.MenuItemSelected:
                        // a menu item was clicked
                        GUIContextMenu menu = ((GUIContextMenu)p_event.Caller);
                        id = menu.GetItemCommandID(menu.SelectedItem);
                        switch (id)
                        {
                            case 100:
                                env.AddFileOpenDialog("Please select a model file to open", false, device.GUIEnvironment.RootElement, 0);
                                break;
                            case 200: // File -> Quit
                                programClosing = true;
                                //device.CloseDevice();
                                break;
                            case 300: // View -> Skybox
                                SkyBox.Visible = SkyBox.Visible ? false : true;
                                break;
                            case 400: // View -> Debug Information
                                if (Model != null)
                                {
                                    if (Model.DebugDataVisible == DebugSceneType.Full)
                                    {
                                        Model.DebugDataVisible = DebugSceneType.Off;
                                    }
                                    else
                                    {
                                        Model.DebugDataVisible = DebugSceneType.Full;
                                    }
                                }
                                break;
                            case 500: // Help->About
                                showAboutText();
                                break;
                            case 610: // View -> Material -> Solid
                                if (Model != null)
                                    Model.SetMaterialType(MaterialType.Solid);
                                break;
                            case 620: // View -> Material -> Transparent
                                if (Model != null)
                                    Model.SetMaterialType(MaterialType.TransparentAddColor);
                                break;
                            case 630: // View -> Material -> Reflection
                                if (Model != null)
                                    Model.SetMaterialType(MaterialType.SphereMap);
                                break;
                        }
                        break;//case GUIEventType.MenuItemSelected:

                    case GUIEventType.FileSelected:
                        // load the model file, selected in the file open dialog
                        GUIFileOpenDialog dialog = ((GUIFileOpenDialog)p_event.Caller);
                        loadModel(dialog.Filename);
                        break;

                    case GUIEventType.ButtonClicked:
                        GUIElement root = env.RootElement;
                        switch (id)
                        {
                            case 1101:
                                // set scale

                                Vector3D scale = new Vector3D();
                                string s;
                                s = root.GetElementFromID(901, true).Text;
                                scale.X = float.Parse(s);
                                s = root.GetElementFromID(902, true).Text;
                                scale.Y = float.Parse(s);
                                s = root.GetElementFromID(903, true).Text;
                                scale.Z = float.Parse(s); ;
                                if (Model != null)
                                    Model.Scale = scale;
                                break;
                            case 1102:
                                env.AddFileOpenDialog("Please select a model file to open", true, root, -1);
                                break;
                            case 1103:
                                showAboutText();
                                break;
                            case 1104:
                                createToolBox();
                                break;
                        }
                        break;
                }
                return false;
            }
            return false;
        }
    }
} 