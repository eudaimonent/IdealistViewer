using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace IdealistViewer
{
    public interface IProtocol
    {
        void BeginLogin(string loginURI, string username, string password, string startlocation);
        void loginStatusCallback(OpenMetaverse.LoginStatus login, string message);
        void Logout();

        void disconnectedCallback(OpenMetaverse.NetworkManager.DisconnectType reason, string message);
        void AvatarAnimationHandler(OpenMetaverse.Packets.Packet packet, OpenMetaverse.Simulator sim);
        
        event SLProtocol.Chat OnChat;
        event SLProtocol.FriendsListchanged OnFriendsListChanged;
        event SLProtocol.GridConnected OnGridConnected;
        event SLProtocol.ImageReceived OnImageReceived;
        event SLProtocol.LandPatch OnLandPatch;
        event SLProtocol.Login OnLogin;
        event SLProtocol.NewAvatar OnNewAvatar;
        event SLProtocol.NewPrim OnNewPrim;
        event SLProtocol.ObjectKilled OnObjectKilled;
        event SLProtocol.ObjectUpdated OnObjectUpdated;
        event SLProtocol.SimConnected OnSimConnected;

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
    }
}
