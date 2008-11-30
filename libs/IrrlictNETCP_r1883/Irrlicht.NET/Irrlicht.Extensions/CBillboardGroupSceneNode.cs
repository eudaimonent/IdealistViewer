using System;
using System.Collections.Generic;
using System.Text;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace Irrlicht.Extensions
{
    public class CBillboardGroupSceneNode : ISceneNode
    {
        float FarDistance = 256.0f;
        float Radius = 0.0f;

        public CBillboardGroupSceneNode(SceneManager mgr, int id, Vector3D Position, Vector3D Rotation, Vector3D scale)
            : base(parent, mgr, id)
        {
            base.Position = Position;
            base.Rotation = Rotation;
            base.Scale = Scale;

            Material mat = base.GetMaterial(0);
            mat.Lighting = false;


        }

        //public void addBillBoard(

    }
}
