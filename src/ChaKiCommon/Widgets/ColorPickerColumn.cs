using System;
using System.Drawing;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using PopupControl;

namespace ChaKi.Common
{
    public class ColorPickerColumn : DataGridViewColumn
    {
        public ColorPickerColumn()
            : base(new ColorPickerCell())
        {
        }

        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }
            set
            {
                // Ensure that the cell used for the template is a ColorPickerCell.
                if (value != null && 
                    !value.GetType().IsAssignableFrom(typeof(ColorPickerCell)))
                {
                    throw new InvalidCastException("Must be a ColorPicker");
                }
                base.CellTemplate = value;
            }
        }
    }

    public class ColorPickerCell : DataGridViewButtonCell
    {
        public Color Color { get; set; }
        private DataGridView m_Parent = null;
        private ColorListBox m_Editor = new ColorListBox();

        public ColorPickerCell()
            : base()
        {
            this.Color = Color.Black;
        }

        protected void DataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == this.RowIndex && e.ColumnIndex == this.ColumnIndex)
            {
                m_Editor.Color = this.Color;
                m_Editor.ColorSelected += new EventHandler(
                    (o, a) =>
                    {
                        this.Color = m_Editor.Color;
                        m_Parent[e.ColumnIndex, e.RowIndex].Value = this.Color;
                    });
                Popup popup = new Popup(m_Editor) { DropShadowEnabled = true, FocusOnOpen = true };
                popup.Show(m_Parent, m_Parent.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false));
            }
        }

        protected override void Paint(Graphics graphics,
                                Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
                                DataGridViewElementStates elementState, object value,
                                object formattedValue, string errorText,
                                DataGridViewCellStyle cellStyle,
                                DataGridViewAdvancedBorderStyle advancedBorderStyle,
                                DataGridViewPaintParts paintParts)
        {
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            if (m_Parent == null && this.DataGridView != null)
            {
                m_Parent = this.DataGridView;
                m_Parent.CellContentClick += new DataGridViewCellEventHandler(DataGridView_CellContentClick);
            }

            formattedValue = null;

            base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue,
                       errorText, cellStyle, advancedBorderStyle, paintParts);


            Rectangle colorBoxRect;
            RectangleF textBoxRect;
            GetDisplayLayout(cellBounds, out colorBoxRect, out textBoxRect);

            //// Draw the cell background, if specified.
            if ((paintParts & DataGridViewPaintParts.Background) == DataGridViewPaintParts.Background)
            {
                SolidBrush cellBackground;
                if (value != null && value.GetType() == typeof(Color))
                {
                    cellBackground = new SolidBrush((Color)value);
                }
                else
                {
                    cellBackground = new SolidBrush(cellStyle.BackColor);
                }
                graphics.FillRectangle(cellBackground, colorBoxRect);
                graphics.DrawRectangle(Pens.Black, colorBoxRect);
                if (value != null)
                {
                    Color lclcolor = (Color)value;
                    graphics.DrawString(lclcolor.Name.ToString(), cellStyle.Font, System.Drawing.Brushes.Black, textBoxRect);
                }

                cellBackground.Dispose();
            }
            this.Color = (Color)value;
        }

        protected virtual void GetDisplayLayout(Rectangle CellRect, out Rectangle colorBoxRect, out RectangleF textBoxRect)
        {
            colorBoxRect = new Rectangle(
                CellRect.X + 4, CellRect.Y + 4,
                25, CellRect.Height - 10);

            textBoxRect = new RectangleF(
                CellRect.X + 30, CellRect.Y + 3,
                CellRect.Width - colorBoxRect.Width - 8, CellRect.Height - 6);
        }

        public override Type EditType
        {
            get { return typeof(string); }
        }

        public override Type ValueType
        {
            get { return typeof(Color); }
        }
        public override object DefaultNewRowValue
        {
            get { return Color.White; }
        }
    }
}
