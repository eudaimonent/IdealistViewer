using System;
using System.IO;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial08
{
    class Tutorial08
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
            device = new IrrlichtDevice(driverType, new Dimension2D(640, 480),
                16, false, true, true, true);
            device.FileSystem.WorkingDirectory = "../../medias"; //We set Irrlicht's current directory to %application directory%/media
            if (device == null)
            {
                tOut.Write("Device creation failed.");
                return;
            }

            SceneManager smgr = device.SceneManager;
            VideoDriver driver = device.VideoDriver;

            /* For our environment, we load a .3ds file. It is a small room I modelled with
               Anim8or and exported it into the 3ds format because the Irrlicht Engine did
               not support the .an8 format when I wrote this tutorial. I am a very bad 3d
               graphic artist, and so the texture mapping is not very nice in this model.
               Luckily I am a better programmer than artist, and so the Irrlicht Engine is
               able to create a cool texture mapping for me: Just use the mesh manipulator
               and create a planar texture mapping for the mesh. If you want to see the
               mapping I made with Anim8or, uncomment this line. I also did not figure out
               how to set the material right in Anim8or, it has an emissive light color
               which I don't really like. I'll switch it off too with this code.*/
            AnimatedMesh mesh = smgr.GetMesh(
                "room.3ds");

            smgr.MeshManipulator.MakePlanarTextureMapping(
                mesh.GetMesh(0), 0.008f);

            SceneNode node = smgr.AddAnimatedMeshSceneNode(mesh);
            node.SetMaterialTexture(
                0, driver.GetTexture("wall.jpg"));
            node.GetMaterial(0).EmissiveColor.Set(0, 0, 0, 0);
            node.SetMaterialFlag(MaterialFlag.Lighting,false);

             /*Now, for the first special effect: Animated water. It works like this: The
              WaterSurfaceSceneNode takes a mesh as input and makes it wave like a water
              surface. And if we let this scene node use a nice material like the
              MT_REFLECTION_2_LAYER, it looks really cool. We are doing this with the
              next few lines of code. As input mesh, we create a hill plane mesh, without
              hills. But any other mesh could be used for this, you could even use the
              room.3ds (which would look really strange) if you wanted to.*/
            mesh = smgr.AddHillPlaneMesh("myHill",
                new Dimension2Df(20, 20),
                new Dimension2D(40, 40), 0,
                new Dimension2Df(0, 0),
                new Dimension2Df(10, 10));

            node = smgr.AddWaterSurfaceSceneNode(mesh.GetMesh(0),
                3.0f, 300.0f, 30.0f, smgr.RootSceneNode, -1);
            node.Position = new Vector3D(0, 7, 0);

            node.SetMaterialTexture(0, driver.GetTexture("Stones.jpg"));
            node.SetMaterialTexture(1, driver.GetTexture("water.jpg"));

            node.SetMaterialType(MaterialType.Reflection2Layer);

            /*The second special effect is very basic, I bet you saw it already in some
              Irrlicht Engine demos: A transparent billboard combined with a dynamic light.
              We simply create a light scene node, let it fly around, an to make it look
              more cool, we attach a billboard scene node to it.*/
            // create light

            node = smgr.AddLightSceneNode(smgr.RootSceneNode, new Vector3D(0, 0, 0),
                new Colorf(1.0f, 0.6f, 0.7f, 1.0f), 600.0f, 0);
            Animator anim = smgr.CreateFlyCircleAnimator(new Vector3D(0, 150, 0), 250.0f, 0.0005f);
            node.AddAnimator(anim);
            anim.Dispose();

            // attach billboard to light
            node = smgr.AddBillboardSceneNode(node, new Dimension2Df(50, 50), -1);
            node.SetMaterialFlag(MaterialFlag.Lighting, false);
            node.SetMaterialType(MaterialType.TransparentAddColor);
            node.SetMaterialTexture(0,
                driver.GetTexture("particlewhite.bmp"));

            /* The next special effect is a lot more interesting: A particle system. The
               particle system in the Irrlicht Engine is quit modular and extensible and
               yet easy to use. There is a particle system scene node into which you can
               put particle emitters, which make particles come out of nothing. These
               emitters are quite flexible and usually have lots of parameters like
               direction, amount and color of the particles they should create.
               There are different emitters, for example a point emitter which lets
               particles pop out at a fixed point. If the particle emitters available
               in the engine are not enough for you, you can easily create your own ones,
               you'll simply have to create a class derived from the IParticleEmitter
               interface and attach it to the particle system using setEmitter().
               In this example we create a box particle emitter, which creates particles
               randomly inside a box. The parameters define the box, direction of the
               articles, minimal and maximal new particles per second, color and minimal
               and maximal livetime of the particles. Because only with emitters particle
               system would be a little bit boring, there are particle affectors, which
               modify particles during they fly around. They can be added to the particle
               system, simulating additional effects like gravity or wind. The particle
               affector we use in this example is an affector, which modifies the color
               of the particles: It lets them fade out. Like the particle emitters,
               additional particle affectors can also be implemented by you, simply derive
               a class from IParticleAffector and add it with addAffector(). After we set
               a nice material to the particle system, we have a cool looking camp fire.
               By adjusting material, texture, particle emitter and affector parameters,
               it is also easily possible to create smoke, rain, explosions, snow, and
               so on.*/
            ParticleSystemSceneNode ps = smgr.AddParticleSystemSceneNode(
                false, smgr.RootSceneNode, -1);//, new Vector3D(-70, 60, 40), new Vector3D(), new Vector3D(2, 2, 2));
            ps.Position = new Vector3D(-70, 60, 40);
            ps.Scale = new Vector3D(2, 2, 2);

            ps.ParticleSize = new Dimension2Df(20, 10);

            ParticleEmitter em = ps.CreateBoxEmitter(
                new Box3D(-7, 0, -7, 7, 1, 7), new Vector3D(0.0f, 0.03f, 0.0f),
                80, 100,
                new Color(0, 255, 255, 255), new Color(0, 255, 255, 255),
                800, 2000, -1);

            ps.SetEmitter(em);

            ParticleAffector paf =
                ps.CreateFadeOutParticleAffector(new Color(), 1500);

            ps.AddAffector(paf);
            paf.Dispose();

            ps.SetMaterialFlag(MaterialFlag.Lighting, false);
            ps.SetMaterialTexture(0,
                driver.GetTexture("particle.bmp"));
            ps.SetMaterialType(MaterialType.TransparentVertexAlpha);

            /*As our last special effect, we want a dynamic shadow be casted from an animated
              character. For this we load a quake 2 .md2 model and place it into our world.
              For creating the shadow, we simply need to call addShadowVolumeSceneNode(). The
              color of shadows is only adjustable globally for all shadows, by calling
              ISceneManager::setShadowColor(). Voila, here is our dynamic shadow. Because
              the character is a little bit too small for this scene, we make it bigger
              using setScale(). And because the character is lighted by a dynamic light,
              we need to normalize the normals to make the lighting on it correct. This
              is always necessary if the scale of a dynamic lighted model is not (1,1,1).
              Otherwise it would get too dark or too bright because the normals will be
              scaled too.*/
            mesh = smgr.GetMesh("dwarf.x");
            AnimatedMeshSceneNode anode = smgr.AddAnimatedMeshSceneNode(mesh);
            anode.Position = new Vector3D(-50, 20, -60);
            anode.AnimationSpeed=15;
           
            // add shadow
            anode.AddShadowVolumeSceneNode(-1,true,1f);
            smgr.ShadowColor = new Color(220, 0, 0, 0);

            // make the model a little bit bigger and normalize its normals
            // because of this for correct lighting
            anode.Scale = new Vector3D(2, 2, 2);
            anode.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);

            /*Finally we simply have to draw everything, that's all.*/
            CameraSceneNode camera = smgr.AddCameraSceneNodeFPS(null,100,100,false);
            camera.Position = new Vector3D(66, 126, -215);
            camera.Rotation = new Vector3D(3,-58,0);
            int lastFPS = -1;

            while (device.Run())
            {
                if (device.WindowActive)
                {
                    device.VideoDriver.BeginScene(true, true, Color.Black);
                    device.SceneManager.DrawAll();
                    device.VideoDriver.EndScene();

                    int fps = device.VideoDriver.FPS;
                    if (lastFPS != fps)
                    {
                        device.WindowCaption = "Irrlicht Engine - SpecialFX example " +
                           " FPS:" + fps.ToString();
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