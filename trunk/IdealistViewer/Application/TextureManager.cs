using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using OpenMetaverse;
//using OpenMetaverse.Rendering;
using IrrlichtNETCP;


namespace IdealistViewer
{
    
    public delegate void TextureCallback(string texname, VObject node, UUID AssetID);
    public class TextureManager
    {
        public event TextureCallback OnTextureLoaded;

        private VideoDriver driver = null;
        private string imagefolder = string.Empty;
        private Dictionary<UUID, TextureExtended> memoryTextures = new Dictionary<UUID, TextureExtended>();
        private Dictionary<UUID, List<VObject>> ouststandingRequests = new Dictionary<UUID, List<VObject>>();
        private IrrlichtDevice device = null;
        private SLProtocol m_user = null;
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public TextureManager(IrrlichtDevice pdevice, VideoDriver pDriver, string folder, SLProtocol pm_user)
        {
            driver = pDriver;
            device = pdevice;
            imagefolder = folder;
            m_user = pm_user;

            m_user.OnImageReceived += imageReceivedCallback;
        }

        public void RequestImage(UUID assetID, VObject requestor)
        {
            
            TextureExtended tex = null;

            lock (memoryTextures)
            {
                
                if (memoryTextures.ContainsKey(assetID))
                {
                    tex = memoryTextures[assetID];
                }
            }

            if (tex != null)
            {
                applyTexture(tex, requestor, assetID);
                

                return;
            }

            string texturefolderpath = device.FileSystem.WorkingDirectory; //System.IO.Path.Combine(Util.ApplicationDataDirectory, imagefolder);
            //device.FileSystem.WorkingDirectory = texturepath;

            if (File.Exists(System.IO.Path.Combine(texturefolderpath, assetID.ToString() + ".tga")))
            {
                string oldfs = device.FileSystem.WorkingDirectory;
                device.FileSystem.WorkingDirectory = texturefolderpath;
                Texture texTnorm = driver.GetTexture(System.IO.Path.Combine(texturefolderpath, assetID.ToString() + ".tga"));
                tex = new TextureExtended(texTnorm.Raw);
                if (tex != null)
                {
                    lock (memoryTextures)
                    {
                        if (!memoryTextures.ContainsKey(assetID))
                        {
                            memoryTextures.Add(assetID, tex);
                        }
                    }
                    applyTexture(tex, requestor, assetID);
                    
                    return;
                }

            }

            lock (ouststandingRequests)
            {
                if (ouststandingRequests.ContainsKey(assetID))
                {
                    ouststandingRequests[assetID].Add(requestor);
                    
                    return;
                }
                else 
                {
                    List<VObject> requestors = new List<VObject>();
                    requestors.Add(requestor);
                    ouststandingRequests.Add(assetID,requestors);
                    
                }
            }
            
            m_user.RequestTexture(assetID);

        }

