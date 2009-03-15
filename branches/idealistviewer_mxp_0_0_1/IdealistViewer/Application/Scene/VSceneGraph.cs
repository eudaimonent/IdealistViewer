using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using log4net;
using System.Reflection;
using OpenMetaverse;
using IrrlichtNETCP.Extensions;

namespace IdealistViewer
{
    public class VSceneGraph
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Reference to the viewer instance.
        /// </summary>
        private Viewer m_viewer;

        /// <summary>
        /// The water in the scene.
        /// </summary>
        public WaterSceneNode WaterNode;
        public VObject m_avatarObject;

        public uint PrimitiveCount = 0;
        public uint FoliageCount = 0;

        /// <summary>
        /// All object modifications run through this Queue
        /// </summary>
        public Queue<VObject> ObjectModifications = new Queue<VObject>();
        /// <summary>
        /// Child prim in a prim group where the children don't yet have parents 
        /// rendered get put in here to wait for the parent
        /// </summary>
        public Queue<VObject> ParentWaitingObjects = new Queue<VObject>();
        /// <summary>
        /// All avatar modifications run through this queue
        /// </summary>
        public Queue<VObject> AvatarModidifications = new Queue<VObject>();
        /// <summary>
        /// All Meshing gets queued up int this queue.
        /// </summary>
        public Queue<VObject> ObjectMeshModifications = new Queue<VObject>();
        /// <summary>
        /// foliage (trees, grass, etc. are queued in this queue.
        /// </summary>
        public Queue<VFoliage> FoliageMeshModifications = new Queue<VFoliage>();
        /// <summary>
        /// The texture has completed downloading, put it into this queue for assigning to linked objects
        /// </summary>
        public Queue<TextureObjectPair> AssignReadyTextures = new Queue<TextureObjectPair>();

        /// <summary>
        /// Simulator that the client think's it's currently a root agent in.
        /// Uses this to determine the offset of prim and objects in neighbor regions
        /// </summary>
        public VSimulator CurrentSimulator;
        /// <summary>
        /// Known Simulators, Indexed by ulong regionhandle
        /// </summary>
        public Dictionary<ulong, VSimulator> Simulators = new Dictionary<ulong, VSimulator>();
        /// <summary>
        /// Known Entities.  Indexed by VUtil.GetHashId
        /// </summary>
        public Dictionary<string, VObject> Objects = new Dictionary<string, VObject>();
        /// <summary>
        /// All objects that are interpolated get put into this dictionary.  Indexed by VUtil.GetHashId
        /// </summary>
        public Dictionary<string, VObject> MovingObjects = new Dictionary<string, VObject>();
        /// <summary>
        /// Known Avatars Indexed by Avatar UUID
        /// </summary>
        public Dictionary<UUID, VObject> Avatars = new Dictionary<UUID, VObject>();

        /// <summary>
        /// Terrain Triangle Selectors indexed by ulong regionhandle.  Used for the Picker
        /// </summary>
        public Dictionary<ulong, TriangleSelector> TerrainTriangleSelectors = new Dictionary<ulong, TriangleSelector>();
        /// <summary>
        /// Combines triangle selectors for all of the objects in the scene.  A reference to the triangles
        /// </summary>
        public MetaTriangleSelector TriangleSelector;
        /// <summary>
        /// Picker
        /// </summary>
        public TrianglePickerMapper TrianglePicker;

        /// <summary>
        /// Use this to ensure that meshing occurs one at a time.
        /// </summary>
        public static Object MeshingLock = new Object();


        public VSceneGraph(Viewer viewer)
        {
            m_viewer = viewer;
        }

        /// <summary>
        /// Enqueues an object for processing.  This is the beginning of the object pipeline.
        /// </summary>
        /// <param name="newObject"></param>
        public void AddObject(VObject newObject)
        {

            if (newObject.Mesh != null)
            {
                lock (Objects)
                {
                    if (!Objects.ContainsKey(VObjectUtil.GetHashId(newObject)))
                    {
                        Objects.Add(VObjectUtil.GetHashId(newObject), newObject);
                    }
                    else
                    {
                        // Full object update
                        //m_log.Warn("[NEWPRIM]   ");
                    }
                }

                lock (ObjectModifications)
                {
                    ObjectModifications.Enqueue(newObject);
                }
            }
        }

