using System;
using System.Collections.Generic;
using System.Text;
using jabber.connection;
using OpenMetaverse;

namespace IdealistViewer.UserInterface
{
    class RoomConversation : ConversationComm
    {
        private ConferenceManager m_manager;
        private jabber.client.JabberClient m_client;
        Forms.frmConversation m_conForm;
        private jabber.JID m_roomJID;
        private Room m_room;
        private string m_nick;

        public RoomConversation(
            Forms.frmConversation c,
            jabber.JID roomJID,
            ConferenceManager mgr,
            jabber.client.JabberClient client)
        {
            m_conForm = c;
            m_manager = mgr;
            m_client = client;
            m_roomJID = roomJID;

            // Default nick in this room is the account name.
            // TODO get this from  the GroupRoomNode
            m_nick = client.User;
            m_room = mgr.GetRoom(
                new jabber.JID( roomJID.User, roomJID.Server, m_nick ));
            if (m_room == null)
            {
                return;
            }

            // Handle room events
            m_room.OnJoin += new RoomEvent(OnJoin);
            m_room.OnParticipantJoin +=new RoomParticipantEvent(OnPartJoin);
            m_room.OnParticipantLeave += new RoomParticipantEvent(OnPartLeave);
//            m_room.OnRoomMessage += new jabber.client.MessageHandler(OnRoomMessage);
//            m_room.OnSelfMessage += new jabber.client.MessageHandler(OnRoomMessage);
            m_room.Join();
        }

        private void OnRoomMessage(object sender, jabber.protocol.client.Message m)
        {
            m_conForm.AddChat(m.From.Resource, m.Body,
                ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.Agent);
        }

        private void OnJoin( Room r )
        {
            m_conForm.AddChat(r.JID.User, "joined",
                ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.System);
            r.RetrieveListByRole(
                jabber.protocol.iq.RoomRole.participant,
                new RoomParticipantsEvent(OnPart),
                null);
        }
        
        private void OnPart( Room r, ParticipantCollection participants, object state )
        {
            if (participants == null) return;

            foreach (jabber.connection.RoomParticipant p in participants)
            {
                m_conForm.AddChat(r.JID.User,
                    p.Nick + " is already here",
                    ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.System);
            }
        }

        private void OnPartJoin(Room r, RoomParticipant p)
        {
            m_conForm.AddChat(r.JID.User, p.Nick+" has entered the room",
                ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.System);
        }

        private void OnPartLeave(Room r, RoomParticipant p)
        {
            m_conForm.AddChat(r.JID.User, p.Nick + " has left the room",
                ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.System);
        }

        /// <summary>
        /// Speak publically in a room
        /// </summary>
        /// <param name="text">What we say</param>
        public override void Say(string text)
        {
            m_room.PublicMessage(text);
        }

        public override void close()
        {
            m_room.Leave("");
        }
    }
}
