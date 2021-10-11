using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.GUICommon
{
    public partial class ExceptionDialogBox : Form
    {
        public ExceptionDialogBox()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public new string Text
        {
            get
            {
                return this.richTextBox1.Text;
            }
            set
            {
                richTextBox1.SelectedText = value;
            }
        }
    }
}
