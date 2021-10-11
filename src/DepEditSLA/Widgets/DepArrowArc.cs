using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Entity.Settings;

namespace DependencyEditSLA.Widgets
{
    class DepArrowArc : DepArrow
    {
        public DepArrowArc(Link _link, int _from, int _to, Rectangle _fromBox, Rectangle _toBox, DepArrowPlacement _placement = DepArrowPlacement.Bottom)
            : base(_link, _from, _to, _fromBox, _toBox, _placement)
        {
        }

        public override void RecalcLayout(Rectangle fromBox, Rectangle toBox)
        {
            m_FromBox = fromBox;
            m_ToBox = toBox;
            this.StartPnt = new Point((fromBox.Right + fromBox.Left) / 2, (this.Placement == DepArrowPlacement.Bottom) ? fromBox.Bottom : fromBox.Top);
            this.EndPnt = new Point(toBox.Left, (this.Placement == DepArrowPlacement.Bottom)? toBox.Bottom : toBox.Top);
            this.EndPnt2 = new Point(toBox.Right, (this.Placement == DepArrowPlacement.Bottom) ? toBox.Bottom: toBox.Top);
            this.IsLayoutCalculated = true;
        }

        public override void Draw(Graphics g, bool bDraft, Point offset)
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

            DrawParamsArc dp = new DrawParamsArc(this.StartPnt, this.EndPnt, this.EndPnt2, bDraft, offset, this.Placement);

            // HitTest領域を求めて保存しておく
            if (true/*typeid(*pDC) != typeid(CMetaFileDC)*/)
            {
                m_Path = new GraphicsPath();
                m_Path.AddBezier(dp.ps, dp.ps1, dp.pe1, dp.pe);
                m_Path.Widen(m_RegionPen);
            }
            //本来の描画処理
            if (!DepEditSettings.Current.ReverseDepArrowDirection)
            {
                g.DrawBezier(pen, dp.ps, dp.ps1, dp.pe1, dp.pe);
            }
            else
            {
                g.DrawBezier(pen, dp.pe, dp.pe1, dp.ps1, dp.ps);
            }

            // タグラベルの更新処理
            if (!bDraft)
            {
                RectangleF r = m_Path.GetBounds();
                this.TagLabel.Location = new Point((int)(r.Left+r.Right)/2, 
                    (this.Placement == DepArrowPlacement.Bottom)? (int)r.Bottom-5 : (int)r.Top - 15);
                this.TagLabel.Text = this.Tag;
            }
            pen.Dispose();
        }
    }


    class DrawParamsArc
    {
        public DrawParamsArc(Point _s, Point _e, Point _e2, bool bDraft, Point offset, DepArrowPlacement placement)
        {
            double curveParamX = DepEditSettings.Current.CurveParamX;
            double curveParamY = DepEditSettings.Current.CurveParamY;

            isReverse = (_s.X > _e.X);
            ps = new Point(_s.X, _s.Y);
            if (!bDraft)
            {
                if (!isReverse)
                {
                    pe = new Point(_e.X + 8, _e.Y);
                }
                else
                {
                    pe = new Point(_e2.X - 20, _e2.Y);
                }
            }
            else
            {
                pe = new Point(_e.X, _e.Y);
            }
            int w = pe.X - ps.X;
            if (placement == DepArrowPlacement.Bottom)
            {
                ps1 = new Point((int)(ps.X + w * curveParamX), (int)(ps.Y + Math.Abs(w) * curveParamY));
                pe1 = new Point((int)(pe.X - w * curveParamX), (int)(pe.Y + Math.Abs(w) * curveParamY));
                pm = new Point((ps1.X + pe1.X) / 2, (ps1.Y + pe1.Y) / 2);
                tagLocation = new Point(pm.X + 5, (int)(ps.Y + Math.Abs(w) * curveParamY * 0.9));
            }
            else
            {
                ps1 = new Point((int)(ps.X + w * curveParamX), (int)(ps.Y - Math.Abs(w) * curveParamY));
                pe1 = new Point((int)(pe.X - w * curveParamX), (int)(pe.Y - Math.Abs(w) * curveParamY));
                pm = new Point((ps1.X + pe1.X) / 2, (ps1.Y + pe1.Y) / 2);
                tagLocation = new Point(pm.X + 5, (int)(ps.Y - Math.Abs(w) * curveParamY * 0.9));
            }

            ps.Offset(offset);
            pe.Offset(offset);
            ps1.Offset(offset);
            pe1.Offset(offset);
            pm.Offset(offset);
            tagLocation.Offset(offset);
        }

        public Point ps;
        public Point pe;
        public Point ps1;
        public Point pe1;
        public Point pm;
        public Point tagLocation;
        public bool isReverse;
    }
}
