using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private Dictionary<UUID, Texture> memoryTextures = new Dictionary<UUID, Texture>();
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
            m_log.DebugFormat("[TEXTURE]: Object Requested TextureID: {0}", assetID);
            Texture tex = null;

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
                m_log.DebugFormat("[TEXTURE]: InProc texture found for TextureID: {0}", assetID);

                return;
            }

            string texturefolderpath = device.FileSystem.WorkingDirectory; //System.IO.Path.Combine(Util.ApplicationDataDirectory, imagefolder);
            //device.FileSystem.WorkingDirectory = texturepath;

            if (File.Exists(System.IO.Path.Combine(texturefolderpath, assetID.ToString() + ".tga")))
            {
                string oldfs = device.FileSystem.WorkingDirectory;
                device.FileSystem.WorkingDirectory = texturefolderpath;
                tex = driver.GetTexture(System.IO.Path.Combine(texturefolderpath, assetID.ToString() + ".tga"));
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
                    m_log.DebugFormat("[TEXTURE]: Disk texture found for TextureID: {0}", assetID);
                    return;
                }

            }

            lock (ouststandingRequests)
            {
                if (ouststandingRequests.ContainsKey(assetID))
                {
                    ouststandingRequests[assetID].Add(requestor);
                    m_log.Debug("[TEXTURE]: Added to OutstandingRequest receivers");
                    return;
                }
                else 
                {
                    List<SceneNode> requestors = new List<SceneNode>();
                    requestors.Add(requestor);
                    ouststandingRequests.Add(assetID,requestors);
                    m_log.Debug("[TEXTURE]: created new OutstandingRequest receivers");
                }
            }
            m_log.DebugFormat("[TEXTURE]: Requesting TextureID: {0} from simulator", assetID);
            m_user.RequestTexture(assetID);

        }

        public void applyTexture(Texture tex, SceneNode requestor)
        {
            try
            {
                requestor.SetMaterialTexture(0, tex);
                //requestor.SetMaterialType(MaterialType.DetailMap);
                requestor.SetMaterialFlag(MaterialFlag.Lighting, true);
                requestor.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                requestor.SetMaterialFlag(MaterialFlag.BackFaceCulling, BaseIdealistViewer.backFaceCulling);
                requestor.SetMaterialFlag(MaterialFlag.GouraudShading, true);

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
