using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Drawing2D;

using ChaKi.Entity.Corpora;
using ChaKi.Service.DependencyEdit;
using System.IO;

namespace DependencyEdit
{
    enum DADragType
    {
        None = 0,
        ArrowEnd
    }

    [Obsolete]
    public partial class SentenceStructure : UserControl
    {
        #region private fields
        private Sentence m_Model;     // Model object
        private DepEditService m_Service;    // Service object

        private Word m_CenterWord;
        private List<BunsetsuBox> m_BunsetsuBoxes;
        private List<ConcatButton> m_ConcatButtons;
        private List<DepArrow> m_Arrows;

        private DepArrow m_SelArrow;    // 選択中の矢印（null可）
        private DepArrow m_DragArrow;   // ドラッグ中の矢印（null可）
        private DADragType m_DragType;  // ドラッグ種別
        private BunsetsuBox m_OnBunsetsuBox;    // ドラッグ中にマウスのかかった文節Box（強調表示する; null可）
        #endregion

        public event EventHandler OnLayoutChanging;
        public event EventHandler OnLayoutChanged;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SentenceStructure()
        {
            InitializeComponent();

            m_Model = null;
            m_Service = null;
            m_BunsetsuBoxes = new List<BunsetsuBox>();
            m_ConcatButtons = new List<ConcatButton>();
            m_Arrows = new List<DepArrow>();
            m_CenterWord = null;
            m_SelArrow = null;
            m_OnBunsetsuBox = null;
        }

        /// <summary>
        /// このコントロールの表示の元となるSentence（モデルオブジェクト）を設定する
        /// </summary>
        /// <param name="sen"></param>
        public void SetSentence(Sentence sen, DepEditService svc)
        {
            m_Model = sen;
            m_Service = svc;
        }

        /// <summary>
        /// 指定したWordの強調表示を指示する.
        /// </summary>
        /// <param name="word"></param>
        public void SetCenterWord(int cid)
        {
            if (m_Model != null && cid >= 0 && cid < m_Model.Words.Count)
            {
                m_CenterWord = (Word)m_Model.Words[cid];
            }
        }

        /// <summary>
        /// モデルを元にコントロールを作る。
        /// 既にコントロールが存在した場合は、消去して作り直す。
        /// </summary>
        public void UpdateContents()
        {
#if true
            throw new NotImplementedException();
#else
            m_BunsetsuBoxes.Clear();
            m_ConcatButtons.Clear();
            m_Arrows.Clear();
            this.Controls.Clear();
            this.Height = 0;
            this.Width = 0;

            this.Visible = false;
            this.SuspendLayout();

            // 文節に対応するBunsetsuBoxを作成する
            for (int i = 0; i < m_Model.Bunsetsus.Count; i++)
            {
                Bunsetsu b = (Bunsetsu)m_Model.Bunsetsus[i];
                // 文節Box
                BunsetsuBox bbox = new BunsetsuBox(b);
                m_BunsetsuBoxes.Add(bbox);
                this.Controls.Add(bbox);
                bbox.OnSplit += new MergeSplitEventHandler(this.OnSplitBunsetsu);
                // 結合ボタン
                if (i < m_Model.Bunsetsus.Count - 1)
                {
                    ConcatButton cb = new ConcatButton(i);
                    m_ConcatButtons.Add(cb);
                    this.Controls.Add(cb);
                    cb.Click += new System.EventHandler(this.OnConcatBunsetsu);
                }
            }
            Debug.Assert(m_BunsetsuBoxes.Count > 0);

            // それぞれのWordを文節に割り当てていく
            foreach (Word w in m_Model.Words)
            {
                Bunsetsu b = w.Bunsetsu;
                BunsetsuBox bbx = FindBunsetsuBox(b);
                if (bbx != null)
                {
                    WordBox wb = bbx.AddWordBox(w, (w == m_CenterWord));
                    wb.OnChagneLexeme += new ChangeLexemeEventHandler(this.OnChangeLexeme);
                }
            }

            this.RecalcLayout();

            // 依存矢印(DepArrow)を作成する
            for (int i = 0; i < m_Model.Bunsetsus.Count; i++)
            {
                Bunsetsu b1 = m_Model.Bunsetsus[i] as Bunsetsu;
                if (b1 == null) continue;
                Bunsetsu b2 = b1.DependsTo;
                if (b2 == null) continue;
                BunsetsuBox box1 = FindBunsetsuBox(b1);
                BunsetsuBox box2 = FindBunsetsuBox(b2);
                if (box1 != null && box2 != null)
                {
                    DepArrow dp = new DepArrowArc(i, b1.Pos, b2.Pos, b1.DependsAs, box1.Bounds, box2.Bounds);
                    m_Arrows.Add(dp);
                    dp.TagLabel.TagChanged += new EventHandler(this.OnTagChanged);
                    this.Controls.Add(dp.TagLabel);
                }
            }
            CheckCrossDep();

            this.ResumeLayout();
            this.Visible = true;
            //            this.Scale(new SizeF(0.5F, 0.5F));
            //            ScrollControlIntoView(m_BunsetsuBoxes[0]);
#endif
        }

