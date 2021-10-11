using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.DocEdit
{
    public partial class DocEditSettingsDialog : Form
    {
        private int m_TextSnipLength;
        public int TextSnipLength
        {
            get
            {
                int val;
                if (!Int32.TryParse(this.textBox1.Text, out val))
                {
                    val = 10;
                }
                return val;
            }
            set { m_TextSnipLength = value; }
        }

        public string UILocale
        {
            get
            {
                int i;
                if ((i = this.comboBox1.Text.IndexOf(' ')) < 0)
                {
                    return this.comboBox1.Text;
                }
                return this.comboBox1.Text.Substring(0, i);
            }
            set
            {
                this.comboBox1.Text = value;
            }
        }

        public DocEditSettingsDialog()
        {
            InitializeComponent();

            this.comboBox1.Items.Add("en-US (English)");
            this.comboBox1.Items.Add("ja-JP (日本語)");
        }

        private void DocEditSettingsDialog_Load(object sender, EventArgs e)
        {
            this.textBox1.Text = m_TextSnipLength.ToString();
        }
    }
}
