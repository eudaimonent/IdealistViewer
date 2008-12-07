using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using OpenMetaverse;

namespace IdealistViewer
{
    public partial class frmCommunications : Form
    {
        private SLProtocol avatarConnection;


        public frmCommunications(SLProtocol avatarConnection)
        {
            this.avatarConnection = avatarConnection;
            InitializeComponent();
            avatarConnection.OnChat += new SLProtocol.Chat(avatarConnection_OnChat);
            avatarConnection.OnFriendsListChanged += new SLProtocol.FriendsListchanged(avatarConnection_OnFriendsListChanged);
        }

        void avatarConnection_OnFriendsListChanged()
        {
            this.BeginInvoke((ThreadStart) (() =>
            {
                listFriends.Items.Clear();
                foreach (var friend in avatarConnection.Friends)
                {
                    string statusString = "(Offline)";
                    if (friend.Value.IsOnline)
                    {
                        statusString = "(Online)";
                    }

                    if (friend.Value.Name != null)
                    {
                        listFriends.Items.Add(friend.Value.Name + statusString);
                    }
                }
            }));
        }

        void avatarConnection_OnChat(string message, OpenMetaverse.ChatAudibleLevel audible, OpenMetaverse.ChatType type, OpenMetaverse.ChatSourceType sourcetype, string fromName, OpenMetaverse.UUID id, OpenMetaverse.UUID ownerid, OpenMetaverse.Vector3 position)
        {
           this.BeginInvoke((ThreadStart)(() =>
           {
               string prefix = "";
               if(txtLocalChat.Text.Length!=0)
               {
                   prefix = System.Environment.NewLine;
               }

               if (message.ToLower().StartsWith("/me "))
                   txtLocalChat.Text += prefix + fromName + message.Substring(3);
               else
                   if (type == ChatType.Shout)
                       txtLocalChat.Text += prefix + fromName + " shouts: " + message;
                   else
                       txtLocalChat.Text += prefix + fromName + ": " + message;


           }));
            
        }

        private void btnLocalChatSay_Click(object sender, EventArgs e)
        {
            avatarConnection.Say(txtLocalChatInput.Text);
            txtLocalChatInput.Text = "";
            txtLocalChatInput.Focus();
        }

        private void txtLocalChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if( e.KeyCode == Keys.Enter)
            {
                avatarConnection.Say(txtLocalChatInput.Text);
                txtLocalChatInput.Text = "";
            }
        }

    }
}
