using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.DependencyEdit;
using ChaKi.Common;
using ChaKi.GUICommon;
using DependencyEditSLA.Widgets;

namespace DependencyEditSLA
{
    internal enum DADragType
    {
        None = 0,
        ArrowEnd
    }

    internal struct AbridgedRange
    {
        public int Start;
        public int End;
        public AbridgedRange(int s, int e) { Start = s; End = e; }
        public bool SegmentInRange(Segment seg) { return (seg.StartChar >= Start && seg.EndChar <= End); }
    }

    public partial class SentenceStructure : UserControl
    {
        public Sentence Model { get; set; }     // Model object

        #region private fields
        private DispModes m_DispMode;
        private IDepEditService m_Service;    // Service object

        private List<Segment> m_Segments;
        private List<Link> m_Links;
        private List<AbridgedRange> m_AbridgedRanges;
        private List<SegmentBox> m_SegmentBoxes;

        private Word m_CenterWord;
        private List<BunsetsuBox> m_BunsetsuBoxes;
        private List<FunctionButton> m_FunctionButtons;
        private List<DepArrow> m_Arrows;

        private DepArrow m_SelArrow;    // 選択中の矢印（null可）
        private DepArrow m_DragArrow;   // ドラッグ中の矢印（null可）
        private DADragType m_DragType;  // ドラッグ種別
        private BunsetsuBox m_OnBunsetsuBox;    // ドラッグ中にマウスのかかった文節Box（強調表示する; null可）

        private bool m_SelRectDragging;  // 矩形ドラッグ中か?
        private Point m_SelRectstart;    //  in Screen Coord
        private Rectangle m_SelRect;     //  in Screen Coord
        private List<WordBox> m_CoveredWordBoxes; // ドラッグ中にマウスのかかったWordBox(null可)
        private SegmentBox m_SelWordGroupItem;  // マウス右クリックで選択されたGroupItem
        #endregion

        public event EventHandler OnLayoutChanging;
        public event EventHandler OnLayoutChanged;
        public event EventHandler OnSentenceNoChanged;

        public event UpdateGuidePanelDelegate UpdateGuidePanel;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SentenceStructure()
        {
            InitializeComponent();

            this.Model = null;
            m_Service = null;
            m_BunsetsuBoxes = new List<BunsetsuBox>();
            m_FunctionButtons = new List<FunctionButton>();
            m_Arrows = new List<DepArrow>();
            m_CenterWord = null;
            m_SelArrow = null;
            m_OnBunsetsuBox = null;
            m_AbridgedRanges = new List<AbridgedRange>();
            m_SelRectDragging = false;
            m_SegmentBoxes = new List<SegmentBox>();
            m_SelWordGroupItem = null;
            m_CoveredWordBoxes = null;
        }

        /// <summary>
        /// このコントロールの表示の元となるSentence（モデルオブジェクト）を設定する
        /// </summary>
        /// <param name="sen"></param>
        public void SetSentence(Sentence sen, IDepEditService svc)
        {
            this.Model = sen;
            m_Service = svc;
            m_AbridgedRanges.Clear();
            m_SegmentBoxes.Clear();
            if (OnSentenceNoChanged != null)
            {
                OnSentenceNoChanged(this, null);
            }
        }

        public DispModes DispMode
        {
            get { return m_DispMode; }
            set
            {
                m_DispMode = value;
                this.Visible = false;
                RecalcLayout();
                CreateArrows();
                m_SelArrow = null;
                m_DragArrow = null;
                this.Visible = true;
                Refresh();
            }
        }

        public void Zoom()
        {
            float newsz = Math.Min(20.0f, WordBox.FontSize * 1.2f);
            WordBox.FontSize = newsz;
            CharBox.FontSize = newsz;
            RecalcLayout();
            Invalidate(true);
        }

        public void Pan()
        {
            float newsz = Math.Max(4.0f, WordBox.FontSize * 0.8f);
            WordBox.FontSize = newsz;
            CharBox.FontSize = newsz;
            RecalcLayout();
            Invalidate(true);
        }

