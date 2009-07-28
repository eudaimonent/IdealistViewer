using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using OpenMetaverse;

namespace IdealistViewer.UserInterface
{
    /// <summary>
    /// A docked conversation is a TabPage within a containing TabControl.
    /// </summary>
    class frmDockedConversation : TabPage
    {
        private bool m_enabled;
        private bool m_unseen;
        public bool Unseen
        {
            get { return m_unseen; }
        }

        private Forms.frmConversation m_conversation;
        public Forms.frmConversation Content
        {
            get { return m_conversation; }
        }

        public frmDockedConversation( Forms.frmConversation c ) : base()
        {
            m_conversation = c;
            c.Dock = DockStyle.Fill;
            Controls.Add(c);
            Location = new System.Drawing.Point(4, 22);

            Name = "Docked Chat";
            Padding = new System.Windows.Forms.Padding(3);
            Size = new System.Drawing.Size(378, 238);
            Text = c.Label;
            UseVisualStyleBackColor = true;
            
            // += new System.EventHandler(onGotFocus);
        }

 
        public void SetFocus( bool enabled )
        {
            // On gaining focus, clear any tab flag.
            if (enabled && !m_enabled)
            {
                m_unseen = false;
            }

            m_enabled = enabled;
        }


    }
}
