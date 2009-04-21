using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class VObject
    {
        public SceneNode SceneNode;
        public Primitive Primitive;
        public Mesh Mesh;
        public bool FullUpdate = false;
        public Vector3 TargetPosition = new Vector3();
    }
}
