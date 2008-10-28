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
            MeshBuffer mb = new MeshBuffer(VertexType.Standard);

            try
            {
                int numViewerFaces = newPrim.viewerFaces.Count;
                uint index = 0;
                for (uint i = 0; i < numViewerFaces; i++)
                {
                    ViewerFace vf = newPrim.viewerFaces[(int)i];
                    if (isSphere)
                    {
                        vf.uv1.U = (vf.uv1.U - 0.5f) * 2.0f;
                        vf.uv2.U = (vf.uv2.U - 0.5f) * 2.0f;
                        vf.uv3.U = (vf.uv3.U - 0.5f) * 2.0f;
                   
                    }
                    mb.SetVertex(index, new Vertex3D(convVect3d(vf.v1), convNormal(vf.n1), color, convVect2d(vf.uv1)));
                    mb.SetVertex(index + 1, new Vertex3D(convVect3d(vf.v2), convNormal(vf.n2), color, convVect2d(vf.uv2)));
                    mb.SetVertex(index + 2, new Vertex3D(convVect3d(vf.v3), convNormal(vf.n3), color, convVect2d(vf.uv3)));

                    mb.SetIndex(index, (ushort)index);
                    mb.SetIndex(index + 1, (ushort)(index + 2));
                    mb.SetIndex(index + 2, (ushort)(index + 1));

                    index += 3;
                }
                mesh.AddMeshBuffer(mb);
                // don't dispose here
                //mb.Dispose();
            }
            catch (AccessViolationException)
            {
                mesh = null;
            }

            return mesh;
        }

        // prolly should delete this, it was experimental when we were first getting prims to display
        public static Mesh PrimitiveToIrrMeshOld(Primitive prim)
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
                sides = 24;

            if ((HoleType)primData.ProfileHole == HoleType.Same)
                hollowsides = sides;
            else if ((HoleType)primData.ProfileHole == HoleType.Circle)
                hollowsides = 24;
            else if ((HoleType)primData.ProfileHole == HoleType.Triangle)
                hollowsides = 3;

            PrimMesh newPrim = new PrimMesh(sides, profileBegin, profileEnd, (float)primData.ProfileHollow, hollowsides);
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

            //newPrim.AddRot(new Quat(new Coord(0, 0, 1), 90 * Utils.DEG_TO_RAD));
            //newPrim.CalcNormals();

            Mesh mesh = new Mesh();
            MeshBuffer mb = new MeshBuffer(VertexType.Standard);

            try
            {

                for (int index = 0; index < newPrim.coords.Count; index++)
                {
                    Vertex3D vert = new Vertex3D();

                    vert.Position = convVect3d(newPrim.coords[index]);
                    vert.Color = new Color(255, 0, 50, 250);
                    //vert.Normal = convVect3d(newPrim.normals[index]);
                    mb.SetVertex((uint)index, vert);
                }

                uint nr = 0;
                int faceIndex = 0;
                foreach (Face f in newPrim.faces)
                {
                    // use surface normals for now...
                    Vector3D surfaceNormal = convVect3d(newPrim.SurfaceNormal(faceIndex));


                    mb.SetIndex(nr++, (ushort)f.v1);
                    mb.SetIndex(nr++, (ushort)f.v3);
                    mb.SetIndex(nr++, (ushort)f.v2);

                    try
                    {
                        mb.GetVertex((ushort)f.v1).Normal = surfaceNormal;
                        mb.GetVertex((ushort)f.v2).Normal = surfaceNormal;
                        mb.GetVertex((ushort)f.v3).Normal = surfaceNormal;
                    }
                    catch (System.Reflection.TargetInvocationException)
                    {
                        m_log.Warn("[NORMAL]: Target Invocation error on a normal");
                    }

                    faceIndex++;
                }

                mesh.AddMeshBuffer(mb);
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
