using System;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.GUICommon;
using ChaKi.Entity.Search;
using ChaKi.Common;
using ChaKi.Common.Settings;
using System.Drawing;
using System.Collections.Generic;

namespace ChaKi.Views.KwicView
{
    public partial class KwicView : UserControl, IChaKiView
    {
        public static KwicViewSettings Settings;

        public bool TwoLineMode
        {
            get
            {
                return this.kwicViewPanel1.TwoLineMode;
            }
            set
            {
                this.kwicViewPanel1.TwoLineMode = value;
                this.kwicViewPanel1.CalculateLineHeight();
                this.kwicViewPanel1.UpdateKwicList();
            }
        }

        private KwicViewPanel m_CurrentPanel;
        public KwicViewPanel CurrentPanel
        {
            get { return m_CurrentPanel; }
            set
            {
                if (m_CurrentPanel != value)
                {
                    m_CurrentPanel = value;
                    if (IsViewSplitted)
                    {
                        // カレントパネルの周囲に色を付ける
                        this.splitContainer1.Panel1.BackColor = System.Drawing.Color.Transparent;
                        this.splitContainer1.Panel2.BackColor = System.Drawing.Color.Transparent;
                        if (m_CurrentPanel == this.kwicViewPanel1)
                        {
                            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.SlateBlue;
                        }
                        else if (m_CurrentPanel == this.kwicViewPanel2)
                        {
                            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.SlateBlue;
                        }
                    }
                    if (CurrentPanelChanged != null)
                    {
                        CurrentPanelChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        private bool m_IsViewSplitted;
        /// <summary>
        /// Viewの分割状態を設定・取得する
        /// </summary>
        public bool IsViewSplitted
        {
            get { return m_IsViewSplitted; }
            set
            {
                m_IsViewSplitted = value;
                if (m_IsViewSplitted)
                {
                    this.splitContainer1.Panel2Collapsed = false;
                    this.splitContainer1.Panel1.Padding = new Padding(3);
                }
                else
                {
                    this.splitContainer1.Panel2Collapsed = true;
                    this.splitContainer1.Panel1.Padding = new Padding(0);
                }
                this.CurrentPanel = this.Panels[0];
            }
        }

        public List<KwicViewPanel> Panels { get; set; }

        /// <summary>
        /// カレントPasnelが StringSearch結果(Lexeme tag情報がないKwicItem) を含む
        /// </summary>
        public bool HasSimpleItem
        {
            get
            {
                if (CurrentPanel != null)
                {
                    return this.CurrentPanel.HasSimpleItem;
                }
                return false;
            }
        }

        public bool KwicMode
        {
            get
            {
                if (CurrentPanel != null)
                {
                    return this.CurrentPanel.KwicMode;
                }
                return true;
            }
            set
            {
                foreach (var panel in this.Panels)
                {
                    panel.SuspendUpdateView = true;
                    panel.KwicMode = value;
                    panel.WordWrap = !value;
                }
                // Columnヘッダをモードに合わせて変更
                string[] newTexts;
                int[] newWidths = new int[Columns.NColumns];
                if (value)
                {
                    newTexts = Columns.DefaultHeaderTextKwic;
                    if (Settings.DefaultHeaderWidthKwic == null || Settings.DefaultHeaderWidthKwic.Length < Columns.NColumns)
                    {
                        Settings.DefaultHeaderWidthKwic = new int[Columns.NColumns];
                    }
                    Settings.DefaultHeaderWidthKwic.CopyTo(newWidths, 0);
                }
                else
                {
                    newTexts = Columns.DefaultHeaderTextNonKwic;
                    if (Settings.DefaultHeaderWidthKwic == null || Settings.DefaultHeaderWidthKwic.Length < Columns.NColumns)
                    {
                        Settings.DefaultHeaderWidthNonKwic = new int[Columns.NColumns];
                    }
                    Settings.DefaultHeaderWidthNonKwic.CopyTo(newWidths, 0);
                }
                for (int i = 0; i < Columns.NColumns; i++)
                {
                    this.dataGridView1.Columns[i].HeaderText = newTexts[i];
                    this.dataGridView1.Columns[i].Width = newWidths[i];
                    m_Columns.Widths[i] = this.dataGridView1.Columns[i].Width;
                    Invalidate(true);
                }
                this.dataGridView1.ColumnHeadersHeight = (int)(23 * currentScaleFactor.Height);
                foreach (var panel in this.Panels)
                {
                    panel.SuspendUpdateView = false;
                }
            }
        }

        private Columns m_Columns;

        private SizeF currentScaleFactor = new SizeF(1f, 1f);

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            //Record the running scale factor used
            this.currentScaleFactor = new SizeF(
               this.currentScaleFactor.Width * factor.Width,
               this.currentScaleFactor.Height * factor.Height);
        }

        #region KwicView Events
        /// <summary>
        /// Current行が変化したときの親(MainForm)への通知
        /// （子Panelからの中継）
        /// </summary>
        public event CurrentChangedDelegate CurrentChanged;
        /// <summary>
        /// Context表示指示時の親(MainForm)への通知 
        /// （子Panelからの中継）
        /// </summary>
        public event RequestContextDelegate ContextRequested;
        /// <summary>
        /// DepEdit開始指示時の親(MainForm)への通知 
        /// （子Panelからの中継）
        /// </summary>
        public event RequestDepEditDelegate DepEditRequested;
        /// <summary>
        /// Guide Panelの表示更新通知 
        /// （子Panelからの中継）
        /// </summary>
        public event UpdateGuidePanelDelegate UpdateGuidePanel;
        /// <summary>
        /// KwicPanelのCurrentが変化したときのMainFormへの通知
        /// </summary>
        public event EventHandler CurrentPanelChanged;
        #endregion

        public KwicView()
        {
            InitializeComponent();

            m_Columns = new Columns();

            // Panel分割状態（DesignerではPanel2をCollapsed状態にしており、それが初期状態である）
            m_IsViewSplitted = false;
            this.Panels = new List<KwicViewPanel>() { this.kwicViewPanel1, this.kwicViewPanel2 };
            this.CurrentPanel = this.Panels[0];

            // Event Handlerの割り当て
            foreach (var panel in this.Panels)
            {
                panel.OnContextRequested += new RequestContextDelegate(HandleContextRequested);
                panel.OnCurrentChanged += new CurrentChangedDelegate(HandleCurrentChanged);
                panel.OnDepEditRequested += new RequestDepEditDelegate(HandleRequestDepEdit);
                panel.OnUpdateGuidePanel += new UpdateGuidePanelDelegate(HandleUpdateGuidePanel);
                panel.GotFocus += new EventHandler(panel_GotFocus);
                panel.SelectionChanged += new EventHandler<SelectionChangedEventArgs>(panel_SelectionChanged);
                panel.WordMappingHilightRequested += new EventHandler<WordMappingHilightEventArgs>(panel_WordMappingHilightRequested);
            }
        }

        void panel_WordMappingHilightRequested(object sender, WordMappingHilightEventArgs e)
        {
            this.kwicViewPanel1.DoWordMappingHilight(e.CorpusName, e.FromWordId, e.ToWordIds);
            this.kwicViewPanel2.DoWordMappingHilight(e.CorpusName, e.FromWordId, e.ToWordIds);
        }

        private bool isInSelectionChangedHandler = false;
        //  複数View表示時に、片方の選択行が変化したら、もう一方の選択行も自動的に更新する.
        void panel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_IsViewSplitted)
            {
                if (!isInSelectionChangedHandler)  // reentrance guard
                {
                    isInSelectionChangedHandler = true;
                    var panel = sender as KwicViewPanel;
                    if (panel == this.kwicViewPanel1)
                    {
                        this.kwicViewPanel2.SetCurSelBySentenceID(e.NewSentenceID);
                    }
                    else if (panel == this.kwicViewPanel2)
                    {
                        this.kwicViewPanel1.SetCurSelBySentenceID(e.NewSentenceID);
                    }
                    isInSelectionChangedHandler = false;
                }
            }
        }

        void panel_GotFocus(object sender, EventArgs e)
        {
            var panel = sender as KwicViewPanel;
            if (panel != null)
            {
                this.CurrentPanel = panel;
            }
        }

        public void SetModel(object model)
        {
            if (model != null && !(model is SearchHistory))
            {
                throw new ArgumentException("Assigning invalid model to KwicPanel");
            }
            if (this.CurrentPanel == null)
            {
                throw new Exception("KWicView: CurrentPanel is null");
            }
            this.CurrentPanel.SetModel((SearchHistory)model);
        }

        public SearchHistory GetModel()
        {
            if (this.CurrentPanel == null)
            {
                throw new Exception("KWicView: CurrentPanel is null");
            }
            return this.CurrentPanel.GetModel();
        }

        /// <summary>
        /// 与えられたsenidを持つKwicItemが存在する場合、
        /// そのKwicItemの前後offsetの位置にあるKwicItemを返す.
        /// なければnullを返す.
        /// </summary>
        /// <param name="senid"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public KwicItem ShiftCurrent(int offset)
        {
            if (this.CurrentPanel == null)
            {
                throw new Exception("KWicView: CurrentPanel is null");
            }
            if (!this.CurrentPanel.ShiftSelection(offset))
            {
                return null;
            }
            return this.CurrentPanel.GetSelection();
        }

        //TODO: MainForm.LoadAnnotations()を改良したら削除するべきメソッド
        public void UpdateSegments()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.UpdateAnnotations();
        }