        public void ZoomToDefault()
        {
            WordBox.FontSize = DepEditSettings.FontSizeDefault;
            CharBox.FontSize = DepEditSettings.FontSizeDefault;
            RecalcLayout();
            Invalidate(true);
        }

        /// <summary>
        /// 指定したWordの強調表示を指示する.
        /// </summary>
        /// <param name="word"></param>
        public void SetCenterWord(int cid)
        {
            if (this.Model != null && cid >= 0 && cid < this.Model.Words.Count)
            {
                m_CenterWord = (Word)this.Model.Words[cid];
            }
        }

        /// <summary>
        /// モデルを元にコントロールを作る。
        /// 既にコントロールが存在した場合は、消去して作り直す。
        /// </summary>
        public void UpdateContents()
        {
            m_BunsetsuBoxes.Clear();
            m_FunctionButtons.Clear();
            this.Controls.Clear();
            m_Arrows.Clear();
            m_SegmentBoxes.Clear();

            this.Visible = false; // Visible=falseにセットすると親のScrollableCnotrolがスクロール位置を変えてしまう。
            this.SuspendLayout();

            if (this.Model != null)
            {
                m_Service.GetBunsetsuTags(out m_Segments, out m_Links);

                // 文節に対応するBunsetsuBoxを作成する
                int last_abridged = -1;  // 直前の文節が省略されている場合、その省略範囲のindex
                for (int i = 0; i < m_Segments.Count; i++)
                {
                    // Segmentが省略対象か否かの判定
                    Segment seg = m_Segments[i];
                    int abridged = -1;
                    for (int j = 0; j < m_AbridgedRanges.Count; j++)
                    {
                        AbridgedRange arange = m_AbridgedRanges[j];
                        if (seg.EndChar != seg.StartChar && arange.SegmentInRange(seg))  // 終端のSegment(End=Start)はAbridgeしない
                        {
                            abridged = j;
                            break;
                        }
                    }
                    //                Console.WriteLine("seg:{0}, abridged={1}, last_abridged={2}", i, abridged, last_abridged);
                    if (abridged >= 0)
                    {
                        last_abridged = abridged;
                        continue;
                    }

                    // 直前の機能ボタン
                    if (i > 0)
                    {
                        FunctionButton cb;
                        if (last_abridged >= 0)
                        {
                            cb = new AbridgeButton(last_abridged);
                            cb.Click += new EventHandler(abridgeButton_Clicked);
                        }
                        else
                        {
                            cb = new ConcatButton(i - 1);
                            cb.Click += new System.EventHandler(concatButton_Clicked);
                        }
                        m_FunctionButtons.Add(cb);
                        this.Controls.Add(cb);
                    }
                    last_abridged = -1;

                    // 文節Box
                    BunsetsuBox bbox = new BunsetsuBox(seg);
                    m_BunsetsuBoxes.Add(bbox);
                    this.Controls.Add(bbox);
                    bbox.OnSplitBunsetsu += new MergeSplitEventHandler(bunsetsuBox_Split);
                    bbox.UpdateGuidePanel += new UpdateGuidePanelDelegate(bbox_UpdateGuidePanel);
                }
                Debug.Assert(m_BunsetsuBoxes.Count > 0);

                // それぞれのWordを文節に割り当てていく
                foreach (Segment b in m_Segments)
                {
                    foreach (Word w in this.Model.Words)
                    {
                        if (w.StartChar >= b.StartChar && w.StartChar < b.EndChar)
                        {
                            BunsetsuBox bbx = FindBunsetsuBox(b);
                            if (bbx != null)
                            {
                                WordBox wb = bbx.AddWordBox(w, (w == m_CenterWord));
                                wb.OnChagneLexeme += new ChangeLexemeEventHandler(this.OnChangeLexeme);
                            }
                        }
                    }
                }
                // Abridge Buttonに省略された部分の表層形を渡しておく
                for (int i = 0; i < m_AbridgedRanges.Count; i++)
                {
                    AbridgedRange arange = m_AbridgedRanges[i];
                    foreach (Word w in this.Model.Words)
                    {
                        if (w.StartChar >= arange.Start && w.StartChar < arange.End)
                        {
                            AbridgeButton abutton = FindAbridgeButton(i);
                            if (abutton != null)
                            {
                                abutton.TooltipText.Append(w.Lex.Surface);
                            }
                        }
                    }
                }
                foreach (FunctionButton ab in m_FunctionButtons)
                {
                    if (ab is AbridgeButton)
                    {
                        ((AbridgeButton)ab).EnableTooltip();
                    }
                }

                CreateArrows();

                CreateSegmentAndGroupBoxes();

                RecalcLayout();
            }

            ResumeLayout();
            Refresh();
            this.Visible = true;
//            this.Scale(new SizeF(0.5F, 0.5F));
//            ScrollControlIntoView(m_BunsetsuBoxes[0]);
        }

