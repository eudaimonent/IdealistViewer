using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial01
{
    class Tutorial01
    {
        static void Main(string[] args)
        {
            /*The most important function of the engine is the 'createDevice' function. The Irrlicht Device, which is the root object for doing
 everything with the engine, can be created with it. createDevice() has 7 parameters:

                * deviceType: Type of the device. This can currently be the Null device, the Software device, DirectX8, DirectX9, or OpenGL. In 
this example we use EDT_SOFTWARE, but, to try them out, you might want to change it to EDT_NULL, EDT_DIRECTX8 or EDT_OPENGL.
                * windowSize: Size of the window or full screen mode to be created. In this example we use 512x384.
                * bits: Number of bits per pixel when in full screen mode. This should be 16 or 32. This parameter is ignored when running in 
windowed mode.
                * fullscreen: Specifies if we want the device to run in full screen mode or not.
                * stencilbuffer: Specifies if we want to use the stencil buffer for drawing shadows.
                * vsync: Specifies if we want to have vsync enabled. This is only useful in full screen mode.
                * eventReceiver: An object to receive events. We do not want to use this parameter here, and set it to 0.
            */
            IrrlichtDevice device = new IrrlichtDevice(DriverType.Software,
                                                     new Dimension2D(512, 384),
                                                    32, false, true, true, true);
            /*Now we set the caption of the window to some nice text. Note that there is a 'L' in front of the string: the Irrlicht Engine uses
wide character strings when displaying text.
            */
            device.WindowCaption = "Hello World! - Irrlicht Engine Demo";
            device.FileSystem.WorkingDirectory = "../../media"; //We set Irrlicht's current directory to %application directory%/media

            //
            VideoDriver driver = device.VideoDriver;
            SceneManager smgr = device.SceneManager;
            GUIEnvironment guienv = device.GUIEnvironment;

            guienv.AddStaticText("Hello World! This is the Irrlicht Software engine!",
                new Rect(new Position2D(10, 10), new Dimension2D(200, 22)), true, false, guienv.RootElement, -1, false);

            //
            AnimatedMesh mesh = smgr.GetMesh("sydney.md2");
            AnimatedMeshSceneNode node = smgr.AddAnimatedMeshSceneNode(mesh);

            //
            if (node != null)
            {
                node.SetMaterialFlag(MaterialFlag.Lighting, false);
                node.SetFrameLoop(0, 310);
                node.SetMaterialTexture(0, driver.GetTexture("sydney.bmp"));
            }
            //
            CameraSceneNode cam = smgr.AddCameraSceneNode(smgr.RootSceneNode);
            cam.Position = new Vector3D(0, 30, -40);
            cam.Target = new Vector3D(0, 5, 0);

            //
            while (device.Run())
            {
                driver.BeginScene(true, true, new Color(255, 100, 101, 140));
                smgr.DrawAll();
                guienv.DrawAll();
                driver.Draw3DTriangle(new Triangle3D(
                    new Vector3D(0, 0, 0),
                    new Vector3D(10, 0, 0),
                    new Vector3D(0, 10, 0)),
                    Color.Red);

                driver.EndScene();
            }
            //In the end, delete the Irrlicht device.
            device.Dispose();

        }
    }
}