using ChaKi.Service.Git;
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
    public partial class GitSetIdentityDialog : Form
    {
        private IGitService m_Service;

        public string Username { get { return this.textBox1.Text; } }

        public string Email { get { return this.textBox2.Text; } }

        public GitSetIdentityDialog(IGitService svc)
        {
            this.m_Service = svc;

            InitializeComponent();

            string name, email;
            svc.GetIdentity(out name, out email);
            this.textBox1.Text = name;
            this.textBox2.Text = email;
        }
    }
}
