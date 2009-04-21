using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using OpenMetaverse.Packets;
using log4net;
using OpenMetaverse.Imaging;
using System.Drawing;
using IdealistViewer.Scene;
using System.IO;
using System.Drawing.Imaging;

namespace IdealistViewer.Network
{

    public class LibOmvNetworkModule : INetworkInterface
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public event NetworkAvatarAddDelegate OnAvatarAdd;
        public event NetworkChatDelegate OnChat;
        public event NetworkConnectedDelegate OnConnected;
        public event NetworkSimulatorConnectedDelegate OnSimulatorConnected;
        public event NetworkLandUpdateDelegate OnLandUpdate;
        public event NetworkLoginDelegate OnLoggedIn;
        public event NetworkObjectAddDelegate OnObjectAdd;
        public event NetworkObjectRemoveDelegate OnObjectRemove;
        public event NeworkObjectUpdateDelegate OnObjectUpdate;
        public event NetworkTextureDownloadedDelegate OnTextureDownloaded;
        public event NetworkFriendsListUpdateDelegate OnFriendsListUpdate;

        public string loginURI;
        public string LoginURI
        {
            get
            {
                return loginURI;
            }
        }
        public string firstName;
        public string FirstName
        {
            get
            {
                return firstName;
            }
        }
        public string lastName;
        public string LastName
        {
            get
            {
                return lastName;
            }
        }
        public string username;
        public string UserName
        {
            get
            {
                return username;
            }
        }
        public string password;
        public string Password
        {
            get
            {
                return password;
            }
        }
        public string startlocation;
        public string StartLocation
        {
            get
            {
                return startlocation;
            }
        }

        // received animations are stored here before being processed in the main frame loop
        public Dictionary<UUID, List<UUID>> avatarAnimations = new Dictionary<UUID,List<UUID>>();
        public Dictionary<UUID, List<UUID>> AvatarAnimations
        {
            get
            {
                return avatarAnimations;
            }
            set
            {
                avatarAnimations = value;
            }
        }

        public GridClient m_user;

        public LibOmvNetworkModule()
        {
            m_user = new GridClient();

            //m_user.Settings.STORE_LAND_PATCHES = true;
            //m_user.Settings.MULTIPLE_SIMS = false;
            //m_user.Settings.OBJECT_TRACKING = true;
            //m_user.Settings.AVATAR_TRACKING = true;
            m_user.Settings.USE_TEXTURE_CACHE = false;
            //m_user.Settings.
            m_user.Settings.ALWAYS_DECODE_OBJECTS = false;
            
            m_user.Settings.SEND_AGENT_THROTTLE = true;
            //m_user.Settings.SEND_PINGS = true;

            m_user.Network.OnConnected += gridConnectedCallback;
            m_user.Network.OnDisconnected += disconnectedCallback;
            m_user.Network.OnSimConnected += simConnectedCallback;
            m_user.Network.OnLogin += loginStatusCallback;
            m_user.Terrain.OnLandPatch += landPatchCallback;
            m_user.Self.OnChat += chatCallback;
            m_user.Objects.OnNewAvatar += newAvatarCallback;
            m_user.Objects.OnNewPrim += newPrim;
            m_user.Objects.OnObjectKilled += objectKilledCallback;
            m_user.Network.OnLogin += loginCallback;
            m_user.Objects.OnObjectUpdated += objectUpdatedCallback;
            m_user.Assets.OnImageReceived += imageReceivedCallback;
            m_user.Friends.OnFriendNamesReceived += Friends_OnFriendNamesReceived;
            m_user.Friends.OnFriendOnline += Friends_OnFriendOnline;
            m_user.Friends.OnFriendOffline += Friends_OnFriendOffline;

            //m_user.Assets.RequestImage(
            //m_user.Assets.Cache..RequestImage(UUID.Zero, ImageType.Normal);
            
            m_user.Network.RegisterCallback(OpenMetaverse.Packets.PacketType.AvatarAnimation, AvatarAnimationHandler);

        }