        public void applyTexture(TextureExtended tex, VObject vObj, UUID AssetID)
        {
            try
            {
                // works
                
                bool alphaimage = false;

                if (tex.Userdata == null)
                {

                    Color[,] imgcolors;

                    tex.Lock();
                    try
                    {
                        imgcolors = tex.Retrieve();
                        tex.Unlock();
                        for (int i = 0; i < imgcolors.GetUpperBound(0); i++)
                        {
                            for (int j = 0; j < imgcolors.GetUpperBound(1); j++)
                            {
                                if (imgcolors[i, j].A != 255)
                                {
                                    alphaimage = true;
                                    break;
                                }
                            }
                            if (alphaimage)
                                break;
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        alphaimage = false;
                    }
                    tex.Userdata = (object)alphaimage;
                }
                else
                {
                    alphaimage = (bool)tex.Userdata;
                }

                if (vObj.prim.Textures != null)
                {
                    if (vObj.prim.Textures.DefaultTexture != null)
                    {
                        Color4 coldata = vObj.prim.Textures.DefaultTexture.RGBA;
                        float shinyval = 0;
                        switch (vObj.prim.Textures.DefaultTexture.Shiny)
                        {
                            case Shininess.Low:
                                shinyval = 0.8f;
                                coldata.R += 0.1f;
                                coldata.B += 0.1f;
                                coldata.G += 0.1f;
                                break;
                            case Shininess.Medium:
                                shinyval = 0.7f;
                                coldata.R += 0.2f;
                                coldata.B += 0.2f;
                                coldata.G += 0.2f;
                                break;
                            case Shininess.High:
                                shinyval = 0.6f;
                                coldata.R += 0.3f;
                                coldata.B += 0.3f;
                                coldata.G += 0.3f;
                                break;
                        }
                        int mbcount = vObj.mesh.MeshBufferCount;
                        for (int j = 0; j < mbcount; j++)
                        {
                            // Only apply default texture if there isn't one already!
                            if (vObj.prim.Textures.DefaultTexture.TextureID == AssetID)
                            {
                                ApplyFaceSettings(vObj, alphaimage, vObj.prim.Textures.DefaultTexture, tex, j, shinyval, coldata);
                            }
                            else
                            {
                                // Apply color settings
                                ApplyFaceSettings(vObj, alphaimage, vObj.prim.Textures.DefaultTexture, null, j, shinyval, coldata);
                            }
                            
                            vObj.mesh.GetMeshBuffer(j).Material.NormalizeNormals = true;
                            vObj.mesh.GetMeshBuffer(j).Material.GouraudShading = true;
                            vObj.mesh.GetMeshBuffer(j).Material.BackfaceCulling = BaseIdealistViewer.backFaceCulling;
                            
                        }

                    }

                    // default taken care of..   now on to the individual face settings.
                    for (int i = 0; i < vObj.prim.Textures.FaceTextures.Length; i++)
                    {
                        if (vObj.prim.Textures.FaceTextures[i] != null)
                        {
                            Primitive.TextureEntryFace teface = vObj.prim.Textures.FaceTextures[i];
                            

                            if (vObj.mesh.MeshBufferCount - 1 > i)
                            {
                                //if (tex.
                                Color4 coldata = teface.RGBA;
                                float shinyval = 0;
                                switch (teface.Shiny)
                                {
                                    case Shininess.Low:
                                        shinyval = 0.8f;
                                        coldata.R += 0.1f;
                                        coldata.B += 0.1f;
                                        coldata.G += 0.1f;
                                        break;
                                    case Shininess.Medium:
                                        shinyval = 0.7f;
                                        coldata.R += 0.2f;
                                        coldata.B += 0.2f;
                                        coldata.G += 0.2f;
                                        break;
                                    case Shininess.High:
                                        shinyval = 0.6f;
                                        coldata.R += 0.3f;
                                        coldata.B += 0.3f;
                                        coldata.G += 0.3f;
                                        break;
                                }

                                if (teface.TextureID == AssetID)
                                {
                                    ApplyFaceSettings(vObj, alphaimage, teface, tex, i, shinyval, coldata);
                                }
                                else
                                {
                                    ApplyFaceSettings(vObj, alphaimage, teface, null, i, shinyval, coldata);
                                }
                                vObj.mesh.GetMeshBuffer(i).Material.NormalizeNormals = true;
                                vObj.mesh.GetMeshBuffer(i).Material.GouraudShading = true;
                                vObj.mesh.GetMeshBuffer(i).Material.BackfaceCulling = BaseIdealistViewer.backFaceCulling;
                            }
                            else
                            {
                                m_log.Warn("[TEXTUREDEF]: Unable to apply Texture to face because mesh buffer doesn't have definition for face");

                            }
                        }// end check if textureentry face is null
                    } // end loop over textureentry faces array

                    SceneNode sn = device.SceneManager.AddMeshSceneNode(vObj.mesh, null, -1);
                    sn.Position = vObj.node.Position;
                    sn.Rotation = vObj.node.Rotation;
                    sn.Scale = vObj.node.Scale;
                    sn.TriangleSelector = vObj.node.TriangleSelector;


                    SceneNode oldnode = vObj.node;
                    vObj.node = sn;
                    device.SceneManager.AddToDeletionQueue(oldnode);
                } // prim texture is not null


                
            }
            catch (AccessViolationException)
            {
                m_log.Error("[TEXTURE]: Failed to load texture.");
            }
        }

                 //requestor.node.SetMaterialTexture(0, tex);
                //requestor.node.SetMaterialFlag(MaterialFlag.Lighting, true);
               // requestor.node.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                //requestor.node.SetMaterialFlag(MaterialFlag.BackFaceCulling, BaseIdealistViewer.backFaceCulling);
                //requestor.node.SetMaterialFlag(MaterialFlag.GouraudShading, true);
                //if (alphaimage)
                //{
               //     requestor.node.SetMaterialType(MaterialType.TransparentAlphaChannelRef);
               // }
        public void ApplyFaceSettings(VObject vObj, bool alpha, Primitive.TextureEntryFace teface, Texture tex, int face, 
            float shinyval, Color4 coldata)
        {
            if (tex != null)
            {
                vObj.mesh.GetMeshBuffer(face).Material.Texture1 = tex;
            }

            if (teface != null)
            {
                ApplyFace(vObj, alpha, face, teface, shinyval, coldata);
            }
        }

        public void ApplyFace(VObject vObj, bool alpha, int face, Primitive.TextureEntryFace teface, float shinyval, Color4 coldata)
        {
            ModifyMeshBuffer(coldata, shinyval, face, vObj.mesh, teface, alpha);
        }

        public void ModifyMeshBuffer(Color4 coldata, float shinyval, int j, Mesh mesh, Primitive.TextureEntryFace teface, bool alpha)
        {
            if (coldata != Color4.White)
            {
                mesh.GetMeshBuffer(j).Material.SpecularColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 50, 0, 255));
                mesh.GetMeshBuffer(j).Material.AmbientColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 50, 0, 255));
                mesh.GetMeshBuffer(j).Material.EmissiveColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 5, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 5, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 5, 0, 255));
                //vObj.mesh.GetMeshBuffer(j).Material.DiffuseColor = new Color((int)(coldata.A * 255), (int)(coldata.R * 255), (int)(coldata.B * 255), (int)(coldata.G * 255));
            }

            mesh.GetMeshBuffer(j).Material.Shininess = shinyval;

            if (teface.Fullbright)
            {
                mesh.GetMeshBuffer(j).Material.Lighting = !teface.Fullbright;
            }

            if (alpha)
            {
                mesh.GetMeshBuffer(j).Material.MaterialType = MaterialType.TransparentAlphaChannelRef;
            }

            if (coldata.A != 1)
            {
                mesh.GetMeshBuffer(j).Material.ZWriteEnable = true;
                //vObj.mesh.GetMeshBuffer(j).Material.ZBuffer = 0;
                mesh.GetMeshBuffer(j).Material.BackfaceCulling = true;
                uint vcount = (uint)mesh.GetMeshBuffer(j).VertexCount;
                for (uint j2 = 0; j2 < vcount; j2++)
                {

                    mesh.GetMeshBuffer(j).GetVertex(j2).Color = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255), 0, 255), Util.Clamp<int>((int)(coldata.G * 255), 0, 255), Util.Clamp<int>((int)(coldata.B * 255), 0, 255));

                }
                mesh.GetMeshBuffer(j).Material.MaterialTypeParam = (float)MaterialType.TransparentVertexAlpha;
            }
        }

        public void imageReceivedCallback(AssetTexture asset)
        {
            if (asset == null)
            {
                m_log.Debug("[TEXTURE]: GotLIBOMV callback but asset was null");
                lock (ouststandingRequests)
                {
                }
                return;
            }
            m_log.Debug("[TEXTURE]: GotLIBOMV callback for asset" + asset.AssetID);
            bool result = false;

            try
            {
                result = asset.Decode();
            }
            catch (Exception)
            {
                m_log.Debug("[TEXTURE]: Failed to decode asset " + asset.AssetID);
            }
            if (result)
            { 
                
                string texturefolderpath = device.FileSystem.WorkingDirectory;//System.IO.Path.Combine(Util.ApplicationDataDirectory, imagefolder);

                string texturepath = System.IO.Path.Combine(texturefolderpath,asset.AssetID.ToString() + ".tga");
                byte[] imgdata = asset.Image.ExportTGA();
                FileStream fi = (File.Open(texturepath, FileMode.Create));
                BinaryWriter bw = new BinaryWriter(fi);
                bw.Write(imgdata);
                bw.Flush();
                bw.Close();
                //fi.Flush();
                //fi.Close();
                //fi.Dispose();

                
                
                List<VObject> nodesToUpdate = new List<VObject>();
                lock (ouststandingRequests)
                {
                    if (ouststandingRequests.ContainsKey(asset.AssetID))
                    {
                        nodesToUpdate = ouststandingRequests[asset.AssetID];
                        ouststandingRequests.Remove(asset.AssetID);
                    }
                }
                lock (nodesToUpdate)
                {
                    for (int i = 0; i < nodesToUpdate.Count; i++)
                    {
                        VObject vObj = nodesToUpdate[i];

                        if (vObj != null)
                        {
                            if (OnTextureLoaded != null)
                            {
                                OnTextureLoaded(asset.AssetID.ToString() + ".tga", vObj, asset.AssetID);
                            }
                            
                        }
                        
                    }
                }
            }
        }
    }
}
