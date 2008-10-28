using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using IrrlichtNETCP;


namespace IdealistViewer
{
    public class TextureManager
    {
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
                    return;
                }
                else 
                {
                    List<SceneNode> requestors = new List<SceneNode>();
                    requestors.Add(requestor);
                    ouststandingRequests.Add(assetID,requestors);
                }
            }
            m_log.DebugFormat("[TEXTURE]: Requesting TextureID: {0} from simulator", assetID);
            m_user.RequestTexture(assetID);

        }

        private void applyTexture(Texture tex, SceneNode requestor)
        {

            requestor.SetMaterialTexture(0, tex);
            requestor.SetMaterialType(MaterialType.DetailMap);
        }
        
        public void imageReceivedCallback(AssetTexture asset)
        {
            if (asset == null)
                return;
            m_log.Debug("[TEXTURE]: GotLIBOMV callback for asset" + asset.AssetID);
            bool result = false;

            try
            {
                result = asset.Decode();
            }
            catch (Exception)
            {

            }
            if (result)
            { 
                
                string texturefolderpath = device.FileSystem.WorkingDirectory;//System.IO.Path.Combine(Util.ApplicationDataDirectory, imagefolder);

                string texturepath = System.IO.Path.Combine(texturefolderpath,asset.AssetID.ToString() + ".tga");
                byte[] imgdata = asset.Image.ExportTGA();
                BinaryWriter bw = new BinaryWriter(File.Open(texturepath, FileMode.Create));
                bw.Write(imgdata);
                bw.Flush();
                bw.Close();
                
                Texture tex = driver.GetTexture(texturepath);

                lock (memoryTextures)
                {
                    if (!memoryTextures.ContainsKey(asset.AssetID))
                        memoryTextures.Add(asset.AssetID, tex);

                }
                List<SceneNode> nodesToUpdate = new List<SceneNode>();
                lock (ouststandingRequests)
                {
                    if (ouststandingRequests.ContainsKey(asset.AssetID))
                    {
                        nodesToUpdate = ouststandingRequests[asset.AssetID];

                    }
                }
                lock (nodesToUpdate)
                {
                    for (int i = 0; i < nodesToUpdate.Count; i++)
                    {
                        SceneNode node = nodesToUpdate[i];

                        if (node != null)
                        {
                            applyTexture(tex, node);
                            
                        }
                        
                    }
                }
            }
        }
    }
}
