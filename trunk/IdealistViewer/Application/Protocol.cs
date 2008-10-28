using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Rendering;

namespace IdealistViewer
{
    public class SLProtocol
    {
        public delegate void GridConnected();
        public delegate void Chat(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourcetype,
                                  string fromName, UUID id, UUID ownerid, Vector3 position);
        public delegate void LandPatch(Simulator sim, int x, int y, int width, float[] data);
        public delegate void NewAvatar(Simulator sim, Avatar avatar, ulong regionHandle,
                                       ushort timeDilation);
        public delegate void NewPrim(Simulator sim, Primitive prim, ulong regionHandle,
                                       ushort timeDilation);
        public delegate void Login(LoginStatus status, string message);
        public delegate void ObjectKilled(Simulator sim, uint objectID);
        public delegate void ObjectUpdated(Simulator simulator, ObjectUpdate update, ulong regionHandle, ushort timeDilation);
        public delegate void SimConnected(Simulator sim);
        public delegate void ImageReceived(AssetTexture tex);

        public event NewAvatar OnNewAvatar;
        public event Chat OnChat;
        public event GridConnected OnGridConnected;
        public event SimConnected OnSimConnected;
        public event LandPatch OnLandPatch;
        public event Login OnLogin;
        public event NewPrim OnNewPrim;
        public event ObjectKilled OnObjectKilled;
        public event ObjectUpdated OnObjectUpdated;
        public event ImageReceived OnImageReceived;

        GridClient m_user;
        public SLProtocol()
        {
            m_user = new GridClient();
            //m_user.Settings.STORE_LAND_PATCHES = true;
            m_user.Settings.MULTIPLE_SIMS = true;
            m_user.Settings.OBJECT_TRACKING = true;
            m_user.Settings.AVATAR_TRACKING = true;
            m_user.Settings.USE_TEXTURE_CACHE = false;
            //m_user.Settings.
            m_user.Settings.ALWAYS_DECODE_OBJECTS = true;
           
            //m_user.Settings.SEND_AGENT_THROTTLE = true;
            //m_user.Settings.SEND_PINGS = true;

            m_user.Network.OnConnected += gridConnectedCallback;
            m_user.Network.OnSimConnected += simConnectedCallback;
            m_user.Terrain.OnLandPatch += landPatchCallback;
            m_user.Self.OnChat += chatCallback;
            m_user.Objects.OnNewAvatar += newAvatarCallback;
            m_user.Objects.OnNewPrim += newPrim;
            m_user.Objects.OnObjectKilled += objectKilledCallback;
            m_user.Network.OnLogin += loginCallback;
            m_user.Objects.OnObjectUpdated += objectUpdatedCallback;
            m_user.Assets.OnImageReceived += imageReceivedCallback;
            //m_user.Assets.RequestImage(
            //m_user.Assets.Cache..RequestImage(UUID.Zero, ImageType.Normal);

        }
        private void imageReceivedCallback(ImageDownload image, AssetTexture asset)
        {
            if (OnImageReceived != null)
            {
                OnImageReceived(asset);
            }
        }
        private void objectKilledCallback(Simulator simulator, uint objectID)
        {
            if (OnObjectKilled != null)
            {
                OnObjectKilled(simulator, objectID);
            }
        }

        public void BeginLogin(string loginURI, string username, string password)
        {
            LoginParams loginParams = getLoginParams(loginURI, username, password);

            m_user.Network.BeginLogin(loginParams);
        }
        private void gridConnectedCallback(object sender)
        {
           
            m_user.Appearance.SetPreviousAppearance(false);



            if (OnGridConnected != null)
            {
                OnGridConnected();
            }
        }
        private void simConnectedCallback(Simulator sender)
        {
            m_user.Throttle.Total = 500000;
            m_user.Throttle.Land = 80000;
            m_user.Throttle.Task = 200000;
            m_user.Throttle.Texture = 150000;
            m_user.Throttle.Wind = 10000;
            m_user.Throttle.Resend = 100000;
            m_user.Throttle.Asset = 100000;
            m_user.Throttle.Cloud = 10000;
           
            
            if (OnSimConnected != null)
            {
                OnSimConnected(sender);
            }
        }
        private void loginCallback(LoginStatus status, string message)
        {
            if (OnLogin != null)
            {
                OnLogin((LoginStatus)status, message);
            }
        }
        public void Logout()
        {
            if (m_user.Network.Connected)
            {
                m_user.Network.Logout();
            }
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

        private LoginParams getLoginParams(string loginURI, string username, string password)
        {
            string firstname;
            string lastname;

            Util.separateUsername(username, out firstname, out lastname);

            LoginParams loginParams = m_user.Network.DefaultLoginParams(
                firstname, lastname, password, "IdealistViewer", "0.0.0.1");//Constants.Version);

            loginParams.URI = Util.getSaneLoginURI(loginURI);

            return loginParams;
        }
        
        private void chatCallback(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourcetype,
                                  string fromName, UUID id, UUID ownerid, Vector3 position)
        {
            // This is weird -- we get start/stop typing chats from
            // other avatars, and we get messages back that we sent.
            // (Tested on OpenSim r3187)
            // So we explicitly check for those cases here.
            if (OnChat != null && (int)type < 4 && id != m_user.Self.AgentID)
            {
                OnChat(message, audible, type, sourcetype,
                                  fromName, id, ownerid, position);
            }
        }
        
        

        private void newPrim(Simulator simulator, Primitive prim, ulong regionHandle,
                             ushort timeDilation)
        {
            
            if (OnNewPrim != null)
            {
                OnNewPrim(simulator,prim, regionHandle,timeDilation);
            }
        }

        
        private void landPatchCallback(Simulator simulator, int x, int y, int width, float[] data)
        {
            if (OnLandPatch != null)
            {
                OnLandPatch(simulator,x, y, width, data);
            }
        }
        private void newAvatarCallback(Simulator simulator, Avatar avatar, ulong regionHandle,
                                       ushort timeDilation)
        {
            if (OnNewAvatar != null)
            {
               //avatar.Velocity
                OnNewAvatar(simulator,avatar,regionHandle,timeDilation);
            }
        }
        private void objectUpdatedCallback(Simulator simulator, ObjectUpdate update, ulong regionHandle,
                                          ushort timeDilation)
        {
            if (OnObjectUpdated != null)
            {
                OnObjectUpdated(simulator, update, regionHandle, timeDilation);
            }
        }

        public void RequestTexture(UUID assetID)
        {
            m_user.Assets.RequestImage(assetID, ImageType.Normal);
        }
    }
}
