using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Extended;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Common;

namespace ChaKi.Views.KwicView
{
    internal class LinkElement
    {
        public Link Link { get; set; }
        public SegElement SegFrom { get; set; }
        public SegElement SegTo { get; set; }

        public void Render(ExtendedGraphics eg)
        {
            if (this.Link == null)
            {
                return;
            }
            Graphics g = eg.Graphics;
            if (this.SegFrom == null || this.SegTo == null)
            {
                return;
            }
            Rectangle r1 = this.SegFrom.Bounds;
            Rectangle r2 = this.SegTo.Bounds;

            if (r1.IsEmpty || r2.IsEmpty)
            {
                return;
            }
            Pen orgPen = LinkPens.Find(this.Link.Tag.Name);
            Pen pen = (Pen)orgPen.Clone();
            Brush br = new SolidBrush(pen.Color);
            eg.FillRoundRectangleLower(br, r1, 3, 2);
            if (this.Link.IsDirected)
            {
                eg.FillRoundRectangleUpper(br, r2, 3, 2);
            }
            else
            {
                eg.FillRoundRectangleLower(br, r2, 3, 2);
            }
            Point[] pts = new Point[] {
                    new Point((r1.Left + r1.Right) / 2, (r1.Top + r1.Bottom) / 2),
                    new Point((r1.Left + r1.Right) / 2, (r1.Top + r1.Bottom) / 2),
                    new Point((r2.Left + r2.Right) / 2, (r2.Top + r2.Bottom) / 2),
                    new Point((r2.Left + r2.Right) / 2, (r2.Top + r2.Bottom) / 2)
                };
            if (pts[0].Y == pts[3].Y)
            {   // 同行内のリンク
                pts[0].Offset(0, 10);
                pts[1].Offset(0, 30);
                pts[2].Offset(0, 30);
                pts[3].Offset(0, 10);
            }
            else if (pts[0].Y < pts[3].Y)
            {   // 下行へのリンク
                pts[0].Offset(0, 10);
                pts[1].Offset(0, 30);
                pts[2].Offset(0, -30);
                pts[3].Offset(0, -10);
            }
            else if (pts[0].Y > pts[3].Y)
            {   // 上行へのリンク
                pts[0].Offset(0, -10);
                pts[1].Offset(0, -30);
                pts[2].Offset(0, 30);
                pts[3].Offset(0, 10);
            }
            if (pts[0].X > pts[3].X)
            {   // 左へのリンク
                pts[1].Offset(-20, 0);
                pts[2].Offset(20, 0);
            }
            else if (pts[0].X > pts[3].X)
            {   // 右へのリンク
                pts[1].Offset(20, 0);
                pts[2].Offset(-20, 0);
            }
            if (this.Link.IsDirected)
            {
                pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            }
            else
            {
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Flat;
            }
            g.DrawBezier(pen, pts[0], pts[1], pts[2], pts[3]);
            pen.Dispose();
            br.Dispose();
        }
    }
}
