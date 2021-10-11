using System;
using System.Linq;
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
using System.Collections;
using PopupControl;
using ChaKi.Common.Settings;
using ChaKi.Entity.Settings;
using ChaKi.Service.Lexicons;
using Tag = ChaKi.Entity.Corpora.Annotations.Tag;
using ChaKi.Common.SequenceMatcher;

namespace DependencyEditSLA
{
    public partial class SentenceStructure : UserControl
    {
        public Sentence Model { get; set; }     // Model object

        public bool EditMode { get; set; }

        public int TargetProjectId { get; set; }

        #region private fields
        // Models
        private DispModes m_DispMode;
        private IDepEditService m_Service;    // Service object

        private List<Segment> m_Segments;
        private List<Link> m_Links;
        private List<Link> m_Links2;        // Bunsetsu Dependency以外のLink
        private List<AbridgedRange> m_AbridgedRanges;

        private List<string> m_SegmentTags;   // Segment Tag Definitions
        private List<string> m_LinkTags;      // Link Tag Definitions
        private List<string> m_GroupTags;     // Group Tag Definitions

        private Word m_CenterWord;

        // Views
        private List<BunsetsuBox> m_BunsetsuBoxes;
        private List<FunctionButton> m_FunctionButtons;
        private List<DepArrow> m_Arrows;
        private List<DepArrow> m_Arrows2;
        private List<SegmentBox> m_SegmentBoxes;
        private List<AbridgedRangePanel> m_AbridgedPanels;

        private DepArrow m_SelArrow;    // 選択中の矢印（null可）
        private DepArrow m_DragArrow;   // ドラッグ中の矢印（null可）
        private Point m_DragArrowOrgPos;  // ドラッグ中矢印のドラッグ前の位置（Linkを変更しないときにDragArrowの位置を戻すため必要）
        private DADragType m_DragType;  // ドラッグ種別
        private BunsetsuBox m_OnBunsetsuBox;    // ドラッグ中にマウスのかかった文節Box（強調表示する; null可）

        private WordBoxIndexRange m_SelRange; // 選択中の範囲

        private bool m_SelRectDragging;  // 矩形ドラッグ中か?
        private Point m_SelRectstart;    //  in Screen Coord
        private Rectangle m_SelRect;     //  in Screen Coord
        private List<WordBox> m_CoveredWordBoxes; // ドラッグ中にマウスのかかったWordBox(null可)
        private List<ISelectable> m_Selections;  // 選択中のSegmentBox, Link

        private LexemeSelectionGrid m_LexSelector;   // Lexeme選択Form
        private MWEUploadGrid m_MWEUploadSelector;   // MWE Upload Form
        private MWEDownloadGrid m_MWEDownloadSelector;   // MWE Download Form

        private bool m_CreateLinkMode; // Link生成操作中
        private Link m_LinkToCreate;  // 生成準備中のターゲットLink ojbect
        private Segment m_LinkDropTarget = null;  // Linkをドラッグした先のSegment (MoueUpでセットされる)

        // 階層編集の各レベルの定義（デフォルト状態では単一階層で、文全体を表すCharRangeのみを含む）
        private Stack<CharRange> m_VisibleLevels;

        private const int LevelGapWidth = 4;  // 編集階層に対応する外枠のマージン幅

        // Mouse Drag時にMoveイベントを継続させるためのタイマ
        private Timer m_PseudoMouseEventTimer;
        #endregion

        public event EventHandler OnLayoutChanging;
        public event EventHandler OnLayoutChanged;
        public event EventHandler OnSentenceNoChanged;
        public event EventHandler ScrollResetRequested;
        public event EventHandler SaveRequested;

        public event UpdateGuidePanelDelegate UpdateGuidePanel;
        public event UpdateAttributePanelDelegate UpdateAttributePanel;


        private object m_UpdateLock = new object();


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SentenceStructure()
        {
            InitializeComponent();

            this.Model = null;
            this.EditMode = false;
            m_Service = null;
            m_BunsetsuBoxes = new List<BunsetsuBox>();
            m_FunctionButtons = new List<FunctionButton>();
            m_Arrows = new List<DepArrow>();
            m_Arrows2 = new List<DepArrow>();
            m_CenterWord = null;
            m_SelArrow = null;
            m_OnBunsetsuBox = null;
            m_AbridgedRanges = new List<AbridgedRange>();
            m_AbridgedPanels = new List<AbridgedRangePanel>();
            m_SelRectDragging = false;
            m_SegmentBoxes = new List<SegmentBox>();
            m_Selections = new List<ISelectable>();
            m_CoveredWordBoxes = null;
            m_SelRange = new WordBoxIndexRange() { Start = 0, End = 0 };
            m_SegmentTags = new List<string>();
            m_LinkTags = new List<string>();
            m_GroupTags = new List<string>();
            m_VisibleLevels = new Stack<CharRange>();
            this.MouseWheel += SentenceStructure_MouseWheel;
            m_PseudoMouseEventTimer = new Timer() { Enabled = false, Interval = 50 };
            m_PseudoMouseEventTimer.Tick += OnPseudoMouseMove;
        }

        /// <summary>
        /// このコントロールの表示の元となるSentence（モデルオブジェクト）を設定する
        /// </summary>
        /// <param name="sen"></param>
        public void SetSentence(Sentence sen, IDepEditService svc, LexemeSelectionGrid selectorcontrol, MWEUploadGrid mweuploadcontrol, MWEDownloadGrid mwedownloadcontrol)
        {
            this.Model = sen;
            m_Service = svc;
            m_AbridgedRanges.Clear();
            m_SegmentBoxes.Clear();
            m_SelRange = new WordBoxIndexRange() { Start = 0, End = 0 };
            this.Parent.Visible = false;  // スクロールをリセットするため
            this.Parent.Visible = true;
            m_VisibleLevels = new Stack<CharRange>();
            SetTagDefinitions(svc);

            m_LexSelector = selectorcontrol;
            m_MWEUploadSelector = mweuploadcontrol;
            m_MWEDownloadSelector = mwedownloadcontrol;

            if (OnSentenceNoChanged != null)
            {
                OnSentenceNoChanged(this, null);
            }
            if (ScrollResetRequested != null)
            {
                ScrollResetRequested(this, null);
            }
        }

