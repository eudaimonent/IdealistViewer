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
    public enum LevelOfDetail
    {
        Low,
        Medium,
        High
    }

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

        private static Mesh PrimMeshToIrrMesh(PrimMesh primMesh)
        {
            Color color = new Color(255, 255, 0, 50);

            Mesh mesh;
            try
            {
                mesh = new Mesh();
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }

            //VertexIndexer vi = new VertexIndexer(primMesh);
            VertexIndexer vi = primMesh.GetVertexIndexer();

            MeshBuffer[] mb = new MeshBuffer[primMesh.numPrimFaces];

            for (int i = 0; i < mb.Length; i++)
                mb[i] = new MeshBuffer(VertexType.Standard);

            try
            {
                uint[] index = new uint[mb.Length];

                for (int i = 0; i < index.Length; i++)
                    index[i] = 0;

                for (int primFaceNum = 0; primFaceNum < primMesh.numPrimFaces; primFaceNum++)
                {
                    
                    List<ViewerVertex> vertList = vi.viewerVertices[primFaceNum];
                    for (uint i = 0; i < vertList.Count; i++)
                    {
                        try
                        {
                            ViewerVertex v = vertList[(int)i];
                            Vertex3D v3d = new Vertex3D(convVect3d(v.v), convNormal(v.n), color, convVect2d(v.uv));
                            mb[primFaceNum].SetVertex(i, v3d);
                        }
                        catch (OutOfMemoryException)
                        {
                            return null;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return null;
                        }
                    }

                    List<ViewerPolygon> polyList = vi.viewerPolygons[primFaceNum];
                    uint mbIndex = 0;
                    for (uint i = 0; i < polyList.Count; i++)
                    {
                        ViewerPolygon p = polyList[(int)i];
                        mb[primFaceNum].SetIndex(mbIndex++, (ushort)p.v1);
                        mb[primFaceNum].SetIndex(mbIndex++, (ushort)p.v3);
                        mb[primFaceNum].SetIndex(mbIndex++, (ushort)p.v2);
                    }
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


        private static Mesh FacesToIrrMesh(List<ViewerFace> viewerFaces, int numPrimFaces)
        {
            Color color = new Color(255, 255, 0, 50);

            Mesh mesh;
            try
            {
                mesh = new Mesh();
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
            int numViewerFaces = viewerFaces.Count;

            MeshBuffer[] mb = new MeshBuffer[numPrimFaces];

            for (int i = 0; i < mb.Length; i++)
                mb[i] = new MeshBuffer(VertexType.Standard);

            try
            {
                uint[] index = new uint[mb.Length];

                for (int i = 0; i < index.Length; i++)
                    index[i] = 0;

                for (uint i = 0; i < numViewerFaces; i++)
                {
                    ViewerFace vf = viewerFaces[(int)i];

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
                    catch (IndexOutOfRangeException)
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


        // experimental - build sculpt mesh using indexed access to vertex, normal, and UV lists
        private static Mesh SculptMeshToIrrMesh(SculptMesh sculptMesh)
        {
            Color color = new Color(255, 255, 0, 50);

            Mesh mesh = new Mesh();

            int numFaces = sculptMesh.faces.Count;

            MeshBuffer mb = new MeshBuffer(VertexType.Standard);

            int numVerts = sculptMesh.coords.Count;

            try
            {
                Vector3D minVector = new Vector3D(float.MaxValue,float.MaxValue,float.MaxValue);
                Vector3D maxVector = new Vector3D(float.MinValue,float.MinValue,float.MinValue);
                for (int i = 0; i < numVerts; i++)
                {
                    Vector3D vector=convVect3d(sculptMesh.coords[i]);
                    mb.SetVertex((uint)i, new Vertex3D(vector, convNormal(sculptMesh.normals[i]), color, convVect2d(sculptMesh.uvs[i])));
                    minVector.X = Math.Min(minVector.X, vector.X);
                    minVector.Y = Math.Min(minVector.Y, vector.Y);
                    minVector.Z = Math.Min(minVector.Z, vector.Z);
                    maxVector.X = Math.Max(maxVector.X, vector.X);
                    maxVector.Y = Math.Max(maxVector.Y, vector.Y);
                    maxVector.Z = Math.Max(maxVector.Z, vector.Z);
                }

                ushort index = 0;
                foreach (Face face in sculptMesh.faces)
                {
                    mb.SetIndex(index++, (ushort)face.v1);
                    mb.SetIndex(index++, (ushort)face.v3);
                    mb.SetIndex(index++, (ushort)face.v2);
                }
                mb.BoundingBox = new Box3D(minVector*2,maxVector*2);
                mesh.AddMeshBuffer(mb);
                //mesh.BoundingBox = new Box3D(minVector * 2, maxVector * 2);

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
      
        public static Mesh PrimitiveToIrrMesh(Primitive prim, LevelOfDetail detail)
        {
            Primitive.ConstructionData primData = prim.PrimData;
            int sides = 4;
            int hollowsides = 4;

            float profileBegin = primData.ProfileBegin;
            float profileEnd = primData.ProfileEnd;
            bool isSphere = false;

            if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.Circle)
            {
                switch (detail)
                {
                    case LevelOfDetail.Low:
                        sides = 6;
                        break;
                    case LevelOfDetail.Medium:
                        sides = 12;
                        break;
                    default:
                        sides = 24;
                        break;
                }
            }
            else if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.EqualTriangle)
                sides = 3;
            else if ((ProfileCurve)(primData.profileCurve & 0x07) == ProfileCurve.HalfCircle)
            { // half circle, prim is a sphere
                isSphere = true;
                switch (detail)
                {
                    case LevelOfDetail.Low:
                        sides = 6;
                        break;
                    case LevelOfDetail.Medium:
                        sides = 12;
                        break;
                    default:
                        sides = 24;
                        break;
                }
                profileBegin = 0.5f * profileBegin + 0.5f;
                profileEnd = 0.5f * profileEnd + 0.5f;
            }

            if ((HoleType)primData.ProfileHole == HoleType.Same)
                hollowsides = sides;
            else if ((HoleType)primData.ProfileHole == HoleType.Circle)
            {
                switch (detail)
                {
                    case LevelOfDetail.Low:
                        hollowsides = 6;
                        break;
                    case LevelOfDetail.Medium:
                        hollowsides = 12;
                        break;
                    default:
                        hollowsides = 24;
                        break;
                }

            }
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
            switch (detail)
            {
                case LevelOfDetail.Low:
                    newPrim.stepsPerRevolution = 6;
                    break;
                case LevelOfDetail.Medium:
                    newPrim.stepsPerRevolution = 12;
                    break;
                default:
                    newPrim.stepsPerRevolution = 24;
                    break;
            }

            

            //if (primData.PathCurve == PathCurve.Line)
            if (primData.PathCurve == PathCurve.Line || primData.PathCurve == PathCurve.Flexible)
            {
                newPrim.taperX = 1.0f - primData.PathScaleX;
                newPrim.taperY = 1.0f - primData.PathScaleY;
                newPrim.twistBegin = (int)(180 * primData.PathTwistBegin);
                newPrim.twistEnd = (int)(180 * primData.PathTwist);
                //newPrim.ExtrudeLinear();
                if (primData.PathCurve == PathCurve.Line)
                    newPrim.Extrude(PathType.Linear);
                else
                    newPrim.Extrude(PathType.Flexible);
            }
            else
            {
                newPrim.taperX = primData.PathTaperX;
                newPrim.taperY = primData.PathTaperY;
                newPrim.twistBegin = (int)(360 * primData.PathTwistBegin);
                newPrim.twistEnd = (int)(360 * primData.PathTwist);
                //newPrim.ExtrudeCircular();
                newPrim.Extrude(PathType.Circular);
            }

            int numViewerFaces = newPrim.viewerFaces.Count;

            for (uint i = 0; i < numViewerFaces; i++)
            {
                ViewerFace vf = newPrim.viewerFaces[(int)i];

                if (isSphere)
                {
                    vf.uv1.U = (vf.uv1.U - 0.5f) * 2.0f;
                    vf.uv2.U = (vf.uv2.U - 0.5f) * 2.0f;
                    vf.uv3.U = (vf.uv3.U - 0.5f) * 2.0f;
                }
            }

            //return FacesToIrrMesh(newPrim.viewerFaces, newPrim.numPrimFaces);
            return PrimMeshToIrrMesh(newPrim);
        }

        public static Mesh SculptIrrMesh(System.Drawing.Bitmap bitmap, byte sculptType)
        {
            return SculptIrrMesh(bitmap, sculptType, null);
        }

        public static Mesh SculptIrrMesh(System.Drawing.Bitmap bitmap, byte sculptType, string rawFileName)
        {
            bool mirror = ((sculptType & 128) != 0);
            bool invert = ((sculptType & 64) != 0);

            OpenMetaverse.SculptType omSculptType = (OpenMetaverse.SculptType)(sculptType & 0x07);

            switch (omSculptType)
            {
                case OpenMetaverse.SculptType.Cylinder:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.cylinder, mirror, invert, rawFileName);
                case OpenMetaverse.SculptType.Plane:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.plane, mirror, invert, rawFileName);
                case OpenMetaverse.SculptType.Sphere:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.sphere, mirror, invert, rawFileName);
                case OpenMetaverse.SculptType.Torus:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.torus, mirror, invert, rawFileName);
                default:
                    return SculptIrrMesh(bitmap, SculptMesh.SculptType.plane, mirror, invert, rawFileName);
            }
        }

        public static Mesh SculptIrrMesh(System.Drawing.Bitmap bitmap)
        {
            return SculptIrrMesh(bitmap, PrimMesher.SculptMesh.SculptType.plane, false, false, null);
        }

        public static Mesh SculptIrrMesh(System.Drawing.Bitmap bitmap, PrimMesher.SculptMesh.SculptType sculptType, bool mirror, bool invert, string rawFileName)
        {
            mirror = invert = false; // remove this after updating to a libomv that can pass these flags
            sculptType = (SculptMesh.SculptType)((int)sculptType & 0x07); // make sure only using lower 3 bits

            SculptMesh newSculpty = new SculptMesh(bitmap, sculptType, 32, true, mirror, invert);
            if (rawFileName != null)
                newSculpty.DumpRaw("", rawFileName, "");

            //return FacesToIrrMesh(newSculpty.viewerFaces, 1);

            // experimental - build sculpt mesh using vertex, normal, and coord lists
            return SculptMeshToIrrMesh(newSculpty);
        }

        public static Mesh SculptIrrMesh(float[,] zMap, float minX, float maxX, float minY, float maxY)
        {
            return SculptMeshToIrrMesh(new SculptMesh(zMap, minX, maxX, minY, maxY, true));
        }
    }
}
