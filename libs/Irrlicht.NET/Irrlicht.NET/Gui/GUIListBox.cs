using System;
using System.Runtime.InteropServices;
using System.Security;

namespace IrrlichtNETCP
{
    public class GUIListBox : GUIElement
    {
        public GUIListBox(IntPtr raw)
            : base(raw)
        {
        }

        public int AddItem(string text, string icon)
        {
            return GUIListBox_AddItem(_raw, text, icon);
        }

        public int AddItem(string text)
        {
            return GUIListBox_AddItemA(_raw, text);
        }

        public void Clear()
        {
            GUIListBox_Clear(_raw);
        }

        public string GetListItem(int id)
        {
            IntPtr ptr_value = IntPtr.Zero;
            string value;
            try
            {
                //New way for string marshalling according to
                //http://www.mono-project/Interop_With_Native_Libraries
                ptr_value = GUIListBox_GetListItem(_raw, id);
                
                //Combined Marshalling and memory release in one method/class (MainDefinition.cs)
                //for general access in all functions using strings
                value = IrrNetMarshal.IntPtrToString(ptr_value);
            }
            catch (Exception)
            {
                return "Error!";
            }
            
            return value;
        }

        public void SetIconFont(GUIFont font)
        {
            GUIListBox_SetIconFont(_raw, font.Raw);
        }

        public int ItemCount
        {
            get
            {
                return GUIListBox_GetItemCount(_raw);
            }
        }

        public int Selected
        {
            get
            {
                return GUIListBox_GetSelected(_raw);
            }
            set
            {
                GUIListBox_SetSelected(_raw, value);
            }
        }

        #region Native Invokes
         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern int GUIListBox_AddItem(IntPtr listb, string text, string icon);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern int GUIListBox_AddItemA(IntPtr listb, string text);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern void GUIListBox_Clear(IntPtr listb);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern int GUIListBox_GetItemCount(IntPtr listb);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern IntPtr GUIListBox_GetListItem(IntPtr listb, int id);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern int GUIListBox_GetSelected(IntPtr listb);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern void GUIListBox_SetIconFont(IntPtr listb, IntPtr font);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern void GUIListBox_SetSelected(IntPtr listb, int sel);

       

        #endregion

    }
}