        private void CreateArrows()
        {
            if (m_Arrows == null || m_Links == null) return;

            m_Arrows.Clear();
            List<Control> cc = new List<Control>();
            foreach (Control c in this.Controls)
            {
                if (c is TagLabel)
                {
                    TagLabel t = (TagLabel)c;
                    t.TagChanged -= this.OnTagChanged;
                    this.Controls.Remove(t);
                }
                else
                {
                    cc.Add(c);
                }
            }
            this.Controls.Clear();
            foreach (Control c in cc)
            {
                this.Controls.Add(c);
            }

            // 依存矢印(DepArrow)を作成する
            for (int i = 0; i < m_Links.Count; i++)
            {
                Link link = m_Links[i];
                Segment b1 = link.From;
                if (b1 == null) continue;
                Segment b2 = link.To;
                if (b2 == null) continue;
                BunsetsuBox box1 = FindBunsetsuBox(b1);
                int box1index = m_BunsetsuBoxes.IndexOf(box1);
                BunsetsuBox box2 = FindBunsetsuBox(b2);
                int box2index = m_BunsetsuBoxes.IndexOf(box2);
                if (box1 != null && box2 != null)
                {
                    DepArrow dp = null;
                    if (m_DispMode == DispModes.Diagonal)
                    {
                        dp = new DepArrow(link, i, box1index, box2index, box1.Bounds, box2.Bounds);
                    }
                    else
                    {
                        dp = new DepArrowArc(link, i, box1index, box2index, box1.Bounds, box2.Bounds);
                    }
                    if (dp != null)
                    {
                        m_Arrows.Add(dp);
                        dp.TagLabel.TagChanged += new EventHandler(this.OnTagChanged);
                        this.Controls.Add(dp.TagLabel);
                    }
                }
            }
            CheckCrossDep();
        }

        private void CreateSegmentAndGroupBoxes()
        {
            if (m_CoveredWordBoxes != null)
            {
                m_CoveredWordBoxes.ForEach(new Action<WordBox>(delegate(WordBox wb) { wb.Hover = false; }));
                m_CoveredWordBoxes = null;
            }

            m_SegmentBoxes.Clear();
            CreateSegmentBoxes();
            CreateGroupBoxes();

            // z-orderレベルの計算
            new TopologicalSort<SegmentBox>(m_SegmentBoxes, delegate(SegmentBox x, SegmentBox y) { return x.Range.Includes(y.Range); })
                .Sort();

            // z-orderに従ってソートする（Levelが小さい＝包含されるものの順、描画の逆順）
            m_SegmentBoxes.Sort(new Comparison<SegmentBox>(
                delegate(SegmentBox x, SegmentBox y) { return x.Level - y.Level; }));
        }

        private void CreateSegmentBoxes()
        {
            IList<Segment> segments = m_Service.GetSegmentTags();
            foreach (Segment seg in segments)
            {
                    int offset = seg.Sentence.StartChar;
                    SegmentBox item = new SegmentBox(new CharRange(seg.StartChar - offset, seg.EndChar - offset), seg, null);
                    m_SegmentBoxes.Add(item);
            }
        }

