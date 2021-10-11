using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DependencyEditSLA
{
    public partial class LineNumberInputForm : Form
    {
        public LineNumberInputForm()
        {
            InitializeComponent();
        }

        public int LineNumber
        {
            get
            {
                int ln;
                if (Int32.TryParse(this.textBox1.Text, out ln))
                {
                    return ln;
                }
                return -1;
            }
            set
            {
                this.textBox1.Text = value.ToString();
            }
        }
    }
}
