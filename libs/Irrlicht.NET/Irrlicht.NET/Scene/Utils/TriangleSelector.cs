using System;
using System.Runtime.InteropServices;
using System.Security;

namespace IrrlichtNETCP
{
	public class TriangleSelector : NativeElement
	{
		public TriangleSelector(IntPtr raw) : base(raw)
		{
		}
	}

    public class MetaTriangleSelector : TriangleSelector
	{
        public MetaTriangleSelector(IntPtr raw) : base(raw)
		{
		}

        /// <summary>
        /// Adds a triangle selector to the collection of triangle selectors in this metaTriangleSelector.
        /// </summary>
        /// <param name="toAdd">Triangle selector to add to the list</param>
        public void AddTriangleSelector(TriangleSelector toAdd)
        {
            MetaTriangleSelector_AddTriangleSelector(_raw, toAdd.Raw);
        }

        /// <summary>
        /// Removes all triangle selectors from the collection.
        /// </summary>
        public void RemoveAllTriangleSelectors()
        {
            MetaTriangleSelector_RemoveAllTriangleSelectors(_raw);
        }

        /// <summary>
        /// Removes a specific triangle selector which was added before from the collection.
        /// </summary>
        /// <param name="toRemove">Triangle selector which is in the list but will be removed.</param>
        public void RemoveTriangleSelector(TriangleSelector toRemove)
        {
            MetaTriangleSelector_RemoveTriangleSelector(_raw, toRemove.Raw);
        }

        #region Native Invokes
         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern void MetaTriangleSelector_AddTriangleSelector(IntPtr mts, IntPtr toadd);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
	    static extern void MetaTriangleSelector_RemoveAllTriangleSelectors(IntPtr mts);

         [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
        static extern void MetaTriangleSelector_RemoveTriangleSelector(IntPtr mts, IntPtr toadd);
        #endregion
	}
	
}
