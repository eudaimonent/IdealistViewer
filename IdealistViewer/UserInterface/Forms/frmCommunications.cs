using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
//using System.IO;
using System.Xml;
using OpenMetaverse;
using IdealistViewer.Network;
using IdealistViewer.UserInterface;
using jabber.protocol.client;
using jabber.protocol.iq;
using muzzle;
using Nini.Config;

namespace IdealistViewer
{
    public partial class frmCommunications : Form
    {
        public static jabber.connection.DiscoManager dMgr;
        private const string SIMGROUP = "InWorld";
        private INetworkInterface avatarConnection;
        /// <summary>
        /// Private conversations over Sim channels.
        /// </summary>
        private Dictionary<UUID, UserInterface.Forms.frmConversation> m_IMconversations;
        /// <summary>
        /// Private conversations over XMPP channels.
        /// </summary>
        private Dictionary<string, UserInterface.Forms.frmConversation> m_XMPPconversations;
        /// <summary>
        /// The one and only local chat conversation.
        /// </summary>
        private UserInterface.Forms.frmConversation m_localChat;
        private IConfigSource m_xmppConfig;
        private ContextMenu m_tempMenu;

        /// <summary>
        /// Create the top level communications user interface window
        /// </summary>
        /// <param name="avatarConnection">Connection to a world simulator</param>
        /// <param name="xmpp">Connection to an IM server</param>
        public frmCommunications( INetworkInterface avatarConnection)
        {
            this.avatarConnection = avatarConnection;

            InitializeComponent();
            dMgr = discoManager;

            // The two dictionaries of dynamic conversations.
            m_IMconversations =
                new Dictionary< UUID, UserInterface.Forms.frmConversation >();
            m_XMPPconversations =
                new Dictionary<string, UserInterface.Forms.frmConversation>(); ;

            // Put the local chat conversation into the permanent Local Chat Tab.
            m_localChat =
                new UserInterface.Forms.frmConversation( LocalChatTab.Text, this );
            ConversationComm com =
                (ConversationComm)new SimConversation(m_localChat, avatarConnection);
            m_localChat.ConnectToComm(com);
            m_localChat.Dock = System.Windows.Forms.DockStyle.Fill;
            LocalChatTab.Controls.Add( m_localChat );

            rosterTree.AllowDrop = false;
            groupTree.AllowDrop = false;

            // Load the XMPP settings.
            LoadXMPPConfig();

            avatarConnection.OnFriendsListUpdate +=
                new NetworkFriendsListUpdateDelegate(avatarConnection_OnFriendsListChanged);
        }

        /// <summary>
        /// Load all the XMPP configuration for conferences, etc.
        /// </summary>
        private void LoadXMPPConfig()
        {
            string iniconfig = System.IO.Path.Combine("../../..", "IdealistJabber.ini");
            if (!System.IO.File.Exists(iniconfig))
            {
                System.Console.WriteLine("XMPP disabled - no INI file");
                return;
            }

           // Connect the Jabber client if the base XMPP config is supplied.
            m_xmppConfig = new IniConfigSource(iniconfig);
            IConfig baseCfg = m_xmppConfig.Configs["XMPP"];
            if (baseCfg == null)
            {
                System.Console.WriteLine("XMPP disabled - no XMPP section");
                m_xmppConfig = null;
                return;
            }

            // Get all the basic connection information.
            JC.User = baseCfg.GetString("username", "");
            JC.Server = baseCfg.GetString("server", "jabber.org");
            JC.Password = baseCfg.GetString("password");
            JC.NetworkHost = baseCfg.GetString("host", JC.Server);
            JC.Resource = baseCfg.GetString("resource", "Idealist");

            // Tell the JabberClient to connect.  This will also
            // populate the Friends tree with XMPP roster items.
            JC.Connect();

        }