        private void CreateGroupBoxes()
        {
            IList<Group> groups = m_Service.GetGroupTags();
            int index = 0;
            foreach (Group g in groups)
            {
                SegmentBoxGroup bg = new SegmentBoxGroup(index++, g);
                foreach (Annotation an in g.Tags)
                {
                    if (!(an is Segment)) continue;
                    Segment s = an as Segment;
                    int offset = s.Sentence.StartChar;
                    SegmentBox item = new SegmentBox(new CharRange(s.StartChar - offset, s.EndChar - offset), s, bg);
                    item.Parent = bg;
                    m_SegmentBoxes.Add(item);
                }
            }
        }

        /// <summary>
        /// コントロールの配置を調整する
        /// </summary>
        public void RecalcLayout()
        {
            if (OnLayoutChanging != null) OnLayoutChanging(this,null);
            DepEditSettings settings = GUISetting.Instance.DepEditSettings;

            int x = settings.LeftMargin;
            int y = settings.TopMargin;
            //int xmargin = 4;
            int ymargin = (int)(BunsetsuBox.DefaultHeight * 1.4);   // BunsetsuBoxをRecalcLayoutした後に正式にセットされる
            int height = 0;
            // 全体の幅を計算・再配置し、それに基づいて必要高さを計算する
            for (int i = 0; i < m_BunsetsuBoxes.Count; i++)
            {
                BunsetsuBox bb = m_BunsetsuBoxes[i];
                bb.RecalcLayout(this.DispMode);
                bb.Left = x;
                bb.Top = y;
                ymargin = (int)(bb.Height * 1.4);
                if (i < m_BunsetsuBoxes.Count - 1)
                {
                    FunctionButton cb = m_FunctionButtons[i];
                    cb.Left = (x + bb.Width + 4);
                    cb.Top = y;
                    x += (cb.Width + 4);
                }
                if (m_DispMode == DispModes.Horizontal || m_DispMode == DispModes.Morphemes)
                {
                    x += (bb.Width + settings.BunsetsuBoxMargin/*xmargin + 4*/);
                }
                else
                {
                    x += settings.BunsetsuBoxMargin;//(xmargin + 4);
                    y += ymargin + settings.LineMargin;
                }
                height = Math.Max(height, bb.Height);
            }
            height += (int)((x - 10) * settings.CurveParamY);

            foreach (DepArrow ar in m_Arrows)
            {
                ar.RecalcLayout(m_BunsetsuBoxes[ar.FromIndex].Bounds, m_BunsetsuBoxes[ar.ToIndex].Bounds);
            }

#if false
            // 全体のY位置を下詰めで計算する
            foreach (BunsetsuBox bb in m_BunsetsuBoxes)
            {
                bb.Top = y - height;
            }
            foreach (ConcatButton cb in m_ConcatButtons)
            {
                cb.Top = y - 22;
            }
#endif
            this.Width = Math.Max(x, this.Parent.Width);    // 最低限、親コントロール以上の幅・高さを確保する.
            this.Height = Math.Max(height, this.Parent.Height);
            if (OnLayoutChanged != null) OnLayoutChanged(this,null);
        }

        /// <summary>
        /// 文節オブジェクトからそのViewとなっているBoxを得る
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private BunsetsuBox FindBunsetsuBox(Segment b)
        {
            foreach (BunsetsuBox bb in m_BunsetsuBoxes)
            {
                if (bb.Model == b)
                {
                    return bb;
                }
            }
            return null;
        }

