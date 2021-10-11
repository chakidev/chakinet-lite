using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Properties;

namespace ChaKi.Common
{
    public partial class ExpandButton : UserControl
    {
        private Bitmap[] m_Image = new Bitmap[2];

        public ExpandButton()
        {
            InitializeComponent();

            m_Image[0] = Resources.ExpanderDown;
            m_Image[1] = Resources.ExpanderUp;
            foreach (Bitmap bmp in m_Image)
            {
                bmp.MakeTransparent(Color.Magenta);
            }

            this.pictureBox1.Image = m_Image[0];
            this.Checked = false;
        }

        public event EventHandler CheckedChanged;
        public bool Checked { get; set; }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (!this.Checked)
            {
                this.pictureBox1.Image = m_Image[1];
                this.Checked = true;
            }
            else
            {
                this.pictureBox1.Image = m_Image[0];
                this.Checked = false;
            }
            if (this.CheckedChanged != null)
            {
                CheckedChanged(this, null);
            }
        }
    }
}