        /// <summary>
        /// Handler for arrival of Sim friend definitions.
        /// </summary>
        /// <remarks>Sim friends are displayed on the same list as are
        /// XMPP friends, under a special group named "InWorld".</remarks>
         void avatarConnection_OnFriendsListChanged()
        {
             // This modifes the roster tree, so we do the work in its
             // creator thread.
             rosterTree.BeginInvoke((ThreadStart)delegate()
             {
                 rosterTree.BeginUpdate();

                // Copy the Sim's friend-list into this group.
                avatarConnection.Friends.ForEach(delegate(FriendInfo friend)
                {
                    if (friend.Name != null)
                    {
                        RosterIQ riq = new RosterIQ(new XmlDocument());
                        riq.Type = IQType.set;
                        Roster r = riq.Instruction;
                        Item i = r.AddItem();
                        i.JID = new jabber.JID(friend.Name.Replace(" ", "."),
                            friend.UUID.ToString(), // TODO Hack of hiding UUID in server name
                            null);
                        i.Nickname = friend.Name;
                        i.Subscription = Subscription.both;

                        /*
                        // Set the presence indicator from the Sim onLine status.
                        System.Xml.XmlDocument presDoc = new System.Xml.XmlDocument();
                        Presence p = new Presence(presDoc);
                        if (friend.IsOnline)
                                                {
                                                    p.Type = PresenceType.available;
                                                    p.Status = "online";
                                                }
                                                else
                                                {
                                                    p.Type = PresenceType.unavailable;
                                                    p.Status = "offline";
                                                }
                                                node.ChangePresence(p);
                         */
                        // TODO different ICON for UUID vs JID friends and conferences.
                        RosterMgr.AddRoster(riq);

                    }
                    rosterTree.EndUpdate();
                });
            });
        }

        /// <summary>
        /// Process an arrived Instant Message from the Region Sim
        /// </summary>
        /// <param name="im">The message itself</param>
        /// <param name="simulator">Where it came from</param>
        public void avatarConnection_OnInstantMessage(InstantMessage im, Simulator simulator)
        {
            // First we have to determine whether we have heard from the Sender before.
            // If not, we need to create a new conversation.
            UserInterface.Forms.frmConversation c =
                LookupIMSession(im.IMSessionID, im.FromAgentName);
 
            c.AddChat( im.FromAgentName, im.Message,
                ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.Agent );
        }

        /// <summary>
        /// Process an arrived Instant Message from XMPP
        /// </summary>
        /// <param name="im">The message itself</param>
        public void XMPP_OnInstantMessage( jabber.protocol.client.Message message )
        {
            string label = ".";
            string session = ".";
            string who = ".";
            switch (message.Type)
            {
                case MessageType.chat:
                case MessageType.normal:
                    // For personal chat the session is the full JID, the
                    // label AND the who are the account name.
                    session = message.From.Bare;
                    label = message.From.User;
                    who = label;
                    break;
                case MessageType.groupchat:
                    // For group chat the session is all but the nick,
                    // the label is the room name, and the nick is who spoke.
                    session = message.From.Bare;
                    label = message.From.User;
                    who = message.From.Resource;
                    break;
                case MessageType.headline:
                    System.Console.WriteLine(message.Body);
                    break;
                case MessageType.error:
                    System.Console.WriteLine(message.Body);
                    break;
            }

            // First we have to determine whether we have heard from this session before.
            // If not, we need to create a new conversation.
            UserInterface.Forms.frmConversation c = LookupXMPPSession(session, label); ;

            c.AddChat( message );
        }

        /// <summary>
        /// Add a conversation to the tabbed main form.
        /// </summary>
        /// <param name="c"></param>
        /// <remarks>All conversations start out docked.  They can be undocked
        /// later if desired.</remarks>
        public void AddDockedConversation(UserInterface.Forms.frmConversation c)
        {
            // Create a new tab page to hold this conversation and bring
            // it to the front.
            commTabControl.BeginInvoke((ThreadStart)delegate()
            {
                UserInterface.frmDockedConversation dc = new UserInterface.frmDockedConversation(c);
                commTabControl.Controls.Add(dc);
                commTabControl.SelectedTab = (TabPage)dc;
            });
        }

        /// <summary>
        /// Find or create an XMPP session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        private UserInterface.Forms.frmConversation LookupXMPPSession(string session, string label)
        {
            if (m_XMPPconversations.ContainsKey(session))
            {
                return m_XMPPconversations[session];
            }
            else
            {
                // Create the visual aspect of the conversation.
                UserInterface.Forms.frmConversation c =
                    new UserInterface.Forms.frmConversation(label, this);

                // Connect it to the network interface.
                ConversationComm comm = (ConversationComm)new XMPPConversation(c, session, JC);
                c.ConnectToComm(comm);
                m_XMPPconversations[session] = c;
                AddDockedConversation(c);
                return c;
            }
        }

