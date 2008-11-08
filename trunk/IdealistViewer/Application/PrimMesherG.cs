using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenMetaverse;
using PrimMesher;
using IrrlichtNETCP;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;

namespace IdealistViewer
{
    public static class PrimMesherG
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Vector2D convVect2d(UVCoord uv)
        {
            return new Vector2D(uv.U, uv.V);
        }

        public static Vector3D convVect3d(Coord c)
        {// translate coordinates XYZ to XZY
            return new Vector3D(c.X, c.Z, c.Y);
        }

        public static Vector3D convNormal(Coord c)
        {// translate coordinates XYZ to XZY
            return new Vector3D(c.X, c.Z, c.Y);
        }

        public static Mesh PrimitiveToIrrMesh(Primitive prim)
        {
            Primitive.ConstructionData primData = prim.PrimData;
            int sides = 4;
            int hollowsides = 4;

            float profileBegin = primData.ProfileBegin;
            float profileEnd = primData.ProfileEnd;
            bool isSphere = false;

            if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.Circle)
                sides = 24;
            else if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.EqualTriangle)
                sides = 3;
            else if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.HalfCircle)
            { // half circle, prim is a sphere
                isSphere = true;
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

            PrimMesh newPrim = new PrimMesh(sides, profileBegin, profileEnd, (float)primData.ProfileHollow, hollowsides);
            newPrim.viewerMode = true;
            newPrim.holeSizeX = primData.PathScaleX;
            newPrim.holeSizeY = primData.PathScaleY;
            newPrim.pathCutBegin = primData.PathBegin;
            newPrim.pathCutEnd = primData.PathEnd;
            newPrim.topShearX = primData.PathShearX;
            newPrim.topShearY = primData.PathShearY;
            newPrim.radius = primData.PathRadiusOffset;
            newPrim.revolutions = primData.PathRevolutions;
            newPrim.skew = primData.PathSkew;
            newPrim.stepsPerRevolution = 24;

            if (primData.PathCurve == PathCurve.Line)
            {
                newPrim.taperX = 1.0f - primData.PathScaleX;
                newPrim.taperY = 1.0f - primData.PathScaleY;
                newPrim.twistBegin = (int)(180 * primData.PathTwistBegin);
                newPrim.twistEnd = (int)(180 * primData.PathTwist);
                newPrim.ExtrudeLinear();
            }
            else
            {
                newPrim.taperX = primData.PathTaperX;
                newPrim.taperY = primData.PathTaperY;
                newPrim.twistBegin = (int)(360 * primData.PathTwistBegin);
                newPrim.twistEnd = (int)(360 * primData.PathTwist);
                newPrim.ExtrudeCircular();
            }
            
            Color color = new Color(255, 255, 0, 50);

            Mesh mesh = new Mesh();
           
            int numViewerFaces = newPrim.viewerFaces.Count;
            int numPrimFaces = 0;
            int startface = 90;
            
            
            for (int i = 0; i < numViewerFaces; i++)
            {
                numPrimFaces = (newPrim.viewerFaces[i].primFaceNumber > numPrimFaces) ? newPrim.viewerFaces[i].primFaceNumber : numPrimFaces;
                startface = (newPrim.viewerFaces[i].primFaceNumber < numPrimFaces) ? newPrim.viewerFaces[i].primFaceNumber : numPrimFaces;
            }
            MeshBuffer[] mb;

            if (numPrimFaces == 0)
            {
                mb = new MeshBuffer[1];
            }
            else
            {
                mb = new MeshBuffer[numPrimFaces];
            }

            for (int i=0;i<mb.Length;i++)
                mb[i] = new MeshBuffer(VertexType.Standard);
             
            try
            {
                
                

                m_log.DebugFormat("MaxFace:{0} - StartFace:{1}",numPrimFaces,startface);

                uint[] index = new uint[mb.Length];
                
                for(int i=0;i<index.Length;i++)
                    index[i] = 0;

                for (uint i = 0; i < numViewerFaces; i++)
                {
                    ViewerFace vf = newPrim.viewerFaces[(int)i];
                    
                    int face = (vf.primFaceNumber != 0) ? vf.primFaceNumber - 1 : 0;

                    if (isSphere)
                    {
                        vf.uv1.U = (vf.uv1.U - 0.5f) * 2.0f;
                        vf.uv2.U = (vf.uv2.U - 0.5f) * 2.0f;
                        vf.uv3.U = (vf.uv3.U - 0.5f) * 2.0f;
                    }
                    try
                    {
                        mb[face].SetVertex(index[face], new Vertex3D(convVect3d(vf.v1), convNormal(vf.n1), color, convVect2d(vf.uv1)));
                        mb[face].SetVertex(index[face] + 1, new Vertex3D(convVect3d(vf.v2), convNormal(vf.n2), color, convVect2d(vf.uv2)));
                        mb[face].SetVertex(index[face] + 2, new Vertex3D(convVect3d(vf.v3), convNormal(vf.n3), color, convVect2d(vf.uv3)));

                    }
                    catch (OutOfMemoryException)
                    {
                        return null;
                    }

                    mb[face].SetIndex(index[face], (ushort)index[face]);
                    mb[face].SetIndex(index[face] + 1, (ushort)(index[face] + 2));
                    mb[face].SetIndex(index[face] + 2, (ushort)(index[face] + 1));

                    index[face] += 3;
                }

                for (int i=0; i<mb.Length;i++)
                    mesh.AddMeshBuffer(mb[i]);

                // don't dispose here
                //mb.Dispose();
            }
            catch (AccessViolationException)
            {
                mesh = null;
            }

            return mesh;
        }
    }
}
