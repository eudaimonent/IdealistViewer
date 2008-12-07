using System;
using System.Collections.Generic;
using IrrlichtNETCP;
using IrrlichtNETCP.Inheritable;

namespace IdealistViewer
{
    class VGUIComboBox : IGUIElement
    {

        private VideoDriver driver;
        private static GUIComboBox m_gcb;
        private static GUIEditBox m_geb;
 
        private string selectedText = "";

        public VGUIComboBox(GUIEnvironment guienv, GUIElement parent, int id,
                              Rect rect)
            : base(guienv, parent, id, rect)
        {
            driver = guienv.VideoDriver;

            
            // For some reason passing this as a parent parameter doesn't work.
            // Probably that's because initialization of the managed part 
            // is not finished yet, so this points to elsewhere but a complete class
            m_gcb = guienv.AddComboBox(rect, null, -1);
            m_geb = guienv.AddEditBox("", new Rect(rect.UpperLeftCorner,new Dimension2D(rect.Size.Width - 15,rect.Size.Height)), true, null, -1);
            m_geb.Noclip = true;
            m_gcb.Noclip = true;
            m_gcb.AddChild(m_geb);
            //m_geb.AddChild(m_gcb);
            

            // let's workaround this by calling addchild instantly
            AddChild(m_gcb);
            //AddChild(m_geb);
            base.BringToFront(m_geb);
            m_geb.BringToFront(m_geb);
            m_gcb.BringToFront(m_geb);
            
        }

        public override string Text
        {
            get { return selectedText; }
            set { 
                selectedText = value;
                m_geb.Text = value;
            }
        }
        public void AddItem(string str)
        {
            m_gcb.AddItem(str);
        }
        public override void Draw()
        {
            //driver.Draw2DRectangle(this.AbsolutePosition, clicked ? Color.Red : Color.Black);
            base.Draw();
        }

        public override bool OnEvent(Event ev)
        {

            if (m_geb.OnEvent(ev))
                return true;

            System.Console.WriteLine(ev.Type.ToString());
            //System.Console.WriteLine(ev.Caller.Text);
            if (ev.Type == EventType.GUIEvent && ev.GUIEvent == GUIEventType.ComboBoxChanged)
            {
                Text = m_gcb.Text;
                m_geb.BringToFront(m_geb);
            }
            if (ev.Type == EventType.GUIEvent)
            {
                if (ev.GUIEvent == GUIEventType.EditBoxEnter)
                    m_gcb.Text = m_gcb.Text;
                    return true;
                //System.Console.WriteLine(ev.GUIEvent.ToString());
            }
            if (m_gcb.OnEvent(ev))
                return true;

            
            
            return base.OnEvent(ev);
        }


    }
}
