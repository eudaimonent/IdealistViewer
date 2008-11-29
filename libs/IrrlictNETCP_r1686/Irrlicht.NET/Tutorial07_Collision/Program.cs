using System;
using System.IO;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial07
{
    class Tutorial07
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

            if (device == null)
            {
                tOut.Write("Device creation failed.");
                return;
            }
            /*
Get a pointer to the video driver and the SceneManager so that
we do not always have to write device->getVideoDriver() and
device->getSceneManager().
*/

            SceneManager smgr = device.SceneManager;
            VideoDriver driver = device.VideoDriver;

            device.FileSystem.AddZipFileArchive("../../media/map-20kdm2.pk3",true,true);

            AnimatedMesh q3levelmesh = smgr.GetMesh("20kdm2.bsp");
            SceneNode q3node = null;
            if (q3levelmesh != null)
                q3node = smgr.AddOctTreeSceneNode(q3levelmesh.GetMesh(0), smgr.RootSceneNode, -1, 128);

            /*So far so good, we've loaded the quake 3 level like in tutorial 2.
             Now, here comes something different: We create a triangle selector. A
             triangle selector is a class which can fetch the triangles from scene
             nodes for doing different things with them, for example collision
             detection. There are different triangle selectors, and all can be
             created with the ISceneManager. In this example, we create an
             OctTreeTriangleSelector, which optimizes the triangle output a little
             bit by reducing it like an octree. This is very useful for huge meshes
             like quake 3 levels.
             After we created the triangle selector, we attach it to the q3node.
             This is not necessary, but in this way, we do not need to care for the
             selector, for example dropping it after we do not need it anymore.*/

            TriangleSelector selector = null;
            if (q3node != null)
            {
                q3node.Position = new Vector3D(-1370, -130, -1400);
                selector = smgr.CreateOctTreeTriangleSelector(
                    q3levelmesh.GetMesh(0), q3node, 128);
                q3node.TriangleSelector=selector;
            }

            /*We add a first person shooter camera to the scene for being able to move in
              the quake 3 level like in tutorial 2. But this, time, we add a special
              animator to the camera: A Collision Response animator. This thing modifies
              the scene node to which it is attached to in that way, that it may no
              more move through walls and is affected by gravity. The only thing we have
              to tell the animator is how the world looks like, how big the scene node is,
              how gravity and so on. After the collision response animator is attached to
              the camera, we do not have to do anything more for collision detection,
              anything is done automaticly, all other collision detection code below is
              for picking. And please note another cool feature: The collsion response
              animator can be attached also to all other scene nodes, not only to cameras.
              And it can be mixed with other scene node animators. In this way, collision
              detection and response in the Irrlicht engine is really, really easy.
              Now we'll take a closer look on the parameters of
              createCollisionResponseAnimator(). The first parameter is the TriangleSelector,
              which specifies how the world, against collision detection is done looks like.
              The second parameter is the scene node, which is the object, which is affected
              by collision detection, in our case it is the camera. The third defines how big
              the object is, it is the radius of an ellipsoid. Try it out and change the radius
              to smaller values, the camera will be able to move closer to walls after this.
              The next parameter is the direction and speed of gravity. You could set it to
              (0,0,0) to disable gravity. And the last value is just a translation: Without
              this, the ellipsoid with which collision detection is done would be around
              the camera, and the camera would be in the middle of the ellipsoid. But as
              human beings, we are used to have our eyes on top of the body, with which
              we collide with our world, not in the middle of it. So we place the scene
              node 50 units over the center of the ellipsoid with this parameter. And
              that's it, collision detection works now.
             */
            CameraSceneNode camera = smgr.AddCameraSceneNodeFPS(smgr.RootSceneNode, 100, 300, false);
            camera.Position = new Vector3D(-100, 50, -150);
            Animator anim = smgr.CreateCollisionResponseAnimator(
                selector, camera, new Vector3D(30, 50, 30),
                new Vector3D(0, -3, 0), new Vector3D(0, 50, 0), 0);
            camera.AddAnimator(anim);

            /*Because collision detection is no big deal in irrlicht, I'll describe how
              to do two different types of picking in the next section. But before this,
              I'll prepare the scene a little. I need three animated characters which we
              could pick later, a dynamic light for lighting them, a billboard for drawing
              where we found an intersection, and, yes, I need to get rid of this mouse
              cursor. :)*/
            //disable mouse cursor
            device.CursorControl.Visible = false;

            // add billboard
            BillboardSceneNode bill = smgr.AddBillboardSceneNode(
                smgr.RootSceneNode, new Dimension2Df(20, 20), -1);
            bill.SetMaterialType(MaterialType.TransparentAddColor);
            bill.SetMaterialTexture(0, driver.GetTexture(
                "../../media/particle.bmp"));
            bill.SetMaterialFlag(MaterialFlag.Lighting, false);
            bill.SetMaterialFlag(MaterialFlag.ZBuffer, false);

            Material material = new Material();
            material.Texture1 = driver.GetTexture(
                "../../media/faerie2.bmp");
            material.Lighting = true;

            AnimatedMeshSceneNode node = null;
            AnimatedMesh faerie = smgr.GetMesh(
                "../../media/faerie.md2");
            if (faerie != null)
            {
                node = smgr.AddAnimatedMeshSceneNode(faerie);
                node.Position = new Vector3D(-70, 0, -90);
                node.SetMD2Animation(MD2Animation.Run);
                node.SetMaterialTexture(0, material.Texture1);

                node = smgr.AddAnimatedMeshSceneNode(faerie);
                node.Position = new Vector3D(-70, 0, -30);
                node.SetMD2Animation(MD2Animation.Salute);
                node.SetMaterialTexture(0, material.Texture1);

                node = smgr.AddAnimatedMeshSceneNode(faerie);
                node.Position = new Vector3D(-70, 0, -60);
                node.SetMD2Animation(MD2Animation.Jump);
                node.SetMaterialTexture(0, material.Texture1);
            }

            //material.Texture1 = null;
            material.Lighting = false;

            //Add a light
            smgr.AddLightSceneNode(smgr.RootSceneNode, new Vector3D(-60, 100, 400),
                new Colorf(1.0f, 1.0f, 1.0f, 1.0f), 600, 0);

            /*For not making it too complicated, I'm doing picking inside the drawing
              loop. We take two pointers for storing the current and the last selected
              scene node and start the loop.*/
            SceneNode selectedSceneNode = null;
            SceneNode lastSelectedSceneNode = null;

            int lastFPS = -1;

            Material mat = new Material();
            mat.Lighting = false;

            while (device.Run())
            {
                if (device.WindowActive)
                {
                    device.VideoDriver.BeginScene(true, true, new Color(0, 200, 200, 200));
                    device.SceneManager.DrawAll();
                    /*After we've drawn the whole scene whit smgr->drawAll(), we'll do the
                      first picking: We want to know which triangle of the world we are
                      looking at. In addition, we want the exact point of the quake 3
                      level we are looking at. For this, we create a 3d line starting at
                      the position of the camera and going through the lookAt-target of it.
                      Then we ask the collision manager if this line collides with a
                      triangle of the world stored in the triangle selector. If yes, we draw
                      the 3d triangle and set the position of the billboard to the intersection
                      point.*/
                    Line3D line = new Line3D();
                    line.Start = camera.Position;
                    line.End = line.Start +
                        (camera.Target - line.Start).Normalize() * 1000;
                    Vector3D intersection;
                    Triangle3D tri;
                    if (smgr.CollisionManager.GetCollisionPoint(
                        line, selector, out intersection, out tri))
                    {
                        bill.Position = intersection;

                        driver.SetTransform(TransformationState.World, Matrix4.Identity);
                        driver.SetMaterial(mat);
                        driver.Draw3DTriangle(tri, Color.Red);
                    }
                    /*Another type of picking supported by the Irrlicht Engine is scene node
                      picking based on bouding boxes. Every scene node has got a bounding box,
                      and because of that, it's very fast for example to get the scene node
                      which the camera looks at. Again, we ask the collision manager for this,
                      and if we've got a scene node, we highlight it by disabling Lighting in
                      its material, if it is not the billboard or the quake 3 level.*/
                    selectedSceneNode = smgr.CollisionManager.
                        GetSceneNodeFromCamera(camera);                   

                    if (lastSelectedSceneNode != null)
                        lastSelectedSceneNode.SetMaterialFlag(
                            MaterialFlag.Lighting, true);

                    if (selectedSceneNode == q3node ||
                        selectedSceneNode == bill)
                        selectedSceneNode = null;

                    if (selectedSceneNode != null)
                        selectedSceneNode.SetMaterialFlag(
                            MaterialFlag.Lighting, false);
                    lastSelectedSceneNode = selectedSceneNode;

                    /*That's it, we just have to finish drawing.*/

                    driver.EndScene();

                    int fps = device.VideoDriver.FPS;
                    if (lastFPS != fps)
                    {
                        device.WindowCaption = "Collision detection example - Irrlicht Engine" +
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
    }
} 