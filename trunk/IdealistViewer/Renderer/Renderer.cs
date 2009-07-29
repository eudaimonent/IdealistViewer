using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using System.Xml;
using System.IO;
using IrrlichtNETCP.Extensions;

namespace IdealistViewer
{
    public class Renderer
    {
        public IrrlichtNETCP.Quaternion CoordinateConversion_XYZ_XZY = new IrrlichtNETCP.Quaternion();

        private Viewer m_viewer;

        /// <summary>
        /// Irrlicht Instance.  A handle to the Irrlicht device.
        /// </summary>
        public IrrlichtDevice Device;
        public VideoDriver Driver;

        public SceneManager SceneManager;
        public GUIEnvironment GuiEnvironment;

        public Renderer(Viewer viewer)
        {
            m_viewer = viewer;
        }

        public virtual void Startup()
        {

            //Create a New Irrlicht Device
            Device = new IrrlichtDevice(DriverType.OpenGL,
                                                     new Dimension2D(m_viewer.WindowWidth, m_viewer.WindowHeight),
                                                    32, false, true, true, true);
            //device.Timer.Stop();
            Device.Timer.Speed = 1;
            Device.WindowCaption = "IdealistViewer 0.001";

            // Sets directory to load assets from
            Device.FileSystem.WorkingDirectory = m_viewer.StartupDirectory + "/" + Util.MakePath("media", "materials", "textures", "");  //We set Irrlicht's current directory to %application directory%/media


            Driver = Device.VideoDriver;
            SceneManager = Device.SceneManager;

            GuiEnvironment = Device.GUIEnvironment;

            // Compose Coordinate space converter quaternion
            IrrlichtNETCP.Matrix4 m4 = new IrrlichtNETCP.Matrix4();
            m4.SetM(0, 0, 1);
            m4.SetM(1, 0, 0);
            m4.SetM(2, 0, 0);
            m4.SetM(3, 0, 0);
            m4.SetM(0, 1, 0);
            m4.SetM(1, 1, 0);
            m4.SetM(2, 1, 1);
            m4.SetM(3, 1, 0);
            m4.SetM(0, 2, 0);
            m4.SetM(1, 2, 1);
            m4.SetM(2, 2, 0);
            m4.SetM(3, 2, 0);
            m4.SetM(0, 3, 0);
            m4.SetM(1, 3, 0);
            m4.SetM(2, 3, 0);
            m4.SetM(3, 3, 1);


            CoordinateConversion_XYZ_XZY = new IrrlichtNETCP.Quaternion(m4);
            CoordinateConversion_XYZ_XZY.makeInverse();
           
        }
    }
}
