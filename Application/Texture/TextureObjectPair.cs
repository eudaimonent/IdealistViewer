using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace IdealistViewer
{
    /// <summary>
    /// embedded struct for texture complete object.
    /// </summary>
    public struct TextureObjectPair
    {
        public VObject Object;
        public string TextureName;
        public UUID TextureID;
    }
}
