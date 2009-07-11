using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace IdealistViewer.UserInterface
{
    /// <summary>ChatText handles formatting of scolling text</summary>
    /// </summary>
    /// <remarks>
    /// This extension of the standard RichTextBox class centralizes all the scrolling
    /// behavior needed for chat and IM display.  It also manages the varying
    /// appearance of text from different sources.
    /// </remarks>
    //
    class ChatText : RichTextBox
    {
        public void AppendChat(string text)
        {
            // Put the new text at the bottom of the text area, and move the
            // scrollbar there.
            Text += text + System.Environment.NewLine;
            SelectionStart =
                TextLength - System.Environment.NewLine.Length;
            ScrollToCaret();
        }

        /// <summary>AppendChat adds one line of chat to a scrolling window.
        /// </summary>
        /// <param name="who">Name of the entity speaking</param>
        /// <param name="message">Text of what was said</param>
        /// <param name="level">Importance level.  0=Me, 1=Other say,
        /// 2=other shout</param>
        public void AppendChat(string who, string message, int level)
        {
            // Put the new text at the bottom of the text area, and move the
            // scrollbar there.
            System.Drawing.Font oldFont = SelectionFont;

            // First put the name of who is speaking, in an appropriate font.
            SelectionFont =
                new System.Drawing.Font(
                    oldFont.FontFamily,
                    oldFont.Size,
                    System.Drawing.FontStyle.Bold);
            // Blue for me speaking.
            if (level == 0)
                SelectionColor = System.Drawing.Color.Blue;
            else
                SelectionColor = System.Drawing.Color.Black;
            AppendText(who + ": ");

            // Then put what was said.
            SelectionFont =
                 new System.Drawing.Font(
                     oldFont.FontFamily,
                     oldFont.Size,
                     System.Drawing.FontStyle.Regular);

            // Shout in red; everything else black.
            if (level < 2)
                SelectionColor = System.Drawing.Color.Black;
            else
                SelectionColor = System.Drawing.Color.Red;
            AppendText( message );
 
            // Finally a newline, and scroll there.
//            SelectionStart = TextLength;
            AppendText( System.Environment.NewLine );
            ScrollToCaret();
        }


    }
}
