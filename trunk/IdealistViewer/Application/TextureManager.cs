using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using IrrlichtNETCP;


namespace IdealistViewer
{
    
    public delegate void TextureCallback(string texname, SceneNode node);
    public class TextureManager
    {
        public event TextureCallback OnTextureLoaded;

        private VideoDriver driver = null;
        private string imagefolder = string.Empty;
        private Dictionary<UUID, TextureExtended> memoryTextures = new Dictionary<UUID, TextureExtended>();
        private Dictionary<UUID, List<SceneNode>> ouststandingRequests = new Dictionary<UUID, List<SceneNode>>();
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

        public void RequestImage(UUID assetID, SceneNode requestor)
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
                tex = new TextureExtended(driver.GetTexture(System.IO.Path.Combine(texturefolderpath, assetID.ToString() + ".tga")).Raw);
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
                    List<SceneNode> requestors = new List<SceneNode>();
                    requestors.Add(requestor);
                    ouststandingRequests.Add(assetID,requestors);
                    
                }
            }
            
            m_user.RequestTexture(assetID);

        }

        public void applyTexture(TextureExtended tex, SceneNode requestor)
        {
            try
            {
                // works
                
                bool alphaimage = false;

                if (tex.Userdata == null)
                {

                    Color[,] imgcolors;

                    tex.Lock();
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
                    tex.Userdata = (object)alphaimage;
                }
                else
                {
                    alphaimage = (bool)tex.Userdata;
                }
                
               
                //requestor.SetMaterialType(MaterialType.NormalMapTransparentVertexAlpha);
                requestor.SetMaterialTexture(0, tex);
                
                //requestor.SetMaterialType(MaterialType.TransparentVertexAlpha);
                //requestor.SetMaterialType(MaterialType.TransparentAlphaChannel);
                
                //requestor.SetMaterialType(MaterialType.DetailMap);
                requestor.SetMaterialFlag(MaterialFlag.Lighting, true);
                requestor.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                requestor.SetMaterialFlag(MaterialFlag.BackFaceCulling, BaseIdealistViewer.backFaceCulling);
                requestor.SetMaterialFlag(MaterialFlag.GouraudShading, true);
                if (alphaimage)
                {
                    requestor.SetMaterialType(MaterialType.TransparentAlphaChannelRef);
                }

            }
            catch (AccessViolationException)
            {
                m_log.Error("[TEXTURE]: Failed to load texture.");
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

                
                
                List<SceneNode> nodesToUpdate = new List<SceneNode>();
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
                        SceneNode node = nodesToUpdate[i];

                        if (node != null)
                        {
                            if (OnTextureLoaded != null)
                            {
                                OnTextureLoaded(asset.AssetID.ToString() + ".tga", node);
                            }
                            
                        }
                        
                    }
                }
            }
        }
    }
}
