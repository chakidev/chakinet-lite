using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ChaKi.Common.Widgets
{
    public partial class AddinDialog : Form
    {
        private object m_Parameter;
        private Action<object, TextWriter> m_Command;
        private TextWriter m_Writer;

        public AddinDialog(object commandParameter, Action<object, TextWriter> command)
        {
            InitializeComponent();

            m_Command = command;
            this.button1.Click += new EventHandler(button1_Click);
            m_Parameter = commandParameter;

            if (m_Parameter != null)
            {
                this.propertyGrid1.SelectedObject = m_Parameter;
            }
            else
            {
                this.propertyGrid1.SelectedObject = new SampleParam();
            }

            m_Writer = new RichTextBoxWriter(this.richTextBox1);
        }

        void button1_Click(object sender, EventArgs e)
        {
            m_Command(m_Parameter, m_Writer);
            //this.richTextBox1.Text = m_Writer.ToString();
        }
    }

    class SampleParam
    {
        public enum TestEnum
        {
            One,
            Two,
            Tree
        }

        public SampleParam()
        {
            this.IntegerValue = 0;
            this.StringValue = "Hello, world!";
            this.BooleanValue = false;
            this.EnumValue = TestEnum.One;
            this.ColorValue = System.Drawing.Color.Red;
        }

        public int IntegerValue { get; set; }

        public string StringValue { get; set; }

        public bool BooleanValue { get; set; }

        public TestEnum EnumValue { get; set; }

        public System.Drawing.Color ColorValue { get; set; }

    }
}