        private void Friends_OnFriendOffline(FriendInfo friend)
        {
            if( OnFriendsListUpdate != null )
            {
                OnFriendsListUpdate();
            }
        }

        private void Friends_OnFriendOnline(FriendInfo friend)
        {
            if (OnFriendsListUpdate != null)
            {
                OnFriendsListUpdate();
            }
        }

        private void Friends_OnFriendNamesReceived(Dictionary<UUID, string> names)
        {
            if (OnFriendsListUpdate != null)
            {
                OnFriendsListUpdate();
            }
        }

        public Dictionary<UUID, FriendInfo> Friends
        {
            get
            {
                return m_user.Friends.FriendList.Dictionary;
            }
        }

        public void AvatarAnimationHandler(OpenMetaverse.Packets.Packet packet, Simulator sim)
        {
            // When animations for any avatar are received put them in the AvatarAnimations dictionary
            // in this module. They should be processed and deleted inbetween frames in the main frame loop
            // or deleted when an avatar is deleted from the scene.
            AvatarAnimationPacket animation = (AvatarAnimationPacket)packet;

            UUID avatarID = animation.Sender.ID;
            List<UUID> currentAnims = new List<UUID>();

            for (int i = 0; i < animation.AnimationList.Length; i++)
                currentAnims.Add(animation.AnimationList[i].AnimID);

            lock (AvatarAnimations)
            {
                if (AvatarAnimations.ContainsKey(avatarID))
                    AvatarAnimations[avatarID] = currentAnims;
                else
                    AvatarAnimations.Add(avatarID, currentAnims);
            }
        }

        public void loginStatusCallback(LoginStatus login, string message)
        {
            if (login == LoginStatus.Failed)
            {
                m_log.ErrorFormat("[CONNECTION]: Login Failed:{0}",message);
            }
        }
        private void imageReceivedCallback(ImageDownload image, AssetTexture asset)
        {
            if (OnTextureDownloaded != null)
            {

                VTexture texture = new VTexture();
                texture.TextureId = asset.AssetID;

                ManagedImage managedImage;
                Image tempImage;
                if (OpenJPEG.DecodeToImage(asset.AssetData, out managedImage, out tempImage))
                {
                    Bitmap textureBitmap = new Bitmap(tempImage.Width, tempImage.Height, PixelFormat.Format32bppArgb);
                    Graphics graphics = Graphics.FromImage(textureBitmap);
                    graphics.DrawImage(tempImage, 0, 0);
                    graphics.Flush();
                    graphics.Dispose();
                    texture.Image = textureBitmap;
                    OnTextureDownloaded(texture);
                }
            }
        }
        private void objectKilledCallback(Simulator simulator, uint objectID)
        {
            if (OnObjectRemove != null)
            {
                OnObjectRemove(new VSimulator(simulator), objectID);
            }
        }

        public void Login(string loginURI, string username, string password, string startlocation)
        {

            string firstname;
            string lastname;

            this.loginURI = loginURI;
            this.username = username;
            this.password = password;
            this.startlocation = startlocation;

            Util.separateUsername(username, out firstname, out lastname);

            this.firstName = firstname;
            this.lastName = lastname;


            LoginParams loginParams = getLoginParams(loginURI, username, password, startlocation);

            m_user.Network.BeginLogin(loginParams);
        }

