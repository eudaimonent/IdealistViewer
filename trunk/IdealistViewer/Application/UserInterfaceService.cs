using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class UserInterfaceService
    {
        public VideoDriver m_driver = null;

        /// <summary>
        /// A handle to the Irrlicht ISceneManager 
        /// </summary>
        public SceneManager m_smgr = null;

        public Camera m_cam;

        public AvatarController m_avControl = null;

        /// <summary>
        /// A Handle to the Irrlicht Gui manager
        /// </summary>
        public GUIEnvironment m_guienv = null;

        protected List<IInterfacePlugin> m_interfacePlugins = new List<IInterfacePlugin>();

        protected int m_toolsMenuId;

        public int ToolsMenuId
        {
            get { return m_toolsMenuId; }
        }

        protected string m_aboutCaption;

        public string AboutCaption
        {
            get { return m_aboutCaption; }
            set { m_aboutCaption = value; }
        }

        protected string m_aboutText;

        public string AboutText
        {
            get { return m_aboutText; }
            set { m_aboutText = value; }
        }

        public UserInterfaceService(VideoDriver driver, SceneManager manager, GUIEnvironment gui, Camera cam, AvatarController avControl)
        {
            m_driver = driver;
            m_smgr = manager;
            m_guienv = gui;
            m_cam = cam;
            m_avControl = avControl;
        }

        public void AddMenuItems(GUIContextMenu menu, int defaultItemsAdded)
        {
            AddToolsMenu(menu, defaultItemsAdded);

            HelpMenu hMenu = new HelpMenu(this);
            hMenu.AddToolsMenuItem(menu, m_toolsMenuId);
            hMenu.AddMenu(menu, defaultItemsAdded + 1);
            m_interfacePlugins.Add(hMenu);
        }

        public void HandleMenuAction(int id)
        {
            bool handled = false;
            foreach (IInterfacePlugin plugin in m_interfacePlugins)
            {
                handled = plugin.MenuHandler(id);
                if (handled)
                {
                    break;
                }
            }
        }

        private void AddToolsMenu(GUIContextMenu menu, int itemNumber)
        {
            menu.AddItem("Tools", -1, true, true);
            m_toolsMenuId = itemNumber;

            //place holder item
            GUIContextMenu submenu;
            submenu = menu.GetSubMenu(itemNumber);
            submenu.AddItem("Place holder", 500, true, false);
        }

    }

    public class HelpMenu : IInterfacePlugin
    {
        protected UserInterfaceService m_interfaceService;

        public HelpMenu(UserInterfaceService parentService)
        {
            m_interfaceService = parentService;
        }

        
        public void AddMenu(GUIContextMenu menu, int itemNumber)
        {
            menu.AddItem("Help", -1, true, true);
            GUIContextMenu submenu = menu.GetSubMenu(itemNumber);
            submenu.AddItem("About", (int)MenuID.About, true, false);
        }

       
        public void AddToolsMenuItem(GUIContextMenu menu, int toolsMenuId)
        {

        }

        public bool MenuHandler(int id)
        {
            if (id == (int)MenuID.About)
            {
                BaseIdealistViewer.Device.GUIEnvironment.AddMessageBox(
                m_interfaceService.AboutCaption ,m_interfaceService.AboutText, true, MessageBoxFlag.OK, BaseIdealistViewer.Device.GUIEnvironment.RootElement, 0);
                return true;
            }
            return false;
        }
    }
}
