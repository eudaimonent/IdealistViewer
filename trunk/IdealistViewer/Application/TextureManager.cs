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
    
    public delegate void TextureCallback(string texname, VObject node);
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
                applyTexture(tex, requestor);
                

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
                    applyTexture(tex, requestor);
                    
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

        public void applyTexture(TextureExtended tex, VObject vObj)
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

                int mbcount = vObj.mesh.MeshBufferCount;
                for (int j = 0; j < mbcount; j++)
                {
                    vObj.mesh.GetMeshBuffer(j).Material.Texture1 = tex;
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
                ApplyFaceSettings(vObj,alphaimage);

               
            }
            catch (AccessViolationException)
            {
                m_log.Error("[TEXTURE]: Failed to load texture.");
            }
        }

        public void ApplyFaceSettings(VObject vObj, bool alpha)
        {
            if (vObj.prim.Textures != null)
            {
                if (vObj.prim.Textures.DefaultTexture != null)
                {
                    float shinyval = 0;
                    Color4 coldata = vObj.prim.Textures.DefaultTexture.RGBA;
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
                        if (coldata != Color4.White)
                        {
                            vObj.mesh.GetMeshBuffer(j).Material.SpecularColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 50, 0, 255));
                            vObj.mesh.GetMeshBuffer(j).Material.AmbientColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 50, 0, 255));
                            vObj.mesh.GetMeshBuffer(j).Material.EmissiveColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 5, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 5, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 5, 0, 255));
                            //vObj.mesh.GetMeshBuffer(j).Material.DiffuseColor = new Color((int)(coldata.A * 255), (int)(coldata.R * 255), (int)(coldata.B * 255), (int)(coldata.G * 255));
                        }

                        vObj.mesh.GetMeshBuffer(j).Material.Shininess = shinyval;

                        if (vObj.prim.Textures.DefaultTexture.Fullbright)
                        {
                            vObj.mesh.GetMeshBuffer(j).Material.Lighting = !vObj.prim.Textures.DefaultTexture.Fullbright;
                        }

                        if (alpha)
                        {
                            vObj.mesh.GetMeshBuffer(j).Material.MaterialType = MaterialType.TransparentAlphaChannelRef;
                        }

                        if (coldata.A != 1)
                        {
                            vObj.mesh.GetMeshBuffer(j).Material.ZWriteEnable = true;
                            vObj.mesh.GetMeshBuffer(j).Material.ZBuffer = 0;
                            vObj.mesh.GetMeshBuffer(j).Material.BackfaceCulling = true;
                            uint vcount = (uint)vObj.mesh.GetMeshBuffer(j).VertexCount;
                            for (uint j2 = 0; j2 < vcount; j2++)
                            {
                                vObj.mesh.GetMeshBuffer(j).GetVertex(j2).Color = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255), 0, 255), Util.Clamp<int>((int)(coldata.G * 255), 0, 255), Util.Clamp<int>((int)(coldata.B * 255), 0, 255));

                            }
                            vObj.mesh.GetMeshBuffer(j).Material.MaterialTypeParam = (float)MaterialType.TransparentAddColor;
                        }
                 /*
                         
                        else
                        {
                            if (vObj.mesh.GetMeshBuffer(j).Material.ZWriteEnable)
                            {
                                vObj.mesh.GetMeshBuffer(j).Material.ZWriteEnable = false;
                                vObj.mesh.GetMeshBuffer(j).Material.ZBuffer = 0;
                                uint vcount = (uint)vObj.mesh.GetMeshBuffer(j).VertexCount;

                                if (alpha)
                                {
                                    vObj.mesh.GetMeshBuffer(j).Material.MaterialTypeParam = (float)MaterialType.TransparentAlphaChannelRef;
                                    vObj.mesh.GetMeshBuffer(j).Material.BackfaceCulling = true;
                                
                                }
                                else
                                {
                                    vObj.mesh.GetMeshBuffer(j).Material.MaterialTypeParam = 0;
                                }
                            }
                        }
                 */

                        vObj.mesh.GetMeshBuffer(j).Material.NormalizeNormals = true;
                        vObj.mesh.GetMeshBuffer(j).Material.GouraudShading = true;
                        
                        
                    }
                    
                }
            }
            SceneNode sn = device.SceneManager.AddMeshSceneNode(vObj.mesh, null, -1);
            sn.Position = vObj.node.Position;
            sn.Rotation = vObj.node.Rotation;
            sn.Scale = vObj.node.Scale;
            sn.TriangleSelector = vObj.node.TriangleSelector;
           
             
            SceneNode oldnode = vObj.node;
            vObj.node = sn;
            device.SceneManager.AddToDeletionQueue(oldnode);
            

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
                                OnTextureLoaded(asset.AssetID.ToString() + ".tga", vObj);
                            }
                            
                        }
                        
                    }
                }
            }
        }
    }
}
