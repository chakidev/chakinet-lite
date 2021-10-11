using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Extended;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Common;

namespace ChaKi.Views.KwicView
{
    internal class SegElement
    {
        public Segment Seg { get; set; }
        public CharRangeElement Range { get; set; }
        public int Level { get; set; }
        public Rectangle Bounds { get; private set; }

        static public int L1Height;
        static public int LineHeight;

        public SegElement( Segment seg)
        {
            this.Seg  = seg;
            this.Range = new CharRangeElement();
        }

        public void ClearBounds()
        {
            this.Bounds = Rectangle.Empty;
        }

        public void Render(ExtendedGraphics eg, int xoffset, int yoffset, Rectangle clientRect, List<WordElement> wordElements)
        {
            if (wordElements.Count == 0)
            {
                return;
            }
            this.Bounds = Rectangle.Empty;

            CharRangeElement segrange = new CharRangeElement();
            bool halfStart = false;
            bool halfEnd = false;
            if (this.Range.Start.WordID < 0)
            {   // Segment開始が表示範囲の前
                segrange.Start.Word = wordElements[0];
                segrange.Start.CharInWord = 0;
                halfStart = true;
            }
            else
            {
                segrange.Start = this.Range.Start;
            }
            if (this.Range.End.WordID < 0)
            {   // Segment終了が表示範囲の後
                segrange.End.Word = wordElements[wordElements.Count - 1];
                segrange.End.CharInWord = wordElements[wordElements.Count - 1].Length - 1;
                halfEnd = true;
            }
            else
            {
                segrange.End = this.Range.End;
            }
            int wordIdOffset = wordElements[0].Index;
            WordElement swb = wordElements[segrange.Start.WordID - wordIdOffset];
            WordElement ewb = wordElements[segrange.End.WordID - wordIdOffset];

            if (segrange.Start.CharInWord < 0 || segrange.Start.CharInWord >= swb.CharPos.Count ||
                segrange.End.CharInWord < 0 || segrange.End.CharInWord >= ewb.CharPos.Count)
            {
                return;
            }
            RectangleF screct = swb.CharPos[segrange.Start.CharInWord];
            RectangleF ecrect = ewb.CharPos[segrange.End.CharInWord];
            Point p1 = new Point((int)(swb.Bounds.Left + screct.Left), (int)(swb.Bounds.Top + screct.Top));
            Point p2 = new Point((int)(ewb.Bounds.Left + ecrect.Right), (int)(ewb.Bounds.Top + ecrect.Top));
            p1.Offset(xoffset, yoffset);
            p2.Offset(xoffset, yoffset);
            Pen pen = SegmentPens.Find(this.Seg.Tag.Name);
            // Segmentを描画するとともに、その代表位置をキャッシュに記憶する
            this.Bounds = DrawSegmentRectangle(eg, pen, p1, p2, this.Level, SegElement.LineHeight, halfStart, halfEnd, clientRect);
        }

        /// <summary>
        /// 複数行にわたるSegmentを描画する
        /// </summary>
        /// <param name="eg"></param>
        /// <param name="pen"></param>
        /// <param name="r"></param>
        /// <param name="interval"></param>
        /// <param name="halfStart">開始端=開状態で描画を始める</param>
        /// <param name="halfEnd">終了端=開状態で描画を終わる</param>
        private Rectangle DrawSegmentRectangle(ExtendedGraphics eg, Pen pen, Point p1, Point p2, int level,
            int interval, bool halfStart, bool halfEnd, Rectangle clientRect)
        {
            Rectangle result = Rectangle.Empty;
            int rows = (int)((p2.Y - p1.Y) / interval) + 1; // 何行にわたるか?
            if (rows <= 0)
            {
                return result;
            }
            for (int row = 0; row < rows; row++)
            {
                int l, r, t, b;
                if (row < rows - 1 || (row == rows - 1 && halfEnd))
                {   // 最終行でないので、右端はWidthよりも右に置く(Clipされる)
                    r = clientRect.Width + 100;
                }
                else
                {
                    r = p2.X;
                }
                if (row > 0 || (row == 0 && halfStart))
                {   // 開始行でないので、左端は0よりも左に置く(Clipされる)
                    l = -100;
                }
                else
                {
                    l = p1.X;
                }

                t = p1.Y + interval * row - 2;
                b = t + SegElement.L1Height + 4;
                Rectangle rct = new Rectangle(l, t, r - l, b - t);
                rct.Inflate(0, level * 6);
                eg.DrawRoundRectangle(pen, rct, 3);

                if (row == 0)
                {
                    result = rct;
                }
            }
            return result;
        }

    }
}
