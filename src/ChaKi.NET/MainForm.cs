using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Common.Widgets;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using ChaKi.Entity.Settings;
using ChaKi.GUICommon;
using ChaKi.Panels;
using ChaKi.Panels.ConditionsPanes;
using ChaKi.Service.Database;
using ChaKi.UICommands;
using ChaKi.Views;
using ChaKi.Views.KwicView;
using DependencyEditSLA;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MessageBox = ChaKi.Common.Widgets.MessageBox;

namespace ChaKi
{
    public delegate void BeginSearchDelegate();

    public partial class MainForm : Form
    {
        #region private members
        private static MainForm instance;
        private Panel commandPanelContainer = new Panel();
        private Panel contextPanelContainer = new Panel();
        private Panel scriptingPanelContainer = new Panel();
        private Panel LexemePanelContainer = new Panel();
        private Panel historyGuidePanelContainer = new Panel();
        private Panel AttributePanelContainer = new Panel();
        private CorpusPane corpusPane;
        private ConditionsPanel condPanel;
        private CommandPanel commandPanel;
        private ContextPanel contextPanel;
        private ScriptingPanel scriptingPanel;
        private WordAttributeListPanel wordAttributeListPanel;
        private HistoryGuidePanel historyGuidePanel;
        private DepEditControl dependencyEditPanel;
        private AttributeListPanel attributePanel;

        // このFormに対応する唯一のModelオブジェクト
        // Formの所有する各子ウィンドウは、このModelの一部を割り当てられ、そのUpdateを通知される。
        private ChaKiModel m_Model;

        private string m_DockStateFile;

        #endregion

        public static Rectangle SBounds
        {
            get
            {
                return (instance == null) ? Rectangle.Empty : instance.Bounds;
            }
        }

        static public MainForm Instance
        {
            get { return instance; }
        }

        public KwicView KwicView
        {
            get { return this.kwicView; }
        }

        public MainForm()
        {
            instance = this;

            InitializeComponent();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }
            Program.MainForm = this;
            m_Model = ChaKiModel.Instance;
            m_Model.Initialize();

            #region create views
            var resources = new ComponentResourceManager(typeof(MainForm));

            // Create Views
            // 初期状態で表示するView
            ChangeView(SearchType.SentenceSearch);

            ResumeLayout();
            #endregion

            #region create docking panels
            // Create Docking Panels
            condPanel = new ConditionsPanel(m_Model.CurrentSearchConditions) { Dock = DockStyle.Fill };
            condPanel.FilterPane.ProjectSelector = this.toolStripComboBox2;
            this.splitContainer2.Panel1.Controls.Add(condPanel);
            this.splitContainer1.Panel1.Controls.Add(condPanel.CorpusPane);
            scriptingPanel = new ScriptingPanel();
            wordAttributeListPanel = new WordAttributeListPanel();
            commandPanel = new CommandPanel(condPanel.GetFilterButton());
            this.splitContainer1.Panel2.Controls.Add(commandPanel);
            historyGuidePanel = new HistoryGuidePanel(m_Model.History);
            dependencyEditPanel = new DepEditControl() { Dock = DockStyle.Fill };
            this.tabPage1.Controls.Add(dependencyEditPanel);
            contextPanel = new ContextPanel() { Dock = DockStyle.Fill };
            this.tabPage2.Controls.Add(contextPanel);
            attributePanel = new AttributeListPanel();

            // ConditionPanelとKwicViewの間にCommandPanelの提供するボタンを配置する
            condPanel.PrepareControlButtons(this.commandPanel.SearchButton, this.commandPanel.WordListButton);

