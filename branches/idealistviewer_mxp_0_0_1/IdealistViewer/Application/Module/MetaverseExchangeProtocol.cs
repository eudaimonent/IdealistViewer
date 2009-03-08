using System;
using System.Collections.Generic;
using System.Text;
using MXP;
using MXP.Messages;
using log4net;
using System.Reflection;
using MXP.Fragments;
using MXP.Extentions.OpenMetaverseFragments.Proto;
using OpenMetaverse;
using MXP.Util;
using MXP.Common.Proto;

namespace IdealistViewer.Module
{
    public class MetaverseExchangeProtocol : IProtocol
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private MxpClient m_client;
        private UUID m_bubbleId;
        private UUID m_userId;
        private Avatar m_avatar;
        private float m_heading = 0;
        private VSimulator m_simulator;
        private bool m_translateChange = false;
        private bool m_orientateChange = false;
        private bool m_walking = false;
        private Vector3 FORWARD = new Vector3(1, 0, 0);
        private Vector3 BACKWARD = new Vector3(-1, 0, 0);
        private Vector3 LEFT = new Vector3(0, 0, -1);
        private Vector3 RIGHT = new Vector3(0, 0, 1);
        private Vector3 UP = new Vector3(0, 1, 0);
        private Vector3 DOWN = new Vector3(0, -1, 0);

        public MetaverseExchangeProtocol()
        {
            m_client = new MxpClient("Idealist Viewer", 0, 0);
            m_client.ConnectionSuccess += MxpEventConnectionSuccess;
            m_client.ConnectionFailure += MxpEventConnectionFailure;
            m_client.ServerDisconnected += MxpEventDisconnected;
            m_client.MessageReceived += MxpEventMessageReceived;
        }

        private void MxpEventConnectionSuccess(JoinResponseMessage message)
        {
            m_log.Info("Login success.");
            m_userId=new UUID(message.ParticipantId);
            m_simulator = new VSimulator();
            m_simulator.Id = m_bubbleId;
            m_simulator.Handle = m_bubbleId.GetULong();

            if (OnLogin != null)
            {
                OnLogin(LoginStatus.Success, "");
            }

            if (OnGridConnected != null)
            {
                OnGridConnected();
            }

            if (OnSimConnected != null)
            {
                OnSimConnected(m_simulator);
            }
        }

        private void MxpEventConnectionFailure(JoinResponseMessage message)
        {
            m_log.ErrorFormat("Login failed: " + message.FailureCode);
            if (OnLogin != null)
            {
                OnLogin(LoginStatus.Failed, "Login failed.");
            }
        }

        private void MxpEventDisconnected(Message message)
        {
            m_log.Info("Disconnected.");
        }

