using System;
using System.Collections.Generic;
using System.Text;

namespace IdealistViewer
{
    public class AnimationManager
    {
        private Viewer m_viewer;

        public int AnimationFramesPerSecond = 30;

        //for animation debugging...
        public int StartFrame = 0;
        public int StopFrame = 90;
        public bool FramesDirty = false;

        public AnimationManager(Viewer viewer)
        {
            m_viewer = viewer;
        }

    }
}
