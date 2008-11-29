using System;
using System.IO;
using System.Xml;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial10
{
    class Tutorial10
    {
        static IrrlichtDevice device;
        static string StartUpModelFile = string.Empty;
        static string MessageText = string.Empty;
        static string Caption = string.Empty;
        static bool UseHighLevelShaders = false;

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
            if (driverType == DriverType.Direct3D9 ||
                driverType == DriverType.OpenGL)
            {
                tOut.Write("Please press 'y' if you want to use high level shaders.\n");
                input = tIn.ReadLine();
                if (input.ToLower() == "y")
                    UseHighLevelShaders = true;
            }

            // Create device and exit if creation fails:
            device = new IrrlichtDevice(driverType, new Dimension2D(640, 480), 32, false, false, true, true);
            device.FileSystem.WorkingDirectory = "../../medias"; //We set Irrlicht's current directory to %application directory%/media

            if (device == null)
            {
                tOut.Write("Device creation failed.");
                return;
            }

            SceneManager smgr = device.SceneManager;
            VideoDriver driver = device.VideoDriver;
            GUIEnvironment gui = device.GUIEnvironment;

            /*Now for the more interesting parts. If we are using Direct3D, we want to
              load vertex and pixel shader programs, if we have OpenGL, we want to use ARB
              fragment and vertex programs. I wrote the corresponding programs down into the
              files d3d8.ps, d3d8.vs, d3d9.ps, d3d9.vs, opengl.ps and opengl.vs. We only
              need the right filenames now. This is done in the following switch. Note,
              that it is not necessary to write the shaders into text files, like in this
              example. You can even write the shaders directly as strings into the cpp source
              file, and use later addShaderMaterial() instead of addShaderMaterialFromFiles().*/
            string vsFileName = "";
            string psFileName = "";

            switch (driverType)
            {
                case DriverType.Direct3D8:
                    psFileName = "d3d8.psh";
                    vsFileName = "d3d8.vsh";
                    break;
                case DriverType.Direct3D9:
                    if (UseHighLevelShaders)
                    {
                        psFileName = "d3d9.hlsl";
                        vsFileName = psFileName; // both shaders are in the same file
                    }
                    else
                    {
                        psFileName = "d3d9.psh";
                        vsFileName = "d3d9.vsh";
                    }
                    break;
                case DriverType.OpenGL:
                    if (UseHighLevelShaders)
                    {
                        psFileName = "opengl.frag";
                        vsFileName = "opengl.vert";
                    }
                    else
                    {
                        psFileName = "opengl.psh";
                        vsFileName = "opengl.vsh";
                    }
                    break;
            }

            /*In addition, we check if the hardware and the selected renderer is capable
              of executing the shaders we want. If not, we simply set the filename string
              to 0. This is not necessary, but useful in this example: For example, if the
              hardware is able to execute vertex shaders but not pixel shaders, we create a
              new material which only uses the vertex shader, and no pixel shader. Otherwise,
              if we would tell the engine to create this material and the engine sees that
              the hardware wouldn't be able to fullfill the request completely, it would not
              create any new material at all. So in this example you would see at least the
              vertex shader in action, without the pixel shader.*/
            if (!driver.QueryFeature(VideoDriverFeature.PixelShader_1_1) &&
                !driver.QueryFeature(VideoDriverFeature.ARB_FragmentProgram_1))
            {
                device.Logger.Log("WARNING: Pixel shaders disabled \n"+
                   "because of missing driver/hardware support.");
                psFileName = null;
            }
            if (!driver.QueryFeature(VideoDriverFeature.VertexShader_1_1) &&
                !driver.QueryFeature(VideoDriverFeature.ARB_FragmentProgram_1))
            {
                device.Logger.Log("WARNING: Vertex shaders disabled \n"+
                   "because of missing driver/hardware support.");
                vsFileName = null;
            }

            /*Now lets create the new materials. As you maybe know from previous examples,
              a material type in the Irrlicht engine is set by simply changing the
              MaterialType value in the SMaterial struct. And this value is just a simple
              32 bit value, like video::EMT_SOLID. So we only need the engine to create a
              new value for us which we can set there. To do this, we get a pointer to the
              IGPUProgrammingServices and call addShaderMaterialFromFiles(), which returns
              such a new 32 bit value. That's all. The parameters to this method are the
              following: First, the names of the files containing the code of the vertex
              and the pixel shader. If you would use addShaderMaterial() instead, you would
              not need file names, then you could write the code of the shader directly as
              string. The following parameter is a pointer to the IShaderConstantSetCallBack
              class we wrote at the beginning of this tutorial. If you don't want to set
              constants, set this to 0. The last paramter tells the engine which material
              it should use as base material. To demonstrate this, we create two materials
              with a different base material, one with EMT_SOLID and one with
              EMT_TRANSPARENT_ADD_COLOR.*/
            // create materials

            GPUProgrammingServices gpu = driver.GPUProgrammingServices;

            int newMaterialType1 = 0;
            int newMaterialType2 = 0;

            if (gpu != null)
            {
                OnShaderConstantSetDelegate callBack = OnSetConstants;
                // create the shaders depending on if the user wanted high level
                // or low level shaders:
                if (UseHighLevelShaders)
                {
                    // create material from high level shaders (hlsl or glsl)
                    newMaterialType1 = gpu.AddHighLevelShaderMaterialFromFiles(
                        vsFileName, "vertexMain", VertexShaderType._1_1,
                        psFileName, "pixelMain", PixelShaderType._1_1,
                        callBack, MaterialType.Solid,0);
                    newMaterialType2 = gpu.AddHighLevelShaderMaterialFromFiles(
                        vsFileName, "vertexMain", VertexShaderType._1_1,
                        psFileName, "pixelMain", PixelShaderType._1_1,
                        callBack, MaterialType.TransparentAddColor,0);
                }
                else
                {
                    newMaterialType1 = gpu.AddShaderMaterialFromFiles(vsFileName,
                        psFileName, callBack, MaterialType.Solid,0);
                    newMaterialType2 = gpu.AddShaderMaterialFromFiles(vsFileName,
                        psFileName, callBack, MaterialType.TransparentAddColor,0);
                }
            }

            /*Now its time for testing out the materials. We create a test cube and set the
              material we created. In addition, we add a text scene node to the cube and a
              rotatation animator, to make it look more interesting and important.*/

            // create test scene node 1, with the new created material type 1
            SceneNode node = smgr.AddCubeSceneNode(50, null, 0);
            node.Position = new Vector3D(0, 0, 0);
            node.SetMaterialTexture(0, driver.GetTexture("wall.bmp"));
            node.SetMaterialType((MaterialType)newMaterialType1);

            smgr.AddTextSceneNode(gui.BuiltInFont, "PS & VS & EMT_SOLID",
                new Color(0, 255, 255, 255), node);

            Animator anim = smgr.CreateRotationAnimator(
                new Vector3D(0, 0.3f, 0));
            node.AddAnimator(anim);

            //Same for the second cube, but with the second material we created.
            node = smgr.AddCubeSceneNode(50, null, 0);
            node.Position = new Vector3D(0, -10, 50);
            node.SetMaterialTexture(0, driver.GetTexture("wall.bmp"));
            node.SetMaterialType((MaterialType)newMaterialType2);


            smgr.AddTextSceneNode(gui.BuiltInFont, "PS & VS & EMT_TRANSPARENT",
                new Color(0, 255, 255, 255),node);

            anim = smgr.CreateRotationAnimator(
                new Vector3D(0, 0.3f, 0));
            node.AddAnimator(anim);

            // Then we add a third cube without a shader on it, to be able to compare the cubes.
            node = smgr.AddCubeSceneNode(50, null, 0);
            node.Position = new Vector3D(0, 50, 25);
            node.SetMaterialTexture(0, driver.GetTexture("wall.bmp"));
            node.SetMaterialFlag(MaterialFlag.Lighting, false);
            //This seems to give problems when you choose high level shaders
            smgr.AddTextSceneNode(gui.BuiltInFont, "NO SHADER",
                new Color(0, 255, 255, 255), node);

            //And last, we add a skybox and a user controlled camera to the scene. For the
            //skybox textures, we disable mipmap generation, because we don't need mipmaps on it.

            // add a nice skybox
            driver.SetTextureFlag(TextureCreationFlag.CreateMipMaps, false);
            smgr.AddSkyBoxSceneNode(null,new Texture[] {
                driver.GetTexture("irrlicht2_up.jpg"),
                driver.GetTexture("irrlicht2_dn.jpg"),
                driver.GetTexture("irrlicht2_lf.jpg"),
                driver.GetTexture("irrlicht2_rt.jpg"),
                driver.GetTexture("irrlicht2_ft.jpg"),
                driver.GetTexture("irrlicht2_bk.jpg")},
                0);
            driver.SetTextureFlag(TextureCreationFlag.CreateMipMaps, true);

            // add a camera and disable the mouse cursor
            CameraSceneNode cam = smgr.AddCameraSceneNodeFPS(null, 100, 100, false);
            cam.Position = new Vector3D(-100, 50, 100);
            cam.Target = new Vector3D();
            device.CursorControl.Visible = false;

            /*Finally we simply have to draw everything, that's all.*/
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
                        device.WindowCaption = "Irrlicht Engine - Vertex and pixel shader example " +
                            "FPS:" + fps.ToString();
                        lastFPS = fps;
                    }
                }
                System.Threading.Thread.Sleep(0);
            }
            /*
            In the end, delete the Irrlicht device.
            */
            // Instead of device->drop, we'll use:
            device.Dispose();
        }

        static float[] colorToArray(Colorf p_m)
        {
            float[] t_a = new float[4];
            t_a[0] = p_m.R;
            t_a[1] = p_m.G;
            t_a[2] = p_m.B;
            t_a[3] = p_m.A;
            return t_a;
        }

        static float[] vectorToArray(Vector3D p_m)
        {
            float[] t_a = new float[4];
            t_a[0] = p_m.X;
            t_a[1] = p_m.Y;
            t_a[2] = p_m.Z;
            t_a[3] = 0;
            return t_a;
        }

        public static void OnSetConstants(MaterialRendererServices services, int userData)
        {
            VideoDriver driver = services.VideoDriver;

            // set inverted world matrix
            // if we are using highlevel shaders (the user can select this when
            // starting the program), we must set the constants by name.
            Matrix4 invWorld = driver.GetTransform(TransformationState.World);
            invWorld.MakeInverse();

            if (UseHighLevelShaders)
                services.SetVertexShaderConstant("mInvWorld",invWorld.ToShader(), 16);
            else
                services.SetVertexShaderConstant(invWorld.ToShader(), 0, 4);

            // set clip matrix
            Matrix4 worldViewProj;
            worldViewProj = driver.GetTransform(TransformationState.Projection);
            worldViewProj *= driver.GetTransform(TransformationState.View);
            worldViewProj *= driver.GetTransform(TransformationState.World);

            if (UseHighLevelShaders)
                services.SetVertexShaderConstant("mWorldViewProj", worldViewProj.ToShader(), 16);
            else
                services.SetVertexShaderConstant(worldViewProj.ToShader(), 4, 4);

            // set camera position
            Vector3D pos = device.SceneManager.ActiveCamera.Position;

            if (UseHighLevelShaders)
                services.SetVertexShaderConstant("mLightPos", vectorToArray(pos), 3);
            else
                services.SetVertexShaderConstant(vectorToArray(pos), 8, 1);

            // set light color
            Colorf col = new Colorf(0.0f, 1.0f, 1.0f, 0.0f);

            if (UseHighLevelShaders)
                services.SetVertexShaderConstant("mLightColor", colorToArray(col), 4);
            else
                services.SetVertexShaderConstant(colorToArray(col), 9, 1);

            // set transposed world matrix
            Matrix4 world = driver.GetTransform(TransformationState.World);
            world = world.GetTransposed();

            if (UseHighLevelShaders)
                services.SetVertexShaderConstant("mTransWorld", world.ToShader(), 16);
            else
                services.SetVertexShaderConstant(world.ToShader(), 10, 4);
        }
    }
} 