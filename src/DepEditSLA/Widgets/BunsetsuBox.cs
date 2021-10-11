using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ChaKi.Common;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using System.Text;
using ChaKi.Common.Settings;

namespace DependencyEditSLA.Widgets
{
    public partial class BunsetsuBox : UserControl
    {
        private Segment Segment;
        public Group Group { get; set; }
        private List<WordBox> m_WordBoxes;
        private List<Gap> m_Gaps;
        private List<ConcatButton> m_Buttons;

        private bool m_bHover;

        private static Pen m_BorderPen;
        private static Font ms_Font;

        public event MergeSplitEventHandler SplitBunsetsuRequested;  // this --> SentenceStructure
        public event MergeSplitEventHandler SplitWordRequested;      // WordBox --> this --> SentenceStructure
        public event MergeSplitEventHandler MergeWordRequested;      // this --> SentenceStructure
        public event EventHandler LexemeEditRequested;               // WordBox --> this --> SentenceStructure
        public event Action<Lexeme> UpdateGuidePanel;


        [Flags]
        public enum AbridgeTypes
        {
            Left = 0x01,
            Right = 0x02,
        }
        public AbridgeTypes AbridgeType { get; set; }
        public bool IsLeftAbridged { get { return (this.AbridgeType & AbridgeTypes.Left) != 0; } }
        public bool IsRightAbridged { get { return (this.AbridgeType & AbridgeTypes.Right) != 0; } }
        public bool IsAllAbridged { get { return (this.AbridgeType == (AbridgeTypes.Left | AbridgeTypes.Right)); } }
        public bool IsAbridged { get { return (this.AbridgeType != 0); } }

        static BunsetsuBox()
        {
            m_BorderPen = new Pen(Color.Red, 2F);
        }

        public static int DefaultHeight
        {
            get { return WordBox.DefaultHeight + 4; }
        }

        public BunsetsuBox(Segment b)
        {
            InitializeComponent();

            Segment = b;
            m_WordBoxes = new List<WordBox>();
            m_Gaps = new List<Gap>();
            m_Buttons = new List<ConcatButton>();
            m_bHover = false;
            this.AbridgeType = 0;
        }

        public Segment Model
        {
            get { return Segment; }
        }

        public bool Hover
        {
            set { this.m_bHover = value; }
        }

        /// <summary>
        /// WordBoxを追加生成する
        /// </summary>
        /// <param name="w"></param>
        /// <param name="bCenter">語を強調表示するならtrue. (Center Word)</param>
        public WordBox AddWordBox(Word w, bool bCenter)
        {
            WordBox wb = new WordBox(w, bCenter);
            m_WordBoxes.Add(wb);
            this.Controls.Add(wb);
            // 一番左でなければ、ギャップ領域とWord接続ボタンも生成する
            if (m_WordBoxes.Count > 1)
            {
                ConcatButton btn = new ConcatButton(m_WordBoxes.Count - 1);
                m_Buttons.Add(btn);
                this.Controls.Add(btn);
                btn.Click += new EventHandler(HandleWordMergeButtonClick);

                Gap gb = new Gap(w.StartChar);
                m_Gaps.Add(gb);
                this.Controls.Add(gb);
                gb.Click += new System.EventHandler(HandleWordGapClick);
            }
            wb.SplitWordRequested += new MergeSplitEventHandler(HandleSplitWordRequested);
            wb.UpdateGuidePanel += HandleUpdateGuidePanel;
            wb.Click += delegate (object o, EventArgs e) { if (this.LexemeEditRequested != null) LexemeEditRequested(o, e); };
            return wb;
        }

