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
using IdealistViewer.Network;

namespace IdealistViewer
{
    public partial class frmCommunications : Form
    {
        private INetworkInterface avatarConnection;


        public frmCommunications(INetworkInterface avatarConnection)
        {
            this.avatarConnection = avatarConnection;
            InitializeComponent();
            avatarConnection.OnChat +=
                new NetworkChatDelegate(avatarConnection_OnChat);
            avatarConnection.OnFriendsListUpdate +=
                new NetworkFriendsListUpdateDelegate(avatarConnection_OnFriendsListChanged);
        }

        void avatarConnection_OnFriendsListChanged()
        {
            this.BeginInvoke((ThreadStart)delegate()
            {
                listFriends.Items.Clear();
                //foreach (UUID friend in avatarConnection.Friends.)
                avatarConnection.Friends.ForEach(delegate(FriendInfo friend)
                {
                    string statusString = "(Offline)";
                    //if (avatarConnection.Friends[friend].IsOnline)
                    if (friend.IsOnline)
                    {
                        statusString = "(Online)";
                    }

                    if (friend.Name != null)
                    {
                        listFriends.Items.Add(friend.Name + statusString);
                    }
                });
            });
        }

        void avatarConnection_OnChat(
            string message,
            OpenMetaverse.ChatAudibleLevel audible,
            OpenMetaverse.ChatType type,
            OpenMetaverse.ChatSourceType sourcetype,
            string fromName,
            OpenMetaverse.UUID id,
            OpenMetaverse.UUID ownerid,
            OpenMetaverse.Vector3 position)
        {
            this.BeginInvoke((ThreadStart)delegate()
            {
                if (message.ToLower().StartsWith("/me "))
                    txtLocalChat.AppendChat( fromName, message.Substring(3), 0);
                else
                    if (type == ChatType.Shout)
                        txtLocalChat.AppendChat(fromName, message, 2);
                    else
                        txtLocalChat.AppendChat(fromName, message, 1);
            });
        }

        private void SayThis()
        {
            avatarConnection.Say(txtLocalChatInput.Text);
            txtLocalChat.AppendChat( "Me", txtLocalChatInput.Text, 0 );
            txtLocalChatInput.Text = "";
        }

        private void btnLocalChatSay_Click(object sender, EventArgs e)
        {
            SayThis();
            txtLocalChatInput.Focus();
        }

        private void txtLocalChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SayThis();
            }
        }


    }
}
