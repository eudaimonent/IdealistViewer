using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace IdealistViewer.UserInterface
{
    /// <summary>
    /// Extension to an XMPP RosterTree item to hold out preferred
    /// conference Nick
    /// </summary>
   public class GroupRoomNode : muzzle.RosterTree.ItemNode
    {
        public GroupRoomNode(jabber.protocol.iq.Item i) : base(i)
        {
         
        }
        public string Nick { get; set; }
    }
}
