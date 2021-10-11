using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using PopupControl;

namespace ChaKi.Common
{
    public partial class ColorPickerButton : Button
    {
        public ColorPickerButton()
        {
            InitializeComponent();

            this.Color = Color.Black;
        }

        public Color Color
        {
            get { return m_Color; }
            set
            {
                m_Color = ColorListBox.FindNamedColor(value);
                m_Editor.Color = m_Color;
            }
        }
        private Color m_Color;

        public event EventHandler ColorChanged;

        private ColorListBox m_Editor = new ColorListBox();

        private void ColorPickerButton_Click(object sender, EventArgs e)
        {
                m_Editor.Color = this.Color;
                m_Editor.ColorSelected += new EventHandler(
                    (o, a) =>
                    {
                        m_Color = m_Editor.Color;
                        if (ColorChanged != null) ColorChanged(this, null);
                    });
                Popup popup = new Popup(m_Editor) { DropShadowEnabled = true, FocusOnOpen = true };
                popup.Show(this);
        }


        private void ColorPickerButton_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle bounds = this.ClientRectangle;
            Rectangle colorBoxRect;
            RectangleF textBoxRect;
            GetDisplayLayout(bounds, out colorBoxRect, out textBoxRect);

            Brush brush = new SolidBrush(this.Color);
            graphics.FillRectangle(brush, colorBoxRect);
            graphics.DrawRectangle(Pens.Black, colorBoxRect);
            graphics.DrawString(this.Color.Name.ToString(), this.Font, System.Drawing.Brushes.Black, textBoxRect);
            brush.Dispose();
        }

        protected void GetDisplayLayout(Rectangle CellRect, out Rectangle colorBoxRect, out RectangleF textBoxRect)
        {
            colorBoxRect = new Rectangle(
                CellRect.X + 4, CellRect.Y + 4,
                25, CellRect.Height - 10);

            textBoxRect = new RectangleF(
                CellRect.X + 30, CellRect.Y + 3,
                CellRect.Width - colorBoxRect.Width - 8, CellRect.Height - 6);
        }
    }
}
