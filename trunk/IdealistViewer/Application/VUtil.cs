using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public static class VUtil
    {
        public static VObject NewVObject(Primitive pPrim, VObject pOldObj)
        {   
            VObject returnVObject = null;

            if (pOldObj == null)
            {
                returnVObject = new VObject();
                returnVObject.node = null;
            }
            else
            {
                returnVObject = pOldObj;
            }

            returnVObject.prim = pPrim;
            returnVObject.mesh = null;
            
            return returnVObject;
        }

        public static string GetHashId(VObject pObj)
        {
            string returnString = string.Empty;
            
            if (pObj.prim != null)
            {
                ulong simhandle = pObj.prim.RegionHandle;
                ulong TESTNEIGHBOR = 1099511628032256;
                if (simhandle == 0)
                        simhandle = TESTNEIGHBOR;

                returnString =simhandle.ToString() + pObj.prim.LocalID.ToString();
            }
           

            return returnString;
        }
    }
}
