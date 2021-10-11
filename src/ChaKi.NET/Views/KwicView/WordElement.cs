using System.Collections.Generic;
using System.Drawing;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using System.Diagnostics;
using System;
using System.Windows.Forms;
using ChaKi.Options;
using ChaKi.Entity.Settings;
using ChaKi.GUICommon;
using ChaKi.Common;
using ChaKi.Common.Settings;

namespace ChaKi.Views.KwicView
{
    internal class WordElement
    {
        public LineElement Parent;
        public int StartChar;      // Corpus内の文字位置
        public int EndChar;        // Corpus内の文字位置
        public Rectangle Bounds;
        public Point L1Location;    // rに対する1行目（表層）の描画開始位置(LeftTop)
        public Point L2Location;    // rに対する2行目（POS）の描画開始位置(LeftTop)
        public KwicWord KwicWord;
        public bool IsCenter;
        public KwicItem KwicItem;   // source of this object
        public int Index;   // index of word in sentence
        public List<RectangleF> CharPos;
        public int LinePos;         // LineElement内の何行目に属するか（WordWrapモードの場合以外は0）
        public int WordMappingHilight;   // word_word mappingのハイライトモード(0:none, 1:from, 2:to)

        public static int LastIndex { get; set; }
        public static int LineHeight { get; set; }
        public static bool RenderTwoLine { get; set; }

        private static Brush NormalBrush { get; set; }
        private static Brush HilightBrush { get; set; }
        private static Font L1FontNormal { get; set; }
        private static Font L2FontNormal { get; set; }
        private static Font L1FontBold { get; set; }
        private static Font L2FontBold { get; set; }
        private static Brush SelectionBrush;

        private static Brush CircleFillBrush { get; set; }


        static WordElement()
        {
            sformat = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            NormalBrush = Brushes.Black;
            HilightBrush = Brushes.Red;
            SelectionBrush = new SolidBrush(Color.FromArgb(50, Color.Blue));
            CircleFillBrush = new SolidBrush(Color.FromArgb(200, Color.Pink));
        }

        public WordElement(LineElement parent)
        {
            Parent = parent;
            StartChar = 0;
            EndChar = 0;
            CharPos = null;
            IsCenter = false;
            LinePos = 0;
            WordMappingHilight = 0;
            this.Index = LastIndex++;
        }

        public static void FurnishFonts(Font l1Font, Font l2Font)
        {
            if (L1FontBold != null) L1FontBold.Dispose();
            if (L2FontBold != null) L2FontBold.Dispose();

            L1FontNormal = l1Font;
            L2FontNormal = l2Font;
            L1FontBold = new Font(l1Font, FontStyle.Bold);
            L2FontBold = new Font(l2Font, FontStyle.Bold);
        }

        public void MeasureStrings(Graphics g, out SizeF sz1, out SizeF sz2)
        {
            sz1 = g.MeasureString(this.L1String, L1FontNormal);
            sz2 = g.MeasureString(this.L2String, L2FontNormal);
        }

