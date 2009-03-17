using System;
using System.Collections.Generic;
using OpenMetaverse;
using IdealistViewer.Scene;

namespace IdealistViewer.Network
{

    public delegate void NetworkConnectedDelegate();
    public delegate void NetworkChatDelegate(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourcetype, string fromName, UUID id, UUID ownerid, Vector3 position);
    public delegate void NetworkLandUpdateDelegate(VSimulator simHandle, int x, int y, int width, float[] data);
    public delegate void NetworkAvatarAddDelegate(VSimulator simHandle, Avatar avatar, ulong regionHandle, ushort timeDilation);
    public delegate void NetworkObjectAddDelegate(VSimulator simHandle, Primitive prim, ulong regionHandle, ushort timeDilation);
    public delegate void NetworkLoginDelegate(LoginStatus status, string message);
    public delegate void NetworkObjectRemoveDelegate(VSimulator simHandle, uint objectID);
    public delegate void NeworkObjectUpdateDelegate(VSimulator simHandle, ObjectUpdate update, ulong regionHandle, ushort timeDilation);
    public delegate void NetworkSimulatorConnectedDelegate(VSimulator simHandle);
    public delegate void NetworkTextureDownloadedDelegate(VTexture tex);
    public delegate void NetworkFriendsListUpdateDelegate();

    public interface INetworkInterface
    {
        void Login(string loginURI, string username, string password, string startlocation);
        void Logout();

        void AvatarAnimationHandler(OpenMetaverse.Packets.Packet packet, OpenMetaverse.Simulator sim);

        event NetworkConnectedDelegate OnConnected;
        event NetworkSimulatorConnectedDelegate OnSimulatorConnected;
        event NetworkLoginDelegate OnLoggedIn;
        event NetworkAvatarAddDelegate OnAvatarAdd;
        event NetworkObjectAddDelegate OnObjectAdd;
        event NeworkObjectUpdateDelegate OnObjectUpdate;
        event NetworkObjectRemoveDelegate OnObjectRemove;
        event NetworkChatDelegate OnChat;
        event NetworkFriendsListUpdateDelegate OnFriendsListUpdate;
        event NetworkTextureDownloadedDelegate OnTextureDownloaded;
        event NetworkLandUpdateDelegate OnLandUpdate;

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
        void SendCameraViewMatrix(OpenMetaverse.Vector3[] camdata);
        void SetHeightWidth(uint height, uint width);
        void Shout(string message);
        void Teleport(string region, float x, float y, float z);
        void TurnToward(OpenMetaverse.Vector3 target);
        void UpdateFromHeading(double heading);
        void Whisper(string message);

        void Process();
    }
}
