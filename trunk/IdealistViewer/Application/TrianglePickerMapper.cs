using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class TrianglePickerMapper
    {
        // The hashcode for the triangle selector is (int)IntPtr based on it's pointer location from Irrlicht
        private List<TriangleSelector> trilist = new List<TriangleSelector>();

        // Don't make this public or you'll have race conditions.
        private Dictionary<IntPtr, SceneNode> triLookup = new Dictionary<IntPtr, SceneNode>();

        private SceneCollisionManager scm = null;

        public TrianglePickerMapper(SceneCollisionManager pscm)
        {
            scm = pscm;
        }

        public void AddTriangleSelector(TriangleSelector trisel, SceneNode node)
        {
            lock (trisel)
            {
                trilist.Add(trisel);

                if (triLookup.ContainsKey(trisel.Raw))
                    triLookup[trisel.Raw] = node;
                else 
                    triLookup.Add(trisel.Raw, node);
            }
        }

        public SceneNode GetSceneNodeFromRay(Line3D ray, int bitMask, bool noDebug, Vector3D campos)
        {
            SceneNode returnobj = null;
            Vector3D collisionpoint = new Vector3D(0, 0, 0);
            Triangle3D tri = new Triangle3D(0, 0, 0, 0, 0, 0, 0, 0, 0);
            Vector3D closestpoint = new Vector3D(999, 999, 9999);
            lock (trilist)
            {
                foreach (TriangleSelector trisel in trilist)
                {
                    if (trisel == null)
                        continue;
                    if (trisel.Raw == IntPtr.Zero)
                        continue;
                    try
                    {
                        if (scm.GetCollisionPoint(ray, trisel, out collisionpoint, out tri))
                        {
                            if (campos.DistanceFrom(collisionpoint) < campos.DistanceFrom(closestpoint))
                            {
                                closestpoint = collisionpoint;

                                if (triLookup.ContainsKey(trisel.Raw))
                                {
                                    SceneNode sreturnobj = triLookup[trisel.Raw];
                                    if (!(sreturnobj is TerrainSceneNode))
                                        returnobj = sreturnobj;
                                }

                            }

                        }
                    }
                    catch (AccessViolationException)
                    {
                        continue;
                    }
                }

            }
            return returnobj;
        }

        public void RemTriangleSelector(TriangleSelector trisel)
        {
            if (trisel != null)
            {
                lock (trisel)
                {
                    trilist.Remove(trisel);
                    if (triLookup.ContainsKey(trisel.Raw))
                        triLookup.Remove(trisel.Raw);
                }
            }
        }
    }
}
