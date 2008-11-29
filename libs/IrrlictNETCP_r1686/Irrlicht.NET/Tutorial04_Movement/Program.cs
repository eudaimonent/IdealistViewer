using System;
using System.IO;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial04
{
    class Tutorial04
    {
        static SceneNode node = null;

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
            IrrlichtDevice device = new IrrlichtDevice(driverType, new Dimension2D(640, 480), 32, false, true, true, true);
            device.FileSystem.WorkingDirectory = "../../medias"; //We set Irrlicht's current directory to %application directory%/media

            if (device == null)
            {
                tOut.Write("Device creation failed.");
                return;
            }

            /* set the event receiver*/
            device.OnEvent += new OnEventDelegate(device_OnEvent); //We had a simple delegate that will handle every event

         /*
         Get a pointer to the video driver and the SceneManager so that
         we do not always have to write device->getVideoDriver() and
         device->getSceneManager().
         */
         // I just left these lines here for example purposes:
         //irrv.IVideoDriver driver = device.VideoDriver;
         //irrs.ISceneManager smgr = device.SceneManager;
         SceneManager smgr=device.SceneManager;
         VideoDriver driver=device.VideoDriver;

         /*Create the node for moving it with the 'W' and 'S' key. We create a
          'test node', which is a cube built in into the engine for testing purposes.
          We place the node a (0,0,30) and we assign a texture to it to let it look a
          little bit more interesting.*/
            //node = smgr.AddCubeSceneNode(10, smgr.RootSceneNode, -1);
            node = smgr.AddSphereSceneNode(6, 16, smgr.RootSceneNode);

            node.Position=new Vector3D(0,0,30);
         node.SetMaterialTexture(0,driver.GetTexture("wall.bmp"));
            node.SetMaterialFlag(MaterialFlag.Lighting, false);

         /* Now we create another node, moving using a scene node animator. Scene
            node animators modify scene nodes and can be attached to any scene node
            like mesh scene nodes, billboards, lights and even camera scene nodes.
            Scene node animators are not only able to modify the position of a scene
            node, they can also animate the textures of an object for example. We create
            a test scene node again an attach a 'fly circle' scene node to it, letting
            this node fly around our first test scene node.*/
            SceneNode n = smgr.AddCubeSceneNode(10, smgr.RootSceneNode, 0);
         n.SetMaterialTexture(0,driver.GetTexture("t351sml.jpg"));
            n.SetMaterialFlag(MaterialFlag.Lighting, false);

         Animator anim = smgr.CreateFlyCircleAnimator(new Vector3D(0,0,30),20,0.001f);
         n.AddAnimator(anim);
         /*The last scene node we add to show possibilities of scene node animators
           is a md2 model, which uses a 'fly straight' animator to run between two
           points.*/

         AnimatedMeshSceneNode anms = smgr.AddAnimatedMeshSceneNode(
            smgr.GetMesh("sydney.md2"));
         if (!anms.Equals(null))
         {
            anim= smgr.CreateFlyStraightAnimator(
                    new Vector3D(100,0,60),new Vector3D(-100,0,60),10000,true);
            anms.AddAnimator(anim);

            /*To make to model look better, we disable lighting (we have created no lights,
              and so the model would be black), set the frames between which the animation
              should loop, rotate the model around 180 degrees, and adjust the animation
              speed and the texture.
              To set the right animation (frames and speed), we would also be able to just
              call "anms->setMD2Animation(scene::EMAT_RUN)" for the 'run' animation
              instead of "setFrameLoop" and "setAnimationSpeed", but this only works with
              MD2 animations, and so you know how to start other animations.*/
            anms.Position=new Vector3D(0,0,40);
            anms.SetMaterialFlag(MaterialFlag.Lighting,false);
            anms.SetFrameLoop(320,360);
            anms.AnimationSpeed=30;
            anms.Rotation=new Vector3D(0,180,0);
            anms.SetMaterialTexture(0,driver.GetTexture("sydney.BMP"));
         }

         /*To be able to look at and move around in this scene, we create a first person
          * shooter style camera and make the mouse cursor invisible.*/
            CameraSceneNode camera = smgr.AddCameraSceneNodeFPS(smgr.RootSceneNode, 100, 100, false);
         camera.Position=new Vector3D(0,0,0);
         device.CursorControl.Visible=false;
         
         /*
         We have done everything, so lets draw it. We also write the current
         frames per second and the drawn primitives to the caption of the
         window.
         */
         int lastFPS = -1;

         while (device.Run())
         {
            if (device.WindowActive)
            {
                    device.VideoDriver.BeginScene(true, true, new Color(255, 100, 101, 140));
               device.SceneManager.DrawAll();
               device.VideoDriver.EndScene();

               int fps = device.VideoDriver.FPS;
               if (lastFPS != fps)
               {
                  device.WindowCaption = "Irrlicht Engine - Movement example " +
                     "FPS:" + fps.ToString();
                  lastFPS = fps;
               }
            }
         }

         /*
         In the end, delete the Irrlicht device.
         */
         // Instead of device->drop, we'll use:
            device.Dispose();
      }

        static bool device_OnEvent(Event p_e)
        {
            if (node != null && p_e.Type == EventType.KeyInputEvent &&
                !p_e.KeyPressedDown)
            {
                switch (p_e.KeyCode)
                {
                    case KeyCode.Key_W:
                    case KeyCode.Key_S:
                        {
                            Vector3D v = node.Position;
                            v.Y += p_e.KeyCode == KeyCode.Key_W ? 2.0f : -2.0f;
                            node.Position = v;
                        }
                        return true;
                }
            }

            return false;
        }

   }
} 