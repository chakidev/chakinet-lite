using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Extended;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Kwic;
using ChaKi.Properties;
using ChaKi.Common;
using ChaKi.Common.Settings;
using System.Text;
using ChaKi.Common.Widgets;

namespace ChaKi.Views.KwicView
{
    internal class LineElement
    {
        public int Index { get; set; }
        public Rectangle Bounds { get; set; }
        public List<WordElement> WordElements { get; set; }
        public List<SegElement> SegElements { get; set; }
        public List<LinkElement> LinkElements { get; set; }
        public KwicItem KwicItem { get; set; }
        public int CenterIndexOffset { get; set; }  // 中心語が this.WordElements配列の何番目から始まるか
        public int LinePosMax { get; set; }  // WordElementが折り返す場合の最大の返し行番号（折り返しなしなら0）

        public static int LastIndex { get; set; }
        public static int LineHeight { get; set; }
        public static int HeaderHeight { get; set; }
        public static Columns Cols { get; set; }
        public static Font AnsiFont { get; set; }
        private static StringFormat m_StringFormatNum;
        private static StringFormat m_StringFormatText;
        private static Brush m_SelBrush;
        private static Bitmap m_BmpChecked;
        private static Bitmap m_BmpUnchecked;
        private static DpiAdjuster m_DpiAdjuster;
        private static Size CheckBoxSize = new Size(13, 13);

        static LineElement()
        {
            m_StringFormatNum = new StringFormat()
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter
            };
            m_StringFormatText = new StringFormat()
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter
            };
            m_BmpChecked = Resources.Checked;
            m_BmpUnchecked = Resources.Unchecked;
            m_SelBrush = new SolidBrush(Color.FromArgb(64, 103, 120, 167));

