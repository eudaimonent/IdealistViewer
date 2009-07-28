using System;
using System.Collections.Generic;
using System.Text;

namespace IdealistViewer.UserInterface
{
    abstract public class ConversationComm
    {
        protected Forms.frmConversation m_conForm;
        public abstract void Say( string msg );
        public abstract void close();
    }

}
