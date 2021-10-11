using ChaKi.Common;
using ChaKi.Common.Widgets;
using ChaKi.GUICommon;
using ChaKi.Service.Readers;
using PopupControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using MessageBox = ChaKi.Common.Widgets.MessageBox;
using Timer = System.Windows.Forms.Timer;

namespace ChaKi.DocEdit
{
    public partial class DocEditView : Form
    {
        private const string m_Title = "ChaKi.NET/DocEdit";
        private string m_CorpusName;
        private DocEdit m_DocEdit;
        private Popup m_DocSelectorPopup;
        private DocumentSelector m_DocSelector;
        private Popup m_SentenceAttributeSelectorPopup;
        private AttributeListSelector m_SentenceAttributeSelector;

        private int[] m_RowToSentenceIds;
        private Dictionary<int, int> m_SentenceIdToRows;
        private Dictionary<int, string[]> m_SentenceList; // senid to string[senid, text, docid] mapping
        private bool m_GridUpdateRequested;
        private Timer m_GridUpdateTimer;

        private List<int> m_SelectedSenIds;
        private int m_SelectedDocId;

        private enum SentenceListColumns { ID = 0, Text = 1, DocID = 2 }

        public DocEditView()
        {
            m_GridUpdateTimer = new Timer() { Interval = 100 };
            m_GridUpdateTimer.Tick += new EventHandler(m_GridUpdateTimer_Tick);
            m_GridUpdateRequested = false;
            m_GridUpdateTimer.Start();
            m_DocEdit = new DocEdit();
            m_DocEdit.SentenceTextListUpdated += new Action<int>(m_DocEdit_SentenceTextListUpdated);
            m_DocEdit.IsDirtyChanged += new EventHandler(m_DocEdit_IsDirtyChanged);
            m_DocSelector = new DocumentSelector();
            m_DocSelectorPopup = new Popup(m_DocSelector) { Resizable = true };
            m_SelectedSenIds = new List<int>();
            m_SelectedDocId = -1;

            InitializeComponent();

            this.dataGridView1.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Sentence No." });
            this.dataGridView1.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Sentence Snippet" });
            this.dataGridView1.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Document ID" });
            this.dataGridView1.Columns[0].DefaultCellStyle.BackColor = Color.Linen;
            this.dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.dataGridView1.Columns[1].DefaultCellStyle.BackColor = Color.Linen;
            this.dataGridView1.Columns[2].DefaultCellStyle.BackColor = Color.White;
            this.dataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            var gridWidths = DocEditSettings.Instance.GridWidthSentenceList;
            for (var i = 0; i < gridWidths.Count && i < 3; i++)
            {
                this.dataGridView1.Columns[i].Width = gridWidths[i];
            }
            this.attributeGrid1.UpdateUIState += new EventHandler(attributeGrid1_UpdateUIState);
            this.attributeGrid1.AddFromListHandler = ShowAttributeListPopup;
            m_SentenceAttributeSelector = new AttributeListSelector("Sentence");
            m_SentenceAttributeSelectorPopup = new Popup(m_SentenceAttributeSelector) { Resizable = true };
            m_SentenceAttributeSelector.TagSelected += new EventHandler(m_SentenceAttributeSelector_TagSelected);

            this.toolStripButton1.Checked = false;
            this.toolStripButton1.Enabled = false;
            this.toolStripButton2.Enabled = false;
            this.toolStripButton3.Enabled = false;
            this.toolStripButton4.Enabled = false;

            this.button1.Enabled = false;
            this.exportAsCSVToolStripMenuItem.Enabled = false;
            this.CommitToolStripMenuItem.Enabled = false;
            this.importFromCSVToolStripMenuItem.Enabled = false;

            this.Text = m_Title;
        }

        private void ShowAttributeListPopup(Control parent)
        {
            // Retrieve Tag List (= existing Sentence attributes) from Database service
            m_SentenceAttributeSelector.Reset();
            var allsenattrs = m_DocEdit.GetSentenceAttributes();
            foreach (var sattr in allsenattrs)
            {
                m_SentenceAttributeSelector.AddTag(sattr);
            }
            m_SentenceAttributeSelectorPopup.Show(parent);
        }

        void m_SentenceAttributeSelector_TagSelected(object sender, EventArgs e)
        {
            var sel = m_SentenceAttributeSelector.Selection;
            if (sel != null)
            {
                this.attributeGrid1.InsertItem(sel.Key, sel.Value);
            }
        }

