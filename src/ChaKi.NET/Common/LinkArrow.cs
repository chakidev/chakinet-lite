using System;
using System.Drawing;
using ChaKi.Entity.Search;
using ChaKi.Common;
using System.Windows.Forms;
using System.Collections.Generic;
using ChaKi.Common.Settings;

namespace ChaKi.GUICommon
{
    internal class LinkArrow
    {
        public int From { get; set; }
        public int To { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
        public LinkCondition Link { get; private set; }

        private Point m_Start;
        private Point m_End;
        private Point[] m_Points; // 折線の通過点
        private RectangleF m_TextRect;

        private Control m_Parent;
        private Button m_AttributeButton;
        private EditAttributesDialog m_EditAttributesDlg;

        private static Pen m_Pen;
        private static Font m_Font;

        static LinkArrow()
        {
            var srcPen = LinkPens.Find("D");
            m_Pen = (Pen)srcPen.Clone();
            m_Pen.Width = 1.5F;
            m_Pen.Color = Color.Gray;

            m_Font = new Font("Arial", 8F);
        }

        public LinkArrow(Control parent, LinkCondition cond)
        {
            this.Link = cond;
            this.Text = "*";
            m_Points = new Point[4];
            m_Parent = parent;
            m_AttributeButton = new Button()
            {   // cf. BunsetsuBox.Designer.csの"button"のデザインと同一とする。
                Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128))),
                ForeColor = System.Drawing.Color.White,
                Location = new System.Drawing.Point(0, 0),
                Margin = new System.Windows.Forms.Padding(0),
                Name = "button1",
                Size = new System.Drawing.Size(25, 25),
                TabIndex = 0,
                Text = "a",
                UseVisualStyleBackColor = false,
            };
            m_AttributeButton.BackColor = (this.Link.LinkAttrs.Count > 0) ? Color.Crimson : Color.DodgerBlue;
            m_AttributeButton.Click += AttributeButton_Click;
            m_Parent.Controls.Add(m_AttributeButton);
            m_EditAttributesDlg = new EditAttributesDialog();
            m_EditAttributesDlg.SetData(this.Link.LinkAttrs);
        }

        // 属性条件の編集
        private void AttributeButton_Click(object sender, EventArgs e)
        {
            var pos = m_Parent.PointToScreen(m_AttributeButton.Location);
            m_EditAttributesDlg.Location = pos;
            m_EditAttributesDlg.SetData(this.Link.LinkAttrs);
            if (m_EditAttributesDlg.ShowDialog() == DialogResult.OK)
            {
                m_EditAttributesDlg.GetData(this.Link.LinkAttrs);
                m_AttributeButton.BackColor = (this.Link.LinkAttrs.Count > 0) ? Color.Crimson : Color.DodgerBlue;
            }
        }

        //public LinkArrow(LinkArrow src)
        //{
        //    this.From = src.From;
        //    this.To = src.To;
        //    this.Text = string.Copy(src.Text);
        //    this.Selected = src.Selected;
        //}

        public void SetPosition(Point start, Point end)
        {
            m_Start = start;
            m_End = end;
        }

        public void SetStart(int x, int y)
        {
            m_Start = new Point(x, y);
            m_End = m_Start;
        }

        public void SetNewEnd(int x)
        {
            m_End.X = x;
        }

        public void Draw(Graphics g, int level, bool drawText)
        {
            var s = m_Start;
            var e = m_End;
            // 矢印の向きをDepEditの設定に同期して変更
            if (DepEditSettings.Current.ReverseDepArrowDirection)
            {
                s = m_End;
                e = m_Start;
            }

            var y = Math.Max(s.Y, e.Y);
            m_Points[0] = new Point(s.X + level * 4, s.Y);
            m_Points[1] = new Point(s.X + level * 4, y + (level + 1) * 10 + 15);
            m_Points[2] = new Point(e.X + level * 4, y + (level + 1) * 10 + 15);
            m_Points[3] = new Point(e.X + level * 4, e.Y);

            g.DrawLines(m_Pen, m_Points);

            if (drawText)
            {
                SizeF sz = g.MeasureString(this.Text, m_Font);
                PointF mp = new PointF((m_Points[1].X + m_Points[2].X) / 2F, (m_Points[1].Y + m_Points[2].Y) / 2F);
                mp.X -= (sz.Width / 2F);
                g.DrawString(this.Text, m_Font, Brushes.DarkGreen, mp);
                m_TextRect = new RectangleF(mp, sz); ;
                m_AttributeButton.Location = new Point((int)(m_TextRect.Right + 3), (int)(m_TextRect.Top));
            }
        }

        public LinkArrowHitType HitTest(Point p)
        {
            if (m_TextRect.Contains((float)p.X, (float)p.Y))
            {
                return LinkArrowHitType.AtText;
            }
            int x1 = Math.Min(m_Points[0].X, m_Points[3].X);
            int x2 = Math.Max(m_Points[0].X, m_Points[3].X);
            int y = m_Points[1].Y;
            if (p.X >= x1 && p.X <= x2 && p.Y > y - 4 && p.Y < y + 4)
            {
                return LinkArrowHitType.AtArrow;
            }

            return LinkArrowHitType.None;
        }


    }
}
