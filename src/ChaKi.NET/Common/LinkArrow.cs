using System;
using System.Drawing;
using ChaKi.Entity.Search;
using ChaKi.Common;
using System.Windows.Forms;
using System.Collections.Generic;
using ChaKi.Common.Settings;
using ChaKi.Entity.Corpora.Annotations;
using System.Drawing.Drawing2D;

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
        private ComboBox m_TagBox;
        private Button m_AttributeButton;
        private EditAttributesDialog m_EditAttributesDlg;
        private TagSelector m_LinkTagSource;

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
            this.Text = cond.Text ?? "*";
            m_Points = new Point[6];
            m_Parent = parent;
            m_LinkTagSource = TagSelector.PreparedSelectors[Tag.LINK];
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
            m_TagBox = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "combo1",
                TabIndex = 0,
                DropDownWidth = 300,
            };
            if (m_TagBox.Items.IndexOf(this.Text) < 0)
            {
                m_TagBox.Items.Add(this.Text);
            }
            m_TagBox.SelectedItem = this.Text;
            TagBox_SelectedValueChanged(null, EventArgs.Empty);  // サイズを強制更新
            m_TagBox.DropDown += TagBox_DropDown;
            m_TagBox.SelectedValueChanged += TagBox_SelectedValueChanged;
            m_AttributeButton.BackColor = (this.Link.LinkAttrs.Count > 0) ? Color.Crimson : Color.DodgerBlue;
            m_AttributeButton.Click += AttributeButton_Click;
            //m_Parent.Controls.Add(m_AttributeButton);  // hide attribute button
            m_Parent.Controls.Add(m_TagBox);
            m_EditAttributesDlg = new EditAttributesDialog();
            m_EditAttributesDlg.SetData(this.Link.LinkAttrs);
        }

        private void TagBox_SelectedValueChanged(object sender, EventArgs e)
        {
            this.Text = m_TagBox.Text;
            this.Link.Text = this.Text;
            m_TagBox.Select(0, 0);

            // ComboBoxのサイズと位置をテキストに合わせて調整
            var sz = TextRenderer.MeasureText(this.Text, m_Font);
            var mp = new PointF((m_Points[2].X + m_Points[3].X) / 2F, (m_Points[2].Y + m_Points[3].Y) / 2F);
            m_TextRect = new RectangleF(mp, sz);
            m_TagBox.Location = new Point((int)(mp.X - sz.Width / 2.0), (int)(m_TextRect.Top + 1));
            m_TagBox.Width = (int)(sz.Width + 40);
        }

        private void TagBox_DropDown(object sender, EventArgs e)
        {
            var curcorpusname = (ChaKiModel.CurrentCorpus != null) ? (ChaKiModel.CurrentCorpus.Name) : string.Empty;
            List<Tag> tags = (m_LinkTagSource != null) ? m_LinkTagSource.GetTagsForCorpus(curcorpusname) : null;
            var maxWidth = 50;
            if (tags != null)
            {
                m_TagBox.Items.Clear();
                m_TagBox.Items.Add("*");
                foreach (var tag in tags)
                {
                    m_TagBox.Items.Add(tag.Name);

                    // Measure max width
                    var w = TextRenderer.MeasureText(tag.Name, m_TagBox.Font).Width;
                    maxWidth = Math.Max(maxWidth, w);
                }
            }
            else
            {
                m_TagBox.Items.Clear();
                m_TagBox.Items.Add("*");
                m_TagBox.Items.Add("D");
            }
            m_TagBox.DropDownWidth = maxWidth + 10;
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
            var rad = 5;
            var xoffs = 4;
            var yoffs = 10;
            var scale = new SizeF(g.DpiX / 96.0F, g.DpiY / 96.0F);
            rad = (int)(rad * scale.Width);
            xoffs = (int)(xoffs * scale.Width);
            yoffs = (int)(yoffs * scale.Height);
            var dx = (s.X < e.X) ? rad : -rad;
            m_Points[0] = new Point(s.X + level * xoffs, s.Y);
            m_Points[1] = new Point(s.X + level * xoffs, y + (level + 1) * yoffs + (yoffs + rad) - rad);
            m_Points[2] = new Point(s.X + level * xoffs + dx, y + (level + 1) * yoffs + (yoffs + rad));
            m_Points[3] = new Point(e.X + level * xoffs - dx, y + (level + 1) * yoffs + (yoffs + rad));
            m_Points[4] = new Point(e.X + level * xoffs, y + (level + 1) * yoffs + (yoffs + rad) - rad);
            m_Points[5] = new Point(e.X + level * xoffs, e.Y);

            var path = new GraphicsPath();
            path.AddLine(m_Points[0], m_Points[1]);
            if (dx > 0)
            {
                path.AddArc(m_Points[1].X, m_Points[1].Y - rad, rad * 2, rad * 2, 180, -90);
            }
            else
            {
                path.AddArc(m_Points[2].X - rad, m_Points[1].Y - rad, rad * 2, rad * 2, 0, 90);
            }
            path.AddLine(m_Points[2], m_Points[3]);
            if (dx > 0)
            {
                path.AddArc(m_Points[3].X - rad, m_Points[4].Y - rad, rad * 2, rad * 2, 90, -90);
            }
            else
            {
                path.AddArc(m_Points[4].X, m_Points[4].Y - rad, rad * 2, rad * 2, 90, 90);
            }
            path.AddLine(m_Points[4], m_Points[5]);
            g.DrawPath(m_Pen, path);

            if (drawText)
            {
                var sz = g.MeasureString(this.Text, m_Font);
                var mp = new PointF((m_Points[2].X + m_Points[3].X) / 2F, (m_Points[2].Y + m_Points[3].Y) / 2F);
                m_TextRect = new RectangleF(mp, sz);
                //m_AttributeButton.Location = new Point((int)(m_TextRect.Right + 3), (int)(m_TextRect.Top));
                m_TagBox.Location = new Point((int)(mp.X - sz.Width/2.0), (int)(m_TextRect.Top + 1));
            }
        }

        public LinkArrowHitType HitTest(Point p)
        {
            if (m_TextRect.Contains((float)p.X, (float)p.Y))
            {
                return LinkArrowHitType.AtText;
            }
            int x1 = Math.Min(m_Points[0].X, m_Points[5].X);
            int x2 = Math.Max(m_Points[0].X, m_Points[5].X);
            int y = m_Points[2].Y;
            if (p.X >= x1 && p.X <= x2 && p.Y > y - 4 && p.Y < y + 4)
            {
                return LinkArrowHitType.AtArrow;
            }

            return LinkArrowHitType.None;
        }


    }
}