        internal void SetAbridged(IList<AbridgedRange> ranges)
        {
            int spos = this.Model.StartChar;
            this.AbridgeType = 0;
            int nAbridgedWords = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_WordBoxes.Count; i++)
            {
                WordBox wb = m_WordBoxes[i];
                int epos = spos + wb.Length;
                wb.Abridged = false;
                foreach (AbridgedRange range in ranges)
                {
                    if (range.Intersects(spos, epos))
                    {
                        wb.Abridged = true;
                        if (i == 0)
                        {
                            this.AbridgeType |= AbridgeTypes.Left;
                        }
                        if (i == m_WordBoxes.Count - 1)
                        {
                            this.AbridgeType |= AbridgeTypes.Right;
                        }
                        sb.Append(wb.Model.Lex.Surface);
                    }
                }
                if (wb.Abridged)
                {
                    nAbridgedWords++;
                }
                spos = epos;
            }
            if (nAbridgedWords == m_WordBoxes.Count)
            {
                this.AbridgeType = AbridgeTypes.Left|AbridgeTypes.Right;
                this.BorderStyle = BorderStyle.None;
            }
            else
            {
                this.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private static Font GetFont()
        {
            float size = DepEditSettings.Current.FontSize;
            Font face = FontDictionary.Current["BaseText"];
            ms_Font = new Font(face.Name, size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            return ms_Font;
        }

        public void RecalcLayout(DispModes mode)
        {
            if (m_WordBoxes.Count == 0)
            {
                this.Width = this.Height = BunsetsuBox.DefaultHeight;
                return;
            }
            if (IsAllAbridged)  // 文節全体が省略表示の場合はChild Controlはすべて非表示. "..."を表示するサイズのみ確保する.
            {
                Graphics g = this.CreateGraphics();
                SizeF sz = g.MeasureString("▽", GetFont());
                this.Width = (int)sz.Width;
                this.Height = BunsetsuBox.DefaultHeight;
                m_WordBoxes.ForEach(new Action<WordBox>(delegate(WordBox b) { b.Visible = false; }));
                m_Buttons.ForEach(new Action<ConcatButton>(delegate(ConcatButton b) { b.Visible = false; }));
                m_Gaps.ForEach(new Action<Gap>(delegate(Gap b) { b.Visible = false; }));
                return;
            }

            int x = 0;
            if (!IsLeftAbridged)
            {
                x += 4;
            }
            int y = 4;
            int height = 0;
            int margin = DepEditSettings.Current.WordBoxMargin;

            Trace.Assert(m_WordBoxes.Count == m_Gaps.Count + 1);

            int n = m_WordBoxes.Count;
            for (int i = 0; i < n; i++)
            {
                if (i < n - 1)
                {
                    m_Buttons[i].Visible = false;
                }
                WordBox wb = m_WordBoxes[i];
                wb.Visible = true;
                wb.RecalcLayout(mode);
                int w = wb.Width;
                wb.Location = new Point(x, y);

                x += w;
                // 連続するabridged wordについて、先頭のwordと同じ場所を占有させる（BunsetsuBoxと同様）
                if (i < n - 1 && wb.Abridged && m_WordBoxes[i + 1].Abridged)
                {
                    x -= w;
                }
                if (!wb.Abridged && i < n - 1)
                {
                    m_Gaps[i].Height = wb.Height;
                    m_Gaps[i].Location = new Point(x, y);

                    if (mode == DispModes.Morphemes)
                    {
                        m_Buttons[i].Visible = true;
                        m_Buttons[i].Location = new Point(x+2, y);
                        m_Gaps[i].Width = m_Buttons[i].Width + margin;
                    }
                    else
                    {
                        m_Gaps[i].Width = margin;
                    }
                    x += m_Gaps[i].Width;
                }
                height = Math.Max(height, wb.Height);
            }
            if (!IsRightAbridged)
            {
                x += 5;
            }
            this.Width = x;
            this.Height = (int)(height + 8F);
        }

        /// <summary>
        /// このBunsetsuBoxの文字位置範囲（文頭からのオフセットを引いたもの）を返す.
        /// </summary>
        /// <returns></returns>
        public CharRange GetCharRange()
        {
            Sentence sen = Segment.Sentence;
            int offset = sen.StartChar;

            return new CharRange(Segment.StartChar - offset, Segment.EndChar - offset );
        }

        /// <summary>
        /// このBunsetsuBoxの文字位置範囲（文頭からのオフセットを引いたもの）が
        /// rangeと重なるか判定する。
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool IntersectsWith(CharRange range)
        {
            Sentence sen = Segment.Sentence;
            int offset = sen.StartChar;

            CharRange r = new CharRange(Segment.StartChar - offset, Segment.EndChar - offset);
            return range.IntersectsWith(r);
        }

        /// <summary>
        /// 与えられたCharRangeにかかるWordBox群をカバーする矩形を返す.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Rectangle GetCoveredRectangle(CharRange range)
        {
            Sentence sen = Segment.Sentence;
            int offset = sen.StartChar;

            Rectangle rect = Rectangle.Empty;
            foreach (WordBox wb in m_WordBoxes)
            {
                if (range.IntersectsWith(wb.GetCharRange()))
                {
                    if (rect.IsEmpty)
                    {
                        rect = wb.Bounds;
                    }
                    else
                    {
                        Rectangle r = wb.Bounds;
                        rect = new Rectangle(rect.X, rect.Y, (r.Right - rect.X), (r.Bottom - rect.Y));
                    }
                }
            }
            return rect;
        }

        public List<WordBox> HitTestWordBox(Rectangle rect)
        {
            Rectangle r = RectangleToClient(rect);
            List<WordBox> res = new List<WordBox>();
            foreach (WordBox wb in m_WordBoxes)
            {
                if (r.IntersectsWith(wb.Bounds))
                {
                    res.Add(wb);
                }
            }
            return res;
        }


        /// <summary>
        /// いずれかのWordGap（子コントロール）がクリックされたときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleWordGapClick(object sender, EventArgs e)
        {
            if (!(sender is Gap))
            {
                return;
            }
            int splitChar = ((Gap)sender).GapPos;

            if (SplitBunsetsuRequested != null)
            {
                SplitBunsetsuRequested(this, new MergeSplitEventArgs(Segment.Doc.ID, Segment.StartChar, Segment.EndChar, splitChar));
            }
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandlePaint(object sender, PaintEventArgs e)
        {
            if (!this.Visible) return;
            if (this.IsAllAbridged)
            {
                Graphics g = e.Graphics;
                g.FillRectangle(Brushes.Linen, new Rectangle(0, 0, this.Width, this.Height));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.DrawString("▽", GetFont(), Brushes.Black, new PointF(0F, 2F));
            }
            else
            {
                if (m_bHover)
                {
                    //Trace.WriteLine("Hover Rect=" + e.ClipRectangle);
                    e.Graphics.DrawRectangle(m_BorderPen, new Rectangle(0, 0, this.Width - 2, this.Height - 2));
                }
            }
        }

        void HandleUpdateGuidePanel(Lexeme lex)
        {
            if (this.UpdateGuidePanel != null)
            {
                UpdateGuidePanel(lex);
            }
        }

        /// <summary>
        /// Word Mergeボタンが押された時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleWordMergeButtonClick(object sender, EventArgs e)
        {
            if (!(sender is ConcatButton)){
                return;
            }
            ConcatButton b = (ConcatButton)sender;
            int pos = b.Pos;
            WordBox leftbox = m_WordBoxes[pos - 1];
            WordBox rightbox = m_WordBoxes[pos];
            if (MergeWordRequested != null)
            {
                MergeWordRequested(this, new MergeSplitEventArgs(this.Model.Doc.ID, leftbox.Model.StartChar, rightbox.Model.EndChar, rightbox.Model.StartChar));
            }
        }

        // WordSplitイベントの中継
        private void HandleSplitWordRequested(object sender, MergeSplitEventArgs args)
        {
            if (SplitWordRequested != null)
            {
                SplitWordRequested(sender, args);
            }
        }
    }
}
