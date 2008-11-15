using System;
using System.Collections.Generic;
using System.Text;

namespace IdealistViewer
{
    class AvatarController
    {
        float heading = 0;
        bool turning_left = false;
        bool turning_right = false;
        
        private const float pi = (float)Math.PI;
        private const float pi2 = pi * 2;
        float turn_increment = (pi / 240f);

        SLProtocol avatarConnection = null;
        
        public AvatarController(SLProtocol pAvatarConnection)
        {
            avatarConnection = pAvatarConnection;


        }

        public void update(int ticks)
        {
            if (turning_left != turning_right)
            {
                if (turning_left)
                {
                    heading = normalizeHeading(heading + turn_increment * ticks);
                }
                else if (turning_right)
                {
                    heading = normalizeHeading(heading - turn_increment * ticks);
                }
                Set_Heading(heading);
            }
        }

        public void Set_Heading(float heading)
        {
            avatarConnection.UpdateFromHeading(heading);
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
            get { return false; }
            set { return; }
        }

        public bool Down
        {
            get { return false; }
            set { return; }
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