        /// <summary>
        /// コントロールの配置を調整する
        /// </summary>
        public void RecalcLayout()
        {
            if (OnLayoutChanging != null) OnLayoutChanging(this, null);

            int x = 10;
            int height = 0;
            // 全体の幅を計算・再配置し、それに基づいて必要高さを計算する
            for (int i = 0; i < m_BunsetsuBoxes.Count; i++)
            {
                BunsetsuBox bb = m_BunsetsuBoxes[i];
                bb.RecalcLayout();
                int width = bb.Width;
                bb.Left = x;
                x += (width + 4);
                if (i < m_BunsetsuBoxes.Count - 1)
                {
                    ConcatButton cb = m_ConcatButtons[i];
                    cb.Left = x;
                    x += cb.Width;
                }
                x += 4;
                height = Math.Max(height, bb.Height);
            }
            int y = height + (int)((x - 10) * DrawParamsArc.CurveParamY);

            // 全体のY位置を下詰めで計算する
            foreach (BunsetsuBox bb in m_BunsetsuBoxes)
            {
                bb.Top = y - height;
            }
            foreach (ConcatButton cb in m_ConcatButtons)
            {
                cb.Top = y - 22;
            }
            this.Width = x;
            this.Height = y;
            if (OnLayoutChanged != null) OnLayoutChanged(this, null);
        }

        /// <summary>
        /// 文節オブジェクトからそのViewとなっているBoxを得る
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private BunsetsuBox FindBunsetsuBox(Bunsetsu b)
        {
            foreach (BunsetsuBox bb in m_BunsetsuBoxes)
            {
                if (bb.Model == b)
                {
                    return bb;
                }
            }
            return null;// m_BunsetsuBoxes[0];
        }

        /// <summary>
        /// 依存矢印の交差がないかどうか判定する
        /// </summary>
        /// <returns></returns>
        public bool CheckCrossDep()
        {
            bool has_cross = false;
            foreach (DepArrow da in m_Arrows)
            {
                da.IsCrossing = false;
            }
            if (m_Arrows.Count < 2)
            {
                return false;
            }
            foreach (DepArrow da1 in m_Arrows)
            {
                int da1_s = da1.FromIndex;
                int da1_e = da1.ToIndex;
                int da1_min = Math.Min(da1_s, da1_e);
                int da1_max = Math.Max(da1_s, da1_e);
                foreach (DepArrow da2 in m_Arrows)
                {
                    int da2_s = da2.FromIndex;
                    int da2_e = da2.ToIndex;
                    int da2_min = Math.Min(da2_s, da2_e);
                    int da2_max = Math.Max(da2_s, da2_e);
                    if ((da1_min <= da2_min && da1_max >= da2_max)
                     || (da1_min >= da2_min && da1_max <= da2_max)
                     || (Math.Max(da1_s, da1_e) <= Math.Min(da2_s, da2_e))
                     || (Math.Max(da2_s, da2_e) <= Math.Min(da1_s, da1_e)))
                    {
                        continue;
                    }
                    else
                    {
                        da1.IsCrossing = true;
                        da2.IsCrossing = true;
                        has_cross = true;
                    }
                }
            }
            return has_cross;
        }

        /// <summary>
        /// 文節の切断が指示された場合のハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSplitBunsetsu(object sender, MergeSplitEventArgs e)
        {
            // Modelに対して文節切断を行う。
            try
            {
                m_Service.SplitBunsetsu(e.bunsetsuPos, e.wordPos);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Operation Failed:" + ex);
            }
            UpdateContents();
        }

