using System.Drawing;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class Control
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Control Parent { get; set; }

        public Rect ActualDimensions
        {
            get
            {
                if(Parent == null)
                {
                    //this should only happen at the application level
                    return new Rect(0,0,Width,Height);
                }

                Position2D offset = Parent.GetRenderOffset(this);
                int width = Parent.CalculateMyActualWidth(this);
                int height = Parent.CalculateMyActualHeight(this);
                return new Rect(offset.X, offset.Y, offset.X + width, offset.Y + height);
            }
        }

        public virtual void RenderControl() {}

        protected virtual Position2D GetRenderOffset(Control control)
        {
            if( Parent == null )
            {
                return new Position2D(0, 0);
            }
            return Parent.GetRenderOffset(this);    
        }

        protected int GetCalculatedWidth()
        {
            return Parent.CalculateMyActualWidth(this);
        }

        protected int GetCalculatedHeight()
        {
            return Parent.CalculateMyActualHeight(this);
        }

        protected virtual int CalculateMyActualHeight(Control control)
        {
            return Width;
        }

        protected virtual int CalculateMyActualWidth(Control control)
        {
            return Height;
        }

        public virtual Control PointToControl(Position2D p, out Position2D pointHit)
        {
            pointHit = p;
            return this;
        }

        public virtual void LeftMouseUp(Event p_event, Position2D pointHit)
        {
            
        }

        public virtual void LeftMouseDown(Event p_event, Position2D pointHit)
        {
            
        }
    }
}
