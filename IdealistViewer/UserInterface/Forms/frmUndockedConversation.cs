using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using IdealistViewer.UserInterface;

namespace IdealistViewer.UserInterface
{
    /// <summary>
    ///  An UnDocked conversation is a standalone Form on the screen.
    /// </summary>
    class frmUndockedConversation : Form
    {
        private Forms.frmConversation m_conversation;
        public Forms.frmConversation Content
        {
            get { return m_conversation; }
        }

        /// <summary>
        /// Create a free-floating window holding a conversation.
        /// </summary>
        /// <param name="c">The conversation</param>
        public frmUndockedConversation( Forms.frmConversation c )
            : base()
        {
            m_conversation = c;
            ClientSize = new System.Drawing.Size(569, 264);
            Controls.Add(c);
            Name = "Conversation";

            // We put the conversation name into the window title bar.
            Text = c.Label;
            Show();
            ResumeLayout(false);
        }

    }
}
