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
        public Vector3D FollowCamTargetOffset = new Vector3D(0, 1f, 0);

        private float zDirection, direction = 0;
        
        public ECameraMode CameraMode = ECameraMode.Build;

        private SceneManager smgr = null;
        private static Vector3 m_lastTargetPos = Vector3.Zero;
        private Vector3[] LookAtCam = new Vector3[3];

        /// <summary>
        /// User's Camera
        /// </summary>
        /// <param name="psmgr">Scene Manager</param>
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

            LookAtCam[0] = Vector3.Zero;
            LookAtCam[1] = Vector3.Zero;
            LookAtCam[2] = Vector3.Zero;

            UpdateCameraPosition();
        
        }

        //LibOMV camera position
        public Vector3 Position
        {
            get { return new Vector3(SNCamera.Position.X,SNCamera.Position.Y,SNCamera.Position.Z); }
        }
        /// <summary>
        /// Update camera position based on it's current PHI, Theta, and mouse offset.
        /// </summary>
        public void UpdateCameraPosition()
        {
            Vector3D newpos = new Vector3D();
            Vector3D oldTarget = SNCamera.Target;
            switch (CameraMode)
            {
                case ECameraMode.Build:

                    newpos.X = oldTarget.X + (CAMDISTANCE * (float)Math.Cos(CamRotationAngleTHETA + loMouseOffsetTHETA) * (float)Math.Sin(CamRotationAnglePHI + loMouseOffsetPHI));
                    newpos.Y = oldTarget.Y + CAMDISTANCE * (float)Math.Cos(CamRotationAnglePHI + loMouseOffsetPHI);
                    newpos.Z = oldTarget.Z + CAMDISTANCE * (float)Math.Sin(CamRotationAngleTHETA + loMouseOffsetTHETA) * (float)Math.Sin(CamRotationAnglePHI + loMouseOffsetPHI);


                    SNCamera.Position = newpos;
                    SNCamera.Target = oldTarget;
                    SNCamera.UpdateAbsolutePosition();
                    break;
                case ECameraMode.Third:

                    if (SNtarget != null)
                    {
                        //SNtarget.UpdateAbsolutePosition();
                        Vector3D currentTargetPos = SNtarget.Position;
                        Vector3D currentTargetRot = SNtarget.Rotation;
                        Vector3D Delta1 = new Vector3D(-CAMDISTANCE, 0, 0);
                        IrrlichtNETCP.Matrix4 transform1 = new IrrlichtNETCP.Matrix4();

                        transform1.RotationDegrees = new Vector3D(SNtarget.Rotation.X, SNtarget.Rotation.Y, SNtarget.Rotation.Z);
                        
                        transform1.TransformVect(ref Delta1);

                        //transform1.RotationDegrees = new Vector3D(0, SNCamera.Rotation.Y - currentTargetRot.Y - 0, 0);
                        //transform1.TransformVect(ref Delta1);
                        newpos = Delta1 + new Vector3D(0, 0.5f * CAMDISTANCE, 0);
                        SNCamera.Position = currentTargetPos + newpos + (FollowCamTargetOffset * 0.5f);
                        SNCamera.Target = SNtarget.Position + FollowCamTargetOffset;


                    }

                    break;


            }
            //m_log.WarnFormat("[CameraPos]: <{0},{1},{2}>", camr.Position.X, camr.Position.Y, camr.Position.Z);

        }

        /// <summary>
        /// Key handler for camera actions
        /// </summary>
        /// <param name="key"></param>
        public void DoKeyAction(KeyCode key)
        {

            switch (key)
            {
                case KeyCode.Up:
                    CAMDISTANCE -= (CAMERAZOOMSPEED - ((MAXZOOM - CAMDISTANCE) / CAMDISTANCE));
                    if (CAMDISTANCE < MAXZOOM) CAMDISTANCE = MAXZOOM;
                    if (CAMDISTANCE > MINZOOM) CAMDISTANCE = MINZOOM;

                    UpdateCameraPosition();
                    break;

                case KeyCode.Down:
                    CAMDISTANCE += CAMERAZOOMSPEED - ((MAXZOOM - CAMDISTANCE) / CAMDISTANCE);
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

                    // Page Down
                case KeyCode.Next:

                    CamRotationAnglePHI -= CAMERASPEED;
                    while (CamRotationAnglePHI < TWOPI)
                        CamRotationAnglePHI += TWOPI;

                    UpdateCameraPosition();
                    break;

                    // Page Up
                case KeyCode.Prior:
                    //vOrbit.Y += 2f;
                    CamRotationAnglePHI += CAMERASPEED;
                    while (CamRotationAnglePHI > TWOPI)
                        CamRotationAnglePHI -= TWOPI;

                    UpdateCameraPosition();

                    break;

            }

        }

        /// <summary>
        /// Mouse Offset Reset.  This usually gets called after applying the 
        /// values to the actual camera phi and theta first
        /// </summary>
        public void ResetMouseOffsets()
        {
            loMouseOffsetPHI = 0;
            loMouseOffsetTHETA = 0;
        }

        /// <summary>
        /// Applies offset PHI and THETA to the camera's values.  
        /// Usually ResetMouseOffsets gets called immediately after this
        /// or you get a double effect.
        /// </summary>
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

        /// <summary>
        /// Action based on mousewheel change
        /// </summary>
        /// <param name="delta"></param>
        public void MouseWheelAction(float delta)
        {
            CAMDISTANCE += ((CAMERAZOOMSPEED-4.8f)  + ((CAMDISTANCE / MINZOOM) * 2.5f)) * ((-delta * 2.9f));
            //System.Console.WriteLine(((CAMERAZOOMSPEED - 4.8f) + ((CAMDISTANCE / MINZOOM) * 4)) * (-delta * 1.9f));
            if (CAMDISTANCE < MAXZOOM) CAMDISTANCE = MAXZOOM;
            if (CAMDISTANCE >= MINZOOM) CAMDISTANCE = MINZOOM;

            UpdateCameraPosition();
        }

        /// <summary>
        /// Set Camera target to a Position
        /// </summary>
        /// <param name="ptarget"></param>
        public void SetTarget(Vector3D ptarget)
        {
            SNCamera.Target = ptarget;
            Vector3 target = new Vector3(ptarget.X, ptarget.Y, ptarget.Z);
            Vector3 camerapos = new Vector3(SNCamera.Position.X, SNCamera.Position.Y, SNCamera.Position.Z);
            CAMDISTANCE = Vector3.Distance(camerapos, target);
            UpdateCameraPosition();
        }

        /// <summary>
        /// Set Target to a ScenNode (for tracking)
        /// </summary>
        /// <param name="pTarget"></param>
        public void SetTarget(SceneNode pTarget)
        {
            SetTarget(pTarget.Position);
            SNtarget = pTarget;
        }

        /// <summary>
        /// Check to ensure that we're still focused on our target.
        /// </summary>
        public void CheckTarget()
        {
            if (SNtarget != null)
            {
                if (SNtarget.Raw != IntPtr.Zero)
                {
                    try
                    {
                        switch (CameraMode)
                        {
                            case ECameraMode.Build:
                                if (SNtarget.Position != SNCamera.Target)
                                {
                                    SNCamera.Target = SNtarget.Position;
                                    UpdateCameraPosition();
                                }
                                break;
                            case ECameraMode.Third:
                                UpdateCameraPosition();
                                break;
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

        /// <summary>
        /// Prepare Camera LookAT for LibOMV
        /// </summary>
        /// <returns></returns>
        public Vector3[] GetCameraLookAt()
        {
            IrrlichtNETCP.Matrix4 viewm = SNCamera.ViewMatrix;
            IrrlichtNETCP.Matrix4 transform = BaseIdealistViewer.Cordinate_XYZ_XZY.Matrix;
            transform.MakeInverse();
            viewm = viewm * transform;

            LookAtCam[0].X = viewm.M[0];
            LookAtCam[0].Y = viewm.M[2];
            LookAtCam[0].Z = viewm.M[1];
            LookAtCam[1].X = viewm.M[4];
            LookAtCam[1].Y = viewm.M[6];
            LookAtCam[1].Z = viewm.M[5];
            LookAtCam[2].Z = viewm.M[8];
            LookAtCam[2].Z = viewm.M[10];
            LookAtCam[2].Z = viewm.M[9];
            return LookAtCam;
        }

        /// <summary>
        /// Used for Orbiting the Camera based on the mouse
        /// </summary>
        /// <param name="deltaX">Mouse change X (pixels)</param>
        /// <param name="deltaY">Mouse change Y (pixels)</param>

        public void SetDeltaFromMouse(float deltaX, float deltaY)
        {
            CamRotationAnglePHI = CamRotationAnglePHI + ((deltaY * CAMERASPEED) * 0.2f);
            CamRotationAngleTHETA = CamRotationAngleTHETA + ((-deltaX * CAMERASPEED) * 0.2f);

            UpdateCameraPosition();
        }

        /// <summary>
        /// Translate the mouse position on the screen into a ray in 3D space
        /// </summary>
        /// <param name="mpos"></param>
        /// <param name="WindowWidth_DIV2"></param>
        /// <param name="WindowHeight_DIV2"></param>
        /// <param name="aspect"></param>
        /// <returns></returns>
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

        public void SwitchMode(ECameraMode pNewMode)
        {
            CameraMode = pNewMode;
            UpdateCameraPosition();
        }
    }
    public enum ECameraMode : int
    {
        Build = 1,
        Third = 2, 
        First = 3
    }


}