        /// <summary>
        /// m_AbridgedRangeのインデックスから対応するAbridgeButtonを得る
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private AbridgeButton FindAbridgeButton(int index)
        {
            foreach (FunctionButton b in m_FunctionButtons)
            {
                if (b is AbridgeButton)
                {
                    AbridgeButton ab = (AbridgeButton)b;
                    if (ab.AbridgedIndex == index)
                    {
                        return ab;
                    }
                }
            }
            return null;
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
                    if (da1.Tag == "P" || da2.Tag == "P")
                    {
                        continue;
                    }
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
        private void bunsetsuBox_Split(object sender, MergeSplitEventArgs e)
        {
            // Modelに対して文節切断を行う。
            try
            {
                m_Service.SplitBunsetsu(e.DocID, e.StartPos, e.EndPos, e.SplitPos);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
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
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            UpdateContents();
        }

        /// <summary>
        /// 「＋」ボタンが押されたときの処理
        /// 文節をマージする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void concatButton_Clicked(object sender, EventArgs e)
        {
            //TODO: 文節間に直接の依存関係がなければ警告を出す。

            try
            {
                int b_pos = ((ConcatButton)sender).Pos;  // 「＋」ボタンの左にある文節のPos
                if (b_pos >= m_Segments.Count-1)
                {
                    return;
                }
                Segment left = m_Segments[b_pos];
                Segment right = m_Segments[b_pos + 1];

                m_Service.MergeBunsetsu(left.Doc.ID, left.StartChar, right.EndChar, left.EndChar);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            UpdateContents();
        }

        private void OnTagChanged(object sender, EventArgs e)
        {
            TagLabel label = (TagLabel)sender;      //TODO: Labelではなく、Tag自体の変更にしなければならない
            Link link = label.Link;
            try
            {
                string curtag = link.Tag.Name;
                string newtag = label.Text;
                m_Service.ChangeLinkTag(link, curtag, newtag);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            UpdateContents();
        }

        /// <summary>
        /// 追加の描画処理
        /// 文節Box間に依存矢印を描画する。
        /// 語グループを描画する。---> todo:UserControlにした方がよい
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SentenceStructure_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (m_CoveredWordBoxes != null) // Drag中のみ
            {
                foreach (WordBox wb in m_CoveredWordBoxes)
                {
                    Rectangle r = wb.Bounds;
                    Point offs = ((BunsetsuBox)wb.Parent).Location;
                    r.Offset(offs);
                    e.Graphics.FillRectangle(Brushes.LightGray, r);
                }
            }

            // リストの後ろ(Z-order値の大きい方）から描画する
            for (int i = m_SegmentBoxes.Count - 1; i >= 0; i--)
            {
                m_SegmentBoxes[i].Draw(e.Graphics, m_BunsetsuBoxes);
            }

            // 矢印を描画する
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
            // 矢印のヒットテスト
            DAHitType ht;
            UnpickDepArrow();
            DepArrow arr = PickDepArrow(e.Location, out ht);
            if (e.Button == MouseButtons.Left)
            {
                if (arr != null)    // 矢印のドラッグ開始
                {
                    m_DragArrow = arr;
                    m_DragArrow.IsSelected = true;
                    m_SelRectDragging = false;
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
                else // ラバーバンドによる範囲選択開始
                {
                    m_DragArrow = null;
                    m_SelRectDragging = true;
                    m_SelRectstart = PointToScreen(e.Location);// ((Control)sender).PointToScreen(new Point(e.X, e.Y));
                    m_SelRect = new Rectangle(m_SelRectstart, Size.Empty);
                    this.Capture = true;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (arr != null)
                {
                    this.contextMenuStrip1.Show(PointToScreen(e.Location));
                    return;
                }
                m_SelWordGroupItem = HitTestSegmentBox(e.Location);
                if (m_SelWordGroupItem != null)
                {
                    m_SelWordGroupItem.Selected = true;
                    Invalidate(m_SelWordGroupItem.Bounds);
                    Update();
                    this.contextMenuStrip2.Items.Clear();
                    if (m_SelWordGroupItem.Parent != null)
                    {
                        ToolStripItem ti = this.contextMenuStrip2.Items.Add("Delete Group", null, DeleteWordGroup);
                        this.contextMenuStrip2.Items.Add(new ToolStripSeparator());
                        ti = this.contextMenuStrip2.Items.Add("Change to Apposition", null, ChangeWordGroupTag);
                        ti.Tag = "Apposition";
                        ti = this.contextMenuStrip2.Items.Add("Change to Parallel", null, ChangeWordGroupTag);
                        ti.Tag = "Parallel";
                    }
                    else
                    {
                        ToolStripItem ti = this.contextMenuStrip2.Items.Add("Delete Segment", null, DeleteSegment);
                        ti = this.contextMenuStrip2.Items.Add("Abridge", null, AbridgeSegment);
                    }
                    this.contextMenuStrip2.Show(PointToScreen(e.Location));
                    m_SelWordGroupItem.Selected = false;
                    return;
                }
                // Default behavior --> Setting Dialog
                EditSettings(PointToScreen(e.Location));
            }
        }

        private void EditSettings(Point location)
        {
            DepEditControlSettingDialog dlg = new DepEditControlSettingDialog();
            DepEditSettings oldSetting = new DepEditSettings(GUISetting.Instance.DepEditSettings);
            dlg.View = this;
            dlg.Location = new Point(location.X, Math.Max(0, location.Y - dlg.Height));
            if (dlg.ShowDialog() != DialogResult.OK)
            { // Revert
                GUISetting.Instance.DepEditSettings = oldSetting;
            }
            RecalcLayout();
            Refresh();
        }

        /// <summary>
        /// 矢印ピック→コンテクストメニュー→Abridge
        /// 矢印のかかる範囲のBoxをまとめて表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void contextMenuStrip1_Click(object sender, System.EventArgs e)
        {
            if (m_SelArrow == null)
            {
                return;
            }
            if (m_SelArrow.IsCrossing)
            {
                MessageBox.Show("Error: Crossing arrows detected.");
                return;
            }
            int from = Math.Min(m_SelArrow.FromIndex, m_SelArrow.ToIndex);
            int to = Math.Max(m_SelArrow.FromIndex, m_SelArrow.ToIndex);
            if (to - from < 2)
            {
                return;
            }
            int fromChar = m_BunsetsuBoxes[from+1].Model.StartChar;
            int toChar = m_BunsetsuBoxes[to - 1].Model.EndChar;
            m_AbridgedRanges.Add(new AbridgedRange(fromChar, toChar));
            UpdateContents();
        }

        private void abridgeButton_Clicked(object sender, EventArgs e)
        {
            if (!(sender is AbridgeButton)) return;
            AbridgeButton ab = (AbridgeButton)sender;
            int index = ab.AbridgedIndex;
            m_AbridgedRanges.RemoveAt(index);
            UpdateContents();
        }

        private void SentenceStructure_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_DragType == DADragType.ArrowEnd && m_DragArrow != null)
            {
                // マウスが左右境界を越えたらスクロールを行う。
                AutoScrollWhileDrag(e.Location);

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
            else if (m_SelRectDragging)
            {
                // マウスが左右境界を越えたらスクロールを行う。
                AutoScrollWhileDrag(e.Location);

                if (m_CoveredWordBoxes != null)
                {
                    m_CoveredWordBoxes.ForEach(new Action<WordBox>(delegate(WordBox wb) { wb.Hover = false; }));
                }

                using (Graphics g = this.CreateGraphics())
                {
                    Rectangle r = RectangleToClient(m_SelRect);

                    // ラバーバンドの描き直し
                    m_CoveredWordBoxes = null;
                    this.Invalidate(new Rectangle(r.X-1, r.Y-1, r.Width+2, r.Height+2));
                    this.Update();
                    Point sp = m_SelRectstart;
                    Point ep = PointToScreen(e.Location);
                    m_SelRect = new Rectangle(Math.Min(sp.X, ep.X), Math.Min(sp.Y, ep.Y), Math.Abs(ep.X - sp.X), Math.Abs(ep.Y - sp.Y));
                    // ドラッグ矩形のかかるWordBoxを求める.
                    g.DrawRectangle(Pens.DarkGray, RectangleToClient(m_SelRect));
                    m_CoveredWordBoxes = HitTestWordBox(m_SelRect);
                    if (m_CoveredWordBoxes != null && m_CoveredWordBoxes.Count > 0)
                    {
                        m_CoveredWordBoxes.ForEach(new Action<WordBox>(delegate(WordBox wb) { wb.Hover = true; }));
                    }
                    Application.DoEvents();
                }
            }
        }

        private void AutoScrollWhileDrag(Point p)
        {
            ScrollableControl parent = this.Parent as ScrollableControl;
            Point pp = parent.PointToClient(this.PointToScreen(p));
            Rectangle r0 = RectangleToClient(m_SelRect);
            Point p0 = PointToClient(m_SelRectstart);
            if (pp.X > parent.Width)
            {
                parent.HorizontalScroll.Value = Math.Min(parent.HorizontalScroll.Value + 10, parent.HorizontalScroll.Maximum);
            }
            else if (pp.X < 0)
            {
                parent.HorizontalScroll.Value = Math.Max(parent.HorizontalScroll.Value - 10, parent.HorizontalScroll.Minimum);
            }
            if (pp.Y > parent.Height)
            {
                parent.VerticalScroll.Value = Math.Min(parent.VerticalScroll.Value + 10, parent.VerticalScroll.Maximum);
            }
            else if (pp.Y < 0)
            {
                parent.VerticalScroll.Value = Math.Max(parent.VerticalScroll.Value - 10, parent.VerticalScroll.Minimum);
            }
            m_SelRect = RectangleToScreen(r0);
            m_SelRectstart = PointToScreen(p0);
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
                    Link link = m_DragArrow.Link;
                    if (pb != null && pb.Model != link.From)
                    {
                        try
                        {
                            m_Service.ChangeLinkEnd(link, link.To, pb.Model);
                        }
                        catch (Exception ex)
                        {
                            ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                            dlg.ShowDialog();
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
                else if (m_SelRectDragging)
                {
                    this.Capture = false;
                    // 矩形範囲内にあるWordBoxを得る。
                    m_CoveredWordBoxes = HitTestWordBox(m_SelRect);
                    if (m_CoveredWordBoxes.Count > 0)
                    {
                        this.groupingContextMenuStrip.Items.Clear();
                        ToolStripItem ti;
                        ti = this.groupingContextMenuStrip.Items.Add("Create New Group", null, CreateNewWordGroup);
                        this.groupingContextMenuStrip.Items.Add(new ToolStripSeparator());
                        int maxGroupIndex = -1;
                        m_SegmentBoxes.ForEach(new Action<SegmentBox>(delegate(SegmentBox i)
                            { if (i.Parent != null) maxGroupIndex = Math.Max(maxGroupIndex, i.Parent.Index); }));
                        for (int i = 0; i <= maxGroupIndex; i++)
                        {
                            ti = this.groupingContextMenuStrip.Items.Add(string.Format("Add to Group-{0}", i + 1), null, AddToWordGroup);
                        }
                        this.groupingContextMenuStrip.Items.Add(new ToolStripSeparator());
                        ti = this.groupingContextMenuStrip.Items.Add("Create 'Nest' Segment", null, CreateSegment);
                        ti.Tag = "Nest";
                        this.groupingContextMenuStrip.Show(PointToScreen(e.Location));
                    }
                    Invalidate();
                    Update();
                }
                m_DragType = DADragType.None;
                m_DragArrow = null;
                m_SelRectDragging = false;
            }
        }

        private void CreateSegment(object sender, EventArgs args)
        {
            ToolStripItem mi = sender as ToolStripItem;
            CharRange range = GetCoveringRange(m_CoveredWordBoxes);
            try
            {
                m_Service.CreateSegment(range, (string)mi.Tag);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
        }

        private void CreateNewWordGroup(object sender, EventArgs args)
        {
            CharRange range = GetCoveringRange(m_CoveredWordBoxes);
            try
            {
                m_Service.CreateWordGroup(range);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
        }

        private void AddToWordGroup(object sender, EventArgs args)
        {
            ToolStripItem mi = sender as ToolStripItem;
            int index = Int32.Parse(mi.Text.Substring("Add to Group-".Length)) - 1;

            try
            {
                CharRange range = GetCoveringRange(m_CoveredWordBoxes);
                Group grp = null;
                m_SegmentBoxes.Find(new Predicate<SegmentBox>(delegate(SegmentBox i)
                    { if (i.Parent.Index == index) { grp = i.Parent.Model; return true; } else return false; }));
                m_Service.AddItemToWordGroup(grp, range);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
        }

        /// <summary>
        /// Group選択→Delete Groupメニュー選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteWordGroup(object sender, System.EventArgs e)
        {
            try
            {
                Segment seg = m_SelWordGroupItem.Model;
                Group grp = m_SelWordGroupItem.Parent.Model;
                m_Service.RemoveItemFromWordGroup(grp, seg);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            m_SelWordGroupItem = null;
        }

        private void ChangeWordGroupTag(object sender, System.EventArgs e)
        {
            try
            {
                Segment seg = m_SelWordGroupItem.Model;
                Group grp = m_SelWordGroupItem.Parent.Model;
                string oldTag = grp.Tag.Name;
                string newTag = (string)((ToolStripItem)sender).Tag;
                if (oldTag != newTag)
                {
                    m_Service.ChangeWordGroupTag(grp, oldTag, newTag);
                    UpdateContents();
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            m_SelWordGroupItem = null;
        }

        /// <summary>
        /// Group選択→Delete Segmentメニュー選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteSegment(object sender, System.EventArgs e)
        {
            try
            {
                Segment seg = m_SelWordGroupItem.Model;
                m_Service.DeleteSegment(seg);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            m_SelWordGroupItem = null;
        }

        /// <summary>
        /// Group選択→Abridgeメニュー選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AbridgeSegment(object sender, System.EventArgs e)
        {
            Segment seg = m_SelWordGroupItem.Model;
            //SegmentのかかるBunsetsuBoxのリストを得る.

            // BUnsetsuBoxListの係り受けが、Abridge対象として適格かどうかを調べる.

            // 

            UpdateContents();
            m_SelWordGroupItem = null;
        }

        // 語BoxのListから、そのカバーする文字位置範囲を求める
        private CharRange GetCoveringRange(List<WordBox> boxes)
        {
            CharRange range = new CharRange();
            foreach (WordBox b in boxes)
            {
                CharRange r = b.GetCharRange();
                range.Union(r);
            }
            return range;
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

        private List<BunsetsuBox> HitTestBunsetsuBox(Rectangle rect)
        {
            List<BunsetsuBox> res = new List<BunsetsuBox>();
            foreach (BunsetsuBox pb in m_BunsetsuBoxes)
            {
                if (rect.IntersectsWith(pb.Bounds))
                {
                    res.Add(pb);
                }
            }
            return res;
        }

        private List<WordBox> HitTestWordBox(Rectangle rect)
        {
            List<WordBox> res = new List<WordBox>();
            foreach (BunsetsuBox bb in m_BunsetsuBoxes)
            {
                res.AddRange(bb.HitTestWordBox(rect));
            }
            return res;
        }


        private SegmentBox HitTestSegmentBox(Point p)
        {
            foreach (SegmentBox item in m_SegmentBoxes)
            {
                if (item.HitTest(p))
                {
                    return item;
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
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
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
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
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
            if (this.Model == null)
            {
                return;
            }
            TextWriter wr = null;
            try
            {
                wr = new StreamWriter(filename);
                m_Service.WriteToDotFile(wr);
            }
            catch (IOException ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            finally
            {
                if (wr != null)
                {
                    wr.Close();
                }
            }
        }

        void bbox_UpdateGuidePanel(Lexeme lex)
        {
            if (this.UpdateGuidePanel != null)
            {
                UpdateGuidePanel(lex);
            }
        }


        void contextMenuStrip2_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            Refresh();
        }
    }
}
