using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Extended;
using System.Threading;
using System.Windows.Forms;
using ChaKi.GUICommon;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Options;
using ChaKi.Common;
using ChaKi.Common.Settings;
using System.Text;
using Iesi.Collections.Generic;

namespace ChaKi.Views.KwicView
{
    /// <summary>
    /// 単一のKwicListに結び付けられた、Kwic/Text Viewerパネル
    /// </summary>
    public partial class KwicViewPanel : UserControl
    {
        #region properties
        public bool WordWrap { get; set; }
        public bool KwicMode { get; set; }
        public bool TwoLineMode { get; set; }

        public int EffectiveWidth
        {
            get { return this.Width - this.vScrollBar1.Width; }
        }

        /// <summary>
        /// 現在表示されている最初の行番号
        /// </summary>
        public int DrawLinesFrom { get; set; }

        /// <summary>
        /// 現在表示されている最後の行番号
        /// </summary>
        public int DrawLinesTo { get; set; }

        public bool SuspendUpdateView { get; set; }


        /// <summary>
        /// Tagを持たない行が存在するか (= SimpleSearchの結果が含まれているか)
        /// </summary>
        public bool HasSimpleItem
        {
            get
            {
                if (m_Model == null) return false;
                foreach (KwicItem ki in m_Model.KwicList.Records)
                {
                    if (ki.IsSimple)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion

        #region Private members
        private SearchHistory m_Model;
        private long m_iNotify;  // 1ならModel Updateが必要, 0ならそのUpdate処理が既に行われていることを示す.
        private int m_recordsUpdated;

        private List<LineElement> m_LineElements;
        private Font[] m_Fonts;
        private Brush m_NormalBrush;
        private Brush m_HilightBrush;
        private Dictionary<string, Pen> m_SegmentPen;
        public Dictionary<string, Pen> LinkPen;
        private Dictionary<string, Brush> m_GroupBrush;

        public Dictionary<Segment, Rectangle> SegPosCache;  // Segment表示位置のキャッシュ

        private Selection m_CurSelLines;  //選択行番号(m_LineElementsへのインデックス）
        private CharRangeElement m_CurSelCharRange;  // 文字列選択範囲

        private Caret m_Caret;

        private bool m_Dragging;

        private Columns m_Columns;

        private System.Threading.Timer m_Timer;      // 描画更新タイマ
        #endregion

        #region KwicView Events
        public event EventHandler OnPaintDone;
        /// <summary>
        /// Current行が変化したときの親(MainForm)への通知
        /// （子Viewからの中継）
        /// </summary>
        public event CurrentChangedDelegate OnCurrentChanged;
        /// <summary>
        /// Context表示指示時の親(MainForm)への通知 
        /// （子Viewからの中継）
        /// </summary>
        public event RequestContextDelegate OnContextRequested;
        /// <summary>
        /// DepEdit開始の親(mainForm)への通知
        /// （子Viewからの中継）
        /// </summary>
        public event RequestDepEditDelegate OnDepEditRequested;
        /// <summary>
        /// Guide Panelの表示更新通知 
        /// （子Viewからの中継）
        /// </summary>
        public event UpdateGuidePanelDelegate OnUpdateGuidePanel;

        /// <summary>
        /// 選択行が変化したときの親(KwicView)への通知
        /// （複数Viewにおける選択行連動機能用）
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        /// <summary>
        /// Word-wordのハイライトを行いたい時のKwicViewへの通知
        /// </summary>
        public event EventHandler<WordMappingHilightEventArgs> WordMappingHilightRequested;

        #endregion

        public KwicViewPanel()
        {
            InitializeComponent();
            m_Fonts = new Font[WordElementStyle.Count];
            FurnishFonts();
            m_NormalBrush = new SolidBrush(Color.Black);
            m_HilightBrush = new SolidBrush(Color.Red);
            m_GroupBrush = new Dictionary<string, Brush>();
            m_GroupBrush.Add("TestGroup", new SolidBrush(Color.FromArgb(150, Color.Pink)));
            this.BackColor = ColorTranslator.FromHtml(GUISetting.Instance.KwicViewBackground);

            m_LineElements = new List<LineElement>();
            this.SegPosCache = new Dictionary<Segment, Rectangle>();
            this.SuspendUpdateView = false;

            this.DrawLinesFrom = 0;
            this.DrawLinesTo = 0;

            m_recordsUpdated = 0;
            m_CurSelLines = new Selection();
            m_CurSelCharRange = new CharRangeElement();
            m_Model = null;

            m_Columns = Columns.Default;

            m_Caret = new Caret(this.pictureBox1);
            m_Caret.Create(m_Fonts[0].Height);
            m_Caret.Visible = true;

            // 描画更新タイマ
            TimerCallback timerDelegate = new TimerCallback(this.OnTimer);
            m_Timer = new System.Threading.Timer(timerDelegate, null, 1000, 1000);
            this.dummyTextBox1.MouseWheel += HandleMouseWheel;
            this.MouseWheel += HandleMouseWheel;

            FontDictionary.Current.FontChanged += new EventHandler(OnFontChanged);

            this.dummyTextBox1.GotFocus += new EventHandler((e, a) =>
            {
                this.InvokeGotFocus(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// コンテンツを削除する
        /// </summary>
        public void DeleteAll()
        {
            ResetUpdateStatus();
            m_Model = null;
        }

        /// <summary>
        /// KwicItemの印付け関係
        /// </summary>
        public void CheckAll()
        {
            if (m_Model == null) return;
            foreach (KwicItem item in m_Model.KwicList.Records)
            {
                item.Checked = true;
            }
            this.Refresh();
        }

        public void CheckSelected()
        {
            if (m_Model == null) return;
            for (int i = 0; i < m_Model.KwicList.Records.Count; i++)
            {
                if (m_CurSelLines.Contains(i))
                {
                    KwicItem item = m_Model.KwicList.Records[i];
                    item.Checked = true;
                }
            }
            this.Refresh();
        }

        public void AlterCheckSelected()
        {
            if (m_Model == null) return;
            for (int i = 0; i < m_Model.KwicList.Records.Count; i++)
            {
                if (m_CurSelLines.Contains(i))
                {
                    KwicItem item = m_Model.KwicList.Records[i];
                    item.Checked = !item.Checked;
                }
            }
            this.Refresh();
        }

        public void UncheckAll()
        {
            if (m_Model == null) return;
            foreach (KwicItem item in m_Model.KwicList.Records)
            {
                item.Checked = false;
            }
            this.Refresh();
        }

        public void SelectChecked()
        {
            if (m_Model == null) return;
            m_CurSelLines.Clear();
            for (int i = 0; i < m_Model.KwicList.Records.Count; i++)
            {
                if (m_Model.KwicList.Records[i].Checked)
                {
                    m_CurSelLines.AddSelection(i);
                }
            }
            this.Refresh();
        }

        public void SelectAll()
        {
            if (m_Model == null) return;
            m_CurSelLines.Clear();
            m_CurSelLines.SetRange(0, m_Model.KwicList.Records.Count - 1);
            this.Refresh();
        }

        public void UnselectAll()
        {
            if (m_Model == null) return;
            m_CurSelLines.Clear();
            this.Refresh();
        }

        /// <summary>
        /// SearchHistoryノード (KwicListを持つ）を元に表示内容を構築する
        /// </summary>
        /// <param name="model"></param>
        public void SetModel(SearchHistory model)
        {
            DeleteAll();
            m_Model = model;
            if (m_Model != null && m_Model.KwicList != null)
            {
                m_Model.KwicList.OnModelChanged += new UpdateKwicListEventHandler(this.OnModelChanged);
                UpdateKwicList();
            }
        }

        public SearchHistory GetModel()
        {
            return m_Model;
        }

        /// <summary>
        /// 内容を強制的にInvalidateする。
        /// </summary>
        public void ResetUpdateStatus()
        {
            m_LineElements.Clear();
            m_recordsUpdated = 0;
            m_CurSelCharRange.Clear();
            m_CurSelLines.Clear();
            LineElement.LastIndex = 0;
            WordElement.LastIndex = 0;
        }

        /// <summary>
        /// フォントから行の高さを再計算し、LineElement, WordElementのstaticフィールドに設定する
        /// </summary>
        public void CalculateLineHeight()
        {
            int lineHeight = m_Fonts[WordElementStyle.Normal].Height + KwicView.Settings.LineMargin;
            if (this.TwoLineMode)
            {
                lineHeight += (m_Fonts[WordElementStyle.Small].Height + KwicView.Settings.LineMargin);
            }
            LineElement.LineHeight = lineHeight;
            WordElement.LineHeight = LineElement.LineHeight - (KwicView.Settings.LineMargin);
        }

        /// <summary>
        /// フォントを初期化する
        /// </summary>
        private void FurnishFonts()
        {
            m_Fonts[WordElementStyle.Normal] = GUISetting.Instance.GetBaseTextFont();
            try
            {
                m_Fonts[WordElementStyle.Small] = new Font(m_Fonts[WordElementStyle.Normal].Name, m_Fonts[WordElementStyle.Normal].Size - 4F);
            }
            catch
            {
                m_Fonts[WordElementStyle.Small] = new Font(FontDictionary.DefaultBaseTextFontName, FontDictionary.DefaultBaseTextFontSize - 4F);
            }
            m_Fonts[WordElementStyle.Header] = GUISetting.Instance.GetBaseAnsiFont();
            try
            {
                m_Fonts[WordElementStyle.Tiny] = new Font(m_Fonts[WordElementStyle.Header].Name, m_Fonts[WordElementStyle.Header].Size - 3F);
            }
            catch
            {
                m_Fonts[WordElementStyle.Tiny] = new Font(FontDictionary.DefaultAnsiTextFontName, FontDictionary.DefaultAnsiTextFontSize - 3F);
            }
            WordElement.FurnishFonts(m_Fonts[WordElementStyle.Normal], m_Fonts[WordElementStyle.Small]);
            LineElement.AnsiFont = m_Fonts[WordElementStyle.Header];
        }

        /// <summary>
        /// m_Model.KwicList.Recordsの順序が変更されたので、
        /// lineElementの順序を合わせる（ソートする）
        /// </summary>
        private void SynchronizeKwicListOrder()
        {
            LineElementComparer comp = new LineElementComparer(m_Model.KwicList.Records);
            m_LineElements.Sort(comp);
            for (int i = 0; i < m_LineElements.Count; i++)
            {
                m_LineElements[i].Index = i;
            }
        }

        /// <summary>
        /// KwicListを元に表示更新を行う。
        /// アノテーションデータはクリアされる。
        /// </summary>
        public void UpdateKwicList()
        {
            if (m_Model == null || m_Model.KwicList == null)
            {
                return;
            }
            if (m_UpdateFrozen)
            {
                return;
            }
            m_Model.AnnotationList.Clear();

            lock (this)
            {
                int lastRecordsUpdated = m_recordsUpdated;
                // Add new LineElements from KwicList starting at 'm_recordsUpdated'
                using (Graphics g = this.pictureBox1.CreateGraphics())
                {
                    // WordBlockのサイズ等を計算する
                    int updateTo = m_Model.KwicList.Records.Count;
                    for (int i = m_recordsUpdated; i < updateTo; i++)
                    {
                        AddKwicItem(m_Model.KwicList.Records[i], g);
                    }
                    m_recordsUpdated = updateTo;
                }
                RecalcLayout();
                // カレント行が既に設定されている場合、
                if (this.SingleSelection >= 0 && this.SingleSelection > lastRecordsUpdated && m_recordsUpdated >= this.SingleSelection)
                {
                    MakeLineVisible(this.SingleSelection);
                }
            }
        }

        private bool m_UpdateFrozen = false;
        public bool UpdateFrozen
        {
            get { return m_UpdateFrozen; }
            set
            {
                if (m_UpdateFrozen != value)
                {
                    m_UpdateFrozen = value;
                    if (!m_UpdateFrozen)
                    {
                        // Freeze解除時にUpdateを行う。
                        UpdateKwicList();
                    }
                }
            }
        }

        /// <summary>
        /// KwicItemから１行を追加する
        /// </summary>
        /// <param name="ki"></param>
        /// <param name="g"></param>
        private void AddKwicItem(KwicItem ki, Graphics g)
        {
            LineElement lineElement = new LineElement();
            lineElement.KwicItem = ki;

            int charPos = ki.StartCharPos;
            lineElement.AddKwicPortion(ki.Left, g, KwicPortionType.Left, this.TwoLineMode, ref charPos);
            lineElement.AddKwicPortion(ki.Center, g, KwicPortionType.Center, this.TwoLineMode, ref charPos);
            lineElement.AddKwicPortion(ki.Right, g, KwicPortionType.Right, this.TwoLineMode, ref charPos);

            m_LineElements.Add(lineElement);
        }

        private void RecalcWordElementBounds()
        {
            using (var g = CreateGraphics())
            {
                foreach (var le in m_LineElements)
                {
                    le.RecalcWordElementBounds(g, this.TwoLineMode);
                }
            }
        }

        /// <summary>
        /// アノテーションを元に表示を更新する
        /// </summary>
        public void UpdateAnnotations()
        {
            if (m_Model == null || m_Model.AnnotationList == null)
            {
                return;
            }
            // 単純なSegment->CharRange算出→要改良
            if (m_Model.KwicList.Records.Count == 0)
            {
                return;
            }
            foreach (LineElement le in m_LineElements)
            {
                le.SegElements.Clear();
                le.LinkElements.Clear();
            }

            Dictionary<Segment, SegElement> segElementTable = new Dictionary<Segment, SegElement>();

            Corpus corpus = m_Model.KwicList.Records[0].Crps;
            foreach (Segment seg in m_Model.AnnotationList.Segments)
            {
                if (seg.StartChar >= seg.EndChar)
                {
                    continue;
                }
                foreach (LineElement le in m_LineElements)
                {
                    if (seg.Doc.ID != le.KwicItem.Document.ID)
                    {
                        continue;
                    }
                    if (le.ContainsRange(seg.StartChar, seg.EndChar))
                    {
                        SegElement se = le.AddSegment(seg);
                        if (se != null && !segElementTable.ContainsKey(seg))
                        {
                            segElementTable.Add(seg, se);
                        }
                    }
                }
            }
            foreach (Link lnk in m_Model.AnnotationList.Links)
            {
                LinkElement lke = new LinkElement();
                lke.Link = lnk;
                SegElement se;
                if (segElementTable.TryGetValue(lnk.From, out se))
                {
                    lke.SegFrom = se;
                }
                if (segElementTable.TryGetValue(lnk.To, out se))
                {
                    lke.SegTo = se;
                }

                foreach (LineElement le in m_LineElements)
                {
                    if (lnk.From.Doc.ID != le.KwicItem.Document.ID || lnk.To.Doc.ID != le.KwicItem.Document.ID)
                    {
                        continue;
                    }
                    if (le.ContainsRange(lnk.From.StartChar, lnk.From.EndChar)
                     || le.ContainsRange(lnk.To.StartChar, lnk.To.EndChar))
                    {
                        le.AddLinkElement(lke);
                    }
                }
            }

            // SegmentのLevelをアサインする
            AssignSegmentLevels();

            KwicView.Settings.ShowSegments = true;
            KwicView.Settings.ShowLinks = true;

            Invalidate(true);
        }

        /// <summary>
        /// Segmentアノテーションを追加する
        /// </summary>
        /// <param name="seg"></param>
        public void AddNewSegment(Segment seg)
        {
            if (seg.StartChar >= seg.EndChar)
            {
                throw new Exception("KwicViewPanel.AddNewSegment: seg.StartChar >= seg.EndChar");
            }
            foreach (LineElement le in m_LineElements)
            {
                if (seg.Doc.ID != le.KwicItem.Document.ID)
                {
                    continue;
                }
                if (le.ContainsRange(seg.StartChar, seg.EndChar))
                {
                    SegElement se = le.AddSegment(seg);
                }
            }

            m_Model.AnnotationList.Segments.Add(seg);
            AssignSegmentLevels();

            Invalidate(true);
        }

        /// <summary>
        /// Segmentのレベル計算（箱が重ならないための位置計算）を行う
        /// </summary>
        private void AssignSegmentLevels()
        {
#if false
            // Segmentsは開始点でソートされていること
            // 先頭から重ならないようにLevelをアサインしていく。
            bool bRemain = true;
            short level = 0;
            while (bRemain) {
                bRemain = false;
                int chpos = -1;
                foreach (Segment seg in m_Model.AnnotationList.Segments)
                {
                    if (seg.Level >= 0)
                    {   // Already assigned
                        continue;
                    }
                    if (seg.Range.Start.CharID > chpos)
                    {   // このsegは重ならない
                        seg.Level = level;
                        chpos = seg.Range.End.CharID;
                    }
                    else
                    {   // このlevelではアサインできないSegmentである
                        bRemain = true;
                    }
                }
                level++;
                if (level > 5)
                {
                    throw new Exception("Too many overlapping segments!");
                }
            }
#endif
        }

        /// <summary>
        /// すべてのElementの位置を再計算する
        /// </summary>
        public void RecalcLayout()
        {
            RecalcWordElementBounds();

            // 描画パラメータをLineElement, WordElementにセットしておく
            WordElement.FurnishFonts(m_Fonts[WordElementStyle.Normal], m_Fonts[WordElementStyle.Small]);
            WordElement.RenderTwoLine = this.TwoLineMode;
            LineElement.AnsiFont = m_Fonts[WordElementStyle.Header];
            LineElement.HeaderHeight = m_Fonts[WordElementStyle.Header].Height;
            LineElement.Cols = m_Columns;
            SegElement.L1Height = m_Fonts[WordElementStyle.Normal].Height;
            SegElement.LineHeight = LineElement.LineHeight + KwicView.Settings.LineMargin;

            // Calculate WordBlock Locations
            Rectangle clientR = this.ClientRectangle;
            int curX = 0;
            int curY = 0;
            int maxWidth = m_Columns.GetMaxSentenceWidth();
            foreach (LineElement le in m_LineElements)
            {
                Rectangle bounds = new Rectangle(0, curY, clientR.Width, curY);

                int linePos = 0;    // LineElement内での行位置
                for (int i = 0; i < le.WordElements.Count; i++)
                {
                    WordElement we = le.WordElements[i];
                    if (this.WordWrap)
                    {
                        if (curX + we.Bounds.Width > maxWidth)
                        {
                            curY += (LineElement.LineHeight + KwicView.Settings.LineMargin);
                            linePos++;
                            curX = 0;
                        }
                    }
                    if (le.CenterIndexOffset == 0 && we.IsCenter)
                    {
                        le.CenterIndexOffset = i;
                    }
                    we.LinePos = linePos;
                    we.Bounds.Location = new Point(curX - bounds.X, curY - bounds.Y);
                    curX += (we.Bounds.Width + KwicView.Settings.WordMargin);
                }
                le.LinePosMax = linePos;
                // end of sentence
                curY += (LineElement.LineHeight + KwicView.Settings.LineMargin);
                curX = 0;
                // KWIC Mode --> 行全体を中心語を軸にオフセット
                if (this.KwicMode)
                {
                    Rectangle clientr = pictureBox1.ClientRectangle;
                    int xcenter = m_Columns.GetLeftWidth(); // Centerカラムの開始位置
                    if (le.WordElements.Count > 0)
                    {
                        WordElement wbcen = le.WordElements[le.CenterIndexOffset];
                        // Center wordまでの座標オフセット値
                        int xoffset = wbcen.Bounds.Left - xcenter;
                        foreach (WordElement wb in le.WordElements)
                        {
                            wb.Bounds.Offset(-xoffset, 0);
                        }
                    }
                }
                else
                {
                    foreach (WordElement wb in le.WordElements)
                    {
                        wb.IsCenter = false;
                    }
                }
                bounds.Height = curY - bounds.Top;
                le.Bounds = bounds;
            }

            this.vScrollBar1.Maximum = curY;
            this.vScrollBar1.SmallChange = LineElement.LineHeight;
            this.vScrollBar1.LargeChange = (clientR.Height / LineElement.LineHeight) * LineElement.LineHeight;
            Invalidate(true);
        }

        /// <summary>
        /// KwicViewの設定パネルを表示し、オプションを更新する
        /// </summary>
        /// <param name="location"></param>
        public void DoKwicViewSettings(Point location)
        {
            KwicViewSettingDialog dlg = new KwicViewSettingDialog();
            KwicViewSettings oldSetting = KwicView.Settings;
            dlg.View = this;
            dlg.Location = location;
            if (dlg.ShowDialog() != DialogResult.OK)
            { // Revert
                KwicView.Settings = oldSetting;
            }
            CalculateLineHeight();
            RecalcLayout();
            Invalidate(true);
        }

        /// <summary>
        /// index行目のLineElementを選択状態にする
        /// </summary>
        /// <param name="index"></param>
        public void AlterCheckState(int index)
        {
            m_Model.KwicList.AlterCheckState(index);
        }

        /// <summary>
        /// 指定位置（スクロールバーの位置で指定）にスクロールする
        /// </summary>
        /// <param name="val"></param>
        private void SetScrollPos(int val)
        {
            VScrollBar vs = this.vScrollBar1;
            val = Math.Min(val, vs.Maximum);
            val = Math.Max(val, vs.Minimum);
            if (vs.Value == val)
            {
                return;
            }
            vs.Value = val;
            pictureBox1.Refresh();  // PictureBoxはInvalidateだけでなくRefreshしなければ全体が再描画されない。
            pictureBox1.Invalidate();
        }

        /// <summary>
        /// 現在のスクロール位置を得る
        /// </summary>
        /// <returns></returns>
        private int GetScrollPos()
        {
            return this.vScrollBar1.Value;
        }

        /// <summary>
        /// LineElementに対するマウスのHitTest判定を行う
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ht"></param>
        /// <returns></returns>
        private LineElement HitTestLineElement(Point p, out HitType ht)
        {
            ht = HitType.None;

            for (int n = this.DrawLinesFrom; n <= this.DrawLinesTo; n++)
            {
                if (n >= m_LineElements.Count)
                {
                    return null;
                }
                LineElement le = m_LineElements[n];
                Rectangle r = le.Bounds;
                if (r.Contains(p))
                {
                    int xoffset = p.X - r.Left;
                    if (m_Columns.IsInSentence(xoffset))
                    {
                        ht = HitType.AtSentence;
                    }
                    else if (m_Columns.IsInCheckBox(xoffset))
                    {
                        ht = HitType.AtCheckBox;
                    }
                    else
                    {
                        ht = HitType.AtLine;
                    }
                    return le;
                }
            }
            return null;
        }

        /// <summary>
        /// すべての選択を解除する
        /// </summary>
        public void ClearSelection()
        {
            ClearLineSelection();
            ClearCharSelection();
        }

        /// <summary>
        /// 行選択を解除する
        /// </summary>
        public void ClearLineSelection()
        {
            m_CurSelLines.Clear();
        }

        /// <summary>
        /// 文字列選択を解除する
        /// </summary>
        public void ClearCharSelection()
        {
            m_CurSelCharRange.Clear();
        }

        /// <summary>
        /// 指定行が見える位置まで最小限のスクロールを行う
        /// </summary>
        public void MakeLineVisible(int idx)
        {
            Rectangle r = m_LineElements[idx].Bounds;
            int y = GetScrollPos();
            int height = this.Height;
            int amount = 0;
            if (r.Top < y)  // 指定行は現在のビューの下にある
            {
                amount = r.Top - y;  // negative value
            }
            else if (r.Bottom > (y + this.Height))
            {
                amount = r.Bottom - y - this.Height;  // positove value
            }
            if (amount != 0)
            {
                SetScrollPos(y + amount);
            }
        }

        /// <summary>
        /// m_CurSelCharRangeを元に、カーソル位置を計算し、カーソル表示更新を行う。
        /// </summary>
        private void UpdateCaret()
        {
            CharPosElement cpe = m_CurSelCharRange.Start;
            if (!cpe.IsValid)
            {
                m_Caret.Visible = false;
                return;
            }
            WordElement we = cpe.Word;
            Size leBase = new Size(we.Parent.Bounds.Location);
            Size weBase = new Size(m_Columns.GetSentenceOffset() + we.Bounds.X, we.Bounds.Y);

            Point at = new Point(leBase.Width + weBase.Width + we.IndexToOffset(cpe.CharInWord),
                                 leBase.Height + weBase.Height);
            at.Y = at.Y - GetScrollPos() + 5;
            Rectangle r = new Rectangle(m_Caret.Location.X, m_Caret.Location.Y, 10, m_Caret.Height);

            m_Caret.Location = at;

            // Caretの残像を消すために、移動前の位置近辺をInvalidateする
            // (ScrollPosが変わっている場合は全体がInvalidateされるので、ここでのInvalidateは無意味）
            pictureBox1.Invalidate(r);
        }

        /// <summary>
        /// ひとつ前のWordElementを得る
        /// </summary>
        /// <param name="we"></param>
        /// <returns></returns>
        private WordElement GetPreviousWordElement(WordElement we)
        {
            LineElement le = we.Parent;
            WordElement we1 = null;
            int idx = le.WordElements.IndexOf(we);
            if (idx > 0)
            {
                we1 = le.WordElements[idx - 1];
            }
            else
            {
                int idx2 = m_LineElements.IndexOf(le);
                if (idx2 > 0)
                {
                    le = m_LineElements[idx2 - 1];
                    try
                    {
                        we1 = le.WordElements[le.WordElements.Count - 1];
                    }
                    catch
                    {
                        Console.WriteLine("//@todo: WordElementsがない場合");
                    }
                }
            }
            return we1;
        }

        /// <summary>
        /// 次のWordElementを得る
        /// </summary>
        /// <param name="we"></param>
        /// <returns></returns>
        private WordElement GetNextWordElement(WordElement we)
        {
            WordElement we1 = null;
            LineElement le = we.Parent;
            int idx = le.WordElements.IndexOf(we);
            if (idx < le.WordElements.Count - 1)
            {
                we1 = le.WordElements[idx + 1];
            }
            else
            {
                int idx2 = m_LineElements.IndexOf(le);
                if (idx2 < m_LineElements.Count - 1)
                {
                    le = m_LineElements[idx2 + 1];
                    try
                    {
                        we1 = le.WordElements[0];
                    }
                    catch
                    {
                        Console.WriteLine("//@todo: WordElementsがない場合");
                    }
                }
            }
            return we1;
        }

        /// <summary>
        /// 1つ上の行において近いX位置に選択文字位置を移動する
        /// なければその行の行末位置へ移動する
        /// </summary>
        /// <param name="we"></param>
        /// <returns>CurSelがなければfalseを返す</returns>
        private bool MoveUpCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.Start;
            if (!cpe.IsValid)
            {
                return false;
            }
            WordElement we = cpe.Word;
            int x = we.Bounds.Left + we.IndexToOffset(cpe.CharInWord);
            if (we.LinePos > 0)
            {
                cpe = we.Parent.GetNearestCharPos(we.LinePos - 1, x);
            }
            else
            {
                int idx = m_LineElements.IndexOf(we.Parent);
                if (idx > 0)
                {
                    LineElement le = m_LineElements[idx - 1];
                    cpe = le.GetNearestCharPos(le.LinePosMax, x);
                }
            }
            if (cpe.IsValid)
            {
                m_CurSelCharRange.Start = cpe;
                m_CurSelCharRange.End = m_CurSelCharRange.Start;
                MakeLineVisible(m_LineElements.IndexOf(cpe.Word.Parent));
            }
            UpdateCaret();
            return true;
        }

        private bool PageUpCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.Start;
            if (!cpe.IsValid)
            {
                return false;
            }
            LineElement le = cpe.Word.Parent;
            int lidx = m_LineElements.IndexOf(le);
            int linePos = cpe.Word.LinePos;
            for (int i = 0; i < 10; i++)
            {
                if (linePos > 0)
                {
                    linePos--;
                }
                else if (lidx > 0)
                {
                    lidx--;
                    le = m_LineElements[lidx];
                    linePos = le.LinePosMax;
                }
            }
            int x = cpe.Word.Bounds.Left + cpe.Word.IndexToOffset(cpe.CharInWord);
            cpe = le.GetNearestCharPos(linePos, x);
            if (cpe.IsValid)
            {
                m_CurSelCharRange.Start = cpe;
                m_CurSelCharRange.End = m_CurSelCharRange.Start;
                MakeLineVisible(m_LineElements.IndexOf(cpe.Word.Parent));
            }
            UpdateCaret();
            return true;
        }

        /// <summary>
        /// 1つ下の行において近いX位置に選択文字位置を移動する
        /// なければその行の行末位置へ移動する
        /// </summary>
        /// <param name="we"></param>
        /// <returns>CurSelがなければfalseを返す</returns>
        private bool MoveDownCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.Start;
            if (!cpe.IsValid)
            {
                return false;
            }
            WordElement we = cpe.Word;
            int x = we.Bounds.Left + we.IndexToOffset(cpe.CharInWord);
            if (we.LinePos < we.Parent.LinePosMax)
            {
                cpe = we.Parent.GetNearestCharPos(we.LinePos + 1, x);
            }
            else
            {
                int idx = m_LineElements.IndexOf(we.Parent);
                if (idx < m_LineElements.Count - 1)
                {
                    LineElement le = m_LineElements[idx + 1];
                    cpe = le.GetNearestCharPos(0, x);
                }
            }
            if (cpe.IsValid)
            {
                m_CurSelCharRange.Start = cpe;
                m_CurSelCharRange.End = m_CurSelCharRange.Start;
                MakeLineVisible(m_LineElements.IndexOf(cpe.Word.Parent));
            }
            UpdateCaret();
            return true;
        }

        private bool PageDownCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.Start;
            if (!cpe.IsValid)
            {
                return false;
            }
            LineElement le = cpe.Word.Parent;
            int lidx = m_LineElements.IndexOf(le);
            int linePos = cpe.Word.LinePos;
            for (int i = 0; i < 10; i++)
            {
                if (linePos < le.LinePosMax)
                {
                    linePos++;
                }
                else if (lidx < m_LineElements.Count - 1)
                {
                    lidx++;
                    le = m_LineElements[lidx];
                    linePos = 0;
                }
            }
            int x = cpe.Word.Bounds.Left + cpe.Word.IndexToOffset(cpe.CharInWord);
            cpe = le.GetNearestCharPos(linePos, x);
            if (cpe.IsValid)
            {
                m_CurSelCharRange.Start = cpe;
                m_CurSelCharRange.End = m_CurSelCharRange.Start;
                MakeLineVisible(m_LineElements.IndexOf(cpe.Word.Parent));
            }
            UpdateCaret();
            return true;
        }

        /// <summary>
        /// 現在の選択文字位置を+1する.
        /// </summary>
        private bool ForwardCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.Start;
            if (!cpe.IsValid)
            {
                return false;
            }

            WordElement we = cpe.Word;
            if (cpe.CharInWord < we.Length - 1)
            {
                m_CurSelCharRange.Start.CharInWord++;
                m_CurSelCharRange.End = m_CurSelCharRange.Start;
            }
            else
            {
                WordElement we1 = GetNextWordElement(we);
                if (we1 != null)
                {
                    m_CurSelCharRange.Start = new CharPosElement(we1, 0);
                    m_CurSelCharRange.End = m_CurSelCharRange.Start;
                    MakeLineVisible(m_LineElements.IndexOf(we1.Parent));
                }

            }
            UpdateCaret();
            return true;
        }

        /// <summary>
        /// 現在の選択文字位置終端を+1する.
        /// </summary>
        private bool ForwardExtendCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.End;
            if (!cpe.IsValid)
            {
                return false;
            }

            WordElement we = cpe.Word;
            if (cpe.CharInWord < we.Length - 1)
            {
                m_CurSelCharRange.End.CharInWord++;
            }
            else
            {
                WordElement we1 = GetNextWordElement(we);
                if (we1 != null)
                {
                    m_CurSelCharRange.End = new CharPosElement(we1, 0);
                    MakeLineVisible(m_LineElements.IndexOf(we1.Parent));
                }
            }
            UpdateCaret();
            return true;
        }

        /// <summary>
        /// 現在の選択文字位置を-1する.
        /// </summary>
        private bool BackwardCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.Start;
            if (!cpe.IsValid)
            {
                return false;
            }

            WordElement we = cpe.Word;
            if (cpe.CharInWord > 0)
            {
                m_CurSelCharRange.Start.CharInWord--;
                m_CurSelCharRange.End = m_CurSelCharRange.Start;
            }
            else
            {
                WordElement we1 = GetPreviousWordElement(we);
                if (we1 != null)
                {
                    m_CurSelCharRange.Start = new CharPosElement(we1, we1.CharPos.Count - 1);
                    m_CurSelCharRange.End = m_CurSelCharRange.Start;
                    MakeLineVisible(m_LineElements.IndexOf(we1.Parent));
                }
            }
            UpdateCaret();
            return true;
        }

