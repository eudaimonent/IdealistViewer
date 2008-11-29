using System;
using System.IO;
using System.Xml;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial15
{
    class Tutorial15
    {
        static IrrlichtDevice device;

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
            SceneManager smgr = device.SceneManager;
            VideoDriver driver = device.VideoDriver;
            GUIEnvironment env = device.GUIEnvironment;

            // load the scene
            smgr.LoadScene("../../media/example.irr");

            // add a user controlled camera
            smgr.AddCameraSceneNodeFPS(smgr.RootSceneNode, 100, 100, false);

            int lastFPS = -1;
            while (device.Run())
            {
                if (device.WindowActive)
                {
                    driver.BeginScene(true, true, new Color(0, 0, 0, 0));
                    smgr.DrawAll();
                    env.DrawAll();
                    driver.EndScene();

                    int fps = device.VideoDriver.FPS;
                    if (lastFPS != fps)
                    {
                        device.WindowCaption = "Per pixel lighting example - Irrlicht Engine " +
                            "FPS:" + fps.ToString();
                        lastFPS = fps;
                    }
                }
            }

            /*
            In the end, delete the Irrlicht device.
            */
            device.Dispose();

        }
    }
} 