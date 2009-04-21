using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public static class VObjectUtil
    {
        public static VObject NewVObject(Primitive pPrim, VObject pOldObj)
        {   
            VObject returnVObject = null;

            if (pOldObj == null)
            {
                returnVObject = new VObject();
                returnVObject.SceneNode = null;
            }
            else
            {
                returnVObject = pOldObj;
            }

            returnVObject.Primitive = pPrim;
            returnVObject.Mesh = null;
            
            return returnVObject;
        }

        public static string GetHashId(VObject pObj)
        {
            string returnString = string.Empty;
            
            if (pObj.Primitive != null)
            {
                ulong simhandle = pObj.Primitive.RegionHandle;
                ulong TESTNEIGHBOR = 1099511628032256;
                if (simhandle == 0)
                        simhandle = TESTNEIGHBOR;

                returnString =simhandle.ToString() + pObj.Primitive.LocalID.ToString();
            }
           

            return returnString;
        }
    }
}