        private void RequestAvatarModification()
        {
            ModifyRequestMessage modifyRequest = new ModifyRequestMessage();
            modifyRequest.ObjectFragment.ObjectId = m_avatar.ID.Guid;
            modifyRequest.ObjectFragment.ParentObjectId = Guid.Empty;
            modifyRequest.ObjectFragment.ObjectIndex = m_avatar.LocalID;
            modifyRequest.ObjectFragment.ObjectName = m_avatar.Name;
            modifyRequest.ObjectFragment.OwnerId = m_userId.Guid;
            modifyRequest.ObjectFragment.TypeId = Guid.Empty;
            modifyRequest.ObjectFragment.TypeName = "Avatar";
            modifyRequest.ObjectFragment.Acceleration = new MsdVector3f();
            modifyRequest.ObjectFragment.AngularAcceleration = new MsdQuaternion4f();
            modifyRequest.ObjectFragment.AngularVelocity = new MsdQuaternion4f();
            modifyRequest.ObjectFragment.BoundingSphereRadius = m_avatar.Scale.Length();
            modifyRequest.ObjectFragment.Location = ToOmVector(m_avatar.Position);
            modifyRequest.ObjectFragment.Mass = 1.0f;
            modifyRequest.ObjectFragment.Orientation = ToOmQuaternion(m_avatar.Rotation);
            modifyRequest.ObjectFragment.Velocity = ToOmVector(m_avatar.Velocity);
            OmAvatarExt avatarExt=new OmAvatarExt();
            //avatarExt.TargetLocation

            //if (m_translateChange)
            //{
                Quaternion orientation = m_avatar.Rotation;
                Vector3 movementDirection = new Vector3(0,0,0);
                if (m_forward)
                {
                    movementDirection += FORWARD*orientation;
                }
                if (m_backward)
                {
                    movementDirection += BACKWARD * orientation;
                }
                if (m_straffLeft)
                {
                    movementDirection += LEFT * orientation;
                }
                if (m_straffRight)
                {
                    movementDirection += RIGHT * orientation;
                }
                if (m_up)
                {
                    movementDirection += UP * orientation;
                }
                if (m_down)
                {
                    movementDirection += DOWN * orientation;
                }
                avatarExt.MovementDirection = ToOmVector(movementDirection);
            //}

            if (m_orientateChange)
            {
                avatarExt.TargetOrientation=ToOmQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitZ,m_heading));
            }

            if (m_translateChange)
            {
                m_walking = !m_walking;

                if (m_walking)
                {
                    m_avatarAnimations[m_avatar.ID] = new List<UUID>();
                    m_avatarAnimations[m_avatar.ID].Add(Animations.WALK);
                }
                else
                {
                    m_avatarAnimations[m_avatar.ID] = new List<UUID>();
                    m_avatarAnimations[m_avatar.ID].Add(Animations.STAND);
                }

            }


            modifyRequest.SetExtension<OmAvatarExt>(avatarExt);

            m_client.Send(modifyRequest);

            m_translateChange = false;
            m_orientateChange = false;
        }

        private void MxpEventMessageReceived(Message message)
        {
            if (message.GetType() == typeof(PerceptionEventMessage))
            {
                PerceptionEventMessage pe = (PerceptionEventMessage)message;
                m_log.Info("Received perception of " + pe.ObjectFragment.ObjectName);

                if (pe.ObjectFragment.TypeName == "Terrain")
                {
                    LandPerceptionReceived(pe);
                }
                else if (pe.ObjectFragment.TypeName == "Avatar")
                {
                    AvatarPerceptionReceived(pe);
                }
                else
                {
                    PrimitivePercetionReceived(pe);
                }
            }
            else if (message.GetType() == typeof(MovementEventMessage))
            {
                ObjectMovementReceived((MovementEventMessage)message);
            }
            else
            {
                m_log.Warn("Unhandled message: " + message.GetType());
            }
        }

        private void LandPerceptionReceived(PerceptionEventMessage pe)
        {
            ObjectFragment objectFragment = pe.ObjectFragment;
            m_log.Info("Received terrain: " + objectFragment.ObjectName + " (" + pe.ObjectFragment.ExtensionLength + ")");
            OmBitmapTerrainExt terrainExt = pe.GetExtension<OmBitmapTerrainExt>();
            m_simulator.WaterHeight = terrainExt.WaterLevel;
            float[] map = CompressUtil.DecompressHeightMap(terrainExt.HeightMap, terrainExt.Offset, terrainExt.Scale);
            for (int x = 0; x < terrainExt.Width; x += 16)
            {
                for (int y = 0; y < terrainExt.Height; y += 16)
                {
                    UpdateLandPatch(x, y, map);
                }
            }
            return;
        }

        private void AvatarPerceptionReceived(PerceptionEventMessage pe)
        {
            ObjectFragment objectFragment = pe.ObjectFragment;

            Avatar primitive = new Avatar();
            primitive.PrimData.PCode = FromOmType(objectFragment.TypeName); ;

            NameValue firstName = new NameValue();
            firstName.Name = "firstName";
            firstName.Value = objectFragment.ObjectName.Split(' ')[0];
            NameValue lastName = new NameValue();
            lastName.Name = "lastName";
            lastName.Value = objectFragment.ObjectName.Split(' ')[1];
            primitive.NameValues = new NameValue[] { firstName, lastName };

            primitive.ID = new UUID(objectFragment.ObjectId);
            primitive.ParentID = 0; // To do resolve parent id
            primitive.LocalID = objectFragment.ObjectIndex;
            primitive.OwnerID = new UUID(objectFragment.OwnerId);
            primitive.RegionHandle = m_simulator.Handle;

            primitive.Rotation = FromOmQuaternion(objectFragment.Orientation);
            primitive.AngularVelocity = FromOmQuaternionToEulerAngles(objectFragment.AngularVelocity);

            primitive.Position = FromOmVector(objectFragment.Location);
            primitive.Velocity = FromOmVector(objectFragment.Velocity);
            primitive.Acceleration = FromOmVector(objectFragment.Acceleration);

            if (m_userId.Guid == objectFragment.OwnerId)
            {
                m_avatar = primitive;
            }

            OnNewAvatar(m_simulator, primitive, m_simulator.Handle, 100);
        }

        private void ObjectMovementReceived(MovementEventMessage movementEvent)
        {
            m_log.Info("Received movement of " + movementEvent.ObjectIndex);

            ObjectUpdate objectUpdate = new ObjectUpdate();
            objectUpdate.LocalID = movementEvent.ObjectIndex;
            objectUpdate.Avatar = m_avatar.LocalID == movementEvent.ObjectIndex;
            objectUpdate.Position = FromOmVector(movementEvent.Location);
            objectUpdate.Rotation = FromOmQuaternion(movementEvent.Orientation);
            objectUpdate.Velocity = new Vector3();
            objectUpdate.Acceleration = new Vector3();
            objectUpdate.AngularVelocity = new Vector3();
            OnObjectUpdated(m_simulator, objectUpdate, m_simulator.Handle, 100);
        }

        private void PrimitivePercetionReceived(PerceptionEventMessage pe)
        {
            ObjectFragment objectFragment = pe.ObjectFragment;

            Primitive primitive = new Primitive();
            PCode pcode = FromOmType(objectFragment.TypeName);
            primitive.PrimData.PCode = pcode;

            primitive.ID = new UUID(objectFragment.ObjectId);
            primitive.ParentID = 0; // To do resolve parent id
            primitive.LocalID = objectFragment.ObjectIndex;
            primitive.OwnerID = new UUID(objectFragment.OwnerId);
            primitive.RegionHandle = m_simulator.Handle;

            primitive.Rotation = FromOmQuaternion(objectFragment.Orientation);
            primitive.AngularVelocity = FromOmQuaternionToEulerAngles(objectFragment.AngularVelocity);

            primitive.Position = FromOmVector(objectFragment.Location);
            primitive.Velocity = FromOmVector(objectFragment.Velocity);
            primitive.Acceleration = FromOmVector(objectFragment.Acceleration);

            if (pe.HasExtension)
            {
                OmSlPrimitiveExt extFragment = pe.GetExtension<OmSlPrimitiveExt>();

                primitive.Flags = (PrimFlags)extFragment.UpdateFlags;

                if (extFragment.TextureEntry == null)
                {
                    int i = 0;
                }

                if (pcode != PCode.Tree && pcode != PCode.NewTree && pcode != PCode.Grass)
                {
                    primitive.PrimData.PathBegin = extFragment.PathBegin;
                    primitive.PrimData.PathEnd = extFragment.PathEnd;
                    primitive.PrimData.PathScaleX = extFragment.PathScaleX / 100.0f;
                    primitive.PrimData.PathScaleY = extFragment.PathScaleY / 100.0f;
                    primitive.PrimData.PathShearX = extFragment.PathShearX;
                    primitive.PrimData.PathShearY = extFragment.PathShearY;

                    primitive.PrimData.PathSkew = extFragment.PathSkew;
                    primitive.PrimData.ProfileBegin = extFragment.ProfileBegin;
                    primitive.PrimData.ProfileEnd = extFragment.ProfileEnd;
                    primitive.PrimData.PathCurve = (PathCurve)extFragment.PathCurve;
                    primitive.PrimData.ProfileCurve = (ProfileCurve)extFragment.ProfileCurve;
                    primitive.PrimData.ProfileHollow = extFragment.ProfileHollow;
                    primitive.PrimData.PathRadiusOffset = extFragment.PathRadiusOffset;
                    primitive.PrimData.PathRevolutions = extFragment.PathRevolutions;
                    primitive.PrimData.PathTaperX = extFragment.PathTaperX;
                    primitive.PrimData.PathTaperY = extFragment.PathTaperY;
                    primitive.PrimData.PathTwist = extFragment.PathTwist;
                    primitive.PrimData.PathTwistBegin = extFragment.PathTwistBegin;
                }

                primitive.PrimData.Material = (Material)extFragment.Material;
                primitive.PrimData.State = (byte)extFragment.State;
                primitive.Textures = new OpenMetaverse.Primitive.TextureEntry(extFragment.TextureEntry, 0, extFragment.TextureEntry.Length);
                primitive.TextureAnim = new OpenMetaverse.Primitive.TextureAnimation(extFragment.TextureAnim, 0);
                primitive.Scale = FromOmVector(extFragment.Scale);
                primitive.Text = extFragment.Text;
                primitive.TextColor = FromOmColor(extFragment.TextColor);
                primitive.ParticleSys = new OpenMetaverse.Primitive.ParticleSystem(extFragment.PSBlock, 0);
                primitive.ClickAction = (ClickAction)extFragment.ClickAction;

                // TODO Figure out what is behind this behaviour in SL protocol
                if (primitive.PrimData.PathEnd == 0)
                {
                    primitive.PrimData.PathEnd = 1;
                }
                if (primitive.PrimData.PathRevolutions == 0)
                {
                    primitive.PrimData.PathRevolutions = 1;
                }
                if (primitive.PrimData.ProfileEnd == 0)
                {
                    primitive.PrimData.ProfileEnd = 1;
                }
                // END OF TO DO
                // What is this extFragment.ExtraParams; ??

            }

            OnNewPrim(m_simulator, primitive, m_simulator.Handle, 100);
        }

        private void UpdateLandPatch(int xs, int ys, float[] map)
        {
            float[] patch = new float[16 * 16];
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    patch[x + y * 16] = map[xs + x + 256 * (ys + y)];
                }
            }
            OnLandPatch(m_simulator, xs / 16, ys / 16, 16, patch);
        }

        private PCode FromOmType(String value)
        {
            if (value == "Avatar")
            {
                return PCode.Avatar;
            }
            if (value == "Grass")
            {
                return PCode.Grass;
            }
            if (value == "NewTree")
            {
                return PCode.NewTree;
            }
            if (value == "None")
            {
                return PCode.None;
            }
            if (value == "ParticleSystem")
            {
                return PCode.ParticleSystem;
            }
            if (value == "Primitive")
            {
                return PCode.Prim;
            }
            if (value == "Tree")
            {
                return PCode.Tree;
            }
            throw new Exception("Unknown PCode: "+value);
        }

        private Color4 FromOmColor(MsdColor4f value)
        {
            return new Color4(value.R / 255, value.G / 255, value.B / 255, value.A / 255);
        }

        private Vector3 FromOmVector(MsdVector3f values)
        {
            return new Vector3(values.X, values.Y, values.Z);
        }

        private Vector3 FromOmQuaternionToEulerAngles(MsdQuaternion4f value)
        {
            Quaternion quaternion = FromOmQuaternion(value);
            float roll;
            float pitch;
            float yaw;
            quaternion.GetEulerAngles(out roll, out pitch, out yaw);
            return new Vector3(roll, pitch, yaw);
        }

        private Quaternion FromOmQuaternion(MsdQuaternion4f quaternion)
        {
            return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        private MsdVector3f ToOmVector(Vector3 value)
        {
            MsdVector3f encodedValue = new MsdVector3f();
            encodedValue.X = value.X;
            encodedValue.Y = value.Y;
            encodedValue.Z = value.Z;
            return encodedValue;
        }

        private MsdQuaternion4f ToOmQuaternion(Quaternion value)
        {
            MsdQuaternion4f encodedValue = new MsdQuaternion4f();
            encodedValue.X = value.X;
            encodedValue.Y = value.Y;
            encodedValue.Z = value.Z;
            encodedValue.W = value.W;
            return encodedValue;
        }

        #region IProtocol Members

        public void BeginLogin(string loginURI, string username, string password, string startlocation)
        {
            this.m_loginURI = loginURI;
            this.m_userName = username;
            this.m_password = password;
            this.m_startLocation = startlocation;

            Util.separateUsername(username, out m_firstName, out m_lastName);

            Uri uri = new Uri(loginURI);
            string host = uri.Host;
            int port = uri.Port;
            string[] pathFolders= uri.AbsolutePath.Split('/');
            string bubbleIdString = pathFolders[1];
            string location = pathFolders[2];
            m_bubbleId = new UUID(bubbleIdString);

            m_client.Connect(host, port, m_bubbleId.Guid, "", "", username, password, false);
        }

        public void Logout()
        {
            if (m_client.IsConnected)
            {
                m_client.Disconnect();
            }
        }

        public void AvatarAnimationHandler(OpenMetaverse.Packets.Packet packet, OpenMetaverse.Simulator sim)
        {
            throw new NotImplementedException();
        }
        
        public event Chat OnChat;

        public event FriendsListchanged OnFriendsListChanged;

        public event GridConnected OnGridConnected;

        public event ImageReceived OnImageReceived;

        public event LandPatch OnLandPatch;

        public event Login OnLogin;

        public event NewAvatar OnNewAvatar;

        public event NewPrim OnNewPrim;

        public event ObjectKilled OnObjectKilled;

        public event ObjectUpdated OnObjectUpdated;

        public event SimConnected OnSimConnected;

        private string m_loginURI;
        public string LoginURI
        {
            get
            {
                return m_loginURI;
            }
        }
        private string m_firstName;
        public string FirstName
        {
            get
            {
                return m_firstName;
            }
        }
        private string m_lastName;
        public string LastName
        {
            get
            {
                return m_lastName;
            }
        }
        private string m_userName;
        public string UserName
        {
            get
            {
                return m_userName;
            }
        }
        private string m_password;
        public string Password
        {
            get
            {
                return m_password;
            }
        }
        private string m_startLocation;
        public string StartLocation
        {
            get
            {
                return m_startLocation;
            }
        }

        private bool m_forward;
        public bool Forward
        {
            get
            {
                return m_forward;
            }
            set
            {
                if(value!=m_forward)
                {
                    m_forward=value;
                    m_translateChange = true;
                }
            }
        }

        private bool m_backward;
        public bool Backward
        {
            get
            {
                return m_backward;
            }
            set
            {
                if (value != m_backward)
                {
                    m_backward = value;
                    m_translateChange = true;
                }
            }
        }

        private bool m_straffLeft;
        public bool StraffLeft
        {
            get
            {
                return m_straffLeft;
            }
            set
            {
                if (value != m_straffLeft)
                {
                    m_straffLeft = value;
                    m_translateChange = true;
                }
            }
        }

        private bool m_straffRight;
        public bool StraffRight
        {
            get
            {
                return m_straffRight;
            }
            set
            {
                if (value != m_straffRight)
                {
                    m_straffRight = value;
                    m_translateChange = true;
                }
            }
        }

        private bool m_up;
        public bool Up
        {
            get
            {
                return m_up;
            }
            set
            {
                if (value != m_up)
                {
                    m_up = value;
                    m_translateChange = true;
                }
            }
        }

        private bool m_down;
        public bool Down
        {
            get
            {
                return m_down;
            }
            set
            {
                if (value != m_down)
                {
                    m_down = value;
                    m_translateChange = true;
                }
            }
        }

        private bool m_flying;
        public bool Flying
        {
            get
            {
                return m_flying;
            }
            set
            {
                m_flying = value;
            }
        }

        private bool m_multipleSims;
        public bool MultipleSims
        {
            get
            {
                return m_multipleSims;
            }
            set
            {
                m_multipleSims = value;
            }
        }

        public bool Connected
        {
            get { return m_client.IsConnected; }
        }

        public bool Jump
        {
            set { throw new NotImplementedException(); }
        }

        public OpenMetaverse.UUID GetSelfUUID
        {
            get { return m_avatar!=null?m_avatar.ID:UUID.Zero; }
        }

        public Dictionary<OpenMetaverse.UUID, OpenMetaverse.FriendInfo> Friends
        {
            get { return new Dictionary<OpenMetaverse.UUID, OpenMetaverse.FriendInfo>(); }
        }

        public Dictionary<UUID, List<UUID>> m_avatarAnimations = new Dictionary<UUID, List<UUID>>();
        public Dictionary<UUID, List<UUID>> AvatarAnimations
        {
            get
            {
                return m_avatarAnimations;
            }
            set
            {
                m_avatarAnimations = value;
            }
        }

        public void RequestTexture(OpenMetaverse.UUID assetID)
        {
            //throw new NotImplementedException();
        }

        public void Say(string message)
        {
            //throw new NotImplementedException();
        }

        public void SetCameraPosition(OpenMetaverse.Vector3[] camdata)
        {
            //throw new NotImplementedException();
        }

        public void SetHeightWidth(uint height, uint width)
        {
            throw new NotImplementedException();
        }

        public void Shout(string message)
        {
            throw new NotImplementedException();
        }

        public void Teleport(string region, float x, float y, float z)
        {
            throw new NotImplementedException();
        }

        public void TurnToward(OpenMetaverse.Vector3 target)
        {
            throw new NotImplementedException();
        }

        public void UpdateFromHeading(double heading)
        {
            if (m_heading != (float)heading)
            {
                m_orientateChange = true;
                m_heading = (float)heading;
            }
            //throw new NotImplementedException();
        }

        public void Whisper(string message)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IProtocol Members


        public void Process()
        {
            if (m_translateChange || m_orientateChange)
            {
                RequestAvatarModification();
            }
            m_client.Process();
        }

        #endregion
    }
}
