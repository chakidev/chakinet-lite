using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Service.DependencyEdit;

namespace DependencyEdit
{
    public partial class WordBox : UserControl
    {
        public event ChangeLexemeEventHandler OnChagneLexeme;

        public new static Font Font
        {
            get { return ms_Font; }
            set { ms_Font = value; }
        }

        private static Font ms_Font;
        private static Brush ms_Brush;
        private static Brush ms_Brush2;

        private bool m_bCenter;

        static WordBox() 
        {
            ms_Font = new Font("Lucida Sans Unicode", 8.0F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            ms_Brush = new SolidBrush(Color.Black);
            ms_Brush2 = new SolidBrush(Color.Red);
        }

        public WordBox( Word w, bool bCenter )
        {
            InitializeComponent();

            m_Model = w;
            m_bCenter = bCenter;
        }

        public void RecalcLayout()
        {
            Graphics g = this.CreateGraphics();
            string s = m_Model.Lex.Surface;
            SizeF sz = g.MeasureString(s, ms_Font);

            this.Width = (int)(sz.Width + 4F);
            this.Height = (int)(sz.Height + 2F);
        }

        public Word Model
        {
            get { return m_Model; }
        }


        private Word m_Model;

        private void WordBox_Paint(object sender, PaintEventArgs e)
        {
            string s = m_Model.Lex.Surface;
            Graphics g = e.Graphics;

            Brush br = m_bCenter ? ms_Brush2 : ms_Brush;
            g.DrawString(s, ms_Font, br, new PointF(0F, 0F));
        }

        private void WordBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point p = this.PointToScreen(e.Location);
                
                LexemeSelectionGrid popup = new LexemeSelectionGrid(DepEditControl.Instance.Cps, m_Model);
                popup.Location = p;
                if (popup.ShowDialog() == DialogResult.OK)
                {
                    Lexeme lex = popup.GetCurrentSelection();
                    if (lex != null)
                    {
                        // イベントによって、親のSentenceStructureがサービス呼び出しを行い、処理を実行する
                        if (this.OnChagneLexeme != null)
                        {
                            this.OnChagneLexeme(this, new ChangeLexemeEventArgs(m_Model.Pos, lex));
                        }
                    }
                }
            }
        }
    }
}