        public void SetVisible(bool f)
        {
            this.Visible = f;
        }

        public void RecalcLayout()
        {
            foreach (var panel in this.Panels)
            {
                panel.RecalcLayout();
            }
        }

        public int GetCurrentCenterWordID()
        {
            return 0;
        }

        public void DeleteAll()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.DeleteAll();
        }

        public void CheckAll()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.CheckAll();
        }

        public void CheckSelected()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.CheckSelected();
        }

        public void UncheckAll()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.UncheckAll();
        }

        public void SelectAll()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.SelectAll();
        }

        public void UnselectAll()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.UnselectAll();
        }

        public void SelectChecked()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.SelectChecked();
        }

        public void AutoAdjustColumnWidths()
        {
            m_Columns.AutoAdjust(this.kwicViewPanel1.EffectiveWidth, this.KwicMode);
            if (this.KwicMode)
            {
                this.dataGridView1.Columns[6].Width = Settings.DefaultHeaderWidthKwic[6];
                this.dataGridView1.Columns[8].Width = Settings.DefaultHeaderWidthKwic[8];
            }
            else
            {
                this.dataGridView1.Columns[8].Width = Settings.DefaultHeaderWidthNonKwic[8];
            }
            Invalidate(true);
        }

        public void LeftAdjustColumnWidths()
        {
        }

        public void ShiftPivot(int shift)
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.ResetUpdateStatus();
            this.CurrentPanel.UpdateKwicList();
        }

        public void CutToClipboard()
        {
            // intentionally left blank
        }

        public void CopyToClipboard()
        {
            if (this.CurrentPanel != null)
            {
                this.CurrentPanel.CopyToClipboard();
            }
        }

        public void PasteFromClipboard()
        {
            // intentionally left blank
        }

        public bool CanCut { get { return false; } }    // always false

        public bool CanCopy
        {
            get
            {
                return true;    //TODO
            }
        }

        public bool CanPaste { get { return false; } }    // always false

        /// <summary>
        /// 現在のカーソル位置以降に現れるkeyと一致する表層を検索して選択状態にする.
        /// </summary>
        /// <param name="key"></param>
        public void FindNext(string key)
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.FindNext(key);
        }

        public void UpdateKwicList()
        {
            if (this.CurrentPanel == null)
            {
                return;
            }
            this.CurrentPanel.ResetUpdateStatus();
            this.CurrentPanel.UpdateKwicList();
        }

        public bool UpdateFrozen
        {
            get
            {
                if (this.CurrentPanel == null)
                {
                    return false; ;
                }
                return this.CurrentPanel.UpdateFrozen;
            }
            set
            {
                if (this.CurrentPanel == null)
                {
                    return;
                }
                this.CurrentPanel.UpdateFrozen = value;
            }
        }

        /// <summary>
        /// 単一の選択行の取得・設定を行う.
        /// 複数行が選択されている場合・選択がない場合は-1である.
        /// </summary>
        public int SelectedLine
        {
            get
            {
                if (this.CurrentPanel == null)
                {
                    return -1;
                }
                return this.CurrentPanel.SingleSelection;
            }
            set
            {
                if (this.CurrentPanel == null)
                {
                    return;
                }
                this.CurrentPanel.SingleSelection = value;
            }
        }

        /// <summary>
        /// 単一の行を選択状態にし、その行が表示されるように自動的にスクロールする.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>実際に選択できた行番号(0-base), -1 if cannot select.</returns>
        public int SetCurSel(int index)
        {
            if (this.CurrentPanel == null)
            {
                return -1;
            }
            return this.CurrentPanel.SetCurSel(index);
        }

        /// <summary>
        /// 現在のRecordから該当するsenidを持つ行を選択状態にし、その行が表示されるように自動的にスクロールする.
        /// </summary>
        /// <param name="docid"></param>
        /// <param name="senid"></param>
        /// <returns>該当行がなければfalse</returns>
        public bool SetCurSelBySentenceID(int senid)
        {
            if (this.CurrentPanel == null)
            {
                return false;
            }
            return this.CurrentPanel.SetCurSelBySentenceID(senid);
        }

        private void dataGridView1_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (m_Columns == null)
            {
                return;
            }
            for (int i = 0; i < Columns.NColumns; i++)
            {
                m_Columns.Widths[i] = this.dataGridView1.Columns[i].Width;
                if (this.KwicMode)
                {
                    KwicView.Settings.DefaultHeaderWidthKwic[i] = m_Columns.Widths[i];
                }
                else
                {
                    KwicView.Settings.DefaultHeaderWidthNonKwic[i] = m_Columns.Widths[i];
                }
            }
            foreach (var panel in this.Panels)
            {
                panel.OnWidthChanged(m_Columns);
            }
        }

        void HandleUpdateGuidePanel(Corpus corpus, Lexeme lex)
        {
            if (this.UpdateGuidePanel != null)
            {
                UpdateGuidePanel(corpus, lex);
            }
        }

        void HandleContextRequested(KwicList list, int row)
        {
            if (this.ContextRequested != null)
            {
                ContextRequested(list, row);
            }
        }

        void HandleCurrentChanged(Corpus cps, int senid)
        {
            if (this.CurrentChanged != null)
            {
                CurrentChanged(cps, senid);
            }
        }

        void HandleRequestDepEdit(KwicItem ki)
        {
            if (this.DepEditRequested != null)
            {
                DepEditRequested(ki);
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            int colno = e.ColumnIndex;
            SortOrder order = this.dataGridView1.Columns[colno].HeaderCell.SortGlyphDirection;
            foreach (var panel in this.Panels)
            {
                panel.Sort(colno, order);
            }
        }

        private bool m_IsDragging;
        private int lastX = -1;
        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (Cursor.Current == Cursors.SizeWE) // this.Cursorは正しい形状を持っていない.
            {
                transparentPanel1.Visible = true;
                m_IsDragging = true;
            }
        }

        private void dataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_IsDragging)
            {
                this.splitContainer1.Refresh();  // 背景をInvalidateすることで、前面にあるTransparentPanelの既存描画が消える.
                this.transparentPanel1.ShowVerticalSplitter = true;
                this.transparentPanel1.VerticalSplitterPos = e.X;
                this.transparentPanel1.Refresh();
            }
        }

        private void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (m_IsDragging)
            {
                m_IsDragging = false;
                this.transparentPanel1.ShowVerticalSplitter = false;
                transparentPanel1.Visible = false;  // HideしないとKwicViewPanelに一切マウスイベントが行かないので注意.（.designer.csでデフォルトfalseにしてある.）
            }
        }
    }
}
