using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using OpenMetaverse;

namespace IdealistViewer
{
    public class UserInterfaceManager
    {

        public Viewer Viewer;

        public VideoDriver m_driver = null;
        public SceneManager m_sceneManager = null;
        public GUIEnvironment m_guiEnvironment = null;

        public CameraController m_cameraController;
        public AvatarController m_avatarController;

        protected List<IMenuPlugin> m_menuPlugins = new List<IMenuPlugin>();

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

        #region Chat State Fields

        public GUIEditBox ChatBoxInput;
        public GUIListBox ChatBoxMessageList;
        public GUIListBox FriendsList;

        public GUIFont DefaultFont = null;


        /// <summary>
        /// Outbound chat messages waiting to be sent.
        /// </summary>
        public Queue<string> OutboundChatMessages = new Queue<string>();
        /// <summary>
        /// Message history
        /// </summary>
        public List<string> MessageHistory = new List<string>();

        public bool NewChat = false;
        public object FocusedElement;

        #endregion

        public UserInterfaceManager(Viewer viewer,VideoDriver driver, SceneManager manager, GUIEnvironment gui, CameraController cam, AvatarController avControl)
        {
            Viewer = viewer;
            m_driver = driver;
            m_sceneManager = manager;
            m_guiEnvironment = gui;
            m_cameraController = cam;
            m_avatarController = avControl;

            GUIContextMenu menu = viewer.Renderer.GuiEnvironment.AddMenu(viewer.Renderer.GuiEnvironment.RootElement, -1);
            menu.AddItem("File", -1, true, true);
            menu.AddItem("View", -1, true, true);
            menu.AddItem("Other", -1, true, true);
            menu.AddItem("Communication", -1, true, true);

            GUIContextMenu submenu;
            submenu = menu.GetSubMenu(0);
            submenu.AddItem("Open File...", (int)MenuItems.FileOpen, true, false);
            submenu.AddSeparator();
            submenu.AddItem("Quit", (int)MenuItems.FileQuit, true, false);

            submenu = menu.GetSubMenu(1);
            submenu.AddItem("Show PrimCount", (int)MenuItems.ShowPrimcount, true, false);
            submenu.AddItem("Clear Cache", (int)MenuItems.ClearCache, true, false);
            submenu.AddItem("toggle mode", -1, true, true);

            submenu = submenu.GetSubMenu(2);
            submenu.AddItem("Solid", (int)MenuItems.ViewModeOne, true, false);
            submenu.AddItem("Transparent", (int)MenuItems.ViewModeTwo, true, false);
            submenu.AddItem("Reflection", (int)MenuItems.ViewModeThree, true, false);

            submenu = menu.GetSubMenu(3);
            submenu.AddItem("Show Chat", (int)MenuItems.ShowChat, true, false);

            AddMenuItems(menu, 4);
        }

        public void AddMenuItems(GUIContextMenu menu, int defaultItemsAdded)
        {
            AddToolsMenu(menu, defaultItemsAdded);

            HelpMenu hMenu = new HelpMenu(this);
            hMenu.AddMenuItem(menu, defaultItemsAdded + 1);
            m_menuPlugins.Add(hMenu);
        }

        public void HandleMenuAction(int id)
        {
            bool handled = false;
            foreach (IMenuPlugin plugin in m_menuPlugins)
            {
                handled = plugin.OnMenuItemClicked(id);
                if (handled)
                {
                    break;
                }
            }
        }

        private void AddToolsMenu(GUIContextMenu menu, int itemId)
        {
            menu.AddItem("Tools", -1, true, true);
            m_toolsMenuId = itemId;

            GUIContextMenu submenu = menu.GetSubMenu(itemId);
            submenu.AddItem("Place holder", 500, true, false);
        }

        /// <summary>
        /// Show Chat History window
        /// </summary>
        public virtual void ShowChatWindow()
        {
            // remove tool box if already there
            GUIEnvironment guienv = Viewer.Renderer.GuiEnvironment;
            GUIElement root = guienv.RootElement;
            GUIElement e = root.GetElementFromID(5000, true);
            if (e != null) e.Remove();

            GUIWindow wnd = guienv.AddWindow(
                new Rect(new Position2D(150, 25), new Position2D(640, 500)),
                false, "Chat Window", guienv.RootElement, 5000);

            // create tab control and tabs
            GUITabControl tab = guienv.AddTabControl(
            new Rect(new Position2D(2, 20), new Position2D(490, 473)),
            wnd, true, true, -1);
            GUITab t1 = tab.AddTab("Main", -1);
            GUITab t2 = tab.AddTab("Groups", -1);
            GUITab t3 = tab.AddTab("Friends", -1);

            // add some edit boxes and a button to tab one
            //env.AddEditBox("1.0", new Rect(40,50,130,70), true, t1, 901);
            ChatBoxMessageList = guienv.AddListBox(new Rect(new Position2D(5, 5), new Position2D(485, 380)),
                t1, 5100, true);
            //            guienv.AddEditBox("1.0",
            //                new Rect(new Position2D(40, 50), new Position2D(130, 70)), true, t1, 901);
            //            guienv.AddEditBox("1.0",
            //                new Rect(new Position2D(40, 350), new Position2D(130, 400)), true, t1, 902);
            ChatBoxInput = guienv.AddEditBox(" ",
                new Rect(new Position2D(5, 385), new Position2D(485, 410)), true, t1, 903);
            //            guienv.AddButton(new
            //                Rect(new Position2D(10, 150), new Position2D(100, 190)), t1, 1101, "set");

            FriendsList = guienv.AddListBox(new Rect(new Position2D(5, 5), new Position2D(485, 410)),
                t3, 5101, true);

            UpdateFriendsList();
            UpdateChatWindow();
        }

        public void UpdateFriendsList()
        {
            if (FriendsList == null)
            {
                return;
            }
            FriendsList.Clear();
            foreach (UUID friend in Viewer.NetworkInterface.Friends.Keys)
            {
                string statusString = "(Offline)";
                if (Viewer.NetworkInterface.Friends[friend].IsOnline)
                {
                    statusString = "(Online)";
                }

                if (Viewer.NetworkInterface.Friends[friend].Name != null)
                {
                    FriendsList.AddItem(Viewer.NetworkInterface.Friends[friend].Name + statusString);
                }
            }
        }

        public void UpdateChatWindow()
        {
            if (NewChat)
            {
                NewChat = false;
                if (ChatBoxMessageList != null)
                {
                    ChatBoxMessageList.Clear();
                    lock (MessageHistory)
                    {
                        for (int i = MessageHistory.Count - 1; i >= 0; i--)
                        {
                            ChatBoxMessageList.AddItem(MessageHistory[i]);
                        }
                    }
                }
            }
        }

        public void OnNetworkChat(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourcetype, string fromName, UUID id, UUID ownerid, Vector3 position)
        {
            lock (MessageHistory)
            {
                if (message.ToLower().StartsWith("/me "))
                    MessageHistory.Add(fromName + message.Substring(3));
                else
                    if (type == ChatType.Shout)
                        MessageHistory.Add(fromName + " shouts: " + message);
                    else
                        MessageHistory.Add(fromName + ": " + message);

                NewChat = true;
            }
        }

    }

}