        /// <summary>
        /// After the prim are meshed, here to be placed in the scene.  Linked object textures are requested
        /// </summary>
        /// <param name="count"></param>
        public void ProcessObjectModifications(int count, ref Queue<VObject> modQueue)
        {
            //for (int i = 0; i < pObjects; i++)
            //int numObjectsOutOfRange = 0;
            while (count-- > 0)
            {
                VObject vObj = null;
                //lock (objectModQueue)
                //{
                //    if (objectModQueue.Count == 0)
                //        break;
                //    vObj = objectModQueue.Dequeue();

                //    // this commented code was an attempt at distance based culling - needs more work as it fails to rez all prims
                //    // as they come into range - ok to delete, especially if you implement a full solution ;)
                //    //
                //    //if (UserAvatar != null && vObj != null && UserAvatar.prim != null && vObj.prim != null)
                //    //    if (Vector3.Distance(UserAvatar.prim.Position, vObj.prim.Position) > 50.0f)
                //    //    {
                //    //        numObjectsOutOfRange++;
                //    //        objectModQueue.Enqueue(vObj);
                //    //        if (objectModQueue.Count - numObjectsOutOfRange > pObjects + 1)
                //    //            pObjects++;
                //    //        continue;
                //    //    }
                //}

                lock (modQueue)
                {
                    if (modQueue.Count == 0)
                        break;
                    vObj = modQueue.Dequeue();
                }

                Primitive prim = vObj.Primitive;
                if (prim != null)
                {

                    ulong simhandle = prim.RegionHandle;

                    if (simhandle == 0)
                        simhandle = m_viewer.TestNeighbor;

                    Vector3 WorldoffsetPos = Vector3.Zero;

                    if (CurrentSimulator != null)
                    {
                        if (simhandle != CurrentSimulator.Handle)
                        {
                            Vector3 gposr = Util.OffsetGobal(simhandle, Vector3.Zero);
                            Vector3 gposc = Util.OffsetGobal(CurrentSimulator.Handle, Vector3.Zero);

                            WorldoffsetPos = gposr - gposc;
                        }
                    }

                    VObject parentObj = null;
                    SceneNode parentNode = m_viewer.Renderer.SceneManager.RootSceneNode;
                    //VObject vObj = UnAssignedChildObjectModQueue.Dequeue();
                    //if (Entities.ContainsKey(prim.RegionHandle.ToString() + prim.ParentID.ToString()))
                    //{

                    if (prim.ParentID != 0)
                    {
#if DebugObjectPipeline
                        m_log.DebugFormat("[OBJ]: NonRootPrim ID: {0}", prim.ID);
#endif
                        lock (Objects)
                        {
                            if (Objects.ContainsKey(simhandle.ToString() + prim.ParentID.ToString()))
                            {
                                parentObj = Objects[simhandle.ToString() + prim.ParentID.ToString()];
                                if (parentObj.SceneNode != null)
                                {
                                    //parentNode = parentObj.node;
                                    //pscalex = parentObj.prim.Scale.X;
                                    //pscaley = parentObj.prim.Scale.Y;
                                    //pscalez = parentObj.prim.Scale.Z;
                                }
                                else
                                {
#if DebugObjectPipeline
                                    m_log.DebugFormat("[OBJ]: No Parent Yet for ID: {0}", prim.ID);
#endif
                                    // No parent yet...    Stick it in the child prim wait queue.
                                    lock (ParentWaitingObjects)
                                    {
                                        ParentWaitingObjects.Enqueue(vObj);
                                    }
                                    continue;
                                }

                            }
                        }

                    }
                    //}
                    bool creatednode = false;
                    #region Avatar

                    SceneNode node = null;
                    if (prim is Avatar)
                    {
#if DebugObjectPipeline
                        m_log.DebugFormat("[OBJ]: Avatar ID: {0}", prim.ID);
#endif
                        // Little known fact.  Dead avatar in LibOMV have the word 'dead' in their UUID
                        // Skip over this one and move on to the next one if it's dead.
                        if (((Avatar)prim).ID.ToString().Contains("dead"))
                            continue;

                        // If we don't have an avatar representation yet for this avatar or it's a full update
                        if (vObj.SceneNode == null && vObj.FullUpdate)
                        {
#if DebugObjectPipeline
                            m_log.DebugFormat("[OBJ]: Created Avatar ID: {0}", prim.ID);
#endif
                            AnimatedMesh avmesh = m_viewer.Renderer.SceneManager.GetMesh(m_viewer.AvatarMesh);

                            bool isTextured = false;
                            int numTextures = 0;
                            int mbcount = avmesh.GetMesh(0).MeshBufferCount;
                            for (int j = 0; j < mbcount; j++)
                            {
                                Texture texDriver = m_viewer.Renderer.Driver.GetTexture(j.ToString() + "-" + m_viewer.AvatarMaterial);
                                numTextures += texDriver == null ? 0 : 1;
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.Texture1 = texDriver;
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.SpecularColor = new Color(255, 128, 128, 128);
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.AmbientColor = new Color(255, 128, 128, 128);
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.EmissiveColor = new Color(255, 128, 128, 128);
                                avmesh.GetMesh(0).GetMeshBuffer(j).Material.Shininess = 0;
                            }
                            if (numTextures == mbcount)
                                isTextured = true;

                            lock (Objects)
                            {
                                vObj.SceneNode = m_viewer.Renderer.SceneManager.AddAnimatedMeshSceneNode(avmesh);
                                node = vObj.SceneNode;
                            }

                            ((AnimatedMeshSceneNode)node).AnimationSpeed = m_viewer.AnimationManager.AnimationFramesPerSecond;

                            // TODO: FIXME - this depends on the mesh being loaded. A good candidate for a config item.
                            node.Scale = new Vector3D(0.035f, 0.035f, 0.035f);
                            //node.Scale = new Vector3D(15f, 15f, 15f);

                            if (!isTextured)
                                node.SetMaterialTexture(0, m_viewer.Renderer.Driver.GetTexture(m_viewer.AvatarMaterial));

                            // Light avatar
                            node.SetMaterialFlag(MaterialFlag.Lighting, true);

#if DebugObjectPipeline
                            m_log.DebugFormat("[OBJ]: Added Interpolation Target for Avatar ID: {0}", prim.ID);
#endif
                            // Add to Interpolation targets
                            lock (MovingObjects)
                            {
                                if (MovingObjects.ContainsKey(simhandle.ToString() + prim.LocalID.ToString()))
                                {
                                    MovingObjects[simhandle.ToString() + prim.LocalID.ToString()] = vObj;
                                }
                                else
                                {
                                    MovingObjects.Add(simhandle.ToString() + prim.LocalID.ToString(), vObj);
                                }
                            }

                            // Is this an update about us?
                            if (prim.ID == m_viewer.NetworkInterface.GetSelfUUID)
                            {
                                if (m_avatarObject == null)
                                {
                                    m_viewer.SetViewerAvatar(vObj);
                                }

                            }

                            // Display the avatar's name over their head.
                            SceneNode trans = m_viewer.Renderer.SceneManager.AddEmptySceneNode(node, -1);
                            node.AddChild(trans);
                            trans.Position = new Vector3D(0, 50, 0);

                            SceneNode trans2 = m_viewer.Renderer.SceneManager.AddEmptySceneNode(node, -1);
                            node.AddChild(trans2);
                            trans2.Position = new Vector3D(0.0f, 49.5f, 0.5f);

                            m_viewer.Renderer.SceneManager.AddTextSceneNode(m_viewer.Renderer.GuiEnvironment.BuiltInFont, ((Avatar)prim).Name, new Color(255, 255, 255, 255), trans);
                            m_viewer.Renderer.SceneManager.AddTextSceneNode(m_viewer.Renderer.GuiEnvironment.BuiltInFont, ((Avatar)prim).Name, new Color(255, 0, 0, 0), trans2);

                            //node
                        }
                        else
                        {
#if DebugObjectPipeline
                            m_log.DebugFormat("[OBJ]: update for existing avatar ID: {0}", prim.ID);
#endif
                            // Set the current working node to the already existing node.
                            node = vObj.SceneNode;
                        }

                    }
                    #endregion
                    else
                    {
#if DebugObjectPipeline
                        m_log.DebugFormat("[OBJ]: Update for Prim ID: {0}", prim.ID);
#endif
                        // No mesh yet, skip over it.
                        if (vObj.Mesh == null)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: No Mesh for Prim ID: {0}.  This prim won't be displayed", prim.ID);
#endif
                            continue;
                        }

                        // Full Update
                        if (vObj.FullUpdate)
                        {
                            // Check if it's a sculptie and we've got it's texture.
                            //if (prim.Sculpt.SculptTexture != UUID.Zero)
                            //    m_log.Warn("[SCULPT]: Sending sculpt to the scene....");

                            //Vertex3D vtest = vObj.mesh.GetMeshBuffer(0).GetVertex(0);
                            //System.Console.WriteLine(" X:" + vtest.Position.X + " Y:" + vtest.Position.Y + " Z:" + vtest.Position.Z);
                            node = m_viewer.Renderer.SceneManager.AddMeshSceneNode(vObj.Mesh, parentNode, (int)prim.LocalID);

                            creatednode = true;
                            vObj.SceneNode = node;
                        }
                        else
                        {
                            // Set the working node to the pre-existing node for this object
                            node = vObj.SceneNode;
                        }

#if DebugObjectPipeline
                        m_log.DebugFormat("[OBJ]: Update Data Prim ID: {0}, FULL:{1}, CREATED:{2}", prim.ID, vObj.updateFullYN ,creatednode);
#endif

                        if (node == null)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: Node was Null for Prim ID: {0}.  This prim won't be displayed", prim.ID);
#endif
                            continue;
                        }
                    }

                    if (node == null && prim is Avatar)
                    {
                        // why would node = null?  Race Condition?
                        continue;
                    }

                    if (prim is Avatar)
                    {
                        // TODO: FIXME - This is dependant on the avatar mesh loaded. a good candidate for a config option.
                        //prim.Position.Z -= 0.2f;
                        //if (prim.Position.Z >= 0)
                        //if (RegionHFArray[(int)(Util.Clamp<float>(prim.Position.Y, 0, 255)), (int)(Util.Clamp<float>(prim.Position.X, 0, 255))] + 2.5f >= prim.Position.Z)
                        //{
                        //prim.Position.Z = RegionHFArray[(int)(Util.Clamp<float>(prim.Position.Y, 0, 255)), (int)(Util.Clamp<float>(prim.Position.X, 0, 255))] + 0.9f;
                        //}

                    }
                    else
                    {
                        // Set the scale of the prim to the libomv reported scale.
                        node.Scale = new Vector3D(prim.Scale.X, prim.Scale.Z, prim.Scale.Y);
                    }

                    // m_log.WarnFormat("[SCALE]: <{0},{1},{2}> = <{3},{4},{5}>", prim.Scale.X, prim.Scale.Z, prim.Scale.Y, pscalex, pscaley, pscalez);

                    // If this prim is either the parent prim or an individual prim
                    if (prim.ParentID == 0)
                    {
                        if (prim is Avatar)
                        {
                            //m_log.WarnFormat("[AVATAR]: W:<{0},{1},{2}> R:<{3},{4},{5}>",WorldoffsetPos.X,WorldoffsetPos.Y,WorldoffsetPos.Z,prim.Position.X,prim.Position.Y,prim.Position.Z);
                            WorldoffsetPos = Vector3.Zero;
                            // The world offset for avatar doesn't work for some reason yet in LibOMV.  
                            // It's offset, so don't offset them by their world position yet.
                            //vObj.position = new Vector3(prim.Position.X, prim.Position.Y, prim.Position.Z);

                        }

                        try
                        {
                            if (node.Raw == IntPtr.Zero)
                                continue;
                            // Offset the node by it's world position
                            if (prim is Avatar)
                                continue;

                            node.Position = new Vector3D(WorldoffsetPos.X + prim.Position.X, WorldoffsetPos.Z + prim.Position.Z, WorldoffsetPos.Y + prim.Position.Y);

                        }
                        catch (System.Runtime.InteropServices.SEHException)
                        {
                            continue;
                        }
                        catch (AccessViolationException)
                        {
                            continue;
                        }

                    }
                    else
                    {
                        // Check if the node died
                        if (node.Raw == IntPtr.Zero)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: Prim ID: {0} Missing Node, IntPtr.Zero", prim.ID);
#endif
                            continue;
                        }
                        if (vObj == null || parentObj == null)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: Prim ID: {0} Missing Node, vObj == null || parentVObj == null", prim.ID);
#endif
                            continue;
                        }
                        if (prim == null || parentObj.Primitive == null)
                        {
#if DebugObjectPipeline
                            m_log.WarnFormat("[OBJ]: Prim ID: {0} Missing prim, prim == null || parentObj.prim == null", prim.ID);
#endif
                            continue;
                        }

