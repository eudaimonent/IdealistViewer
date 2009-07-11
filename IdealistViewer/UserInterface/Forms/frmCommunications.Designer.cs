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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabNearbyChat = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.txtLocalChat = new IdealistViewer.UserInterface.ChatText();
            this.txtLocalChatInput = new System.Windows.Forms.TextBox();
            this.btnLocalChatSay = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabNearby = new System.Windows.Forms.TabPage();
            this.tabFriends = new System.Windows.Forms.TabPage();
            this.listFriends = new System.Windows.Forms.ListBox();
            this.tabGroups = new System.Windows.Forms.TabPage();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabNearbyChat.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabFriends.SuspendLayout();
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
            this.splitContainer1.Panel1.Controls.Add(this.tabControl2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer1.Size = new System.Drawing.Size(569, 264);
            this.splitContainer1.SplitterDistance = 386;
            this.splitContainer1.TabIndex = 0;
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabNearbyChat);
            this.tabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl2.Location = new System.Drawing.Point(0, 0);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(386, 264);
            this.tabControl2.TabIndex = 1;
            // 
            // tabNearbyChat
            // 
            this.tabNearbyChat.Controls.Add(this.tableLayoutPanel1);
            this.tabNearbyChat.Location = new System.Drawing.Point(4, 22);
            this.tabNearbyChat.Name = "tabNearbyChat";
            this.tabNearbyChat.Padding = new System.Windows.Forms.Padding(3);
            this.tabNearbyChat.Size = new System.Drawing.Size(378, 238);
            this.tabNearbyChat.TabIndex = 0;
            this.tabNearbyChat.Text = "Local Chat";
            this.tabNearbyChat.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.txtLocalChat, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtLocalChatInput, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnLocalChatSay, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(372, 232);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // txtLocalChat
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.txtLocalChat, 2);
            this.txtLocalChat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLocalChat.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLocalChat.Location = new System.Drawing.Point(3, 3);
            this.txtLocalChat.Name = "txtLocalChat";
            this.txtLocalChat.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLocalChat.Size = new System.Drawing.Size(366, 197);
            this.txtLocalChat.TabIndex = 2;
            this.txtLocalChat.Text = "";
            // 
            // txtLocalChatInput
            // 
            this.txtLocalChatInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLocalChatInput.Location = new System.Drawing.Point(3, 206);
            this.txtLocalChatInput.Name = "txtLocalChatInput";
            this.txtLocalChatInput.Size = new System.Drawing.Size(325, 20);
            this.txtLocalChatInput.TabIndex = 0;
            this.txtLocalChatInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtLocalChatInput_KeyDown);
            // 
            // btnLocalChatSay
            // 
            this.btnLocalChatSay.Location = new System.Drawing.Point(334, 206);
            this.btnLocalChatSay.Name = "btnLocalChatSay";
            this.btnLocalChatSay.Size = new System.Drawing.Size(35, 23);
            this.btnLocalChatSay.TabIndex = 1;
            this.btnLocalChatSay.Text = "Say";
            this.btnLocalChatSay.UseVisualStyleBackColor = true;
            this.btnLocalChatSay.Click += new System.EventHandler(this.btnLocalChatSay_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabNearby);
            this.tabControl1.Controls.Add(this.tabFriends);
            this.tabControl1.Controls.Add(this.tabGroups);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(179, 264);
            this.tabControl1.TabIndex = 0;
            // 
            // tabNearby
            // 
            this.tabNearby.Location = new System.Drawing.Point(4, 22);
            this.tabNearby.Name = "tabNearby";
            this.tabNearby.Size = new System.Drawing.Size(171, 238);
            this.tabNearby.TabIndex = 0;
            this.tabNearby.Text = "Nearby";
            this.tabNearby.UseVisualStyleBackColor = true;
            // 
            // tabFriends
            // 
            this.tabFriends.Controls.Add(this.listFriends);
            this.tabFriends.Location = new System.Drawing.Point(4, 22);
            this.tabFriends.Name = "tabFriends";
            this.tabFriends.Size = new System.Drawing.Size(171, 238);
            this.tabFriends.TabIndex = 1;
            this.tabFriends.Text = "Friends";
            this.tabFriends.UseVisualStyleBackColor = true;
            // 
            // listFriends
            // 
            this.listFriends.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listFriends.FormattingEnabled = true;
            this.listFriends.Location = new System.Drawing.Point(0, 0);
            this.listFriends.Name = "listFriends";
            this.listFriends.Size = new System.Drawing.Size(171, 238);
            this.listFriends.TabIndex = 0;
            // 
            // tabGroups
            // 
            this.tabGroups.Location = new System.Drawing.Point(4, 22);
            this.tabGroups.Name = "tabGroups";
            this.tabGroups.Size = new System.Drawing.Size(171, 238);
            this.tabGroups.TabIndex = 2;
            this.tabGroups.Text = "Groups";
            this.tabGroups.UseVisualStyleBackColor = true;
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
            this.tabControl2.ResumeLayout(false);
            this.tabNearbyChat.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabFriends.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabNearbyChat;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabNearby;
        private System.Windows.Forms.TabPage tabFriends;
        private System.Windows.Forms.TabPage tabGroups;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private UserInterface.ChatText txtLocalChat;
        private System.Windows.Forms.TextBox txtLocalChatInput;
        private System.Windows.Forms.Button btnLocalChatSay;
        private System.Windows.Forms.ListBox listFriends;


    }
}
