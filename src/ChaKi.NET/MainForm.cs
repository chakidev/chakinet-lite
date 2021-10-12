using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

using ChaKi.Common;
using ChaKi.GUICommon;
using ChaKi.Panels;
using ChaKi.UICommands;
using DependencyEditSLA;
using ChaKi.Entity.Settings;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using ChaKi.Common.Settings;
using ChaKi.Views.KwicView;
using ChaKi.Views;
using ChaKi.ToolDialogs;
using System.Diagnostics;
using System.Reflection;
using ChaKi.Service.Database;
using ChaKi.Panels.ConditionsPanes;

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

        // ����Form�ɑΉ�����B���Model�I�u�W�F�N�g
        // Form�̏��L����e�q�E�B���h�E�́A����Model�̈ꕔ�����蓖�Ă��A����Update��ʒm�����B
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

            Program.MainForm = this;
            m_Model = ChaKiModel.Instance;
            m_Model.Initialize();

            #region create views
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

            // Create Views
            //this.kwicView.Dock = DockStyle.Fill;
            //this.wordListView.Dock = DockStyle.Fill;
            //this.collocationView.Dock = DockStyle.Fill;

            // ������Ԃŕ\������View
            this.ChangeView(SearchType.SentenceSearch);

            this.ResumeLayout();
            #endregion

            #region create docking panels
            // Create Docking Panels
            condPanel = new ConditionsPanel(m_Model.CurrentSearchConditions) { Dock = DockStyle.Fill };
            condPanel.FilterPane.ProjectSelector = this.toolStripComboBox2;
            this.splitContainer2.Panel1.Controls.Add(condPanel);
            this.splitContainer1.Panel1.Controls.Add(condPanel.CorpusPane);
            contextPanel = new ContextPanel();
            scriptingPanel = new ScriptingPanel();
            wordAttributeListPanel = new WordAttributeListPanel();
            commandPanel = new CommandPanel(condPanel.GetFilterButton());
            historyGuidePanel = new HistoryGuidePanel(m_Model.History);
            dependencyEditPanel = new DepEditControl() { Dock = DockStyle.Fill };
            this.splitContainer3.Panel2.Controls.Add(dependencyEditPanel);
            attributePanel = new AttributeListPanel();

            m_DockStateFile = Program.SettingDir + @"\DockState.xml";
            try
            {
                if (Program.IsIgnoreDockSetting)
                {
                    throw new FileNotFoundException();
                }
            }
            catch (FileNotFoundException)
            {
                // ���ߍ��܂ꂽ���\�[�X����f�t�H���g�h�b�L���O�ݒ��ǂݍ���
                try
                {
                }
                catch
                {
                    MessageBox.Show("Could not read default docking positions.  Resetting.");
                }
            }
            catch
            {
                MessageBox.Show("Could not restore docking positions.  Resetting.");
            }

            // Menu�͏�ɍŏ㕔�ɔz�u����.
            this.menuStrip1.Location = new Point(0, 0);
            #endregion

            #region UICommand�o�^
            //
            // �A�v���P�[�V�����̃R�}���h�Ǘ����J�j�Y���́A
            // ChaKi.UICommands�T�u�t�H���_�ɑ��݂���UICommand�N���X���s���Ă���
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

            // CommandPanel�����Callback
            commandPanel.BeginSearch = this.OnBeginSearch;
            commandPanel.NarrowSearch = this.OnBeginSearchNarrow;
            commandPanel.AppendSearch = this.OnBeginSearchAppend;
            commandPanel.BeginWordList = this.OnBeginWordList;
            commandPanel.BeginCollocation = this.OnBeginCollocation;

            // ConditionsPanel�����Callback
            condPanel.TabChanged += new EventHandler(OnConditionTabChanged);

            // KwicView�����Callback
            this.kwicView.UpdateGuidePanel += new UpdateGuidePanelDelegate(this.OnUpdateGuidePanel);
            this.kwicView.CurrentChanged += new CurrentChangedDelegate(this.OnCurrentChanged);
            this.kwicView.DepEditRequested += new RequestDepEditDelegate(this.OnRequestDepEdit);
            this.kwicView.ContextRequested += new RequestContextDelegate(this.OnRequestContext);
            this.kwicView.ContextRequested += new RequestContextDelegate(this.OnRequestSentenceTagList);
            this.kwicView.CurrentPanelChanged += new EventHandler(HandleCurrentPanelChanged);

            // WordListView�����Callback
            this.wordListView.OccurrenceRequested += new EventHandler(this.OnWordOccurenceRequested);

            // CollocationView�����callback
            this.collocationView.OccurrenceRequested += new EventHandler<SentenceIdsOccurrenceEventArgs>(HandleCollocationOccurrenceRequested);

            // HistoryGuidePanel�����Callback
            this.historyGuidePanel.HistoryNavigating +=new NavigateHistoryDelegate(this.OnHistoryNavigating);
            this.historyGuidePanel.HistorySaveRequested += new EventHandler(this.OnHistorySaveRequested);
            this.historyGuidePanel.DeleteHistoryRequested += new EventHandler(this.OnDeleteHistoryRequested);

            // DepEdit�����Callback
            this.dependencyEditPanel.UpdateGuidePanel += new UpdateGuidePanelDelegate(this.OnUpdateGuidePanel);
            this.dependencyEditPanel.UpdateAttributePanel += new UpdateAttributePanelDelegate(this.OnUpdateAttributePanel);
            this.dependencyEditPanel.ChangeSenenceRequested += new ChangeSentenceDelegate(this.OnChangeSenenceRequested);

            this.dependencyEditPanel.EditContextChanged += new Action<OpContext>(HandleDependencyEditPanelEditModeChanged);
            this.dependencyEditPanel.Saving += new Action(HandleDependencyEditPanelSaving);
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
            // DependencyEdit�̏I���O�Z�[�u���m�F
            if (!this.dependencyEditPanel.EndEditing())
            {
                e.Cancel = true;
                return;
            }
            // �I������DepEdit�̃R���g���[�������ׂăN���A���邱�ƂŁA��������y������B
            this.dependencyEditPanel.Terminate();

            // AttributeEdit�̏I���O�Z�[�u�m�F
            if (!this.attributePanel.EndEditing())
            {
                e.Cancel = true;
                return;
            }

            // SentenceEdit�̏I���O�Z�[�u�m�F
            if (!this.contextPanel.EndEditing())
            {
                e.Cancel = true;
                return;
            }

            // WordAttributeEdit�̏I���O�Z�[�u�m�F
            if (!this.wordAttributeListPanel.EndEditing())
            {
                e.Cancel = true;
                return;
            }

            // CommandPanel�ɃX���b�h���f���w������
            this.commandPanel.Abort(false);

            // Docking��Ԃ�ۑ�����
            //dockingManager.SaveConfigToFile(m_DockStateFile);

            // ���[�U�ݒ��ۑ�����
            UserSettings.GetInstance().LastCorpus = m_Model.CurrentSearchConditions.SentenceCond.Corpora;
            UserSettings.GetInstance().DefaultCollCond = m_Model.CurrentSearchConditions.CollCond;
        }

        delegate void BeginSentenceEditDele(Corpus c, int i, int j);

        public void OnCurrentChanged(Corpus cps, int senid)
        {
            if (cps != null && senid >= 0) {
                // StatusBar���X�V����
                this.statusStrip1.SetCorpusName(cps.Name);
                this.statusStrip1.SetSentenceNo(senid);
            }
        }

        public void OnRequestDepEdit(KwicItem ki)
        {
            BeginSentenceEdit(ki.Crps, ki.SenID, ki.CenterWordID);
        }

        /// <summary>
        /// DepEdit����ҏW���镶�̈ړ���v�����ꂽ�Ƃ��̏���
        /// </summary>
        /// <param name="direction"></param>
        public void OnChangeSenenceRequested(Corpus crps, int currentSenid, int direction, bool moveInKwicList)
        {
            // �ŏ��ɃZ�[�u�m�F
            if (!this.dependencyEditPanel.EndEditing())
            {
                return;
            }

            Corpus c;
            int newSenId = 0;
            int center = -1;
            if (moveInKwicList)
            {
                // KwicList���̑O��̕��Ɉړ�
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
                // �P����Corpsu���̕����ړ�
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
        /// �^�O�ҏW���J�n����.
        /// </summary>
        /// <param name="crps">�R�[�p�X</param>
        /// <param name="senid">���ԍ�</param>
        /// <param name="cwid">���S��i�ԐF�\������j�s�v�Ȃ�-1</param>
        /// <returns></returns>
        private bool BeginSentenceEdit(Corpus crps, int senid, int cwid)
        {
            // �ŏ��ɃZ�[�u�m�F
            if (!this.dependencyEditPanel.EndEditing())
            {
                return true;
            }
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            // Toolbar��Project ID��ݒ�
            var projid = UpdateCurrentProject();

            // DependencyEdit���J�n����
            bool res = (bool)this.dependencyEditPanel.Invoke(
                new Func<Corpus, int, int, int, bool>(this.dependencyEditPanel.BeginSentenceEdit),
                crps, senid, cwid, projid);

            this.Cursor = oldCur;
            return res;
        }

        // DependencyEdit��Edit�J�n/�I���� AttributeEdit�������ɕҏW���[�h�Ɉڍs/�I��
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

        // DependencyEdit��Save���삪�s���� �� AttributeEdit�Ƃ̘A����Transaction.Commit�Ŏ����I�ɍs����.
        void HandleDependencyEditPanelSaving()
        {
            // �Z�[�u�ς݃t���O�݂̂��X�V.
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
                    commandEnableStates = new bool[5] { true, true, false, false, true };
                    break;
                case ConditionsPanelType.CP_FILTER:
                    commandEnableStates = new bool[5] { true, false, false, false, true };
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
        /// KwicView��Current Panel���ύX���ꂽ��AToolbar��Proj ID������������.
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
            // KwicView���Project ID���擾
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
            // Toolbar��Project ID��ݒ�
            var projid = GetCurrentProjectId();
            if (projid >= 0)
            {
                this.toolStripComboBox2.Text = projid.ToString();
            }
            return projid;
        }

        /// <summary>
        /// �w�肳�ꂽ�����̎�ނɂ����View��؂�ւ���
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
                                        // �������ꂽDB�����[�h����.
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