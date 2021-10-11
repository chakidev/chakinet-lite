using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using PopupControl;

namespace ChaKi.Common
{
    public partial class ColorListBox : UserControl
    {
        public Color Color
        {
            get { return m_Color; }
            set
            {
                this.listBox1.SelectedIndex = FindIndexFromColor(value);
            }
        }
        private Color m_Color;

        public event EventHandler ColorSelected;

        static List<Color> KnownColorList;

        static ColorListBox()
        {
            string[] allColors = Enum.GetNames(typeof(KnownColor));
            string[] systemEnvironmentColors = new string[(typeof(SystemColors)).GetProperties().Length];
            int index = 0;
            foreach (MemberInfo member in (typeof(SystemColors)).GetProperties())
            {
                systemEnvironmentColors[index++] = member.Name;
            }

            KnownColorList = new List<Color>();
            foreach (string color in allColors)
            {
                if (Array.IndexOf(systemEnvironmentColors, color) < 0)
                {
                    KnownColorList.Add(Color.FromName(color));
                }
            }
            KnownColorList.Sort(new Comparison<Color>(
                (c1, c2) =>
                {
                    long c1val = (long)((uint)c1.A << 24 | (uint)c1.R << 16 | (uint)c1.G << 8 | (uint)c1.B);
                    long c2val = (long)((uint)c2.A << 24 | (uint)c2.R << 16 | (uint)c2.G << 8 | (uint)c2.B);
                    return (int)(c2val - c1val);
                }));
        }

        public ColorListBox()
        {
            InitializeComponent();

            foreach (Color c in KnownColorList)
            {
                int index = this.listBox1.Items.Add(c);
            }
            this.Color = Color.White; // default
        }

        private static int FindIndexFromColor(Color c)
        {
            for (int i = 0; i < KnownColorList.Count; i++)
            {
                Color cc = (Color)KnownColorList[i];
                if (c.R == cc.R && c.G == cc.G && c.B == cc.B)
                {
                    return i;
                }
            }
            return -1;
        }

        public static Color FindNamedColor(Color c)
        {
            foreach (Color cc in KnownColorList)
            {
                if (c.R == cc.R && c.G == cc.G && c.B == cc.B)
                {
                    return cc;
                }
            }
            return c;
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            int i = e.Index;
            if (i < 0 || i >= KnownColorList.Count) return;

            Color c = (Color)this.listBox1.Items[e.Index];
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle clipBounds = e.Bounds;
            Rectangle colorBoxRect = new Rectangle(clipBounds.X+3, clipBounds.Y+3, 25, clipBounds.Height-6);
            RectangleF textBoxRect = new Rectangle(clipBounds.X+30, clipBounds.Y+3, clipBounds.Width-25-6, clipBounds.Height-6);

            Brush backBrush = new SolidBrush(e.BackColor);
            g.FillRectangle(backBrush, clipBounds);
            Brush brush = new SolidBrush(c);
            g.FillRectangle(brush, colorBoxRect);
            g.DrawRectangle(Pens.Black, colorBoxRect);
            g.DrawString(c.Name.ToString(), this.Font, Brushes.Black, textBoxRect);

            brush.Dispose();
            backBrush.Dispose();
       }

        private void listBox1_Click(object sender, EventArgs e)
        {
            int index = (this.listBox1.SelectedIndices.Count > 0) ? this.listBox1.SelectedIndices[0] : -1;
            if (index >= 0 && index < KnownColorList.Count)
            {
                m_Color = KnownColorList[index];
                if (ColorSelected != null)
                {
                    ColorSelected(this, null);
                }
            }
            Popup p = this.Parent as Popup;
            if (p != null)
            {
                p.Close(ToolStripDropDownCloseReason.ItemClicked);
            }
        }

        private void listBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '\r')
            {
                listBox1_Click(sender, e);
            }
            e.Handled = true;
        }

    }
}