            m_DpiAdjuster = new DpiAdjuster((xscale, yscale) => {
                CheckBoxSize = new Size((int)(CheckBoxSize.Width * xscale), (int)(CheckBoxSize.Height * yscale));
            });
        }

        public LineElement()
        {
            this.WordElements = new List<WordElement>();
            this.SegElements = new List<SegElement>();
            this.LinkElements = new List<LinkElement>();
            this.Index = LastIndex++;
            LinePosMax = 0;

        }

        public void AddKwicPortion(KwicPortion kp, Graphics g, KwicPortionType kptype, bool twoLineMode, ref int charPos)
        {
            foreach (KwicWord kw in kp.Words)
            {
                WordElement wb = new WordElement(this);
                wb.KwicWord = kw;
                wb.StartChar = charPos;
                int wlen = kw.Length;
                wb.EndChar = charPos + wlen;
                charPos += wlen;
                wb.IsCenter = (kptype == KwicPortionType.Center);//(kw.ExtAttr & KwicWord.KWA_PIVOT) != 0);
                wb.KwicItem = kp.Parent;
                wb.Bounds = new Rectangle();
                SizeF sz1;
                SizeF sz2;
                wb.MeasureStrings(g, out sz1, out sz2);
                if (twoLineMode)
                {
                    wb.Bounds.Width = (int)Math.Max(sz1.Width, sz2.Width);
                }
                else
                {
                    wb.Bounds.Width = (int)sz1.Width;
                }
                wb.Bounds.Height = LineHeight;
                wb.L1Location = new Point((int)((wb.Bounds.Width - sz1.Width) / 2), 0);
                wb.L2Location = new Point((int)((wb.Bounds.Width - sz2.Width) / 2), (int)sz1.Height);
                wb.CalculateCharPos2(g);

                this.WordElements.Add(wb);
            }
        }

        public void RecalcWordElementBounds(Graphics g, bool twoLineMode)
        {
            foreach (var we in this.WordElements)
            {
                SizeF sz1;
                SizeF sz2;
                we.MeasureStrings(g, out sz1, out sz2);
                if (twoLineMode)
                {
                    we.Bounds.Width = (int)Math.Max(sz1.Width, sz2.Width);
                }
                else
                {
                    we.Bounds.Width = (int)sz1.Width;
                }
                we.L1Location = new Point((int)((we.Bounds.Width - sz1.Width) / 2), 0);
                we.L2Location = new Point((int)((we.Bounds.Width - sz2.Width) / 2), (int)sz1.Height);
            }
        }


        public bool ContainsRange(int start, int end)
        {
            if ((start >= this.KwicItem.StartCharPos && start < this.KwicItem.EndCharPos)
             || (end > this.KwicItem.StartCharPos && end <= this.KwicItem.EndCharPos))
            {
                return true;
            }
            return false;
        }

        public SegElement AddSegment(Segment seg)
        {
            // SegmentをこのLineElementの何語目の何文字目という区切りに変換する
            SegElement se = new SegElement(seg);
            se.Range.Start = new CharPosElement();
            se.Range.End = new CharPosElement();
            for (int i = 0; i < this.WordElements.Count; i++)
            {
                WordElement wb = this.WordElements[i];
                if (seg.StartChar >= wb.StartChar && seg.StartChar < wb.EndChar)
                {
                    se.Range.Start = new CharPosElement(this.WordElements[i], seg.StartChar - wb.StartChar);
                }
                if (seg.EndChar > wb.StartChar && seg.EndChar <= wb.EndChar)
                {
                    se.Range.End = new CharPosElement(this.WordElements[i], seg.EndChar - wb.StartChar - 1);
                }
            }
            Debug.WriteLine(string.Format("AddSegment {0}:{1} - {2}:{3}",
                se.Range.Start.WordID, se.Range.Start.CharInWord,
                se.Range.End.WordID, se.Range.End.CharInWord));
            if (se.Range.Start.WordID == -1 && se.Range.End.WordID == -1)
            {
                return null;
            }
            this.SegElements.Add(se);
            return se;
        }

        public void AddLinkElement(LinkElement lke)
        {
            this.LinkElements.Add(lke);
        }

        public void Render(ExtendedGraphics eg, int xoffset, int yoffset, Selection lineSel, CharRangeElement charSel,
            Rectangle clientRect, ref bool continueSelectionFlag, KwicViewSettings settings)
        {
            if (this.WordElements.Count == 0)
            {
                return;
            }
            Graphics g = eg.Graphics;

            m_DpiAdjuster.Adjust(g);

            Rectangle rect = this.Bounds;
            rect.Offset(xoffset, -yoffset);
            // 選択行の背景を描画
            if (lineSel.Contains(this.Index))
            {
                g.FillRectangle(m_SelBrush, rect);
            }
            rect.Height = AnsiFont.Height;
            rect.Offset(0, 4);
            int colno = 0;
            // 0. Row Header
            #region Header描画
            Rectangle cliprect = new Rectangle(xoffset, 0, Cols.Widths[colno], clientRect.Height);
            g.SetClip(cliprect);
            string h = string.Format("{0:D}", this.KwicItem.ID);
            rect.X = xoffset;
            rect.Width = Cols.Widths[colno];
            g.DrawString(h, AnsiFont, Brushes.Black, rect, m_StringFormatNum);
            g.ResetClip();
            #endregion

            xoffset += Cols.Widths[colno];
            colno++;
            // 1. Check Button
            rect.X = xoffset;
            rect.Width = Cols.Widths[colno];
            g.DrawImage(
                (this.KwicItem.Checked ? m_BmpChecked : m_BmpUnchecked),
                rect.X + (rect.Width - m_BmpChecked.Size.Width) / 2, rect.Y + (rect.Height - m_BmpChecked.Size.Height) / 2,
                CheckBoxSize.Width, CheckBoxSize.Height);

            xoffset += Cols.Widths[colno];
            colno++;
            // 2. Corpus
            #region CorpusName描画
            cliprect = new Rectangle(xoffset, 0, Cols.Widths[colno], clientRect.Height);
            g.SetClip(cliprect);
            h = string.Format("{0}", this.KwicItem.Crps.Name);
            rect.X = xoffset;
            rect.Width = Cols.Widths[colno];
            g.DrawString(h, AnsiFont, Brushes.Black, rect, m_StringFormatText);
            g.ResetClip();
            #endregion

            xoffset += Cols.Widths[colno];
            colno++;
            // 3. Document
            #region CorpusName描画
            cliprect = new Rectangle(xoffset, 0, Cols.Widths[colno], clientRect.Height);
            g.SetClip(cliprect);
            h = string.Format("{0:D}", this.KwicItem.Document.ID);
            rect.X = xoffset;
            rect.Width = Cols.Widths[colno];
            g.DrawString(h, AnsiFont, Brushes.Black, rect, m_StringFormatText);
            g.ResetClip();
            #endregion

            xoffset += Cols.Widths[colno];
            colno++;
            // 4. CharPos
            #region CharPos描画
            cliprect = new Rectangle(xoffset, 0, Cols.Widths[colno], clientRect.Height);
            g.SetClip(cliprect);
            h = string.Format("{0:D}", this.KwicItem.StartCharPos);
            rect.X = xoffset;
            rect.Width = Cols.Widths[colno];
            g.DrawString(h, AnsiFont, Brushes.Black, rect, m_StringFormatNum);
            g.ResetClip();
            #endregion

            xoffset += Cols.Widths[colno];
            colno++;
            // 5. SentencePos
            #region SentencePos描画
            cliprect = new Rectangle(xoffset, 0, Cols.Widths[colno], clientRect.Height);
            g.SetClip(cliprect);
            h = string.Format("{0:D}", this.KwicItem.SenID);
            rect.X = xoffset;
            rect.Width = Cols.Widths[colno];
            g.DrawString(h, AnsiFont, Brushes.Black, rect, m_StringFormatNum);
            g.ResetClip();
            #endregion

            xoffset += Cols.Widths[colno];
            colno++;

            // 6. WordElements
            #region WordElements描画
            cliprect = new Rectangle(xoffset, 0, Cols.GetMaxSentenceWidth(), clientRect.Height);
            g.SetClip(cliprect);
            rect.X = xoffset;
            rect.Width = Cols.Widths[colno];
            foreach (WordElement we in this.WordElements)
            {
                we.Render(g, rect.X, rect.Y, charSel, ref continueSelectionFlag);
            }
            #endregion

            #region  SegElement描画
            if (settings.ShowSegments)
            {
                foreach (SegElement se in this.SegElements)
                {
                    se.Render(eg, rect.X, rect.Y, clientRect, this.WordElements);
                }
            }
            #endregion

            g.ResetClip();
        }

        /// <summary>
        /// Segmentの描画位置情報をリセットする.
        /// </summary>
        public void ClearSegmentCache()
        {
            foreach (var se in this.SegElements)
            {
                se.ClearBounds();
            }
        }

        public void RenderLinks(ExtendedGraphics eg, KwicViewSettings settings)
        {
            //Link描画は、Seg描画時にSegElementに格納されたSeg描画位置を基にしている。
            // （表示されないSegElement.Boundは、SegElement.ResetCache()によりEmptyに設定される。）
            // そのため、全SegElementの描画後にこのメソッドを呼んでLink描画を行う。
            if (settings.ShowLinks)
            {
                foreach (LinkElement lke in this.LinkElements)
                {
                    lke.Render(eg);
                }
            }
        }

        public WordElement HitTestWordElement(Point p)
        {
            p.Offset(-Cols.GetSentenceOffset(), 0);
            if (p.X < 0 || p.X > Cols.GetMaxSentenceWidth())
            {
                return null;
            }

            foreach (WordElement we in this.WordElements)
            {
                if (we.Bounds.Contains(p))
                {
                    return we;
                }
            }
            return null;
        }

        /// <summary>
        /// このLineElementの表示上の行がlinePos行目であって、
        /// xに最も近い位置の文字位置を計算する.
        /// </summary>
        /// <param name="linePos"></param>
        /// <param name="x"></param>
        /// <returns>WordElementがない場合はnullを返す.</returns>
        public CharPosElement GetNearestCharPos(int linePos, int x)
        {
            CharPosElement cpe = new CharPosElement();
            foreach (WordElement we in this.WordElements)
            {
                if (we.LinePos < linePos)
                {
                    continue;
                }
                else if (we.LinePos == linePos)
                {
                    if (x < we.Bounds.Left)
                    {
                        if (!cpe.IsValid)
                        {
                            cpe = new CharPosElement(we, 0);
                        }
                    }
                    else if (x >= we.Bounds.Left && x < we.Bounds.Right)
                    {
                        cpe = new CharPosElement(we, we.OffsetToIndex(x - we.Bounds.Left));
                    }
                    else if (x >= we.Bounds.Right)
                    {
                        cpe = new CharPosElement(we, we.CharPos.Count - 1);
                    }
                }
                else
                {
                    break;
                }
            }
            return cpe;
        }

        /// <summary>
        /// この行のposInLineで指定された文字位置以降で最初に見つかったkeyの位置を返す.
        /// </summary>
        /// <param name="posInLine"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public CharRangeElement FindNext(int posInLine, string key)
        {
            StringBuilder sb = new StringBuilder();
            List<int> posList = new List<int>();
            int offset = 0;
            foreach (WordElement we in this.WordElements)
            {
                string s = we.L1String;
                sb.Append(s);
                offset += s.Length;
                posList.Add(offset);
            }
            string text = sb.ToString();
            if (posInLine < 0 || posInLine >= text.Length)
            {
                return null;
            }
            int pos = text.IndexOf(key, posInLine);
            if (pos < 0) return null;
            int epos = pos + key.Length;

            CharRangeElement ret = new CharRangeElement();
            for (int i = 0; i < posList.Count; i++)
            {
                if (!ret.Start.IsValid && posList[i] > pos)
                {
                    if (i > 0)
                    {
                        ret.Start = new CharPosElement(this.WordElements[i], pos - posList[i - 1]);
                    }
                    else
                    {
                        ret.Start = new CharPosElement(this.WordElements[0], pos);
                    }
                }
                if (!ret.End.IsValid && posList[i] >= epos)
                {
                    if (i > 0)
                    {
                        ret.End = new CharPosElement(this.WordElements[i], epos - posList[i - 1]);
                    }
                    else
                    {
                        ret.End = new CharPosElement(this.WordElements[0], epos);
                    }
                    break;
                }
            }
            if (ret.Valid)
            {
                return ret;
            }
            return null;
        }
    }
}