        private void gridConnectedCallback(object sender)
        {
           
            m_user.Appearance.SetPreviousAppearance(false);



            if (OnConnected != null)
            {
                OnConnected();
            }
        }
        private void simConnectedCallback(Simulator sender)
        {
            m_user.Throttle.Total = 600000;
            m_user.Throttle.Land = 80000;
            m_user.Throttle.Task = 200000;
            m_user.Throttle.Texture = 100000;
            m_user.Throttle.Wind = 10000;
            m_user.Throttle.Resend = 100000;
            m_user.Throttle.Asset = 100000;
            m_user.Throttle.Cloud = 10000;
            m_user.Self.Movement.Camera.Far = 64f;
            m_user.Self.Movement.Camera.Position = m_user.Self.RelativePosition;
            SetHeightWidth(768, 1024);
            if (OnSimulatorConnected != null)
            {
                OnSimulatorConnected(new VSimulator(sender));
            }
        }
        private void loginCallback(LoginStatus status, string message)
        {
            if (OnLoggedIn != null)
            {
                OnLoggedIn((LoginStatus)status, message);
            }
        }
        public void Logout()
        {
            if (m_user.Network.Connected)
            {
                m_user.Network.Logout();
            }
        }
        public void disconnectedCallback(NetworkManager.DisconnectType reason, string message)
        {
            m_log.ErrorFormat("[CONNECTION]: Disconnected{0}: Message:{1}",reason.ToString(), message);
        }
        public bool Connected
        {
            get { return m_user.Network.Connected; }
        }
        public void Whisper(string message)
        {
            m_user.Self.Chat(message, 0, ChatType.Whisper);
        }

        public void Say(string message)
        {
            m_user.Self.Chat(message, 0, ChatType.Normal);
        }

        public void Shout(string message)
        {
            m_user.Self.Chat(message, 0, ChatType.Shout);
        }

        public void Teleport(string region, float x, float y, float z)
        {
            m_user.Self.Teleport(region, new Vector3(x, y, z));
        }

        private LoginParams getLoginParams(string loginURI, string username, string password, string startlocation)
        {
            string firstname;
            string lastname;

            Util.separateUsername(username, out firstname, out lastname);

            LoginParams loginParams = m_user.Network.DefaultLoginParams(
                firstname, lastname, password, "IdealistViewer", "0.0.0.1");//Constants.Version);


            loginURI = Util.getSaneLoginURI(loginURI);
            
            if (startlocation.Length == 0)
            {

                if (!loginURI.EndsWith("/"))
                    loginURI += "/";

                string[] locationparse = loginURI.Split('/');
                try
                {
                    startlocation = locationparse[locationparse.Length - 2];
                    if (startlocation == locationparse[2])
                    {
                        startlocation = "last";
                    }
                    else
                    {
                        loginURI = "";
                        for (int i = 0; i < locationparse.Length - 2; i++)
                        {
                            loginURI += locationparse[i] + "/";
                        }
                    }

                }
                catch (Exception)
                {
                    startlocation = "last";
                }

            }
            else
            {
                

                //if (!loginURI.EndsWith("/"))
                //    loginURI += "/";

               // string[] locationparse = loginURI.Split('/');
               // try
               // {
               //     string end = locationparse[locationparse.Length - 2];
               //     if (end != locationparse[2])
               //     {
               //         loginURI = "";
               //         for (int i = 0; i < 3; i++)
               //         {
               //             if (locationparse[i].Length != 0 || i==1)
               //                 loginURI += locationparse[i] + "/";
               //         }
               //     }

                //}
               // catch (Exception)
                //{
                    //startlocation = "last";
                //    m_log.Warn("[URLPARSING]: Unable to parse URL provided!");
                //}


            }

            loginParams.URI = loginURI;
           

            if (startlocation != "last" && startlocation != "home")
                startlocation = "uri:" + startlocation + "&128&128&20";

            loginParams.Start = startlocation;

            return loginParams;
        }
        
        private void chatCallback(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourcetype,
                                  string fromName, UUID id, UUID ownerid, Vector3 position)
        {
            // This is weird -- we get start/stop typing chats from
            // other avatars, and we get messages back that we sent.
            // (Tested on OpenSim r3187)
            // So we explicitly check for those cases here.
            if ((int)type < 4 && id != m_user.Self.AgentID)
            {
                m_log.Debug("Chat: " + fromName + ": " + message);
                if (OnChat != null)
                {
                    OnChat(message, audible, type, sourcetype,
                                      fromName, id, ownerid, position);
                }
            }
        }
        
        

