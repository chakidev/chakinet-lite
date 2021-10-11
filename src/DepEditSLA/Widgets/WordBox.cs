using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Service.DependencyEdit;
using ChaKi.Common;
using ChaKi.Common.Settings;

namespace DependencyEditSLA.Widgets
{
    public partial class WordBox : UserControl
    {
        public event Action<Lexeme> UpdateGuidePanel;

        private static Font ms_Font;
        private static Brush ms_Brush;
        private static Brush ms_Brush2;
        private static Pen m_BorderPen;

        private bool m_bCenter;

        private Word m_Model;

        private DispModes m_CurDispMode;
        private List<CharBox> m_CharBoxes;
        private List<Gap> m_CharGaps;

        private bool m_Hovering;

        public event MergeSplitEventHandler SplitWordRequested;

        /// <summary>
        /// この語Boxが縮退表示対象ならtrue
        /// </summary>
        public bool Abridged {
            get
            {
                return m_Abridged;
            }
            set
            {
                m_Abridged = value;
                this.BorderStyle = (m_Abridged) ? BorderStyle.None : BorderStyle.Fixed3D;
            }
        }
        private bool m_Abridged;

        public int Length
        {
            get { return (m_Model != null) ? m_Model.CharLength : 0; }
        }

        static WordBox() 
        {
            ms_Brush = new SolidBrush(Color.Black);
            ms_Brush2 = new SolidBrush(Color.Red);
            m_BorderPen = new Pen(Color.Red, 2F);
        }

        public WordBox(Word w, bool bCenter)
        {
            m_CharBoxes = new List<CharBox>();
            m_CharGaps = new List<Gap>();

            m_CurDispMode = DispModes.None;

            InitializeComponent();

            m_Model = w;
            m_bCenter = bCenter;
            m_Hovering = false;
            Abridged = false;

            UpdateContents();

            this.lexemeBox1.Model = w.Lex;
            this.lexemeBox1.Click += new EventHandler(lexemeBox1_Click);
        }

        void lexemeBox1_Click(object sender, EventArgs e)
        {
            OnClick(e);
        }

        private void UpdateContents()
        {
            m_CharBoxes.Clear();
            m_CharGaps.Clear();

            this.SuspendLayout();
            this.Controls.Clear();

            if (!this.Abridged && m_Model != null && m_Model.Lex != null)
            {
                foreach (char ch in m_Model.Lex.Surface)
                {
                    CharBox cb = new CharBox(ch);
                    this.Controls.Add(cb);
                    if (m_CharBoxes.Count > 0)
                    {
                        Gap g = new Gap(m_CharBoxes.Count - 1);
                        this.Controls.Add(g);
                        m_CharGaps.Add(g);
                        g.Click += new EventHandler(HandleGapClick);
                    }
                    m_CharBoxes.Add(cb);
                }
                this.Controls.Add(this.lexemeBox1);
            }
            this.ResumeLayout();
        }

        public static float FontSize
        {
            get
            {
                return DepEditSettings.Current.FontSize;
            }
            set
            {
                if (ms_Font != null)
                {
                    ms_Font.Dispose();
                    DepEditSettings.Current.FontSize = value;
                    CreateFont();
                }
            }
        }

