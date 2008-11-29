using System;
using System.IO;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial02
{
    class Tutorial02
    {
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
            IrrlichtDevice device = new IrrlichtDevice(driverType, new Dimension2D(640, 480), 32, false, true, true,true);
            device.FileSystem.WorkingDirectory = "../../medias"; //We set Irrlicht's current directory to %application directory%/media

            if (device == null)
            {
                tOut.Write("Device creation failed.");
                return;
            }

            /*
         To display the Quake 3 map, we first need to load it. Quake 3 maps
         are packed into .pk3 files wich are nothing other than .zip files.
         So we add the .pk3 file to our FileSystem. After it was added,
         we are able to read from the files in that archive as they would
         directly be stored on disk.
         */
         device.FileSystem.AddZipFileArchive("map-20kdm2.pk3",true,true);

         /*
         Now we can load the mesh by calling getMesh(). We get a pointer returned
         to a IAnimatedMesh. As you know, Quake 3 maps are not really animated,
         they are only a huge chunk of static geometry with some materials
         attached. Hence the IAnimated mesh consists of only one frame,
         so we get the "first frame" of the "animation", which is our quake level
         and create an OctTree scene node with it, using addOctTreeSceneNode().
         The OctTree optimizes the scene a little bit, trying to draw only geometry
         which is currently visible. An alternative to the OctTree would be a
         AnimatedMeshSceneNode, which would draw always the complete geometry of
         the mesh, without optimization. Try it out: Write addAnimatedMeshSceneNode
         instead of addOctTreeSceneNode and compare the primitives drawed by the
         video driver. (There is a getPrimitiveCountDrawed() method in the
         IVideoDriver class). Note that this optimization with the Octree is only
         useful when drawing huge meshes consiting of lots of geometry.
         */
         AnimatedMesh mesh = device.SceneManager.GetMesh("20kdm2.bsp");
         SceneNode node = null;

         if (mesh != null)
                node = device.SceneManager.AddOctTreeSceneNode(mesh, device.SceneManager.RootSceneNode, -1, 128);

         /*
         Because the level was modelled not around the origin (0,0,0), we translate
         the whole level a little bit.
         */
         if (node != null)
            node.Position = (new Vector3D(-1300, -144, -1249));

         /*
         Now we only need a Camera to look at the Quake 3 map.
         And we want to create a user controlled camera. There are some
         different cameras available in the Irrlicht engine. For example the
         Maya Camera which can be controlled compareable to the camera in Maya:
         Rotate with left mouse button pressed, Zoom with both buttons pressed,
         translate with right mouse button pressed. This could be created with
         addCameraSceneNodeMaya(). But for this example, we want to create a
         camera which behaves like the ones in first person shooter games (FPS).
         */
            device.SceneManager.AddCameraSceneNodeFPS(device.SceneManager.RootSceneNode, 100, 100, false);

         /*
         The mouse cursor needs not to be visible, so we make it invisible.
         */
         device.CursorControl.Visible = false;

         /*
         We have done everything, so lets draw it. We also write the current
         frames per second and the drawn primitives to the caption of the
         window. The 'if (device->isWindowActive())' line is optional, but
         prevents the engine render to set the position of the mouse cursor
         after task switching when other program are active.
         */
         int lastFPS = -1;

         while (device.Run())
         {
            if (device.WindowActive)
            {
               device.VideoDriver.BeginScene(true, true, new Color(0, 200, 200, 200));
               device.SceneManager.DrawAll();
               device.VideoDriver.EndScene();

               int fps = device.VideoDriver.FPS;
               if (lastFPS != fps)
               {
                  device.WindowCaption = "Irrlicht Engine - Quake 3 Map example " +
                     "FPS:" + fps.ToString();
                  lastFPS = fps;
               }
            }
         }

         //In the end, delete the Irrlicht device.
            device.Dispose();
        }
    }
}
