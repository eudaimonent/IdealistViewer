using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Tutorial14
{
    public partial class Form1 : Form
    {
        IrrlichtDevice device;
        private Thread irrThread;
        static Animator anim;
        public static void Main(string[] args)
        {

        }
        public Form1()
        {
            //InitializeComponent();
            // start an irrlicht thread
            //System.Threading.ThreadStart irrThreadStart = new System.Threading.ThreadStart(startIrr);
            //irrThread = new System.Threading.Thread(irrThreadStart);
            //irrThread.Start();

        }

        void startIrr()
        {
            //device = new IrrlichtDevice(DriverType.Direct3D9,
            //    new Dimension2D(640, 480), 32, false, true, true, true, this.myPanel.Handle);
            device.FileSystem.WorkingDirectory = "../../medias"; //We set Irrlicht's current directory to %application directory%/media
            device.OnEvent += new OnEventDelegate(device_OnEvent); //We had a simple delegate that will handle every event

            // setup a simple 3d scene
            SceneManager smgr = device.SceneManager;
            VideoDriver driver = device.VideoDriver;

            CameraSceneNode cam = smgr.AddCameraSceneNode(null);
            cam.Target = new Vector3D(0, 0, 0);

            anim =
               smgr.CreateFlyCircleAnimator(new Vector3D(0, 10, 0), 30.0f, 0.001f);
            cam.AddAnimator(anim);
            anim.Dispose();

            SceneNode cube = smgr.AddCubeSceneNode(25, null, -1);
            cube.SetMaterialFlag(MaterialFlag.Lighting, false);

            cube.SetMaterialTexture(0, driver.GetTexture("rockwall.bmp"));

            smgr.AddSkyBoxSceneNode(null, new Texture[] {
   driver.GetTexture("irrlicht2_up.jpg"),
   driver.GetTexture("irrlicht2_dn.jpg"),
   driver.GetTexture("irrlicht2_lf.jpg"),
   driver.GetTexture("irrlicht2_rt.jpg"),
   driver.GetTexture("irrlicht2_ft.jpg"),
   driver.GetTexture("irrlicht2_bk.jpg")}, -1);

            //// show and execute dialog
            //ShowWindow(hWnd , SW_SHOW);
            //UpdateWindow(hWnd);

            while (device.Run())
            {
                driver.BeginScene(true, true, new IrrlichtNETCP.Color());
                smgr.DrawAll();
                driver.EndScene();
            }

            // the alternative, own message dispatching loop without Device->run() would
            // look like this:

            /*MSG msg;
            while (true)
            {
                if (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
                {
                    TranslateMessage(&msg);
                    DispatchMessage(&msg);

                    if (msg.message == WM_QUIT)
                        break;
                }
      
                // advance virtual time
                device->getTimer()->tick();

                // draw engine picture
                driver->beginScene(true, true, 0);
                smgr->drawAll();
                driver->endScene();
            }*/

            //device.closeDevice();
            device.Dispose();

            return;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            startIrr();
        }

        public static bool device_OnEvent(Event p_event)
        {
            // check if user presses the key 'x'
            if (p_event.Type == EventType.KeyInputEvent &&
                !p_event.KeyPressedDown)
            {
                switch (p_event.KeyCode)
                {
                    case KeyCode.Key_X:
                        MessageBox.Show("X Pressed");
                        break;
                }
            }
            return false;
        }
    }
} 