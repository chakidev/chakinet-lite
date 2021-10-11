using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Common.Widgets;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Readers;
using ChaKi.Entity.Search;
using ChaKi.Entity.Settings;
using ChaKi.GUICommon;
using ChaKi.Options;
using ChaKi.Panels;
using ChaKi.Properties;
using ChaKi.Service.Database;
using ChaKi.Service.DependencyEdit;
using ChaKi.Service.Export;
using ChaKi.Service.Git;
using ChaKi.Service.Readers;
using ChaKi.Service.Search;
using ChaKi.ToolDialogs;
using ChaKi.Views;
using ChaKi.Views.KwicView;
using Crownwood.DotNetMagic.Docking;
using DependencyEditSLA;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MessageBox = ChaKi.Common.Widgets.MessageBox;

namespace ChaKi
{
    partial class MainForm
    {
        private void OnFileOpen(object sender, EventArgs e)
        {
            FileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Chaki files (*.chaki)|*.chaki";
            dlg.CheckFileExists = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Cursor oldCur = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    SearchHistory hist = LoadFile(dlg.FileName);
                    if (hist != null)
                    {
                        SearchHistory.Root.AddChild(hist);
                        if (hist.CondSeq.Count > 0)
                        {
                            this.condPanel.ChangeConditions(hist.CondSeq[0]);
                        }
                        this.kwicView.SetModel(hist);
                        m_Model.CurrentSearchConditions = hist.CondSeq[0];
                    }
                }
                finally
                {
                    Cursor.Current = oldCur;
                }
            }
        }

        private void OnFileSave(object sender, EventArgs e)
        {
            if (this.historyGuidePanel.Current == null)
            {
                MessageBox.Show("Current history node not found.");
                return;
            }
            if (this.historyGuidePanel.Current.FilePath == null)
            {
                OnFileSaveAs(sender, e);
            }
            else
            {
                Save(this.historyGuidePanel.Current.FilePath, null);
            }
        }

        private void OnFileSaveAs(object sender, EventArgs e)
        {
            if (this.historyGuidePanel.Current == null)
            {
                MessageBox.Show("Current history node not found.");
                return;
            }

            IChaKiView aview = GetActiveView();
            if (aview == null) return;

            FileDialog dlg = new SaveFileDialog();
            if (aview is KwicView)
            {
                dlg.Filter = "Chaki files (*.chaki)|*.chaki|Text files (*.txt)|*.txt|XML files (*.xml)|*.xml|Cabocha files (*.cabocha)|*.cabocha";
            }
            else
            {
                dlg.Filter = "Chaki files (*.chaki)|*.chaki|CSV files (*.csv)|*.csv";
            }
            dlg.CheckPathExists = true;
            dlg.Title = "Save/Export Search Result";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (Save(dlg.FileName, null))
                {
                    this.historyGuidePanel.Current.FilePath = dlg.FileName;
                }
            }
        }

        private void OnFileSendToExcelCSV(object sender, EventArgs e)
        {
            if (this.kwicView.Visible)
            {
                ExportToExcelDialog dlg = new ExportToExcelDialog();
                dlg.SetModel(UserSettings.GetInstance().ExportSetting);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    UserSettings.GetInstance().ExportSetting = dlg.GetModel();
                    ExportToExcel();
                }
            }
            else if (this.wordListView.Visible)
            {
                ExportGridToExcelDialog dlg = new ExportGridToExcelDialog();
                dlg.SetModel(UserSettings.GetInstance().ExportSetting);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    UserSettings.GetInstance().ExportSetting = dlg.GetModel();
                    ExportGridToExcel(this.wordListView.Grid);
                }
            }
            else if (this.collocationView.Visible)
            {
                ExportGridToExcelDialog dlg = new ExportGridToExcelDialog();
                dlg.SetModel(UserSettings.GetInstance().ExportSetting);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    UserSettings.GetInstance().ExportSetting = dlg.GetModel();
                    ExportGridToExcel(this.collocationView.Grid);
                }
            }
        }

        private void OnFileExit(object sender, EventArgs e)
        {
            Close();
        }

        private void OnEditCutCopyPasteUpdate(object sender, EventArgs e)
        {
            IChaKiView aview = GetActiveView();
            if (aview == null) return;

            bool b = aview.CanCut;
            this.UICEditCutToolStripButton.Enabled = b;
            this.UICEditCutToolStripMenuItem.Enabled = b;
            b = aview.CanCopy;
            this.UICEditCopyToolStripButton.Enabled = b;
            this.UICEditCopyToolStripMenuItem.Enabled = b;
            b = aview.CanPaste;
            this.UICEditPasteToolStripButton.Enabled = b;
            this.UICEditPasteToolStripMenuItem.Enabled = b;
        }

        private void OnEditCopy(object sender, EventArgs e)
        {
            IChaKiView aview = GetActiveView();
            if (aview != null)
            {
                aview.CopyToClipboard();
            }
        }

        private void OnEditCut(object sender, EventArgs e)
        {
            IChaKiView aview = GetActiveView();
            if (aview != null)
            {
                aview.CutToClipboard();
            }
        }

        private void OnEditPaste(object sender, EventArgs e)
        {
            IChaKiView aview = GetActiveView();
            if (aview != null)
            {
                aview.PasteFromClipboard();
            }
        }

        private void OnEditDeleteAll(object sender, EventArgs e)
        {
            SearchHistory hist = this.historyGuidePanel.Current;
            m_Model.DeleteHistory(hist);
            this.historyGuidePanel.UpdateView();
            // KwicViewも消去
            this.kwicView.DeleteAll();
            this.kwicView.Invalidate();
            ChangeView(SearchType.SentenceSearch);
        }

        private void OnEditCheckAll(object sender, EventArgs e)
        {
            this.kwicView.CheckAll();
        }

        private void OnEditCheckSelected(object sender, EventArgs e)
        {
            this.kwicView.CheckSelected();
        }

        private void OnEditUncheckAll(object sender, EventArgs e)
        {
            this.kwicView.UncheckAll();
        }

        private void OnEditSelectChecked(object sender, EventArgs e)
        {
            this.kwicView.SelectChecked();
        }

        private void OnEditSelectAll(object sender, EventArgs e)
        {
            this.kwicView.SelectAll();
        }

        private void OnEditUnselectAll(object sender, EventArgs e)
        {
            this.kwicView.UnselectAll();
        }

        private void OnEditChangeLexeme(object sender, EventArgs e)
        {
            //TODO: このパネルは、モードレスのドッキングパネルにすることを予定している。
            var panel = new WordEditPanel();
            // KwicListからチェックされているレコードの中心語を追加
            var current = this.historyGuidePanel.Current;
            if (current == null || current.KwicList == null)
            {
                return;
            }
            try
            {
                foreach (var item in current.KwicList.Records)
                {
                    if (!item.Checked)
                    {
                        continue;
                    }
                    panel.AddTarget(item);
                }
                if (panel.Target.Count > 0)
                {
                    panel.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog errdlg = new ErrorReportDialog(string.Format("Error: {0}", ex.Message), ex);
                errdlg.ShowDialog();
                return;
            }
        }

        private void OnViewToolbar(object sender, EventArgs e)
        {
            this.toolStrip.Visible = !this.toolStrip.Visible;
        }

        private void OnViewToolbarUpdate(object sender, EventArgs e)
        {
            this.UICViewToolbarToolStripMenuItem.Checked = this.toolStrip.Visible;
        }

        private void OnViewStatusBar(object sender, EventArgs e)
        {
            this.statusStrip1.Visible = !this.statusStrip1.Visible;
        }

        private void OnViewStatusBarUpdate(object sender, EventArgs e)
        {
            this.UICViewStatusBarToolStripMenuItem.Checked = this.statusStrip1.Visible;
        }

        private void OnViewSearchPanel(object sender, EventArgs e)
        {
            SwitchVisible(this.condPanelContainer);
        }

        private void OnViewSearchPanelUpdate(object sender, EventArgs e)
        {
            this.UICViewSearchPanelToolStripMenuItem.Checked = this.condPanelContainer.Visible;
        }

        private void OnViewHistoryPanel(object sender, EventArgs e)
        {
            SwitchVisible(this.historyGuidePanelContainer);
        }

        private void OnViewHistoryPanelUpdate(object sender, EventArgs e)
        {
            this.UICViewHistoryPanelToolStripMenuItem.Checked = this.historyGuidePanelContainer.Visible;
        }

        private void OnViewCommandPanel(object sender, EventArgs e)
        {
            SwitchVisible(this.commandPanelContainer);
        }

        private void OnViewCommandPanelUpdate(object sender, EventArgs e)
        {
            this.UICViewCommandPanelToolStripMenuItem.Checked = this.commandPanelContainer.Visible;
        }

        private void OnViewContextPanel(object sender, EventArgs e)
        {
            SwitchVisible(this.contextPanelContainer);
        }

        private void OnViewContextPanelUpdate(object sender, EventArgs e)
        {
            this.UICViewContextPanelToolStripMenuItem.Checked = this.contextPanelContainer.Visible;
        }

        private void OnViewDependencyEditPanel(object sender, EventArgs e)
        {
            SwitchVisible(this.dependencyEditContainer);
        }

        private void OnViewDependencyEditPanelUpdate(object sender, EventArgs e)
        {
            this.UICViewDependencyEditPanelToolStripMenuItem.Checked = this.dependencyEditContainer.Visible;
        }

        private void OnViewLexemePanel(object sender, EventArgs e)
        {
            SwitchVisible(this.LexemePanelContainer);
        }

        private void OnViewLexemePanelUpdate(object sender, EventArgs e)
        {
            this.UICViewLexemePanelToolStripMenuItem.Checked = this.LexemePanelContainer.Visible;
        }

        private void OnViewAttributePanel(object sender, EventArgs e)
        {
            SwitchVisible(this.AttributePanelContainer);
        }

        private void OnViewAttributePanelUpdate(object sender, EventArgs e)
        {
            this.UICViewAttributePanelToolStripMenuItem.Checked = this.AttributePanelContainer.Visible;
        }

        private void OnViewScriptingPanel(object sender, EventArgs e)
        {
            SwitchVisible(this.scriptingPanelContainer);
        }

        private void OnViewScriptingPanelUpdate(object sender, EventArgs e)
        {
            this.UICViewScriptingPanelToolStripMenuItem.Checked = this.scriptingPanelContainer.Visible;
        }


        private void OnViewViewAttributes(object sender, EventArgs e)
        {
            bool currentIsTwoLineMode = this.kwicView.TwoLineMode;
            if (!currentIsTwoLineMode)
            {
                if (this.kwicView.HasSimpleItem)
                {
                    if (MessageBox.Show(Resources.OneLineToTwoLine, Resources.Confirm,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        try
                        {
                            ReloadKwicResult();
                        }
                        catch (Exception ex)
                        {
                            ErrorReportDialog errdlg = new ErrorReportDialog("Error while executing query:", ex);
                            errdlg.ShowDialog();
                        }
                    }
                }
            }
            this.kwicView.TwoLineMode = !currentIsTwoLineMode;
        }

        private void OnViewViewAttributesUpdate(object sender, EventArgs e)
        {
            this.UICViewViewAttributesToolStripMenuItem.Checked = this.kwicView.TwoLineMode;
            this.UICViewViewAttributesToolStripButton.Checked = this.kwicView.TwoLineMode;
        }

        private void OnViewKwicMode(object sender, EventArgs e)
        {
            this.kwicView.KwicMode = true;
            this.kwicView.RecalcLayout();
        }

        private void OnViewKwicModeUpdate(object sender, EventArgs e)
        {
            this.UICViewKwicModeToolStripMenuItem.Checked = this.kwicView.KwicMode;
            this.UICViewKwicModeToolStripButton.Checked = this.kwicView.KwicMode;
        }

        private void OnViewTextMode(object sender, EventArgs e)
        {
            this.kwicView.KwicMode = false;
            this.kwicView.RecalcLayout();
        }

        private void OnViewTextModeUpdate(object sender, EventArgs e)
        {
            this.UICViewTextModeToolStripMenuItem.Checked = !this.kwicView.KwicMode;
            this.UICViewTextModeToolStripButton.Checked = !this.kwicView.KwicMode;
        }

        private void OnViewLoadAnnotations(object sender, EventArgs e)
        {
            LoadAnnotations();
        }

        private void OnViewReloadKwicView(object sender, EventArgs e)
        {
            try
            {
                KwicSearchAgain(this.kwicView.GetModel());
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Reload Error", ex);
                dlg.ShowDialog();
            }
        }

        private void OnViewAutoAdjustRowWidth(object sender, EventArgs e)
        {
            this.kwicView.AutoAdjustColumnWidths();
        }

        private void OnViewAdjustRowWidthToTheLeft(object sender, EventArgs e)
        {
            this.kwicView.LeftAdjustColumnWidths();
        }

        private void OnViewGotoSentence(object sender, EventArgs e)
        {
            int index = -1;
            if (Int32.TryParse(this.toolStripTextBox1.Text, out index) && index >= 0)
            {
                if (!this.kwicView.SetCurSelBySentenceID(index))
                {
                    MessageBox.Show("Not Found.");
                }
            }
        }

        private void OnViewFreezeUpdate(object sender, EventArgs e)
        {
            var b = !this.UICViewFreezeUpdateToolStripButton.Checked;
            this.UICViewFreezeUpdateToolStripButton.Checked = b;
            var oldCur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                this.kwicView.UpdateFrozen = b;
            }
            catch (Exception ex)
            {
                ErrorReportDialog errdlg = new ErrorReportDialog("Error while executing update:", ex);
                errdlg.ShowDialog();
            }
            try
            {
                this.wordListView.UpdateFrozen = b;
            }
            catch (Exception ex)
            {
                ErrorReportDialog errdlg = new ErrorReportDialog("Error while executing update:", ex);
                errdlg.ShowDialog();
            }
            Cursor.Current = oldCur;
        }

        private void OnViewFreezeUpdateUpdate(object sender, EventArgs e)
        {
            this.UICViewFreezeUpdateToolStripButton.Checked = (this.kwicView.UpdateFrozen || this.wordListView.UpdateFrozen);
        }

        private void toolStripTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                OnViewGotoSentence(sender, e);
            }
        }

        private void OnViewSplitView(object sender, EventArgs e)
        {
            this.kwicView.IsViewSplitted = !this.kwicView.IsViewSplitted;
        }

        private void OnViewSplitViewUpdate(object sender, EventArgs e)
        {
            this.UICViewSplitViewToolStripButton.Checked = this.kwicView.IsViewSplitted;
        }

        private bool m_ViewFullScreenGuard = false;
        private FullScreenContainer m_FullScreenContainer = null;
        private void OnViewFullScreen(object sender, EventArgs e)
        {
            if (m_ViewFullScreenGuard)
            {
                return;
            }
            m_ViewFullScreenGuard = true;
            // 現在のDocking状態をメモリ上に待避
            var tempSetting = dockingManager.SaveConfigToArray(Encoding.UTF8);

            // 現在FloatingになっているパネルはそのままTopMostで表示を維持する。
            var floatings = new List<Form>();
            foreach (Content c in this.dockingManager.Contents)
            {
                if (c.Visible)
                {
                    Control p = c.Control;
                    while (p != null)
                    {
                        if (p is FloatingForm)
                        {
                            p.Visible = true;
                            var form = p.TopLevelControl as Form;
                            if (form != null)
                            {
                                form.TopMost = true;
                                floatings.Add(form);
                            }
                        }
                        p = p.Parent;
                    }
                }
            }

            if (m_FullScreenContainer == null)
            {
                m_FullScreenContainer = new FullScreenContainer(new Action<Form>(
                    d =>
                    {
                        // Hide時の処理(callback)
                        this.kwicView.Parent = null;
                        this.toolStripContainer1.ContentPanel.Controls.Add(this.kwicView);
                        foreach (var form in floatings)
                        {
                            form.TopMost = false;
                        }
                        // Docking状態を戻す
                        dockingManager.LoadConfigFromArray(tempSetting);
                        this.kwicView.Enabled = true;
                        m_ViewFullScreenGuard = false;
                    }));
            }

            this.kwicView.Parent = m_FullScreenContainer;
            m_FullScreenContainer.Show();
        }

        private void OnSearchSelectCorpus(object sender, EventArgs e)
        {
            this.condPanel.SelectedTabIndex = 0;
        }

        private void OnSearchSelectCorpusUpdate(object sender, EventArgs e)
        {
            this.UICSearchSelectCorpusToolStripMenuItem.Checked = (this.condPanel.SelectedTabIndex == 0);
        }

        private void OnSearchSetSearchFilters(object sender, EventArgs e)
        {
            this.condPanel.SelectedTabIndex = 1;
        }

        private void OnSearchSetSearchFiltersUpdate(object sender, EventArgs e)
        {
            this.UICSearchSetSearchFiltersToolStripMenuItem.Checked = (this.condPanel.SelectedTabIndex == 1);
        }

        private void OnSearchStringSearchSettings(object sender, EventArgs e)
        {
            this.condPanel.SelectedTabIndex = 2;
        }

        private void OnSearchStringSearchSettingsUpdate(object sender, EventArgs e)
        {
            this.UICSearchStringSearchSettingsToolStripMenuItem.Checked = (this.condPanel.SelectedTabIndex == 2);
        }

        private void OnSearchTagSearchSettings(object sender, EventArgs e)
        {
            this.condPanel.SelectedTabIndex = 3;
        }

        private void OnSearchTagSearchSettingsUpdate(object sender, EventArgs e)
        {
            this.UICSearchTagSearchSettingsToolStripMenuItem.Checked = (this.condPanel.SelectedTabIndex == 3);
        }

        private void OnSearchDependencySearchSettings(object sender, EventArgs e)
        {
            this.condPanel.SelectedTabIndex = 4;
        }

        private void OnSearchDependencySearchSettingsUpdate(object sender, EventArgs e)
        {
            this.UICSearchDependencySearchSettingsToolStripMenuItem.Checked = (this.condPanel.SelectedTabIndex == 4);
        }

        private void OnSearchCollocationSettings(object sender, EventArgs e)
        {
            this.condPanel.SelectedTabIndex = 5;
        }

        private void OnSearchCollocationSettingsUpdate(object sender, EventArgs e)
        {
            this.UICSearchCollocationSettingsToolStripMenuItem.Checked = (this.condPanel.SelectedTabIndex == 5);
        }

        private void OnSearchBeginSearch(object sender, EventArgs e)
        {
            OnBeginSearch();
        }

        private void OnSearchBeginSearchNarrow(object sender, EventArgs e)
        {
            OnBeginSearchNarrow();
        }

        private void OnSearchBeginSearchAppend(object sender, EventArgs e)
        {
            OnBeginSearchAppend();
        }

        private void OnSearchBeginSearchUpdate(object sender, EventArgs e)
        {
            bool b = this.commandPanel.CanBeginSearch;
            this.UICSearchBeginSearchToolStripMenuItem.Enabled = b;
            this.UICSearchBeginSearchToolStripButton.Enabled = b;
            this.UICSearchBeginSearchNarrowToolStripMenuItem.Enabled = b;
            this.UICSearchBeginSearchNarrowToolStripButton.Enabled = b;
            this.UICSearchBeginSearchAppendToolStripMenuItem.Enabled = b;
            this.UICSearchBeginSearchAppendToolStripButton.Enabled = b;
        }

        private void OnSearchBeginWordList(object sender, EventArgs e)
        {
            OnBeginWordList();
        }

        private void OnSearchBeginWordListUpdate(object sender, EventArgs e)
        {
            bool b = this.commandPanel.CanBeginWordList;
            this.UICSearchBeginWordListToolStripMenuItem.Enabled = b;
            this.UICSearchBeginWordListToolStripButton.Enabled = b;
        }

        private void OnSearchBeginCollocation(object sender, EventArgs e)
        {
            OnBeginCollocation();
        }

        private void OnSearchBeginCollocationUpdate(object sender, EventArgs e)
        {
            bool b = this.commandPanel.CanBeginCollocation;
            this.UICSearchBeginCollocationToolStripMenuItem.Enabled = b;
            this.UICSearchBeginCollocationToolStripButton.Enabled = b;
        }

        private void OnSearchLoadSearchConditions(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.CheckPathExists = true;
            dlg.CheckFileExists = true;
            dlg.Title = "Load Search Condition";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    LoadSearchCond(dlg.FileName);
                }
                catch (Exception ex)
                {
                    ErrorReportDialog errdlg = new ErrorReportDialog("Error while executing query:", ex);
                    errdlg.ShowDialog();
                }
            }
        }

        private void OnSearchSaveCurrentSearchConditions(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".xml";
            dlg.Title = "Save Search Condition";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    SaveSearchCond(dlg.FileName);
                }
                catch (Exception ex)
                {
                    ErrorReportDialog errdlg = new ErrorReportDialog("Error while executing query:", ex);
                    errdlg.ShowDialog();
                }
            }
        }

        private void OnFormatShiftUpdate(object sender, EventArgs e)
        {
            SearchHistory hist = this.historyGuidePanel.Current;
            bool f = (hist != null && hist.CanShift());
            this.UICFormatShiftPivotLeftToolStripMenuItem.Enabled = f;
            this.UICFormatShiftPivotLeftToolStripButton.Enabled = f;
            this.UICFormatShiftPivotRightToolStripMenuItem.Enabled = f;
            this.UICFormatShiftPivotRightToolStripButton.Enabled = f;
            this.UICFormatHilightNextWordToolStripMenuItem.Enabled = f;
            this.UICFormatHilightNextWordToolStripButton.Enabled = f;
            this.UICFormatHilightPreviousWordToolStripMenuItem.Enabled = f;
            this.UICFormatHilightPreviousWordToolStripButton.Enabled = f;
        }

        private void OnSearchResetAllSearchSettings(object sender, EventArgs e)
        {
            this.condPanel.ResetConditions();

        }

        private void OnSearchExamineLastSearch(object sender, EventArgs e)
        {
            ErrorReportDialog dlg = new ErrorReportDialog();
            dlg.Text = "Last Query";
            dlg.Message = "Last HQL Query String is:";
            dlg.Detail = QueryBuilder.Instance.LastQuery;
            dlg.ShowDialog();
        }

        private void OnSearchExamineLastEdit(object sender, EventArgs e)
        {
            ErrorReportDialog dlg = new ErrorReportDialog();
            dlg.Text = "Last Edit";
            dlg.Message = "Last Edit Script is:";
            dlg.Detail = DepEditService.LastScriptingStatements;
            dlg.ShowDialog();
        }

        private void OnSearchSearchInView(object sender, EventArgs e)
        {
            string key = this.toolStripComboBox1.Text;
            if (key.Length > 0)
            {
                this.kwicView.FindNext(key);
                // DropDown候補を更新
                List<string> items = new List<string>();
                foreach (object item in this.toolStripComboBox1.Items)
                {
                    items.Add(item as string);
                }
                this.toolStripComboBox1.Items.Clear();
                this.toolStripComboBox1.Items.Add(key);
                int count = 1;
                foreach (string s in items)
                {
                    this.toolStripComboBox1.Items.Add(s);
                    if (++count >= 10) break;
                }
            }
        }

        private void OnFormatShiftPivotLeft(object sender, EventArgs e)
        {
            SearchHistory hist = this.historyGuidePanel.Current;
            if (hist == null) return;
            var cond0 = hist.Shift(-1);
            this.kwicView.ShiftPivot(-1);

            // TagCondもシフトする
            // （History中の条件とCurrentSearchCondtionsとはインスタンスが異なることがあるため。）
            SearchConditions cond = this.m_Model.CurrentSearchConditions;
            if (cond0 != cond && cond != null && cond.TagCond != null)
            {
                cond.TagCond.Shift(-1);
            }
        }

        private void OnFormatShiftPivotRight(object sender, EventArgs e)
        {
            SearchHistory hist = this.historyGuidePanel.Current;
            if (hist == null) return;
            var cond0 = hist.Shift(1);
            this.kwicView.ShiftPivot(1);

            // TagCondもシフトする
            // （History中の条件とCurrentSearchCondtionsとはインスタンスが異なることがあるため。）
            SearchConditions cond = this.m_Model.CurrentSearchConditions;
            if (cond0 != cond && cond != null && cond.TagCond != null)
            {
                cond.TagCond.Shift(1);
            }
        }

        private void OnFormatHilightPreviousWord(object sender, EventArgs e)
        {
            SearchHistory hist = this.historyGuidePanel.Current;
            if (hist == null) return;
            hist.ShiftHilight(-1);
        }

        private void OnFormatHilightNextWord(object sender, EventArgs e)
        {
            SearchHistory hist = this.historyGuidePanel.Current;
            if (hist == null) return;
            hist.ShiftHilight(1);
        }

        private void OnToolsCreateSQLiteCorpus(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new CreateSQLiteCorpus())
                {
                    dlg.ShowDialog();
                }
            }
            catch (BadImageFormatException ex)
            {
                MessageBox.Show(ex.Message);
                using (var dlg = new CreateSQLiteCorpusNoFileFolderDialog())
                {
                    dlg.ShowDialog();
                }
            }
        }

        private void OnToolsCreateMySQLCorpus(object sender, EventArgs e)
        {
            DBLogin dlg = new DBLogin();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                CreateMySQLCorpus dlg2 = new CreateMySQLCorpus();
                dlg2.DBMS = dlg.DBMS;
                dlg2.Server = dlg.Server;
                dlg2.User = dlg.User;
                dlg2.Password = dlg.Password;
                dlg2.DatabaseCandidates = dlg.Databases;
                dlg2.ShowDialog();
                dlg2.Dispose();
            }
        }


        private void OnToolsEditTagSetDefinitions(object sender, EventArgs e)
        {
            // Current Corpusをチェック
            Corpus c = ChaKiModel.CurrentCorpus;
            if (c == null)
            {
                MessageBox.Show("Corpus not selected.");
                return;
            }
            Process process = new Process();
            process.StartInfo.Domain = "";
            process.StartInfo.ErrorDialog = true;
            process.StartInfo.FileName = "TagSetDefinitionEditor.exe";
            process.StartInfo.LoadUserProfile = true;
            process.StartInfo.Password = null;
            process.StartInfo.StandardErrorEncoding = null;
            process.StartInfo.StandardOutputEncoding = null;
            process.StartInfo.UserName = "";
            if (c.Source == null || c.Source.Length == 0)
            {
                MessageBox.Show(string.Format("Could not find Corpus source path for \"{0}\".\n Please re-open the corpus in the Corpus Tab and try again.", c.Name));
                return;
            }

            // ID0のTagSet nameを得る
            var tagsetname = string.Empty;
            var svc = DBService.Create(c.DBParam);
            var names = svc.LoadTagSetNames();
            if (names.Count > 0)
            {
                tagsetname = names[0];
            }

            process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", c.Source, tagsetname);
            process.StartInfo.WorkingDirectory = Program.ProgramDir;
            process.Start();
        }

        private void OnToolsCreateDictionary(object sender, EventArgs e)
        {
            CreateDictionary dlg = new CreateDictionary();
            dlg.ShowDialog();
            dlg.Dispose();
        }

        private void OnToolsTextFormatter(object sender, EventArgs e)
        {
            Process process = new Process();
            process.StartInfo.Domain = "";
            process.StartInfo.ErrorDialog = true;
            process.StartInfo.FileName = "TextFormatter.exe";
            process.StartInfo.LoadUserProfile = true;
            process.StartInfo.Password = null;
            process.StartInfo.StandardErrorEncoding = null;
            process.StartInfo.StandardOutputEncoding = null;
            process.StartInfo.UserName = "";
            process.StartInfo.Arguments = null;
            process.StartInfo.WorkingDirectory = Program.ProgramDir;
            process.Start();
        }

        private void OnToolsText2Corpus(object sender, EventArgs e)
        {
            Process process = new Process();
            process.StartInfo.Domain = "";
            process.StartInfo.ErrorDialog = true;
            process.StartInfo.FileName = "Text2Corpus.exe";
            process.StartInfo.LoadUserProfile = true;
            process.StartInfo.Password = null;
            process.StartInfo.StandardErrorEncoding = null;
            process.StartInfo.StandardOutputEncoding = null;
            process.StartInfo.UserName = "";
            process.StartInfo.Arguments = null;
            process.StartInfo.WorkingDirectory = Program.ProgramDir;
            process.Start();
        }

        private void OnOptionsSettings(object sender, EventArgs e)
        {
            OptionDialog dlg = new OptionDialog();
            dlg.Applied = (sr, ea) => { this.contextPanel.Refresh(); this.dependencyEditPanel.Refresh(); };
            dlg.ShowDialog();   // OK時の処理はOptionDialog側で行う.
        }

        private void OnOptionsPropertyBoxSettings(object sender, EventArgs e)
        {
            PropertyBoxSettings temp = new PropertyBoxSettings(PropertyBoxSettings.Instance);
            CustomizePropertyBoxDialog dlg = new CustomizePropertyBoxDialog();
            dlg.Model = temp.Settings;
            dlg.UseShortPOS = GUISetting.Instance.UseShortPOS;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                PropertyBoxSettings.Instance.CopyFrom(temp);
                GUISetting.Instance.UseShortPOS = dlg.UseShortPOS;
                foreach (var panel in this.kwicView.Panels)
                {
                    panel.RecalcLayout();
                }
            }
        }

        private void OnOptionsWordColorSettings(object sender, EventArgs e)
        {
            Corpus cps = ChaKiModel.CurrentCorpus;
            if (cps == null)
            {
                MessageBox.Show("No Current Corpus Found.");
                return;
            }
            WordColorSettingDialog dlg = new WordColorSettingDialog(cps);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                dlg.UpdateModel();
                WordColorSettings.GetInstance().Save(Program.SettingFileWordColor);
                this.kwicView.Refresh();
            }
        }

        private void OnOptionsTagAppearance(object sender, EventArgs args)
        {
            TagAppearanceDialog dlg = new TagAppearanceDialog();
            dlg.RefreshRequested += new EventHandler(
                (o, e) =>
                {
                    SegmentPens.AddPens(TagSetting.Instance.Segment);
                    LinkPens.AddPens(TagSetting.Instance.Link);
                    this.kwicView.Refresh();
                    this.dependencyEditPanel.Refresh();
                });
            dlg.ShowDialog();
        }

        private void OnOptionsDictionarySettings(object sender, EventArgs args)
        {
            DictionarySettingDialog dlg = new DictionarySettingDialog();
            dlg.Corpora = this.condPanel.GetCorpusList();
            dlg.ShowDialog();
        }

        private void OnHelpHelp(object sender, EventArgs e)
        {
            try
            {
                Help.ShowHelp(this, Path.Combine(Program.ProgramDir, "chaki.chm"));
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Error:", ex);
                dlg.ShowDialog();
            }
        }

        private void OnHelpAbout(object sender, EventArgs e)
        {
            AboutChaKiDialog dlg = new AboutChaKiDialog();
            dlg.ShowDialog();
            dlg.Dispose();
        }

        /// <summary>
        /// WordListViewからの"Search Occurence"イベントのハンドラ.
        /// WordOccurence検索を行う.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWordOccurenceRequested(object sender, EventArgs e)
        {
            WordOccurrenceEventArgs args = (WordOccurrenceEventArgs)e;
            SearchHistory hist = this.historyGuidePanel.Current;
            try
            {
                KwicSearchWordOccurence(hist, args.AdditionalLexCondList);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Error while executing query:", ex);
                dlg.ShowDialog();
            }
        }

        /// <summary>
        /// CollocationViewからの"List Occurrences"のイベントハンドラ.
        /// Sentence ID Listから文検索を行う.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleCollocationOccurrenceRequested(object sender, SentenceIdsOccurrenceEventArgs e)
        {
            SearchHistory hist = this.historyGuidePanel.Current;
            try
            {
                KwicSearchSentenceOccurence(hist, e.SentenceIDList);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Error while executing query:", ex);
                dlg.ShowDialog();
            }
        }

        private void OnVersioncontrolCommit(object sender, EventArgs e)
        {
            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var svc = new GitService(ChaKiModel.CurrentCorpus);
                svc.CredentialProvider = ShowGitLogonDialogHandler;
                if (!svc.CheckRepositoryExists())
                {
                    if (MessageBox.Show(Resources.CreateRepositoryMessage, "Confirm",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }
                    svc.CreateRepository();
                }
                svc.OpenRepository();
                var dlg = new GitCommitDialog(svc, ChaKiModel.CurrentCorpus);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        svc.Commit(dlg.Message);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    if (dlg.AutoPush)
                    {
                        try
                        {
                            svc.Push();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var dlg = new ErrorReportDialog("Error:", ex);
                dlg.ShowDialog();
            }
            finally
            {
                Cursor.Current = cur;
            }
        }

        private Tuple<string, string> ShowGitLogonDialogHandler(string url)
        {
            var dlg = new GitLoginDialog();
            dlg.Url = url;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return Tuple.Create(dlg.Username, dlg.Password);
            }
            return null;
        }


        private void OnVersioncontrolCommitUpdate(object sender, EventArgs e)
        {
            try
            {
                this.UICVersioncontrolCommitToolStripButton.Enabled = (ChaKiModel.CurrentCorpus != null);
            }
            catch (Exception ex)
            {
            }
        }

        private void OnDictionaryExport(object sender, EventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog();
                dlg.Title = "Export Lexicon to...";
                dlg.CheckFileExists = false;
                dlg.DefaultExt = ".mecab";
                dlg.Filter = "Mecab|*.mecab|Mecab-Unidic|*.mecab";
                dlg.FilterIndex = 2;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var filename = dlg.FileName;
                    ReaderDef def;
                    if (dlg.FilterIndex == 1)
                    {
                        def = CorpusSourceReaderFactory.Instance.ReaderDefs.Find("Mecab|Cabocha");
                    }
                    else
                    {
                        def = CorpusSourceReaderFactory.Instance.ReaderDefs.Find("Mecab|Cabocha|UniDic2");
                    }

                    var cancelFlag = false;
                    var waitDone = new ManualResetEvent(false);
                    using (var progress = new ProgressDialog())
                    {
                        progress.Title = "Exporting...";
                        progress.ProgressMax = 100;
                        progress.ProgressReset();
                        progress.WorkerCancelled += (o, a) => { cancelFlag = true; };
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            try
                            {
                                using (var wr = new StreamWriter(filename))
                                {
                                    var svc = new ExportServiceMecab(wr, def);
                                    svc.ExportDictionary(ChaKiModel.CurrentCorpus, ref cancelFlag, p => { progress.ProgressCount = p; });
                                }
                            }
                            catch (Exception ex)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    var edlg = new ErrorReportDialog("Error while executing lexicon export:", ex);
                                    edlg.ShowDialog();
                                }), null);
                            }
                            finally
                            {
                                progress.DialogResult = DialogResult.Cancel;
                                waitDone.Set();
                            }
                        });
                        progress.ShowDialog();
                        waitDone.WaitOne();
                    }
                }
            }
            catch (Exception ex)
            {
                var edlg = new ErrorReportDialog("Error while executing lexicon export:", ex);
                edlg.ShowDialog();
            }
        }

        private void OnDictionaryExportUpdate(object sender, EventArgs e)
        {
            this.UICDictionaryExportToolStripMenuItem.Enabled = (ChaKiModel.CurrentCorpus != null);
        }

        private void OnDictionaryExportMWE(object sender, EventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog();
                dlg.Title = "Export MWE to...";
                dlg.CheckFileExists = false;
                dlg.DefaultExt = ".conll";
                dlg.Filter = "CONLL|*.conll";
                dlg.FilterIndex = 0;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var filename = dlg.FileName;
                    var cancelFlag = false;
                    var waitDone = new ManualResetEvent(false);
                    using (var progress = new ProgressDialog())
                    {
                        progress.Title = "Exporting...";
                        progress.ProgressMax = 100;
                        progress.ProgressReset();
                        progress.WorkerCancelled += (o, a) => { cancelFlag = true; };
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            try
                            {
                                using (var wr = new StreamWriter(filename))
                                {
                                    var svc = new ExportServiceMweToConll(wr);
                                    svc.ExportMWE(ChaKiModel.CurrentCorpus, ref cancelFlag, (p, o) => { progress.ProgressCount = p; });
                                }
                            }
                            catch (Exception ex)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    var edlg = new ErrorReportDialog("Error while executing MWE export:", ex);
                                    edlg.ShowDialog();
                                }), null);
                            }
                            finally
                            {
                                progress.DialogResult = DialogResult.Cancel;
                                waitDone.Set();
                            }
                        });
                        progress.ShowDialog();
                        waitDone.WaitOne();
                    }
                }
            }
            catch (Exception ex)
            {
                var edlg = new ErrorReportDialog("Error while executing MWE export:", ex);
                edlg.ShowDialog();
            }
        }

        private void OnDictionaryExportMWEUpdate(object sender, EventArgs e)
        {
            this.UICDictionaryExportMWEToolStripMenuItem.Enabled = (ChaKiModel.CurrentCorpus != null);
        }

    }
}
