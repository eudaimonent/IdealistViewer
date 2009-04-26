using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using IdealistViewer.Network;

namespace IdealistViewer
{

    /// <summary>
    /// Avatar controller class containing steering algorithms and state.
    /// </summary>
    public class AvatarController
    {

        private INetworkInterface m_protocol = null;
        private SceneNode m_sceneNode = null;

        public float Heading = 0;
        private bool m_turningLeft = false;
        private bool m_turningRight = false;
        
        private const float m_pi = (float)Math.PI;
        private const float m_pi2 = m_pi * 2;
        private float m_turnIncrement = (m_pi / 240f);

        public volatile int m_userRotated = 0;

        public AvatarController(INetworkInterface protocol, SceneNode sceneNode)
        {
            m_protocol = protocol;
            m_sceneNode = sceneNode;
        }

        public SceneNode AvatarNode
        {
            set { m_sceneNode = value; }

        }

        public void Update(int ticks)
        {
            ticks = (int)(ticks * 0.2f);
            if (m_turningLeft != m_turningRight)
            {
                if (m_turningLeft)
                {
                    Heading = NormalizeHeading(Heading + m_turnIncrement * ticks);
                    m_userRotated = System.Environment.TickCount;
                }
                else if (m_turningRight)
                {
                    Heading = NormalizeHeading(Heading - m_turnIncrement * ticks);
                    m_userRotated = System.Environment.TickCount;
                }
                UpdateLocal();
                
            }
        }

        public void UpdateRemote()
        {
            m_protocol.UpdateFromHeading(Heading);
            
        }
        public void UpdateLocal()
        {
            if (m_sceneNode != null && m_sceneNode.Raw != IntPtr.Zero)
            {
                try
                {
                    float vheading = -Heading * NewMath.RADTODEG;
                    //m_sceneNode.Rotation = new Vector3D(0, vheading,0);
                }
                catch (AccessViolationException)
                {
                    m_sceneNode = null; 
                }
            }
        }

        private float NormalizeHeading(float heading)
        {
            while (heading >= m_pi)
                heading -= m_pi2;
            while (heading < -m_pi)
                heading += m_pi2;
            return heading;
        }

        public bool TurnLeft
        {
            get { return m_turningLeft; }
            set { m_turningLeft = value; }
        }

        public bool TurnRight
        {
            get { return m_turningRight; }
            set { m_turningRight = value; }
        }

        public bool Forward
        {
            get { return m_protocol.Forward; }
            set { m_protocol.Forward = value; }
        }

        public bool Back
        {
            get { return m_protocol.Backward; }
            set { m_protocol.Backward = value; }
        }

        public bool StrafeLeft
        {
            get { return m_protocol.StraffLeft; }
            set { m_protocol.StraffLeft = value; }
        }

        public bool StrafeRight
        {
            get { return m_protocol.StraffRight; }
            set { m_protocol.StraffRight = value; }
        }

        public bool Up
        {
            get { return m_protocol.Up; }
            set { m_protocol.Up = value; }
        }

        public bool Down
        {
            get { return m_protocol.Down; }
            set { m_protocol.Down = value; }
        }

        public bool Fly
        {
            get { return m_protocol.Flying; }
            set { m_protocol.Flying = value; }
        }

        public bool Jump
        {
            set { m_protocol.Jump = value; }
        }

    }
}
