using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class AvatarController
    {
        float heading = 0;
        bool turning_left = false;
        bool turning_right = false;
        
        private const float pi = (float)Math.PI;
        private const float pi2 = pi * 2;
        float turn_increment = (pi / 240f);

        private IProtocol avatarConnection = null;
        private SceneNode av = null;

        public volatile int userRotated = 0;

        public AvatarController(IProtocol pAvatarConnection, SceneNode pAVNode)
        {
            avatarConnection = pAvatarConnection;
            av = pAVNode;
        }

        public SceneNode AvatarNode
        {
            set { av = value; }

        }

        public void update(int ticks)
        {
            ticks = (int)(ticks * 0.2f);
            if (turning_left != turning_right)
            {
                if (turning_left)
                {
                    heading = normalizeHeading(heading + turn_increment * ticks);
                    userRotated = System.Environment.TickCount;
                }
                else if (turning_right)
                {
                    heading = normalizeHeading(heading - turn_increment * ticks);
                    userRotated = System.Environment.TickCount;
                }
                UpdateLocal();
                
            }
        }

        public void UpdateRemote()
        {
            avatarConnection.UpdateFromHeading(heading);
            
        }
        public void UpdateLocal()
        {
            if (av != null && av.Raw != IntPtr.Zero)
            {
                try
                {
                    float vheading = -heading * NewMath.RADTODEG;
                    av.Rotation = new Vector3D((vheading > 180)? 180 : 0, vheading, (vheading > 180)? 180 : 0);
                }
                catch (AccessViolationException)
                {
                    av = null; 
                }
            }
        }

        public bool TurnLeft
        {
            get { return turning_left; }
            set { turning_left = value; }
        }

        public bool TurnRight
        {
            get { return turning_right; }
            set { turning_right = value; }
        }

        public bool Forward
        {
            get { return avatarConnection.Forward; }
            set { avatarConnection.Forward = value; }
        }

        public bool Back
        {
            get { return avatarConnection.Backward; }
            set { avatarConnection.Backward = value; }
        }

        public bool StrafeLeft
        {
            get { return avatarConnection.StraffLeft; }
            set { avatarConnection.StraffLeft = value; }
        }

        public bool StrafeRight
        {
            get { return avatarConnection.StraffRight; }
            set { avatarConnection.StraffRight = value; }
        }

        public bool Up
        {
            get { return avatarConnection.Up; }
            set { avatarConnection.Up = value; }
        }

        public bool Down
        {
            get { return avatarConnection.Down; }
            set { avatarConnection.Down = value; }
        }

        public bool Fly
        {
            get { return avatarConnection.Flying; }
            set { avatarConnection.Flying = value; }
        }

        public bool Jump
        {
            set { avatarConnection.Jump = value; }
        }

        private float normalizeHeading(float heading)
        {
            while (heading >= pi)
                heading -= pi2;
            while (heading < -pi)
                heading += pi2;
            return heading;
        }


    }
}
