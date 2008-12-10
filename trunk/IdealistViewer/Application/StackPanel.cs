using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using IrrlichtNETCP;

namespace IdealistViewer
{
    public class StackPanel : Control
    {
        int totalChildrenWidth;

        public StackPanel()
        {
            totalChildrenWidth = 0;
            Children = new List<Control>();
        }

        protected List<Control> Children { get; set; }

        public void AddChild(Control child)
        {
            if (child.Parent != null)
            {
                throw new Exception("Child already belongs to another control.");
            }
            child.Parent = this;
            totalChildrenWidth += child.Width;
            Children.Add(child);
        }

        protected override Position2D GetRenderOffset(Control control)
        {
            int index = Children.IndexOf(control);
            
            int offset = 0;
            
            for( int i = 0 ; i < index; i++)
            {
                offset += Children[i].Width;
            }

            return new Position2D((int)(((double)Width * (double)offset) / (double)totalChildrenWidth), 0);
        } 

        public override void RenderControl()
        {
            base.RenderControl();
            foreach (var child in Children)
            {
                child.RenderControl();
            }
        }

        protected override int CalculateMyActualHeight(Control control)
        {
            return Height;
        }

        protected override int CalculateMyActualWidth(Control control)
        {
            return (int) ((double)Width*(double)control.Width / (double)totalChildrenWidth);
        }

        public override Control PointToControl(Position2D p, out Position2D pointHit)
        {
            foreach (var child in Children)
            {
                Position2D offset = GetRenderOffset(child);

                if (child.ActualDimensions.IsPointInside(p))
                {
                    return child.PointToControl(p - offset, out pointHit);
                }
            }

            pointHit = p;
            return this;
        }
    }
}