        /// <summary>
        /// Find or create a chat/IM session to the Simulator.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        private UserInterface.Forms.frmConversation LookupIMSession(UUID session, string label)
        {
            if (m_IMconversations.ContainsKey(session))
            {
                return m_IMconversations[session];
            }
            else
            {
                // Create the visual aspect of the conversation.
                UserInterface.Forms.frmConversation c =
                    new UserInterface.Forms.frmConversation(label, this);

                // Connect it to the network interface.
                ConversationComm comm =
                    (ConversationComm)new SimConversation(c, avatarConnection);
                m_IMconversations[session] = c;
                c.ConnectToComm(comm);
                AddDockedConversation(c);
 
                return c;
            }
        }

        /// <summary>
        /// Handle all XMPP "messages".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        /// <remarks>We match the message up with an existing conversation, or
        /// create a new one.  Various attributes of the message will determine
        /// how the message is presented.
        /// </remarks>
       private void JC_OnMessage(object sender, jabber.protocol.client.Message msg)
        {
            string label = ".";
            switch (msg.Type)
            {
                case MessageType.chat:
                case MessageType.normal:
                    // For personal chat the session is the full JID, the
                    // label AND the who are the account name.
                    label = msg.From.User;
                    LookupXMPPSession(msg.From.Bare, label).AddChat(
                        label,
                        msg.Body,
                        ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.Agent );
                    break;

                case MessageType.groupchat:
                    // For group chat the session is all but the nick,
                    // the label is the room name, and the nick is who spoke.
                    LookupXMPPSession(msg.From.Bare, msg.From.User).AddChat(
                        msg.From.Resource,
                        msg.Body,
                        ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.Agent );
                    break;

                case MessageType.headline:
                    // Headlines just go to the chat window
                    m_localChat.AddChat(
                        msg.From.ToString(),
                        msg.Body,
                        ChatType.Normal,
                        ChatAudibleLevel.Fully,
                        ChatSourceType.Object);
                    break;

                case MessageType.error:
                    // Error messages just go to the chat window
                    m_localChat.AddChat(
                        msg.From.ToString(),
                        msg.Body,
                        ChatType.OwnerSay,
                        ChatAudibleLevel.Fully,
                        ChatSourceType.Object);
                    break; ;
            }
       }

       private bool JC_OnInvalidCertificate(object sender,
           System.Security.Cryptography.X509Certificates.X509Certificate certificate,
           System.Security.Cryptography.X509Certificates.X509Chain chain,
           System.Net.Security.SslPolicyErrors sslPolicyErrors)
       {
           return true;  // Blindly approve dubious certificates.
       }

        /// <summary>
        /// Notice that we have connected to the XMPP server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="stream"></param>
        private void JC_OnConnect(object sender, jabber.connection.StanzaStream stream)
        {
           m_localChat.AddChat("XMPP", "connected",
               ChatType.Normal, ChatAudibleLevel.Barely, ChatSourceType.System);
        }

        /// <summary>
        /// Notice that the XMPP server accepts us.
        /// </summary>
        /// <param name="sender"></param>
        private void JC_OnAuthenticate(object sender)
        {
            m_localChat.AddChat("XMPP", "authenticated",
               ChatType.Normal, ChatAudibleLevel.Barely, ChatSourceType.System);

            LoadConferences();
       }

        /// <summary>
        /// Load Jabber conference definitions from the jabber.ini file.
        /// </summary>
        /// <remarks>Whle an XMPP server will remember our list of "friends" in our
        /// account "roster", it does not remember which Conferences we like to enter.
        /// So we store that list in a local .ini file.</remarks>
        private void LoadConferences()
        {
            // We look at all the sections in the
            // jabber.ini file with names starting "XMPPGroup-".  The rest
            // of the name is not important.
            if (m_xmppConfig == null) return;

            this.BeginInvoke((ThreadStart)delegate()
            {
                groupTree.BeginUpdate();
                string defaultNick = JC.User;

                jabber.protocol.iq.Group rg = new jabber.protocol.iq.Group( new XmlDocument());
                RosterTree.GroupNode gr = new RosterTree.GroupNode(rg);
                gr.Name = "Chatrooms";
                gr.Text = "Chatrooms";
                groupTree.Nodes.Add(gr);
                foreach (IConfig c in m_xmppConfig.Configs)
                {
                    if (c.Name.StartsWith("XMPPGroup-"))
                    {
                        jabber.protocol.iq.RosterIQ riq =
                            new jabber.protocol.iq.RosterIQ(new XmlDocument());
                        riq.Type = IQType.set;
                        jabber.protocol.iq.Roster r = riq.Instruction;
                        jabber.protocol.iq.Item i = r.AddItem();
                        i.JID = new jabber.JID(c.GetString("JID"));
                        i.Nickname = c.GetString("name", i.JID.User);
                        //i.Subscription = jabber.protocol.iq.Subscription.both;

                        GroupRoomNode itm = new GroupRoomNode(i);
                        itm.Text = c.GetString("name", i.JID.User);
                        itm.Nick = c.GetString("nick", defaultNick);
                        itm.Name = itm.Text;
                        gr.Nodes.Add(itm);
                    }
                }
                groupTree.EndUpdate();
                groupTree.ExpandAll();
                groupTree.Refresh();
             });
        }

