using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial14
{
    class Tutorial14
    {
        static IrrlichtDevice device;
        //Resolution
        static int ResX = 800;
        static int ResY = 600;
        //cameras
        static CameraSceneNode[] camera = { null, null, null, null };
        static bool fullScreen = false;
        static SceneNode nodeUnderCursor=null;
        static SceneNode nodeSelected = null;

        //Use SplitScreen?
        static bool SplitScreen = true;

        public static bool OnEvent(Event p_event)
        {
            if (p_event.Type == EventType.MouseInputEvent)
            {
                if (p_event.MouseInputEvent == MouseInputEvent.MMousePressedDown)
                {
                    if (camera[3].SceneNodeType == SceneNodeType.Camera)
                    {
                        //camera[3].SceneNodeType = SceneNodeType.CameraFPS;
                        Vector3D tempPos = camera[3].Position;
                        Vector3D tempTarget = camera[3].Target;
                        //Hide mouse
                        device.CursorControl.Visible = false;
                        camera[3] = device.SceneManager.AddCameraSceneNodeFPS(
                            null, 100, 100, false);
                        camera[3].Position = tempPos;
                        camera[3].Target = tempTarget;
                    }
                    else if (camera[3].SceneNodeType == SceneNodeType.CameraFPS)
                    {
                        //Show mouse
                        device.CursorControl.Visible = true;
                        //camera[3].SceneNodeType = SceneNodeType.Camera;
                        Vector3D tempPos = camera[3].Position;
                        Vector3D tempTarget = camera[3].Target;
                        camera[3] = device.SceneManager.AddCameraSceneNode(null);
                        camera[3].Position = tempPos;
                        camera[3].Target = tempTarget;

                    }
                }
                else if (p_event.MouseInputEvent == MouseInputEvent.MouseMoved)
                {
                    nodeUnderCursor=device.SceneManager.CollisionManager.
                        GetSceneNodeFromScreenCoordinates(device.CursorControl.Position);
                }
                else if (p_event.MouseInputEvent == MouseInputEvent.LMousePressedDown)
                {
                    SceneNode tempSel = device.SceneManager.CollisionManager.
                        GetSceneNodeFromScreenCoordinates(device.CursorControl.Position);
                    if (nodeSelected == tempSel)
                        nodeSelected = null;
                    else
                        nodeSelected = tempSel;
                }

            }

            // check if user presses the key 'S'
            if (p_event.Type == EventType.KeyInputEvent &&
                !p_event.KeyPressedDown)
            {
                switch (p_event.KeyCode)
                {
                    case KeyCode.Key_S:
                        SplitScreen = !SplitScreen;
                        //break;
                        return true;
                }

                //Send all other events to camera4
                if (camera[3] != null)
                {
                    camera[3].OnEvent(p_event);
                }
            }
            return false;
        }

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
            device = new IrrlichtDevice(driverType, new Dimension2D(ResX, ResY), 32, fullScreen, true, true, true);
            device.FileSystem.WorkingDirectory = "../../media"; //We set Irrlicht's current directory to %application directory%/media
            device.OnEvent += new OnEventDelegate(OnEvent); //We had a simple delegate that will handle every event
            SceneManager smgr = device.SceneManager;
            VideoDriver driver = device.VideoDriver;

            //Load model
            AnimatedMesh model = smgr.GetMesh("sydney.md2");
            AnimatedMeshSceneNode model_node = smgr.AddAnimatedMeshSceneNode(model);
            //Load texture
            Texture texture = driver.GetTexture("sydney.bmp");
            model_node.SetMaterialTexture(0, texture);
            //Disable lighting (we've got no light)
            model_node.SetMaterialFlag(MaterialFlag.Lighting, false);

            //Load map
            device.FileSystem.AddZipFileArchive("map-20kdm2.pk3",true,true);
            AnimatedMesh map = smgr.GetMesh("20kdm2.bsp");
            SceneNode map_node = smgr.AddOctTreeSceneNode(map.GetMesh(0),null,-1,128);
            //Set position
            map_node.Position=new Vector3D(-850, -220, -850);

            //-=Create 3 fixed and one user-controlled cameras=-
            //Front
            camera[0] = smgr.AddCameraSceneNode(null);
            camera[0].Position = new Vector3D(70, 0, 0);
            camera[0].Target= new Vector3D(0, 0, 0);
            //Top
            camera[1] = smgr.AddCameraSceneNode(null);
            camera[1].Position = new Vector3D(0, 70, 0);
            camera[1].Target = new Vector3D(0, 0, 0);
            //Left
            camera[2] = smgr.AddCameraSceneNode(null);
            camera[2].Position = new Vector3D(0, 0, 70);
            camera[2].Target  = new Vector3D(0, 0, 0);
            //User-controlled
            camera[3] = smgr.AddCameraSceneNodeFPS(null,100,100,false);
            //camera[3] = smgr.AddCameraSceneNodeMaya(null, 100, 100, 100,-1);
            camera[3].Position = new Vector3D(70, 70, 70);
            camera[3].Target = new Vector3D(0, 0, 0);


            //Hide mouse
            device.CursorControl.Visible=false;

            //We want to count the fps
            int lastFPS = -1;

            while (device.Run())
            {
                //Set the viewpoint the the whole screen and begin scene
                driver.ViewPort = new Rect(new Position2D(0, 0), new Position2D(ResX, ResY));
                driver.BeginScene(true, true, new Color(0, 100, 100, 100));

                //If SplitScreen is used
                if (SplitScreen)
                {
                    //Activate camera1
                    smgr.ActiveCamera = camera[0];
                    //Set viewpoint to the first quarter (left top)
                    driver.ViewPort = new Rect(new Position2D(0, 0), new Position2D(ResX / 2, ResY / 2));
                    //Draw scene
                    smgr.DrawAll();

                    //Activate camera2
                    smgr.ActiveCamera = camera[1];
                    //Set viewpoint to the second quarter (right top)
                    driver.ViewPort = new Rect(new Position2D(ResX / 2, 0), new Position2D(ResX, ResY / 2));
                    //Draw scene
                    smgr.DrawAll();

                    //Activate camera3
                    smgr.ActiveCamera = camera[2];
                    //Set viewpoint to the third quarter (left bottom)
                    driver.ViewPort = new Rect(new Position2D(0, ResY / 2), new Position2D(ResX / 2, ResY));
                    //Draw scene
                    smgr.DrawAll();

                    //Set viewport the last quarter (right bottom)
                    driver.ViewPort = new Rect(new Position2D(ResX / 2, ResY / 2), new Position2D(ResX, ResY));
                }

                //Activate camera4
                smgr.ActiveCamera = camera[3];
                //Draw scene
                smgr.DrawAll();
                if (nodeUnderCursor != null)
                    device.VideoDriver.Draw3DBox(nodeUnderCursor.TransformedBoundingBox, Color.Green);
                if (nodeSelected != null)
                    device.VideoDriver.Draw3DBox(nodeSelected.TransformedBoundingBox, Color.Red);
                driver.EndScene();

                   //Get and show fps
                if (driver.FPS != lastFPS) {
                    device.WindowCaption = "Irrlicht SplitScreen-Example " +
                            " FPS:" + lastFPS.ToString();
                    lastFPS = driver.FPS;
                }
                System.Threading.Thread.Sleep(0);
            }
           
            //Delete device
            device.Dispose();

        }
    }
} 