            // DPI微調整
            {
                var px = currentScaleFactor.Width;
                var py = currentScaleFactor.Height;
                this.menuStrip1.ImageScalingSize = new Size((int)(16 * px), (int)(16 * py));
                this.toolStrip.SuspendLayout();
                this.toolStrip.AutoSize = false;
                this.toolStrip.ImageScalingSize = new Size((int)(16 * px), (int)(16 * py));
                this.toolStrip.ResumeLayout();
                this.toolStrip.AutoSize = true;
                this.splitContainer1.SplitterDistance = this.condPanel.CorpusPane.ExpandedWidth;
            }
            this.condPanel.CorpusPane.SizeChanged += CorpusPane_SizeChanged;

            // Window stateの復元
            this.Location = new Point(
                (int)(WindowStates.Instance.WindowLocation.X * currentScaleFactor.Width),
                (int)(WindowStates.Instance.WindowLocation.Y * currentScaleFactor.Height));
            this.Size = new Size(
                (int)(WindowStates.Instance.WindowSize.Width * currentScaleFactor.Width),
                (int)(WindowStates.Instance.WindowSize.Height * currentScaleFactor.Height));
            if (!WindowStates.Instance.IsSplitter1Expanded)
            {
                this.condPanel.CorpusPane.Shrink();
            }
            this.splitContainer2.SplitterDistance = 
                (int)(WindowStates.Instance.SplitterPos2  * this.currentScaleFactor.Height);
            this.splitContainer3.SplitterDistance =
                (int)(WindowStates.Instance.SplitterPos3 * this.currentScaleFactor.Height);

            // Menuは常に最上部に配置する.
            this.menuStrip1.Location = new Point(0, 0);
            this.kwicView.Dock = DockStyle.Fill;
            this.wordListView.Dock = DockStyle.Fill;
            this.collocationView.Dock = DockStyle.Fill;
            #endregion

            this.kwicView.PreparePopup(this.commandPanel.DataGridView);

            #region UICommand登録
            //
            // アプリケーションのコマンド管理メカニズムは、
            // ChaKi.UICommandsサブフォルダに存在するUICommandクラスが行っている
            // cf. http://geekswithblogs.net/willemf/archive/2007/06/05/113005.aspx
            //
            UICommand uc;
            UICommand.Commands.Add((uc = new UICommand(this, "FileOpen")));
            UICommand.Commands.Add((uc = new UICommand(this, "FileSave")));
            UICommand.Commands.Add((uc = new UICommand(this, "FileSaveAs")));
            UICommand.Commands.Add((uc = new UICommand(this, "FileSendToExcelCSV")));
            UICommand.Commands.Add((uc = new UICommand(this, "FileExit")));

