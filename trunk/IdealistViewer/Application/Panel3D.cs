using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class Panel3D : Control
    {
        public Panel3D()
        {
            
        }

        public virtual VideoDriver Driver { get; set; }

        public override void RenderControl()
        {
            base.RenderControl();

            PreRender();
            Position2D p = GetRenderOffset(this);
            Rect viewPortRect = new Rect(p.X, p.Y, p.X + GetCalculatedWidth(), p.Y + GetCalculatedHeight());
            Driver.ViewPort = viewPortRect;
            Render();
            PostRender();
        }

        public virtual void PreRender()
        {
            
        }

        public virtual void Render()
        {
            //Render scene inside of bounds
        }
        
        public virtual void PostRender()
        {

        }
    }
}