        /// <summary>
        /// 語のLexeme対応変更が指示された場合のハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChangeLexeme(object sender, ChangeLexemeEventArgs e)
        {
            try
            {
                m_Service.ChangeLexeme(e.wordPos, e.newLex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Operation Failed:" + ex);
            }
            UpdateContents();
        }

        /// <summary>
        /// 「＋」ボタンが押されたときの処理
        /// 文節をマージする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConcatBunsetsu(object sender, EventArgs e)
        {
#if true
            throw new NotImplementedException();
#else
            //TODO: 文節間に直接の依存関係がなければ警告を出す。

            int b_pos = ((ConcatButton)sender).Pos;  // 「＋」ボタンの左にある文節のPos
            // b_posに属するWordの最大Posを求める（現在マージしようとしているWord Gapの位置）
            int w_pos = 0;
            foreach (Word w in m_Model.Words)
            {
                if (w.Bunsetsu.Pos <= b_pos)
                {
                    w_pos = w.Pos;
                }
            }
            try
            {
                m_Service.MergeBunsetsu(b_pos, w_pos);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Operation Failed:" + ex);
            }
            UpdateContents();
#endif
        }

        private void OnTagChanged(object sender, EventArgs e)
        {
            TagLabel label = (TagLabel)sender;
            int b_pos = label.Index;
            try
            {
                string curtag = ((Bunsetsu)m_Model.Bunsetsus[b_pos]).DependsAs;
                string newtag = label.Text;
                m_Service.ChangeDependencyTag(b_pos, curtag, newtag);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Operation Failed:" + ex);
            }
            UpdateContents();
        }

        /// <summary>
        /// 追加の描画処理
        /// 文節Box間に依存矢印を描画する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SentenceStructure_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            foreach (DepArrow arrow in m_Arrows)
            {
                arrow.Draw(e.Graphics, (m_DragType != DADragType.None), this.DisplayRectangle.Location);
            }
        }

        /// <summary>
        /// マウス左押下時の処理
        /// DepArrow に対してヒットテストを行う。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SentenceStructure_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // 矢印のヒットテスト
            DAHitType ht;
            UnpickDepArrow();
            m_DragArrow = PickDepArrow(e.Location, out ht);
            if (m_DragArrow != null)
            {
                m_DragArrow.IsSelected = true;
                if (ht == DAHitType.Other)
                {
                    m_DragType = DADragType.ArrowEnd;
                    this.Capture = true;
                }
                else if (ht == DAHitType.TagText)
                {
                    m_SelArrow = m_DragArrow;
                    m_DragArrow = null;
                }
            }
        }

