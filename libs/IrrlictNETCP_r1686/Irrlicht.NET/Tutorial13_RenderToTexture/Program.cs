using System;
using System.IO;
using System.Xml;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial13
{
    class Tutorial13
    {
        static IrrlichtDevice device;
        static string StartUpModelFile = string.Empty;
        static string MessageText = string.Empty;
        static string Caption = string.Empty;

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
            device = new IrrlichtDevice(driverType, new Dimension2D(640, 480), 32, false, true, true, true);
            device.FileSystem.WorkingDirectory = "../../media"; //We set Irrlicht's current directory to %application directory%/media

            /*************************************************/
            /* First, we add standard stuff to the scene: A nice irrlicht engine logo,
               a small help text, a user controlled camera, and we disable the mouse
               cursor.*/
            SceneManager smgr = device.SceneManager;
            VideoDriver driver = device.VideoDriver;
            GUIEnvironment env = device.GUIEnvironment;

            // load and display animated fairy mesh

            AnimatedMeshSceneNode fairy = smgr.AddAnimatedMeshSceneNode(
                smgr.GetMesh("faerie.md2"));

            if (fairy != null)
            {
                fairy.SetMaterialTexture(0,
                    driver.GetTexture("faerie2.bmp")); // set diffuse texture
                fairy.SetMaterialFlag(MaterialFlag.Lighting, true); // enable dynamic lighting
                fairy.GetMaterial(0).Shininess = 20.0f; // set size of specular highlights
                fairy.Position = new Vector3D(-10, 0, -100);
            }

            // add white light
            LightSceneNode light = smgr.AddLightSceneNode(null,
                new Vector3D(-15, 5, -105), new Colorf(0, 1.0f, 1.0f, 1.0f), 10, -1);

            // set ambient light
            //smgr.SetAmbientLight(Colorf.FromBCL(new System.Drawing.Color(0, 60, 60, 60)));

            // add fps camera
            CameraSceneNode fpsCamera = smgr.AddCameraSceneNodeFPS(null, 100, 100, false);
            fpsCamera.Position = new Vector3D(-50, 50, -150);

            // disable mouse cursor
            device.CursorControl.Visible = false;

            // create test cube
            SceneNode test = smgr.AddCubeSceneNode(60, null, -1);

            // let the cube rotate and set some light settings
            Animator anim = smgr.CreateRotationAnimator(
                new Vector3D(0.3f, 0.3f, 0));

            test.Position = new Vector3D(-100, 0, -100);
            test.SetMaterialFlag(MaterialFlag.Lighting, false); // disable dynamic lighting
            test.AddAnimator(anim);
            anim.Dispose();

            // set window caption
            device.WindowCaption =
                "Irrlicht Engine - Render to Texture and Specular Highlights example";


            // create render target
            Texture rt = null;
            CameraSceneNode fixedCam = null;


            if (driver.QueryFeature(VideoDriverFeature.RenderToTarget))
            {
                rt = driver.CreateRenderTargetTexture(new Dimension2D(256, 256));
                test.SetMaterialTexture(0, rt); // set material of cube to render target

                // add fixed camera
                fixedCam = smgr.AddCameraSceneNode(null);

                fixedCam.Position = new Vector3D(10, 10, -80);
                fixedCam.Target = new Vector3D(-10, 10, -100);
            }
            else
            {
                // create problem text
                GUISkin skin = env.Skin;
                GUIFont font = env.GetFont("../../media/fonthaettenschweiler.bmp");
                // Not implemented
                //if (font!=null)
                //    skin.Font.SetFont(font);

                GUIStaticText text = env.AddStaticText(
                    "Your hardware or this renderer is not able to use the \n" +
                    "render to texture feature. RTT Disabled.",
                    new Rect(new Position2D(150, 20), new Position2D(470, 60)),
                    true, false, null, -1, true);

                text.OverrideColor = new Color(100, 255, 255, 255);
            }
            while (device.Run())
                if (device.WindowActive)
                {
                    driver.BeginScene(true, true, Color.Black);

                    if (rt != null)
                    {
                        // draw scene into render target

                        // set render target texture
                        driver.SetRenderTarget(rt, true, true, Color.Blue);

                        // make cube invisible and set fixed camera as active camera
                        test.Visible = false;
                        smgr.ActiveCamera = fixedCam;

                        // draw whole scene into render buffer
                        smgr.DrawAll();

                        // set back old render target
                        driver.SetRenderTarget(null, false, false, Color.White);

                        // make the cube visible and set the user controlled camera as active one
                        test.Visible = true;
                        smgr.ActiveCamera = fpsCamera;
                    }

                    // draw scene normally
                    smgr.DrawAll();
                    env.DrawAll();

                    driver.EndScene();
                }

            if (rt != null)
                rt.Dispose(); // drop render target because we
            // created if with a create() method

            device.Dispose(); // drop device
        }
    }
} 