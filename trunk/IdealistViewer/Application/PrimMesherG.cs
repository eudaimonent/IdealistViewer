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

            MeshBuffer[] mb = new MeshBuffer[newPrim.numPrimFaces];
                  
            for (int i=0;i<mb.Length;i++)
                mb[i] = new MeshBuffer(VertexType.Standard);
             
            try
            {
                uint[] index = new uint[mb.Length];
                
                for(int i=0;i<index.Length;i++)
                    index[i] = 0;

                for (uint i = 0; i < numViewerFaces; i++)
                {
                    ViewerFace vf = newPrim.viewerFaces[(int)i];

                    if (isSphere)
                    {
                        vf.uv1.U = (vf.uv1.U - 0.5f) * 2.0f;
                        vf.uv2.U = (vf.uv2.U - 0.5f) * 2.0f;
                        vf.uv3.U = (vf.uv3.U - 0.5f) * 2.0f;
                    }
                    try
                    {
                        mb[vf.primFaceNumber].SetVertex(index[vf.primFaceNumber], new Vertex3D(convVect3d(vf.v1), convNormal(vf.n1), color, convVect2d(vf.uv1)));
                        mb[vf.primFaceNumber].SetVertex(index[vf.primFaceNumber] + 1, new Vertex3D(convVect3d(vf.v2), convNormal(vf.n2), color, convVect2d(vf.uv2)));
                        mb[vf.primFaceNumber].SetVertex(index[vf.primFaceNumber] + 2, new Vertex3D(convVect3d(vf.v3), convNormal(vf.n3), color, convVect2d(vf.uv3)));

                    }
                    catch (OutOfMemoryException)
                    {
                        return null;
                    }

                    mb[vf.primFaceNumber].SetIndex(index[vf.primFaceNumber], (ushort)index[vf.primFaceNumber]);
                    mb[vf.primFaceNumber].SetIndex(index[vf.primFaceNumber] + 1, (ushort)(index[vf.primFaceNumber] + 2));
                    mb[vf.primFaceNumber].SetIndex(index[vf.primFaceNumber] + 2, (ushort)(index[vf.primFaceNumber] + 1));

                    index[vf.primFaceNumber] += 3;
                }

                for (int i=0; i<mb.Length;i++)
                    mesh.AddMeshBuffer(mb[i]);

                // don't dispose here
                //mb.Dispose();
            }
            catch (AccessViolationException)
            {
                m_log.Error("ACCESSVIOLATION");
                mesh = null;
            }

            return mesh;
        }

        public static Mesh SculptIrrMesh(System.Drawing.Bitmap bitmap, OpenMetaverse.SculptType omSculptType)
        {
            switch (omSculptType)
            {
                case OpenMetaverse.SculptType.Cylinder:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.cylinder);
                case OpenMetaverse.SculptType.Plane:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.plane);
                case OpenMetaverse.SculptType.Sphere:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.sphere);
                case OpenMetaverse.SculptType.Torus:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.torus);
                default:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.plane);
            }
        }

        public static Mesh SculptIrrMesh(System.Drawing.Bitmap bitmap)
        {
            return SculptIrrMesh(bitmap, PrimMesher.SculptMesh.SculptType.sphere);
        }

        public static Mesh SculptIrrMesh(System.Drawing.Bitmap bitmap, PrimMesher.SculptMesh.SculptType sculptType)
        {
            SculptMesh newSculpty = new SculptMesh(bitmap, sculptType, 32, true);
            //newSculpty.Scale(15, 15, 15);
            Color color = new Color(255, 255, 0, 50);

            Mesh mesh = new Mesh();

            int numViewerFaces = newSculpty.viewerFaces.Count;
            Console.WriteLine("SculptIrrMesh(): numViewerFaces: " + numViewerFaces.ToString());

            MeshBuffer[] mb = new MeshBuffer[1];

            for (int i = 0; i < mb.Length; i++)
                mb[i] = new MeshBuffer(VertexType.Standard);

            try
            {
                uint[] index = new uint[mb.Length];

                for (int i = 0; i < index.Length; i++)
                    index[i] = 0;

                for (uint i = 0; i < numViewerFaces; i++)
                {
                    ViewerFace vf = newSculpty.viewerFaces[(int)i];

                    try
                    {
                        mb[vf.primFaceNumber].SetVertex(index[vf.primFaceNumber], new Vertex3D(convVect3d(vf.v1), convNormal(vf.n1), color, convVect2d(vf.uv1)));
                        mb[vf.primFaceNumber].SetVertex(index[vf.primFaceNumber] + 1, new Vertex3D(convVect3d(vf.v2), convNormal(vf.n2), color, convVect2d(vf.uv2)));
                        mb[vf.primFaceNumber].SetVertex(index[vf.primFaceNumber] + 2, new Vertex3D(convVect3d(vf.v3), convNormal(vf.n3), color, convVect2d(vf.uv3)));

                    }
                    catch (OutOfMemoryException)
                    {
                        return null;
                    }

                    mb[vf.primFaceNumber].SetIndex(index[vf.primFaceNumber], (ushort)index[vf.primFaceNumber]);
                    mb[vf.primFaceNumber].SetIndex(index[vf.primFaceNumber] + 1, (ushort)(index[vf.primFaceNumber] + 2));
                    mb[vf.primFaceNumber].SetIndex(index[vf.primFaceNumber] + 2, (ushort)(index[vf.primFaceNumber] + 1));

                    index[vf.primFaceNumber] += 3;
                }

                for (int i = 0; i < mb.Length; i++)
                    mesh.AddMeshBuffer(mb[i]);

                // don't dispose here
                //mb.Dispose();
            }
            catch (AccessViolationException)
            {
                m_log.Error("ACCESSVIOLATION");
                mesh = null;
            }

            return mesh;
        }

        internal static Mesh SculptIrrMesh(System.Drawing.Image image)
        {
            throw new NotImplementedException();
        }
    }
}
