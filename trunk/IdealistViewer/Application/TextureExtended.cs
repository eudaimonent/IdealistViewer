using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class TextureExtended : Texture
    {
        public object Userdata = null;
        public TextureExtended(IntPtr raw) : base(raw)
        {
        }
        
    }
}
