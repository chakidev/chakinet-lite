using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Options
{
    public partial class FontSampleLabel : UserControl
    {
        public new Font Font {
            get
            {
                return m_Font;
            }
            set
            {
                m_Font = value;
                Invalidate();
            }
        } Font m_Font;

        public FontSampleLabel()
        {
            InitializeComponent();
        }

        private void FontSampleLabel_Paint(object sender, PaintEventArgs e)
        {
            if (m_Font != null)
            {
                Graphics g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                string str = m_Font.Name;
                g.DrawString(str, m_Font, Brushes.Black, new PointF(0F, 3F));
            }
        }
    }
}