       private void onItems(jabber.connection.DiscoManager mgr,
           jabber.connection.DiscoNode node, object arg)
       {
           m_localChat.AddLine(node.Name);
       }

       private void JC_OnError(object sender, Exception ex)
       {
           m_localChat.AddChat("XMPP", ex.Message,
               ChatType.Normal, ChatAudibleLevel.Barely, ChatSourceType.System);
       }

       private void JC_OnReadText(object sender, string txt)
       {
 //          m_localChat.AddChat("XMPP R", txt,
 //              ChatType.Debug, ChatAudibleLevel.Fully, ChatSourceType.System);
       }

       private void JC_OnWriteText(object sender, string txt)
       {
 //          m_localChat.AddChat("XMPP S", txt,
 //              ChatType.Debug, ChatAudibleLevel.Fully, ChatSourceType.System);

       }

        /// <summary>
        /// Test whether a JID is actually a Sim UUID in disguise.
        /// </summary>
        /// <param name="jid"></param>
        /// <returns>True if the JID represents an inworld UUID rather than an XMPP JID.</returns>
        /// <remarks>For Sim connections the server name is the UUID.  There should be a more
        /// robust way to flag a different connection type, which will require
        /// extending the RosterTree.ItemNode class.  For now we look for something
        /// that has the appearance of a UUID: 36 lower case hex digits and dashes.
        /// To be more precise, it should be xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
        /// but since it does NOT contain a dot it is unlikely to be a network domain name.
        /// </remarks>
        private bool isSimJid(jabber.JID jid)
        {
            if (jid == null) return false;
            if (jid.Server.Length!=36) return false;
            Match m = Regex.Match(jid.Server, "^([a-f0-9-]*)$");
            return m.Success;
        }

        /// <summary>
        /// Action routine for sending an IM to a chosen friend.
        /// </summary>
        /// <param name="sender">A MenuItem</param>
        /// <param name="arg"></param>
        private void onSendIM(object sender, EventArgs arg)
        {
            ClearTempMenu();
            RosterTree.ItemNode node = (RosterTree.ItemNode)rosterTree.SelectedNode;
            if (node.JID == null) return;

            if (isSimJid(node.JID))
            {
                // It appears to be a UUID, so initiate a Sim IM conversation
                LookupIMSession(new UUID(node.JID.Server), node.JID.User);
            }
            else
            {
                // It is not a UUID, so initiate an XMPP conversation
                LookupXMPPSession(node.JID.Bare, node.JID.User);
            }
        }

        /// <summary>
        /// Respond to double-click on a friend name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>This is the same as right-clicking and choosing "Send IM".
        /// </remarks>
        private void rosterTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TreeNode node = (TreeNode)rosterTree.GetNodeAt(e.Location);
            if (node is RosterTree.ItemNode)
            {
                onSendIM(sender, e);
            }
        }

        private void JC_OnAuthError(object sender, XmlElement rp)
        {
            m_localChat.AddChat("XMPP", "not authorized",
                ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.System);
        }

        /// <summary>
        /// Create or rediscover a conversation with an XMPP COnference Room
        /// </summary>
        /// <param name="jid">Room identifier</param>
        /// <param name="name">Pretty name to use in titles</param>
        public void JoinConference(jabber.JID jid, string name)
        {
            UserInterface.Forms.frmConversation c;
            if (m_XMPPconversations.ContainsKey(jid.Bare))
            {
                c = m_XMPPconversations[jid.Bare];
            }
            else
            {
                // Create the visual aspect of the conversation.
                c = new UserInterface.Forms.frmConversation(name, this);

                // Connect it to the network interface for XMPP
                // Multi User Chat.
                ConversationComm comm =
                    (ConversationComm)new RoomConversation(c,
                        jid,
                        conferenceManager,
                        JC);
                c.ConnectToComm(comm);
                m_XMPPconversations[jid.Bare] = c;

                // Create a new tab page to hold this conversation.
                AddDockedConversation(c);
            }
        }

