using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ChaKi.Common
{
    public class RichTextBoxWriter : TextWriter
    {
        private RichTextBox m_RichTextBox;

        public RichTextBoxWriter(RichTextBox rtb)
        {
            m_RichTextBox = rtb;
        }

        public override Encoding Encoding
        {
            get { return null; }
        }
        
        public override void Write(char value)
        {
            if (value != '\r')
            {
                m_RichTextBox.AppendText(new string(value, 1));
            }
        }

        public override void WriteLine()
        {
            m_RichTextBox.AppendText("\n");
        }

        public override void WriteLine(string value)
        {
            m_RichTextBox.AppendText(value);
            m_RichTextBox.AppendText("\n");
        }

        public override void WriteLine(string format, Object arg0)
        {
            WriteLine(format, arg0, null, null);
        }

        public override void WriteLine(string format, Object arg0, Object arg1)
        {
            WriteLine(format, arg0, arg1, null);
        }

        public override void WriteLine(string format, Object arg0, Object arg1, Object arg2)
        {
            m_RichTextBox.AppendText(string.Format(format, arg0, arg1, arg2));
            m_RichTextBox.AppendText("\n");
        }

        public override void WriteLine(string format, Object[] args)
        {
            m_RichTextBox.AppendText(string.Format(format, args));
            m_RichTextBox.AppendText("\n");
        }
    }
}
