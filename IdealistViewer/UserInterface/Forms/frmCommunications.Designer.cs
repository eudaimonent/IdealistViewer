namespace IdealistViewer
{
    partial class frmCommunications
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.commTabControl = new System.Windows.Forms.TabControl();
            this.LocalChatTab = new System.Windows.Forms.TabPage();
            this.grpTabControl = new System.Windows.Forms.TabControl();
            this.tabFriends = new System.Windows.Forms.TabPage();
            this.rosterTree = new muzzle.RosterTree();
            this.JC = new jabber.client.JabberClient(this.components);
            this.PresenceMgr = new jabber.client.PresenceManager(this.components);
            this.RosterMgr = new jabber.client.RosterManager(this.components);
            this.tabLocal = new System.Windows.Forms.TabPage();
            this.tabGroups = new System.Windows.Forms.TabPage();
            this.groupTree = new System.Windows.Forms.TreeView();
            this.tabNearbyChat = new System.Windows.Forms.TabPage();
            this.conversationPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnLocalChatSay = new System.Windows.Forms.Button();
            this.discoManager = new jabber.connection.DiscoManager(this.components);
            this.conferenceManager = new jabber.connection.ConferenceManager(this.components);
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.commTabControl.SuspendLayout();
            this.grpTabControl.SuspendLayout();
            this.tabFriends.SuspendLayout();
            this.tabGroups.SuspendLayout();
            this.tabNearbyChat.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.commTabControl);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.grpTabControl);
            this.splitContainer1.Size = new System.Drawing.Size(569, 264);
            this.splitContainer1.SplitterDistance = 386;
            this.splitContainer1.TabIndex = 0;
            // 
            // commTabControl
            // 
            this.commTabControl.CausesValidation = false;
            this.commTabControl.Controls.Add(this.LocalChatTab);
            this.commTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commTabControl.Location = new System.Drawing.Point(0, 0);
            this.commTabControl.Name = "commTabControl";
            this.commTabControl.SelectedIndex = 0;
            this.commTabControl.Size = new System.Drawing.Size(386, 264);
            this.commTabControl.TabIndex = 0;
            // 
            // LocalChatTab
            // 
            this.LocalChatTab.Location = new System.Drawing.Point(4, 22);
            this.LocalChatTab.Name = "LocalChatTab";
            this.LocalChatTab.Padding = new System.Windows.Forms.Padding(3);
            this.LocalChatTab.Size = new System.Drawing.Size(378, 238);
            this.LocalChatTab.TabIndex = 0;
            this.LocalChatTab.Text = "Local Chat";
            this.LocalChatTab.UseVisualStyleBackColor = true;
            // 
            // grpTabControl
            // 
            this.grpTabControl.Controls.Add(this.tabFriends);
            this.grpTabControl.Controls.Add(this.tabLocal);
            this.grpTabControl.Controls.Add(this.tabGroups);
            this.grpTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTabControl.Location = new System.Drawing.Point(0, 0);
            this.grpTabControl.Name = "grpTabControl";
            this.grpTabControl.SelectedIndex = 0;
            this.grpTabControl.Size = new System.Drawing.Size(179, 264);
            this.grpTabControl.TabIndex = 0;
            // 
            // tabFriends
            // 
            this.tabFriends.Controls.Add(this.rosterTree);
            this.tabFriends.Location = new System.Drawing.Point(4, 22);
            this.tabFriends.Name = "tabFriends";
            this.tabFriends.Size = new System.Drawing.Size(171, 238);
            this.tabFriends.TabIndex = 1;
            this.tabFriends.Text = "Friends";
            this.tabFriends.UseVisualStyleBackColor = true;
            // 
            // rosterTree
            // 
            this.rosterTree.AllowDrop = true;
            this.rosterTree.Client = this.JC;
            this.rosterTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rosterTree.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
            this.rosterTree.ImageIndex = 1;
            this.rosterTree.Location = new System.Drawing.Point(0, 0);
            this.rosterTree.Name = "rosterTree";
            this.rosterTree.PresenceManager = this.PresenceMgr;
            this.rosterTree.RosterManager = this.RosterMgr;
            this.rosterTree.SelectedImageIndex = 0;
            this.rosterTree.ShowLines = false;
            this.rosterTree.ShowRootLines = false;
            this.rosterTree.Size = new System.Drawing.Size(171, 238);
            this.rosterTree.Sorted = true;
            this.rosterTree.StatusColor = System.Drawing.Color.Teal;
            this.rosterTree.TabIndex = 0;
            this.rosterTree.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.rosterTree_NodeMouseDoubleClick);
            this.rosterTree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.rosterTree_NodeMouseClick);
            // 
            // JC
            // 
            this.JC.AutoReconnect = 30F;
            this.JC.AutoStartCompression = true;
            this.JC.AutoStartTLS = true;
            this.JC.InvokeControl = this;
            this.JC.KeepAlive = 30F;
            this.JC.LocalCertificate = null;
            this.JC.NetworkHost = "";
            this.JC.Password = "";
            this.JC.Resource = "Idealist";
            this.JC.Server = "";
            this.JC.User = "";
            this.JC.OnError += new bedrock.ExceptionHandler(this.JC_OnError);
            this.JC.OnInvalidCertificate += new System.Net.Security.RemoteCertificateValidationCallback(this.JC_OnInvalidCertificate);
            this.JC.OnAuthenticate += new bedrock.ObjectHandler(this.JC_OnAuthenticate);
            this.JC.OnReadText += new bedrock.TextHandler(this.JC_OnReadText);
            this.JC.OnWriteText += new bedrock.TextHandler(this.JC_OnWriteText);
            this.JC.OnConnect += new jabber.connection.StanzaStreamHandler(this.JC_OnConnect);
            this.JC.OnAuthError += new jabber.protocol.ProtocolHandler(this.JC_OnAuthError);
            this.JC.OnMessage += new jabber.client.MessageHandler(this.JC_OnMessage);
            // 
            // PresenceMgr
            // 
            this.PresenceMgr.CapsManager = null;
            this.PresenceMgr.Stream = this.JC;
            // 
            // RosterMgr
            // 
            this.RosterMgr.Stream = this.JC;
            // 
            // tabLocal
            // 
            this.tabLocal.Location = new System.Drawing.Point(4, 22);
            this.tabLocal.Name = "tabLocal";
            this.tabLocal.Size = new System.Drawing.Size(171, 238);
            this.tabLocal.TabIndex = 0;
            this.tabLocal.Text = "Nearby";
            this.tabLocal.UseVisualStyleBackColor = true;
            // 
            // tabGroups
            // 
            this.tabGroups.Controls.Add(this.groupTree);
            this.tabGroups.Location = new System.Drawing.Point(4, 22);
            this.tabGroups.Name = "tabGroups";
            this.tabGroups.Size = new System.Drawing.Size(171, 238);
            this.tabGroups.TabIndex = 2;
            this.tabGroups.Text = "Groups";
            this.tabGroups.UseVisualStyleBackColor = true;
            // 
            // groupTree
            // 
            this.groupTree.BackColor = System.Drawing.SystemColors.Window;
            this.groupTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupTree.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
            this.groupTree.Location = new System.Drawing.Point(0, 0);
            this.groupTree.Name = "groupTree";
            this.groupTree.Size = new System.Drawing.Size(171, 238);
            this.groupTree.Sorted = true;
            this.groupTree.TabIndex = 0;
            this.groupTree.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.groupTree_DrawNode);
            this.groupTree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.groupTree_NodeMouseClick);
            // 
            // tabNearbyChat
            // 
            this.tabNearbyChat.BackColor = System.Drawing.SystemColors.Control;
            this.tabNearbyChat.Controls.Add(this.conversationPanel);
            this.tabNearbyChat.ForeColor = System.Drawing.SystemColors.ControlText;
            this.tabNearbyChat.Location = new System.Drawing.Point(4, 22);
            this.tabNearbyChat.Name = "tabNearbyChat";
            this.tabNearbyChat.Padding = new System.Windows.Forms.Padding(3);
            this.tabNearbyChat.Size = new System.Drawing.Size(378, 238);
            this.tabNearbyChat.TabIndex = 0;
            this.tabNearbyChat.Text = "Local Chat";
            // 
            // conversationPanel
            // 
            this.conversationPanel.Location = new System.Drawing.Point(0, 0);
            this.conversationPanel.Name = "conversationPanel";
            this.conversationPanel.Size = new System.Drawing.Size(200, 100);
            this.conversationPanel.TabIndex = 0;
            // 
            // btnLocalChatSay
            // 
            this.btnLocalChatSay.Location = new System.Drawing.Point(0, 0);
            this.btnLocalChatSay.Name = "btnLocalChatSay";
            this.btnLocalChatSay.Size = new System.Drawing.Size(75, 23);
            this.btnLocalChatSay.TabIndex = 0;
            // 
            // discoManager
            // 
            this.discoManager.Stream = this.JC;
            // 
            // conferenceManager
            // 
            this.conferenceManager.Stream = this.JC;
            // 
            // frmCommunications
            // 
            this.ClientSize = new System.Drawing.Size(569, 264);
            this.Controls.Add(this.splitContainer1);
            this.Name = "frmCommunications";
            this.Text = "Communications";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.commTabControl.ResumeLayout(false);
            this.grpTabControl.ResumeLayout(false);
            this.tabFriends.ResumeLayout(false);
            this.tabGroups.ResumeLayout(false);
            this.tabNearbyChat.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabPage tabNearbyChat;
        private System.Windows.Forms.TabPage tabFriends;
        private System.Windows.Forms.TabPage tabLocal;
        private System.Windows.Forms.TabPage tabGroups;
        private System.Windows.Forms.Button btnLocalChatSay;
        private System.Windows.Forms.TableLayoutPanel conversationPanel;
        private jabber.client.JabberClient JC;
        private jabber.client.RosterManager RosterMgr;
        private jabber.client.PresenceManager PresenceMgr;
        private System.Windows.Forms.TabControl grpTabControl;
        private System.Windows.Forms.TabControl commTabControl;
        private System.Windows.Forms.TabPage LocalChatTab;
        private muzzle.RosterTree rosterTree;
        private jabber.connection.DiscoManager discoManager;
        private jabber.connection.ConferenceManager conferenceManager;
        private System.Windows.Forms.TreeView groupTree;


    }
}
