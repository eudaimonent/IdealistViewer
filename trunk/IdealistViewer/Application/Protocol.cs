using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using log4net;


namespace IdealistViewer
{
    public class SLProtocol
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
            //m_user.Settings.MULTIPLE_SIMS = false;
            //m_user.Settings.OBJECT_TRACKING = true;
            //m_user.Settings.AVATAR_TRACKING = true;
            m_user.Settings.USE_TEXTURE_CACHE = false;
            //m_user.Settings.
            m_user.Settings.ALWAYS_DECODE_OBJECTS = false;
            
            m_user.Settings.SEND_AGENT_THROTTLE = true;
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

        public void BeginLogin(string loginURI, string username, string password, string startlocation)
        {
            LoginParams loginParams = getLoginParams(loginURI, username, password, startlocation);

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
            m_user.Throttle.Total = 600000;
            m_user.Throttle.Land = 80000;
            m_user.Throttle.Task = 200000;
            m_user.Throttle.Texture = 150000;
            m_user.Throttle.Wind = 10000;
            m_user.Throttle.Resend = 100000;
            m_user.Throttle.Asset = 100000;
            m_user.Throttle.Cloud = 10000;
            m_user.Self.Movement.Camera.Far = 64f;
            m_user.Self.Movement.Camera.Position = m_user.Self.RelativePosition;
            SetHeightWidth(768, 1024);
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

        public void SetCameraPosition(Vector3[] camdata)
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

        


    }
}
