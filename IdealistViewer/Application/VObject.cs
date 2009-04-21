using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class VObject
    {
        public SceneNode node; // Reference to graphics node
        public Primitive prim; // Avatar Extend the primative type
        public Mesh mesh; // Reference to graphics mesh
        public bool updateFullYN = false;

        public  VObject()

        {
        }

    }
}