                        // apply rotation and position reported form LibOMV
                        prim.Position = prim.Position * parentObj.Primitive.Rotation;
                        prim.Rotation = parentObj.Primitive.Rotation * prim.Rotation;

                        node.Position = new Vector3D(WorldoffsetPos.X + parentObj.Primitive.Position.X + prim.Position.X, WorldoffsetPos.Z + parentObj.Primitive.Position.Z + prim.Position.Z, WorldoffsetPos.Y + parentObj.Primitive.Position.Y + prim.Position.Y);
                    }

                    if (vObj.FullUpdate)
                    {
                        // If the prim is physical, add it to the interpolation targets.
                        if ((prim.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                        {
                            lock (MovingObjects)
                            {
                                if (!MovingObjects.ContainsKey(simhandle.ToString() + prim.LocalID.ToString()))
                                    MovingObjects.Add(simhandle.ToString() + prim.LocalID.ToString(), vObj);
                            }
                        }
                        else
                        {
                            lock (MovingObjects)
                            {
                                if (!(prim is Avatar))
                                    if (MovingObjects.ContainsKey(simhandle.ToString() + prim.LocalID.ToString()))
                                        MovingObjects.Remove(simhandle.ToString() + prim.LocalID.ToString());
                            }
                        }

                    }

                    if (node.Raw == IntPtr.Zero)
                    {
#if DebugObjectPipeline
                        m_log.WarnFormat("[OBJ]: Prim ID: {0} Node IntPtr.Zero, node.Raw == IntPtr.Zero", prim.ID);
#endif
                        continue;
                    }

                    bool ApplyRotationYN = true;

                    if (prim is Avatar)
                    {
                        if (m_avatarObject != null && m_avatarObject.Primitive != null && m_viewer.AvatarController != null)
                        {
                            if (prim.ID == m_avatarObject.Primitive.ID)
                            {
                                // If this is our avatar and the update came less then 5 seconds 
                                // after we last rotated, it'll just confuse the user
                                if (System.Environment.TickCount - m_viewer.AvatarController.m_userRotated < 5000)
                                {
                                    ApplyRotationYN = false;
                                }
                            }
                        }
                    }

                    //m_log.Warn(prim.Rotation.ToString());
                    if (ApplyRotationYN)
                    {
                        // Convert Cordinate space
                        IrrlichtNETCP.Quaternion iqu = new IrrlichtNETCP.Quaternion(prim.Rotation.X, prim.Rotation.Z, prim.Rotation.Y, prim.Rotation.W);

                        iqu.makeInverse();

                        IrrlichtNETCP.Quaternion finalpos = iqu;

                        finalpos = m_viewer.Renderer.CoordinateConversion_XYZ_XZY * finalpos;
                        node.Rotation = finalpos.Matrix.RotationDegrees;
                    }


                    if (creatednode)
                    {
                        // If we created this node, then we need to add it to the 
                        // picker targets and request it's textures

                        TriangleSelector trisel = m_viewer.Renderer.SceneManager.CreateTriangleSelector(vObj.Mesh, node);
                        node.TriangleSelector = trisel;
                        TrianglePicker.AddTriangleSelector(trisel, node);
                        if (TriangleSelector != null)
                        {
                            lock (TriangleSelector)
                            {
                                TriangleSelector.AddTriangleSelector(trisel);
                            }
                        }
                        if (prim.Textures != null)
                        {
                            if (prim.Textures.DefaultTexture != null)
                            {
                                if (prim.Textures.DefaultTexture.TextureID != UUID.Zero)
                                {
                                    UUID textureID = prim.Textures.DefaultTexture.TextureID;

                                    // Only request texture if texture downloading is enabled.
                                    if (m_viewer.TextureManager != null)
                                        m_viewer.TextureManager.RequestImage(textureID, vObj);

                                }
                            }

                            // If we have individual face texture settings
                            if (prim.Textures.FaceTextures != null)
                            {

                                Primitive.TextureEntryFace[] objfaces = prim.Textures.FaceTextures;
                                for (int i2 = 0; i2 < objfaces.Length; i2++)
                                {
                                    if (objfaces[i2] == null)
                                        continue;

                                    UUID textureID = objfaces[i2].TextureID;

                                    if (textureID != UUID.Zero)
                                    {
                                        if (m_viewer.TextureManager != null)
                                            m_viewer.TextureManager.RequestImage(textureID, vObj);
                                    }
                                }
                            }
                        }
                    }
                    // Check for dead nodes
                    if (node.Raw == IntPtr.Zero)
                        continue;
                    node.UpdateAbsolutePosition();

                }
            }
        }

        /// <summary>
        /// This is mostly a duplication of doObjectMods.  A good candidate for abstraction
        /// Acts on child objects
        /// </summary>
        /// <param name="count"></param>
        public void ProcessParentWaitingObjects(int count)
        {
            if (ParentWaitingObjects.Count < count)
                count = ParentWaitingObjects.Count;

            for (int i = 0; i < count; i++)
            {
                VObject vObj = null;
                Vector3 WorldoffsetPos = Vector3.Zero;
                lock (ParentWaitingObjects)
                {
                    if (ParentWaitingObjects.Count == 0)
                        break;



                    vObj = ParentWaitingObjects.Dequeue();
                }

                Primitive prim = vObj.Primitive;

                ulong simhandle = prim.RegionHandle;

                if (simhandle == 0)
                    simhandle = m_viewer.TestNeighbor;

                if (Objects.ContainsKey(simhandle.ToString() + prim.ParentID.ToString()))
                {
                    VObject parentObj = Objects[simhandle.ToString() + prim.ParentID.ToString()];

                    if (parentObj.SceneNode != null)
                    {
                        if (CurrentSimulator != null)
                        {
                            if (simhandle != CurrentSimulator.Handle)
                            {
                                Vector3 gposr = Util.OffsetGobal(simhandle, Vector3.Zero);
                                Vector3 gposc = Util.OffsetGobal(CurrentSimulator.Handle, Vector3.Zero);
                                WorldoffsetPos = gposr - gposc;
                            }
                        }
                        bool creatednode = false;

                        SceneNode node = null;
                        if (vObj.SceneNode == null)
                        {
                            if (prim is Avatar)
                            {

                                //AnimatedMesh avmesh = smgr.GetMesh("sydney.md2");
                                AnimatedMesh avmesh = m_viewer.Renderer.SceneManager.GetMesh(m_viewer.AvatarMesh);

                                AnimatedMeshSceneNode node2 = m_viewer.Renderer.SceneManager.AddAnimatedMeshSceneNode(avmesh);
                                node = node2;
                                node.Scale = new Vector3D(0.035f, 0.035f, 0.035f);
                                vObj.SceneNode = node2;

                                lock (MovingObjects)
                                {
                                    if (MovingObjects.ContainsKey(simhandle.ToString() + prim.LocalID.ToString()))
                                    {
                                        MovingObjects[simhandle.ToString() + prim.LocalID.ToString()] = vObj;
                                    }
                                    else
                                    {
                                        MovingObjects.Add(simhandle.ToString() + prim.LocalID.ToString(), vObj);
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    node = m_viewer.Renderer.SceneManager.AddMeshSceneNode(vObj.Mesh, m_viewer.Renderer.SceneManager.RootSceneNode, (int)prim.LocalID);
                                    creatednode = true;
                                    vObj.SceneNode = node;
                                }
                                catch (AccessViolationException)
                                {

                                    continue;
                                }
                            }
                        }
                        else
                        {
                            node = vObj.SceneNode;
                        }

                        //parentObj.node.AddChild(node);
                        node.Scale = new Vector3D(prim.Scale.X, prim.Scale.Z, prim.Scale.Y);

                        //m_log.WarnFormat("[SCALE]: <{0},{1},{2}> = <{3},{4},{5}>", prim.Scale.X, prim.Scale.Z, prim.Scale.Y, parentObj.node.Scale.X, parentObj.node.Scale.Y, parentObj.node.Scale.Z);

                        prim.Position = prim.Position * parentObj.Primitive.Rotation;
                        prim.Rotation = parentObj.Primitive.Rotation * prim.Rotation;

                        node.Position = new Vector3D(WorldoffsetPos.X + parentObj.Primitive.Position.X + prim.Position.X, WorldoffsetPos.Z + parentObj.Primitive.Position.Z + prim.Position.Z, WorldoffsetPos.Y + parentObj.Primitive.Position.Y + prim.Position.Y);

                        //m_log.Warn(prim.Rotation.ToString());
                        IrrlichtNETCP.Quaternion iqu = new IrrlichtNETCP.Quaternion(prim.Rotation.X, prim.Rotation.Z, prim.Rotation.Y, prim.Rotation.W);
                        iqu.makeInverse();

                        //IrrlichtNETCP.Quaternion parentrot = new IrrlichtNETCP.Quaternion(parentObj.node.Rotation.X, parentObj.node.Rotation.Y, parentObj.node.Rotation.Z);
                        //parentrot.makeInverse();

                        //parentrot = Cordinate_XYZ_XZY * parentrot;

                        IrrlichtNETCP.Quaternion finalpos = iqu;
                        //IrrlichtNETCP.Quaternion finalpos = parentrot * iqu;

                        finalpos = m_viewer.Renderer.CoordinateConversion_XYZ_XZY * finalpos;

                        node.Rotation = finalpos.Matrix.RotationDegrees;

                        if (creatednode)
                        {
                            //node.SetMaterialFlag(MaterialFlag.NormalizeNormals, true);
                            //node.SetMaterialFlag(MaterialFlag.BackFaceCulling, backFaceCulling);
                            //node.SetMaterialFlag(MaterialFlag.GouraudShading, true);
                            //node.SetMaterialFlag(MaterialFlag.Lighting, true);
                            //node.SetMaterialTexture(0, driver.GetTexture("red_stained_wood.tga"));


                            TriangleSelector trisel = m_viewer.Renderer.SceneManager.CreateTriangleSelector(vObj.Mesh, node);
                            node.TriangleSelector = trisel;
                            TrianglePicker.AddTriangleSelector(trisel, node);
                            if (TriangleSelector != null)
                            {
                                lock (TriangleSelector)
                                {
                                    TriangleSelector.AddTriangleSelector(trisel);
                                }
                            }
                            if (prim.Textures != null)
                            {
                                if (prim.Textures.DefaultTexture != null)
                                {
                                    if (prim.Textures.DefaultTexture.TextureID != UUID.Zero)
                                    {
                                        UUID textureID = prim.Textures.DefaultTexture.TextureID;
                                        if (m_viewer.TextureManager != null)
                                            m_viewer.TextureManager.RequestImage(textureID, vObj);

                                    }
                                }
                                if (prim.Textures.FaceTextures != null)
                                {
                                    Primitive.TextureEntryFace[] objfaces = prim.Textures.FaceTextures;
                                    for (int i2 = 0; i2 < objfaces.Length; i2++)
                                    {
                                        if (objfaces[i2] == null)
                                            continue;
                                        UUID textureID = objfaces[i2].TextureID;

                                        if (textureID != UUID.Zero)
                                        {
                                            if (m_viewer.TextureManager != null)
                                                m_viewer.TextureManager.RequestImage(textureID, vObj);
                                        }
                                    }
                                }
                            }
                        }

                        node.UpdateAbsolutePosition();
                    }
                    else
                    {
                        m_log.Warn("[CHILDOBJ]: Found Parent Object but it doesn't have a SceneNode, Skipping");
                        lock (ParentWaitingObjects)
                        {
                            ParentWaitingObjects.Enqueue(vObj);
                        }
                    }
                }
                else
                {
                    lock (ParentWaitingObjects)
                    {
                        ParentWaitingObjects.Enqueue(vObj);
                    }
                }
            }
        }

        /// <summary>
        /// Mesh pObjects count prim in the ObjectMeshQueue
        /// </summary>
        /// <param name="count">number of prim to mesh this time around</param>
        public void ProcessMeshModifications(int count)
        {
            bool sculptYN = false;
            TextureExtended sculpttex = null;

            for (int i = 0; i < count; i++)
            {
                sculptYN = false;
                VObject vobj = null;
                lock (ObjectMeshModifications)
                {
                    if (ObjectMeshModifications.Count == 0)
                        break;
                    vobj = ObjectMeshModifications.Dequeue();
                }
                Primitive prim = vobj.Primitive;
                Primitive.SculptData sculpt = null;
                if (m_viewer.TextureManager != null)
                {
                    if ((sculpt = prim.Sculpt) != null)
                    {
                        if (sculpt.SculptTexture != UUID.Zero)
                        {
                            m_log.Warn("[SCULPT]: Got Sculpt");
                            if (!m_viewer.TextureManager.tryGetTexture(sculpt.SculptTexture, out sculpttex))
                            {
                                m_log.Warn("[SCULPT]: Didn't have texture, requesting it");
                                m_viewer.TextureManager.RequestImage(sculpt.SculptTexture, vobj);
                                //Sculpt textures will cause the prim to get put back into the Mesh objects queue
                                // Skipping it for now.
                                continue;
                            }
                            else
                            {
                                m_log.Warn("[SCULPT]: have texture, setting sculpt to true");
                                sculptYN = true;
                            }
                        }
                    }
                }
                else
                {
                    sculptYN = false;
                }

                if (sculptYN == false || sculpttex == null)
                {
                    // Mesh a regular prim.
                    vobj.Mesh = m_viewer.MeshManager.GetMeshInstance(prim);
                }
                else
                {
                    if (sculpt != null)
                    {
                        // Mesh a scupted prim.
                        m_log.Warn("[SCULPT]: Meshing Sculptie......");
                        vobj.Mesh = m_viewer.MeshManager.GetSculptMesh(sculpt.SculptTexture, sculpttex, sculpt.Type, prim);
                        m_log.Warn("[SCULPT]: Sculptie Meshed");
                    }

                }

                // Add the newly meshed object ot the objectModQueue
                ulong regionHandle = prim.RegionHandle;

                if (prim.ParentID != 0)
                {
                    bool foundEntity = false;

                    lock (Objects)
                    {
                        if (!Objects.ContainsKey(regionHandle.ToString() + prim.ParentID.ToString()))
                        {
                            lock (ParentWaitingObjects)
                            {
                                ParentWaitingObjects.Enqueue(vobj);
                            }
                        }
                        else
                        {
                            foundEntity = true;
                        }
                    }

                    if (foundEntity)
                    {
                        vobj.FullUpdate = true;
                        AddObject(vobj);
                    }
                }
                else
                {

                    vobj.FullUpdate = true;
                    AddObject(vobj);
                }
            }
        }

        /// <summary>
        /// Processes foliage mesh modications.
        /// </summary>
        /// <param name="max"></param>
        public void ProcessFoliageMeshModifications(int count)
        {
            if (CurrentSimulator == null)
                return;

            int i = 0;
            bool done = false;
            while (!done)
            {
                /*
                    // Pine 1 -0
                    // Oak 1 
                    // Tropical Bush 1 - 2
                    // Palm 1 -3
                    // Dogwood - 4    
                    // Tropical Bush 2 - 5
                    // Palm 2 - 6
                    // Cypress 1 - 7
                    // Cypress 2 - 8
                    // Pine 2 - 9
                    // Plumeria - 10
                    // Winter Pine 1 - 11
                    // Winter Aspen - 12
                    // Winter Pine 2 - 13
                    // Eucalyptus - 14
                    // Fern - 15
                    // Eelgrass - 16
                    // Sea Sword - 17
                    // Kelp 1 - 18
                    // Beach Grass 1 - 19
                    // Kelp 2 - 20
                 */

                if (FoliageMeshModifications.Count > 0)
                {
                    VFoliage foliage = FoliageMeshModifications.Dequeue();
                    Primitive prim = foliage.Primitive;
                    ulong handle = 0;
                    float scaleScalar = 0.1f;

                    if (CurrentSimulator != null)
                    {
                        handle = CurrentSimulator.Handle;
                    }
                    Vector3 globalPositionToRez = Util.OffsetGobal(prim.RegionHandle, Vector3.Zero);
                    Vector3 currentGlobalPosition = Util.OffsetGobal(handle, Vector3.Zero);
                    Vector3 worldOffsetPosition = globalPositionToRez - currentGlobalPosition;
                    Vector3 position = prim.Position;
                    Vector3 scale = prim.Scale;

                    SceneNode tree;

                    int type = foliage.Primitive.PrimData.State;

                    switch (type)
                    {
                        case 0: // Pine 1 -0
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("PineBark.png"), m_viewer.Renderer.Driver.GetTexture("PineLeaf.png"), m_viewer.Renderer.Driver.GetTexture("PineBillboard.png"));
                            break;
                        case 1: // Oak 1
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("OakBark.png"), m_viewer.Renderer.Driver.GetTexture("OakLeaf.png"), m_viewer.Renderer.Driver.GetTexture("OakBillboard.png"));
                            break;
                        case 2: // Tropical Bush 1 - 2
                            scaleScalar = 0.025f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("PineBark.png"), m_viewer.Renderer.Driver.GetTexture("PineLeaf.png"), m_viewer.Renderer.Driver.GetTexture("PineBillboard.png"));
                            break;
                        case 3: // Palm 1 -3
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("OakBark.png"), m_viewer.Renderer.Driver.GetTexture("OakLeaf.png"), m_viewer.Renderer.Driver.GetTexture("OakBillboard.png"));
                            break;
                        case 4: // Dogwood - 4
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("OakBark.png"), m_viewer.Renderer.Driver.GetTexture("OakLeaf.png"), m_viewer.Renderer.Driver.GetTexture("OakBillboard.png"));
                            break;
                        case 5: // Tropical Bush 2 - 5
                            scaleScalar = 0.025f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("OakBark.png"), m_viewer.Renderer.Driver.GetTexture("OakLeaf.png"), m_viewer.Renderer.Driver.GetTexture("OakBillboard.png"));
                            break;
                        case 6: // Palm 2 - 6
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("PineBark.png"), m_viewer.Renderer.Driver.GetTexture("PineLeaf.png"), m_viewer.Renderer.Driver.GetTexture("PineBillboard.png"));
                            break;
                        case 7: // Cypress 1 - 7
                        case 8: // Cypress 2 - 8
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("OakBark.png"), m_viewer.Renderer.Driver.GetTexture("OakLeaf.png"), m_viewer.Renderer.Driver.GetTexture("OakBillboard.png"));
                            break;
                        case 9: // Pine 2 - 9
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("PineBark.png"), m_viewer.Renderer.Driver.GetTexture("PineLeaf.png"), m_viewer.Renderer.Driver.GetTexture("PineBillboard.png"));
                            break;
                        case 10: // Plumeria - 10
                        case 11: // Winter Pine 1 - 11
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("PineBark.png"), m_viewer.Renderer.Driver.GetTexture("PineLeaf.png"), m_viewer.Renderer.Driver.GetTexture("PineBillboard.png"));
                            break;
                        case 12: // Winter Aspen - 12
                            scaleScalar = 0.01f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Oak.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("OakBark.png"), m_viewer.Renderer.Driver.GetTexture("OakLeaf.png"), m_viewer.Renderer.Driver.GetTexture("OakBillboard.png"));
                            break;
                        case 13: // Winter Pine 2 - 13
                            scaleScalar = 0.1f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("PineBark.png"), m_viewer.Renderer.Driver.GetTexture("PineLeaf.png"), m_viewer.Renderer.Driver.GetTexture("PineBillboard.png"));
                            break;
                        case 15: // Fern - 15
                        case 16: // Eelgrass - 16
                        case 17: // Sea Sword - 17
                        case 18: // Kelp 1 - 18
                        case 19: // Beach Grass 1 - 19
                        case 20: // Kelp 2 - 20
                        default:
                            scaleScalar = 0.01f;
                            tree = m_viewer.Renderer.SceneManager.AddTreeSceneNode("Pine.xml", null, -1, new Vector3D(position.X, position.Z, position.Y), new Vector3D(0, 0, 0), new Vector3D(scale.X, scale.Z, scale.Y), m_viewer.Renderer.Driver.GetTexture("PineBark.png"), m_viewer.Renderer.Driver.GetTexture("PineLeaf.png"), m_viewer.Renderer.Driver.GetTexture("PineBillboard.png"));
                            break;
                    }

                    if (tree == null)
                    {
                        m_log.Warn("[FOLIAGE]: Couldn't make tree, shaders not supported by hardware");
                    }

                    if (tree != null)
                    {



                        m_log.Debug("[FOLIAGE]: got foliage, location: " + prim.Position.ToString() + " type: " + type.ToString());

                        tree.Position = new Vector3D(position.X + worldOffsetPosition.X,
                            position.Z + worldOffsetPosition.Z,
                            position.Y + worldOffsetPosition.Y);
                        tree.Scale = new Vector3D(scale.X * scaleScalar, scale.Z * scaleScalar, scale.Y * scaleScalar);

                    }
                    else
                    {
                        m_log.Warn("[FOLIAGE]: Couldn't make tree, shaders not supported by hardware");
                    }
                }
                else
                    done = true;

                if (++i >= count)
                    done = true;
            }
        }

        /// <summary>
        /// Animations that are received are stored in a dictionary in the protocol module and associated
        /// with an avatar. They are removed from that dictionary here and applied to the proper avatars
        /// in the scene.
        /// </summary>
        public void ProcessAnimations()
        {
            lock (Avatars)
            {
                foreach (UUID avatarID in Avatars.Keys)
                {
                    VObject avobj = Avatars[avatarID];
                    if (avobj != null)
                    {
                        if (avobj.Primitive != null)
                            if (avobj.Primitive.ID.ToString().Contains("dead"))
                                continue;


                        if (avobj.Mesh != null)
                        {
                        }


                        if (avobj.SceneNode != null) // this is the scenenode for an animated mesh
                        {
                            List<UUID> newAnims = null;
                            lock (m_viewer.NetworkInterface.AvatarAnimations)
                            {
                                // fetch any pending animations from the dictionary and then
                                // delete them from the dictionary
                                if (m_viewer.NetworkInterface.AvatarAnimations.ContainsKey(avatarID))
                                {
                                    newAnims = m_viewer.NetworkInterface.AvatarAnimations[avatarID];
                                    m_viewer.NetworkInterface.AvatarAnimations.Remove(avatarID);
                                }
                            }
                            if (newAnims != null)
                            {
                                int startFrame = 0;
                                int endFrame = 40 * 4 - 1;
                                int animFramesPerSecond = m_viewer.AnimationManager.AnimationFramesPerSecond;
                                //MD2Animation md2Anim = MD2Animation.Stand;
                                foreach (UUID animID in newAnims)
                                {
                                    //m_log.Debug("[ANIMATION] - got animID: " + animID.ToString());

                                    if (animID == Animations.STAND
                                        || animID == Animations.STAND_1
                                        || animID == Animations.STAND_2
                                        || animID == Animations.STAND_3
                                        || animID == Animations.STAND_4)
                                    {
                                        m_log.Debug("[ANIMATION] - standing");
                                        //md2Anim = MD2Animation.Stand;
                                        startFrame = 0;
                                        endFrame = 40 * 4 - 1;

                                    }
                                    if (animID == Animations.CROUCHWALK)
                                    {
                                        m_log.Debug("[ANIMATION] - crouchwalk");
                                        //md2Anim = MD2Animation.CrouchWalk;
                                        // 154-159
                                        startFrame = 154 * 4;
                                        endFrame = 159 * 4 - 1;
                                    }
                                    if (animID == Animations.WALK
                                        || animID == Animations.FEMALE_WALK)
                                    {
                                        m_log.Debug("[ANIMATION] - walking");
                                        //md2Anim = MD2Animation.Run;
                                        startFrame = 160;
                                        endFrame = 183;
                                        animFramesPerSecond = 15;
                                    }
                                    if (animID == Animations.RUN)
                                    {
                                        m_log.Debug("[ANIMATION] - running");
                                        //md2Anim = MD2Animation.Run;
                                        startFrame = 160;
                                        endFrame = 183;
                                    }

                                    if (animID == Animations.SIT
                                        || animID == Animations.SIT_FEMALE
                                        || animID == Animations.SIT_GENERIC
                                        || animID == Animations.SIT_GROUND
                                        || animID == Animations.SIT_GROUND_staticRAINED
                                        || animID == Animations.SIT_TO_STAND)
                                    {
                                        m_log.Debug("[ANIMATION] - sitting");
                                        //md2Anim = MD2Animation.Pain3;
                                        startFrame = 169 * 4;
                                        endFrame = 169 * 4;
                                    }
                                    if (animID == Animations.FLY
                                        || animID == Animations.FLYSLOW)
                                    {
                                        m_log.Debug("[ANIMATION] - flying");
                                        //md2Anim = MD2Animation.Jump;
                                        startFrame = 195 * 4;
                                        endFrame = 197 * 4 - 1;
                                        animFramesPerSecond = 7;
                                    }
                                    if (animID == Animations.HOVER
                                        || animID == Animations.HOVER_DOWN
                                        || animID == Animations.HOVER_UP)
                                    {
                                        m_log.Debug("[ANIMATION] - hover");
                                        startFrame = 75 * 4;
                                        endFrame = 79 * 4 - 1;
                                        animFramesPerSecond = 7;
                                    }
                                    if (animID == Animations.CROUCH)
                                    {
                                        m_log.Debug("[ANIMATION] - crouching");
                                        //md2Anim = MD2Animation.CrouchPain;
                                        // 135-153
                                        startFrame = 135 * 4;
                                        endFrame = 153 * 4 - 1;
                                    }
                                    //else md2Anim = MD2Animation.Stand;
                                }
                                if (avobj.SceneNode is AnimatedMeshSceneNode)
                                {
                                    ((AnimatedMeshSceneNode)avobj.SceneNode).AnimationSpeed = animFramesPerSecond;
                                    //((AnimatedMeshSceneNode)avobj.node).SetMD2Animation(md2Anim);
                                    ((AnimatedMeshSceneNode)avobj.SceneNode).SetFrameLoop(startFrame, endFrame);

                                }
                            }


                            if (avatarID == m_viewer.NetworkInterface.GetSelfUUID && m_viewer.AnimationManager.FramesDirty)
                            {
                                if (avobj.SceneNode is AnimatedMeshSceneNode)
                                {
                                    m_viewer.AnimationManager.FramesDirty = false;

                                    ((AnimatedMeshSceneNode)avobj.SceneNode).SetFrameLoop(m_viewer.AnimationManager.StartFrame, m_viewer.AnimationManager.StopFrame);
                                    m_log.Debug("setting frames to " + m_viewer.AnimationManager.StartFrame.ToString() + " " + m_viewer.AnimationManager.StopFrame.ToString());
                                }
                            }

                        }

                    }
                }
            }

        }

        /// <summary>
        /// Assign Textures to objects that requested them
        /// </summary>
        /// <param name="pCount">Number of textures to process this round</param>
        public void ProcessTextureModifications(int count)
        {
            lock (AssignReadyTextures)
            {
                if (AssignReadyTextures.Count < count)
                    count = AssignReadyTextures.Count;
            }

            for (int i = 0; i < count; i++)
            {

                TextureObjectPair tx;
                TextureExtended tex = null;

                lock (AssignReadyTextures)
                {
                    if (i >= AssignReadyTextures.Count)
                        break;

                    tx = AssignReadyTextures.Dequeue();

                    // Try not to double load the texture first.
                    if (!m_viewer.TextureManager.tryGetTexture(tx.TextureID, out tex))
                    {
                        // Nope, we really don't have that texture loaded yet.  Load it now.
                        tex = new TextureExtended(m_viewer.Renderer.Driver.GetTexture(tx.TextureName).Raw);
                    }
                }

                if (tx.Object != null && tex != null)
                {
                    Primitive.SculptData sculpt = tx.Object.Primitive.Sculpt;
                    if (sculpt != null && tx.TextureID == sculpt.SculptTexture)
                    {
                        tx.Object.FullUpdate = true;
                        //tx.vObj.mesh.Dispose();

                        if (tx.Object.SceneNode != null && tx.Object.SceneNode.TriangleSelector != null)
                        {
                            if (TriangleSelector != null)
                            {
                                TriangleSelector.RemoveTriangleSelector(tx.Object.SceneNode.TriangleSelector);
                            }

                        }
                        if (tx.Object.SceneNode != null && tx.Object.SceneNode.Raw != IntPtr.Zero)
                            m_viewer.Renderer.SceneManager.AddToDeletionQueue(tx.Object.SceneNode);

                        //tx.vObj.mesh = null;

                        lock (ObjectMeshModifications)
                        {
                            m_log.Warn("[SCULPT]: Got Sculpt Callback, remeshing");
                            ObjectMeshModifications.Enqueue(tx.Object);
                        }
                        continue;
                        // applyTexture will skip over textures that are not 
                        // defined in the textureentry
                    }

                    m_viewer.TextureManager.applyTexture(tex, tx.Object, tx.TextureID);
                }
            }
        }

        /// <summary>
        /// Updates all interpolation targets
        /// </summary>
        public void UpdateMovingObjects()
        {
            List<string> removestr = null;
            lock (MovingObjects)
            {
                foreach (string str in MovingObjects.Keys)
                {
                    VObject obj = MovingObjects[str];

                    // Check if the target is dead.
                    if (obj == null)
                    {
                        //if (removestr == null)
                        //removestr = new List<string>();

                        //removestr.Add(str);
                        continue;
                    }
                    if (obj.SceneNode == null)
                    {
                        //if (removestr == null)
                        //    removestr = new List<string>();

                        //removestr.Add(str);
                        continue;
                    }
                    if (obj.SceneNode.Raw == IntPtr.Zero)
                    {
                        //if (removestr == null)
                        //    removestr = new List<string>();

                        //removestr.Add(str);
                        continue;
                    }

                    // Interpolate
                    try
                    {
                        if (m_avatarObject != null && m_avatarObject.Primitive != null)
                        {
                            if (obj.Primitive.ID != m_avatarObject.Primitive.ID)
                            {
                                // If this is our avatar and the update came less then 5 seconds 
                                // after we last rotated, it'll just confuse the user
                                if (System.Environment.TickCount - m_viewer.AvatarController.m_userRotated < 5000)
                                {
                                    continue;
                                }
                            }
                        }

                        /*
                        bool againstground = false;
                        if (obj.prim != null && UserAvatar != null && UserAvatar.prim != null && currentSim != null)
                        {
                            if (UserAvatar.prim.ID == obj.prim.ID)
                            {
                                if (obj.prim.Position.Z >= 0)
                                    //terrainBitmap lower then avatar byte 2.3
                                    if (RegionHFArray[(int)(Util.Clamp<float>(obj.prim.Position.Y, 0, 255)), (int)(Util.Clamp<float>(obj.prim.Position.X, 0, 255))] + 1.5f >= obj.prim.Position.Z)
                                    {

                                        againstground = true;
                                    }
                                //m_log.InfoFormat("[INTERPOLATION]: TerrainHeight:{0}-{1}-{2}-<{3},{4},{5}>", RegionHFArray[(int)(Util.Clamp<float>(obj.prim.Position.Y, 0, 255)),(int)(Util.Clamp<float>(obj.prim.Position.X, 0, 255))], obj.prim.Position.Z, (int)(Util.Clamp<float>(obj.prim.Position.Y, 0, 255) * 256 + Util.Clamp<float>(obj.prim.Position.X, 0, 255)), obj.prim.Position.X, obj.prim.Position.Y, obj.prim.Position.Z);
                            }
                        }
                        if (againstground)
                        {
                            obj.prim.Velocity.Z = 0;
                        }
                         */

                        /*Vector3D pos = new Vector3D(obj.node.Position.X, (againstground ? RegionHFArray[(int)(Util.Clamp<float>(obj.prim.Position.Y, 0, 255)), (int)(Util.Clamp<float>(obj.prim.Position.X, 0, 255))] + 0.9f : obj.node.Position.Y), obj.node.Position.Z);
                        Vector3D interpolatedpos = ((new Vector3D(obj.prim.Velocity.X, obj.prim.Velocity.Z, obj.prim.Velocity.Y) * 0.073f) * TimeDilation);
                        if (againstground)
                        {
                            interpolatedpos.Y = 0;
                        }

                        //if (obj.prim is Avatar)
                        //{
                        //Avatar av = (Avatar)obj.prim;
                        //if (obj.prim.Velocity.Z < 0 && obj.prim.Velocity.Z > -2f)
                        //    obj.prim.Velocity.Z = 0;
                        // }
                        obj.node.Position = pos + interpolatedpos;
                        */

                        Vector3 distance = obj.TargetPosition - obj.Primitive.Position;
                        obj.Primitive.Position = obj.Primitive.Position + distance * 0.05f;
                        obj.SceneNode.Position = new Vector3D(obj.Primitive.Position.X, obj.Primitive.Position.Z, obj.Primitive.Position.Y);
                    }
                    catch (AccessViolationException)
                    {
                        //if (removestr == null)
                        //    removestr = new List<string>();

                        //removestr.Add(str);
                        continue;
                    }
                    catch (System.Runtime.InteropServices.SEHException)
                    {

                        // if (removestr == null)
                        //    removestr = new List<string>();

                        // removestr.Add(str);
                        continue;
                    }

                }

                // Remove dead Interpolation targets
                if (removestr != null)
                {
                    foreach (string str2 in removestr)
                    {
                        if (MovingObjects.ContainsKey(str2))
                        {
                            MovingObjects.Remove(str2);
                        }
                    }
                }
            }

        }

        public void OnNetworkSimulatorConnected(VSimulator sim)
        {
            bool isCurrentSim = false;
            ulong simhandle = sim.Handle;
            m_log.Warn("Connected to sim with:" + simhandle);

            if (simhandle == 0)
                simhandle = m_viewer.TestNeighbor;



            if (CurrentSimulator == null)
            {
                CurrentSimulator = sim;
                isCurrentSim = true;
            }
            else
            {
                if (CurrentSimulator.Handle == simhandle)
                {
                    isCurrentSim = true;
                }
            }

            // Add the simulators to our known simulators and initialize the terrain constructs
            lock (Simulators)
            {
                if (!Simulators.ContainsKey(simhandle))
                {
                    Simulators.Add(simhandle, sim);
                    m_viewer.TerrainManager.OnSimulatorConnected(sim);
                }
            }
            if (isCurrentSim)
            {
                // Set the water position
                //SNGlobalwater.Position = new Vector3D(0, sim.WaterHeight-0.5f, 0);
                WaterNode.Position = new Vector3D(0, sim.WaterHeight - 0.5f, 0);
                //SNGlobalwater.Position = new Vector3D(0, sim.WaterHeight - 50.5f, 0);
                // TODO REFIX!
            }
        }

        public void OnNetworkObjectAdd(VSimulator sim, Primitive prim, ulong regionHandle, ushort timeDilation)
        {
            PCode pCode = prim.PrimData.PCode;
            if (pCode == PCode.Grass || pCode == PCode.NewTree || pCode == PCode.Tree)
            {
                if (m_viewer.ProcessFoliage)
                {
                    FoliageCount++;
                    //m_log.Debug("[FOLIAGE]: got foliage, location: " + foliage.Position.ToString());

                    VFoliage newFoliageObject = new VFoliage();

                    // add to the foliage queue
                    newFoliageObject.Primitive = prim;
                    lock (FoliageMeshModifications)
                    {
                        FoliageMeshModifications.Enqueue(newFoliageObject);
                    }
                }
                return;
            }
            //System.Console.WriteLine(prim.ToString());
            //return;
            PrimitiveCount++;
            VObject newObject = null;

            //bool foundEntity = false;
#if DebugObjectPipeline
            m_log.DebugFormat("[OBJ]: Got New Prim ID: {0}", prim.ID);
#endif
            lock (Objects)
            {
                if (Objects.ContainsKey(regionHandle.ToString() + prim.LocalID.ToString()))
                {
                    //foundEntity = true;
                    newObject = Objects[regionHandle.ToString() + prim.LocalID.ToString()];
#if DebugObjectPipeline
                    m_log.DebugFormat("[OBJ]: Reusing Entitity ID: {0}", prim.ID);
#endif
                }
                else
                {
#if DebugObjectPipeline
    m_log.DebugFormat("[OBJ]: New Entitity ID: {0}", prim.ID);
#endif
                }
            }

            if (newObject != null)
            {
                if (newObject.SceneNode != null)
                {
                    if (newObject.SceneNode.TriangleSelector != null)
                    {
                        if (TriangleSelector != null)
                        {
                            TriangleSelector.RemoveTriangleSelector(newObject.SceneNode.TriangleSelector);
                        }
                    }

                    for (uint i = 0; i < newObject.SceneNode.MaterialCount; i++)
                    {
                        //IrrlichtNETCP.Material objmaterial = newObject.node.GetMaterial((int)i);
                        //objmaterial.Texture1.Dispose();
                        //if (objmaterial.Layer1 != null)
                        //{
                        //    if (objmaterial.Layer1.Texture != null)
                        //    {
                        //         objmaterial.Layer1.Texture.Dispose();
                        //     }
                        //     objmaterial.Layer1.Dispose();
                        // }
                        //objmaterial.Dispose();
                    }

                    m_viewer.Renderer.SceneManager.AddToDeletionQueue(newObject.SceneNode);
#if DebugObjectPipeline
                    m_log.DebugFormat("[OBJ]: Deleted Node for ID: {0}", prim.ID);
#endif

                    newObject.SceneNode = null;
                    Mesh objmesh = newObject.Mesh;
                    for (int i = 0; i < objmesh.MeshBufferCount; i++)
                    {
                        MeshBuffer mb = objmesh.GetMeshBuffer(i);
                        mb.Dispose();

                    }
                    newObject.Mesh.Dispose();
                    newObject.Primitive = null;
                }

            }


            // Box the object and node
            newObject = VObjectUtil.NewVObject(prim, newObject);



            // Add to the mesh queue
            lock (ObjectMeshModifications)
            {
                ObjectMeshModifications.Enqueue(newObject);
            }
        }

        public void OnNetworkAvatarAdd(VSimulator sim, Avatar avatar, ulong regionHandle, ushort timeDilation)
        {
            VObject avob = null;

            lock (Objects)
            {
                // If we've got an entitiy for this avatar, then this is a full object update
                // not a new avatar
                if (Objects.ContainsKey(regionHandle.ToString() + avatar.LocalID.ToString()))
                {
                    VObject existingob = Objects[regionHandle.ToString() + avatar.LocalID.ToString()];
                    existingob.Primitive = avatar;
                    Objects[regionHandle.ToString() + avatar.LocalID.ToString()] = existingob;
                    avob = existingob;
                    avob.TargetPosition = new Vector3(avatar.Position.X, avatar.Position.Y, avatar.Position.Z);
                    if (!MovingObjects.ContainsKey(regionHandle.ToString() + avatar.LocalID.ToString()))
                    {
                        MovingObjects.Add(regionHandle.ToString() + avatar.LocalID.ToString(), avob);
                    }
                }
                else
                {
                    avob = new VObject();
                    avob.Primitive = avatar;
                    avob.Mesh = null;
                    avob.SceneNode = null;
                    Objects.Add(regionHandle.ToString() + avatar.LocalID.ToString(), avob);

                }
            }

            avob.FullUpdate = true;
            // Add to the Object Modification queue.
            //lock (objectModQueue)
            //{
            //    objectModQueue.Enqueue(avob);
            //}

            // Add to the Avatar modification queue
            lock (AvatarModidifications) { AvatarModidifications.Enqueue(avob); }

            bool needInitialAnimationState = false;

            lock (Avatars)
            {
                if (Avatars.ContainsKey(avatar.ID))
                {
                    Avatars[avatar.ID] = avob;
                }
                else
                {
                    Avatars.Add(avatar.ID, avob);
                    needInitialAnimationState = true;
                }
            }

            if (needInitialAnimationState)
            {
                lock (m_viewer.NetworkInterface.AvatarAnimations)
                {
                    List<UUID> initialAnims = new List<UUID>();
                    initialAnims.Add(Animations.STAND);
                    if (m_viewer.NetworkInterface.AvatarAnimations.ContainsKey(avatar.ID))
                        m_viewer.NetworkInterface.AvatarAnimations[avatar.ID] = initialAnims;
                    else
                        m_viewer.NetworkInterface.AvatarAnimations.Add(avatar.ID, initialAnims);
                }
            }
        }

        public void OnNetworkObjectUpdate(VSimulator simulator, ObjectUpdate update, ulong regionHandle, ushort timeDilation)
        {
            VObject obj = null;
            //if (!update.Avatar)
            //{
            lock (Objects)
            {
                if (Objects.ContainsKey(regionHandle.ToString() + update.LocalID.ToString()))
                {
                    obj = Objects[regionHandle.ToString() + update.LocalID.ToString()];
                    if (obj.SceneNode == null)
                    {
                        return;
                    }
                    if (obj.Primitive is Avatar)
                    {
                        if (obj.SceneNode != null)
                        {
                            obj.FullUpdate = false;
                        }
                        //obj.TargetPosition = update.Position + new Vector3(0.0f, 0.0f, (float)(-obj.SceneNode.BoundingBox.MinEdge.Y * obj.SceneNode.Scale.Y));
                        obj.TargetPosition = update.Position;
                    }
                    else
                    {
                        obj.FullUpdate = false;
                        obj.TargetPosition = update.Position;
                    }



                    //Vector3 direction=(update.Position-obj.prim.Position);
                    //obj.prim.Position = obj.prim.Position + direction*0.1f;

                    // Update the primitive properties for this object.
                    obj.Primitive.Acceleration = update.Acceleration;
                    obj.Primitive.AngularVelocity = update.AngularVelocity;
                    obj.Primitive.CollisionPlane = update.CollisionPlane;
                    //obj.prim.Position = update.Position;
                    obj.Primitive.Rotation = update.Rotation;
                    obj.Primitive.PrimData.State = update.State;
                    obj.Primitive.Textures = update.Textures;
                    obj.Primitive.Velocity = update.Velocity;

                    // Save back to the Entities.  vObject used to be a value type, so this was neccessary.
                    // it may not be anymore.

                    Objects[regionHandle.ToString() + update.LocalID.ToString()] = obj;
                }
            }

            // Enqueue this object into the modification queue.
            if (obj != null)
            {
                if (obj.Primitive is Avatar)
                {
                    if (obj.SceneNode != null)
                    {
                        //lock (objectModQueue)
                        //{
                        //    objectModQueue.Enqueue(obj);
                        //}
                        lock (AvatarModidifications) { AvatarModidifications.Enqueue(obj); }
                    }
                }
                else
                {
                    AddObject(obj);
                }
            }
            //}
        }

        public void OnNetworkObjectRemove(VSimulator psim, uint pLocalID)
        {
            ulong regionHandle = psim.Handle;
            m_log.Debug("[DELETE]: obj " + regionHandle.ToString() + ":" + pLocalID.ToString());
            VObject obj = null;

            lock (Objects)
            {

                if (Objects.ContainsKey(regionHandle.ToString() + pLocalID.ToString()))
                {
                    obj = Objects[regionHandle.ToString() + pLocalID.ToString()];

                    if (obj.SceneNode != null)
                    {
                        // If we're interpolating this object, stop
                        lock (MovingObjects)
                        {
                            if (MovingObjects.ContainsKey(regionHandle.ToString() + obj.Primitive.LocalID.ToString()))
                            {
                                MovingObjects.Remove(regionHandle.ToString() + obj.Primitive.LocalID.ToString());
                            }

                        }
                        // If the camera is targetting this object, stop targeting this object
                        if (m_viewer.CameraController.TargetNode == obj.SceneNode)
                            m_viewer.CameraController.TargetNode = null;

                        // Remove this object from our picker.
                        if (obj.SceneNode.TriangleSelector != null)
                        {
                            if (TriangleSelector != null)
                            {
                                TriangleSelector.RemoveTriangleSelector(obj.SceneNode.TriangleSelector);
                            }
                        }

                        m_viewer.Renderer.SceneManager.AddToDeletionQueue(obj.SceneNode);
                        obj.SceneNode = null;

                    }
                    // Remove this object from the known entities.
                    Objects.Remove(regionHandle.ToString() + pLocalID.ToString());
                }
            }

            // If it's an avatar, remove it from known avatars
            if (obj != null)
            {
                if (obj.Primitive is Avatar)
                {
                    lock (Avatars)
                    {
                        if (Avatars.ContainsKey(obj.Primitive.ID))
                        {
                            Avatars.Remove(obj.Primitive.ID);
                        }
                    }
                    // remove any pending animations for the avatar
                    lock (m_viewer.NetworkInterface.AvatarAnimations)
                    {
                        if (m_viewer.NetworkInterface.AvatarAnimations.ContainsKey(obj.Primitive.ID))
                            m_viewer.NetworkInterface.AvatarAnimations.Remove(obj.Primitive.ID);
                    }
                    if (obj.Primitive.ID == m_viewer.NetworkInterface.GetSelfUUID)
                    {
                        m_avatarObject = null;
                    }
                }
            }
        }

        public void OnNetworkTextureDownloaded(string tex, VObject vObj, UUID AssetID)
        {
            TextureObjectPair tx = new TextureObjectPair();
            tx.TextureName = tex;
            tx.Object = vObj;
            tx.TextureID = AssetID;
            AssignReadyTextures.Enqueue(tx);
        }
    }
}
