using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using System.Drawing.Text;

namespace ChaKi.Common
{
    public partial class LexemeBox : UserControl
    {
        #region static fields
        private static List<string> m_tags;
        private static Font m_font;
        private static Brush m_brush;
        private static Font m_font2;
        private static Brush m_brush2;
        private static Brush m_brush3;
        private static Brush m_brush4;
        private static Brush m_brush5;
        private static Pen m_pen;
        #endregion

        private Lexeme m_Model;

        public new event EventHandler Click;

        static LexemeBox()
        {
            m_tags = new List<string>();
            foreach (KeyValuePair<LP, string> pair in Lexeme.PropertyName)
            {
                m_tags.Add(pair.Value);
            }
            m_font = new Font("ＭＳ Ｐゴシック", 9);
            m_font2 = new Font("Lucida Sans Unicode", 8, FontStyle.Italic);
            m_brush = new SolidBrush(Color.Black);
            m_brush2 = new SolidBrush(Color.MediumSlateBlue);
            m_brush3 = new SolidBrush(Color.White);
            m_brush4 = new SolidBrush(Color.Red);
            m_brush5 = new SolidBrush(Color.Coral);
            m_pen = new Pen(Color.Red, 2);
        }

        public LexemeBox()
        {
            InitializeComponent();

            // PropertyBoxの初期化
            foreach (string tag in m_tags)
            {
                this.listBox1.Items.Add(tag);
            }

            // コントロールの高さを計算
            int height = this.listBox1.GetItemHeight(0) * m_tags.Count + 6;
            this.listBox1.Height = height;
            this.Height = this.listBox1.Height;

            this.listBox1.Click += delegate(object o, EventArgs e) { if (Click != null) Click(o, e); };  //ListBox Clickイベントの中継
        }

        public LexemeBox(Lexeme model)
        {
            this.Model = model;
        }

        public Lexeme Model
        {
            set
            {
                m_Model = value;
                Invalidate();
                Update();
            }
        }


        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            int index = e.Index;
            if (index < 0 || index >= m_tags.Count)
            {
                return;
            }
            Rectangle r = e.Bounds;
            using (Graphics g = e.Graphics)
            {
                if (m_Model.PartOfSpeech == PartOfSpeech.Default)
                {
                    g.FillRectangle(m_brush5, e.Bounds);  // Unassignedが含まれる場合の背景色は赤系
                }
                else
                {
                    g.FillRectangle(m_brush3, e.Bounds);
                }

                // テキストの平滑化を有効にする
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                string s = null;
                if (m_Model != null)
                {
                    s = m_Model.GetStringProperty((LP)index);
                }
                if (s != null && s.Length > 0)
                {
                    g.DrawString(s, m_font, m_brush, r);
                }
                else
                {
                    g.DrawString("<" + m_tags[index] + ">", m_font2, m_brush2, r);
                }
            }
        }

    }
}
