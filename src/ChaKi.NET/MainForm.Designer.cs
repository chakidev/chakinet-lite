using ChaKi.GUICommon;
namespace ChaKi
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.collocationView = new ChaKi.Views.CollocationView();
            this.kwicView = new ChaKi.Views.KwicView.KwicView();
            this.wordListView = new ChaKi.Views.WordListView();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.UICFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICFileOpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICFileSaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICFileSaveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.UICFileSendToExcelCSVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.UICFileExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICEditToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICEditCutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICEditCopyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICEditPasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.UICEditDeleteAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.UICEditSelectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICEditUnselectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICEditSelectCheckedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.UICEditCheckAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICEditCheckSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICEditUncheckAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewToolbarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewStatusBarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewSearchPanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewHistoryPanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewCommandPanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewContextPanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewDependencyEditPanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewLexemePanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewAttributePanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewScriptingPanelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.UICViewKwicModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewTextModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewReloadKwicViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewViewAttributesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICViewLoadAnnotationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.UICViewAutoAdjustRowWidthToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator27 = new System.Windows.Forms.ToolStripSeparator();
            this.UICViewSplitViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchSelectCorpusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchSetSearchFiltersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchStringSearchSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchTagSearchSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchDependencySearchSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchCollocationSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.UICSearchBeginSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchBeginSearchNarrowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchBeginSearchAppendToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchBeginWordListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchBeginCollocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.UICSearchLoadSearchConditionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchSaveCurrentSearchConditionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchResetAllSearchSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
            this.UICSearchSearchInViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator21 = new System.Windows.Forms.ToolStripSeparator();
            this.UICSearchExamineLastSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICSearchExamineLastEditToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICFormatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICFormatShiftPivotLeftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICFormatShiftPivotRightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.UICFormatHilightPreviousWordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICFormatHilightNextWordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICToolsText2CorpusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator25 = new System.Windows.Forms.ToolStripSeparator();
            this.UICToolsCreateSQLiteCorpusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICToolsCreateMySQLCorpusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICToolsCreateDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
            this.UICToolsEditTagSetDefinitionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.UICToolsTextFormatterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICOptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICOptionsSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICOptionsPropertyBoxSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICOptionsWordColorSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICOptionsTagAppearanceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICOptionsDictionarySettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICHelpHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICHelpAboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.UICFileOpenToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICFileSaveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICFileSendToExcelCSVToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.UICEditCutToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICEditCopyToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICEditPasteToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICEditDeleteAllToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
            this.UICSearchBeginSearchToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICSearchBeginSearchNarrowToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICSearchBeginSearchAppendToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICSearchBeginWordListToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICSearchBeginCollocationToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator24 = new System.Windows.Forms.ToolStripSeparator();
            this.UICSearchLoadSearchConditionsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICSearchSaveCurrentSearchConditionsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICSearchResetAllSearchSettingsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.UICViewKwicModeToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICViewTextModeToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICViewViewAttributesToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICViewReloadKwicViewToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICViewLoadAnnotationsToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICViewFreezeUpdateToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICViewFullScreenToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator23 = new System.Windows.Forms.ToolStripSeparator();
            this.UICEditChangeLexemeToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
            this.UICFormatShiftPivotLeftToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICFormatShiftPivotRightToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
            this.UICFormatHilightPreviousWordToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.UICFormatHilightNextWordToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
            this.UICViewAutoAdjustRowWidthToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
            this.UICSearchSearchInViewToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator20 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.UICViewGotoSentenceToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator26 = new System.Windows.Forms.ToolStripSeparator();
            this.UICViewSplitViewToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripComboBox2 = new ChaKi.Panels.ProjectSelector();
            this.toolStripSeparator22 = new System.Windows.Forms.ToolStripSeparator();
            this.UICHelpHelpToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.BottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.TopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.RightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.LeftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.ContentPanel = new System.Windows.Forms.ToolStripContentPanel();
            this.statusStrip1 = new ChaKi.GUICommon.ChakiStatusStrip();
            this.UICViewFullScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICVersioncontrolCommitToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator28 = new System.Windows.Forms.ToolStripSeparator();
            this.UICDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICDictionaryExportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UICDictionaryExportMWEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            resources.ApplyResources(this.toolStripContainer1.ContentPanel, "toolStripContainer1.ContentPanel");
            this.toolStripContainer1.ContentPanel.Controls.Add(this.collocationView);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.kwicView);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.wordListView);
            resources.ApplyResources(this.toolStripContainer1, "toolStripContainer1");
            this.toolStripContainer1.Name = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.menuStrip1);
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip);
            // 
            // collocationView
            // 
            resources.ApplyResources(this.collocationView, "collocationView");
            this.collocationView.Name = "collocationView";
            // 
            // kwicView
            // 
            resources.ApplyResources(this.kwicView, "kwicView");
            this.kwicView.IsViewSplitted = false;
            this.kwicView.KwicMode = false;
            this.kwicView.Name = "kwicView";
            this.kwicView.SelectedLine = -1;
            this.kwicView.TwoLineMode = true;
            this.kwicView.UpdateFrozen = false;
            // 
            // wordListView
            // 
            this.wordListView.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.wordListView, "wordListView");
            this.wordListView.Name = "wordListView";
            this.wordListView.UpdateFrozen = false;
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICFileToolStripMenuItem,
            this.UICEditToolStripMenuItem,
            this.UICViewToolStripMenuItem,
            this.UICSearchToolStripMenuItem,
            this.UICFormatToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.UICDictionaryToolStripMenuItem,
            this.UICOptionsToolStripMenuItem,
            this.UICHelpToolStripMenuItem});
            this.menuStrip1.Name = "menuStrip1";
            // 
            // UICFileToolStripMenuItem
            // 
            this.UICFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICFileOpenToolStripMenuItem,
            this.UICFileSaveToolStripMenuItem,
            this.UICFileSaveAsToolStripMenuItem,
            this.toolStripSeparator10,
            this.UICFileSendToExcelCSVToolStripMenuItem,
            this.toolStripSeparator11,
            this.UICFileExitToolStripMenuItem});
            this.UICFileToolStripMenuItem.Name = "UICFileToolStripMenuItem";
            resources.ApplyResources(this.UICFileToolStripMenuItem, "UICFileToolStripMenuItem");
            // 
            // UICFileOpenToolStripMenuItem
            // 
            resources.ApplyResources(this.UICFileOpenToolStripMenuItem, "UICFileOpenToolStripMenuItem");
            this.UICFileOpenToolStripMenuItem.Name = "UICFileOpenToolStripMenuItem";
            // 
            // UICFileSaveToolStripMenuItem
            // 
            resources.ApplyResources(this.UICFileSaveToolStripMenuItem, "UICFileSaveToolStripMenuItem");
            this.UICFileSaveToolStripMenuItem.Name = "UICFileSaveToolStripMenuItem";
            // 
            // UICFileSaveAsToolStripMenuItem
            // 
            this.UICFileSaveAsToolStripMenuItem.Name = "UICFileSaveAsToolStripMenuItem";
            resources.ApplyResources(this.UICFileSaveAsToolStripMenuItem, "UICFileSaveAsToolStripMenuItem");
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            resources.ApplyResources(this.toolStripSeparator10, "toolStripSeparator10");
            // 
            // UICFileSendToExcelCSVToolStripMenuItem
            // 
            this.UICFileSendToExcelCSVToolStripMenuItem.Image = global::ChaKi.Properties.Resources.ToExcel;
            this.UICFileSendToExcelCSVToolStripMenuItem.Name = "UICFileSendToExcelCSVToolStripMenuItem";
            resources.ApplyResources(this.UICFileSendToExcelCSVToolStripMenuItem, "UICFileSendToExcelCSVToolStripMenuItem");
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            resources.ApplyResources(this.toolStripSeparator11, "toolStripSeparator11");
            // 
            // UICFileExitToolStripMenuItem
            // 
            this.UICFileExitToolStripMenuItem.Name = "UICFileExitToolStripMenuItem";
            resources.ApplyResources(this.UICFileExitToolStripMenuItem, "UICFileExitToolStripMenuItem");
            // 
            // UICEditToolStripMenuItem
            // 
            this.UICEditToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICEditCutToolStripMenuItem,
            this.UICEditCopyToolStripMenuItem,
            this.UICEditPasteToolStripMenuItem,
            this.toolStripSeparator3,
            this.UICEditDeleteAllToolStripMenuItem,
            this.toolStripSeparator1,
            this.UICEditSelectAllToolStripMenuItem,
            this.UICEditUnselectAllToolStripMenuItem,
            this.UICEditSelectCheckedToolStripMenuItem,
            this.toolStripSeparator4,
            this.UICEditCheckAllToolStripMenuItem,
            this.UICEditCheckSelectedToolStripMenuItem,
            this.UICEditUncheckAllToolStripMenuItem});
            this.UICEditToolStripMenuItem.Name = "UICEditToolStripMenuItem";
            resources.ApplyResources(this.UICEditToolStripMenuItem, "UICEditToolStripMenuItem");
            // 
            // UICEditCutToolStripMenuItem
            // 
            resources.ApplyResources(this.UICEditCutToolStripMenuItem, "UICEditCutToolStripMenuItem");
            this.UICEditCutToolStripMenuItem.Name = "UICEditCutToolStripMenuItem";
            // 
            // UICEditCopyToolStripMenuItem
            // 
            resources.ApplyResources(this.UICEditCopyToolStripMenuItem, "UICEditCopyToolStripMenuItem");
            this.UICEditCopyToolStripMenuItem.Name = "UICEditCopyToolStripMenuItem";
            // 
            // UICEditPasteToolStripMenuItem
            // 
            resources.ApplyResources(this.UICEditPasteToolStripMenuItem, "UICEditPasteToolStripMenuItem");
            this.UICEditPasteToolStripMenuItem.Name = "UICEditPasteToolStripMenuItem";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // UICEditDeleteAllToolStripMenuItem
            // 
            resources.ApplyResources(this.UICEditDeleteAllToolStripMenuItem, "UICEditDeleteAllToolStripMenuItem");
            this.UICEditDeleteAllToolStripMenuItem.Name = "UICEditDeleteAllToolStripMenuItem";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // UICEditSelectAllToolStripMenuItem
            // 
            this.UICEditSelectAllToolStripMenuItem.Name = "UICEditSelectAllToolStripMenuItem";
            resources.ApplyResources(this.UICEditSelectAllToolStripMenuItem, "UICEditSelectAllToolStripMenuItem");
            // 
            // UICEditUnselectAllToolStripMenuItem
            // 
            this.UICEditUnselectAllToolStripMenuItem.Name = "UICEditUnselectAllToolStripMenuItem";
            resources.ApplyResources(this.UICEditUnselectAllToolStripMenuItem, "UICEditUnselectAllToolStripMenuItem");
            // 
            // UICEditSelectCheckedToolStripMenuItem
            // 
            this.UICEditSelectCheckedToolStripMenuItem.Name = "UICEditSelectCheckedToolStripMenuItem";
            resources.ApplyResources(this.UICEditSelectCheckedToolStripMenuItem, "UICEditSelectCheckedToolStripMenuItem");
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // UICEditCheckAllToolStripMenuItem
            // 
            this.UICEditCheckAllToolStripMenuItem.Name = "UICEditCheckAllToolStripMenuItem";
            resources.ApplyResources(this.UICEditCheckAllToolStripMenuItem, "UICEditCheckAllToolStripMenuItem");
            // 
            // UICEditCheckSelectedToolStripMenuItem
            // 
            this.UICEditCheckSelectedToolStripMenuItem.Name = "UICEditCheckSelectedToolStripMenuItem";
            resources.ApplyResources(this.UICEditCheckSelectedToolStripMenuItem, "UICEditCheckSelectedToolStripMenuItem");
            // 
            // UICEditUncheckAllToolStripMenuItem
            // 
            this.UICEditUncheckAllToolStripMenuItem.Name = "UICEditUncheckAllToolStripMenuItem";
            resources.ApplyResources(this.UICEditUncheckAllToolStripMenuItem, "UICEditUncheckAllToolStripMenuItem");
            // 
            // UICViewToolStripMenuItem
            // 
            this.UICViewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICViewToolbarToolStripMenuItem,
            this.UICViewStatusBarToolStripMenuItem,
            this.UICViewSearchPanelToolStripMenuItem,
            this.UICViewHistoryPanelToolStripMenuItem,
            this.UICViewCommandPanelToolStripMenuItem,
            this.UICViewContextPanelToolStripMenuItem,
            this.UICViewDependencyEditPanelToolStripMenuItem,
            this.UICViewLexemePanelToolStripMenuItem,
            this.UICViewAttributePanelToolStripMenuItem,
            this.UICViewScriptingPanelToolStripMenuItem,
            this.toolStripSeparator5,
            this.UICViewKwicModeToolStripMenuItem,
            this.UICViewTextModeToolStripMenuItem,
            this.UICViewReloadKwicViewToolStripMenuItem,
            this.UICViewViewAttributesToolStripMenuItem,
            this.UICViewLoadAnnotationsToolStripMenuItem,
            this.toolStripSeparator6,
            this.UICViewAutoAdjustRowWidthToolStripMenuItem,
            this.toolStripSeparator27,
            this.UICViewSplitViewToolStripMenuItem,
            this.UICViewFullScreenToolStripMenuItem});
            this.UICViewToolStripMenuItem.Name = "UICViewToolStripMenuItem";
            resources.ApplyResources(this.UICViewToolStripMenuItem, "UICViewToolStripMenuItem");
            // 
            // UICViewToolbarToolStripMenuItem
            // 
            this.UICViewToolbarToolStripMenuItem.Checked = true;
            this.UICViewToolbarToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewToolbarToolStripMenuItem.Name = "UICViewToolbarToolStripMenuItem";
            resources.ApplyResources(this.UICViewToolbarToolStripMenuItem, "UICViewToolbarToolStripMenuItem");
            // 
            // UICViewStatusBarToolStripMenuItem
            // 
            this.UICViewStatusBarToolStripMenuItem.Checked = true;
            this.UICViewStatusBarToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewStatusBarToolStripMenuItem.Name = "UICViewStatusBarToolStripMenuItem";
            resources.ApplyResources(this.UICViewStatusBarToolStripMenuItem, "UICViewStatusBarToolStripMenuItem");
            // 
            // UICViewSearchPanelToolStripMenuItem
            // 
            this.UICViewSearchPanelToolStripMenuItem.Checked = true;
            this.UICViewSearchPanelToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewSearchPanelToolStripMenuItem.Name = "UICViewSearchPanelToolStripMenuItem";
            resources.ApplyResources(this.UICViewSearchPanelToolStripMenuItem, "UICViewSearchPanelToolStripMenuItem");
            // 
            // UICViewHistoryPanelToolStripMenuItem
            // 
            this.UICViewHistoryPanelToolStripMenuItem.Checked = true;
            this.UICViewHistoryPanelToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewHistoryPanelToolStripMenuItem.Name = "UICViewHistoryPanelToolStripMenuItem";
            resources.ApplyResources(this.UICViewHistoryPanelToolStripMenuItem, "UICViewHistoryPanelToolStripMenuItem");
            // 
            // UICViewCommandPanelToolStripMenuItem
            // 
            this.UICViewCommandPanelToolStripMenuItem.Checked = true;
            this.UICViewCommandPanelToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewCommandPanelToolStripMenuItem.Name = "UICViewCommandPanelToolStripMenuItem";
            resources.ApplyResources(this.UICViewCommandPanelToolStripMenuItem, "UICViewCommandPanelToolStripMenuItem");
            // 
            // UICViewContextPanelToolStripMenuItem
            // 
            this.UICViewContextPanelToolStripMenuItem.Checked = true;
            this.UICViewContextPanelToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewContextPanelToolStripMenuItem.Name = "UICViewContextPanelToolStripMenuItem";
            resources.ApplyResources(this.UICViewContextPanelToolStripMenuItem, "UICViewContextPanelToolStripMenuItem");
            // 
            // UICViewDependencyEditPanelToolStripMenuItem
            // 
            this.UICViewDependencyEditPanelToolStripMenuItem.Checked = true;
            this.UICViewDependencyEditPanelToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewDependencyEditPanelToolStripMenuItem.Name = "UICViewDependencyEditPanelToolStripMenuItem";
            resources.ApplyResources(this.UICViewDependencyEditPanelToolStripMenuItem, "UICViewDependencyEditPanelToolStripMenuItem");
            // 
            // UICViewLexemePanelToolStripMenuItem
            // 
            this.UICViewLexemePanelToolStripMenuItem.Checked = true;
            this.UICViewLexemePanelToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewLexemePanelToolStripMenuItem.Name = "UICViewLexemePanelToolStripMenuItem";
            resources.ApplyResources(this.UICViewLexemePanelToolStripMenuItem, "UICViewLexemePanelToolStripMenuItem");
            // 
            // UICViewAttributePanelToolStripMenuItem
            // 
            this.UICViewAttributePanelToolStripMenuItem.Checked = true;
            this.UICViewAttributePanelToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewAttributePanelToolStripMenuItem.Name = "UICViewAttributePanelToolStripMenuItem";
            resources.ApplyResources(this.UICViewAttributePanelToolStripMenuItem, "UICViewAttributePanelToolStripMenuItem");
            // 
            // UICViewScriptingPanelToolStripMenuItem
            // 
            this.UICViewScriptingPanelToolStripMenuItem.Name = "UICViewScriptingPanelToolStripMenuItem";
            resources.ApplyResources(this.UICViewScriptingPanelToolStripMenuItem, "UICViewScriptingPanelToolStripMenuItem");
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            // 
            // UICViewKwicModeToolStripMenuItem
            // 
            resources.ApplyResources(this.UICViewKwicModeToolStripMenuItem, "UICViewKwicModeToolStripMenuItem");
            this.UICViewKwicModeToolStripMenuItem.Name = "UICViewKwicModeToolStripMenuItem";
            // 
            // UICViewTextModeToolStripMenuItem
            // 
            resources.ApplyResources(this.UICViewTextModeToolStripMenuItem, "UICViewTextModeToolStripMenuItem");
            this.UICViewTextModeToolStripMenuItem.Name = "UICViewTextModeToolStripMenuItem";
            // 
            // UICViewReloadKwicViewToolStripMenuItem
            // 
            resources.ApplyResources(this.UICViewReloadKwicViewToolStripMenuItem, "UICViewReloadKwicViewToolStripMenuItem");
            this.UICViewReloadKwicViewToolStripMenuItem.Name = "UICViewReloadKwicViewToolStripMenuItem";
            // 
            // UICViewViewAttributesToolStripMenuItem
            // 
            this.UICViewViewAttributesToolStripMenuItem.Checked = true;
            this.UICViewViewAttributesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            resources.ApplyResources(this.UICViewViewAttributesToolStripMenuItem, "UICViewViewAttributesToolStripMenuItem");
            this.UICViewViewAttributesToolStripMenuItem.Name = "UICViewViewAttributesToolStripMenuItem";
            // 
            // UICViewLoadAnnotationsToolStripMenuItem
            // 
            resources.ApplyResources(this.UICViewLoadAnnotationsToolStripMenuItem, "UICViewLoadAnnotationsToolStripMenuItem");
            this.UICViewLoadAnnotationsToolStripMenuItem.Name = "UICViewLoadAnnotationsToolStripMenuItem";
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // UICViewAutoAdjustRowWidthToolStripMenuItem
            // 
            resources.ApplyResources(this.UICViewAutoAdjustRowWidthToolStripMenuItem, "UICViewAutoAdjustRowWidthToolStripMenuItem");
            this.UICViewAutoAdjustRowWidthToolStripMenuItem.Name = "UICViewAutoAdjustRowWidthToolStripMenuItem";
            // 
            // toolStripSeparator27
            // 
            this.toolStripSeparator27.Name = "toolStripSeparator27";
            resources.ApplyResources(this.toolStripSeparator27, "toolStripSeparator27");
            // 
            // UICViewSplitViewToolStripMenuItem
            // 
            this.UICViewSplitViewToolStripMenuItem.Image = global::ChaKi.Properties.Resources.TileWindowsHorizontally;
            this.UICViewSplitViewToolStripMenuItem.Name = "UICViewSplitViewToolStripMenuItem";
            resources.ApplyResources(this.UICViewSplitViewToolStripMenuItem, "UICViewSplitViewToolStripMenuItem");
            // 
            // UICSearchToolStripMenuItem
            // 
            this.UICSearchToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICSearchSelectCorpusToolStripMenuItem,
            this.UICSearchSetSearchFiltersToolStripMenuItem,
            this.UICSearchStringSearchSettingsToolStripMenuItem,
            this.UICSearchTagSearchSettingsToolStripMenuItem,
            this.UICSearchDependencySearchSettingsToolStripMenuItem,
            this.UICSearchCollocationSettingsToolStripMenuItem,
            this.toolStripSeparator7,
            this.UICSearchBeginSearchToolStripMenuItem,
            this.UICSearchBeginSearchNarrowToolStripMenuItem,
            this.UICSearchBeginSearchAppendToolStripMenuItem,
            this.UICSearchBeginWordListToolStripMenuItem,
            this.UICSearchBeginCollocationToolStripMenuItem,
            this.toolStripSeparator8,
            this.UICSearchLoadSearchConditionsToolStripMenuItem,
            this.UICSearchSaveCurrentSearchConditionsToolStripMenuItem,
            this.UICSearchResetAllSearchSettingsToolStripMenuItem,
            this.toolStripSeparator18,
            this.UICSearchSearchInViewToolStripMenuItem,
            this.toolStripSeparator21,
            this.UICSearchExamineLastSearchToolStripMenuItem,
            this.UICSearchExamineLastEditToolStripMenuItem});
            this.UICSearchToolStripMenuItem.Name = "UICSearchToolStripMenuItem";
            resources.ApplyResources(this.UICSearchToolStripMenuItem, "UICSearchToolStripMenuItem");
            // 
            // UICSearchSelectCorpusToolStripMenuItem
            // 
            this.UICSearchSelectCorpusToolStripMenuItem.Name = "UICSearchSelectCorpusToolStripMenuItem";
            resources.ApplyResources(this.UICSearchSelectCorpusToolStripMenuItem, "UICSearchSelectCorpusToolStripMenuItem");
            // 
            // UICSearchSetSearchFiltersToolStripMenuItem
            // 
            this.UICSearchSetSearchFiltersToolStripMenuItem.Name = "UICSearchSetSearchFiltersToolStripMenuItem";
            resources.ApplyResources(this.UICSearchSetSearchFiltersToolStripMenuItem, "UICSearchSetSearchFiltersToolStripMenuItem");
            // 
            // UICSearchStringSearchSettingsToolStripMenuItem
            // 
            this.UICSearchStringSearchSettingsToolStripMenuItem.Name = "UICSearchStringSearchSettingsToolStripMenuItem";
            resources.ApplyResources(this.UICSearchStringSearchSettingsToolStripMenuItem, "UICSearchStringSearchSettingsToolStripMenuItem");
            // 
            // UICSearchTagSearchSettingsToolStripMenuItem
            // 
            this.UICSearchTagSearchSettingsToolStripMenuItem.Name = "UICSearchTagSearchSettingsToolStripMenuItem";
            resources.ApplyResources(this.UICSearchTagSearchSettingsToolStripMenuItem, "UICSearchTagSearchSettingsToolStripMenuItem");
            // 
            // UICSearchDependencySearchSettingsToolStripMenuItem
            // 
            this.UICSearchDependencySearchSettingsToolStripMenuItem.Name = "UICSearchDependencySearchSettingsToolStripMenuItem";
            resources.ApplyResources(this.UICSearchDependencySearchSettingsToolStripMenuItem, "UICSearchDependencySearchSettingsToolStripMenuItem");
            // 
            // UICSearchCollocationSettingsToolStripMenuItem
            // 
            this.UICSearchCollocationSettingsToolStripMenuItem.Name = "UICSearchCollocationSettingsToolStripMenuItem";
            resources.ApplyResources(this.UICSearchCollocationSettingsToolStripMenuItem, "UICSearchCollocationSettingsToolStripMenuItem");
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // UICSearchBeginSearchToolStripMenuItem
            // 
            resources.ApplyResources(this.UICSearchBeginSearchToolStripMenuItem, "UICSearchBeginSearchToolStripMenuItem");
            this.UICSearchBeginSearchToolStripMenuItem.Name = "UICSearchBeginSearchToolStripMenuItem";
            // 
            // UICSearchBeginSearchNarrowToolStripMenuItem
            // 
            this.UICSearchBeginSearchNarrowToolStripMenuItem.Image = global::ChaKi.Properties.Resources.NarrowSearch;
            this.UICSearchBeginSearchNarrowToolStripMenuItem.Name = "UICSearchBeginSearchNarrowToolStripMenuItem";
            resources.ApplyResources(this.UICSearchBeginSearchNarrowToolStripMenuItem, "UICSearchBeginSearchNarrowToolStripMenuItem");
            // 
            // UICSearchBeginSearchAppendToolStripMenuItem
            // 
            this.UICSearchBeginSearchAppendToolStripMenuItem.Image = global::ChaKi.Properties.Resources.AppendSearch;
            this.UICSearchBeginSearchAppendToolStripMenuItem.Name = "UICSearchBeginSearchAppendToolStripMenuItem";
            resources.ApplyResources(this.UICSearchBeginSearchAppendToolStripMenuItem, "UICSearchBeginSearchAppendToolStripMenuItem");
            // 
            // UICSearchBeginWordListToolStripMenuItem
            // 
            resources.ApplyResources(this.UICSearchBeginWordListToolStripMenuItem, "UICSearchBeginWordListToolStripMenuItem");
            this.UICSearchBeginWordListToolStripMenuItem.Name = "UICSearchBeginWordListToolStripMenuItem";
            // 
            // UICSearchBeginCollocationToolStripMenuItem
            // 
            resources.ApplyResources(this.UICSearchBeginCollocationToolStripMenuItem, "UICSearchBeginCollocationToolStripMenuItem");
            this.UICSearchBeginCollocationToolStripMenuItem.Name = "UICSearchBeginCollocationToolStripMenuItem";
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // UICSearchLoadSearchConditionsToolStripMenuItem
            // 
            resources.ApplyResources(this.UICSearchLoadSearchConditionsToolStripMenuItem, "UICSearchLoadSearchConditionsToolStripMenuItem");
            this.UICSearchLoadSearchConditionsToolStripMenuItem.Name = "UICSearchLoadSearchConditionsToolStripMenuItem";
            // 
            // UICSearchSaveCurrentSearchConditionsToolStripMenuItem
            // 
            resources.ApplyResources(this.UICSearchSaveCurrentSearchConditionsToolStripMenuItem, "UICSearchSaveCurrentSearchConditionsToolStripMenuItem");
            this.UICSearchSaveCurrentSearchConditionsToolStripMenuItem.Name = "UICSearchSaveCurrentSearchConditionsToolStripMenuItem";
            // 
            // UICSearchResetAllSearchSettingsToolStripMenuItem
            // 
            resources.ApplyResources(this.UICSearchResetAllSearchSettingsToolStripMenuItem, "UICSearchResetAllSearchSettingsToolStripMenuItem");
            this.UICSearchResetAllSearchSettingsToolStripMenuItem.Name = "UICSearchResetAllSearchSettingsToolStripMenuItem";
            // 
            // toolStripSeparator18
            // 
            this.toolStripSeparator18.Name = "toolStripSeparator18";
            resources.ApplyResources(this.toolStripSeparator18, "toolStripSeparator18");
            // 
            // UICSearchSearchInViewToolStripMenuItem
            // 
            this.UICSearchSearchInViewToolStripMenuItem.Name = "UICSearchSearchInViewToolStripMenuItem";
            resources.ApplyResources(this.UICSearchSearchInViewToolStripMenuItem, "UICSearchSearchInViewToolStripMenuItem");
            // 
            // toolStripSeparator21
            // 
            this.toolStripSeparator21.Name = "toolStripSeparator21";
            resources.ApplyResources(this.toolStripSeparator21, "toolStripSeparator21");
            // 
            // UICSearchExamineLastSearchToolStripMenuItem
            // 
            this.UICSearchExamineLastSearchToolStripMenuItem.Name = "UICSearchExamineLastSearchToolStripMenuItem";
            resources.ApplyResources(this.UICSearchExamineLastSearchToolStripMenuItem, "UICSearchExamineLastSearchToolStripMenuItem");
            // 
            // UICSearchExamineLastEditToolStripMenuItem
            // 
            this.UICSearchExamineLastEditToolStripMenuItem.Name = "UICSearchExamineLastEditToolStripMenuItem";
            resources.ApplyResources(this.UICSearchExamineLastEditToolStripMenuItem, "UICSearchExamineLastEditToolStripMenuItem");
            // 
            // UICFormatToolStripMenuItem
            // 
            this.UICFormatToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICFormatShiftPivotLeftToolStripMenuItem,
            this.UICFormatShiftPivotRightToolStripMenuItem,
            this.toolStripSeparator9,
            this.UICFormatHilightPreviousWordToolStripMenuItem,
            this.UICFormatHilightNextWordToolStripMenuItem});
            this.UICFormatToolStripMenuItem.Name = "UICFormatToolStripMenuItem";
            resources.ApplyResources(this.UICFormatToolStripMenuItem, "UICFormatToolStripMenuItem");
            // 
            // UICFormatShiftPivotLeftToolStripMenuItem
            // 
            resources.ApplyResources(this.UICFormatShiftPivotLeftToolStripMenuItem, "UICFormatShiftPivotLeftToolStripMenuItem");
            this.UICFormatShiftPivotLeftToolStripMenuItem.Name = "UICFormatShiftPivotLeftToolStripMenuItem";
            // 
            // UICFormatShiftPivotRightToolStripMenuItem
            // 
            resources.ApplyResources(this.UICFormatShiftPivotRightToolStripMenuItem, "UICFormatShiftPivotRightToolStripMenuItem");
            this.UICFormatShiftPivotRightToolStripMenuItem.Name = "UICFormatShiftPivotRightToolStripMenuItem";
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            resources.ApplyResources(this.toolStripSeparator9, "toolStripSeparator9");
            // 
            // UICFormatHilightPreviousWordToolStripMenuItem
            // 
            resources.ApplyResources(this.UICFormatHilightPreviousWordToolStripMenuItem, "UICFormatHilightPreviousWordToolStripMenuItem");
            this.UICFormatHilightPreviousWordToolStripMenuItem.Name = "UICFormatHilightPreviousWordToolStripMenuItem";
            // 
            // UICFormatHilightNextWordToolStripMenuItem
            // 
            resources.ApplyResources(this.UICFormatHilightNextWordToolStripMenuItem, "UICFormatHilightNextWordToolStripMenuItem");
            this.UICFormatHilightNextWordToolStripMenuItem.Name = "UICFormatHilightNextWordToolStripMenuItem";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICToolsText2CorpusToolStripMenuItem,
            this.toolStripSeparator25,
            this.UICToolsCreateSQLiteCorpusToolStripMenuItem,
            this.UICToolsCreateMySQLCorpusToolStripMenuItem,
            this.UICToolsCreateDictionaryToolStripMenuItem,
            this.toolStripSeparator19,
            this.UICToolsEditTagSetDefinitionsToolStripMenuItem,
            this.toolStripSeparator12,
            this.UICToolsTextFormatterToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            resources.ApplyResources(this.toolsToolStripMenuItem, "toolsToolStripMenuItem");
            // 
            // UICToolsText2CorpusToolStripMenuItem
            // 
            this.UICToolsText2CorpusToolStripMenuItem.Name = "UICToolsText2CorpusToolStripMenuItem";
            resources.ApplyResources(this.UICToolsText2CorpusToolStripMenuItem, "UICToolsText2CorpusToolStripMenuItem");
            // 
            // toolStripSeparator25
            // 
            this.toolStripSeparator25.Name = "toolStripSeparator25";
            resources.ApplyResources(this.toolStripSeparator25, "toolStripSeparator25");
            // 
            // UICToolsCreateSQLiteCorpusToolStripMenuItem
            // 
            this.UICToolsCreateSQLiteCorpusToolStripMenuItem.Name = "UICToolsCreateSQLiteCorpusToolStripMenuItem";
            resources.ApplyResources(this.UICToolsCreateSQLiteCorpusToolStripMenuItem, "UICToolsCreateSQLiteCorpusToolStripMenuItem");
            // 
            // UICToolsCreateMySQLCorpusToolStripMenuItem
            // 
            this.UICToolsCreateMySQLCorpusToolStripMenuItem.Name = "UICToolsCreateMySQLCorpusToolStripMenuItem";
            resources.ApplyResources(this.UICToolsCreateMySQLCorpusToolStripMenuItem, "UICToolsCreateMySQLCorpusToolStripMenuItem");
            // 
            // UICToolsCreateDictionaryToolStripMenuItem
            // 
            this.UICToolsCreateDictionaryToolStripMenuItem.Name = "UICToolsCreateDictionaryToolStripMenuItem";
            resources.ApplyResources(this.UICToolsCreateDictionaryToolStripMenuItem, "UICToolsCreateDictionaryToolStripMenuItem");
            // 
            // toolStripSeparator19
            // 
            this.toolStripSeparator19.Name = "toolStripSeparator19";
            resources.ApplyResources(this.toolStripSeparator19, "toolStripSeparator19");
            // 
            // UICToolsEditTagSetDefinitionsToolStripMenuItem
            // 
            this.UICToolsEditTagSetDefinitionsToolStripMenuItem.Name = "UICToolsEditTagSetDefinitionsToolStripMenuItem";
            resources.ApplyResources(this.UICToolsEditTagSetDefinitionsToolStripMenuItem, "UICToolsEditTagSetDefinitionsToolStripMenuItem");
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            resources.ApplyResources(this.toolStripSeparator12, "toolStripSeparator12");
            // 
            // UICToolsTextFormatterToolStripMenuItem
            // 
            this.UICToolsTextFormatterToolStripMenuItem.Name = "UICToolsTextFormatterToolStripMenuItem";
            resources.ApplyResources(this.UICToolsTextFormatterToolStripMenuItem, "UICToolsTextFormatterToolStripMenuItem");

            // 
            // UICDictionaryToolStripMenuItem
            // 
            this.UICDictionaryToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICDictionaryExportToolStripMenuItem,
            this.UICDictionaryExportMWEToolStripMenuItem,
            });
            this.UICDictionaryToolStripMenuItem.Name = "UICDictionaryToolStripMenuItem";
            resources.ApplyResources(this.UICDictionaryToolStripMenuItem, "UICDictionaryToolStripMenuItem");
            // 
            // UICDictionaryExportToolStripMenuItem
            // 
            resources.ApplyResources(this.UICDictionaryExportToolStripMenuItem, "UICDictionaryExportToolStripMenuItem");
            this.UICDictionaryExportToolStripMenuItem.Name = "UICDictionaryExportToolStripMenuItem";
            // 
            // UICDictionaryExportMWEToolStripMenuItem
            // 
            resources.ApplyResources(this.UICDictionaryExportMWEToolStripMenuItem, "UICDictionaryExportMWEToolStripMenuItem");
            this.UICDictionaryExportMWEToolStripMenuItem.Name = "UICDictionaryExportMWEToolStripMenuItem";
            // 
            // UICOptionsToolStripMenuItem
            // 
            this.UICOptionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICOptionsSettingsToolStripMenuItem,
            this.UICOptionsPropertyBoxSettingsToolStripMenuItem,
            this.UICOptionsWordColorSettingsToolStripMenuItem,
            this.UICOptionsTagAppearanceToolStripMenuItem,
            this.UICOptionsDictionarySettingsToolStripMenuItem});
            this.UICOptionsToolStripMenuItem.Name = "UICOptionsToolStripMenuItem";
            resources.ApplyResources(this.UICOptionsToolStripMenuItem, "UICOptionsToolStripMenuItem");
            // 
            // UICOptionsSettingsToolStripMenuItem
            // 
            this.UICOptionsSettingsToolStripMenuItem.Name = "UICOptionsSettingsToolStripMenuItem";
            resources.ApplyResources(this.UICOptionsSettingsToolStripMenuItem, "UICOptionsSettingsToolStripMenuItem");
            // 
            // UICOptionsPropertyBoxSettingsToolStripMenuItem
            // 
            this.UICOptionsPropertyBoxSettingsToolStripMenuItem.Name = "UICOptionsPropertyBoxSettingsToolStripMenuItem";
            resources.ApplyResources(this.UICOptionsPropertyBoxSettingsToolStripMenuItem, "UICOptionsPropertyBoxSettingsToolStripMenuItem");
            // 
            // UICOptionsWordColorSettingsToolStripMenuItem
            // 
            this.UICOptionsWordColorSettingsToolStripMenuItem.Name = "UICOptionsWordColorSettingsToolStripMenuItem";
            resources.ApplyResources(this.UICOptionsWordColorSettingsToolStripMenuItem, "UICOptionsWordColorSettingsToolStripMenuItem");
            // 
            // UICOptionsTagAppearanceToolStripMenuItem
            // 
            this.UICOptionsTagAppearanceToolStripMenuItem.Name = "UICOptionsTagAppearanceToolStripMenuItem";
            resources.ApplyResources(this.UICOptionsTagAppearanceToolStripMenuItem, "UICOptionsTagAppearanceToolStripMenuItem");
            // 
            // UICOptionsDictionarySettingsToolStripMenuItem
            // 
            this.UICOptionsDictionarySettingsToolStripMenuItem.Name = "UICOptionsDictionarySettingsToolStripMenuItem";
            resources.ApplyResources(this.UICOptionsDictionarySettingsToolStripMenuItem, "UICOptionsDictionarySettingsToolStripMenuItem");
            // 
            // UICHelpToolStripMenuItem
            // 
            this.UICHelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICHelpHelpToolStripMenuItem,
            this.UICHelpAboutToolStripMenuItem});
            this.UICHelpToolStripMenuItem.Name = "UICHelpToolStripMenuItem";
            resources.ApplyResources(this.UICHelpToolStripMenuItem, "UICHelpToolStripMenuItem");
            // 
            // UICHelpHelpToolStripMenuItem
            // 
            resources.ApplyResources(this.UICHelpHelpToolStripMenuItem, "UICHelpHelpToolStripMenuItem");
            this.UICHelpHelpToolStripMenuItem.Name = "UICHelpHelpToolStripMenuItem";
            // 
            // UICHelpAboutToolStripMenuItem
            // 
            resources.ApplyResources(this.UICHelpAboutToolStripMenuItem, "UICHelpAboutToolStripMenuItem");
            this.UICHelpAboutToolStripMenuItem.Name = "UICHelpAboutToolStripMenuItem";
            // 
            // toolStrip
            // 
            resources.ApplyResources(this.toolStrip, "toolStrip");
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UICFileOpenToolStripButton,
            this.UICFileSaveToolStripButton,
            this.UICFileSendToExcelCSVToolStripButton,
            this.toolStripSeparator,
            this.UICEditCutToolStripButton,
            this.UICEditCopyToolStripButton,
            this.UICEditPasteToolStripButton,
            this.UICEditDeleteAllToolStripButton,
            this.toolStripSeparator15,
            this.UICSearchBeginSearchToolStripButton,
            this.UICSearchBeginSearchNarrowToolStripButton,
            this.UICSearchBeginSearchAppendToolStripButton,
            this.UICSearchBeginWordListToolStripButton,
            this.UICSearchBeginCollocationToolStripButton,
            this.toolStripSeparator24,
            this.UICSearchLoadSearchConditionsToolStripButton,
            this.UICSearchSaveCurrentSearchConditionsToolStripButton,
            this.UICSearchResetAllSearchSettingsToolStripButton,
            this.toolStripSeparator2,
            this.UICViewKwicModeToolStripButton,
            this.UICViewTextModeToolStripButton,
            this.UICViewViewAttributesToolStripButton,
            this.UICViewReloadKwicViewToolStripButton,
            this.UICViewLoadAnnotationsToolStripButton,
            this.UICViewFreezeUpdateToolStripButton,
            this.UICViewFullScreenToolStripButton,
            this.toolStripSeparator23,
            this.UICEditChangeLexemeToolStripButton,
            this.toolStripSeparator14,
            this.UICFormatShiftPivotLeftToolStripButton,
            this.UICFormatShiftPivotRightToolStripButton,
            this.toolStripSeparator17,
            this.UICFormatHilightPreviousWordToolStripButton,
            this.UICFormatHilightNextWordToolStripButton,
            this.toolStripSeparator16,
            this.UICViewAutoAdjustRowWidthToolStripButton,
            this.toolStripSeparator13,
            this.toolStripComboBox1,
            this.UICSearchSearchInViewToolStripButton,
            this.toolStripSeparator20,
            this.toolStripTextBox1,
            this.UICViewGotoSentenceToolStripButton,
            this.toolStripSeparator26,
            this.UICViewSplitViewToolStripButton,
            this.toolStripLabel1,
            this.toolStripComboBox2,
            this.toolStripSeparator22,
            this.UICVersioncontrolCommitToolStripButton,
            this.toolStripSeparator28,
            this.UICHelpHelpToolStripButton});
            this.toolStrip.Name = "toolStrip";
            // 
            // UICFileOpenToolStripButton
            // 
            this.UICFileOpenToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICFileOpenToolStripButton, "UICFileOpenToolStripButton");
            this.UICFileOpenToolStripButton.Name = "UICFileOpenToolStripButton";
            // 
            // UICFileSaveToolStripButton
            // 
            this.UICFileSaveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICFileSaveToolStripButton, "UICFileSaveToolStripButton");
            this.UICFileSaveToolStripButton.Name = "UICFileSaveToolStripButton";
            // 
            // UICFileSendToExcelCSVToolStripButton
            // 
            this.UICFileSendToExcelCSVToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UICFileSendToExcelCSVToolStripButton.Image = global::ChaKi.Properties.Resources.ToExcel;
            resources.ApplyResources(this.UICFileSendToExcelCSVToolStripButton, "UICFileSendToExcelCSVToolStripButton");
            this.UICFileSendToExcelCSVToolStripButton.Name = "UICFileSendToExcelCSVToolStripButton";
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            resources.ApplyResources(this.toolStripSeparator, "toolStripSeparator");
            // 
            // UICEditCutToolStripButton
            // 
            this.UICEditCutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICEditCutToolStripButton, "UICEditCutToolStripButton");
            this.UICEditCutToolStripButton.Name = "UICEditCutToolStripButton";
            // 
            // UICEditCopyToolStripButton
            // 
            this.UICEditCopyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICEditCopyToolStripButton, "UICEditCopyToolStripButton");
            this.UICEditCopyToolStripButton.Name = "UICEditCopyToolStripButton";
            // 
            // UICEditPasteToolStripButton
            // 
            this.UICEditPasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICEditPasteToolStripButton, "UICEditPasteToolStripButton");
            this.UICEditPasteToolStripButton.Name = "UICEditPasteToolStripButton";
            // 
            // UICEditDeleteAllToolStripButton
            // 
            this.UICEditDeleteAllToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICEditDeleteAllToolStripButton, "UICEditDeleteAllToolStripButton");
            this.UICEditDeleteAllToolStripButton.Name = "UICEditDeleteAllToolStripButton";
            // 
            // toolStripSeparator15
            // 
            this.toolStripSeparator15.Name = "toolStripSeparator15";
            resources.ApplyResources(this.toolStripSeparator15, "toolStripSeparator15");
            // 
            // UICSearchBeginSearchToolStripButton
            // 
            resources.ApplyResources(this.UICSearchBeginSearchToolStripButton, "UICSearchBeginSearchToolStripButton");
            this.UICSearchBeginSearchToolStripButton.Name = "UICSearchBeginSearchToolStripButton";
            // 
            // UICSearchBeginSearchNarrowToolStripButton
            // 
            this.UICSearchBeginSearchNarrowToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UICSearchBeginSearchNarrowToolStripButton.Image = global::ChaKi.Properties.Resources.NarrowSearch;
            resources.ApplyResources(this.UICSearchBeginSearchNarrowToolStripButton, "UICSearchBeginSearchNarrowToolStripButton");
            this.UICSearchBeginSearchNarrowToolStripButton.Name = "UICSearchBeginSearchNarrowToolStripButton";
            // 
            // UICSearchBeginSearchAppendToolStripButton
            // 
            this.UICSearchBeginSearchAppendToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UICSearchBeginSearchAppendToolStripButton.Image = global::ChaKi.Properties.Resources.AppendSearch;
            resources.ApplyResources(this.UICSearchBeginSearchAppendToolStripButton, "UICSearchBeginSearchAppendToolStripButton");
            this.UICSearchBeginSearchAppendToolStripButton.Name = "UICSearchBeginSearchAppendToolStripButton";
            // 
            // UICSearchBeginWordListToolStripButton
            // 
            this.UICSearchBeginWordListToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICSearchBeginWordListToolStripButton, "UICSearchBeginWordListToolStripButton");
            this.UICSearchBeginWordListToolStripButton.Name = "UICSearchBeginWordListToolStripButton";
            // 
            // UICSearchBeginCollocationToolStripButton
            // 
            this.UICSearchBeginCollocationToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICSearchBeginCollocationToolStripButton, "UICSearchBeginCollocationToolStripButton");
            this.UICSearchBeginCollocationToolStripButton.Name = "UICSearchBeginCollocationToolStripButton";
            // 
            // toolStripSeparator24
            // 
            this.toolStripSeparator24.Name = "toolStripSeparator24";
            resources.ApplyResources(this.toolStripSeparator24, "toolStripSeparator24");
            // 
            // UICSearchLoadSearchConditionsToolStripButton
            // 
            this.UICSearchLoadSearchConditionsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICSearchLoadSearchConditionsToolStripButton, "UICSearchLoadSearchConditionsToolStripButton");
            this.UICSearchLoadSearchConditionsToolStripButton.Name = "UICSearchLoadSearchConditionsToolStripButton";
            // 
            // UICSearchSaveCurrentSearchConditionsToolStripButton
            // 
            this.UICSearchSaveCurrentSearchConditionsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICSearchSaveCurrentSearchConditionsToolStripButton, "UICSearchSaveCurrentSearchConditionsToolStripButton");
            this.UICSearchSaveCurrentSearchConditionsToolStripButton.Name = "UICSearchSaveCurrentSearchConditionsToolStripButton";
            // 
            // UICSearchResetAllSearchSettingsToolStripButton
            // 
            this.UICSearchResetAllSearchSettingsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICSearchResetAllSearchSettingsToolStripButton, "UICSearchResetAllSearchSettingsToolStripButton");
            this.UICSearchResetAllSearchSettingsToolStripButton.Name = "UICSearchResetAllSearchSettingsToolStripButton";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // UICViewKwicModeToolStripButton
            // 
            this.UICViewKwicModeToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICViewKwicModeToolStripButton, "UICViewKwicModeToolStripButton");
            this.UICViewKwicModeToolStripButton.Name = "UICViewKwicModeToolStripButton";
            // 
            // UICViewTextModeToolStripButton
            // 
            this.UICViewTextModeToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICViewTextModeToolStripButton, "UICViewTextModeToolStripButton");
            this.UICViewTextModeToolStripButton.Name = "UICViewTextModeToolStripButton";
            // 
            // UICViewViewAttributesToolStripButton
            // 
            this.UICViewViewAttributesToolStripButton.Checked = true;
            this.UICViewViewAttributesToolStripButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UICViewViewAttributesToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICViewViewAttributesToolStripButton, "UICViewViewAttributesToolStripButton");
            this.UICViewViewAttributesToolStripButton.Name = "UICViewViewAttributesToolStripButton";
            // 
            // UICViewReloadKwicViewToolStripButton
            // 
            this.UICViewReloadKwicViewToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICViewReloadKwicViewToolStripButton, "UICViewReloadKwicViewToolStripButton");
            this.UICViewReloadKwicViewToolStripButton.Name = "UICViewReloadKwicViewToolStripButton";
            // 
            // UICViewLoadAnnotationsToolStripButton
            // 
            this.UICViewLoadAnnotationsToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICViewLoadAnnotationsToolStripButton, "UICViewLoadAnnotationsToolStripButton");
            this.UICViewLoadAnnotationsToolStripButton.Name = "UICViewLoadAnnotationsToolStripButton";
            // 
            // UICViewFreezeUpdateToolStripButton
            // 
            this.UICViewFreezeUpdateToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UICViewFreezeUpdateToolStripButton.Image = global::ChaKi.Properties.Resources.none;
            this.UICViewFreezeUpdateToolStripButton.Name = "UICViewFreezeUpdateToolStripButton";
            resources.ApplyResources(this.UICViewFreezeUpdateToolStripButton, "UICViewFreezeUpdateToolStripButton");
            // 
            // UICViewFullScreenToolStripButton
            // 
            this.UICViewFullScreenToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UICViewFullScreenToolStripButton.Image = global::ChaKi.Properties.Resources.FullScreenHS;
            resources.ApplyResources(this.UICViewFullScreenToolStripButton, "UICViewFullScreenToolStripButton");
            this.UICViewFullScreenToolStripButton.Name = "UICViewFullScreenToolStripButton";
            // 
            // toolStripSeparator23
            // 
            this.toolStripSeparator23.Name = "toolStripSeparator23";
            resources.ApplyResources(this.toolStripSeparator23, "toolStripSeparator23");
            // 
            // UICEditChangeLexemeToolStripButton
            // 
            this.UICEditChangeLexemeToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UICEditChangeLexemeToolStripButton.Image = global::ChaKi.Properties.Resources.ChangeLexeme;
            resources.ApplyResources(this.UICEditChangeLexemeToolStripButton, "UICEditChangeLexemeToolStripButton");
            this.UICEditChangeLexemeToolStripButton.Name = "UICEditChangeLexemeToolStripButton";
            // 
            // toolStripSeparator14
            // 
            this.toolStripSeparator14.Name = "toolStripSeparator14";
            resources.ApplyResources(this.toolStripSeparator14, "toolStripSeparator14");
            // 
            // UICFormatShiftPivotLeftToolStripButton
            // 
            this.UICFormatShiftPivotLeftToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICFormatShiftPivotLeftToolStripButton, "UICFormatShiftPivotLeftToolStripButton");
            this.UICFormatShiftPivotLeftToolStripButton.Name = "UICFormatShiftPivotLeftToolStripButton";
            // 
            // UICFormatShiftPivotRightToolStripButton
            // 
            this.UICFormatShiftPivotRightToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICFormatShiftPivotRightToolStripButton, "UICFormatShiftPivotRightToolStripButton");
            this.UICFormatShiftPivotRightToolStripButton.Name = "UICFormatShiftPivotRightToolStripButton";
            // 
            // toolStripSeparator17
            // 
            this.toolStripSeparator17.Name = "toolStripSeparator17";
            resources.ApplyResources(this.toolStripSeparator17, "toolStripSeparator17");
            // 
            // UICFormatHilightPreviousWordToolStripButton
            // 
            this.UICFormatHilightPreviousWordToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICFormatHilightPreviousWordToolStripButton, "UICFormatHilightPreviousWordToolStripButton");
            this.UICFormatHilightPreviousWordToolStripButton.Name = "UICFormatHilightPreviousWordToolStripButton";
            // 
            // UICFormatHilightNextWordToolStripButton
            // 
            this.UICFormatHilightNextWordToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICFormatHilightNextWordToolStripButton, "UICFormatHilightNextWordToolStripButton");
            this.UICFormatHilightNextWordToolStripButton.Name = "UICFormatHilightNextWordToolStripButton";
            // 
            // toolStripSeparator16
            // 
            this.toolStripSeparator16.Name = "toolStripSeparator16";
            resources.ApplyResources(this.toolStripSeparator16, "toolStripSeparator16");
            // 
            // UICViewAutoAdjustRowWidthToolStripButton
            // 
            this.UICViewAutoAdjustRowWidthToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICViewAutoAdjustRowWidthToolStripButton, "UICViewAutoAdjustRowWidthToolStripButton");
            this.UICViewAutoAdjustRowWidthToolStripButton.Name = "UICViewAutoAdjustRowWidthToolStripButton";
            // 
            // toolStripSeparator13
            // 
            this.toolStripSeparator13.Name = "toolStripSeparator13";
            resources.ApplyResources(this.toolStripSeparator13, "toolStripSeparator13");
            // 
            // toolStripComboBox1
            // 
            this.toolStripComboBox1.BackColor = System.Drawing.Color.White;
            this.toolStripComboBox1.DropDownHeight = 200;
            this.toolStripComboBox1.DropDownWidth = 200;
            resources.ApplyResources(this.toolStripComboBox1, "toolStripComboBox1");
            this.toolStripComboBox1.Margin = new System.Windows.Forms.Padding(1);
            this.toolStripComboBox1.Name = "toolStripComboBox1";
            // 
            // UICSearchSearchInViewToolStripButton
            // 
            this.UICSearchSearchInViewToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICSearchSearchInViewToolStripButton, "UICSearchSearchInViewToolStripButton");
            this.UICSearchSearchInViewToolStripButton.Name = "UICSearchSearchInViewToolStripButton";
            // 
            // toolStripSeparator20
            // 
            this.toolStripSeparator20.Name = "toolStripSeparator20";
            resources.ApplyResources(this.toolStripSeparator20, "toolStripSeparator20");
            // 
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            resources.ApplyResources(this.toolStripTextBox1, "toolStripTextBox1");
            this.toolStripTextBox1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.toolStripTextBox1_KeyUp);
            // 
            // UICViewGotoSentenceToolStripButton
            // 
            this.UICViewGotoSentenceToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICViewGotoSentenceToolStripButton, "UICViewGotoSentenceToolStripButton");
            this.UICViewGotoSentenceToolStripButton.Name = "UICViewGotoSentenceToolStripButton";
            // 
            // toolStripSeparator26
            // 
            this.toolStripSeparator26.Name = "toolStripSeparator26";
            resources.ApplyResources(this.toolStripSeparator26, "toolStripSeparator26");
            // 
            // UICViewSplitViewToolStripButton
            // 
            this.UICViewSplitViewToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UICViewSplitViewToolStripButton.Image = global::ChaKi.Properties.Resources.TileWindowsHorizontally;
            resources.ApplyResources(this.UICViewSplitViewToolStripButton, "UICViewSplitViewToolStripButton");
            this.UICViewSplitViewToolStripButton.Name = "UICViewSplitViewToolStripButton";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            resources.ApplyResources(this.toolStripLabel1, "toolStripLabel1");
            // 
            // toolStripComboBox2
            // 
            this.toolStripComboBox2.Name = "toolStripComboBox2";
            resources.ApplyResources(this.toolStripComboBox2, "toolStripComboBox2");
            // 
            // toolStripSeparator22
            // 
            this.toolStripSeparator22.Name = "toolStripSeparator22";
            resources.ApplyResources(this.toolStripSeparator22, "toolStripSeparator22");
            // 
            // toolStripSeparator28
            // 
            this.toolStripSeparator28.Name = "toolStripSeparator28";
            resources.ApplyResources(this.toolStripSeparator28, "toolStripSeparator28");
            // 
            // UICHelpHelpToolStripButton
            // 
            this.UICHelpHelpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.UICHelpHelpToolStripButton, "UICHelpHelpToolStripButton");
            this.UICHelpHelpToolStripButton.Name = "UICHelpHelpToolStripButton";
            // 
            // BottomToolStripPanel
            // 
            resources.ApplyResources(this.BottomToolStripPanel, "BottomToolStripPanel");
            this.BottomToolStripPanel.Name = "BottomToolStripPanel";
            this.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // TopToolStripPanel
            // 
            resources.ApplyResources(this.TopToolStripPanel, "TopToolStripPanel");
            this.TopToolStripPanel.Name = "TopToolStripPanel";
            this.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // RightToolStripPanel
            // 
            resources.ApplyResources(this.RightToolStripPanel, "RightToolStripPanel");
            this.RightToolStripPanel.Name = "RightToolStripPanel";
            this.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // LeftToolStripPanel
            // 
            resources.ApplyResources(this.LeftToolStripPanel, "LeftToolStripPanel");
            this.LeftToolStripPanel.Name = "LeftToolStripPanel";
            this.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // ContentPanel
            // 
            resources.ApplyResources(this.ContentPanel, "ContentPanel");
            // 
            // statusStrip1
            // 
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.ManagerRenderMode;
            // 
            // UICViewFullScreenToolStripMenuItem
            // 
            this.UICViewFullScreenToolStripMenuItem.Image = global::ChaKi.Properties.Resources.FullScreenHS;
            this.UICViewFullScreenToolStripMenuItem.Name = "UICViewFullScreenToolStripMenuItem";
            resources.ApplyResources(this.UICViewFullScreenToolStripMenuItem, "UICViewFullScreenToolStripMenuItem");
            // 
            // UICVersioncontrolCommitToolStripButton
            // 
            this.UICVersioncontrolCommitToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UICVersioncontrolCommitToolStripButton.Image = global::ChaKi.Properties.Resources.git_commit;
            this.UICVersioncontrolCommitToolStripButton.Name = "UICVersioncontrolCommitToolStripButton";
            resources.ApplyResources(this.UICVersioncontrolCommitToolStripButton, "UICVersioncontrolCommitToolStripButton");
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.toolStripContainer1);
            this.Controls.Add(this.statusStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Style = Crownwood.DotNetMagic.Common.VisualStyle.Office2007Silver;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyUp);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStripPanel BottomToolStripPanel;
        private System.Windows.Forms.ToolStripPanel TopToolStripPanel;
        private System.Windows.Forms.ToolStripPanel RightToolStripPanel;
        private System.Windows.Forms.ToolStripPanel LeftToolStripPanel;
        private System.Windows.Forms.ToolStripContentPanel ContentPanel;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem UICFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICFileExitToolStripMenuItem;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton UICFileOpenToolStripButton;
        private System.Windows.Forms.ToolStripButton UICFileSaveToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripButton UICEditCutToolStripButton;
        private System.Windows.Forms.ToolStripButton UICEditCopyToolStripButton;
        private System.Windows.Forms.ToolStripButton UICEditPasteToolStripButton;
        private System.Windows.Forms.ToolStripButton UICHelpHelpToolStripButton;
        private ChakiStatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripMenuItem UICEditToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditCopyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditDeleteAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditSelectAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditSelectCheckedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditCheckAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditCheckSelectedToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem UICEditUncheckAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewToolbarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewStatusBarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewSearchPanelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewCommandPanelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewContextPanelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewLexemePanelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewDependencyEditPanelToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem UICViewViewAttributesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewAutoAdjustRowWidthToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchSelectCorpusToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchSetSearchFiltersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchStringSearchSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchTagSearchSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchDependencySearchSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchCollocationSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem UICSearchBeginSearchToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem UICSearchLoadSearchConditionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchSaveCurrentSearchConditionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchResetAllSearchSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICFormatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICFormatShiftPivotLeftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICFormatShiftPivotRightToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem UICFormatHilightPreviousWordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICFormatHilightNextWordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICOptionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICOptionsSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICHelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICHelpAboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICFileOpenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICFileSaveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICFileSaveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem UICFileSendToExcelCSVToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICToolsCreateSQLiteCorpusToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton UICViewAutoAdjustRowWidthToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
        private System.Windows.Forms.ToolStripButton UICSearchResetAllSearchSettingsToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
        private System.Windows.Forms.ToolStripButton UICEditDeleteAllToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
        private System.Windows.Forms.ToolStripMenuItem UICToolsCreateMySQLCorpusToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton UICSearchBeginCollocationToolStripButton;
        private System.Windows.Forms.ToolStripButton UICSearchLoadSearchConditionsToolStripButton;
        private System.Windows.Forms.ToolStripButton UICSearchSaveCurrentSearchConditionsToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton UICFormatShiftPivotLeftToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator16;
        private System.Windows.Forms.ToolStripButton UICFormatShiftPivotRightToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
        private System.Windows.Forms.ToolStripButton UICFormatHilightPreviousWordToolStripButton;
        private System.Windows.Forms.ToolStripButton UICFormatHilightNextWordToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem UICViewKwicModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewTextModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton UICViewKwicModeToolStripButton;
        private System.Windows.Forms.ToolStripButton UICViewTextModeToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem UICViewHistoryPanelToolStripMenuItem;
        private ChaKi.Views.WordListView wordListView;
        private ChaKi.Views.KwicView.KwicView kwicView;
        private System.Windows.Forms.ToolStripButton UICViewViewAttributesToolStripButton;
        private System.Windows.Forms.ToolStripButton UICViewLoadAnnotationsToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator18;
        private System.Windows.Forms.ToolStripMenuItem UICSearchExamineLastSearchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewLoadAnnotationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem UICViewAttributePanelToolStripMenuItem;
        private ChaKi.Views.CollocationView collocationView;
        private System.Windows.Forms.ToolStripMenuItem UICOptionsWordColorSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton UICSearchBeginSearchNarrowToolStripButton;
        private System.Windows.Forms.ToolStripButton UICSearchBeginSearchAppendToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem UICSearchBeginSearchNarrowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchBeginSearchAppendToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator19;
        private System.Windows.Forms.ToolStripMenuItem UICToolsTextFormatterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditCutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditPasteToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton UICFileSendToExcelCSVToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem UICToolsCreateDictionaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.ToolStripMenuItem UICOptionsPropertyBoxSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICHelpHelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICViewScriptingPanelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICToolsEditTagSetDefinitionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICOptionsTagAppearanceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICOptionsDictionarySettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchExamineLastEditToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICEditUnselectAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton UICSearchSearchInViewToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator20;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox1;
        private System.Windows.Forms.ToolStripMenuItem UICSearchSearchInViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator21;
        private System.Windows.Forms.ToolStripButton UICViewReloadKwicViewToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem UICViewReloadKwicViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
        private System.Windows.Forms.ToolStripButton UICViewGotoSentenceToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator22;
        private System.Windows.Forms.ToolStripButton UICViewFreezeUpdateToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator23;
        private System.Windows.Forms.ToolStripButton UICEditChangeLexemeToolStripButton;
        private System.Windows.Forms.ToolStripButton UICSearchBeginSearchToolStripButton;
        private System.Windows.Forms.ToolStripButton UICSearchBeginWordListToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator24;
        private System.Windows.Forms.ToolStripMenuItem UICSearchBeginWordListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICSearchBeginCollocationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICToolsText2CorpusToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator25;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator26;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private ChaKi.Panels.ProjectSelector toolStripComboBox2;
        private System.Windows.Forms.ToolStripButton UICViewSplitViewToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator27;
        private System.Windows.Forms.ToolStripMenuItem UICViewSplitViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton UICViewFullScreenToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem UICViewFullScreenToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton UICVersioncontrolCommitToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator28;
        private System.Windows.Forms.ToolStripMenuItem UICDictionaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICDictionaryExportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UICDictionaryExportMWEToolStripMenuItem;
    }
}

