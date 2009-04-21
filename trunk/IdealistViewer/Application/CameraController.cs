using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using IrrlichtNETCP;

namespace IdealistViewer
{
    
    /// <summary>
    /// Camera controller class containing camera steering algorithms and state.
    /// </summary>
    public class CameraController
    {
        private Viewer m_viewer;

        public CameraSceneNode CameraNode;
        public SceneNode TargetNode;
        public ECameraMode CameraMode = ECameraMode.Build;

        private Vector3[] m_cameraOrientation = new Vector3[3];
        private static Vector3 m_lastTargetPosition = Vector3.Zero;

        private float m_speed = 0.05f;
        private float m_zoomSpeed = 6f;
        private static float m_startAngle = 0f;
        private static float m_startDistance = 3f;
        public float m_distance = 5f;
        private float m_maxZoom = 1f;
        private float m_minZoom = 10f;
        private float m_pi2 = (float)(Math.PI * 2);
        public float Phi = 0f;
        public float Theta = 0f;
        public float OffsetPhi = 0;
        public float OffsetTheta = 0;
        public Vector3D TargetOffset = new Vector3D(0, 1f, 0);
        
        private SceneManager m_sceneManager = null;

        /// <summary>
        /// User's Camera
        /// </summary>
        /// <param name="psmgr">Scene Manager</param>
        public CameraController(Viewer viewer,SceneManager psmgr)
        {
            m_viewer = viewer;
            TargetNode = null;
            m_sceneManager = psmgr;
            CameraNode = m_sceneManager.AddCameraSceneNode(null);
            CameraNode.Position = new Vector3D(0f, 0f, 0f);
            CameraNode.Target = new Vector3D(0f, 0f, 0f);

            Phi = m_startAngle;
            Theta = m_startAngle;
            m_distance = m_startDistance;

            m_cameraOrientation[0] = Vector3.Zero;
            m_cameraOrientation[1] = Vector3.Zero;
            m_cameraOrientation[2] = Vector3.Zero;

            UpdateCameraPosition();
        
        }
        //LibOMV camera position
        public Vector3 Position
        {
            get { return new Vector3(CameraNode.Position.X,CameraNode.Position.Y,CameraNode.Position.Z); }
        }
        /// <summary>
        /// Update camera position based on it's current PHI, Theta, and mouse offset.
        /// </summary>
        public void UpdateCameraPosition()
        {
            Vector3D newpos = new Vector3D();
            Vector3D oldTarget = CameraNode.Target;
            switch (CameraMode)
            {
                case ECameraMode.Build:

                    newpos.X = oldTarget.X + (m_distance * (float)Math.Cos(Theta + OffsetTheta) * (float)Math.Sin(Phi + OffsetPhi));
                    newpos.Y = oldTarget.Y + m_distance * (float)Math.Cos(Phi + OffsetPhi);
                    newpos.Z = oldTarget.Z + m_distance * (float)Math.Sin(Theta + OffsetTheta) * (float)Math.Sin(Phi + OffsetPhi);


                    CameraNode.Position = newpos;
                    CameraNode.Target = oldTarget;
                    CameraNode.UpdateAbsolutePosition();
                    break;
                case ECameraMode.Third:

                    if (TargetNode != null)
                    {
                        
                        Vector3D currentTargetPos = TargetNode.Position;
                        //Vector3D currentTargetRot = TargetNode.Rotation;
                        Vector3D Delta1 = new Vector3D(-m_distance, 0, 0);
                        IrrlichtNETCP.Matrix4 transform1 = new IrrlichtNETCP.Matrix4();

                        transform1.RotationRadian = new Vector3D(0, 0, Phi + OffsetPhi);

                        transform1.TransformVect(ref Delta1);

                        transform1.RotationRadian = new Vector3D(0, Theta + OffsetTheta, 0);
                        
                        transform1.TransformVect(ref Delta1);

                        newpos = Delta1 + new Vector3D(0, 0.5f * m_distance, 0);
                        CameraNode.Position = currentTargetPos + newpos + (TargetOffset * 0.5f);
                        CameraNode.Target = TargetNode.Position + TargetOffset;
                        
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
                    m_distance -= (m_zoomSpeed - ((m_maxZoom - m_distance) / m_distance));
                    if (m_distance < m_maxZoom) m_distance = m_maxZoom;
                    if (m_distance > m_minZoom) m_distance = m_minZoom;

                    UpdateCameraPosition();
                    break;

                case KeyCode.Down:
                    m_distance += m_zoomSpeed - ((m_maxZoom - m_distance) / m_distance);
                    if (m_distance < m_maxZoom) m_distance = m_maxZoom;
                    if (m_distance > m_minZoom) m_distance = m_minZoom;

                    UpdateCameraPosition();
                    break;

                case KeyCode.Left:
                    Theta -= m_speed;
                    while (Theta < m_pi2)
                        Theta += m_pi2;

                    UpdateCameraPosition();
                    break;

                case KeyCode.Right:
                    Theta += m_speed;
                    while (Theta > m_pi2)
                        Theta -= m_pi2;

                    UpdateCameraPosition();
                    break;

                    // Page Down
                case KeyCode.Next:

                    Phi -= m_speed;
                    while (Phi < m_pi2)
                        Phi += m_pi2;

                    UpdateCameraPosition();
                    break;

                    // Page Up
                case KeyCode.Prior:
                    //vOrbit.Y += 2f;
                    Phi += m_speed;
                    while (Phi > m_pi2)
                        Phi -= m_pi2;

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
            OffsetPhi = 0;
            OffsetTheta = 0;
        }

        /// <summary>
        /// Applies offset PHI and THETA to the camera's values.  
        /// Usually ResetMouseOffsets gets called immediately after this
        /// or you get a double effect.
        /// </summary>
        public void ApplyMouseOffsets()
        {
            if (OffsetPhi != 0 || OffsetTheta != 0)
            {

                Phi = Phi + OffsetPhi;
                Theta = Theta + OffsetTheta;
                OffsetPhi = 0;
                OffsetTheta = 0;
                UpdateCameraPosition();
            }
        }

        /// <summary>
        /// Action based on mousewheel change
        /// </summary>
        /// <param name="delta"></param>
        public void MouseWheelAction(float delta)
        {
            m_distance += ((m_zoomSpeed-4.8f)  + ((m_distance / m_minZoom) * 2.5f)) * ((-delta * 2.9f));
            //System.Console.WriteLine(((CAMERAZOOMSPEED - 4.8f) + ((CAMDISTANCE / MINZOOM) * 4)) * (-delta * 1.9f));
            if (m_distance < m_maxZoom) m_distance = m_maxZoom;
            if (m_distance >= m_minZoom) m_distance = m_minZoom;

            UpdateCameraPosition();
        }

        /// <summary>
        /// Set Camera target to a Position
        /// </summary>
        /// <param name="ptarget"></param>
        public void SetTarget(Vector3D ptarget)
        {
            CameraNode.Target = ptarget;
            Vector3 target = new Vector3(ptarget.X, ptarget.Y, ptarget.Z);
            Vector3 camerapos = new Vector3(CameraNode.Position.X, CameraNode.Position.Y, CameraNode.Position.Z);
            m_distance = Vector3.Distance(camerapos, target);
            UpdateCameraPosition();
        }

        /// <summary>
        /// Set Target to a ScenNode (for tracking)
        /// </summary>
        /// <param name="pTarget"></param>
        public void SetTarget(SceneNode pTarget)
        {
            SetTarget(pTarget.Position);
            TargetNode = pTarget;
        }

        /// <summary>
        /// Check to ensure that we're still focused on our target.
        /// </summary>
        public void CheckTarget()
        {
            if (TargetNode != null)
            {
                if (TargetNode.Raw != IntPtr.Zero)
                {
                    try
                    {
                        switch (CameraMode)
                        {
                            case ECameraMode.Build:
                                if (TargetNode.Position != CameraNode.Target)
                                {
                                    CameraNode.Target = TargetNode.Position;
                                    UpdateCameraPosition();
                                }
                                break;
                            case ECameraMode.Third:
                                if (TargetNode.Position != CameraNode.Target)
                                {
                                    CameraNode.Target = TargetNode.Position;
                                    UpdateCameraPosition();
                                }
                                break;
                        }
                    }
                    catch (AccessViolationException)
                    {
                        System.Console.WriteLine("Picked camera target is dead");
                        TargetNode = null;
                    }
                }
            }
        }

        /// <summary>
        /// Prepare Camera LookAT for LibOMV
        /// </summary>
        /// <returns></returns>
        public Vector3[] GetCameraViewMatrix()
        {
            IrrlichtNETCP.Matrix4 viewm = CameraNode.ViewMatrix;
            IrrlichtNETCP.Matrix4 transform = m_viewer.Renderer.CoordinateConversion_XYZ_XZY.Matrix;
            transform.MakeInverse();
            viewm = viewm * transform;

            m_cameraOrientation[0].X = viewm.M[0];
            m_cameraOrientation[0].Y = viewm.M[2];
            m_cameraOrientation[0].Z = viewm.M[1];
            m_cameraOrientation[1].X = viewm.M[4];
            m_cameraOrientation[1].Y = viewm.M[6];
            m_cameraOrientation[1].Z = viewm.M[5];
            m_cameraOrientation[2].Z = viewm.M[8];
            m_cameraOrientation[2].Z = viewm.M[10];
            m_cameraOrientation[2].Z = viewm.M[9];
            return m_cameraOrientation;
        }

        /// <summary>
        /// Used for Orbiting the Camera based on the mouse
        /// </summary>
        /// <param name="deltaX">Mouse change X (pixels)</param>
        /// <param name="deltaY">Mouse change Y (pixels)</param>

        public void SetDeltaFromMouse(float deltaX, float deltaY)
        {
            Phi = Phi + ((deltaY * m_speed) * 0.2f);
            Theta = Theta + ((-deltaX * m_speed) * 0.2f);

            UpdateCameraPosition();
        }

        /// <summary>
        /// Used for follow the Camera based on the mouse
        /// </summary>
        /// <param name="deltaX">Mouse change X (pixels)</param>
        /// <param name="deltaY">Mouse change Y (pixels)</param>

        public void SetRotationDelta(float deltaX, float deltaY)
        {
            Phi -= ((deltaY * m_speed) * 0.2f);
            if (Phi > Math.PI/5)
            {
                Phi = (float)(Math.PI / 5);
            }
            if (Phi < -Math.PI / 5)
            {
                Phi = (float)(-Math.PI / 5);
            }

            Theta += ((deltaX * m_speed) * 0.2f);
            Theta %= m_pi2;

            UpdateCameraPosition();
            m_viewer.AvatarController.Heading = -Theta - OffsetTheta;
            m_viewer.AvatarController.UpdateLocal();
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
            pos.X = (float)(Math.Tan(CameraNode.FOV * 0.5f) * (mpos.X / WindowWidth_DIV2 - 1.0f));
            pos.Y = (float)(Math.Tan(CameraNode.FOV * 0.5f) * (1.0f - mpos.Y / WindowHeight_DIV2) / aspect);

            Vector3D p1 = new Vector3D(pos.X * CameraNode.NearValue, pos.Y * CameraNode.NearValue, CameraNode.NearValue);
            Vector3D p2 = new Vector3D(pos.X * CameraNode.FarValue, pos.Y * CameraNode.FarValue, CameraNode.FarValue);

            // Inverse the view matrix
            IrrlichtNETCP.Matrix4 viewm = CameraNode.ViewMatrix;
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
