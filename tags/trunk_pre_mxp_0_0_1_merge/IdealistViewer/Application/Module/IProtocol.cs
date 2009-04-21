using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace IdealistViewer.Module
{
    public delegate void GridConnected();
    public delegate void Chat(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourcetype,
                              string fromName, UUID id, UUID ownerid, Vector3 position);
    public delegate void LandPatch(VSimulator simHandle, int x, int y, int width, float[] data);
    public delegate void NewAvatar(VSimulator simHandle, Avatar avatar, ulong regionHandle,
                                   ushort timeDilation);
    public delegate void NewPrim(VSimulator simHandle, Primitive prim, ulong regionHandle,
                                   ushort timeDilation);
    public delegate void Login(LoginStatus status, string message);
    public delegate void ObjectKilled(VSimulator simHandle, uint objectID);
    public delegate void ObjectUpdated(VSimulator simHandle, ObjectUpdate update, ulong regionHandle, ushort timeDilation);
    public delegate void SimConnected(VSimulator simHandle);
    public delegate void ImageReceived(AssetTexture tex);
    public delegate void FriendsListchanged();

    public interface IProtocol
    {
        void BeginLogin(string loginURI, string username, string password, string startlocation);
        void Logout();

        void AvatarAnimationHandler(OpenMetaverse.Packets.Packet packet, OpenMetaverse.Simulator sim);
        
        event Chat OnChat;
        event FriendsListchanged OnFriendsListChanged;
        event GridConnected OnGridConnected;
        event ImageReceived OnImageReceived;
        event LandPatch OnLandPatch;
        event Login OnLogin;
        event NewAvatar OnNewAvatar;
        event NewPrim OnNewPrim;
        event ObjectKilled OnObjectKilled;
        event ObjectUpdated OnObjectUpdated;
        event SimConnected OnSimConnected;

        string LoginURI { get; }
        string FirstName { get; }
        string LastName { get; }
        string UserName { get; }
        string Password { get; }
        string StartLocation { get; }

        bool Backward { get; set; }
        bool MultipleSims { get; set; }
        bool Connected { get; }
        bool Up { get; set; }
        bool Down { get; set; }
        bool StraffLeft { get; set; }
        bool StraffRight { get; set; }
        bool Flying { get; set; }
        bool Forward { get; set; }
        bool Jump { set; }
        UUID GetSelfUUID { get; }
        Dictionary<OpenMetaverse.UUID, OpenMetaverse.FriendInfo> Friends { get; }
        Dictionary<UUID, List<UUID>> AvatarAnimations { get; }

        void RequestTexture(OpenMetaverse.UUID assetID);
        void Say(string message);
        void SetCameraPosition(OpenMetaverse.Vector3[] camdata);
        void SetHeightWidth(uint height, uint width);
        void Shout(string message);
        void Teleport(string region, float x, float y, float z);
        void TurnToward(OpenMetaverse.Vector3 target);
        void UpdateFromHeading(double heading);
        void Whisper(string message);

        void Process();
    }
}