        private static void CreateFont()
        {
            float size = DepEditSettings.Current.FontSize;
            Font face = FontDictionary.Current["BaseText"];
            ms_Font = new Font(face.Name, size, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        }

        public static int DefaultHeight
        {
            get
            {
                return (int)FontSize + 8;
            }
        }

        private void SetMode(DispModes mode)
        {
            if (m_CurDispMode == mode)
            {
                return;
            }

            if (mode == DispModes.Morphemes && !this.Abridged)
            {
                int cbHeight = 0;
                foreach (CharBox cb in m_CharBoxes)
                {
                    cb.Visible = true;
                    cbHeight = cb.Height;
                }
                foreach (Gap g in m_CharGaps)
                {
                    g.Visible = true;
                    if (cbHeight > 0)
                    {
                        g.Height = cbHeight;
                    }
                    g.Width = DepEditSettings.Current.WordBoxMargin;
                }
                this.lexemeBox1.Visible = true;
            }
            else
            {
                foreach (CharBox cb in m_CharBoxes) cb.Visible = false;
                foreach (Gap g in m_CharGaps) g.Visible = false;
                this.lexemeBox1.Visible = false;
            }
            m_CurDispMode = mode;
        }

        public void RecalcLayout(DispModes mode)
        {
            if (ms_Font == null)
            {
                CreateFont();
            }

            // LexemeBoxの表示切替
            SetMode(mode);

            if (this.Abridged)
            {
                Graphics g = this.CreateGraphics();
                SizeF sz = g.MeasureString("△", ms_Font);
                this.Width = (int)sz.Width;
                this.Height = (int)sz.Height;
            }
            else if (mode == DispModes.Morphemes)
            {
                int x = 3;
                int h = 3;
                var charboxMargin = DepEditSettings.Current.WordBoxMargin;
                for (int i = 0; i < m_CharBoxes.Count; i++)
                {
                    if (i > 0)
                    {
                        m_CharGaps[i - 1].Location = new Point(x, 3);
                        m_CharGaps[i - 1].Width = charboxMargin;
                        x += charboxMargin;
                    }
                    m_CharBoxes[i].Location = new Point(x, 3);
                    x += m_CharBoxes[i].Width;
                    h = Math.Max(h, m_CharBoxes[i].Height+3);
                }
                x += 3;
                h += 3;
                this.lexemeBox1.Location = new Point(Math.Max(3, (x-this.lexemeBox1.Width)/2), h);
                this.Width = Math.Max(this.lexemeBox1.Width+6, x+3);
                this.Height = h + this.lexemeBox1.Height + 3;
            }
            else
            {
                Graphics g = this.CreateGraphics();
                string s = m_Model.Lex.Surface;
                SizeF sz = g.MeasureString(s, ms_Font);
                this.Width = (int)(sz.Width + 4F);
                this.Height = (int)(sz.Height + 2F);
            }
        }

        public Word Model
        {
            get { return m_Model; }
        }

        /// <summary>
        /// このWordBoxの文字位置範囲（文頭からのオフセットを引いたもの）が
        /// rangeと重なるか判定する。
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool IntersectsWith(CharRange range)
        {
            Sentence sen = m_Model.Sen;
            int offset = sen.StartChar;

            CharRange r = new CharRange(m_Model.StartChar - offset, m_Model.EndChar - offset);
            return range.IntersectsWith(r);
        }

        /// <summary>
        /// このWordBoxの文字位置範囲（文頭からのオフセットを引いたもの）を返す.
        /// </summary>
        /// <returns></returns>
        public CharRange GetCharRange()
        {
            if (m_Model == null) return CharRange.Empty;
            Sentence sen = m_Model.Sen;
            int offset = sen.StartChar;

            return new CharRange(m_Model.StartChar - offset, m_Model.EndChar - offset);
        }

        private void WordBox_Paint(object sender, PaintEventArgs e)
        {
            if (!this.Visible) return;

            if (this.Abridged)
            {
                Graphics g = e.Graphics;
                g.FillRectangle(Brushes.AliceBlue, new Rectangle(0, 0, this.Width, this.Height));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                if (ms_Font == null)
                {
                    CreateFont();
                }
                g.DrawString("△", ms_Font, ms_Brush, new PointF(0F, -3F));
                return;
            }
            if (m_CurDispMode != DispModes.Morphemes)
            {
                Graphics g = e.Graphics;
                g.FillRectangle(Brushes.Snow, new Rectangle(0, 0, this.Width, this.Height));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                Brush br = m_bCenter ? ms_Brush2 : ms_Brush;
                string s = m_Model.Lex.Surface;

                if (ms_Font == null)
                {
                    CreateFont();
                }
                g.DrawString(s, ms_Font, br, new PointF(0F, 0F));
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.AntiqueWhite, new Rectangle(0, 0, this.Width, this.Height));
            }
            if (DepEditSettings.Current.ShowHeadInfo)
            {
                if (this.Model.HeadInfo == HeadInfo.Independent)
                {
                    e.Graphics.FillRectangle(Brushes.Green, new Rectangle(0, this.Height - 7, this.Width, 7));
                }
                else if (this.Model.HeadInfo == HeadInfo.Ancillary)
                {
                    e.Graphics.FillRectangle(Brushes.MediumVioletRed, new Rectangle(0, this.Height - 7, this.Width, 7));
                }
                else if (this.Model.HeadInfo == (HeadInfo.Independent | HeadInfo.Ancillary))
                {
                    e.Graphics.FillRectangle(Brushes.SaddleBrown, new Rectangle(0, this.Height - 7, this.Width, 7));
                }
            }

            if (m_Hovering)
            {
                e.Graphics.DrawRectangle(m_BorderPen, new Rectangle(0, 0, this.Width - 4, this.Height - 4));
            }
        }

        public bool Hover
        {
            get
            {
                return m_Hovering;
            }
            set
            {
                if (m_Hovering != value)
                {
                    m_Hovering = value;
                    Refresh();
                }
            }
        }

        private void WordBox_MouseHover(object sender, MouseEventArgs e)
        {
            if (this.UpdateGuidePanel != null)
            {
                UpdateGuidePanel(m_Model.Lex);
            }
        }

        /// <summary>
        /// GapがClickされたときのWord分割処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleGapClick(object sender, EventArgs e)
        {
            if (!(sender is Gap))
            {
                return;
            }
            Gap g = sender as Gap;
            int splitChar = m_Model.StartChar + g.GapPos + 1;

            if (this.SplitWordRequested != null)
            {
                SplitWordRequested(this, new MergeSplitEventArgs(m_Model.Bunsetsu.Doc.ID, m_Model.StartChar, m_Model.EndChar, splitChar));
            }
        }
    }
}
