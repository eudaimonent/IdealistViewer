using System;
using System.Collections.Generic;
using System.Text;
using IdealistViewer.Network;
using jabber.client;

namespace IdealistViewer.UserInterface
{
    public class XMPPConversation : ConversationComm
    {
        private JabberClient xmppConnection;
        private string m_partner;
        private jabber.JID m_jid;

        public XMPPConversation(Forms.frmConversation c, string jid, JabberClient net)
        {
            m_conForm = c;
            xmppConnection = net;
            m_jid = new jabber.JID(jid);
        }

        public override void Say( string msg )
        {
            xmppConnection.Message( m_jid, msg );
        }

        public override void close()
        {
        }
    }
}
