using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class FoliageObject
    {
        public SceneNode node; // Reference to graphics node
        public Primitive prim; // Avatar Extend the primative type
        public ulong regionHandle;
        public bool updateFullYN = false;

        public FoliageObject()
        {
        }

    }
}
