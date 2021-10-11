using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Extended;
using ChaKi.Common;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Settings;
using DependencyEditSLA.Properties;
using System.Windows.Forms;
using PopupControl;
using ChaKi.Common.Settings;

namespace DependencyEditSLA.Widgets
{
    internal enum SegmentBoxHitTestResult
    {
        None,
        HitLayerButton,
        HitSegment,
    }

    internal class SegmentBox : INestable, ISelectable
    {
        static Font m_Font;
        static Brush m_SelBrush;
        static Image m_LayerDownImage;
        static Brush m_HilightBrush;

        private Rectangle m_LayerButtonRect;
        private bool m_HoverOnPartEditButton;

        private static Popup m_PopupMenu;
        public event EventHandler<TagChangedEventArgs> TagChanged;

        static SegmentBox()
        {
            m_Font = new Font("Lucida Sans Unicode", 7f, FontStyle.Bold);
            m_SelBrush = new HatchBrush(HatchStyle.ForwardDiagonal, Color.Red, Color.Transparent);
            m_LayerDownImage = Resources.LayerDown;
            m_HilightBrush = new SolidBrush(Color.FromArgb(80, 0, 255, 255));

            m_PopupMenu = TagSelector.PreparedPopups[ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT];
        }

        public SegmentBox(CharRange r, Segment s, SegmentBoxGroup p)
        {
            this.Range = r;
            this.Model = s;
            this.Parent = p;
            this.Level = 0;
            this.Selected = false;
            m_LayerButtonRect = Rectangle.Empty;
            m_HoverOnPartEditButton = false;
        }

        public Annotation Model { get; set; }
        public SegmentBoxGroup Parent { get; set; }
        public CharRange Range { get; set; }
        public Rectangle Bounds { get; set; }
        public int Level { get; set; }
        public bool Selected { get; set; }

        private bool m_IsHover = false;

        public void OnHover(Control parent, bool hover = true)
        {
            if (m_IsHover != hover)
            {
                m_IsHover = hover;
                parent.Invalidate(m_LayerButtonRect);
            }
        }

        public SegmentBoxHitTestResult HitTest(Point p)
        {
            if (m_LayerButtonRect.Contains(p))
            {
                return SegmentBoxHitTestResult.HitLayerButton;
            }
            if (this.Bounds != null && this.Bounds.Contains(p))
            {
                return SegmentBoxHitTestResult.HitSegment;
            }
            return SegmentBoxHitTestResult.None;
        }

        public void Draw(Graphics g, Rectangle rect)
        {
            var levelmarginx = (int)DepEditSettings.Current.SegmentBoxLevelMarginX;
            var levelmarginy = (int)DepEditSettings.Current.SegmentBoxLevelMarginY;
            var lv = this.Level + 1;
            rect.Offset(0, -levelmarginy * lv);
            rect.Inflate(levelmarginx * 2 * lv, levelmarginy * 2 + levelmarginy * 2 * lv);
            g.SetClip(rect);
            ExtendedGraphics eg = new ExtendedGraphics(g);
            string tagStr = string.Empty;
            // Groupの描画
            if (this.Parent != null)
            {
                // Groupの描画色はParent(SegmentBoxGroup)のIndexに従う.
                tagStr = string.Format("{0}: {1}", this.Parent.Index + 1, this.Parent.Model.Tag.Name);
                eg.FillRoundRectangle(CyclicColorTable.GetBrush(this.Parent.Index), rect, 4);
            }
            // Segmentの描画
            Pen pen = SegmentPens.Find(this.Model.Tag.Name);
            eg.DrawRoundRectangle(pen, rect, 4);
            this.Bounds = rect;
            if (this.Selected)
            {
                g.FillRectangle(m_SelBrush, rect);
            }

            if (tagStr.Length == 0)
            {
                tagStr = this.Model.Tag.Name; // Groupのタグ表示を優先する.
            }
            g.DrawString(tagStr, m_Font, Brushes.Black, rect.Left + 2, rect.Top + 4, new StringFormat(StringFormatFlags.NoClip));

            g.ResetClip();

            m_LayerButtonRect = new Rectangle(rect.Left + 6, rect.Top - 6, 12, 12);

            if (m_IsHover)
            {
                g.DrawImage(m_LayerDownImage, m_LayerButtonRect);
                if (m_HoverOnPartEditButton)
                {
                    //RECT r = new RECT() { Left = m_LayerButtonRect.Left, Top = m_LayerButtonRect.Top, Right = m_LayerButtonRect.Right, Bottom = m_LayerButtonRect.Bottom };
                    //NativeFunctions.DrawFrameControl(g.GetHdc(), ref r, 4/*=DFC_BUTTON*/, 0x0010 | 0x1000/*=DFCS_BUTTONPUSH | DFCS_HOT*/);
                    g.FillRectangle(m_HilightBrush, m_LayerButtonRect);
                }
            }
        }

        // PartEditボタン部分の矩形部分について、マウスOver時にPushButton Controlと同等のハイライトを行う.
        public void HilightPartEditButton(Control parent, bool onoff)
        {
            if (m_HoverOnPartEditButton != onoff)
            {
                m_HoverOnPartEditButton = onoff;
                parent.Invalidate(m_LayerButtonRect);
            }
        }

        public void InvalidateElement(Control container)
        {
            container.Invalidate(this.Bounds);
        }

        public void DoEditTaglabel(Control sender, Point location)
        {
            m_PopupMenu.Location = location;
            var selector = (TagSelector)m_PopupMenu.Content;
            selector.TagSelected += selector_TagSelected;
            m_PopupMenu.Closed += PopupMenu_Closed;
            m_PopupMenu.Show(sender);
        }

        private void PopupMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            m_PopupMenu.Closed -= PopupMenu_Closed;
            if (!(sender is Popup)) return;
            ((TagSelector)m_PopupMenu.Content).TagSelected -= selector_TagSelected;
        }

        private void selector_TagSelected(object sender, EventArgs e)
        {
            var selector = sender as TagSelector;
            if (selector == null) return;
            TagChanged?.Invoke(this, new TagChangedEventArgs(selector.Selection.Name));
        }
    }
}
