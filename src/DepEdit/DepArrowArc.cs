using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace DependencyEdit
{
    class DepArrowArc : DepArrow
    {
        [DllImport("gdi32.dll")]
        static extern bool PtInRegion(IntPtr hrgn, int X, int Y);


        public DepArrowArc(int _id, int _from, int _to, string _tag, Rectangle _fromBox, Rectangle _toBox)
            : base(_id, _from, _to, _tag, _fromBox, _toBox)
        {
            this.StartPnt = new Point((_fromBox.Right + _fromBox.Left) / 2, _fromBox.Top);
            this.EndPnt = new Point(_toBox.Left, _toBox.Top);
            this.EndPnt2 = new Point(_toBox.Right, _toBox.Top);

            m_Region = new Region();
        }

        public override void Draw(Graphics g, bool bDraft, Point offset)
        {
            Pen pen = m_Pen[1];
            if (IsSelected) pen = m_Pen[2];
            if (IsCrossing) pen = m_Pen[4];

            DrawParamsArc dp = new DrawParamsArc(this.StartPnt, this.EndPnt, this.EndPnt2, bDraft, offset);

            // HitTest領域を求めて保存しておく
            if (true/*typeid(*pDC) != typeid(CMetaFileDC)*/)
            {
                GraphicsPath gp = new GraphicsPath();
                gp.AddBezier(dp.ps, dp.ps1, dp.pe1, dp.pe);
                gp.Widen(m_Pen[0]);

                if (m_Region != null)
                {
                    m_Region.Dispose();
                    m_Region = new Region(gp);
                }
            }
            //本来の描画処理
            g.DrawBezier(pen, dp.ps, dp.ps1, dp.pe1, dp.pe);

            // タグラベルの更新処理
            if (!bDraft)
            {
                RectangleF r = m_Region.GetBounds(g);
                this.TagLabel.Location = new Point((int)(r.Left + r.Right) / 2, (int)r.Top);
                this.TagLabel.Text = this.Tag;
            }
        }

        public override DAHitType HitTest(Graphics g, Point point)
        {
            // Hit at other neighbouring area?
            if (PtInRegion(m_Region.GetHrgn(g), point.X, point.Y))
            {
                return DAHitType.Other;
            }
            return DAHitType.None;
        }

        public override void MoveHeadTo(Point point)
        {
            this.EndPnt = point;
        }
    }


    class DrawParamsArc
    {
        public static double CurveParamX = 0.05;
        public static double CurveParamY = 0.14;

        public DrawParamsArc(Point _s, Point _e, Point _e2, bool bDraft, Point offset)
        {
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
            ps1 = new Point((int)(ps.X + w * CurveParamX), (int)(ps.Y - Math.Abs(w) * CurveParamY));
            pe1 = new Point((int)(pe.X - w * CurveParamX), (int)(pe.Y - Math.Abs(w) * CurveParamY));
            pm = new Point((ps1.X + pe1.X) / 2, (ps1.Y + pe1.Y) / 2);

            tagLocation = new Point(pm.X + 5, (int)(ps.Y - Math.Abs(w) * CurveParamY * 0.9));

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

