using System;
using IrrlichtNETCP;
namespace IdealistViewer
{
    public interface IMenuPlugin
    {
        /// <summary>
        /// Invoked when menu item is clicked.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        bool OnMenuItemClicked(int itemId);

        /// <summary>
        /// Add a new Menu with sub items.
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="itemNumber"></param>
        void AddMenuItem(GUIContextMenu menu, int itemId);

    }
}