            UICommand.Commands.Add((uc = new UICommand(this, "EditCopy")));
            uc.UpdateEventHandler += this.OnEditCutCopyPasteUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "EditCut")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditPaste")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditDeleteAll")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditCheckAll")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditCheckSelected")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditUncheckAll")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditSelectAll")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditUnselectAll")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditSelectChecked")));
            UICommand.Commands.Add((uc = new UICommand(this, "EditChangeLexeme")));

            UICommand.Commands.Add((uc = new UICommand(this, "ViewToolbar")));
            uc.UpdateEventHandler += this.OnViewToolbarUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewStatusBar")));
            uc.UpdateEventHandler += this.OnViewStatusBarUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewSearchPanel")));
            uc.UpdateEventHandler += this.OnViewSearchPanelUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewHistoryPanel")));
            uc.UpdateEventHandler += this.OnViewHistoryPanelUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewCommandPanel")));
            uc.UpdateEventHandler += this.OnViewCommandPanelUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewContextPanel")));
            uc.UpdateEventHandler += this.OnViewContextPanelUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewDependencyEditPanel")));
            uc.UpdateEventHandler += this.OnViewDependencyEditPanelUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewScriptingPanel")));
            uc.UpdateEventHandler += this.OnViewScriptingPanelUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewKwicMode")));
            uc.UpdateEventHandler += this.OnViewKwicModeUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewTextMode")));
            uc.UpdateEventHandler += this.OnViewTextModeUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewAttributePanel")));
            uc.UpdateEventHandler += this.OnViewAttributePanelUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewLexemePanel")));
            uc.UpdateEventHandler += this.OnViewLexemePanelUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewViewAttributes")));
            uc.UpdateEventHandler += this.OnViewViewAttributesUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewReloadKwicView")));
            UICommand.Commands.Add((uc = new UICommand(this, "ViewLoadAnnotations")));
            UICommand.Commands.Add((uc = new UICommand(this, "ViewAutoAdjustRowWidth")));
            UICommand.Commands.Add((uc = new UICommand(this, "ViewAdjustRowWidthToTheLeft")));
            UICommand.Commands.Add((uc = new UICommand(this, "ViewGotoSentence")));
            UICommand.Commands.Add((uc = new UICommand(this, "ViewFreezeUpdate")));
            uc.UpdateEventHandler += this.OnViewFreezeUpdateUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewSplitView")));
            uc.UpdateEventHandler += this.OnViewSplitViewUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "ViewFullScreen")));

            UICommand.Commands.Add((uc = new UICommand(this, "SearchSelectCorpus")));
            uc.UpdateEventHandler += this.OnSearchSelectCorpusUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchSetSearchFilters")));
            uc.UpdateEventHandler += this.OnSearchSetSearchFiltersUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchStringSearchSettings")));
            uc.UpdateEventHandler += this.OnSearchStringSearchSettingsUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchTagSearchSettings")));
            uc.UpdateEventHandler += this.OnSearchTagSearchSettingsUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchDependencySearchSettings")));
            uc.UpdateEventHandler += this.OnSearchDependencySearchSettingsUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchCollocationSettings")));
            uc.UpdateEventHandler += this.OnSearchCollocationSettingsUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchBeginSearch")));
            uc.UpdateEventHandler += this.OnSearchBeginSearchUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchBeginSearchNarrow")));
            UICommand.Commands.Add((uc = new UICommand(this, "SearchBeginSearchAppend")));
            UICommand.Commands.Add((uc = new UICommand(this, "SearchBeginWordList")));
            uc.UpdateEventHandler += this.OnSearchBeginWordListUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchBeginCollocation")));
            uc.UpdateEventHandler += this.OnSearchBeginCollocationUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "SearchLoadSearchConditions")));
            UICommand.Commands.Add((uc = new UICommand(this, "SearchSaveCurrentSearchConditions")));
            UICommand.Commands.Add((uc = new UICommand(this, "SearchResetAllSearchSettings")));
            UICommand.Commands.Add((uc = new UICommand(this, "SearchExamineLastSearch")));
            UICommand.Commands.Add((uc = new UICommand(this, "SearchExamineLastEdit")));
            UICommand.Commands.Add((uc = new UICommand(this, "SearchSearchInView")));

            UICommand.Commands.Add((uc = new UICommand(this, "FormatShiftPivotLeft")));
            uc.UpdateEventHandler += this.OnFormatShiftUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "FormatShiftPivotRight")));
            UICommand.Commands.Add((uc = new UICommand(this, "FormatHilightPreviousWord")));
            UICommand.Commands.Add((uc = new UICommand(this, "FormatHilightNextWord")));

            UICommand.Commands.Add((uc = new UICommand(this, "ToolsText2Corpus")));
            UICommand.Commands.Add((uc = new UICommand(this, "ToolsCreateSQLiteCorpus")));
            UICommand.Commands.Add((uc = new UICommand(this, "ToolsCreateMySQLCorpus")));
            UICommand.Commands.Add((uc = new UICommand(this, "ToolsEditTagSetDefinitions")));
            UICommand.Commands.Add((uc = new UICommand(this, "ToolsCreateDictionary")));
            UICommand.Commands.Add((uc = new UICommand(this, "ToolsTextFormatter")));

            UICommand.Commands.Add((uc = new UICommand(this, "DictionaryExport")));
            uc.UpdateEventHandler += this.OnDictionaryExportUpdate;
            UICommand.Commands.Add((uc = new UICommand(this, "DictionaryExportMWE")));
            uc.UpdateEventHandler += this.OnDictionaryExportMWEUpdate;

            UICommand.Commands.Add((uc = new UICommand(this, "OptionsSettings")));
            UICommand.Commands.Add((uc = new UICommand(this, "OptionsPropertyBoxSettings")));
            UICommand.Commands.Add((uc = new UICommand(this, "OptionsWordColorSettings")));
            UICommand.Commands.Add((uc = new UICommand(this, "OptionsTagAppearance")));
            UICommand.Commands.Add((uc = new UICommand(this, "OptionsDictionarySettings")));

            UICommand.Commands.Add((uc = new UICommand(this, "VersioncontrolCommit")));
            uc.UpdateEventHandler += this.OnVersioncontrolCommitUpdate;

            UICommand.Commands.Add((uc = new UICommand(this, "HelpHelp")));
            UICommand.Commands.Add((uc = new UICommand(this, "HelpAbout")));
            #endregion

            // CommandPanelからのCallback
            commandPanel.BeginSearch = this.OnBeginSearch;
            commandPanel.NarrowSearch = this.OnBeginSearchNarrow;
            commandPanel.AppendSearch = this.OnBeginSearchAppend;
            commandPanel.BeginWordList = this.OnBeginWordList;
            commandPanel.BeginCollocation = this.OnBeginCollocation;
            commandPanel.SearchStatusReported += (s, e) =>
                kwicView.UpdateSearchStatus(e);

            // ConditionsPanelからのCallback
            condPanel.TabChanged += new EventHandler(OnConditionTabChanged);

            // KwicViewからのCallback
            this.kwicView.UpdateGuidePanel += new UpdateGuidePanelDelegate(this.OnUpdateGuidePanel);
            this.kwicView.CurrentChanged += new CurrentChangedDelegate(this.OnCurrentChanged);
            this.kwicView.DepEditRequested += new RequestDepEditDelegate(this.OnRequestDepEdit);
            this.kwicView.ContextRequested += new RequestContextDelegate(this.OnRequestContext);
            this.kwicView.ContextRequested += new RequestContextDelegate(this.OnRequestSentenceTagList);
            this.kwicView.CurrentPanelChanged += new EventHandler(HandleCurrentPanelChanged);
            this.kwicView.CollocationRequested += (s, e) => HandleCollocation(s, e);
            this.kwicView.ExportRequested += (s, e) => OnFileSendToExcelCSV(s, e);
            this.kwicView.AbortRequested += (s, e) => this.commandPanel.Abort(true);

            // WordListViewからのCallback
            this.wordListView.OccurrenceRequested += new EventHandler(this.OnWordOccurenceRequested);

            // CollocationViewからのcallback
            this.collocationView.OccurrenceRequested += new EventHandler<SentenceIdsOccurrenceEventArgs>(HandleCollocationOccurrenceRequested);
            this.collocationView.GoBackRequested += (s, e) => HandleHistoryBack(s, e);
            this.collocationView.ExportRequested += (s, e) => OnFileSendToExcelCSV(s, e);

            // HistoryGuidePanelからのCallback
            this.historyGuidePanel.HistoryNavigating +=new NavigateHistoryDelegate(this.OnHistoryNavigating);
            this.historyGuidePanel.HistorySaveRequested += new EventHandler(this.OnHistorySaveRequested);
            this.historyGuidePanel.DeleteHistoryRequested += new EventHandler(this.OnDeleteHistoryRequested);

            // DepEditからのCallback
            this.dependencyEditPanel.UpdateGuidePanel += new UpdateGuidePanelDelegate(this.OnUpdateGuidePanel);
            this.dependencyEditPanel.UpdateAttributePanel += new UpdateAttributePanelDelegate(this.OnUpdateAttributePanel);
            this.dependencyEditPanel.ChangeSenenceRequested += new ChangeSentenceDelegate(this.OnChangeSenenceRequested);

            this.dependencyEditPanel.EditContextChanged += new Action<OpContext>(HandleDependencyEditPanelEditModeChanged);
            this.dependencyEditPanel.Saving += new Action(HandleDependencyEditPanelSaving);
        }

        private void CorpusPane_SizeChanged(object sender, EventArgs e)
        {
            this.splitContainer1.SplitterDistance = this.condPanel.CorpusPane.Width;
        }

        private SizeF currentScaleFactor = new SizeF(1f, 1f);

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            //Record the running scale factor used
            this.currentScaleFactor = new SizeF(factor.Width, factor.Height);
        }


        public void ResetLayout()
        {
            try
            {
                using (var resstr = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChaKi.Resources.DefaultDockState.xml"))
                {
                }
            }
            catch
            {
                MessageBox.Show("Could not read default docking positions.  Resetting.");
            }
        }

        public void IdleUpdate()
        {
            UICommand.UpdateCommands();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // DependencyEditの終了前セーブを確認
            if (!this.dependencyEditPanel.EndEditing())
            {
                e.Cancel = true;
                return;
            }
            // 終了時にDepEditのコントロールをすべてクリアすることで、ちらつきを軽減する。
            this.dependencyEditPanel.Terminate();

            // AttributeEditの終了前セーブ確認
            if (!this.attributePanel.EndEditing())
            {
                e.Cancel = true;
                return;
            }

            // SentenceEditの終了前セーブ確認
            if (!this.contextPanel.EndEditing())
            {
                e.Cancel = true;
                return;
            }

            // WordAttributeEditの終了前セーブ確認
            if (!this.wordAttributeListPanel.EndEditing())
            {
                e.Cancel = true;
                return;
            }

            // CommandPanelにスレッド中断を指示する
            this.commandPanel.Abort(false);

            // Window状態を保存する
            WindowStates.Instance.WindowLocation = new Point(
                    (int)(this.Location.X / this.currentScaleFactor.Width),
                    (int)(this.Location.Y / this.currentScaleFactor.Height));
            WindowStates.Instance.WindowSize = new Size(
                    (int)(this.Size.Width / this.currentScaleFactor.Width),
                    (int)(this.Size.Height / this.currentScaleFactor.Height));
            WindowStates.Instance.IsSplitter1Expanded = this.condPanel.CorpusPane.IsExpanded;
            WindowStates.Instance.SplitterPos2 =
                (int)(this.splitContainer2.SplitterDistance / this.currentScaleFactor.Height);
            WindowStates.Instance.SplitterPos3 =
                (int)(this.splitContainer3.SplitterDistance / this.currentScaleFactor.Height);

            // ユーザ設定を保存する
            UserSettings.GetInstance().LastCorpusGroup = m_Model.CurrentSearchConditions.SentenceCond.CorpusGroup;
            UserSettings.GetInstance().DefaultCollCond = m_Model.CurrentSearchConditions.CollCond;
        }

        delegate void BeginSentenceEditDele(Corpus c, int i, int j);

        public void OnCurrentChanged(Corpus cps, int senid)
        {
            if (cps != null && senid >= 0) {
                // StatusBarを更新する
                this.statusStrip1.SetCorpusName(cps.Name);
                this.statusStrip1.SetSentenceNo(senid);
            }
        }

        public void OnRequestDepEdit(KwicItem ki)
        {
            BeginSentenceEdit(ki.Crps, ki.SenID, ki.CenterWordID);
        }

        /// <summary>
        /// DepEditから編集する文の移動を要求されたときの処理
        /// </summary>
        /// <param name="direction"></param>
        public void OnChangeSenenceRequested(Corpus crps, int currentSenid, int direction, bool moveInKwicList)
        {
            // 最初にセーブ確認
            if (!this.dependencyEditPanel.EndEditing())
            {
                return;
            }

            Corpus c;
            int newSenId = 0;
            int center = -1;
            if (moveInKwicList)
            {
                // KwicList中の前後の文に移動
                KwicItem ki = this.kwicView.ShiftCurrent(direction);
                if (ki == null)
                {
                    c = null;
                }
                else
                {
                    c = ki.Crps;
                    newSenId = ki.SenID;
                    center = ki.CenterWordID;
                }
            }
            else
            {
                // 単純にCorpsu内の文を移動
                if (currentSenid >= 0)
                {
                    newSenId = currentSenid + direction;
                }
                else
                {
                    newSenId = direction;
                }
                c = crps;
            }
            if (c == null)
            {
                MessageBox.Show("No more sentence found.");
                return;
            }
            if (!BeginSentenceEdit(c, newSenId, center))
            {
                BeginSentenceEdit(crps, currentSenid, -1);
            }
        }

        /// <summary>
        /// タグ編集を開始する.
        /// </summary>
        /// <param name="crps">コーパス</param>
        /// <param name="senid">文番号</param>
        /// <param name="cwid">中心語（赤色表示する）不要なら-1</param>
        /// <returns></returns>
        private bool BeginSentenceEdit(Corpus crps, int senid, int cwid)
        {
            // 最初にセーブ確認
            if (!this.dependencyEditPanel.EndEditing())
            {
                return true;
            }
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            // ToolbarにProject IDを設定
            var projid = UpdateCurrentProject();

            // DependencyEditを開始する
            bool res = (bool)this.dependencyEditPanel.Invoke(
                new Func<Corpus, int, int, int, bool>(this.dependencyEditPanel.BeginSentenceEdit),
                crps, senid, cwid, projid);

            this.Cursor = oldCur;
            return res;
        }

        // DependencyEditでEdit開始/終了→ AttributeEditも同時に編集モードに移行/終了
        void HandleDependencyEditPanelEditModeChanged(OpContext obj)
        {
            if (obj != null)
            {
                this.attributePanel.StartEditMode(obj);
                this.dependencyEditPanel.AttributeChanged = () => 
                { 
                    return this.attributePanel.UpdatedSinceLastOpen || this.attributePanel.CanSave; 
                };
            }
            else
            {
                this.attributePanel.EndEditMode();
            }
        }

        // DependencyEditでSave操作が行われる → AttributeEditとの連動はTransaction.Commitで自動的に行われる.
        void HandleDependencyEditPanelSaving()
        {
            // セーブ済みフラグのみを更新.
            this.attributePanel.HandleSave();
        }


        public void OnUpdateGuidePanel(Corpus corpus, Lexeme lex)
        {
            if (lex == null)
            {
                lex = Lexeme.Empty;
            }
            this.wordAttributeListPanel.SetSource(corpus, lex);
        }

        public void OnUpdateAttributePanel(Corpus corpus, object source)
        {
            if (source != null)
            {
                this.attributePanel.SetSource(corpus, source);
            }
        }

        private void OnConditionTabChanged(object sender, EventArgs args)
        {
            ConditionsPanelType tab = this.condPanel.SelectedTab;
            bool[] commandEnableStates = null;
            switch (tab)
            {
                case ConditionsPanelType.CP_CORPUS:
                case ConditionsPanelType.CP_FILTER:
                    commandEnableStates = new bool[5] { true, true, false, false, true };
                    break;
                case ConditionsPanelType.CP_STRING:
                    commandEnableStates = new bool[5] { true, true, false, false, true };
                    break;
                case ConditionsPanelType.CP_TAG:
                case ConditionsPanelType.CP_DEP:
                    commandEnableStates = new bool[5] { true, true, true, false, true };
                    break;
                case ConditionsPanelType.CP_COLLOCATION:
                    commandEnableStates = new bool[5] { false, false, false, true, true };
                    break;
            }
            if (commandEnableStates != null)
            {
                this.commandPanel.CommandEnableStates = commandEnableStates;
            }
        }

        /// <summary>
        /// KwicViewのCurrent Panelが変更されたら、ToolbarのProj IDも書き換える.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleCurrentPanelChanged(object sender, EventArgs e)
        {
            UpdateCurrentProject();
        }

        public int CurrentProject
        {
            get
            {
                var projid = 0;
                int.TryParse(this.toolStripComboBox2.Text, out projid);
                return projid;
            }
        }

        public int GetCurrentProjectId()
        {
            // KwicViewよりProject IDを取得
            int projid = 0;
            if (this.kwicView.CurrentPanel != null && this.kwicView.CurrentPanel.GetModel() != null)
            {
                var conds = this.kwicView.CurrentPanel.GetModel().CondSeq;
                if (conds.Count > 0)
                {
                    projid = conds[0].FilterCond.TargetProjectId;
                }
            }
            return projid;
        }

        private int UpdateCurrentProject()
        {
            // ToolbarにProject IDを設定
            var projid = GetCurrentProjectId();
            if (projid >= 0)
            {
                this.toolStripComboBox2.Text = projid.ToString();
            }
            return projid;
        }

        /// <summary>
        /// 指定された検索の種類によってViewを切り替える
        /// </summary>
        /// <param name="st"></param>
        public IChaKiView ChangeView(SearchType st)
        {
            IChaKiView activeView = null;

            this.kwicView.SetVisible(false);
            this.wordListView.SetVisible(false);
            this.collocationView.SetVisible(false);

            if (st == SearchType.TagWordList || st == SearchType.DepWordList)
            {
                activeView = this.wordListView;
            }
            else if (st == SearchType.Collocation)
            {
                activeView = this.collocationView;
            }
            else 
            {
                activeView = this.kwicView;
                this.kwicView.KwicMode = (st != SearchType.SentenceSearch);
                this.kwicView.TwoLineMode = (st != SearchType.StringSearch);
            }

            if (activeView != null)
            {
                activeView.SetVisible(true);
            }
            return activeView;
        }

        public IChaKiView GetActiveView()
        {
            if (this.attributePanel.ContainsFocus)
            {
                return this.attributePanel;
            }
            if (this.contextPanel.ContainsFocus)
            {
                return this.contextPanel;
            }
            if (this.kwicView.Visible)
            {
                return this.kwicView;
            }
            if (this.wordListView.Visible)
            {
                return this.wordListView;
            }
            if (this.collocationView.Visible)
            {
                return this.collocationView;
            }
            return null;
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            bool f = this.kwicView.Focused;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        public void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        public void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                var ext = Path.GetExtension(files[0]).ToUpper();
                if (ext == ".DB")
                {
                    this.condPanel.AddCorpus(files[0], (e.Effect == DragDropEffects.Copy) ? false : true);
                }
                else if (ext == ".TXT" || ext == ".CHASEN" || ext == ".MECAB" || ext == ".CABOCHA")
                {
                    var processStartInfo = new ProcessStartInfo();
                    processStartInfo.FileName = Path.Combine(Program.ProgramDir, "Text2Corpus.exe");
                    processStartInfo.Arguments = string.Format("\"{0}\"", files[0]);
                    Process p = new Process() { StartInfo = processStartInfo, EnableRaisingEvents = true };
                    p.Exited += (_s, _e) =>
                        {
                            if (p.ExitCode == 0)
                            {
                                Invoke(new Action(() =>
                                    {
                                        Text2CorpusSettings.Load(Path.Combine(Program.SettingDir, "Text2CorpusSettings.xml"));
                                        // 生成されたDBをロードする.
                                        var newdbfile = Text2CorpusSettings.Instance.OutputFile;
                                        if (File.Exists(newdbfile))
                                        {
                                            this.condPanel.AddCorpus(newdbfile, true);
                                        }
                                    }));
                            }
                        };
                    p.Start();
                }
            }
        }
    }
}