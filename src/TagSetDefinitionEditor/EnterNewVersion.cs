using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.TagSetDefinitionEditor
{
    public partial class EnterNewVersion : Form
    {
        public EnterNewVersion()
        {
            InitializeComponent();
        }

        public string VersionName
        {
            get
            {
                return this.textBox1.Text;
            }
        }

        private void EnterNewVersion_Validating(object sender, CancelEventArgs e)
        {
            if (this.textBox1.Text.Length == 0)
            {
                e.Cancel = true;
            }
        }
    }
}