        /// <summary>
        /// 現在の選択文字位置開始端を-1する.
        /// </summary>
        private bool BackwardExtendCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.End;
            if (!cpe.IsValid)
            {
                return false;
            }

            WordElement we = cpe.Word;
            if (cpe.CharInWord > 0)
            {
                m_CurSelCharRange.End.CharInWord--;
            }
            else
            {
                WordElement we1 = GetPreviousWordElement(we);
                if (we1 != null)
                {
                    m_CurSelCharRange.End = new CharPosElement(we1, we1.CharPos.Count - 1);
                    MakeLineVisible(m_LineElements.IndexOf(we1.Parent));
                }
            }
            UpdateCaret();
            return true;
        }

        /// <summary>
        /// カーソルを行頭へ移動
        /// </summary>
        public bool MoveHomeCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.End;
            if (!cpe.IsValid)
            {
                return false;
            }
            LineElement le = cpe.Word.Parent;
            if (le.WordElements.Count == 0)
            {
                return true;
            }
            m_CurSelCharRange.Start = new CharPosElement(le.WordElements[0], 0);
            m_CurSelCharRange.End = m_CurSelCharRange.Start;
            UpdateCaret();
            return true;
        }

        /// <summary>
        /// カーソルを行末へ移動
        /// </summary>
        public bool MoveEndCurSel()
        {
            CharPosElement cpe = m_CurSelCharRange.End;
            if (!cpe.IsValid)
            {
                return false;
            }
            LineElement le = cpe.Word.Parent;
            if (le.WordElements.Count == 0)
            {
                return true;
            }
            WordElement we = le.WordElements[le.WordElements.Count - 1];
            m_CurSelCharRange.Start = new CharPosElement(we, we.CharPos.Count - 1);
            m_CurSelCharRange.End = m_CurSelCharRange.Start;
            UpdateCaret();
            return true;
        }

        /// <summary>
        /// 選択行をPage Upする
        /// </summary>
        /// <returns></returns>
        public bool PageUpCurSelLine(bool isShiftPressed)
        {
            return MoveCurSel(-10, isShiftPressed);
        }

        /// <summary>
        /// 選択行をPage Downする
        /// </summary>
        /// <returns></returns>
        public bool PageDownCurSelLine(bool isShiftPressed)
        {
            return MoveCurSel(10, isShiftPressed);
        }

        /// <summary>
        /// 選択行を-1する
        /// </summary>
        /// <returns></returns>
        public bool MoveUpCurSelLine(bool isShiftPressed)
        {
            return MoveCurSel(-1, isShiftPressed);
        }

        /// <summary>
        /// 選択行を+1する
        /// </summary>
        /// <returns></returns>
        public bool MoveDownCurSelLine(bool isShiftPressed)
        {
            return MoveCurSel(1, isShiftPressed);
        }

        private bool MoveCurSel(int offset, bool bExtending)
        {
            int idx = m_CurSelLines.Move(0, m_LineElements.Count - 1, offset, bExtending);
            if (idx >= 0)
            {
                MakeLineVisible(idx);
                RaiseSelectionChanged(idx);
                return true;
            }
            return false;
        }

        public int SetCurSel(int index)
        {
            if (m_Model == null || m_Model.KwicList == null)
            {
                return -1;
            }
            index = Math.Max(0, index);
            index = Math.Min(m_Model.KwicList.Records.Count - 1, index);
            m_CurSelLines.Selections.Clear();
            m_CurSelLines.AddSelection(index);
            RaiseSelectionChanged(index);
            MakeLineVisible(index);
            Invalidate(true);
            return index;
        }

        /// <summary>
        /// 現在のRecordから該当するdocid, senidを持つ行を選択状態にし、その行が表示されるように自動的にスクロールする.
        /// </summary>
        /// <param name="docid"></param>
        /// <param name="senid"></param>
        /// <returns>該当行がなければfalse</returns>
        public bool SetCurSelBySentenceID(int senid)
        {
            if (m_Model == null || m_Model.KwicList == null)
            {
                return true;
            }
            var index = m_Model.KwicList.Records.FindIndex(rec => rec.SenID == senid);
            if (index >= 0)
            {
                m_CurSelLines.Selections.Clear();
                m_CurSelLines.AddSelection(index);
                RaiseSelectionChanged(index);
                MakeLineVisible(index);
                Invalidate(true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// カレント行に対する既定の処理を開始する.
        /// 1. Context取得処理
        /// 2. DepEdit開始処理
        /// </summary>
        public void ActivateCurSelLine()
        {
            if (m_CurSelLines.Selections.Count == 0)
            {
                return;
            }
            int lineno = m_CurSelLines.Selections[0];
            if (OnContextRequested != null)
            {
                OnContextRequested(m_Model.KwicList, lineno);
            }
            if (OnCurrentChanged != null)
            {
                KwicItem ki = m_Model.KwicList.Records[lineno];
                OnCurrentChanged(ki.Crps, ki.SenID);
            }
            if (OnDepEditRequested != null)
            {
                OnDepEditRequested(m_Model.KwicList.Records[lineno]);
            }
        }

        /// <summary>
        /// 行ソートを行う
        /// </summary>
        /// <param name="colno"></param>
        /// <param name="order"></param>
        public void Sort(int colno, SortOrder order)
        {
            //            ResetUpdateStatus();
            if (m_Model == null || m_Model.KwicList == null)
            {
                return;
            }
            m_Model.KwicList.Sort(colno, (order == SortOrder.Ascending));
        }

        /// <summary>
        /// 現在のカーソル位置以降に現れるkeyと一致する表層を検索して選択状態にする.
        /// </summary>
        /// <param name="key"></param>
        public void FindNext(string key)
        {
            int posInLine = 0;
            int startIndex = 0;
            if (m_CurSelCharRange != null && m_CurSelCharRange.Valid)
            {
                WordElement we = m_CurSelCharRange.End.Word;
                startIndex = we.Parent.Index;
                LineElement le = m_LineElements[startIndex];
                if (le.WordElements.Count == 0)
                {
                    posInLine = 0;
                }
                else
                {
                    posInLine = we.StartChar - le.WordElements[0].StartChar + m_CurSelCharRange.End.CharInWord;
                }
            }
            while (true)
            {
                for (int index = startIndex; index < m_LineElements.Count; index++)
                {
                    LineElement line = m_LineElements[index];
                    CharRangeElement range = line.FindNext(posInLine, key);
                    if (range != null)
                    {
                        m_CurSelCharRange = range;
                        MakeLineVisible(index);
                        Refresh();
                        return;
                    }
                    posInLine = 0;
                }
                if (MessageBox.Show(
                        "End of search scope has been reached.\nDo you want to continue from the beginning?",
                        "Search",
                        MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
                posInLine = 0;
                startIndex = 0;
            }
        }

        public int SingleSelection
        {
            get
            {
                if (m_CurSelLines == null)
                {
                    return -1;
                }
                return m_CurSelLines.GetSingleSelection();
            }
            set
            {
                if (m_CurSelLines != null)
                {
                    m_CurSelLines.Clear();
                    if (value >= 0 && value < m_Model.KwicList.Records.Count)
                    {
                        m_CurSelLines.Selections.Add(value);
                    }
                }
            }
        }

        public void CopyToClipboard()
        {
            var sel = m_CurSelCharRange.ToAscendingRange();
            if (!sel.IsEmpty)
            {
                // 文字範囲選択があれば、それをコピー（表層形）
                CopyCharSelectionToClipboard();
            }
            else
            {
                // 文字範囲選択がなく、行選択があれば、選択行単位でコピー（表層形）
                if (m_CurSelLines != null)
                {
                    CopyLineSelectionToClipboard();
                }
            }
        }

        public void CopyCharSelectionToClipboard()
        {
            var sel = m_CurSelCharRange.ToAscendingRange();

            var sb = new StringBuilder();
            var continueSelectionFlag = false;
            var we = sel.Start.Word;
            while (we != null)
            {
                if (continueSelectionFlag)
                {
                    if (sel.End.WordID > we.Index)
                    {
                        // fullly select this WordElement
                        sb.Append(we.L1String);
                        if (we.KwicWord.Word != null && we.KwicWord.Word.Extras.Length > 0)
                        {
                            sb.Append(we.KwicWord.Word.Extras);
                        }
                    }
                    else if (sel.End.WordID == we.Index)
                    {
                        // selection ends in this WordElement
                        sb.Append(we.L1String.Substring(0, sel.End.CharInWord));
                        break;
                    }
                }
                else
                {
                    if (sel.Start.WordID == we.Index)
                    {
                        if (sel.End.WordID == we.Index)
                        {
                            // selection starts and ends in this WordElement
                            sb.Append(we.L1String.Substring(sel.Start.CharInWord, (sel.End.CharInWord - sel.Start.CharInWord)));
                            if (we.KwicWord.Word != null && we.KwicWord.Word.Extras.Length > 0)
                            {
                                sb.Append(we.KwicWord.Word.Extras);
                            }
                            break;
                        }
                        else
                        {
                            // selection starts in this WordElement and continues
                            sb.Append(we.L1String.Substring(sel.Start.CharInWord, (we.L1String.Length - sel.Start.CharInWord)));
                            if (we.KwicWord.Word != null && we.KwicWord.Word.Extras.Length > 0)
                            {
                                sb.Append(we.KwicWord.Word.Extras);
                            }
                            continueSelectionFlag = true;
                        }
                    }
                }
                var nwe = we.Parent.WordElements.Count;
                if (nwe > 0 && we.Parent.WordElements[nwe - 1] == we)
                {
                    // Last WordElement of parent LineElement
                    sb.AppendLine();
                }
                we = GetNextWordElement(we);
            }
            Clipboard.SetDataObject(sb.ToString());
        }

        public void CopyLineSelectionToClipboard()
        {
            var sb = new StringBuilder();
            foreach (var le in m_LineElements)
            {
                if (m_CurSelLines.Contains(le.Index))
                {
                    foreach (var we in le.WordElements)
                    {
                        sb.Append(we.L1String);
                    }
                    sb.AppendLine();
                }
            }
            Clipboard.SetDataObject(sb.ToString());
        }

        #region -------------------- イベントハンドラ -------------------------

        delegate void UpdateKwicListDelegate();

        static bool TimerGuard = false;

        void OnTimer(object obj)
        {
            if (TimerGuard)
            {
                return;
            }
            if (m_Model == null)
            {
                return;
            }
            TimerGuard = true;
            if (Interlocked.Exchange(ref m_iNotify, 0) != 0) // m_iNotfyが 1->0 に変化した時のみ下を実行
            {
                try
                {
                    Invoke(new UpdateKwicListDelegate(this.UpdateKwicList), null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            TimerGuard = false;
        }

        void OnModelChanged(object sender, UpdateKwicListEventArgs e)
        {
            if (e.Sorted)
            {
                SynchronizeKwicListOrder();
                RecalcLayout();
                return;
            }
            Interlocked.Exchange(ref m_iNotify, 1);
            if (e.RedrawNeeded)
            {
                Invalidate(true);
            }
        }

        internal void OnWidthChanged(Columns cols)
        {
            if (cols != null)
            {
                m_Columns = cols;
                if (!this.SuspendUpdateView)
                {
                    RecalcLayout();
                    Invalidate(true);
                }
            }
        }

        /// <summary>
        /// ChaKiOptionのフォントが変更されたときのEvent Handler。
        /// 再描画を行う。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnFontChanged(object sender, EventArgs e)
        {
            FurnishFonts();
            this.pictureBox1.BackColor = ColorTranslator.FromHtml(GUISetting.Instance.KwicViewBackground);
            this.pictureBox1.Invalidate(true);
        }

        /// <summary>
        /// 単一行が選択されている場合、選択行を上下する.
        /// </summary>
        /// <param name="offset"></param>
        public bool ShiftSelection(int offset)
        {
            if (m_CurSelLines.Selections.Count != 1)
            {
                return false;
            }
            int i = m_CurSelLines.Selections[0] + offset;
            if (i < 0 || i >= m_Model.KwicList.Records.Count)
            {
                return false;
            }
            ClearLineSelection();
            m_CurSelLines.AddSelection(i);
            RaiseSelectionChanged(i);
            Refresh();
            return true;
        }

        /// <summary>
        /// 単一行が選択されている場合、選択されているKwicItemを返す.
        /// </summary>
        /// <param name="offset"></param>
        public KwicItem GetSelection()
        {
            if (m_CurSelLines.Selections.Count != 1)
            {
                return null;
            }
            int i = m_CurSelLines.Selections[0];
            if (i < 0 || i >= m_Model.KwicList.Records.Count)
            {
                return null;
            }
            return m_Model.KwicList.Records[i];
        }

#if DEBUG
        private static int count = 0;
        private static Stopwatch swatch = new Stopwatch();
#endif

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
#if DEBUG
            swatch.Reset();
            swatch.Start();
#endif
            Graphics g = e.Graphics;
            ExtendedGraphics eg = new ExtendedGraphics(g);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle clientR = this.ClientRectangle; 
            WordElement.RenderTwoLine = this.TwoLineMode;

            int xoffset = 0;
            int yoffset = 0;

            // カラム境界線を描画する
            if (m_Columns == null)
            {
                return;
            }
            for (int i = 0; i < m_Columns.Widths.Length - 1; i++)
            {
                xoffset += m_Columns.Widths[i];
                if (i != 6 && i != 7)
                {
                    g.DrawLine(Pens.LightGray, xoffset - 1, 0, xoffset - 1, clientR.Height);
                }
            }

            if (m_Model == null || m_LineElements.Count == 0)
            {
                return;
            }

            SegPosCache.Clear();
            foreach (var le in m_LineElements)
            {
                le.ClearSegmentCache();
            }

            // WordBlockの描画範囲を決定する
            lock (this)
            {
                xoffset = 0;
                yoffset = GetScrollPos();
                this.DrawLinesFrom = 0;
                this.DrawLinesTo = m_LineElements.Count - 1;
                for (int i = 0; i < m_LineElements.Count; i++)
                {
                    if (m_LineElements[i].Bounds.Bottom > yoffset)
                    {
                        this.DrawLinesFrom = i;
                        break;
                    }
                }
                for (int i = this.DrawLinesFrom; i < m_LineElements.Count; i++)
                {
                    Rectangle r = m_LineElements[i].Bounds;
                    r.Offset(xoffset, -yoffset);
                    if (r.Top > clientR.Height)
                    {
                        this.DrawLinesTo = i;
                        break;
                    }
                }
            }

            CharRangeElement sel = m_CurSelCharRange.ToAscendingRange();
            bool continueSelectionFlag = false;
#if false // 表示範囲を超えて続く選択範囲をどうするか?
            if (sel.Valid && sel.Start.WordID < this.WordElements[0].Index)
            {
                continueSelectionFlag = true;
            }
#endif
            //            Debug.WriteLine(string.Format("Start={0}, End={1}", this.DrawLinesFrom, this.DrawLinesTo));
            //            Debug.WriteLine(string.Format("SelStart={0};{1}, SelEnd={2};{3}", 
            //                                sel.Start.WordID, sel.Start.CharInWord, sel.End.WordID, sel.End.CharInWord));
            for (int i = this.DrawLinesFrom; i <= this.DrawLinesTo; i++)
            {
                m_LineElements[i].Render(eg, xoffset, yoffset, m_CurSelLines, sel, clientR, ref continueSelectionFlag, KwicView.Settings);
            }

            // Link&Group描画範囲にClippingRectを設定
            var cliprect = new Rectangle(m_Columns.GetSentenceOffset(), 0, m_Columns.GetMaxSentenceWidth(), pictureBox1.Height);
            g.SetClip(cliprect);

            #region Link描画
            for (int i = this.DrawLinesFrom; i <= this.DrawLinesTo; i++)
            {
                m_LineElements[i].RenderLinks(eg, KwicView.Settings);
            }
            #endregion

            #region Group描画
#if false
            if (this.ShowGroups)
            {
                foreach (Group grp in m_Model.Groups)
                {
                    foreach (Segment seg in grp.Members)
                    {
                        Rectangle r;
                        if (!this.SegPosCache.TryGetValue(seg, out r))
                        {
                            continue;
                        }
                        r.Inflate(0, -5);
                        Brush br;
                        if (!m_GroupBrush.TryGetValue(grp.Tag, out br))
                        {
                            continue;
                        }
                        g.FillRectangle(br, r);
                        g.DrawString(string.Format("G{0}", grp.ID), m_TinyFont, Brushes.Black, (float)r.Left, (float)r.Top - 12);
                    }
                }
            }
#endif
            #endregion

            g.ResetClip();


            if (OnPaintDone != null)
            {
                OnPaintDone(this, null);
            }
#if DEBUG
            swatch.Stop();
            Debug.WriteLine(string.Format("pictureBox1_Paint {0} time={1}", count++, swatch.ElapsedMilliseconds));
#endif

            m_Caret.Visible = true;
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            SetScrollPos(e.NewValue);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //            this.vScrollBar1.Focus(); // このPanelのトップレベルコントロールであるvscrollがキーイベントを受け取るようにする
            this.Focus(); // dummyTextBoxがキーイベントを受け取るようにする

            m_Caret.Create(m_Fonts[0].Height);
            bool isShiftPressed = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift);
            bool isCtrlPressed = ((Control.ModifierKeys & Keys.Control) == Keys.Control);

            if (e.Button == MouseButtons.Left)
            {
                ClearCharSelection();
                // 選択開始Boxを検索する
                Point p = e.Location;
                p.Offset(0, GetScrollPos());
                HitType ht;
                LineElement le = HitTestLineElement(p, out ht);
                if (ht == HitType.AtCheckBox)
                {
                    ClearLineSelection();
                    m_CurSelLines.AddSelection(le.Index);
                    RaiseSelectionChanged(le.Index);
                    AlterCheckState(le.Index);
                }
                else
                {
                    if (le != null)
                    {
                        if (ht == HitType.AtSentence)
                        {
                            Size leBase = new Size(le.Bounds.Location);
                            p -= leBase;
                            WordElement we = le.HitTestWordElement(p);
                            if (we != null)
                            {
                                // 選択開始文字を特定して、ドラッグモードに入る
                                Size weBase = new Size(m_Columns.GetSentenceOffset() + we.Bounds.X, we.Bounds.Y);
                                p -= weBase;
                                Point at;
                                m_CurSelCharRange.Start = we.HitTestChar(p, out at);
                                m_CurSelCharRange.End = m_CurSelCharRange.Start;
                                UpdateCaret();
                                if (m_CurSelCharRange.Start.IsValid)
                                {
                                    m_Dragging = true;
                                }
                            }
                        }
                    }
                    if (ht == HitType.AtLine || ht == HitType.AtSentence)
                    {
                        // カレント行を変更する
                        if (!isCtrlPressed && !isShiftPressed)
                        {
                            ClearLineSelection();
                        }
                        if (isShiftPressed)
                        {
                            m_CurSelLines.ExtendSelectionTo(le.Index);
                        }
                        else
                        {
                            m_CurSelLines.AddSelection(le.Index);
                            RaiseSelectionChanged(le.Index);
                        }
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // コンテクストメニュー表示
                var p = this.PointToScreen(e.Location);
                this.contextMenuStrip1.Show(p);
            }
            this.pictureBox1.Refresh();
            this.pictureBox1.Invalidate();
            //            Invalidate(true);
        }


        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point p = e.Location;
                p.Offset(0, GetScrollPos());
                HitType ht;
                LineElement le = HitTestLineElement(p, out ht);
                if (le != null)
                {
                    if (ht == HitType.AtSentence)
                    {
                        p.Offset(-le.Bounds.X, -le.Bounds.Y);
                        WordElement we = le.HitTestWordElement(p);
                        if (we != null)
                        {
                            // １語全体を選択状態にする
                            ClearCharSelection();
                            m_CurSelCharRange.Start = new CharPosElement(we, 0);
                            m_CurSelCharRange.End = new CharPosElement(we, we.Length);
                            m_Dragging = false;
                        }
                    }
                    if (ht == HitType.AtLine || ht == HitType.AtSentence)
                    {
                        // 選択行を変更する
                        ClearLineSelection();
                        m_CurSelLines.AddSelection(le.Index);
                        RaiseSelectionChanged(le.Index);
                        ActivateCurSelLine();
                    }
                }
            }

            Invalidate(true);
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (m_Dragging)
                {
                    if (m_CurSelCharRange.Valid)
                    {
                        if (m_CurSelCharRange.IsEmpty)
                        {
                            //                            ClearCharSelection();
                        }
                    }
                    else
                    {
                        ClearCharSelection();
                    }
                    m_Dragging = false;
                    pictureBox1.Invalidate();
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.Location;
            p.Offset(0, GetScrollPos());
            WordElement we = null;
            HitType ht;
            LineElement le = HitTestLineElement(p, out ht);
            if (le != null)
            {
                p.Offset(-le.Bounds.X, -le.Bounds.Y);
                we = le.HitTestWordElement(p);
            }
            if (we == null)
            {
                this.pictureBox1.Cursor = Cursors.Arrow;
                return;
            }
            this.pictureBox1.Cursor = Cursors.IBeam;
            // Guide Panelへのイベント通知
            if (this.OnUpdateGuidePanel != null && we.KwicWord.Lex != null)
            {
                this.OnUpdateGuidePanel(we.KwicItem.Crps, we.KwicWord.Lex);
            }
            // Word Mappingがある場合はハイライト表示を行う
            if (we.KwicWord.Word != null && !we.KwicWord.Word.MappedTo.IsEmpty)
            {
                if (WordMappingHilightRequested != null)
                {
                    WordMappingHilightRequested(this, new WordMappingHilightEventArgs(
                        we.Parent.KwicItem.Crps.Name, we.KwicWord.Word.ID, we.KwicWord.Word.MappedTo));
                }
            }
            if (m_Dragging)
            {
                // 選択終了位置を更新する
                p.Offset(-m_Columns.GetSentenceOffset() - we.Bounds.X, -we.Bounds.Y);
                Point at;
                CharPosElement cs = we.HitTestChar(p, out at);
                if (cs.IsValid)
                {
                    m_CurSelCharRange.End = cs;
                }

                pictureBox1.Invalidate();
            }
        }

        public void DoWordMappingHilight(string corpusname, int from_word_id, Iesi.Collections.Generic.ISet<int> to_word_ids)
        {
            for (int n = this.DrawLinesFrom; n <= this.DrawLinesTo; n++)
            {
                if (n >= m_LineElements.Count)
                {
                    return;
                }
                LineElement le = m_LineElements[n];
                if (le.KwicItem.Crps.Name == corpusname)
                {
                    foreach (var we in le.WordElements)
                    {
                        if (we.KwicWord.Word == null)
                        {
                            continue;
                        }
                        var id = we.KwicWord.Word.ID;
                        if (id == from_word_id)
                        {
                            we.WordMappingHilight = 1;
                        }
                        else if (to_word_ids.Contains(id))
                        {
                            we.WordMappingHilight = 2;
                        }
                    }
                }
            }
            pictureBox1.Refresh();
        }

        /// <summary>
        /// マウスホイールのイベント処理（スクロール）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines;
            SetScrollPos(this.vScrollBar1.Value - numberOfTextLinesToMove);
        }


        /// <summary>
        /// キーイベントの処理 （PictureBoxの背後に隠れたDummyTextBoxがキー入力を得るようにしている）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dummyTextBox1_GotKey(object sender, KeyEventArgs e)
        {
            Keys k = e.KeyCode;
            bool isShiftPressed = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift);
            bool isCtrlPressed = ((Control.ModifierKeys & Keys.Control) == Keys.Control);

            switch (k)
            {
                case Keys.PageUp:
                    if (!PageUpCurSel())
                    {
                        PageUpCurSelLine(isShiftPressed);
                    }
                    pictureBox1.Refresh();
                    pictureBox1.Invalidate();
                    break;
                case Keys.PageDown:
                    if (!PageDownCurSel())
                    {
                        PageDownCurSelLine(isShiftPressed);
                    }
                    pictureBox1.Refresh();
                    pictureBox1.Invalidate();
                    break;
                case Keys.Up:
                    if (!MoveUpCurSel())
                    {
                        MoveUpCurSelLine(isShiftPressed);
                    }
                    pictureBox1.Refresh();
                    break;
                case Keys.Down:
                    if (!MoveDownCurSel())
                    {
                        MoveDownCurSelLine(isShiftPressed);
                    }
                    pictureBox1.Refresh();
                    break;
                case Keys.Left:
                    if (e.Shift)
                    {
                        BackwardExtendCurSel();
                        pictureBox1.Refresh();
                        pictureBox1.Invalidate();
                    }
                    else
                    {
                        BackwardCurSel();
                    }
                    break;
                case Keys.Right:
                    if (e.Shift)
                    {
                        ForwardExtendCurSel();
                        pictureBox1.Refresh();
                        pictureBox1.Invalidate();
                    }
                    else
                    {
                        ForwardCurSel();
                    }
                    break;
                case Keys.Home:
                    MoveHomeCurSel();
                    break;
                case Keys.End:
                    MoveEndCurSel();
                    break;
                case Keys.Enter:
                    ActivateCurSelLine();
                    break;
                case Keys.C:
                    if (isCtrlPressed)
                    {
                        // Copy
                        CopyToClipboard();
                    }
                    break;
                //case Keys.A:    // For Test (Segment assign)
                //    if (m_CurSelCharRange.Valid)
                //    {
                //        try
                //        {
                //            Segment seg = new Segment();
                //            seg.Tag = new Tag(ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT, "Test-A");
                //            seg.StartChar = m_CurSelCharRange.Start.CharID;
                //            seg.EndChar = m_CurSelCharRange.End.CharID;
                //            seg.Doc = m_CurSelCharRange.Start.Word.KwicItem.Document;   //TODO: Start,EndでDocumentが異なる場合のチェック
                //            AddNewSegment(seg);
                //        }
                //        catch (Exception ex)
                //        {
                //            Console.WriteLine(ex);
                //        }
                //    }
                //    break;
                case Keys.Space:
                    AlterCheckSelected();
                    break;
                case Keys.Escape:
                    {
                        // KwicViewがFullScreen Dialog内で表示されているときにEscキーが押されたらDialogを終了する.
                        var dlg = ControlHelper.FindAncestor<FullScreenContainer>(this);
                        if (dlg != null)
                        {
                            dlg.DoHide();
                        }
                    }
                    break;
            }
        }

        private void KwicViewPanel_Enter(object sender, EventArgs e)
        {
        }

        private void KwicViewPanel_Leave(object sender, EventArgs e)
        {
            m_Caret.Delete();
        }
        #endregion

        // Context Menuのハンドラ
        private void kwicViewSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 設定変更ダイアログを出す
            var p = new Point(this.contextMenuStrip1.Left, this.contextMenuStrip1.Top);
            DoKwicViewSettings(p);
        }

        private void RaiseSelectionChanged(int sel)
        {
            if (this.SelectionChanged != null)
            {
                var senid = m_Model.KwicList.Records[sel].SenID;
                this.SelectionChanged(this, new SelectionChangedEventArgs(sel, senid));
            }
        }
    }
}