        public void Render(Graphics g, int xoffset, int yoffset, CharRangeElement sel, ref bool continueSelectionFlag)
        {
            if (this.L1String == null)
            {
                return;
            }
            xoffset += this.Bounds.X;
            yoffset += this.Bounds.Y;
            Font l1Font = L1FontNormal;
            Font l2Font = L2FontNormal;
            Brush brush = NormalBrush;
            if ((this.KwicWord.ExtAttr & KwicWord.KWA_HILIGHT) != 0)
            {
                l1Font = L1FontBold;
                l2Font = L2FontBold;
            }
            if ((this.KwicWord.ExtAttr & KwicWord.KWA_PIVOT) != 0)
            {
                l1Font = L1FontBold;
                l2Font = L2FontBold;
                brush = HilightBrush;
            }
            // Apply WordColorSetting
            Corpus crps = this.KwicItem.Crps;
            WordColorSetting wcs = WordColorSettings.GetInstance().Match(crps.Name, this.KwicWord.Lex);
            if (wcs != null)
            {
                brush = BrushCache.Instance.Get(wcs.FgColor);
                Brush bgbrush = BrushCache.Instance.Get(wcs.BgColor);
                int h = RenderTwoLine ? (this.L2Location.Y - this.L1Location.Y + l2Font.Height) : (l1Font.Height);
                g.FillRectangle(bgbrush, xoffset, this.L1Location.Y + yoffset, this.Bounds.Width, h);
            }

            // word-word link Hilighting
            if (this.WordMappingHilight == 1)
            {
                Brush bgbrush = BrushCache.Instance.Get(0x400000FF);
                int h = RenderTwoLine ? (this.L2Location.Y - this.L1Location.Y + l2Font.Height) : (l1Font.Height);
                g.FillRectangle(bgbrush, xoffset, this.L1Location.Y + yoffset, this.Bounds.Width, h);
                this.WordMappingHilight = 0;
            }
            else if (this.WordMappingHilight == 2)
            {
                Brush bgbrush = BrushCache.Instance.Get(0x40FF0000);
                int h = RenderTwoLine ? (this.L2Location.Y - this.L1Location.Y + l2Font.Height) : (l1Font.Height);
                g.FillRectangle(bgbrush, xoffset, this.L1Location.Y + yoffset, this.Bounds.Width, h);
                this.WordMappingHilight = 0;
            }

            // --> Test
            if (PropertyBoxSettings.Instance.KwicRow2Property == LWP.Duration && this.KwicWord.Word != null)
            {
                double duration = this.KwicWord.Word.Duration.GetValueOrDefault(0.0);
                float d = (float)Math.Sqrt(duration) * 40.0f / 100.0f;
                g.FillEllipse(CircleFillBrush, xoffset + 10.0f, yoffset + (this.Bounds.Height - d) / 2.0f, d, d);
            }
            // <--

            g.DrawString(this.L1String, l1Font, brush, this.L1Location.X + xoffset, this.L1Location.Y + yoffset);
            if (RenderTwoLine)
            {
                g.DrawString(this.L2String, l2Font, brush, this.L2Location.X + xoffset, this.L2Location.Y + yoffset);
            }

            if (sel.Valid && this.Length > 0)
            {
                #region Selection描画
                if (continueSelectionFlag)
                {
                    if (sel.End.WordID > this.Index)
                    {
                        // fullly select this WordElement
                        Rectangle rs = new Rectangle(xoffset, yoffset, this.Bounds.Width, WordElement.L1FontNormal.Height);
                        g.FillRectangle(SelectionBrush, rs);
                    }
                    else if (sel.End.WordID == this.Index)
                    {
                        // selection ends in this WordElement
                        Rectangle rs = new Rectangle(xoffset, yoffset, this.IndexToOffset(sel.End.CharInWord), WordElement.L1FontNormal.Height);
                        g.FillRectangle(SelectionBrush, rs);
                        continueSelectionFlag = false;
                    }
                }
                else
                {
                    if (sel.Start.WordID == this.Index)
                    {
                        if (sel.End.WordID == this.Index)
                        {
                            // selection starts and ends in this WordElement
                            Rectangle rs = GetRangeRectangle(sel);
                            rs.Height = WordElement.L1FontNormal.Height;
                            rs.Offset(xoffset, yoffset);
                            g.FillRectangle(SelectionBrush, rs);
                        }
                        else
                        {
                            // selection starts in this WordElement and continues
                            int x = IndexToOffset(sel.Start.CharInWord);
                            Rectangle rs = new Rectangle(x + xoffset, yoffset, this.Bounds.Width - x, WordElement.L1FontNormal.Height);
                            g.FillRectangle(SelectionBrush, rs);
                            continueSelectionFlag = true;
                        }
                    }
                }
                #endregion
            }
        }

        public CharPosElement HitTestChar(Point p, out Point at)
        {
            for (int i = 0; i < this.CharPos.Count; i++)
            {
                RectangleF rect = this.CharPos[i];
                if (rect.Contains(p))
                {
                    if (p.X < rect.Width / 2)
                    {
                        // ヒットした文字の左
                        at = new Point((int)rect.Left, (int)rect.Top);
                        return new CharPosElement(this, i);
                    }
                    // ヒットした文字の右
                    if (i + 1 >= this.Length)
                    {
                        at = new Point((int)rect.Right, (int)rect.Top);
                        int widx = this.Parent.WordElements.IndexOf(this);
                        if (widx < this.Parent.WordElements.Count - 1)
                        {
                            return new CharPosElement(this.Parent.WordElements[widx + 1], 0);
                        }
                    }
                    at = new Point((int)rect.Left, (int)rect.Top);
                    return new CharPosElement(this, i + 1);
                }
            }
            at = Point.Empty;
            return new CharPosElement();
        }


        public void Invalidate()
        {
            CharPos = null;
        }

        public int Length
        {
            get
            {
                return this.L1String.Length;
            }
        }

        public string L1String
        {
            get
            {
                if (this.KwicWord.Lex == null)
                {
                    return this.KwicWord.Text;
                }
                return GetKwicWordProperty(PropertyBoxSettings.Instance.KwicRow1Property);
            }
        }

        public string L2String
        {
            get
            {
                if (this.KwicWord.Lex == null)
                {
                    return this.KwicWord.Text;
                }
                return GetKwicWordProperty(PropertyBoxSettings.Instance.KwicRow2Property);
            }
        }

