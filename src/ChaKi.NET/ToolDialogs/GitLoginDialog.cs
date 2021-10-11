using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.ToolDialogs
{
    public partial class GitLoginDialog : Form
    {
        public string Username { get { return this.textBox2.Text; } }
        public string Password { get { return this.textBox3.Text; } }

        public bool SaveCredentials { get { return this.checkBox1.Checked; } }

        public string Url { set { this.textBox1.Text = value; } }

        public GitLoginDialog()
        {
            InitializeComponent();
            this.textBox2.Focus();
        }
    }
}
