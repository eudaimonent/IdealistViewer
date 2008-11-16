using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using OpenMetaverse;

namespace IdealistViewer
{
    public class MeshFactory
    {
        private Dictionary<string, IrrlichtNETCP.Mesh> StoredMesh = new Dictionary<string, Mesh>();
        
        private MeshManipulator mm = null;
        
        public MeshFactory(MeshManipulator pmm)
        {
            mm = pmm;
        }

        public IrrlichtNETCP.Mesh GetMeshInstance(Primitive prim)
        {
            Primitive.ConstructionData primData = prim.PrimData;
            int sides = 4;
            int hollowsides = 4;

            float profileBegin = primData.ProfileBegin;
            float profileEnd = primData.ProfileEnd;
            

            if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.Circle)
                sides = 24;
            else if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.EqualTriangle)
                sides = 3;
            else if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.HalfCircle)
            { // half circle, prim is a sphere
                sides = 24;
                profileBegin = 0.5f * profileBegin + 0.5f;
                profileEnd = 0.5f * profileEnd + 0.5f;
            }

            if ((HoleType)primData.ProfileHole == HoleType.Same)
                hollowsides = sides;
            else if ((HoleType)primData.ProfileHole == HoleType.Circle)
                hollowsides = 24;
            else if ((HoleType)primData.ProfileHole == HoleType.Triangle)
                hollowsides = 3;
            Mesh objMesh = null;

            string code = (sides.ToString() + profileBegin.ToString() + profileEnd.ToString() + ((float)primData.ProfileHollow).ToString() + hollowsides.ToString() + primData.PathScaleX.ToString() + primData.PathScaleY.ToString() + primData.PathBegin.ToString() +
                primData.PathEnd.ToString() + primData.PathShearX.ToString() + primData.PathShearY.ToString() +
                primData.PathRadiusOffset.ToString() + primData.PathRevolutions.ToString() + primData.PathSkew.ToString() +
                ((int)primData.PathCurve).ToString() + primData.PathScaleX.ToString() + primData.PathScaleY.ToString() +
                primData.PathTwistBegin.ToString() + primData.PathTwist.ToString());
            
            lock (StoredMesh)
            {
                if (StoredMesh.ContainsKey(code))
                {
                    objMesh = StoredMesh[code];
                }
            }

            if (objMesh == null)
            {
                objMesh = PrimMesherG.PrimitiveToIrrMesh(prim);
                lock (StoredMesh)
                {
                    if (!StoredMesh.ContainsKey(code))
                    {
                        StoredMesh.Add(code, objMesh);
                    }
                }
            }

            // outside lock.
            if (objMesh != null)
            {
                return mm.CreateMeshCopy(objMesh);
            }

            return null;
        }
        public int UniqueObjects
        {
            get { return StoredMesh.Count; }
        }
    }
}
