using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DependencyEdit
{
    class DepArrow
    {
        static DepArrow()
        {
            m_Pen[0] = new Pen(Color.Gray, 12);
            m_Pen[1] = new Pen(Color.Gray, 1);
            m_Pen[2] = new Pen(Color.Red, 1);
            m_Pen[3] = new Pen(Color.Black, 1);
            m_Pen[4] = new Pen(Color.DarkOrange, 1);
            m_Pen[0].SetLineCap(LineCap.RoundAnchor, LineCap.RoundAnchor, DashCap.Flat);
            for (int i = 1; i < 5; i++)
            {
                m_Pen[i].SetLineCap(LineCap.Square, LineCap.ArrowAnchor, DashCap.Flat);
            }
            m_Brush[0] = new SolidBrush(Color.Gray);
            m_Brush[1] = new SolidBrush(Color.Red);
            m_Brush[2] = new SolidBrush(Color.Black);
            m_Brush[3] = new SolidBrush(Color.DarkOrange);

            m_Font = new Font("Arial", 10, FontStyle.Italic);
        }

        public DepArrow(int id, int _from, int _to, string _tag, Rectangle _fromBox, Rectangle _toBox)
        {
            this.Index = id;
            this.FromIndex = _from;
            this.ToIndex = _to;
            this.Tag = _tag;
            this.IsSelected = false;
            this.IsCrossing = false;

            // 位置の計算
            m_FromBox = _fromBox;
            m_ToBox = _toBox;

            // ラベル（コントロール）を作成
            m_TagLabel = new TagLabel(id);
        }

        public Point StartPnt { get; set; }
        public Point EndPnt { get; set; }
        public Point EndPnt2 { get; set; }
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
        public int Index { get; set; }
        public string Tag { get; set; }
        public bool IsSelected { get; set; }
        public bool IsCrossing { get; set; }
        public Region Rgn
        {
            get { return m_Region; }
        }
        public TagLabel TagLabel
        {
            get { return m_TagLabel; }
        }


        public virtual void Draw(Graphics g, bool bDraft, Point offset)
        {
        }

        public virtual DAHitType HitTest(Graphics g, Point point)
        {
            return DAHitType.None;
        }

        public virtual void MoveHeadTo(Point point)
        {
        }

        protected Rectangle m_FromBox;
        protected Rectangle m_ToBox;
        protected Region m_Region;  // 矢印のヒットエリア全体を表す領域オブジェクト
        protected Point m_TagLocation;  // タグ文字列の左上位置
        protected TagLabel m_TagLabel;

        static protected Pen[] m_Pen = new Pen[5];
        static protected Brush[] m_Brush = new Brush[4];
        static protected Font m_Font;

    }
}