        private void SentenceStructure_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_DragType == DADragType.ArrowEnd)
            {
                if (m_DragArrow != null)
                {
                    // マウスが左右境界を越えたらスクロールを行う。
                    if (e.X > this.Width)
                    {
                        this.HorizontalScroll.Value = Math.Min(this.HorizontalScroll.Value + 10, this.HorizontalScroll.Maximum);
                    }
                    else if (e.X < 0)
                    {
                        this.HorizontalScroll.Value = Math.Max(this.HorizontalScroll.Value - 10, this.HorizontalScroll.Minimum);
                    }
                    // 矢印線の描画更新
                    this.Invalidate(m_DragArrow.Rgn);
                    Point newpnt = new Point(e.Location.X - this.DisplayRectangle.Left, e.Location.Y - this.DisplayRectangle.Top);
                    m_DragArrow.MoveHeadTo(newpnt);
                    this.Invalidate(m_DragArrow.Rgn);

                    //  Phraseの強調
                    BunsetsuBox pb = HitTestBunsetsuBox(e.Location);
                    if (pb != null)
                    {
                        if (pb != m_OnBunsetsuBox)
                        {
                            if (m_OnBunsetsuBox != null)
                            {
                                m_OnBunsetsuBox.Hover = false;
                            }
                            m_OnBunsetsuBox = pb;
                            m_OnBunsetsuBox.Hover = true;
                            m_OnBunsetsuBox.Invalidate();
                            Application.DoEvents();
                        }
                    }
                    else
                    {
                        if (m_OnBunsetsuBox != null)
                        {
                            m_OnBunsetsuBox.Hover = false;
                            m_OnBunsetsuBox.Invalidate();
                            Application.DoEvents();
                        }
                        m_OnBunsetsuBox = null;
                    }
                }
            }
        }

        private void SentenceStructure_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (m_OnBunsetsuBox != null)
                {
                    m_OnBunsetsuBox.Hover = false;
                }
                if (m_DragType == DADragType.ArrowEnd && m_DragArrow != null)
                {
                    this.Capture = false;
                    // 後でUpdateContentsした後にスクロール位置を戻すため、現在のスクロール位置を記憶する
                    Point offset0 = this.DisplayRectangle.Location;

                    // 終点が文節Box内かどうか判定
                    BunsetsuBox pb = HitTestBunsetsuBox(e.Location);
                    if (pb != null && pb.Model.Pos != m_DragArrow.FromIndex)
                    {
                        Bunsetsu ph = m_Model.Bunsetsus[m_DragArrow.FromIndex] as Bunsetsu;
                        try
                        {
                            m_Service.ChangeDependency(ph.Pos, ph.DependsTo.Pos, pb.Model.Pos);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Operation Failed:" + ex);
                        }
                        UpdateContents();
                    }
                    else
                    {
                        UpdateContents();
                    }
                    SetDisplayRectLocation(offset0.X, offset0.Y);
                    AdjustFormScrollbars(true);
                    Invalidate();
                }
                m_DragType = DADragType.None;
                m_DragArrow = null;
            }
        }

        private DepArrow HitTestDepArrow(Point pt, out DAHitType ht)
        {
            Graphics g = this.CreateGraphics();
            foreach (DepArrow arrow in m_Arrows)
            {
                ht = arrow.HitTest(g, pt);
                if (ht != DAHitType.None)
                {
                    return arrow;
                }
            }
            g.Dispose();
            ht = DAHitType.None;
            return null;
        }

        private BunsetsuBox HitTestBunsetsuBox(Point pt)
        {
            foreach (BunsetsuBox pb in m_BunsetsuBoxes)
            {
                if (pb.Bounds.Contains(pt))
                {
                    return pb;
                }
            }
            return null;
        }

        private DepArrow PickDepArrow(Point pt, out DAHitType ht)
        {
            DepArrow arrow = HitTestDepArrow(pt, out ht);
            if (arrow != null)
            {
                m_SelArrow = arrow;
                return arrow;
            }
            return null;
        }

        private void UnpickDepArrow()
        {
            foreach (DepArrow arrow in m_Arrows)
            {
                arrow.IsSelected = false;
            }
            m_SelArrow = null;
        }

        public void Undo()
        {
            try
            {
                if (m_Service.Undo())
                {
                    UpdateContents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Operation Failed:" + ex);
            }
        }

        public void Redo()
        {
            try
            {
                if (m_Service.Redo())
                {
                    UpdateContents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Operation Failed:" + ex);
            }
        }

        public bool CanUndo()
        {
            if (m_Service != null)
            {
                return m_Service.CanUndo();
            }
            return false;
        }

        public bool CanRedo()
        {
            if (m_Service != null)
            {
                return m_Service.CanRedo();
            }
            return false;
        }

        public bool CanSave()
        {
            if (m_Service != null)
            {
                return m_Service.CanSave();
            }
            return false;
        }

        public void WriteToDotFile(string filename)
        {
            if (m_Model == null)
            {
                return;
            }
            TextWriter wr = null;
            try
            {
                wr = new StreamWriter(filename);
                wr.WriteLine(string.Format("digraph \"{0}.{1}\" {{", m_Service.GetCorpus().Name, m_Model.ID));
                wr.WriteLine("graph [charset = \"utf-8\"];");
                wr.WriteLine("node [fontname = \"sans\"];");
                foreach (Bunsetsu b in m_Model.Bunsetsus)
                {
                    string str = string.Format("{0} [label = \"{1}:{2}\"];", b.Pos, b.Pos, b.ToString());
                    wr.WriteLine(str);
                }
                foreach (Bunsetsu b in m_Model.Bunsetsus)
                {
                    if (b.DependsTo != null)
                    {
                        string str = string.Format("{0} -> {1} [label=\"{2}\"];", b.Pos, b.DependsTo.Pos, b.DependsAs);
                        wr.WriteLine(str);
                    }
                }
                wr.WriteLine("}");
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                if (wr != null)
                {
                    wr.Close();
                }
            }
        }
    }
}
