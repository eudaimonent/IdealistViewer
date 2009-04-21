
using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class HelpMenu : IMenuPlugin
    {
        protected UserInterfaceManager m_userInterface;

        public HelpMenu(UserInterfaceManager userInterface)
        {
            m_userInterface = userInterface;
        }


        public void AddMenuItem(GUIContextMenu menu, int itemNumber)
        {
            menu.AddItem("Help", -1, true, true);
            GUIContextMenu submenu = menu.GetSubMenu(itemNumber);
            submenu.AddItem("About", (int)MenuItems.About, true, false);
        }

        public bool OnMenuItemClicked(int id)
        {
            if (id == (int)MenuItems.About)
            {
                m_userInterface.Viewer.Renderer.Device.GUIEnvironment.AddMessageBox(
                m_userInterface.AboutCaption, m_userInterface.AboutText, true, MessageBoxFlag.OK, m_userInterface.Viewer.Renderer.Device.GUIEnvironment.RootElement, 0);
                return true;
            }
            return false;
        }
    }
}
