using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using System.Diagnostics;
using System.Reflection;

namespace DependencyEdit
{
    public partial class BunsetsuBox : UserControl
    {
        private Bunsetsu m_Model;
        private List<WordBox> m_WordBoxes;
        private List<WordGap> m_Gaps;
        private bool m_bHover;

        static private Pen m_BorderPen;

        public event MergeSplitEventHandler OnSplit;

        static BunsetsuBox()
        {
            m_BorderPen = new Pen(Color.Red, 2F);
        }

        public BunsetsuBox(Bunsetsu b)
        {
            InitializeComponent();

            m_Model = b;
            m_WordBoxes = new List<WordBox>();
            m_Gaps = new List<WordGap>();
            m_bHover = false;
        }

        public Bunsetsu Model
        {
            get { return m_Model; }
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
            // 一番左でなければ、ギャップ領域も生成する
            if (m_WordBoxes.Count > 1)
            {
                WordGap gb = new WordGap(m_Gaps.Count);
                m_Gaps.Add(gb);
                this.Controls.Add(gb);
                gb.Click += new System.EventHandler(this.WordGaps_Click);
            }
            return wb;
        }

        public void RecalcLayout()
        {
            int x = 5;
            int y = 3;
            int height = 0;
            if (m_WordBoxes.Count == 0)
            {
                this.Width = this.Height = 20;
                return;
            }

            Trace.Assert(m_WordBoxes.Count == m_Gaps.Count + 1);

            for (int i = 0; i < m_WordBoxes.Count; i++)
            {
                WordBox wb = m_WordBoxes[i];
                wb.RecalcLayout();
                int width = wb.Width;
                wb.Location = new Point(x, y);

                x += width;
                if (i < m_WordBoxes.Count - 1)
                {
                    m_Gaps[i].Height = wb.Height;
                    m_Gaps[i].Location = new Point(x, y);
                }
                x += 5;

                height = Math.Max(height, wb.Height);
            }
            this.Width = x;
            this.Height = (int)(height + 8F);
        }

        /// <summary>
        /// いずれかのWordGap（子コントロール）がクリックされたときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WordGaps_Click(object sender, EventArgs e)
        {
            if (!(sender is WordGap))
            {
                return;
            }
            int gapPos = ((WordGap)sender).GapPos;
            int wordPos = this.m_WordBoxes[gapPos].Model.Pos;

            this.OnSplit(this, new MergeSplitEventArgs(this.m_Model.Pos, wordPos));
        }

        private void BunsetsuBox_Paint(object sender, PaintEventArgs e)
        {
            if (m_bHover)
            {
                //                Trace.WriteLine("Hover Rect=" + e.ClipRectangle);
                e.Graphics.DrawRectangle(m_BorderPen, new Rectangle(0, 0, this.Width - 2, this.Height - 2));
            }
        }
    }
}
