using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using System.Runtime.InteropServices;
using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Entity.Settings;
using System.Windows.Forms;

namespace DependencyEditSLA.Widgets
{
    internal class DepArrow : ISelectable
    {
        static DepArrow()
        {
            m_RegionPen = new Pen(Color.Gray, 12);
            m_RegionPen.SetLineCap(LineCap.RoundAnchor, LineCap.RoundAnchor, DashCap.Flat);

            m_Brush[0] = new SolidBrush(Color.Gray);
            m_Brush[1] = new SolidBrush(Color.Red);
            m_Brush[2] = new SolidBrush(Color.Black);
            m_Brush[3] = new SolidBrush(Color.DarkOrange);

            m_Font = new Font("Arial", 10, FontStyle.Italic);
        }

        public DepArrow(Link _link, int _from, int _to, Rectangle _fromBox, Rectangle _toBox, DepArrowPlacement placement = DepArrowPlacement.Bottom)
        {
            this.Visible = true;
            this.FromIndex = _from;
            this.ToIndex = _to;
            this.Selected = false;
            this.IsCrossing = false;
            this.Link = _link;
            this.Tag = _link.Tag.Name;
            this.Placement = placement;
            this.IsLayoutCalculated = false;

            // ラベル（コントロール）を作成
            m_TagLabel = new LinkTagLabel(_link);

            m_Path = null;

            // デフォルト（Diagonal Mode）の描画位置をセーブ
            RecalcLayout(_fromBox, _toBox);
        }

        public virtual void RecalcLayout(Rectangle fromBox, Rectangle toBox)
        {
            m_FromBox = fromBox;
            m_ToBox = toBox;
            if (this.Placement == DepArrowPlacement.Bottom)
            {
                this.StartPnt = new Point(fromBox.Left, fromBox.Bottom);
                this.EndPnt = new Point(toBox.Left, toBox.Bottom);
            }
            else
            {
                this.StartPnt = new Point(fromBox.Right, fromBox.Top);
                this.EndPnt = new Point(toBox.Right, toBox.Top);
            }
            this.IsLayoutCalculated = true;
        }

        public bool Visible { get; set; }
        public Point StartPnt { get; set; }
        public Point EndPnt { get; set; }
        public Point EndPnt2 { get; set; }
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
        public Link Link { get; set; }
        public string Tag { get; set; }
        public bool Selected { get; set; }
        public bool IsCrossing { get; set; }
        public DepArrowPlacement Placement { get; set; }
        public bool IsDependencyLink { get { return (this.Placement == DepArrowPlacement.Bottom); } }
        public bool IsLayoutCalculated { get; protected set; }

        public LinkTagLabel TagLabel
        {
            get { return m_TagLabel; }
        }

        public RectangleF Bounds
        {
            get
            {
                if (m_Path == null)
                {
                    return Rectangle.Empty;
                }
                return m_Path.GetBounds();
            }
        }

        public virtual void Draw(Graphics g, bool bDraft, Point offset)
        {
            if (!this.Visible)
            {
                return;
            }

            Pen srcPen = LinkPens.Find(this.Tag);
            Pen pen = (Pen)srcPen.Clone();
            if (Selected) pen.Color = Color.Red;
            if (IsCrossing) pen.Color = Color.DarkOrange;
            if (this.Tag == "P")
            {   // "P"はNon-directedなのでLinecapなし. TODO: 名称ではなくIsDirectedによる判定が必要
                pen.SetLineCap(LineCap.Square, LineCap.Square, DashCap.Flat);
            }

            DrawParams dp = new DrawParams(this.StartPnt, this.EndPnt, this.EndPnt2, bDraft, offset, this.Placement);

            // HitTest領域を求めて保存しておく
            if (true/*typeid(*pDC) != typeid(CMetaFileDC)*/)
            {
                m_Path = new GraphicsPath();
                m_Path.AddLines(new Point[] { dp.ps, dp.pm, dp.pe });
                m_Path.Widen(m_RegionPen);
            }
            //本来の描画処理
            if (!DepEditSettings.Current.ReverseDepArrowDirection)
            {
                g.DrawLines(pen, new Point[] { dp.ps, dp.pm, dp.pe });
            }
            else
            {
                g.DrawLines(pen, new Point[] { dp.pe, dp.pm, dp.ps });
            }

            // タグラベルの更新処理
            if (!bDraft)
            {
                this.TagLabel.Location = dp.tagLocation;
                this.TagLabel.Text = this.Tag;
            }

            pen.Dispose();
        }

        public virtual DAHitType HitTest(Graphics g, Point point)
        {
            if (m_Path == null)
            {
                return DAHitType.None;
            }
            // Hit at other neighbouring area?
            using (Pen p = new Pen(Color.Black, 10))
            {
                if (m_Path.IsOutlineVisible(point, p))
                {
                    return DAHitType.Other;
                }
            }
            return DAHitType.None;
        }

        public virtual void MoveHeadTo(Point point)
        {
            this.EndPnt = point;
        }

        protected Rectangle m_FromBox;
        protected Rectangle m_ToBox;
        protected GraphicsPath m_Path;  // 矢印のヒットエリア全体を表す領域オブジェクト
        protected Point m_TagLocation;  // タグ文字列の左上位置
        protected LinkTagLabel m_TagLabel;

        static protected Pen m_RegionPen;
        static protected Brush[] m_Brush = new Brush[4];
        static protected Font m_Font;


        public void InvalidateElement(Control container)
        {
            container.Invalidate(new Rectangle((int)this.Bounds.Left, (int)this.Bounds.Top, (int)this.Bounds.Width, (int)this.Bounds.Height));
        }

        public Annotation Model
        {
            get { return this.Link; }
        }
    }

    class DrawParams
    {
        public DrawParams(Point _s, Point _e, Point _e2, bool bDraft, Point offset, DepArrowPlacement placement)
        {
            isReverse = (_s.X > _e.X);
            ps = new Point(_s.X, _s.Y);
            if (!isReverse)
            {
                if (placement == DepArrowPlacement.Bottom)
                {
                    ps = new Point(_s.X + 8, _s.Y);
                    if (bDraft)
                    {
                        pe = _e;
                    }
                    else
                    {
                        pe = new Point(_e.X - 2, _e.Y - 16);
                    }
                    pm = new Point(ps.X, pe.Y);
                }
                else
                {
                    ps = new Point(_s.X + 2, _s.Y + 16);
                    if (bDraft)
                    {
                        pe = _e;
                    }
                    else
                    {
                        pe = new Point(_e.X - 8, _e.Y);
                    }
                    pm = new Point(pe.X, ps.Y);
                }
            }
            else
            {
                if (placement == DepArrowPlacement.Bottom)
                {
                    ps = new Point(_s.X, _s.Y - 8);
                    if (bDraft)
                    {
                        pe = _e;
                    }
                    else
                    {
                        pe = new Point(_e.X + 16, _e.Y);
                    }
                    pm = new Point(pe.X, ps.Y);
                }
                else
                {
                    ps = new Point(_s.X - 16, _s.Y);
                    if (bDraft)
                    {
                        pe = _e;
                    }
                    else
                    {
                        pe = new Point(_e.X, _e.Y + 8);
                    }
                    pm = new Point(ps.X, pe.Y);
                }
            }
            tagLocation = pm;

            ps.Offset(offset);
            pe.Offset(offset);
            pm.Offset(offset);
            tagLocation.Offset(offset);
        }

        public Point ps;
        public Point pe;
        public Point pm;
        public Point tagLocation;
        public bool isReverse;
    }
}
