using System;
using IrrlichtNETCP;
namespace IdealistViewer
{
    public interface IInterfacePlugin
    {
        bool MenuHandler(int id);

        /// <summary>
        /// Add a new Menu with sub items.
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="itemNumber"></param>
        void AddMenu(GUIContextMenu menu, int itemNumber);

        /// <summary>
        /// Add a sub item to the Tools Menu
        /// </summary>
        /// <param name="menuId"></param>
        void AddToolsMenuItem(GUIContextMenu menu, int toolsMenuId);
    }
}
