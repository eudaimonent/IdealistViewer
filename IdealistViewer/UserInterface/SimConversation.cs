using System;
using System.Collections.Generic;
using System.Text;
using IdealistViewer.Network;
using System.Threading;
using OpenMetaverse;
using jabber.protocol.client;

namespace IdealistViewer.UserInterface
{
    /// <summary>
    /// Handle conversations that are mediated by the Region Simulator.
    /// </summary>
    public class SimConversation : ConversationComm
    {
        INetworkInterface m_simConnection;

        public SimConversation(Forms.frmConversation c, INetworkInterface net)
        {
            m_conForm = c;
            m_simConnection = net;
            // Subscribe to sim chat events
            net.OnChat += new NetworkChatDelegate(OnChat);
        }

        public override void Say( string msg )
        {
            m_simConnection.Say(msg);
        }

        public void avatarConnection_OnCloseIM()
        {
        }

        /// <summary>
        /// Process an arrived local chat message.
        /// </summary>
        /// <param name="message">Text of the chat line</param>
        /// <param name="audible">How loud did we hear it (a clue to distance)</param>
        /// <param name="type">Say, Shout, etc</param>
        /// <param name="sourcetype"></param>
        /// <param name="fromName">Who said it</param>
        /// <param name="id"></param>
        /// <param name="ownerid"></param>
        /// <param name="position">Region coordinates</param>
        void OnChat(
            string message,
            OpenMetaverse.ChatAudibleLevel audible,
            OpenMetaverse.ChatType type,
            OpenMetaverse.ChatSourceType sourcetype,
            string fromName,
            OpenMetaverse.UUID id,
            OpenMetaverse.UUID ownerid,
            OpenMetaverse.Vector3 position)
        {
            m_conForm.AddChat( fromName, message, type, audible, sourcetype );
        }

        public override void close()
        {
        }
    }
}