        void m_GridUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (m_GridUpdateRequested)
            {
                this.dataGridView1.BeginInvoke(new Action(() => this.dataGridView1.Refresh()), null);
                m_GridUpdateRequested = false;
            }
        }

        void m_DocEdit_SentenceTextListUpdated(int senid)
        {
//            m_GridUpdateRequested = true;
            int row;
            if (m_SentenceIdToRows.TryGetValue(senid, out row))
            {
                this.dataGridView1.Invoke(new Action(() => this.dataGridView1.InvalidateCell(1, row)), null);
            }
        }

        public void OpenCorpus(string corpusName)
        {
            if (!ConfirmLeaving())
            {
                return;
            }

            m_CorpusName = corpusName;
            var oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                m_DocEdit.OpenCorpus(corpusName);
                this.dataGridView1.Rows.Clear();
                m_RowToSentenceIds = m_DocEdit.GetSentenceIdList();
                m_SentenceIdToRows = new Dictionary<int,int>();
                for (int i = 0; i < m_RowToSentenceIds.Length; i++)
                {
                    m_SentenceIdToRows.Add(i, m_RowToSentenceIds[i]);
                }
                m_SentenceList = m_DocEdit.GetSentenceList();
            }
            finally
            {
                this.Cursor = oldCur;
            }
            if (m_SentenceList.Count == 0)
            {
                this.dataGridView1.Rows.Clear();
            }
            else
            {
                this.dataGridView1.Rows.Add(m_SentenceList.Count);
            }