        private void SetTagDefinitions(IDepEditService svc)
        {
            var corpus = svc.GetCorpus();
            if (corpus == null)
            {
                return;
            }
            var corpusname = corpus.Name;
            TagSet tset = svc.GetTagSet();
            if (tset == null)
            {
                return;
            }

            m_SegmentTags.Clear();
            m_LinkTags.Clear();
            m_GroupTags.Clear();
            TagSelector.PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT].RemoveTab(corpusname);
            TagSelector.PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.LINK].RemoveTab(corpusname);
            TagSelector.PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.GROUP].RemoveTab(corpusname);
            //            LinkTagLabel.Tags.Clear();

            foreach (Tag tag in tset.Tags.OrderBy(t => t?.Name))
            {
                if (tag == null) continue;
                if (!tag.Version.IsCurrent) continue;  // Current Versionのみを追加
                if (tag.Type == ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT)
                {
                    m_SegmentTags.Add(tag.Name);
                    TagSelector.PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT].AddTag(tag, corpusname);
                }
                else if (tag.Type == ChaKi.Entity.Corpora.Annotations.Tag.LINK)
                {
                    m_LinkTags.Add(tag.Name);
                    TagSelector.PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.LINK].AddTag(tag, corpusname);
                    //                    LinkTagLabel.Tags.Add(tag);
                }
                else if (tag.Type == ChaKi.Entity.Corpora.Annotations.Tag.GROUP)
                {
                    m_GroupTags.Add(tag.Name);
                    TagSelector.PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.GROUP].AddTag(tag, corpusname);
                }
            }
        }

        public DispModes DispMode
        {
            get { return m_DispMode; }
            set
            {
                m_DispMode = value;
                try
                {
                    UpdateContents();
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Cannot Update Contents. Please make sure DepEdit is in Edit Mode.", ex);
                    dlg.ShowDialog();
                }
#if false
                this.Visible = false;
                lock (m_UpdateLock)
                {
                    RecalcLayout();
                    //矢印の再生成（TagLabel Controlの再生成を含む）
                    RemoveTagLabels();
                    List<Control> controlsToAdd = new List<Control>();
                    CreateArrows(controlsToAdd);
                    controlsToAdd.ForEach(new Action<Control>(delegate(Control c) { this.Controls.Add(c); }));
                    m_SelArrow = null;
                    m_DragArrow = null;
                }
                this.Visible = true;
#endif
                if (this.ScrollResetRequested != null)
                {
                    this.ScrollResetRequested(this, null);
                }
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
        public void SetCenterWord(int wid)
        {
            var words = this.Model.GetWords(this.TargetProjectId);
            if (this.Model != null && wid >= 0 && wid < words.Count)
            {
                m_CenterWord = words[wid];
                m_SelRange.Start = m_SelRange.End = wid;
            }
        }

        /// <summary>
        /// モデルを元にコントロールを作る。
        /// 既にコントロールが存在した場合は、消去して作り直す。
        /// </summary>
        public void UpdateContents()
        {
            lock (m_UpdateLock)
            {
                m_Selections.Clear();
                m_BunsetsuBoxes.Clear();
                m_FunctionButtons.Clear();
                m_Arrows.Clear();
                m_SegmentBoxes.Clear();
                m_Arrows2.Clear();

                if (this.Model != null)
                {
                    m_Service.GetBunsetsuTags(out m_Segments, out m_Links);
                    m_Links2 = (List<Link>)m_Service.GetLinkTags();

                    List<Control> controlsToAdd = new List<Control>();
                    CreateBunsetsuBoxes(controlsToAdd);
                    CreateArrows(controlsToAdd);
                    CreateSegmentAndGroupBoxes();
                    CreateLinks(controlsToAdd);  // Bunsetsu係り受け以外のLinkをm_Link2から生成 --> m_Arrows2

                    this.SuspendLayout();
                    this.Visible = false;
                    this.Controls.Clear();
                    controlsToAdd.ForEach(new Action<Control>(delegate (Control c) { this.Controls.Add(c); }));
                    RecalcLayout();
                    ResumeLayout();
                    this.Visible = true;
                    foreach (BunsetsuBox bb in m_BunsetsuBoxes)
                    {
                        bb.Visible = bb.Enabled;
                    }
                    Refresh();
                }
                else
                {
                    this.Controls.Clear();
                    Refresh();
                }
            }
            SetActive();
            Focus();
        }

        // 文節Segmentに対応するBunsetsuBoxを作成する
        private void CreateBunsetsuBoxes(IList controlsToAdd)
        {
            // 文節Boxを作成
            foreach (Segment seg in m_Segments)
            {
                BunsetsuBox bbox = new BunsetsuBox(seg);
                m_BunsetsuBoxes.Add(bbox);
                controlsToAdd.Add(bbox);
                bbox.SplitBunsetsuRequested += new MergeSplitEventHandler(HandleSplitBunsetsu);
                bbox.SplitWordRequested += new MergeSplitEventHandler(HandleSplitWord);
                bbox.MergeWordRequested += new MergeSplitEventHandler(HandleMergeWord);
                bbox.UpdateGuidePanel += bbox_UpdateGuidePanel;
                bbox.LexemeEditRequested += new EventHandler(bbox_LexemeEditRequested);
                if (m_VisibleLevels.Count > 0)
                {
                    CharRange visibleRange = m_VisibleLevels.Peek();
                    if (!visibleRange.IntersectsWith(new CharRange(seg.StartChar - seg.Sentence.StartChar, seg.EndChar - seg.Sentence.StartChar)))
                    {
                        bbox.Enabled = false;
                    }
                }
                bbox.Visible = false;
            }
            // "Return to Parent Level" buttonの追加（階層編集時のみ）
            if (m_VisibleLevels.Count > 0)
            {
                int d = LevelGapWidth * (m_VisibleLevels.Count);
                this.ReturnToParentButton.Location = new Point(d, d);
                controlsToAdd.Add(this.ReturnToParentButton);
            }

            // それぞれのWordを文節に割り当てていく
            foreach (Segment b in m_Segments)
            {
                foreach (Word w in this.Model.GetWords(this.TargetProjectId))
                {
                    if (w.StartChar >= b.StartChar && w.StartChar < b.EndChar)
                    {
                        BunsetsuBox bbx = FindBunsetsuBox(b);
                        if (bbx != null)
                        {
                            WordBox wb = bbx.AddWordBox(w, (w == m_CenterWord));
                        }
                    }
                }
            }

            // abridged部分をBunsetsuBox, WordBoxに反映する
            foreach (BunsetsuBox bb in m_BunsetsuBoxes)
            {
                Segment seg = bb.Model;
                if (seg.EndChar != seg.StartChar)
                {
                    bb.SetAbridged(m_AbridgedRanges);
                }
            }

            // AbridgedPanelを作成
            string text = this.Model.GetText(ContextPanelSettings.Current.UseSpacing, this.TargetProjectId);
            for (int i = 0; i < m_AbridgedRanges.Count; i++)
            {
                AbridgedRange range = m_AbridgedRanges[i];
                string aText = text.Substring(range.Start - this.Model.StartChar, range.Length);
                AbridgedRangePanel p = new AbridgedRangePanel(i, range, aText);
                m_AbridgedPanels.Add(p);
                p.Click += new EventHandler(HandleAbridgedRangePanelClick);
                controlsToAdd.Insert(0, p);
            }

#if false
            // 連続するAll-abridged bunsetsuについて、先頭のbunsetsu以外は非表示にする
            for (int i = 1; i < m_BunsetsuBoxes.Count; i++)
            {
                if (m_BunsetsuBoxes[i].IsAllAbridged && m_BunsetsuBoxes[i - 1].IsAllAbridged)
                {
                    m_BunsetsuBoxes[i].Visible = false;
                }
            }
#endif

            // 機能ボタン（各Bunsetsuの前にあると考える）
            FunctionButton cbFirst = null;
            for (int i = 0; i < m_BunsetsuBoxes.Count; i++)
            {
                FunctionButton cb = null;
                BunsetsuBox bb = m_BunsetsuBoxes[i];
                if (bb.Enabled)
                {
                    // 連続した省略文節の前にはボタンを置かない（改行しない）
                    if (!bb.IsAbridged
                     || (bb.IsRightAbridged && !bb.IsAllAbridged)
                     || (bb.IsLeftAbridged && i > 0 && !m_BunsetsuBoxes[i - 1].IsRightAbridged))
                    {
                        cb = new ConcatButton(i - 1);
                        cb.Click += new System.EventHandler(concatButton_Clicked);
                        if (cbFirst == null) cbFirst = cb;
                    }
                }
                m_FunctionButtons.Add(cb);
                controlsToAdd.Add(cb);
            }
            // 先頭にはボタンを置かない
            if (m_FunctionButtons.Count > 0 && cbFirst != null)
            {
                m_FunctionButtons.Remove(cbFirst);
                controlsToAdd.Remove(cbFirst);
            }
        }

        private void RemoveTagLabels()
        {
            List<Control> cc = new List<Control>();
            foreach (Control c in this.Controls)
            {
                if (c is LinkTagLabel)
                {
                    LinkTagLabel t = (LinkTagLabel)c;
                    t.TagChanged -= this.OnTagChanged;
                }
                else
                {
                    cc.Add(c);
                }
            }
            this.Controls.Clear();
            cc.ForEach(new Action<Control>(delegate (Control c) { this.Controls.Add(c); }));
        }

        private void CreateArrows(IList<Control> controlsToAdd)
        {
            if (m_Arrows == null || m_Links == null) return;

            m_Arrows.Clear();

            // 依存矢印(DepArrow)を作成する
            for (int i = 0; i < m_Links.Count; i++)
            {
                var arrow = CreateArrow(m_Links[i]);
                if (arrow != null)
                {
                    m_Arrows.Add(arrow);
                    controlsToAdd.Add(arrow.TagLabel);
                    arrow.TagLabel.TagChanged += new EventHandler(this.OnTagChanged);
                }

            }
            CheckCrossDep();
        }

        private DepArrow CreateArrow(Link link)
        {
            Segment b1 = link.From;
            if (b1 == null) return null;
            Segment b2 = link.To;
            if (b2 == null) return null;
            Rectangle? bounds1 = null;
            Rectangle? bounds2 = null;
            var bbox1 = FindBunsetsuBox(b1);
            int box1index = m_BunsetsuBoxes.IndexOf(bbox1);
            var bbox2 = FindBunsetsuBox(b2);
            int box2index = m_BunsetsuBoxes.IndexOf(bbox2);
            var placement = DepArrowPlacement.Bottom;
            if (bbox1 != null && bbox2 != null)
            {
                bounds1 = bbox1.Bounds;
                bounds2 = bbox2.Bounds;
            }
            else
            {
                // 係り受けでないLinkが指定された場合
                var sbox1 = FindSegmentBox(b1);
                box1index = m_SegmentBoxes.IndexOf(sbox1);
                var sbox2 = FindSegmentBox(b2);
                box2index = m_SegmentBoxes.IndexOf(sbox2);
                if (sbox1 != null)
                {
                    bounds1 = sbox1.Bounds;
                }
                else if (bbox1 != null)
                {
                    bounds1 = bbox1.Bounds;
                    box1index = box2index; // BunsetsuBox -> SegmentBoxへのLinkは生成できないので強制表示させるため自己リンクとする.
                }
                if (sbox2 != null)
                {
                    bounds2 = sbox2.Bounds;
                }
                else if (bbox2 != null)
                {
                    bounds2 = bbox2.Bounds;
                    box2index = box1index; // SegmentBox -> BunsetsuBoxへのLinkは生成できないので強制表示させるため自己リンクとする.
                }
                placement = DepArrowPlacement.Top;
            }
            if (bounds1.HasValue && bounds2.HasValue)
            {
                DepArrow dp = null;
                if (m_DispMode == DispModes.Diagonal)
                {
                    dp = new DepArrow(link, box1index, box2index, bounds1.Value, bounds2.Value, placement);
                }
                else
                {
                    dp = new DepArrowArc(link, box1index, box2index, bounds1.Value, bounds2.Value, placement);
                }
                if (dp != null)
                {
                    return dp;
                }
            }
            return null;
        }

        // Dependency以外のLinkを作成.
        private void CreateLinks(IList<Control> controlsToAdd)
        {
            if (m_Arrows2 == null || m_Links2 == null) return;

            // 依存矢印(DepArrow)を作成する
            for (int i = 0; i < m_Links2.Count; i++)
            {
                var arrow = CreateArrow(m_Links2[i]);
                if (arrow != null)
                {
                    m_Arrows2.Add(arrow);
                    controlsToAdd.Add(arrow.TagLabel);
                    arrow.TagLabel.TagChanged += new EventHandler(this.OnTagChanged);
                }
            }
        }


        private void CreateSegmentAndGroupBoxes()  // 現在の所このメソッド以下ではControlを生成しない
        {
            if (m_CoveredWordBoxes != null)
            {
                m_CoveredWordBoxes.ForEach(new Action<WordBox>(delegate (WordBox wb) { wb.Hover = false; }));
                m_CoveredWordBoxes = null;
            }

            m_SegmentBoxes.Clear();
            CreateSegmentBoxes();
            CreateGroupBoxes();

            // z-orderレベルの計算
            new TopologicalSort<SegmentBox>(m_SegmentBoxes,
                (x, y) =>
                {
                    if (x.Range.IsSameStartEnd(y.Range))
                    {
                        return ((x.Model as Segment).ID > (y.Model as Segment).ID);
                    }
                    return x.Range.Includes(y.Range);
                })
                .Sort();

            // z-orderに従ってソートする（Levelが小さい＝包含されるものの順、描画の逆順）
            m_SegmentBoxes.Sort(new Comparison<SegmentBox>(
                delegate (SegmentBox x, SegmentBox y) { return x.Level - y.Level; }));
        }

        private void CreateSegmentBoxes()
        {
            IList<Segment> segments = m_Service.GetSegmentTags();
            foreach (Segment seg in segments)
            {
                int offset = seg.Sentence.StartChar;
                if (SegmentIsAbridged(seg)) continue;
                if (m_VisibleLevels.Count > 0)
                {
                    CharRange visibleRange = m_VisibleLevels.Peek();
                    if (!visibleRange.Includes(new CharRange(seg.StartChar - offset, seg.EndChar - offset)))
                    {
                        continue;
                    }
                }
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
                    if (m_VisibleLevels.Count > 0)
                    {
                        CharRange visibleRange = m_VisibleLevels.Peek();
                        if (!visibleRange.Includes(new CharRange(s.StartChar - offset, s.EndChar - offset)))
                        {
                            continue;
                        }
                    }
                    if (SegmentIsAbridged(s)) continue;
                    SegmentBox item = m_SegmentBoxes.Find(b => b.Model == s);
                    if (item == null)
                    {
                        item = new SegmentBox(new CharRange(s.StartChar - offset, s.EndChar - offset), s, bg);
                        m_SegmentBoxes.Add(item);
                    }
                    item.Parent = bg;
                }
            }
        }

        /// <summary>
        /// 同じ親(Group)を持つSegmentBoxのリストを返す.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        private IList<SegmentBox> FindSiblingGroupItems(SegmentBox box)
        {
            List<SegmentBox> res = new List<SegmentBox>();
            if (box == null)
            {
                return res;
            }
            SegmentBoxGroup grp = box.Parent;
            foreach (SegmentBox sb in m_SegmentBoxes)
            {
                if (sb.Parent == grp) res.Add(sb);
            }
            return res;
        }

        private bool SegmentIsAbridged(Segment seg)
        {
            foreach (AbridgedRange range in m_AbridgedRanges)
            {
                if (range.Includes(seg.StartChar, seg.EndChar))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// コントロールの配置を調整する
        /// </summary>
        public void RecalcLayout()
        {
            if (OnLayoutChanging != null) OnLayoutChanging(this, null);
            if (this.Model == null)
            {
                return;
            }
            DepEditSettings settings = DepEditSettings.Current;

            int x = settings.LeftMargin + LevelGapWidth * m_VisibleLevels.Count;
            int linestart_x = x;
            int y = settings.TopMargin + LevelGapWidth * m_VisibleLevels.Count;
            //int xmargin = 4;
            int ymargin = (int)(BunsetsuBox.DefaultHeight * 1.4);   // BunsetsuBoxをRecalcLayoutした後に正式にセットされる
            int height = 0;
            int xindent_diagonal = 0;
            int cur_w = 0;
            // 全体の幅を計算・再配置し、それに基づいて必要高さを計算する
            for (int i = 0; i < m_BunsetsuBoxes.Count; i++)
            {
                BunsetsuBox bb = m_BunsetsuBoxes[i];
                if (!bb.Enabled)
                {
                    continue;
                }
                bb.RecalcLayout(this.DispMode);
                bb.Left = x;
                bb.Top = y;
                ymargin = Math.Max(ymargin, (int)(bb.Height * 1.4));
                cur_w = bb.Width;
                x += cur_w;
                if (bb.IsAllAbridged && i < m_BunsetsuBoxes.Count - 1 && m_BunsetsuBoxes[i + 1].IsAllAbridged)
                {   // 連続する全省略文節は同じ位置に表示
                    x -= cur_w;
                    cur_w = 0;
                }
                FunctionButton cb = null;
                if (i < m_BunsetsuBoxes.Count - 1)
                {
                    cb = m_FunctionButtons[i];
                    if (cb != null)
                    {
                        cb.Left = (x + settings.BunsetsuBoxMargin);
                        cb.Top = y;
                        x += (cb.Width + settings.BunsetsuBoxMargin);
                        xindent_diagonal += (cb.Width + settings.BunsetsuBoxMargin);
                    }
                }
                if (m_DispMode == DispModes.Horizontal || m_DispMode == DispModes.Morphemes)
                {
                    x += settings.BunsetsuBoxMargin;
                }
                else
                {
                    bool needNewLine = true;
                    if (cb == null)
                    {
                        needNewLine = false;
                    }
                    if (!needNewLine) // 横に並ぶ文節のうちAllAbridgedでないものは、Diagonal表示のインデントの考慮に入れる
                    {
                        xindent_diagonal += cur_w;
                    }
                    if (needNewLine)
                    {
                        y += ymargin + settings.LineMargin;
                        linestart_x += (xindent_diagonal + settings.BunsetsuBoxMargin);
                        x = linestart_x;
                        xindent_diagonal = 0;
                    }
                }
                height = Math.Max(height, bb.Height);
            }
            height += (int)((x - 10) * settings.CurveParamY);

            // AbridgedRangePanelの位置を計算する
            int offset = this.Model.StartChar;
            foreach (AbridgedRangePanel panel in m_AbridgedPanels)
            {
                Rectangle r = CalculateSegmentRegion(new CharRange(panel.Range.Start - offset, panel.Range.End - offset));
                r.Offset(0, 3);
                panel.Location = r.Location;
                panel.Width = r.Width;
                panel.Height = r.Height;
            }

            // 矢印の位置を計算する
            foreach (DepArrow ar in m_Arrows)
            {
                var box1 = m_BunsetsuBoxes[ar.FromIndex];
                var box2 = m_BunsetsuBoxes[ar.ToIndex];
                if (box1.Enabled && box2.Enabled
                 && !box1.IsAllAbridged && !box2.IsAllAbridged)
                {
                    ar.Visible = true;
                    ar.TagLabel.Visible = true;
                    ar.RecalcLayout(m_BunsetsuBoxes[ar.FromIndex].Bounds, m_BunsetsuBoxes[ar.ToIndex].Bounds);
                }
                else
                {
                    ar.Visible = false;
                    ar.TagLabel.Visible = false;
                }
            }

            // m_Arrows2の位置計算はここではなく、Picturebox.Paintハンドラで行う.(ここではまだSegmentBoxの位置が決まらないため.)
            foreach (DepArrow ar in m_Arrows2)
            {
                ar.Visible = true;
                ar.TagLabel.Visible = true;
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
            if (OnLayoutChanged != null) OnLayoutChanged(this, null);
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
        /// SegmentオブジェクトからそのViewとなっているBoxを得る
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private SegmentBox FindSegmentBox(Segment b)
        {
            foreach (SegmentBox bb in m_SegmentBoxes)
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
        private void HandleSplitBunsetsu(object sender, MergeSplitEventArgs e)
        {
            if (!EditMode)
            {
                return;
            }

            // Modelに対して文節切断を行う。
            try
            {
                m_Service.SplitBunsetsu(e.DocID, e.StartPos, e.EndPos, e.SplitPos);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
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
            if (!EditMode)
            {
                return;
            }

            //TODO: 文節間に直接の依存関係がなければ警告を出す。

            try
            {
                int b_pos = ((ConcatButton)sender).Pos;  // 「＋」ボタンの左にある文節のPos
                if (b_pos >= m_Segments.Count - 1)
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
                m_Service.ResetTransaction();
            }
            UpdateContents();
        }

        private void OnTagChanged(object sender, EventArgs e)
        {
            if (!EditMode)
            {
                return;
            }

            LinkTagLabel label = (LinkTagLabel)sender;      //TODO: Labelではなく、Tag自体の変更にしなければならない
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
                m_Service.ResetTransaction();
            }
            UpdateContents();
        }

        /// <summary>
        /// 語を分割する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleSplitWord(object sender, MergeSplitEventArgs args)
        {
            if (!EditMode)
            {
                return;
            }

            // Modelに対して語切断を行う。
            try
            {
                string sentext = this.Model.GetText(false, this.TargetProjectId);
                string surface1 = sentext.Substring(args.StartPos - this.Model.StartChar, (args.SplitPos - args.StartPos));
                string surface2 = sentext.Substring(args.SplitPos - this.Model.StartChar, (args.EndPos - args.SplitPos));
                // 仮：空白文字の認定方法を整理する必要あり.
                surface1 = surface1.Trim();
                surface2 = surface2.Trim();

                m_Service.SplitWord(args.DocID, args.StartPos, args.EndPos, args.SplitPos, surface1, surface2);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            UpdateContents();
        }

        /// <summary>
        /// 語をマージする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleMergeWord(object sender, MergeSplitEventArgs args)
        {
            if (!EditMode)
            {
                return;
            }

            // Modelに対して語切断を行う。
            try
            {
                string sentext = this.Model.GetText(false, this.TargetProjectId);
                string surface = sentext.Substring(args.StartPos - this.Model.StartChar, (args.EndPos - args.StartPos));
                surface = surface.Trim();  // 仮：空白文字の認定方法を整理する必要あり.
                m_Service.MergeWord(args.DocID, args.StartPos, args.EndPos, args.SplitPos, surface);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
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
            if (e.Graphics == null) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            for (int i = 0; i < m_VisibleLevels.Count; i++)
            {
                Rectangle borderRectangle = this.ClientRectangle;
                int w = LevelGapWidth * (i + 1);
                borderRectangle.Inflate(-w, -w);
                ControlPaint.DrawBorder3D(e.Graphics, borderRectangle, Border3DStyle.Bump);
            }

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
                SegmentBox sb = m_SegmentBoxes[i];
                sb.Draw(e.Graphics, CalculateSegmentRegion(sb.Range));
            }

            // 矢印を描画する
            foreach (DepArrow arrow in m_Arrows)
            {
                var draftDraw = (m_DragType != DADragType.None && m_DragArrow == arrow);
                arrow.Draw(e.Graphics, draftDraw, this.DisplayRectangle.Location);
            }
            foreach (DepArrow arrow in m_Arrows2)
            {
                var cnt = m_SegmentBoxes.Count;
                var draftDraw = (m_DragType != DADragType.None && m_DragArrow == arrow);
                if (arrow.FromIndex < cnt && arrow.ToIndex < cnt && arrow.FromIndex >= 0 && arrow.ToIndex >= 0)
                {
                    if (m_DragType != DADragType.ArrowEnd || !arrow.IsLayoutCalculated)
                    {
                        arrow.RecalcLayout(m_SegmentBoxes[arrow.FromIndex].Bounds, m_SegmentBoxes[arrow.ToIndex].Bounds);
                    }
                    arrow.Draw(e.Graphics, draftDraw, this.DisplayRectangle.Location);
                }
                else
                {
                    //                        Console.WriteLine("Link drawing error: from={0}, to={1}, SegmentBoxes.Count={2}", arrow.FromIndex, arrow.ToIndex, cnt);
                }
            }
        }

        private Rectangle CalculateSegmentRegion(CharRange range)
        {
            Rectangle r = Rectangle.Empty;
            foreach (BunsetsuBox box in m_BunsetsuBoxes)
            {
                if (!box.IntersectsWith(range))
                {
                    continue;
                }
                Rectangle rr;
                if (box.IsAllAbridged)
                {
                    rr = box.Bounds;
                }
                else
                {
                    rr = box.GetCoveredRectangle(range);
                    rr.Offset(box.Location);
                }
                if (r.IsEmpty)
                {
                    r = rr;
                }
                else
                {
                    Point TopLeft = new Point(Math.Min(r.Left, rr.Left), Math.Min(r.Top, rr.Top));
                    Point BottomRight = new Point(Math.Max(r.Right, rr.Right), Math.Max(r.Bottom, rr.Bottom));
                    r = new Rectangle(TopLeft.X, TopLeft.Y, BottomRight.X - TopLeft.X, BottomRight.Y - TopLeft.Y);
                }
            }
            return r;
        }

        /// <summary>
        /// キーボードフォーカスを取得する
        /// </summary>
        public void SetActive()
        {
            // ボタンにフォーカスを設定することで、ProcessDialogKey()でキーのキャプチャが可能となる.
            if (m_FunctionButtons.Count > 0)
            {
                if (m_FunctionButtons[0] != null)
                {
                    m_FunctionButtons[0].Select();
                }
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
            SetActive();

            // 矢印のヒットテスト
            DAHitType ht;
            //UnpickDepArrow();
            DepArrow arr = PickDepArrow(e.Location, out ht);
            SegmentBox sbHit;
            SegmentBoxHitTestResult res = HitTestSegmentBox(e.Location, out sbHit);
            if (e.Button == MouseButtons.Left)
            {
                if (!this.EditMode)
                {
                    return;
                }
                ClearSelection();
                if (arr != null && !m_CreateLinkMode)    // 矢印のドラッグ開始（但し、CrateLink操作時は矢印にはヒットさせない）
                {
                    m_DragArrow = arr;
                    m_DragArrowOrgPos = arr.EndPnt;
                    AddToSelection(arr, true);
                    // AttributePanel更新
                    if (arr.Link != null)
                    {
                        UpdateAttributePanel(m_Service.GetCorpus(), arr.Link);
                    }
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
                // Segment Boxのクリック
                else if (res == SegmentBoxHitTestResult.HitLayerButton)
                {
                    AddToSelection(sbHit, true);
                    StartPartEdit(this, null);
                }
                else if (res == SegmentBoxHitTestResult.HitSegment)
                {
                    m_SelArrow = null;  // 矢印の選択状態は解除する.
                    AddToSelection(sbHit, true);
                    // Attribute Panelの内容を更新
                    if (sbHit.Parent != null)
                    {
                        // Segment & Group
                        UpdateAttributePanel(m_Service.GetCorpus(), new object[] { sbHit.Model, sbHit.Parent.Model });
                    }
                    else
                    {
                        // Segment only
                        UpdateAttributePanel(m_Service.GetCorpus(), sbHit.Model);
                    }
                    if (m_CreateLinkMode)
                    {
                        // このSegmentを開始点とするLinkを新たに生成する準備
                        var link = new Link()
                        {
                            From = sbHit.Model as Segment,
                            To = sbHit.Model as Segment,
                            Tag = new Tag(ChaKi.Entity.Corpora.Annotations.Tag.LINK, "")
                        };
                        m_DragArrow = CreateArrow(link);
                        m_DragType = DADragType.ArrowEnd;
                        m_Arrows2.Add(m_DragArrow);
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
                if (!this.EditMode)
                {
                    return;
                }
                if (arr != null)
                {
                    this.deleteLinkToolStripMenuItem.Enabled = !arr.IsDependencyLink;  // 係り受け矢印ならDeleteは使用不可とする.
                    this.contextMenuStrip1.Show(PointToScreen(e.Location));
                    return;
                }
                ClearSelection();
                if (res == SegmentBoxHitTestResult.HitSegment)
                {
                    AddToSelection(sbHit, true);
                    SetupContextMenu();
                    this.contextMenuStrip2.Show(PointToScreen(e.Location));
                    return;
                }
                // それ以外の場所での右クリック → コンテクストメニュー表示
                var p = this.PointToScreen(e.Location);
                this.contextMenuStrip3.Show(p);
            }
        }

        public void ClearSelection()
        {
            if (m_Selections.Count > 0)
            {
                m_Selections.ForEach(sel =>
                    {
                        sel.Selected = false;
                        sel.InvalidateElement(this);
                    });
                m_Selections.Clear();
                Update();
            }
        }

        private void AddToSelection(ISelectable elem, bool singlesel = false)
        {
            if (!m_Selections.Contains(elem))
            {
                if (singlesel)
                {
                    ClearSelection();
                }
                elem.Selected = true;
                m_Selections.Add(elem);
                elem.InvalidateElement(this);
                Update();
            }
        }

        private bool TryGetSingleSelectionAsSegmentBox(out SegmentBox sbox)
        {
            if (m_Selections.Count != 1)
            {
                sbox = null;
                return false;
            }
            sbox = m_Selections[0] as SegmentBox;
            if (sbox == null)
            {
                return false;
            }
            return true;
        }

        private void SetupContextMenu()
        {
            this.contextMenuStrip2.Items.Clear();
            if (m_Selections.Count != 1)
            {
                return;
            }
            var sel = m_Selections[0] as SegmentBox;
            if (sel.Parent != null)
            {
                ToolStripItem ti = this.contextMenuStrip2.Items.Add("Delete Group", null, DeleteWordGroup);
                ti = this.contextMenuStrip2.Items.Add("Abridge", null, AbridgeGroup);
                this.contextMenuStrip2.Items.Add(new ToolStripSeparator());
                ti = this.contextMenuStrip2.Items.Add("Change to Apposition", null, ChangeWordGroupTag);
                ti.Tag = "Apposition";
                ti = this.contextMenuStrip2.Items.Add("Change to Parallel", null, ChangeWordGroupTag);
                ti.Tag = "Parallel";
                this.contextMenuStrip2.Items.Add(new ToolStripSeparator());
                this.contextMenuStrip2.Items.Add("Edit Comment...", null, EditComment);
                this.contextMenuStrip2.Items.Add(new ToolStripSeparator());
                this.contextMenuStrip2.Items.Add("Edit this part", null, StartPartEdit);
            }
            else
            {
                this.contextMenuStrip2.Items.Add("Delete Segment", null, DeleteSegment);
                this.contextMenuStrip2.Items.Add("Modify Segment Tag", null, ModifySegmentTag);
                this.contextMenuStrip2.Items.Add("Abridge", null, AbridgeSegment);
                this.contextMenuStrip2.Items.Add(new ToolStripSeparator());
                this.contextMenuStrip2.Items.Add("Edit Comment...", null, EditComment);
                this.contextMenuStrip2.Items.Add(new ToolStripSeparator());
                this.contextMenuStrip2.Items.Add("Edit this part", null, StartPartEdit);
            }
        }

        public void EditSettings(Point location)
        {
            DepEditControlSettingDialog dlg = new DepEditControlSettingDialog();
            DepEditSettings oldSetting = new DepEditSettings(DepEditSettings.Current);
            dlg.View = this;
            dlg.Location = new Point(location.X, Math.Max(0, location.Y - dlg.Height));
            if (dlg.ShowDialog() != DialogResult.OK)
            { // Revert
                DepEditSettings.Current = oldSetting;
            }
            RecalcLayout();
            Refresh();
        }

        // コンテクストメニュー → Delete Link
        private void deleteLinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelection();
        }

        // コンテクストメニュー → Modify Link Tag
        private void modifyLinkTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_SelArrow == null)
            {
                return;
            }

            try
            {
                m_SelArrow.TagLabel.DoEditTaglabel(this, this.modifyLinkTagToolStripMenuItem.Bounds.Location);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            UpdateContents();
        }

        /// <summary>
        /// 矢印ピック→コンテクストメニュー→Abridge by Link
        /// 矢印のかかる範囲のBoxをまとめて表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void abridgeToolStripMenuItem_Clicked(object sender, System.EventArgs e)
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
            int fromChar = m_BunsetsuBoxes[from + 1].Model.StartChar;
            int toChar = m_BunsetsuBoxes[to - 1].Model.EndChar;
            m_AbridgedRanges.Add(new AbridgedRange(fromChar, toChar));
            UpdateContents();
        }

        //private void abridgeButton_Clicked(object sender, EventArgs e)
        //{
        //    if (!(sender is AbridgeButton)) return;
        //    AbridgeButton ab = (AbridgeButton)sender;
        //    int index = ab.AbridgedIndex;
        //    m_AbridgedRanges.RemoveAt(index);
        //    UpdateContents();
        //}

        void HandleAbridgedRangePanelClick(object sender, EventArgs e)
        {
            if (!(sender is AbridgedRangePanel))
            {
                return;
            }
            int i = ((AbridgedRangePanel)sender).Index;
            m_AbridgedRanges.RemoveAt(i);
            UpdateContents();
        }

        private void SentenceStructure_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_DragType == DADragType.ArrowEnd && m_DragArrow != null)
            {
                // マウスが左右境界を越えたらスクロールを行う。
                AutoScrollWhileDrag(e.Location);

                // 矢印線の描画更新
                var rect = new Rectangle(
                    (int)m_DragArrow.Bounds.Location.X, (int)m_DragArrow.Bounds.Location.Y,
                    (int)m_DragArrow.Bounds.Size.Width, (int)m_DragArrow.Bounds.Size.Height);
                this.Invalidate(rect);
                Point newpnt = new Point(e.Location.X - this.DisplayRectangle.Left, e.Location.Y - this.DisplayRectangle.Top);
                m_DragArrow.MoveHeadTo(newpnt);
                this.Invalidate(rect);

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
                    m_CoveredWordBoxes.ForEach(new Action<WordBox>(delegate (WordBox wb) { wb.Hover = false; }));
                }

                using (Graphics g = this.CreateGraphics())
                {
                    Rectangle r = RectangleToClient(m_SelRect);

                    // ラバーバンドの描き直し
                    m_CoveredWordBoxes = null;
                    this.Invalidate(new Rectangle(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2));
                    this.Update();
                    Point sp = m_SelRectstart;
                    Point ep = PointToScreen(e.Location);
                    m_SelRect = new Rectangle(Math.Min(sp.X, ep.X), Math.Min(sp.Y, ep.Y), Math.Abs(ep.X - sp.X), Math.Abs(ep.Y - sp.Y));
                    // ドラッグ矩形のかかるWordBoxを求める.
                    g.DrawRectangle(Pens.DarkGray, RectangleToClient(m_SelRect));
                    m_CoveredWordBoxes = HitTestWordBox(m_SelRect);
                    if (m_CoveredWordBoxes != null && m_CoveredWordBoxes.Count > 0)
                    {
                        m_CoveredWordBoxes.ForEach(new Action<WordBox>(delegate (WordBox wb) { wb.Hover = true; }));
                    }
                    Application.DoEvents();
                }
            }
            else
            {
                // Drag時以外のMouseMoveイベントハンドラ
                // Segment/Link/Group関係：Attributeパネル更新と部分編集ボタンのハイライト
                SegmentBox sbHit;
                SegmentBoxHitTestResult res = HitTestSegmentBox(e.Location, out sbHit);
                if (m_Selections.Count == 0)
                {
                    // 選択中は、MouseMoveのみではAttributePanelを更新しない.
                    if (sbHit != null && sbHit.Model != null)
                    {
                        // Attribute Panelの内容を更新
                        if (sbHit.Parent != null)
                        {
                            // Segment & Group
                            UpdateAttributePanel(m_Service.GetCorpus(), new object[] { sbHit.Model, sbHit.Parent.Model });
                        }
                        else
                        {
                            // Segment only
                            UpdateAttributePanel(m_Service.GetCorpus(), sbHit.Model);
                        }
                        bbox_UpdateGuidePanel((sbHit.Model as Segment)?.Lex);
                    }
                    else
                    {
                        DAHitType dummy;
                        DepArrow arrow = HitTestDepArrow(e.Location, out dummy);
                        if (arrow != null && arrow.Link != null)
                        {
                            UpdateAttributePanel(m_Service.GetCorpus(), arrow.Link);
                            bbox_UpdateGuidePanel(arrow.Link.Lex);
                        }
                    }
                }
                foreach (SegmentBox sb in m_SegmentBoxes)
                {
                    // 部分編集ボタンのハイライトOn/Off
                    sb.HilightPartEditButton(this, (res == SegmentBoxHitTestResult.HitLayerButton && sb == sbHit));
                }
            }
        }

        private void AutoScrollWhileDrag(Point p)
        {
            var parent = this.Parent as ScrollableControl;
            var pp = parent.PointToClient(this.PointToScreen(p));
            var r0 = RectangleToClient(m_SelRect);
            var p0 = PointToClient(m_SelRectstart);
            var shoudScrollHorizontal = false;
            var shouldScrollVertical = false;
            if (pp.X > parent.Width)
            {
                shoudScrollHorizontal = true;
                var dx = pp.X - parent.Width;
                parent.HorizontalScroll.Value = Math.Min(parent.HorizontalScroll.Value + dx, parent.HorizontalScroll.Maximum);
            }
            else if (pp.X < 0)
            {
                shoudScrollHorizontal = true;
                var dx = pp.X;
                parent.HorizontalScroll.Value = Math.Max(parent.HorizontalScroll.Value + dx, parent.HorizontalScroll.Minimum);
            }
            if (this.m_DispMode == DispModes.Diagonal && pp.Y > parent.Height)
            {
                shouldScrollVertical = true;
                var dy = pp.Y - parent.Height;
                parent.VerticalScroll.Value = Math.Min(parent.VerticalScroll.Value + dy, parent.VerticalScroll.Maximum);
            }
            else if (this.m_DispMode == DispModes.Diagonal && pp.Y < 0)
            {
                shouldScrollVertical = true;
                var dy = pp.Y;
                parent.VerticalScroll.Value = Math.Max(parent.VerticalScroll.Value + dy, parent.VerticalScroll.Minimum);
            }
            m_SelRect = RectangleToScreen(r0);
            m_SelRectstart = PointToScreen(p0);
            if (shoudScrollHorizontal || shouldScrollVertical)
            {
                m_PseudoMouseEventTimer.Enabled = true;
            }
        }

        private void OnPseudoMouseMove(object sender, EventArgs e)
        {
            AutoScrollWhileDrag(this.PointToClient(MousePosition));
            m_PseudoMouseEventTimer.Enabled = false;
        }

        private void SentenceStructure_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!this.EditMode)
                {
                    return;
                }
                if (m_OnBunsetsuBox != null)
                {
                    m_OnBunsetsuBox.Hover = false;
                }
                if (m_DragType == DADragType.ArrowEnd && m_DragArrow != null)
                {
                    this.Capture = false;
                    // 後でUpdateContentsした後にスクロール位置を戻すため、現在のスクロール位置を記憶する
                    Point offset0 = this.DisplayRectangle.Location;

                    // 終点が文節Box内・SegmentBox内かどうか判定
                    BunsetsuBox pb = HitTestBunsetsuBox(e.Location);
                    SegmentBox sb;
                    var sbtest = HitTestSegmentBox(e.Location, out sb);
                    Link link = m_DragArrow.Link;
                    var oldCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        if (pb != null && pb.Model != link.From)
                        {
                            // 係り受けを文節へ移動
                            m_Service.ChangeLinkEnd(link, link.To, pb.Model);
                            UpdateContents();
                        }
                        else if (sbtest == SegmentBoxHitTestResult.HitSegment && sb != null
                            && link.From.Tag.Name != "Bunsetsu")
                        {
                            var toseg = sb.Model as Segment;
                            if (!m_Links2.Contains(link))
                            {
                                m_LinkToCreate = link;
                                m_LinkDropTarget = toseg;
                                // Link生成用のPopup Menuを動的に作成して表示する.
                                SetupContextMenuForLink(this.linkContextMenuStrip);
                                this.linkContextMenuStrip.Closed += linkContextMenuStrip_Closed;
                                this.linkContextMenuStrip.Show(PointToScreen(e.Location));
                                return;
                            }
                            else
                            {
                                // 一般のLinkをSegmentへ移動
                                if (link.To.ID != toseg.ID)
                                {
                                    m_Service.ChangeLinkEnd(link, link.To, toseg);
                                }
                            }
                            UpdateContents();
                        }
                        else
                        {
                            // 元に戻す
                            m_DragArrow.MoveHeadTo(m_DragArrowOrgPos);
                        }
                    }
                    catch (Exception ex)
                    {
                        Cursor.Current = oldCursor;
                        ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                        dlg.ShowDialog();
                        m_Service.ResetTransaction();
                    }
                    finally
                    {
                        Cursor.Current = oldCursor;
                    }
                    SetDisplayRectLocation(offset0.X, offset0.Y);
                    AdjustFormScrollbars(true);
                    m_DragType = DADragType.None;
                    m_DragArrow = null;
                    this.CreateLinkMode = false;
                    Refresh();
                    return;
                }
                else if (m_SelRectDragging)
                {
                    this.Capture = false;
                    // 矩形範囲内にあるWordBoxを得る。
                    m_CoveredWordBoxes = HitTestWordBox(m_SelRect);
                    if (m_CoveredWordBoxes.Count > 0)
                    {
                        // Segment, Group生成用のPopup Menuを動的に作成して表示する.
                        SetupContextMenuForSegmentGroup(this.groupingContextMenuStrip);
                        this.groupingContextMenuStrip.Show(PointToScreen(e.Location));
                        this.groupingContextMenuStrip.Closed += new ToolStripDropDownClosedEventHandler(groupingContextMenuStrip_Closed);
                    }
                    Refresh();
                }
                m_SelRectDragging = false;

                // 選択がなければ、Attribute Panelの内容をSentenceとDocumentのAttributeで更新
                if (m_Selections.Count == 0 && m_Service != null)
                {
                    UpdateAttributePanel(m_Service.GetCorpus(), this.Model);
                }
            }
        }

        private void SetupContextMenuForSegmentGroup(ContextMenuStrip m)
        {
            m.Items.Clear();
            ToolStripItem ti;
            // Segments
            foreach (KeyValuePair<string, TagSettingItem> item in TagSetting.Instance.Segment)
            {
                if (item.Value.ShowInSelectorMenu && item.Key != "Bunsetsu")
                {
                    var label = $"Create '{item.Key}' Segment";
                    if (item.Value.ShortcutKey != '\0')
                    {
                        label += $" (&{item.Value.ShortcutKey})";
                    }
                    ti = m.Items.Add(label, null, CreateSegment);
                    ti.Tag = item.Key;
                }
            }
            Popup popup = TagSelector.PreparedPopups[ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT];
            ((TagSelector)popup.Content).TagSelected += new EventHandler(CreateSegmentHandler);
            ti = new ToolStripDropDownButton("Create Segment") { DropDown = popup };
            m.Items.Add(ti);
            //
            m.Items.Add(new ToolStripSeparator());
            // Groups
            foreach (KeyValuePair<string, TagSettingItem> item in TagSetting.Instance.Group)
            {
                if (item.Value.ShowInSelectorMenu)
                {
                    ti = m.Items.Add(string.Format("Create '{0}' Group", item.Key), null, CreateNewWordGroup);
                    ti.Tag = item.Key;
                }
            }
            popup = TagSelector.PreparedPopups[ChaKi.Entity.Corpora.Annotations.Tag.GROUP];
            ((TagSelector)popup.Content).TagSelected += new EventHandler(CreateGroupHandler);
            ti = new ToolStripDropDownButton("Create Group") { DropDown = popup };
            m.Items.Add(ti);
            //
            m.Items.Add(new ToolStripSeparator());
            // Groupへの追加
            int maxGroupIndex = -1;
            m_SegmentBoxes.ForEach(new Action<SegmentBox>(
                    i =>
                    { if (i.Parent != null) maxGroupIndex = Math.Max(maxGroupIndex, i.Parent.Index); }));
            for (int i = 0; i <= maxGroupIndex; i++)
            {
                m.Items.Add(string.Format("Add to Group-{0}", i + 1), null, AddToWordGroup);
            }
        }

        // ContextMenuを閉じたときに、MenuハンドラメソッドのEventHandler登録を解除する
        void groupingContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            foreach (ToolStripItem item in this.groupingContextMenuStrip.Items)
            {
                if (item is ToolStripDropDownButton)
                {
                    Popup p = ((ToolStripDropDownButton)item).DropDown as Popup;
                    if (p != null)
                    {
                        TagSelector selector = (p.Content) as TagSelector;
                        if (selector.TagType == ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT)
                        {
                            selector.TagSelected -= CreateSegmentHandler;
                        }
                        else if (selector.TagType == ChaKi.Entity.Corpora.Annotations.Tag.GROUP)
                        {
                            selector.TagSelected -= CreateGroupHandler;
                        }
                    }
                }
            }
            this.groupingContextMenuStrip.Closed -= groupingContextMenuStrip_Closed;
        }

        private void CreateSegmentHandler(object sender, EventArgs e)
        {
            TagSelector selector = sender as TagSelector;
            if (selector == null) return;
            if (m_CoveredWordBoxes == null) return;

            if (selector.Selection.Name == "Bunsetsu")
            {
                MessageBox.Show("Prohibited operation.");
                return;
            }
            CharRange range = GetCoveringRange(m_CoveredWordBoxes);
            try
            {
                m_Service.CreateSegment(range, selector.Selection.Name);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
        }

        private void CreateGroupHandler(object sender, EventArgs e)
        {
            TagSelector selector = sender as TagSelector;
            if (selector == null) return;
            if (m_CoveredWordBoxes == null) return;

            CharRange range = GetCoveringRange(m_CoveredWordBoxes);
            try
            {
                m_Service.CreateWordGroup(range, selector.Selection.Name);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
        }

        private void CreateSegment(object sender, EventArgs args)
        {
            ToolStripItem mi = sender as ToolStripItem;
            CharRange range = GetCoveringRange(m_CoveredWordBoxes);
            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                m_Service.CreateSegment(range, (string)mi.Tag);
                UpdateContents();
            }
            catch (Exception ex)
            {
                Cursor.Current = oldCursor;
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void CreateNewWordGroup(object sender, EventArgs args)
        {
            if (m_CoveredWordBoxes == null) return;
            ToolStripItem mi = sender as ToolStripItem;
            CharRange range = GetCoveringRange(m_CoveredWordBoxes);
            try
            {
                m_Service.CreateWordGroup(range, (string)mi.Tag);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
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
                m_SegmentBoxes.Find(new Predicate<SegmentBox>(delegate (SegmentBox i)
                    { if (i.Parent != null && i.Parent.Index == index) { grp = i.Parent.Model; return true; } else return false; }));
                m_Service.AddItemToWordGroup(grp, range);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
        }

        private void SetupContextMenuForLink(ContextMenuStrip m)
        {
            m.Items.Clear();
            ToolStripItem ti;
            // Segments
            foreach (KeyValuePair<string, TagSettingItem> item in TagSetting.Instance.Link)
            {
                if (item.Value.ShowInSelectorMenu)
                {
                    var label = $"Create '{item.Key}' Link";
                    if (item.Value.ShortcutKey != '\0')
                    {
                        label += $" (&{item.Value.ShortcutKey})";
                    }
                    ti = m.Items.Add(label, null, CreateLink);
                    ti.Tag = item.Key;
                }
            }
            var popup = TagSelector.PreparedPopups[ChaKi.Entity.Corpora.Annotations.Tag.LINK];
            ((TagSelector)popup.Content).TagSelected += CreateLinkHandler;
            ti = new ToolStripDropDownButton("Create Link") { DropDown = popup };
            m.Items.Add(ti);
        }

        // ContextMenuを閉じたときに、MenuハンドラメソッドのEventHandler登録を解除する
        void linkContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            foreach (ToolStripItem item in this.groupingContextMenuStrip.Items)
            {
                if (item is ToolStripDropDownButton)
                {
                    var p = ((ToolStripDropDownButton)item).DropDown as Popup;
                    if (p != null)
                    {
                        var selector = (p.Content) as TagSelector;
                        if (selector.TagType == ChaKi.Entity.Corpora.Annotations.Tag.LINK)
                        {
                            selector.TagSelected -= CreateLinkHandler;
                        }
                    }
                }
            }
            this.linkContextMenuStrip.Closed -= linkContextMenuStrip_Closed;

            m_DragType = DADragType.None;
            m_DragArrow = null;
            this.CreateLinkMode = false;
            UpdateContents();
        }

        private void CreateLink(object sender, EventArgs args)
        {
            var mi = sender as ToolStripItem;
            var oldCursor = Cursor.Current;

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                m_Service.CreateLink(m_LinkToCreate.From, m_LinkDropTarget, (string)mi.Tag);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            finally
            {
                m_LinkToCreate = null;
                m_LinkDropTarget = null;
                Cursor.Current = oldCursor;
            }
        }

        private void CreateLinkHandler(object sender, EventArgs e)
        {
            var selector = sender as TagSelector;
            if (selector == null) return;
            if (m_LinkToCreate == null || m_LinkDropTarget == null) return;

            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                m_Service.CreateLink(m_LinkToCreate.From, m_LinkDropTarget, selector.Selection.Name);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            finally
            {
                m_LinkToCreate = null;
                m_LinkDropTarget = null;
                Cursor.Current = oldCursor;
            }
        }


        /// <summary>
        /// Group選択→Delete Groupメニュー選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteWordGroup(object sender, System.EventArgs e)
        {
            DeleteSelection();
        }

        private void DeleteSelection()
        {
            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (m_SelArrow != null && !m_SelArrow.IsDependencyLink)
                {
                    // Delete Link
                    m_Service.DeleteLink(m_SelArrow.Link);
                    m_SelArrow = null;
                }
                else
                {
                    // Delete Segment (with Group)
                    foreach (var sel in m_Selections)
                    {
                        var sbox = sel as SegmentBox;
                        if (sbox != null)
                        {
                            Segment seg = (Segment)sbox.Model;
                            Group grp = (sbox.Parent != null) ? sbox.Parent.Model : null;
                            if (m_Service.HasInOutLink(seg)) //todo: Segment Delete時にLinkも削除すればこの判定は不要.
                            {
                                MessageBox.Show("Segment has link(s). Remove them first.");
                                UpdateContents();
                                return;
                                //var msg = string.Format("Segment[{0}] has Link(s). Remove them too?",
                                //    seg.Sentence.GetTextInRange(seg.StartChar, seg.EndChar, true));
                                //if (MessageBox.Show(msg, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                //{
                                //    if (grp != null)
                                //    {
                                //        m_Service.RemoveItemFromWordGroupWithInOutLinks(grp, seg);
                                //    }
                                //    else
                                //    {
                                //        m_Service.DeleteSegmentWithInOutLinks(seg);
                                //    }
                                //}
                                //else
                                //{
                                //    MessageBox.Show("Cannot remove the segment.");
                                //    return;
                                //}
                            }
                            else
                            {
                                if (grp != null)
                                {
                                    m_Service.RemoveItemFromWordGroup(grp, seg);
                                }
                                else
                                {
                                    m_Service.DeleteSegment(seg);
                                }
                            }
                        }
                    }
                }
                UpdateContents();
            }
            catch (Exception ex)
            {
                Cursor.Current = oldCursor;
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            finally
            {
                Cursor.Current = oldCursor;
                ClearSelection();
            }
        }

        private void ChangeWordGroupTag(object sender, System.EventArgs e)
        {
            try
            {
                SegmentBox sbox;
                if (TryGetSingleSelectionAsSegmentBox(out sbox))
                {
                    Segment seg = (Segment)sbox.Model;
                    if (sbox.Parent != null)
                    {
                        Group grp = sbox.Parent.Model;
                        string oldTag = grp.Tag.Name;
                        string newTag = (string)((ToolStripItem)sender).Tag;
                        if (oldTag != newTag)
                        {
                            m_Service.ChangeWordGroupTag(grp, oldTag, newTag);
                            UpdateContents();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
        }

        private void EditComment(object sender, System.EventArgs e)
        {
            if (m_Selections.Count != 1)
            {
                return;
            }
            try
            {
                Annotation ann = ann = m_Selections[0].Model;
                //TODO: 今のところはGroupへのコメント付加は行わない
                // GroupとSegmentを明示的に選択分けできるようになれば追加する.
#if false
                if (m_SelWordGroupItem.Parent != null)
                {
                    ann = m_SelWordGroupItem.Parent.Model;
                }
#endif

                using (EditCommentDialog dlg = new EditCommentDialog(ann.Comment))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        m_Service.ChangeComment(ann, dlg.Comment);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
        }

        /// <summary>
        /// Group選択→Delete Segmentメニュー選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteSegment(object sender, EventArgs e)
        {
            DeleteSelection();
        }

        /// <summary>
        /// Group選択→Abridgeメニュー選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AbridgeGroup(object sender, EventArgs e)
        {
            SegmentBox sbox;
            if (!TryGetSingleSelectionAsSegmentBox(out sbox))
            {
                return;
            }
            AbridgedRange range = AbridgedRange.Empty;
            IList<SegmentBox> items = FindSiblingGroupItems(sbox);
            foreach (SegmentBox sb in items)
            {
                var seg = (Segment)sb.Model;
                range.Union(seg.StartChar, seg.EndChar);
            }
            if (!range.IsEmpty)
            {
                m_AbridgedRanges.Add(range);
            }
            UpdateContents();
        }

        /// <summary>
        /// Segment選択→Abridgeメニュー選択時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AbridgeSegment(object sender, EventArgs e)
        {
            SegmentBox sbox;
            if (!TryGetSingleSelectionAsSegmentBox(out sbox))
            {
                return;
            }
            Segment seg = (Segment)sbox.Model;

            //TODO: BunsetsuBoxListの係り受けが、Abridge対象として適格かどうかを調べる.

            m_AbridgedRanges.Add(new AbridgedRange(seg.StartChar, seg.EndChar));

            UpdateContents();
        }

        private void ModifySegmentTag(object sender, EventArgs e)
        {
            if (!EditMode)
            {
                return;
            }
            SegmentBox sbox;
            if (!TryGetSingleSelectionAsSegmentBox(out sbox))
            {
                return;
            }

            sbox.DoEditTaglabel(this, this.modifyLinkTagToolStripMenuItem.Bounds.Location);
            sbox.TagChanged += Sbox_TagChanged;
        }

        private void Sbox_TagChanged(object sender, TagChangedEventArgs e)
        {
            var sbox = sender as SegmentBox;
            if (sbox == null) return;
            sbox.TagChanged -= Sbox_TagChanged;

            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var seg = (Segment)sbox.Model;
                string curtag = seg.Tag.Name;
                string newtag = e.Text;
                m_Service.ChangeSegmentTag(seg, curtag, newtag);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        /// <summary>
        /// WordBox Click → Lexeme Selection Popup表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bbox_LexemeEditRequested(object sender, EventArgs e)
        {
            if (!this.EditMode) return;

            if (!(sender is WordBox)) return;
            WordBox wb = (WordBox)sender;

            try
            {
                var words = this.Model.GetWords(this.TargetProjectId);
                m_LexSelector.BeginSelection(wb.Model.Lex.Surface, wb.Model.Lex, words);
                if (m_LexSelector.ShowDialog() == DialogResult.OK)
                {
                    var lex = m_LexSelector.Selection?.Lexeme;
                    var mwe_match = m_LexSelector.Selection?.Match;
                    if (lex != null && mwe_match == null)
                    {
                        try
                        {
                            m_Service.ChangeLexeme(wb.Model.Sen.ParentDoc.ID, wb.Model.Pos, lex);
                            UpdateContents();
                        }
                        catch (Exception ex)
                        {
                            ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                            dlg.ShowDialog();
                            m_Service.ResetTransaction();
                        }
                    }
                    else if (mwe_match != null)
                    {
                        try
                        {
                            mwe_match.CalcCharRangeList(words, this.Model.StartChar);
                            var op = m_Service.CreateMWEAnnotation(mwe_match, true);
                            if (op == null)
                            {
                                MessageBox.Show("Duplicate segment is prohibited!");
                            }
                            else
                            {
                                UpdateContents();
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                            dlg.ShowDialog();
                            m_Service.ResetTransaction();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Show Lexeme Selection Dialog. Please make sure DepEdit is in Edit Mode.", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
        }

        // 語BoxのListから、そのカバーする文字位置範囲を求める
        private CharRange GetCoveringRange(List<WordBox> boxes)
        {
            lock (m_UpdateLock)
            {
                CharRange range = new CharRange();
                foreach (WordBox b in boxes)
                {
                    CharRange r = b.GetCharRange();
                    range.Union(r);
                }
                return range;
            }
        }

        private DepArrow HitTestDepArrow(Point pt, out DAHitType ht)
        {
            lock (m_UpdateLock)
            {
                Graphics g = this.CreateGraphics();
                try
                {
                    foreach (DepArrow arrow in m_Arrows)
                    {
                        ht = arrow.HitTest(g, pt);
                        if (ht != DAHitType.None)
                        {
                            return arrow;
                        }
                    }
                    foreach (DepArrow arrow in m_Arrows2)
                    {
                        ht = arrow.HitTest(g, pt);
                        if (ht != DAHitType.None)
                        {
                            return arrow;
                        }
                    }
                    ht = DAHitType.None;
                    return null;
                }
                finally
                {
                    g.Dispose();
                }
            }
        }

        private BunsetsuBox HitTestBunsetsuBox(Point pt)
        {
            lock (m_UpdateLock)
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
        }

        private List<BunsetsuBox> HitTestBunsetsuBox(Rectangle rect)
        {
            lock (m_UpdateLock)
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
        }

        private List<WordBox> HitTestWordBox(Rectangle rect)
        {
            lock (m_UpdateLock)
            {

                List<WordBox> res = new List<WordBox>();
                foreach (BunsetsuBox bb in m_BunsetsuBoxes)
                {
                    res.AddRange(bb.HitTestWordBox(rect));
                }
                return res;
            }
        }


        private SegmentBoxHitTestResult HitTestSegmentBox(Point p, out SegmentBox hitAt)
        {
            lock (m_UpdateLock)
            {
                foreach (SegmentBox item in m_SegmentBoxes)
                {
                    SegmentBoxHitTestResult res = item.HitTest(p);
                    if (res != SegmentBoxHitTestResult.None)
                    {
                        hitAt = item;
                        item.OnHover(this);
                        return res;
                    }
                    else
                    {
                        item.OnHover(this, false);
                    }
                }
                hitAt = null;
                return SegmentBoxHitTestResult.None;
            }
        }

        private DepArrow PickDepArrow(Point pt, out DAHitType ht)
        {
            lock (m_UpdateLock)
            {
                DepArrow arrow = HitTestDepArrow(pt, out ht);
                if (arrow != null)
                {
                    m_SelArrow = arrow;
                    return arrow;
                }
                return null;
            }
        }

        private void UnpickDepArrow()
        {
            lock (m_UpdateLock)
            {
                foreach (DepArrow arrow in m_Arrows)
                {
                    arrow.Selected = false;
                }
                m_SelArrow = null;
            }
        }

        public void Undo()
        {
            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (m_Service.Undo())
                {
                    UpdateContents();
                }
            }
            catch (Exception ex)
            {
                Cursor.Current = oldCursor;
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        public void Redo()
        {
            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (m_Service.Redo())
                {
                    UpdateContents();
                }
            }
            catch (Exception ex)
            {
                Cursor.Current = oldCursor;
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
                m_Service.ResetTransaction();
            }
            finally
            {
                Cursor.Current = oldCursor;
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
                UpdateGuidePanel(m_Service.GetCorpus(), lex);
            }
        }


        void contextMenuStrip2_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            Refresh();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.S && (keyData & Keys.Modifiers) == Keys.Control)
            {
                if (CanSave())
                {
                    if (SaveRequested != null)
                    {
                        SaveRequested(this, EventArgs.Empty);
                        return true;
                    }
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Right)
            {
                Console.WriteLine("Right");
                if ((keyData & Keys.Modifiers) == Keys.Shift)
                {
                    Console.WriteLine("+Shift");
                    m_SelRange.Start++;
                }
                m_SelRange.End++;
                return true;
            }
            if ((keyData & Keys.KeyCode) == Keys.Left)
            {
                Console.WriteLine("Left");
                m_SelRange.Start = Math.Max(0, m_SelRange.Start - 1);
                if ((keyData & Keys.Modifiers) == Keys.Shift)
                {
                    Console.WriteLine("+Shift");
                    m_SelRange.End = Math.Max(0, m_SelRange.End - 1);
                }
                return true;
            }
            if ((keyData & Keys.KeyCode) == Keys.Z && (keyData & Keys.Modifiers) == Keys.Control)
            {
                Undo();
                return true;
            }
            if ((keyData & Keys.KeyCode) == Keys.Y && (keyData & Keys.Modifiers) == Keys.Control)
            {
                Redo();
                return true;
            }
            if ((keyData & Keys.KeyCode) == Keys.Delete)
            {
                DeleteSelection();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 部分編集モードに入る
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartPartEdit(object sender, System.EventArgs e)
        {
            try
            {
                SegmentBox sbox;
                if (!TryGetSingleSelectionAsSegmentBox(out sbox))
                {
                    return;
                }
                m_VisibleLevels.Push(sbox.Range);
                UpdateContents();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
        }

        /// <summary>
        /// 部分編集モードから出て全体を表示する.
        /// </summary>
        public void EndPartEdit()
        {
            if (m_VisibleLevels.Count == 0) return;
            m_VisibleLevels.Pop();
            UpdateContents();
        }

        public bool IsPartEditing
        {
            get { return m_VisibleLevels.Count > 0; }
        }

        private void ReturnToParentButton_Click(object sender, EventArgs e)
        {
            EndPartEdit();
        }

        private void dependencyEditSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var p = new Point(this.contextMenuStrip3.Left, this.contextMenuStrip3.Top);
            EditSettings(p);
        }

        public bool CanAutoDetectMWE()
        {
            return (this.EditMode && this.Model != null);
        }

        public void UploadMWE()
        {
            if (!CanAutoDetectMWE())
            {
                return;
            }
            try
            {
                var words = this.Model.GetWords(this.TargetProjectId);
                m_MWEUploadSelector.Words = words;
                if (m_MWEUploadSelector.ShowDialog() == DialogResult.OK)
                {
                    foreach (var result in m_MWEUploadSelector.Results)
                    {
                        m_Service.RegisterMWEToDictionary(result.Item1);
                    }
                    UpdateContents();
                }
            }
            catch (Exception ex)
            {
                var edlg = new ErrorReportDialog("Command failed.", ex);
                edlg.ShowDialog();
            }

        }

        private List<IOperation> m_MweOps = new List<IOperation>();

        public void DownloadMWE()
        {
            if (!CanAutoDetectMWE())
            {
                return;
            }
            try
            {
                m_MweOps.Clear();
                var words = this.Model.GetWords(this.TargetProjectId);
                m_MWEDownloadSelector.Words = words;
                m_MWEDownloadSelector.OnApplyMweMatches += OnApplyMWEMatches;
                m_MWEDownloadSelector.ShowDialog();
            }
            catch (Exception ex)
            {
                var edlg = new ErrorReportDialog("Command failed.", ex);
                edlg.ShowDialog();
            }
            finally
            {
                m_MWEDownloadSelector.OnApplyMweMatches -= OnApplyMWEMatches;
            }
            try
            {
                // 現時点のLocal Execution HistoryをPushする.
                m_Service.PushHistories(m_MweOps);
            }
            catch (Exception ex)
            {
                var edlg = new ErrorReportDialog("Command failed.", ex);
                edlg.ShowDialog();
            }
        }

        // MWEアノテーションを一旦元に戻し、onされたMWEを順に生成しなおす.
        // 処理結果に一致したcheck状態を返す.
        private bool[] OnApplyMWEMatches(MatchingResult[] matches, bool[] onoffs)
        {
            var words = m_MWEDownloadSelector.Words;
            var oldCur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            var checkStates = new bool[matches.Length]; // すべてデフォルトではunchecked状態
            try
            {
                // チェックが変更されるたびにRollbackする
                m_Service.Unexecute(m_MweOps);
                m_MweOps.Clear();
                for (var i = 0; i < matches.Length; i++)
                {
                    var match = matches[i];
                    var onoff = onoffs[i];
                    if (onoff)
                    {
                        // Word番号リストから文字位置範囲リストへ変換
                        match.CalcCharRangeList(words, this.Model.StartChar);
                        var op = m_Service.CreateMWEAnnotation(match, false);
                        if (op != null)
                        {
                            m_MweOps.Add(op);
                            checkStates[i] = true; // Executeが成功したので、Check状態とする.
                        }
                        else
                        {
                            MessageBox.Show("Duplicate segment is prohibited!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_MweOps.Clear();
                var edlg = new ErrorReportDialog("Command failed.", ex);
                edlg.ShowDialog();
            }
            finally
            {
                try
                {
                    UpdateContents();
                }
                catch (Exception ex)
                {
                    var edlg = new ErrorReportDialog("Command failed.", ex);
                    edlg.ShowDialog();
                }
                Cursor.Current = oldCur;
            }
            return checkStates;
        }

        public event EventHandler CreateLinkModeChanged;

        public bool CreateLinkMode
        {
            get { return m_CreateLinkMode; }
            set
            {
                m_CreateLinkMode = value;
                if (this.CreateLinkModeChanged != null)
                {
                    this.CreateLinkModeChanged(this, EventArgs.Empty);
                }
            }
        }

        private void SentenceStructure_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.Delta > 0)
                {
                    Zoom();
                }
                else if (e.Delta < 0)
                {
                    Pan();
                }
            }
        }
    }
}
