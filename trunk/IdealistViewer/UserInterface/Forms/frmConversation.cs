using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using OpenMetaverse;
using System.Threading;
using jabber.protocol.client;

namespace IdealistViewer.UserInterface.Forms
{
    /// <summary>
    /// A conversation is a single set of controls for talking with
    /// a specific group of people.
    /// </summary>
    /// <remarks>A conversation can be local chat in a simulator,
    /// private IM with another simulator user, private IM over a
    /// Jabber service, group IM within a simulator, or group
    /// (conference) chat through a Jabber service.  A conversation
    /// can be presented either docked as a tab in the main communication
    /// window, or as a free-floating window.</remarks>
    public partial class frmConversation : UserControl
    {
        /// <summary>
        /// The module that handles communications.  This hanldes the
        /// differences between individual and group chat, and Sim or
        /// Jabber technologies.
        /// </summary>
        private ConversationComm m_commModule;
        /// <summary>
        /// The base form for all communications.
        /// </summary>
        private frmCommunications m_commBase;
        private bool m_logging = false;
        /// <summary>
        /// A nice label for the tab or window.
        /// </summary>
        private string m_label;
        private jabber.connection.Room m_room;  // non-null for a Jabber conference
        private ContextMenu m_tempMenu;

        public string Label
        {
            get { return m_label; }
        }

        public frmConversation( string name, frmCommunications combase )
        {
            m_label = name;
            m_commBase = combase;
            InitializeComponent();
        }

        public void ConnectToComm(ConversationComm com)
        {
            m_commModule = com;
        }

        public void AddChat(jabber.protocol.client.Message msg)
        {
            this.BeginInvoke((ThreadStart)delegate()
            {
                chatHistory.InsertMessage(msg);

                if (m_logging)
                {
                    // TODO
                }
            });
        }

        /// <summary>
        /// Add a Sim chat message to the chat history.
        /// </summary>
        /// <param name="from">Who said it</param>
        /// <param name="body">What they said</param>
        /// <param name="type">How it was said</param>
        /// <remarks>The way in which a message appears depends on the
        /// type and source.</remarks>
        public void AddChat(string from,
            string body,
            ChatType type,
            ChatAudibleLevel audible,
            ChatSourceType source)
        {
            Color c;
            // Tag color depends on who says it and how they said it.
            switch (source)
            {
                case ChatSourceType.Object: c = Color.Green; break;
                case ChatSourceType.System: c = Color.Red; break;
                default:
                    switch (type)
                    {
                        case ChatType.Normal: c = Color.BlueViolet; break;
                        case ChatType.OwnerSay: c = Color.Orange; break;
                        case ChatType.RegionSay:
                        case ChatType.Shout: c = Color.Purple; break;
                        default: c = Color.Black; break;
                    }
                    break;
            }

            // Turn self references into actions in accordance with standard
            // Internet chat practice.
            if (body.StartsWith("/me "))
                body = body.Substring(4);
            else
                from = from + ":";

            BeginInvoke((ThreadStart)delegate()
            {
                chatHistory.AppendMaybeScroll(c, from, body);
            });

        }

        public void AddLine(string txt)
        {
            this.BeginInvoke((ThreadStart)delegate()
            {
                chatHistory.AppendText(txt+"\n");
            });
        }

        private void DescribeDisco(jabber.connection.DiscoNode node)
        {
            if (node == null) return;

            string nWho = node.JID.ToString();
            chatHistory.AppendMaybeScroll(
                Color.Navy, node.Name, nWho+"\n");

            foreach (string f in node.FeatureNames)
            {
                chatHistory.AppendMaybeScroll(
                Color.Navy, node.Name, "has " + f);
            }
            foreach (string f in node.Identities)
            {
                chatHistory.AppendMaybeScroll(
                Color.Navy, node.Name, "id " + f);
            }

        }

        /// <summary>
        /// Respond to mouse clicks in the chat history panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatHistory_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Right-click presents a popup menu.
                this.BeginInvoke((ThreadStart)delegate()
                {
                    // Put up context menu
                    m_tempMenu = new ContextMenu();
                    m_tempMenu.MenuItems.Add(new MenuItem("Save", new EventHandler(onSave)));
                    m_tempMenu.MenuItems.Add(new MenuItem("Clear", new EventHandler(onClear)));
                    Control parent = this.Parent;

                    // The Dock/Undock choice depends on what our parent is.
                    if (parent is TabPage)
                        m_tempMenu.MenuItems.Add(new MenuItem("Undock", new EventHandler(onUndock)));
                    else
                        m_tempMenu.MenuItems.Add(new MenuItem("Dock", new EventHandler(onDock)));
                    m_tempMenu.Show(parent, e.Location);
                });

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
        /// Save a chat history to a file.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void onSave(object o, EventArgs e)
        {
            ClearTempMenu();
            this.BeginInvoke((ThreadStart)delegate()
            {
                chatHistory.SaveFile(m_label+".txt", RichTextBoxStreamType.PlainText);
            });
        }

        /// <summary>
        /// Clear a chat history.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void onClear(object o, EventArgs e)
        {
            ClearTempMenu();
            this.BeginInvoke((ThreadStart)delegate()
            {
                chatHistory.Clear();
            });
        }

        /// <summary>
        /// Undock a conversation to its own window.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void onUndock(object o, EventArgs e)
        {
            ClearTempMenu();
            this.BeginInvoke((ThreadStart)delegate()
           {
               // Uncouple from the parent TabPage and delete it.
               Control tabPage = this.Parent;
               Control tabMaster = tabPage.Parent;
               tabPage.Controls.Remove(this);
               tabMaster.Controls.Remove(tabPage);
               tabPage.Dispose();

               // Create a new Undocked container for this conversation.
               frmUndockedConversation newCon = new frmUndockedConversation(this);
               newCon.Controls.Add(this);
           });
        }

        private void onDock(object o, EventArgs e)
        {
            ClearTempMenu();
            this.BeginInvoke((ThreadStart)delegate()
            {
                Control dock = this.Parent;
                // Create a new docked container for this conversation.
                frmDockedConversation newCon = new frmDockedConversation(this);
                m_commBase.AddDockedConversation(this);

                // Delete the undocked one we were in.
                dock.Dispose();
            });
        }

        // Simple editing controls for the history and input boxes.

        private void chatHistory_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Control && e.KeyCode == Keys.C  )
            {
                  chatHistory.Copy();
                  e.Handled = true;
            }
        }

        /// <summary>
        /// Process keys in the chat input box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatInput_KeyDown(object sender, KeyEventArgs e)
        {
            // Standard ctrl-V to paste
            if (e.Control && e.KeyCode == Keys.V)
            {
                chatHistory.Paste();
                e.Handled = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.C)
            {
                chatHistory.Copy();
                e.Handled = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.X)
            {
                chatHistory.Cut();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                // The chat input box is set to "MultiLine"
                // to prevent it from beeping when ENTER is pressed, but we
                // actually make it behave line a one-line field by sending
                // the text right away and blanking the field.
                e.Handled = true;

                // Let the communications module send the text
                // in a manner apprioriate to the kind of conversation.
                // TODO look at if ((e.Modifiers & Keys.Control) != 0)
                m_commModule.Say(chatInput.Text);

                this.BeginInvoke((ThreadStart)delegate()
                {
                    chatInput.Text = "";            
                });
            }
        }


    }
}
