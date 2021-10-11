using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DependencyEditSLA
{
    public partial class EditCommentDialog : Form
    {
        public EditCommentDialog(string comment)
        {
            InitializeComponent();

            this.textBox1.Text = comment;
        }

        public string Comment
        {
            get
            {
                return this.textBox1.Text;
            }
        }
    }
}
