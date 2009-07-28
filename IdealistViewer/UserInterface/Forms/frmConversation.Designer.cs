namespace IdealistViewer.UserInterface.Forms
{
    partial class frmConversation
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.conversationLayout = new System.Windows.Forms.TableLayoutPanel();
            this.chatHistory = new muzzle.ChatHistory();
            this.chatInput = new System.Windows.Forms.TextBox();
            this.conversationLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // conversationLayout
            // 
            this.conversationLayout.ColumnCount = 1;
            this.conversationLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.conversationLayout.Controls.Add(this.chatHistory, 0, 0);
            this.conversationLayout.Controls.Add(this.chatInput, 0, 1);
            this.conversationLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.conversationLayout.Location = new System.Drawing.Point(0, 0);
            this.conversationLayout.Name = "conversationLayout";
            this.conversationLayout.RowCount = 2;
            this.conversationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90F));
            this.conversationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.conversationLayout.Size = new System.Drawing.Size(271, 221);
            this.conversationLayout.TabIndex = 0;
            // 
            // chatHistory
            // 
            this.chatHistory.BackColor = System.Drawing.SystemColors.Window;
            this.conversationLayout.SetColumnSpan(this.chatHistory, 2);
            this.chatHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chatHistory.Location = new System.Drawing.Point(3, 3);
            this.chatHistory.Name = "chatHistory";
            this.chatHistory.Nickname = null;
            this.chatHistory.ReadOnly = true;
            this.chatHistory.Size = new System.Drawing.Size(265, 192);
            this.chatHistory.TabIndex = 0;
            this.chatHistory.TabStop = false;
            this.chatHistory.Text = "";
            this.chatHistory.KeyDown += new System.Windows.Forms.KeyEventHandler(this.chatHistory_KeyDown);
            this.chatHistory.MouseDown += new System.Windows.Forms.MouseEventHandler(this.chatHistory_MouseDown);
            // 
            // chatInput
            // 
            this.chatInput.AcceptsReturn = true;
            this.chatInput.AcceptsTab = true;
            this.chatInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.chatInput.Location = new System.Drawing.Point(3, 201);
            this.chatInput.Multiline = true;
            this.chatInput.Name = "chatInput";
            this.chatInput.Size = new System.Drawing.Size(265, 17);
            this.chatInput.TabIndex = 1;
            this.chatInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.chatInput_KeyDown);
            // 
            // frmConversation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CausesValidation = false;
            this.Controls.Add(this.conversationLayout);
            this.Name = "frmConversation";
            this.Size = new System.Drawing.Size(271, 221);
            this.conversationLayout.ResumeLayout(false);
            this.conversationLayout.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel conversationLayout;
        private muzzle.ChatHistory chatHistory;
        private System.Windows.Forms.TextBox chatInput;

    }
}
