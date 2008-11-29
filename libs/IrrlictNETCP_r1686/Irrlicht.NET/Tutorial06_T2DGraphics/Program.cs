using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial06
{
    class Tutorial06
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
            // I just left these lines here for example purposes:
            //irrv.IVideoDriver driver = device.VideoDriver;
            //irrs.ISceneManager smgr = device.SceneManager;
            //ISceneManager smgr=device.SceneManager;
            VideoDriver driver = device.VideoDriver;
            //IGUIEnvironment env = device.GUIEnvironment;

            /*All 2d graphics in this example are put together into one texture,
              2ddemo.bmp. Because we want to draw colorkey based sprites, we need
              to load this texture and tell the engine, which part of it should be
              transparent based on a colorkey. In this example, we don't tell it
              the color directly, we just say "Hey Irrlicht Engine, you'll find the
              color I want at position (0,0) on the texture.". Instead, it would be
              also possible to call
              driver->makeColorKeyTexture(images, video::SColor(0,0,0,0))
              to make e.g. all black pixels transparent. Please note, that
              makeColorKeyTexture just creates an alpha channel based on the color.*/
            Texture images = driver.GetTexture("../../medias/2ddemo.bmp");
            driver.MakeColorKeyTexture(images, new Position2D(0, 0));

            /*
             To be able to draw some text with two different fonts,
             we load them. Ok, we load just one, as first font we just
             use the default font which is built into the engine.
             Also, we define two rectangles, which specify the position
             of the images of the red imps (little flying creatures) in
             the texture.
             */
            GUIFont font = device.GUIEnvironment.BuiltInFont;
            GUIFont font2 = device.GUIEnvironment.GetFont("../../media/fonthaettenschweiler.bmp");
            Rect imp1 = new Rect(new Position2D(349, 15), new Position2D(385, 78));
            Rect imp2 = new Rect(new Position2D(387, 15), new Position2D(423, 78));

            /*
             Everything is prepared, now we can draw everything in the draw loop,
             between the begin scene and end scene calls. In this example, we are just
             doing 2d graphics, but it would be no problem to mix them with 3d graphics.
             Just try it out, and draw some 3d vertices or set up a scene with the scene
             manager and draw it.
             */
            while (device.Run() && driver != null)
            {
                if (device.WindowActive)
                {
                    uint time = device.Timer.Time;
                    driver.BeginScene(true, true, new Color(0, 120, 102, 136));
                    /*
                     First, we draw 3 sprites, using the alpha channel we created with
                     makeColorKeyTexture. The last parameter specifiys that the drawing
                     method should use thiw alpha channel. The parameter before the last
                     one specifies a color, with which the sprite should be colored.
                     (255,255,255,255) is full white, so the sprite will look like the
                     original. The third sprite is drawed colored based on the time.*/
                    // draw fire & dragons background world
                    driver.Draw2DImage(images, new Position2D(50, 50),
                        new Rect(new Position2D(0, 0), new Position2D(342, 224)),
                        new Color(255, 255, 255, 255), true);

                    // draw flying imp
                    driver.Draw2DImage(images, new Position2D(164, 125),
                        (time / 500 % 2) == 0 ? imp1 : imp2,
                        new Color(255, 255, 255, 255), true);

                    // draw second flying imp with colorcylce
                    driver.Draw2DImage(images, new Position2D(270, 105),
                        (time / 500 % 2) == 0 ? imp1 : imp2,
                        new Color(255, ((int)(time) % 255), 255, 255), true);

                    // Drawing text is really simple. The code should be self explanatory.
                    if (font != null)
                    {
                        font.Draw("This is some text",
                            new Position2D(130, 10), new Color(255,255,255,255),
                            false,false,
                            new Rect (new Position2D(130, 10), new Position2D(300, 50)));
                    }

                    if (font2 != null)
                    {
                        font2.Draw("This is some text",
                            new Position2D(130, 20), new Color(255, (int)time % 255, (int)time % 255, 255),
                            false, false,
                            new Rect(new Position2D(130, 20), new Position2D(300, 60)));
                    }

                    /*At last, we draw the Irrlicht Engine logo (without using
                      a color or an alpha channel) and a transparent 2d Rectangle
                      at the position of the mouse cursor.*/
                    // draw logo
                    driver.Draw2DImage(images, new Position2D(10, 10),
                        new Rect(new Position2D(354, 87), new Position2D(442, 118)), new Color(255, 255, 255, 255), false);

                    // draw transparent rect under cursor
                    Position2D m = device.CursorControl.Position;
                    driver.Draw2DRectangle(
                        new Rect(new Position2D(m.X - 20, m.Y - 20), new Position2D(m.X + 20, m.Y + 20)),
                        new Color(100, 255, 255, 255));

                    driver.EndScene();

                }
            }
            /*
            In the end, delete the Irrlicht device.*/
            device.Dispose();
        }
    }
} 