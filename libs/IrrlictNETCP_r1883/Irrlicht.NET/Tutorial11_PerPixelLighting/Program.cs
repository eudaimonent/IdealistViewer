using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial11
{
    class Tutorial11
    {
        static IrrlichtDevice device;
        static string StartUpModelFile = string.Empty;
        static string MessageText = string.Empty;
        static string Caption = string.Empty;
        static VideoDriver driver=null;
        static GUIEnvironment env;
        static SceneManager smgr=null;
        static SceneNode room;
        static GUIListBox ListBox=null;
        static GUIStaticText ProblemText;

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

            // ask the user if we should use high level shaders for this example
            //if (driverType == DriverType.Direct3D9 ||
            //    driverType == DriverType.OpenGL)
            //{
            //    tOut.Write("Please press 'y' if you want to use high level shaders.\n");
            //    input = tIn.ReadLine();
            //    if (input.ToLower() == "y")
            //        UseHighLevelShaders = true;
            //}

            // Create device and exit if creation fails:
            device = new IrrlichtDevice(driverType, new Dimension2D(640, 480), 32, false, false, true, true);
            device.FileSystem.WorkingDirectory = "../../medias"; //We set Irrlicht's current directory to %application directory%/media
            //MyEventReceiver receiver(room, env, driver);
            device.OnEvent += new OnEventDelegate(device_OnEvent); //We had a simple delegate that will handle every event

            env = device.GUIEnvironment;
            driver = device.VideoDriver;
            smgr = device.SceneManager;
            /*************************************************/
            // not implemented
            // set a nicer font
            //GUISkin skin = env.Skin;
            //GUIFont font = env.GetFont("fonthaettenschweiler.bmp");
            //if (font != null)
            //    skin.Font = font;

            // add window and listbox
            GUIElement window = env.AddWindow(
                new Rect(new Position2D(490, 390), new Position2D(630, 470)), false, "Use 'E' + 'R' to change", null, 0);

            ListBox = env.AddListBox(
                new Rect(new Position2D(2, 22), new Position2D(135, 78)), window, 0, true);

            ListBox.AddItem("Diffuse");
            ListBox.AddItem("Bump mapping");
            ListBox.AddItem("Parallax mapping");
            ListBox.Selected = 1;

            // create problem text
            ProblemText = env.AddStaticText(
                "Your hardware or this renderer is not able to use the " +
                "needed shaders for this material. Using fall back materials.",
                new Rect(new Position2D(150, 20), new Position2D(470, 60)), true, true, null, 0, true);
            ProblemText.OverrideColor = new Color(100, 255, 255, 255);
            ProblemText.Visible = false;

            // set start material (prefer parallax mapping if available)
            //not implemented
            /*MaterialRender renderer =
                driver.GetMaterialRenderer(MaterialRenderer.EMT_PARALLAX_MAP_SOLID);
            if (renderer && renderer.getRenderCapability() == 0)
                ListBox.setSelected(2);*/

            // set the material which is selected in the listbox
            //setMaterial();
            /*************************************************/

            driver.SetTextureFlag(TextureCreationFlag.Always32Bit, true);

            // add Irrlicht logo
            env.AddImage(driver.GetTexture( "irrlichtlogoalpha.tga"), new Position2D(10, 10), true, null, 0, "");

            // add camera
            CameraSceneNode camera = smgr.AddCameraSceneNodeFPS(null, 100, 300, false);
            camera.Position = new Vector3D(-200, 200, -200);

            // disable mouse cursor
            device.CursorControl.Visible = false;

            /*Because we want the whole scene to look a little bit scarier, we add some fog to it. This is done by a call to 
IVideoDriver::setFog(). There you can set
various fog settings. In this example, we use pixel fog, because it will work well with the materials we'll use in this example. 
Please note that you will have to set the material flag EMF_FOG_ENABLE to 'true' in every scene node which should be affected by this fog.*/

            driver.SetFog(new Color(0, 138, 125, 81), true, 250, 1000, 0, true, false);

            /*To be able to display something interesting, we load a mesh from a .3ds file which is a room I modeled with anim8or. 
It is the same room as
from the specialFX example. Maybe you remember from that tutorial, I am no good modeler at all and so 
I totally messed up the texture mapping in this model, but we can simply repair it with the IMeshManipulator::makePlanarTextureMapping() method.
*/
            AnimatedMesh roomMesh = smgr.GetMesh(
                "room.3ds");
            room = null;

            if (roomMesh != null)
            {
                smgr.MeshManipulator.MakePlanarTextureMapping(roomMesh.GetMesh(0), 0.003f);

                /*
    Now for the first exciting thing: If we successfully loaded the mesh we need to apply textures to it. Because we want this room to be displayed
 with a very cool material, we have to do a little bit more than just set the textures. Instead of only loading a color map as usual, we also load 
a height map which is simply a grayscale texture. From this height map, we create a normal map which we will set as second texture of the room. If
 you already have a normal map, you could directly set it, but I simply didn´t find a nice normal map for this texture. The normal map texture is 
being generated by the makeNormalMapTexture method
    of the VideoDriver. The second parameter specifies the height of the heightmap. If you set it to a bigger value, the map will look more rocky.*/

                Texture  colorMap = driver.GetTexture("rockwall.bmp");
                Texture normalMap = driver.GetTexture("rockwall_height.bmp");

                driver.MakeNormalMapTexture(normalMap, 9.0f);

                /*But just setting color and normal map is not everything. The material we want to use needs some additional informations per
 vertex like tangents and binormals.
    Because we are too lazy to calculate that information now, we let Irrlicht do this for us. That's why we call
 IMeshManipulator::createMeshWithTangents(). It
    creates a mesh copy with tangents and binormals from any other mesh. After we've done that, we simply create a standard mesh scene node with
this
    mesh copy, set color and normal map and adjust some other material settings. Note that we set EMF_FOG_ENABLE to true to enable fog in the
room.*/

                Mesh tangentMesh = smgr.MeshManipulator.CreateMeshWithTangents(roomMesh.GetMesh(0));

                room = smgr.AddMeshSceneNode(tangentMesh, null, 0);
                room.SetMaterialTexture(0, colorMap);
                room.SetMaterialTexture(1, normalMap);
                room.GetMaterial(0).EmissiveColor.Set(0, 0, 0, 0);
                room.SetMaterialFlag(MaterialFlag.FogEnable, true);
                room.SetMaterialFlag(MaterialFlag.Lighting, false);
                room.SetMaterialType(MaterialType.ParallaxMapSolid);
                room.GetMaterial(0).MaterialTypeParam=0.02f;// adjust height for parallax effect
                // drop mesh because we created it with a create.. call.
                tangentMesh.Dispose();
            }

            /*After we've created a room shaded by per pixel lighting, we add a sphere into it
             * with the same material, but we'll make it transparent. In addition, because
             * the sphere looks somehow like a familiar planet, we make it rotate. The procedure
             * is similar as before. The difference is that we are loading the mesh from an .x
             * file which already contains a color map so we do not need to load it manually.
             * But the sphere is a little bit too small for our needs, so we scale it by the
             * factor 50.*/

            // add earth sphere
            AnimatedMesh earthMesh = smgr.GetMesh("earth.x");
            if (earthMesh != null)
            {
                // create mesh copy with tangent informations from original earth.x mesh
                Mesh tangentSphereMesh =
                    smgr.MeshManipulator.CreateMeshWithTangents(earthMesh.GetMesh(0));

                // set the alpha value of all vertices to 200
                smgr.MeshManipulator.SetVertexColorAlpha(tangentSphereMesh, 200);

                // scale the mesh by factor 50
                smgr.MeshManipulator.ScaleMesh(
                    tangentSphereMesh, new Vector3D(50, 50, 50));

                // create mesh scene node
                SceneNode sphere = smgr.AddMeshSceneNode(tangentSphereMesh, null, 0);
                sphere.Position = new Vector3D(-70, 130, 45);

                // load heightmap, create normal map from it and set it
                Texture earthNormalMap = driver.GetTexture("earthbump.bmp");
                driver.MakeNormalMapTexture(earthNormalMap, 20.0f);
                sphere.SetMaterialTexture(1, earthNormalMap);

                // adjust material settings
                sphere.SetMaterialFlag(MaterialFlag.FogEnable, true);
                sphere.SetMaterialFlag(MaterialFlag.Lighting, false);
                sphere.SetMaterialType(MaterialType.NormalMapTransparentVertexAlpha);

                // add rotation animator
                Animator anim =
                    smgr.CreateRotationAnimator(new Vector3D(0, 0.1f, 0));
                sphere.AddAnimator(anim);

                // drop mesh because we created it with a create.. call.
                tangentSphereMesh.Dispose();
            }

            /* Per pixel lighted materials only look cool when there are moving lights.
             * So we add some. And because moving lights alone are so boring, we add
             * billboards to them, and a whole particle system to one of them. We start
             * with the first light which is red and has only the billboard attached.*/
            // add light 1 (nearly red)
            LightSceneNode light1 =
                smgr.AddLightSceneNode(null, new Vector3D(0, 0, 0),
                new Colorf(0.5f, 1.0f, 0.5f, 0.0f), 200.0f, -1);

            // add fly circle animator to light 1
            Animator anim2 =
                smgr.CreateFlyCircleAnimator(new Vector3D(50, 300, 0), 190.0f, -0.003f);
            light1.AddAnimator(anim2);

            // attach billboard to the light
            SceneNode bill =
                smgr.AddBillboardSceneNode(light1, new Dimension2Df(60, 60), 0);

            bill.SetMaterialFlag(MaterialFlag.Lighting, false);
            bill.SetMaterialType(MaterialType.TransparentAddColor);
            bill.SetMaterialTexture(0, driver.GetTexture("particlered.bmp"));

            /*Now the same again, with the second light. The difference is that we add a particle
             * system to it too. And because the light moves, the particles of the particlesystem
             * will follow. If you want to know more about how particle systems are created in
             * Irrlicht, take a look at the specialFx example.
             * Maybe you will have noticed that we only add 2 lights, this has a simple reason: The
             * low end version of this material was written in ps1.1 and vs1.1, which doesn't
             * allow more lights. You could add a third light to the scene, but it won't be used
             * to shade the walls. But of course, this will change in future versions of Irrlicht
             * were higher versions of pixel/vertex shaders will be implemented too.*/
            // add light 2 (gray)
            SceneNode light2 =
                smgr.AddLightSceneNode(null, new Vector3D(0, 0, 0),
                new Colorf(1.0f, 0.2f, 0.2f, 0.0f), 200.0f, 0);

            // add fly circle animator to light 2
            anim2 = smgr.CreateFlyCircleAnimator(new Vector3D(0, 150, 0), 200.0f, 0.0005f);
            light2.AddAnimator(anim2);

            // attach billboard to light
            bill = smgr.AddBillboardSceneNode(light2, new Dimension2Df(120, 120), 0);
            bill.SetMaterialFlag(MaterialFlag.Lighting, false);
            bill.SetMaterialType(MaterialType.TransparentAddColor);
            bill.SetMaterialTexture(0, driver.GetTexture("particlewhite.bmp"));

            // add particle system
            ParticleSystemSceneNode ps =
                smgr.AddParticleSystemSceneNode(false, light2, 0);

            ps.ParticleSize = new Dimension2Df(30.0f, 40.0f);

            // create and set emitter
            ParticleEmitter em = ps.CreateBoxEmitter(
                new Box3D(-3, 0, -3, 3, 1, 3),
                new Vector3D(0.0f, 0.03f, 0.0f),
                80, 100,
                new Color(0, 255, 255, 255), new Color(0, 255, 255, 255),
                400, 1100, 0);
            ps.SetEmitter(em);

            // create and set affector
            ParticleAffector paf = ps.CreateFadeOutParticleAffector(new Color(), 1000);
            ps.AddAffector(paf);

            // adjust some material settings
            ps.SetMaterialFlag(MaterialFlag.Lighting, false);
            ps.SetMaterialTexture(0, driver.GetTexture("fireball.bmp"));
            ps.SetMaterialType(MaterialType.TransparentVertexAlpha);

           
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
            // Instead of device.drop, we'll use:
            device.Dispose();

        }

        public static bool device_OnEvent(Event p_event)
        {
            // check if user presses the key 'E' or 'R'
            if (p_event.Type == EventType.KeyInputEvent &&
                !p_event.KeyPressedDown && room != null && ListBox != null)
            {
                // change selected item in listbox

                int sel = ListBox.Selected;
                if (p_event.KeyCode == KeyCode.Key_R)
                    ++sel;
                else
                    if (p_event.KeyCode == KeyCode.Key_E)
                        --sel;
                    else
                        return false;

                if (sel > 2) sel = 0;
                if (sel < 0) sel = 2;
                ListBox.Selected = sel;

                // set the material which is selected in the listbox
                setMaterial();
            }

            return false;
        }

        // sets the material of the room mesh the the one set in the
        // list box.
        static void setMaterial()
        {
           MaterialType type = MaterialType.Solid;

            // change material setting
            switch(ListBox.Selected)
            {
                case 0: type = MaterialType.Solid;
                    break;
                case 1: type = MaterialType.NormalMapSolid;
                    break;
                case 2: type = MaterialType.ParallaxMapSolid;
                    break;
            }

             room.SetMaterialType(type);
            /*We need to add a warning if the materials will not be able to be displayed 100% correctly. This is no problem, they will be 
renderered using fall back materials, but at least the user should know that it would look better on better hardware. We simply check if the 
material renderer is able to draw at full quality on the current hardware. The IMaterialRenderer::getRenderCapability() returns 0 if this is the
case.*/
            //MaterialRendererServices renderer = driver.getMaterialRender(type);

            // display some problem text when problem
            //if (renderer==null || renderer.getRenderCapability() != 0)
            //    ProblemText.Visible=true;
            //else
                ProblemText.Visible=false;
        }
    }
}