        private string GetKwicWordProperty(LWP lwp)
        {
            switch (lwp)
            {
                case LWP.Surface:
                    return this.KwicWord.Lex.Surface;
                case LWP.Reading:
                    return this.KwicWord.Lex.Reading;
                case LWP.LemmaForm:
                    return this.KwicWord.Lex.LemmaForm;
                case LWP.Pronunciation:
                    return this.KwicWord.Lex.Pronunciation;
                case LWP.BaseLexeme:
                    return (this.KwicWord.Lex.BaseLexeme != null) ? this.KwicWord.Lex.BaseLexeme.Surface : string.Empty;
                case LWP.Lemma:
                    return this.KwicWord.Lex.Lemma;
                case LWP.PartOfSpeech:
                    if (GUISetting.Instance.UseShortPOS)
                    {
                        return this.KwicWord.Lex.PartOfSpeech.Name1;
                    }
                    return this.KwicWord.Lex.PartOfSpeech.Name;
                case LWP.CType:
                    return this.KwicWord.Lex.CType.Name;
                case LWP.CForm:
                    return this.KwicWord.Lex.CForm.Name;
                // Word Properties
                case LWP.StartTime:
                    return (this.KwicWord.Word == null || this.KwicWord.Word.StartTime == null) ?
                        "?" : string.Format("{0:F3}/", this.KwicWord.Word.StartTime);
                case LWP.EndTime:
                    return (this.KwicWord.Word == null || this.KwicWord.Word.EndTime == null) ?
                        "?" : string.Format("{0:F3}/", this.KwicWord.Word.EndTime);
                case LWP.Duration:
                    return (this.KwicWord.Word == null || this.KwicWord.Word.Duration == null) ?
                        "?" : string.Format("{0:F3}/", this.KwicWord.Word.Duration);
                default:
                    return this.KwicWord.Lex.Surface;
            }
        }

        public void CalculateCharPos(Graphics g)
        {
            if (this.CharPos != null)
            {
                return;
            }
            this.CharPos = new List<RectangleF>();
            string s = this.L1String;
            float x = 0F;
            for (int i = 0; i < s.Length; i++)
            {
                Size sz = TextRenderer.MeasureText(
                    s.Substring(i, 1),
                    GUISetting.Instance.GetBaseTextFont(),
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.Default);
                this.CharPos.Add(new RectangleF(x, 0F, (float)sz.Width, (float)sz.Height));
                x += (float)sz.Width;
            }
        }

        static private Dictionary<int, CharacterRange[]> characterRangesMap = new Dictionary<int, CharacterRange[]>();
        private static StringFormat sformat;

        public void CalculateCharPos2(Graphics g)
        {
            if (this.CharPos != null || this.L1String == null)
            {
                return;
            }
            this.CharPos = new List<RectangleF>();
            string s = this.L1String;
            Rectangle rect = this.Bounds;
            rect.Offset(-Bounds.Left, -Bounds.Top);
            rect.Width += 100;   // 文字が途切れないように
            CharacterRange[] characterRanges;
            if (!characterRangesMap.TryGetValue(s.Length, out characterRanges))
            {
                characterRanges = new CharacterRange[s.Length];
                characterRangesMap[s.Length] = characterRanges;
                for (int i = 0; i < s.Length; i++)
                {
                    characterRanges[i] = new CharacterRange(i, 1);
                }
            }
            try
            {
                sformat.SetMeasurableCharacterRanges(characterRanges);
                Region[] rgns = g.MeasureCharacterRanges(s, GUISetting.Instance.GetBaseTextFont(), rect, sformat);
                for (int i = 0; i < rgns.Length; i++)
                {
                    RectangleF r = rgns[i].GetBounds(g);
                    r.Height = WordElement.LineHeight;
                    r.Offset(this.L1Location);
                    this.CharPos.Add(r);
                    rgns[i].Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                for (int i = 0; i < s.Length; i++)
                {
                    this.CharPos.Add(new RectangleF());
                }
            }
        }

        public int IndexToOffset(int index)
        {
            if (index >= this.CharPos.Count)
            {
                return (int)this.CharPos[this.CharPos.Count - 1].Right;
            }
            if (index < 0)
            {
                return (int)this.CharPos[0].Left;
            }
            return (int)this.CharPos[index].Left;
        }

        /// <summary>
        /// このWordElementに対する相対X位置から、該当する文字のインデックスを得る.
        /// Xが左端よりも左ならば-1, Xが右端よりも右ならば(CharPos.Count)を返す.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int OffsetToIndex(int x)
        {
            float fx = (float)x;
            int idx = 0;
            for (idx = 0; idx < this.CharPos.Count; idx++)
            {
                RectangleF r = this.CharPos[idx];
                if (idx == 0 && fx < r.Left)
                {
                    return -1;
                }
                if (fx <= r.Right)
                {
                    break;
                }
            }
            return idx;
        }

        public Rectangle GetRangeRectangle(CharRangeElement range)
        {
            int index1 = range.Start.CharInWord;
            int index2 = range.End.CharInWord;
            if (index1 < 0 || index1 >= this.CharPos.Count)
            {
                return Rectangle.Empty;
            }

            if (index2 < 0 || index2 >= this.CharPos.Count)
            {
                return new Rectangle(
                    (int)this.CharPos[index1].Left,
                    0,
                    (int)this.CharPos[index2 - 1].Right - (int)this.CharPos[index1].Left,
                    20);
            }
            return new Rectangle(
                (int)this.CharPos[index1].Left,
                0,
                (int)this.CharPos[index2].Left - (int)this.CharPos[index1].Left,
                20);
        }
    }

}