            this.Text = string.Format("{0} [{1}]", m_Title, m_CorpusName);
            this.attributeGrid1.IsEditing = true;
            this.exportAsCSVToolStripMenuItem.Enabled = true;
            this.importFromCSVToolStripMenuItem.Enabled = true;
        }

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            int row = e.RowIndex;
            int col = e.ColumnIndex;
            var senid = m_RowToSentenceIds[row];
            if (col == (int)SentenceListColumns.Text)
            {
                // Get Sentence Text
                e.Value = m_DocEdit.GetSentenceTextAsync(senid);
            }
            else
            {
                e.Value = m_SentenceList[senid][col];
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // Selectionの変更前に、現在のAttribute編集をセーブするか否かを確認.
            ConfirmLeavingAttributeEdit(false);

            m_SelectedSenIds = new List<int>();

            m_SelectedDocId = -1;
            bool uniquedocid = true;
            foreach (DataGridViewRow row in this.dataGridView1.SelectedRows)
            {
                var senid = m_RowToSentenceIds[row.Index];
                m_SelectedSenIds.Add(senid);
                int docid_currentrow;
                if (Int32.TryParse((string)this.dataGridView1[2, row.Index].Value, out docid_currentrow))
                {
                    if (uniquedocid && m_SelectedDocId == -1)
                    {
                        m_SelectedDocId = docid_currentrow;
                    }
                    else if (m_SelectedDocId != docid_currentrow)
                    {
                        m_SelectedDocId = -1;
                        uniquedocid = false;
                    }
                }
            }

            this.attributeGrid1.ClearUndoRedo();
            // Save Grid Column width before clearing.
            if (this.attributeGrid1.Columns.Count > 0)
            {
                var gridWidths = DocEditSettings.Instance.GridWidthAttributeList;
                gridWidths.Clear();
                foreach (DataGridViewColumn col in this.attributeGrid1.Columns)
                {
                    gridWidths.Add(col.Width);
                }
            }

            if (m_SelectedSenIds.Count == 0)
            {
                this.attributeGrid1.Model = new AttributeGridData(); // set empty
                return;
            }

            var model = new AttributeGridData();
            var rows = model.Rows;

            try
            {
                var docattr = m_DocEdit.GetDocumentAttribute(m_SelectedDocId);
                var senattr = m_DocEdit.GetSentenceAttribute(m_SelectedSenIds);

                rows.Add(new GroupData(GroupData.DOCUMENT));
                rows.Add(new AttributeData("DocumentID", uniquedocid ? m_SelectedDocId.ToString() : "(multiple)", AttributeGridRowType.ReadOnly));
                foreach (var pair in docattr)
                {
                    rows.Add(new AttributeData(pair.Key, pair.Value, AttributeGridRowType.KeyValueWritable));
                }
                rows.Add(new GroupData(GroupData.SENTENCE));
                rows.Add(new AttributeData("SentenceID", m_SelectedSenIds.Count == 1 ? m_SelectedSenIds[0].ToString() : "(multiple)", AttributeGridRowType.ReadOnly));
                foreach (var pair in senattr)
                {
                    rows.Add(new AttributeData(pair.Key, pair.Value, AttributeGridRowType.KeyValueWritable));
                }
                model.Sort();
            }
            catch (Exception ex)
            {
                new ErrorReportDialog("Failed to load attributes:", ex).ShowDialog();
            }
            this.attributeGrid1.Model = model;
            this.attributeGrid1.IsEditing = true;
            this.toolStripButton1.Enabled = true;

            // Restore Column Width Settings
            {
                var gridWidths = DocEditSettings.Instance.GridWidthAttributeList;
                for (var i = 0; i < gridWidths.Count && i < 2; i++)
                {
                    this.attributeGrid1.Columns[i].Width = gridWidths[i];
                }
            }
        }

        private void dataGridView1_CellContextMenuStripNeeded(object sender, DataGridViewCellContextMenuStripNeededEventArgs e)
        {
            // 選択されていない行で右クリック→カレント選択をクリアしてその行を新たに選択状態にする
            // 選択されている行で右クリック→カレント選択をクリアしない
            if (!dataGridView1[e.ColumnIndex, e.RowIndex].Selected)
            {
                dataGridView1.ClearSelection();
                dataGridView1[e.ColumnIndex, e.RowIndex].Selected = true;
            }
            var location = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int docid;
            if (!Int32.TryParse(dataGridView1[(int)SentenceListColumns.DocID, e.RowIndex].Value as string, out docid))
            {
                return;
            }
            m_DocSelector.Populate(m_DocEdit.GetDocumentList());
            m_DocSelector.SelectDocument(docid);
            m_DocSelector.DocidSelected += new Action<int>(m_DocSelector_DocidSelected);
            m_DocSelectorPopup.Show(dataGridView1.PointToScreen(new Point(location.Right, location.Bottom)));
        }

        private void m_DocSelector_DocidSelected(int newdocid)
        {
            var senids = new List<int>();
            foreach (DataGridViewRow row in this.dataGridView1.SelectedRows)
            {
                var senid = m_RowToSentenceIds[row.Index];
                senids.Add(senid);
            }

            // update database model
            if (m_DocEdit.AssignDocumentIdToSentences(senids, newdocid))
            {
                // Update local model
                string newdocid_str = newdocid.ToString();
                foreach (int senid in senids)
                {
                    m_SentenceList[senid][(int)SentenceListColumns.DocID] = newdocid_str;
                }
                // update grid view
                foreach (DataGridViewRow row in this.dataGridView1.SelectedRows)
                {
                    this.dataGridView1.InvalidateRow(row.Index);
                }
            }
        }

        // AttributeEdit BeginEdit Command Handler
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (!this.attributeGrid1.IsEditing)
            {
                // Enter Edit Mode
                this.attributeGrid1.IsEditing = true;
            }
            else
            {
                // 1. Confirm to cancel current editing
                if (this.attributeGrid1.CanUndo)
                {
                    if (MessageBox.Show("Leave Edit Mode without Saving?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                    this.attributeGrid1.RewindUndo();  // 初期状態に戻す.
                }

                // 2. Reset EditMode
                this.attributeGrid1.IsEditing = false;
            }
        }

        // AttributeEdit Undo Command Handler
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            this.attributeGrid1.Undo();
        }

        // AttributeEdit Redo Command Handler
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            this.attributeGrid1.Redo();
        }

        // AttributeEdit Save Command Handler
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            try
            {
                SaveAttributes();
                this.attributeGrid1.IsEditing = false;
                this.attributeGrid1.ClearUndoRedo();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot save: ", ex);
                dlg.ShowDialog();
            }
        }

        void attributeGrid1_UpdateUIState(object sender, EventArgs e)
        {
            var action = new Action(() =>
            {
                this.toolStripButton1.Checked = this.attributeGrid1.IsEditing;
                this.toolStripButton2.Enabled = this.attributeGrid1.CanUndo;
                this.toolStripButton3.Enabled = this.attributeGrid1.CanRedo;
                this.toolStripButton4.Enabled = this.attributeGrid1.CanUndo;
            });
            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }

        void m_DocEdit_IsDirtyChanged(object sender, EventArgs e)
        {
            var action = new Action(() =>
            {
                this.button1.Enabled = m_DocEdit.IsDirty;
                this.CommitToolStripMenuItem.Enabled = m_DocEdit.IsDirty;
                this.Text = string.Format("{0} [{1}]*", m_Title, m_CorpusName);
            });
            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }

        // AttributeGridの編集中であれば、Save/Cancelを確認する.
        private bool ConfirmLeavingAttributeEdit(bool canCancel)
        {
            if (this.attributeGrid1.CanUndo)  //Saveすべきデータが残っている
            {
                DialogResult res = MessageBox.Show("Save Current Changes?", "AttributeEdit",
                    canCancel ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    SaveAttributes();
                }
                else if (res == DialogResult.No)
                {
                }
                else if (res == DialogResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        // Commitすべき変更があれば、Save/Cancelを確認する.
        private bool ConfirmLeaving()
        {
            if (!ConfirmLeavingAttributeEdit(true))
            {
                return false;
            }

            if (m_DocEdit.IsDirty)  //Commitすべきデータが残っている
            {
                DialogResult res = MessageBox.Show("Commit Current Changes?", "DocEdit", MessageBoxButtons.YesNoCancel);
                if (res == DialogResult.Yes)
                {
                    CommitAsync();
                }
                else if (res == DialogResult.No)
                {
                }
                else if (res == DialogResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }


        // 終了確認
        private void DocEditView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmLeaving())
            {
                e.Cancel = true;
                return;
            }

            // Save Settings
            DocEditSettings.Instance.WindowLocation = this.Bounds;
            var gridWidths = DocEditSettings.Instance.GridWidthSentenceList;
            gridWidths.Clear();
            foreach (DataGridViewColumn col in this.dataGridView1.Columns)
            {
                gridWidths.Add(col.Width);
            }
            if (this.attributeGrid1.Columns.Count > 0)
            {

                gridWidths = DocEditSettings.Instance.GridWidthAttributeList;
                gridWidths.Clear();
                foreach (DataGridViewColumn col in this.attributeGrid1.Columns)
                {
                    gridWidths.Add(col.Width);
                }
            }
        }

        // attributeGridの内容をDatabaseに反映する.
        private void SaveAttributes()
        {
            var viewModel = this.attributeGrid1.Model;

            // ( GroupName --> ( Key --> Value )* )*
            var updateDataTable = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> currentGroupData = null;
            foreach (AttributeGridRowData rd in viewModel.Rows)
            {
                if (rd is GroupData)
                {
                    GroupData gd = rd as GroupData;
                    currentGroupData = new Dictionary<string, string>();
                    updateDataTable[gd.Name] = currentGroupData;

                }
                else if (rd is AttributeData)
                {
                    AttributeData ad = rd as AttributeData;
                    if (ad.RowType == AttributeGridRowType.ReadOnly)
                    {
                        continue;
                    }
                    if (currentGroupData.ContainsKey(ad.Key))
                    {
                        throw new InvalidOperationException(string.Format("Duplicated Key found: {0}", ad.Key));
                    }
                    currentGroupData[ad.Key] = ad.Value;
                }
            }
            m_DocEdit.UpdateAttributesForSentences(m_SelectedDocId, m_SelectedSenIds, updateDataTable[GroupData.SENTENCE], updateDataTable[GroupData.DOCUMENT]);

            this.attributeGrid1.ClearUndoRedo();
        }

        // Commit and Exit
        private void button1_Click(object sender, EventArgs e)
        {
            var oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                ConfirmLeavingAttributeEdit(false);
                CommitAsync();
                m_DocEdit.CloseCorpus();
            }
            catch (Exception ex)
            {
                new ErrorReportDialog("Cannot commit: ", ex).ShowDialog();
            }
            finally
            {
                this.Cursor = oldCur;
            }
            Close();
        }

        // Rollback and Exit
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                ConfirmLeavingAttributeEdit(false);
                m_DocEdit.CloseCorpus();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot exit: ", ex);
                dlg.ShowDialog();
            }
            Close();
        }

        // Commit operation
        private void CommitAsync()
        {
            using (var progress = new ProgressDialog())
            {
                progress.CancelEnabled = false;
                progress.Work = DoCommit;
                progress.WorkerParameter = new object[] {
                    progress,
                    new Action<Exception>(ex => new ErrorReportDialog("Cannot commit: ", ex).ShowDialog())
                };  // Work parameter
                progress.ShowDialog();
            }
        }

        public void Commit()
        {
            var args = new DoWorkEventArgs(new object[] {
                new ConsoleProgress(),
                new Action<Exception>(ex => Console.Error.WriteLine(ex)) });
            DoCommit(null, args);
        }

        private void DoCommit(object sender, DoWorkEventArgs e)
        {
            var progress = (IProgress)((object[])e.Argument)[0];
            var erroraction = (Action<Exception>)((object[])e.Argument)[1];
            try
            {
                m_DocEdit.Commit(progress);
            }
            catch (Exception ex)
            {
                erroraction(ex);
            }
            finally
            {
                progress.EndWork();
            }
        }

        // Open command
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Database files (*.db;*.def)|*.db;*.def|SQLite database files (*.db)|*.db|Corpus definition files (*.def)|*.def|All files (*.*)|*.*";
                dlg.Title = "Select Corpus to Open";
                dlg.CheckFileExists = true;
                dlg.Multiselect = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        OpenCorpus(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        new ErrorReportDialog("Cannot open: ", ex).ShowDialog();
                    }
                }
            }
        }

        // Commit
        private void CommitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                CommitAsync();
                m_DocEdit.CloseCorpus();
                // Reopen
                m_DocEdit.OpenCorpus(m_CorpusName);
            }
            catch (Exception ex)
            {
                new ErrorReportDialog("Cannot commit: ", ex).ShowDialog();
            }
            finally
            {
                this.Cursor = oldCur;
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new DocEditSettingsDialog();
            dlg.TextSnipLength = DocEditSettings.Instance.TextSnipLength;
            dlg.UILocale = DocEditSettings.Instance.UILocale;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DocEditSettings.Instance.TextSnipLength = dlg.TextSnipLength;
                DocEditSettings.Instance.UILocale = dlg.UILocale;
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(DocEditSettings.Instance.UILocale);
            }
        }

        private void exportAsCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            dlg.Title = "Select CSV Filename to save";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                using (var wr = new StreamWriter(dlg.FileName))
                using (var progress = new ProgressDialog())
                {
                    progress.Work = DoExportCSV;
                    progress.WorkerParameter = new object[] { progress, wr }; // Work parameter
                    progress.ShowDialog();
                }
            }
        }

        private void DoExportCSV(object sender, DoWorkEventArgs e)
        {
            var progress = (IProgress)((object[])e.Argument)[0];
            var wr = (TextWriter)((object[])e.Argument)[1];

            try
            {
                wr.WriteLine("sentence_id,text,document_id,document_attributes,sentence_attributes");
                var documentIdsExported = new Dictionary<int, bool>();
                int n = 0;
                progress.ProgressMax = m_SentenceList.Count;
                foreach (var item in m_SentenceList)
                {
                    var senid = item.Key;
                    var text = m_DocEdit.GetSentenceTextSync(senid).Replace("\"", "\\\"");
                    var dattr = string.Empty;
                    int docid = -1;
                    Int32.TryParse(item.Value[2], out docid);
                    if (!documentIdsExported.ContainsKey(docid))
                    {
                        // first document to export
                        dattr = AttributeToXmlFragment(m_DocEdit.GetDocumentAttribute(docid));
                        documentIdsExported.Add(docid, true);
                    }
                    var sattr = AttributeToXmlFragment(m_DocEdit.GetSentenceAttribute(senid));
                    wr.WriteLine("{0},\"{1}\",{2},\"{3}\",\"{4}\"", item.Value[0], text, item.Value[2], dattr, sattr);
                    n++;
                    progress.ProgressCount = n;
                    if (progress.Canceled)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                new ErrorReportDialog("Cannot save: ", ex).ShowDialog();
            }
            finally
            {
                progress.EndWork();
            }
        }

        private string AttributeToXmlFragment(Dictionary<string,string> attrs)
        {
            using (var stream = new StringWriter())
            {
                using (var wr = XmlWriter.Create(stream, 
                    new XmlWriterSettings()
                    {
                        ConformanceLevel = ConformanceLevel.Fragment,
                        Encoding = new UTF8Encoding()
                    }))
                {
                    foreach (var attr in attrs)
                    {
                        wr.WriteStartElement(attr.Key);
                        wr.WriteString(attr.Value);
                        wr.WriteEndElement();
                    }
                }
                return stream.ToString().Replace("\"", "&quot;");
            }
        }

        private Dictionary<string, string> XmlFragmentToAttributes(string xmlstr)
        {
            var result = new Dictionary<string, string>();
            xmlstr = string.Format("<root>{0}</root>", xmlstr);
            using (var stream = new StringReader(xmlstr))
            using (var rdr = XmlReader.Create(stream, 
                    new XmlReaderSettings()
                    {
                        ConformanceLevel = ConformanceLevel.Fragment
                    }))
            {
                rdr.Read();
                while (rdr.Read())
                {
                    if (rdr.IsStartElement())
                    {
                        string key = rdr.Name;
                        rdr.Read();
                        string value = rdr.ReadContentAsString();
                        result.Add(key, value);
                    }
                }
            }

            return result;
        }

        private void importFromCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            dlg.Title = "Select CSV Filename to open";
            dlg.CheckFileExists = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ImportFromCSVAsync(dlg.FileName);
            }
        }

        public void ImportFromCSVAsync(string filename)
        {
            using (var strm = new FileStream(filename, FileMode.Open))
            using (var progress = new ProgressDialog())
            {
                progress.Work = DoLoadCSV;
                progress.WorkerParameter = new object[] { progress, strm, new Action<Exception>(ex => new ErrorReportDialog("Cannot load: ", ex).ShowDialog()) }; // Work parameter
                progress.ShowDialog();
            }
        }

        public void ImportFromCSV(string filename)
        {
            try
            {
                using (var strm = new FileStream(filename, FileMode.Open))
                {
                    var args = new DoWorkEventArgs(new object[] { new ConsoleProgress(), strm, new Action<Exception>(ex => Console.Error.WriteLine(ex)) });
                    DoLoadCSV(null, args);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        private void DoLoadCSV(object sender, DoWorkEventArgs e)
        {
            var progress = (IProgress)((object[])e.Argument)[0];
            var strm = (FileStream)((object[])e.Argument)[1];
            var erroraction = (Action<Exception>)((object[])e.Argument)[2];
            var rdr = new StreamReader(strm, new UTF8Encoding());
            var docAttrSavedDocIds = new List<int>();

            try
            {
                int n = 0;
                string line;
                var regex = new Regex("^[0-9]+,");
                while ((line = rdr.ReadLine()) != null)
                {
                    if (regex.IsMatch(line))
                    {
                        n++;
                    }
                }
                if (n != m_SentenceList.Count)
                {
                    throw new Exception("Sentence count mismatch");
                }
                strm.Seek(0, SeekOrigin.Begin);
                progress.ProgressMax = n;
                n = 0;
                while ((line = rdr.ReadLine()) != null)
                {
                    List<String> fields;
                    try
                    {
                        fields = LexiconBuilder.CsvTextToFields(line);
                        if (fields.Count != 5)
                        {
                            throw new Exception(string.Format("[{0}] Invalid field count: {1}\n", n + 1, line));
                        }
                        int senid;
                        int docid;
                        if (!Int32.TryParse(fields[0], out senid))
                        {
                            continue;
                        }
                        if (!Int32.TryParse(fields[2], out docid))
                        {
                            continue;
                        }
                        Dictionary<string, string> dattrs = null;
                        if (!docAttrSavedDocIds.Contains(docid))
                        {
                            // document attrは、各Documentについて、最初の行にあるもの以外は無視.
                            dattrs = XmlFragmentToAttributes(fields[3]);
                            docAttrSavedDocIds.Add(docid);
                        }
                        var sattrs = XmlFragmentToAttributes(fields[4]);

                        // Update model
                        m_DocEdit.AssignDocumentIdToSentence(senid, docid);
                        m_DocEdit.UpdateAttributesForSentence(docid, senid, sattrs, dattrs);
                        // Update local model
                        m_SentenceList[senid][(int)SentenceListColumns.DocID] = docid.ToString();

                        n++;
                        progress.ProgressCount = n;
                    }
                    catch (IOException ex)
                    {
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        // Other than an IOException, continue.
                    }
                }
                bool b = this.InvokeRequired;
                var action = new Action(() =>
                    {
                        m_DocEdit.InvalidateCache();
                        if (this.dataGridView1.IsHandleCreated)
                        {
                            this.dataGridView1.Refresh();
                        }
                    });
                if (this.InvokeRequired)
                {
                    this.Invoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                erroraction(ex);
            }
            finally
            {
                progress.EndWork();
            }
        }
    }
}