        /// <summary>
        /// Join a conference or Group IM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>An implicit input is the group tree selected node.</remarks>
        private void onJoinRoom( object sender, EventArgs e)
        {
            ClearTempMenu();
            GroupRoomNode node = (GroupRoomNode)groupTree.SelectedNode;

            if (isSimJid(node.JID))
            {
                // It appears to be a UUID, so initiate a Sim IM conversation
                m_localChat.AddChat("Groups", "Joining group IM not yet implemented",
                        ChatType.Normal, ChatAudibleLevel.Fully, ChatSourceType.System);
            }
            else
            {
                // It is not a UUID, so initiate an XMPP conversation
                JoinConference(node.JID, node.Text);
            }
        }

        /// <summary>
        /// Handle right-click on a group tree node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void groupTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Select this node so the action routines can find it.
                groupTree.SelectedNode = e.Node;

                if (e.Node is RosterTree.ItemNode)
                {
                    m_tempMenu = new ContextMenu();
                    m_tempMenu.MenuItems.Add("Join", new EventHandler(onJoinRoom));

                    m_tempMenu.Show(groupTree, e.Location);
                }
            }
        }

        private void onHide(object o, EventArgs e)
        {
            ClearTempMenu();
            RosterTree.ItemNode i = (RosterTree.ItemNode)rosterTree.SelectedNode;
            m_localChat.AddLine(
                "Hiding from " + i.JID.ToString() + " NYI\n");
        }
        
        private void rosterTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
               if (e.Node is RosterTree.ItemNode)
                {
                    rosterTree.SelectedNode = e.Node;
                    this.BeginInvoke((ThreadStart)delegate()
                    {
                        // Put up context menu
                        ContextMenu m_tempMenu = new ContextMenu();
                        m_tempMenu.MenuItems.Add(new MenuItem("Send IM", new EventHandler(onSendIM)));
                        m_tempMenu.MenuItems.Add("Hide from", new EventHandler(onHide));
                        m_tempMenu.Show(rosterTree, e.Location);
                    });
                }
            }
        }

        private void ClearTempMenu()
        {
            if (m_tempMenu != null)
            {
                m_tempMenu.Dispose();
                m_tempMenu = null;
            }
        }

        /// <summary>
        /// Respond to double-click on a friend name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>This is the same as right-clicking and choosing "Send IM".
        /// </remarks>
        private void rosterTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Only pay attention to clicks on leaf nodes.
            if (sender is RosterTree.ItemNode)
            {
                onSendIM(sender, e);
            }

        }

        private void DrawGroup(DrawTreeNodeEventArgs e)
        {
            RosterTree.GroupNode node = (RosterTree.GroupNode)e.Node;
//            string counts = String.Format("({0}/{1})", node.Current, node.Total);

/*            if (node.IsSelected)
            {
                string newText = node.GroupName + " " + counts;
                e.DrawDefault = true;
                if (node.Text != newText)
                    node.Text = newText;
                return;
            }
*/
            Graphics g = e.Graphics;
            Brush fg = new SolidBrush(this.ForeColor);

            g.DrawString(node.Text,
                new Font( this.Font, FontStyle.Bold), fg,
                new Point(e.Bounds.Left, e.Bounds.Top),
                StringFormat.GenericTypographic);
 /*           if (node.Total > 0)
            {
                SizeF name_size = g.MeasureString(node.GroupName, this.Font);
                g.DrawString(counts, this.Font, stat_fg, new PointF(e.Bounds.Left + name_size.Width, e.Bounds.Top), StringFormat.GenericTypographic);
            } */
        }

        private void DrawItem(DrawTreeNodeEventArgs e)
        {
            RosterTree.ItemNode node = (RosterTree.ItemNode)e.Node;
            if (node.IsSelected)
            {
                e.DrawDefault = true;
                return;
            }

            Graphics g = e.Graphics;
            Brush fg = new SolidBrush(this.ForeColor);

            g.DrawString(node.Nickname,
                this.Font, fg,
                new Point(e.Bounds.Left, e.Bounds.Top),
                StringFormat.GenericTypographic);
        }

        
        private void groupTree_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node is RosterTree.GroupNode)
                DrawGroup(e);
            else if (e.Node is RosterTree.ItemNode)
                DrawItem(e);
            else
                e.DrawDefault = true; // or assert(false)

        }

    }
}
