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
        private Dictionary<string, IrrlichtNETCP.Mesh> IdenticalMesh = new Dictionary<string, Mesh>();
        IrrlichtDevice device;
        private MeshManipulator mm = null;
        private List<IntPtr> killed = new List<IntPtr>();
        
        public MeshFactory(MeshManipulator pmm, IrrlichtDevice pdevice)
        {
            mm = pmm;
            device = pdevice;
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

            string storedmeshcode = (sides.ToString() + profileBegin.ToString() + profileEnd.ToString() + ((float)primData.ProfileHollow).ToString() + hollowsides.ToString() + primData.PathScaleX.ToString() + primData.PathScaleY.ToString() + primData.PathBegin.ToString() +
                primData.PathEnd.ToString() + primData.PathShearX.ToString() + primData.PathShearY.ToString() +
                primData.PathRadiusOffset.ToString() + primData.PathRevolutions.ToString() + primData.PathSkew.ToString() +
                ((int)primData.PathCurve).ToString() + primData.PathScaleX.ToString() + primData.PathScaleY.ToString() +
                primData.PathTwistBegin.ToString() + primData.PathTwist.ToString());

            
            
            bool identicalcandidate = true;
            if (prim.Textures != null)
            {
                foreach (Primitive.TextureEntryFace face in prim.Textures.FaceTextures)
                {
                    if (face != null)
                        identicalcandidate = false;
                }
            }

            StringBuilder sbIdenticalMesh = new StringBuilder();
            sbIdenticalMesh.Append(storedmeshcode);
            if (prim.Textures != null)
            {
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.TextureID);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.Bump);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.Fullbright);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.Glow);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.MediaFlags);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.OffsetU);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.OffsetV);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.RepeatU);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.RepeatV);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.RGBA.ToRGBString());
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.Rotation);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.Shiny);
                sbIdenticalMesh.Append(prim.Textures.DefaultTexture.TexMapType.ToString());
            }

            string identicalmeshcode = sbIdenticalMesh.ToString();


            if (identicalcandidate)
            {
                lock (IdenticalMesh)
                {
                    if (IdenticalMesh.ContainsKey(identicalmeshcode))
                        objMesh = IdenticalMesh[identicalmeshcode];
                    
                }
                if (objMesh == null)
                {
                    objMesh = PrimMesherG.PrimitiveToIrrMesh(prim, LevelOfDetail.High);
                }
                lock (IdenticalMesh)
                {
                    if (!IdenticalMesh.ContainsKey(identicalmeshcode))
                        IdenticalMesh.Add(identicalmeshcode, objMesh);
                }

                lock (StoredMesh)
                {
                    if (!StoredMesh.ContainsKey(storedmeshcode))
                        StoredMesh.Add(storedmeshcode, objMesh);
                }
                return objMesh;
            }

            
            lock (StoredMesh)
            {
                if (StoredMesh.ContainsKey(storedmeshcode))
                {
                    objMesh = StoredMesh[storedmeshcode];
                }
            }

            if (objMesh == null)
            {
                objMesh = PrimMesherG.PrimitiveToIrrMesh(prim, LevelOfDetail.High);
                lock (StoredMesh)
                {
                    if (!StoredMesh.ContainsKey(storedmeshcode))
                    {
                        StoredMesh.Add(storedmeshcode, objMesh);
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

        public Mesh GetSculptMesh(UUID assetid, TextureExtended sculpttex, SculptType stype, Primitive prim)
        {
            Mesh result = null;


            lock (StoredMesh)
            {
                if (StoredMesh.ContainsKey(assetid.ToString()))
                {
                    result = StoredMesh[assetid.ToString()];
                    return result;
                }
            }
            if (result == null)
            {
                System.Drawing.Bitmap bm = sculpttex.DOTNETImage;
                result = PrimMesherG.SculptIrrMesh(bm, stype);
                if (!killed.Contains(sculpttex.Raw))
                {
                    try
                    {
                        killed.Add(sculpttex.Raw);
                        device.VideoDriver.RemoveTexture(sculpttex);
                    }
                    catch (AccessViolationException)
                    {
                        System.Console.WriteLine("Unable to remove a sculpt texture from the video driver!");
                    }
                }
                bm.Dispose();
                if (result != null)
                {
                    lock (StoredMesh)
                    {
                        if (!StoredMesh.ContainsKey(assetid.ToString()))
                        {
                            StoredMesh.Add(assetid.ToString(), result);
                        }
                    }
                }
            }

            if (result != null)
            {
                //return mm.CreateMeshCopy(result);
                return result;
            }

            return null;
            
        }

        public int UniqueObjects
        {
            get { return StoredMesh.Count; }
        }
    }
}
