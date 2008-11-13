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
    
    /// <summary>
    /// Texture Callback for when we've got the texture
    /// </summary>
    /// <param name="texname"></param>
    /// <param name="node"></param>
    /// <param name="AssetID"></param>
    public delegate void TextureCallback(string texname, VObject node, UUID AssetID);
    
    /// <summary>
    /// Handles General texture work.  Downloading, requesting, managing duplicates etc.
    /// </summary>
    public class TextureManager
    {
        public event TextureCallback OnTextureLoaded;

        private VideoDriver driver = null;
        private string imagefolder = string.Empty;

        /// <summary>
        /// In Memory texture cache
        /// </summary>
        private Dictionary<UUID, TextureExtended> memoryTextures = new Dictionary<UUID, TextureExtended>();
        
        /// <summary>
        /// Texture requests outstanding.
        /// </summary>
        private Dictionary<UUID, List<VObject>> ouststandingRequests = new Dictionary<UUID, List<VObject>>();
        private IrrlichtDevice device = null;
        private SLProtocol m_user = null;
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Our picker.  It's here so that when an object gets retextured, we can manage the picker without leaking.
        /// </summary>
        private TrianglePickerMapper triPicker = null;
        private MetaTriangleSelector mts = null;

        public TextureManager(IrrlichtDevice pdevice, VideoDriver pDriver, TrianglePickerMapper ptriPicker, MetaTriangleSelector pmts, string folder, SLProtocol pm_user)
        {
            driver = pDriver;
            device = pdevice;
            imagefolder = folder;
            m_user = pm_user;
            triPicker = ptriPicker;
            mts = pmts;
            m_user.OnImageReceived += imageReceivedCallback;
        }
        /// <summary>
        /// Requests an image for an object.
        /// </summary>
        /// <param name="assetID"></param>
        /// <param name="requestor"></param>
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
                // We already have the texture, jump to applyTexture
                applyTexture(tex, requestor, assetID);
                

                return;
            }

            // Check to see if we've got the texture on disk

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
                            // Add it to the texture cache.
                            memoryTextures.Add(assetID, tex);
                        }
                    }

                    // apply texture
                    applyTexture(tex, requestor, assetID);
                    
                    return;
                }

            }

            // Check if we've already got an outstanding request for this texture
            lock (ouststandingRequests)
            {
                if (ouststandingRequests.ContainsKey(assetID))
                {
                    // Add it to the objects to be notified when this texture download is complete.
                    ouststandingRequests[assetID].Add(requestor);
                    
                    return;
                }
                else 
                {
                    // Create a new outstanding request entry
                    List<VObject> requestors = new List<VObject>();
                    requestors.Add(requestor);
                    ouststandingRequests.Add(assetID,requestors);
                    
                }
            }
            
            // Request texture from LibOMV
            m_user.RequestTexture(assetID);

        }

        /// <summary>
        /// If we've got the texture, return it and true.  If not, then return false
        /// </summary>
        /// <param name="assetID">the Asset ID of the texture</param>
        /// <param name="tex">The texture</param>
        /// <returns>If we have the texture or not</returns>
        public bool tryGetTexture(UUID assetID, out TextureExtended tex)
        {

            tex = null;
            lock (memoryTextures)
            {
                if (memoryTextures.ContainsKey(assetID))
                {
                    tex = memoryTextures[assetID];
                }
            }

            if (tex != null)
            {
                return true;
            }

            string texturefolderpath = device.FileSystem.WorkingDirectory; //System.IO.Path.Combine(Util.ApplicationDataDirectory, imagefolder);
            //device.FileSystem.WorkingDirectory = texturepath;

            // Check if we've got this texture on the file system.
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
                    return true;
                }

            }

            return false;
        }

        /// <summary>
        /// Bread and butter of the texture system.
        /// This is the start point for the texture-> graphics pipeline
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="vObj"></param>
        /// <param name="AssetID"></param>
        public void applyTexture(TextureExtended tex, VObject vObj, UUID AssetID)
        {
            try
            {
                // works
                if (vObj.mesh == null)
                    return;

                bool alphaimage = false;

                // Check if we've already run this through our image alpha checker
                if (tex.Userdata == null)
                {
                    // Check if this image has an alpha channel in use
                    // All textures are 32 Bit and alpha capable, so we have to scan it for an 
                    // alpha pixel
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
                    // Save result
                    tex.Userdata = (object)alphaimage;
                }
                else
                {
                    // Use cached result
                    alphaimage = (bool)tex.Userdata;
                }

                // Apply the Texture based on the TextureEntry
                if (vObj.prim.Textures != null)
                {
                    // Check the default texture to ensure that it's not null (why would it be null?)
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
                        
                        // The mesh buffers correspond to the faces defined in the textureentry

                        int mbcount = vObj.mesh.MeshBufferCount;
                        for (int j = 0; j < mbcount; j++)
                        {
                            // Only apply default texture if there isn't one already!
                            // we don't want to overwrite a face specific texture with the default
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


                            if (vObj.mesh.MeshBufferCount > i)
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

                                // Apply texture only if this face has it linked
                                if (teface.TextureID == AssetID)
                                {
                                    ApplyFaceSettings(vObj, alphaimage, teface, tex, i, shinyval, coldata);
                                }
                                else
                                {
                                    // Only apply the color settings..
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
                    
                    // Swap out the visible untextured object with a textured one.
                    SceneNode sn = device.SceneManager.AddMeshSceneNode(vObj.mesh, null, -1);
                    sn.Position = vObj.node.Position;
                    sn.Rotation = vObj.node.Rotation;
                    sn.Scale = vObj.node.Scale;
                    sn.DebugDataVisible = DebugSceneType.Off;

                    // If it's translucent, register it for the Transparent phase of rendering
                    if (vObj.prim.Textures.DefaultTexture.RGBA.A != 1)
                    {
                        device.SceneManager.RegisterNodeForRendering(sn, SceneNodeRenderPass.Transparent);
                    }

                    // Delete the old triangle selector
                    triPicker.RemTriangleSelector(sn.TriangleSelector);

                    // Add the new one
                    sn.TriangleSelector = device.SceneManager.CreateTriangleSelector(vObj.mesh, sn);
                    triPicker.AddTriangleSelector(sn.TriangleSelector, sn);
                    
                    // Delete the old node
                    SceneNode oldnode = vObj.node;
                    vObj.node = sn;
                    if (oldnode.TriangleSelector != null)
                        mts.RemoveTriangleSelector(oldnode.TriangleSelector);

                    device.SceneManager.AddToDeletionQueue(oldnode);
                } // prim texture is not null



            }
            catch (AccessViolationException)
            {
                m_log.Error("[TEXTURE]: Failed to load texture.");
            }
            catch (NullReferenceException)
            {
                m_log.Error("unable to update texture");
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

        /// <summary>
        /// Applies individual face settings
        /// </summary>
        /// <param name="vObj"></param>
        /// <param name="alpha">Is this an alpha texture</param>
        /// <param name="teface">Texture Entry Face</param>
        /// <param name="tex">Texture to apply</param>
        /// <param name="face">Which face this is</param>
        /// <param name="shinyval">The selected shiny value</param>
        /// <param name="coldata">The modified Color settings</param>
        public void ApplyFaceSettings(VObject vObj, bool alpha, Primitive.TextureEntryFace teface, Texture tex, int face, 
            float shinyval, Color4 coldata)
        {
            // Apply texture
            if (tex != null)
            {
                vObj.mesh.GetMeshBuffer(face).Material.Texture1 = tex;
            }

            // Apply colors/transforms
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
            MeshBuffer mb = mesh.GetMeshBuffer(j);
            //if (coldata != Color4.White)
            //{
            //mb.Material.SpecularColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 50, 0, 255));
            //mb.Material.AmbientColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 50, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 50, 0, 255));
            //mb.Material.EmissiveColor = new Color((int)(coldata.A * 255), Util.Clamp<int>((int)(coldata.R * 255) - 5, 0, 255), Util.Clamp<int>((int)(coldata.G * 255) - 5, 0, 255), Util.Clamp<int>((int)(coldata.B * 255) - 5, 0, 255));
                //vObj.mesh.GetMeshBuffer(j).Material.DiffuseColor = new Color((int)(coldata.A * 255), (int)(coldata.R * 255), (int)(coldata.B * 255), (int)(coldata.G * 255));
            //}
            //mb.Material.MaterialType = MaterialType.Solid;
            mb.Material.Shininess = shinyval;

            //if (teface.Fullbright)
            //{
            
           // }

            // If it's an alpha texture, ensure Irrlicht knows or you get artifacts.
            if (alpha)
            {
                mb.Material.MaterialType = MaterialType.TransparentAlphaChannelRef;
            }

            // Create texture transform based on the UV transforms specified in the texture entry
            IrrlichtNETCP.Matrix4 mat = mb.Material.Layer1.TextureMatrix;

            mat = IrrlichtNETCP.Matrix4.buildTextureTransform(teface.Rotation, new Vector2D(-0.5f, -0.5f * teface.RepeatV), new Vector2D(0.5f + teface.OffsetU, -(0.5f + teface.OffsetV)), new Vector2D(teface.RepeatU, teface.RepeatV));
            //m_log.WarnFormat("[TEXREPEAT]: <{0},{1}>", teface.RepeatU, teface.RepeatV);
            mb.Material.Layer1.TextureMatrix = mat;
            
            //mesh = device.SceneManager.MeshManipulator.CreateMeshWithTangents(mesh);



            mb.Material.ZWriteEnable = true;
            //mb.Material.ZBuffer = 1;
            mb.Material.BackfaceCulling = true;
            
            if (coldata.A != 1)
            {
                coldata.R *= coldata.A;
                coldata.B *= coldata.A;
                coldata.G *= coldata.A;
            }

            // Set the color and alpha
            uint vcount = (uint)mb.VertexCount;
            for (uint j2 = 0; j2 < vcount; j2++)
            {
                
                mb.GetVertex(j2).Color = new Color(Util.Clamp<int>((int)(coldata.A * 255),0,255), Util.Clamp<int>((int)(coldata.R * 255), 0, 255), Util.Clamp<int>((int)(coldata.G * 255), 0, 255), Util.Clamp<int>((int)(coldata.B * 255), 0, 255));
                
                //m_log.Debug(coldata.A.ToString());
            }   

            // If it's partially translucent inform Irrlicht
            if (coldata.A != 1)
            {
                mb.Material.MaterialType = MaterialType.TransparentAddColor;
                mb.Material.Lighting = false;
                //mb.Material.Lighting = !teface.Fullbright;
            }
            else
            {
                // Full bright means no lighting
                mb.Material.Lighting = !teface.Fullbright;
            }
                //mb.Material.MaterialTypeParam = (float)MaterialType.TransparentAddColor;
           // }
        }

        // LibOMV callback for completed image texture.
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
                
                // Write it to disk for picking up later in the pipeline.
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

                
                // Update nodes that the texture is downloaded.
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
