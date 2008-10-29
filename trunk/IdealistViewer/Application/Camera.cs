using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class Camera
    {
        public CameraSceneNode SNCamera;
        public SceneNode SNtarget;
        private float CAMERASPEED = 0.05f;
        private float CAMERAZOOMSPEED = 5f;
        private static float STARTANGLE = 0f;
        private static float STARTDISTANCE = 35f;
        public float CAMDISTANCE = 35f;
        private float MAXZOOM = 1f;
        private float MINZOOM = 250f;
        private float TWOPI = (float)(Math.PI * 2);
        public float loMouseOffsetPHI = 0;
        public float loMouseOffsetTHETA = 0;
        public float CamRotationAnglePHI = 0f;
        public float CamRotationAngleTHETA = 0f;
        private SceneManager smgr = null;
        private static Vector3 m_lastTargetPos = Vector3.Zero;


        public Camera(SceneManager psmgr)
        {
            //
            SNtarget = null;
            smgr = psmgr;
            SNCamera = smgr.AddCameraSceneNode(null);
            SNCamera.Position = new Vector3D(0f, 235f, -44.70f);
            SNCamera.Target = new Vector3D(-273, -255.3f, 407.3f);

            CamRotationAnglePHI = STARTANGLE;
            CamRotationAngleTHETA = STARTANGLE;
            CAMDISTANCE = STARTDISTANCE;

            UpdateCameraPosition();
        
        }
        public Vector3 Position
        {
            get { return new Vector3(SNCamera.Position.X,SNCamera.Position.Y,SNCamera.Position.Z); }
        }

        public void UpdateCameraPosition()
        {
            Vector3D newpos = new Vector3D();
            Vector3D oldTarget = SNCamera.Target;
            newpos.X = oldTarget.X + (CAMDISTANCE * (float)Math.Cos(CamRotationAngleTHETA + loMouseOffsetTHETA) * (float)Math.Sin(CamRotationAnglePHI + loMouseOffsetPHI));
            newpos.Y = oldTarget.Y + CAMDISTANCE * (float)Math.Cos(CamRotationAnglePHI + loMouseOffsetPHI);
            newpos.Z = oldTarget.Z + CAMDISTANCE * (float)Math.Sin(CamRotationAngleTHETA + loMouseOffsetTHETA) * (float)Math.Sin(CamRotationAnglePHI + loMouseOffsetPHI);


            SNCamera.Position = newpos;
            SNCamera.Target = oldTarget;
            SNCamera.UpdateAbsolutePosition();
            //m_log.WarnFormat("[CameraPos]: <{0},{1},{2}>", camr.Position.X, camr.Position.Y, camr.Position.Z);

        }
        public void DoKeyAction(KeyCode key)
        {

            switch (key)
            {
                case KeyCode.Up:
                    CAMDISTANCE -= CAMERAZOOMSPEED;
                    if (CAMDISTANCE < MAXZOOM) CAMDISTANCE = MAXZOOM;
                    if (CAMDISTANCE > MINZOOM) CAMDISTANCE = MINZOOM;

                    UpdateCameraPosition();
                    break;

                case KeyCode.Down:
                    CAMDISTANCE += CAMERAZOOMSPEED;
                    if (CAMDISTANCE < MAXZOOM) CAMDISTANCE = MAXZOOM;
                    if (CAMDISTANCE > MINZOOM) CAMDISTANCE = MINZOOM;

                    UpdateCameraPosition();
                    break;

                case KeyCode.Left:
                    CamRotationAngleTHETA -= CAMERASPEED;
                    while (CamRotationAngleTHETA < TWOPI)
                        CamRotationAngleTHETA += TWOPI;

                    UpdateCameraPosition();
                    break;

                case KeyCode.Right:
                    CamRotationAngleTHETA += CAMERASPEED;
                    while (CamRotationAngleTHETA > TWOPI)
                        CamRotationAngleTHETA -= TWOPI;

                    UpdateCameraPosition();
                    break;
                case KeyCode.Next:

                    CamRotationAnglePHI -= CAMERASPEED;
                    while (CamRotationAnglePHI < TWOPI)
                        CamRotationAnglePHI += TWOPI;

                    UpdateCameraPosition();
                    break;
                case KeyCode.Prior:
                    //vOrbit.Y += 2f;
                    CamRotationAnglePHI += CAMERASPEED;
                    while (CamRotationAnglePHI > TWOPI)
                        CamRotationAnglePHI -= TWOPI;

                    UpdateCameraPosition();

                    break;

            }

        }
        public void ResetMouseOffsets()
        {
            loMouseOffsetPHI = 0;
            loMouseOffsetTHETA = 0;
        }

        public void ApplyMouseOffsets()
        {
            if (loMouseOffsetPHI != 0 || loMouseOffsetTHETA != 0)
            {

                CamRotationAnglePHI = CamRotationAnglePHI + loMouseOffsetPHI;
                CamRotationAngleTHETA = CamRotationAngleTHETA + loMouseOffsetTHETA;
                loMouseOffsetPHI = 0;
                loMouseOffsetTHETA = 0;
                UpdateCameraPosition();
            }
        }

        public void MouseWheelAction(float delta)
        {
            CAMDISTANCE += CAMERAZOOMSPEED * -delta;
            if (CAMDISTANCE < MAXZOOM) CAMDISTANCE = MAXZOOM;
            if (CAMDISTANCE > MINZOOM) CAMDISTANCE = MINZOOM;

            UpdateCameraPosition();
        }

        public void SetTarget(Vector3D ptarget)
        {
            SNCamera.Target = ptarget;
            Vector3 target = new Vector3(ptarget.X, ptarget.Y, ptarget.Z);
            Vector3 camerapos = new Vector3(SNCamera.Position.X, SNCamera.Position.Y, SNCamera.Position.Z);
            CAMDISTANCE = Vector3.Distance(camerapos, target);
            UpdateCameraPosition();
        }

        public void SetTarget(SceneNode pTarget)
        {
            SetTarget(pTarget.Position);
            SNtarget = pTarget;
        }

        public void CheckTarget()
        {
            if (SNtarget != null)
            {
                if (SNtarget.Raw != IntPtr.Zero)
                {
                    try
                    {
                        if (SNtarget.Position != SNCamera.Target)
                        {
                            SNCamera.Target = SNtarget.Position;
                            UpdateCameraPosition();
                        }
                    }
                    catch (AccessViolationException)
                    {
                        System.Console.WriteLine("Picked camera target is dead");
                        SNtarget = null;
                    }
                }
            }
        }

        public void SetDeltaFromMouse(float deltaX, float deltaY)
        {
            CamRotationAnglePHI = CamRotationAnglePHI + ((deltaY * CAMERASPEED) * 0.2f);
            CamRotationAngleTHETA = CamRotationAngleTHETA + ((-deltaX * CAMERASPEED) * 0.2f);

            UpdateCameraPosition();
        }

        public Vector3D[] ProjectRayPoints(Position2D mpos, float WindowWidth_DIV2, float WindowHeight_DIV2, float aspect)
        {

            Vector3 pos = Vector3.Zero;
            pos.X = (float)(Math.Tan(SNCamera.FOV * 0.5f) * (mpos.X / WindowWidth_DIV2 - 1.0f));
            pos.Y = (float)(Math.Tan(SNCamera.FOV * 0.5f) * (1.0f - mpos.Y / WindowHeight_DIV2) / aspect);

            Vector3D p1 = new Vector3D(pos.X * SNCamera.NearValue, pos.Y * SNCamera.NearValue, SNCamera.NearValue);
            Vector3D p2 = new Vector3D(pos.X * SNCamera.FarValue, pos.Y * SNCamera.FarValue, SNCamera.FarValue);

            // Inverse the view matrix
            IrrlichtNETCP.Matrix4 viewm = SNCamera.ViewMatrix;
            viewm.MakeInverse();

            p1 = viewm.TransformVect(ref p1);
            p2 = viewm.TransformVect(ref p2);
            //m_log.DebugFormat("Ray: <{0},{1},{2}>, <{3},{4},{5}>", p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z);
            Vector3D[] returnvectors = new Vector3D[2];
            returnvectors[0] = p1;
            returnvectors[1] = p2;
            return returnvectors;
        }


    }
}