        private void newPrim(Simulator simulator, Primitive prim, ulong regionHandle,
                             ushort timeDilation)
        {
            
            if (OnObjectAdd != null)
            {
                OnObjectAdd(new VSimulator(simulator), prim, regionHandle, timeDilation);
            }
        }

        
        private void landPatchCallback(Simulator simulator, int x, int y, int width, float[] data)
        {
            if (OnLandUpdate != null)
            {
                OnLandUpdate(new VSimulator(simulator), x, y, width, data);
            }
        }
        private void newAvatarCallback(Simulator simulator, Avatar avatar, ulong regionHandle,
                                       ushort timeDilation)
        {
            if (OnAvatarAdd != null)
            {
               //avatar.Velocity
                OnAvatarAdd(new VSimulator(simulator), avatar, regionHandle, timeDilation);
            }
        }
        private void objectUpdatedCallback(Simulator simulator, ObjectUpdate update, ulong regionHandle,
                                          ushort timeDilation)
        {
            if (OnObjectUpdate != null)
            {
                OnObjectUpdate(new VSimulator(simulator), update, regionHandle, timeDilation);
            }
        }

        public void RequestTexture(UUID assetID)
        {
            m_user.Assets.RequestImage(assetID, ImageType.Normal);
        }

        public void SendCameraViewMatrix(Vector3[] camdata)
        {

            for (int i=0;i<camdata.Length; i++)
                if (Single.IsNaN(camdata[i].X) || Single.IsNaN(camdata[i].Y) || Single.IsNaN(camdata[i].Z))
                    return;

                //m_user.Self.Movement.Camera.Position = pPosition;
                //m_user.Self.Movement.Camera.LookAt(pPosition, pTarget);
            m_user.Self.Movement.Camera.AtAxis = camdata[1];
            m_user.Self.Movement.Camera.LeftAxis = camdata[0];
            m_user.Self.Movement.Camera.LeftAxis = camdata[2];
            
        }
        
        public void SetHeightWidth(uint height, uint width)
        {
            m_user.Self.SetHeightWidth((ushort)height, (ushort)width);
        }

        public UUID GetSelfUUID
        {
            get { return m_user.Self.AgentID; }
        }

        public bool StraffLeft
        {
            set {m_user.Self.Movement.LeftPos = value;}
            get { return m_user.Self.Movement.LeftPos; }
        }
        public bool StraffRight
        {
            set { m_user.Self.Movement.LeftNeg = value; }
            get { return m_user.Self.Movement.LeftNeg; }
        }

        public void UpdateFromHeading(double heading)
        {
            m_user.Self.Movement.UpdateFromHeading(heading ,false);
        }

        public void TurnToward(Vector3 target)
        {
            m_user.Self.Movement.TurnToward(target);
        }

        public bool Forward
        {
            set {m_user.Self.Movement.AtPos = value;}
            get { return m_user.Self.Movement.AtPos; }
        }

        public bool Backward
        {
            set { m_user.Self.Movement.AtNeg = value; }
            get { return m_user.Self.Movement.AtNeg; }
        }

        public bool Jump
        {
            set { m_user.Self.Jump(value); }
        }

        public bool Flying
        {
            get { return m_user.Self.Movement.Fly; }
            set { m_user.Self.Movement.Fly = value; }
        }

        public bool Up
        {
            get { return m_user.Self.Movement.UpPos; }
            set { m_user.Self.Movement.UpPos = value; }
        }

        public bool Down
        {
            get { return m_user.Self.Movement.UpNeg; }
            set { m_user.Self.Movement.UpNeg = value; }
        }

        public bool MultipleSims
        {
            get
            {
                return m_user.Settings.MULTIPLE_SIMS;
            }
            set
            {
                m_user.Settings.MULTIPLE_SIMS=value;
            }
        }


        #region IProtocol Members


        public void Process()
        {
        }

        #endregion
    }
}
