using ChaKi.Common.Settings;
using ChaKi.Common.Widgets;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Readers;
using ChaKi.GUICommon;
using ChaKi.Service.Database;
using ChaKi.Service.Export;
using ChaKi.Service.Lexicons;
using ChaKi.Service.Readers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using MessageBox = ChaKi.Common.Widgets.MessageBox;

namespace DependencyEditSLA
{
    public partial class DictionarySettingDialog : Form
    {
        public List<Corpus> Corpora { get; set; }
        private DataTable m_Table;
        private const int C_READONLY = 2;
        private const int C_USERDIC = 3;
        private const int C_COMPOUND = 4;


        public DictionarySettingDialog()
        {
            this.Corpora = new List<Corpus>();

            InitializeComponent();
        }

        private void DictionarySettingDialog_Load(object sender, EventArgs e)
        {
            DictionarySettings settings = DictionarySettings.Instance;

            m_Table = new DataTable();
            DataColumn col;
            col = new DataColumn("Name", typeof(string));
            m_Table.Columns.Add(col);
            col = new DataColumn("Path", typeof(string)) { ReadOnly = true };
            m_Table.Columns.Add(col);
            col = new DataColumn("ReadOnly", typeof(bool)) { ReadOnly = true };
            m_Table.Columns.Add(col);
            col = new DataColumn("UserDic", typeof(bool));
            m_Table.Columns.Add(col);
            col = new DataColumn("CompoundWord", typeof(bool));
            m_Table.Columns.Add(col);

            foreach (DictionarySettingItem item in settings)
            {
                DataRow row = m_Table.NewRow();
                row[0] = item.Name;
                row[1] = item.Path;
                row[2] = item.ReadOnly;
                row[3] = item.IsUserDic;
                row[4] = item.IsCompoundWordDic;
                m_Table.Rows.Add(row);
            }

            this.dataGridView1.DataSource = m_Table;
            this.dataGridView1.Columns[0].Width = 100;
            this.dataGridView1.Columns[1].Width = 200;
            this.dataGridView1.Columns[2].Width = 70;
            this.dataGridView1.Columns[3].Width = 70;
            this.dataGridView1.Columns[4].Width = 70;
        }

        private void Accept()
        {
            DictionarySettings settings = DictionarySettings.Instance;
            settings.Clear();

            m_Table.AcceptChanges();
            foreach (DataRow row in m_Table.Rows)
            {
                DictionarySettingItem item = new DictionarySettingItem();
                item.Name = (string)row[0];
                item.Path = (string)row[1];
                item.ReadOnly = (bool)row[2];
                item.IsUserDic = (bool)row[3];
                item.IsCompoundWordDic = (bool)row[4];
                settings.Add(item);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Dictionary DB to add";
            dlg.CheckFileExists = true;
            dlg.DefaultExt = ".db";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DataRow row = m_Table.NewRow();
                row[0] = Path.GetFileNameWithoutExtension(dlg.FileName);
                row[1] = dlg.FileName;
                row[2] = true;
                row[3] = false;
                row[4] = false;
                m_Table.Rows.Add(row);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var dlg = new OpenWebServiceDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var url = new Uri(dlg.Url);
                    DataRow row = m_Table.NewRow();
                    row[0] = url.Host;
                    row[1] = url.ToString();
                    row[2] = true;
                    row[3] = false;
                    row[4] = true;
                    m_Table.Rows.Add(row);
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid URL specified.", "Error", MessageBoxButtons.OK);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            while (this.dataGridView1.SelectedRows.Count > 0)
            {
                this.dataGridView1.Rows.Remove(this.dataGridView1.SelectedRows[0]);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Accept();
        }

        /// <summary>
        /// ユーザー辞書更新コマンド
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            var refdics = new List<string>();
            var userdic = string.Empty;

            try
            {
                m_Table.AcceptChanges();
                foreach (DataRow row in m_Table.Rows)
                {
                    refdics.Add((string)row[1]);
                    if ((bool)row[3])
                    {
                        if (userdic.Length == 0)
                        {
                            userdic = (string)row[1];
                        }
                        else
                        {
                            MessageBox.Show("Only one userdic is allowed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                if (this.Corpora.Count == 0)
                {
                    MessageBox.Show("No Corpus selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (userdic.Length == 0)
                {
                    MessageBox.Show("No User Dictionary found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ILexemeEditService service = new LexemeEditService();
                List<LexemeCorpusBoolLongTuple> list = null;
                var progress = new ProgressDialog();
                progress.Work = (s, ev) =>
                { 
                    list = service.ListLexemesNotInRefDic(this.Corpora, refdics, progress);
                };
                progress.ShowDialog();
                if (progress.Canceled || list == null)
                {
                    return;
                }
                if (list.Count > 0)
                {
                    var checkdlg = new LexemeListCheckDialog();
                    checkdlg.Model = list;
                    // 参照用辞書を含め、全ての使用可能なPOS, CType, CFormタグのリストを得て、PropTreeにセットする.
                    Dictionary<string, IList<PartOfSpeech>> pos;  // stringはDictionary名（カレントコーパスは"Default"）
                    Dictionary<string, IList<CType>> ctypes;
                    Dictionary<string, IList<CForm>> cforms;
                    service.GetLexiconTags(out pos, out ctypes, out cforms);
                    checkdlg.SetLexiconTags(pos, ctypes, cforms);
                    if (checkdlg.ShowDialog() == DialogResult.OK)
                    {
                        var oldCur = this.Cursor;
                        try
                        {
                            this.Cursor = Cursors.WaitCursor;
                            service.UpdateCorpusInternalDictionaries(list);
                            service.AddToUserDictionary(list, userdic);
                        }
                        finally
                        {
                            this.Cursor = oldCur;
                        }
                        MessageBox.Show("UserDic Successfully Updated.");
                    }
                }
                else
                {
                    MessageBox.Show("Nothing to add to UserDic.");
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error while executing userdic update:", ex);
                edlg.ShowDialog();
            }
        }

        // Col 3 を単一チェック動作にする
        // Col 3がチェックされたらCol 2 (ReadOnly)のチェックをはずす.
        // Col 4 を単一チェック動作にする
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.EndEdit(); // CheckBoxの状態を確定させる.

            if (e.ColumnIndex == C_USERDIC)
            {
                m_Table.AcceptChanges();
                MakeUserDicRow(e.RowIndex);
            }
            if (e.ColumnIndex == C_COMPOUND)
            {
                m_Table.AcceptChanges();
                MakeSingleSel(C_COMPOUND, e.RowIndex);
            }
        }

        private void MakeUserDicRow(int row)
        {
            MakeSingleSel(C_USERDIC, row);

            m_Table.Columns[C_READONLY].ReadOnly = false;
            for (int r = 0; r < m_Table.Rows.Count; r++)
            {
                var rdata = m_Table.Rows[r];
                rdata[C_READONLY] = !(bool)rdata[C_USERDIC];
            }
            m_Table.Columns[C_READONLY].ReadOnly = true;
        }

        private void MakeSingleSel(int col, int row)
        {
            for (int r = 0; r < m_Table.Rows.Count; r++)
            {
                m_Table.Rows[r][col] = (r == row);
            }
        }

        // ユーザー辞書新規追加コマンド
        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Enter Dictionary DB to add";
                dlg.CheckFileExists = false;
                dlg.DefaultExt = ".ddb";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var filename = dlg.FileName;
                    var dict = Dictionary.Create(filename) as Dictionary_DB;
                    if (dict == null)
                    {
                        MessageBox.Show(string.Format("Cannot create database: {0}", filename), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    var svc = DBService.Create(dict.DBParam);
                    if (svc.DatabaseExists())
                    {
                        MessageBox.Show("Database already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    svc.CreateDatabase();
                    svc.CreateDictionaryTables();
                    svc.CreateDictionaryIndices();

                    // Tableに追加
                    DataRow row = m_Table.NewRow();
                    row[0] = Path.GetFileNameWithoutExtension(filename);
                    row[1] = dlg.FileName;
                    row[2] = true;
                    row[3] = false;
                    m_Table.Rows.Add(row);
                    m_Table.AcceptChanges();
                    MakeUserDicRow(m_Table.Rows.Count - 1);
                }

            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error while executing userdic creation:", ex);
                edlg.ShowDialog();
            }
        }

        // Export selected dictionary
        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.dataGridView1.CurrentRow == null)
                {
                    MessageBox.Show("Please select a row.", "Confirmation", MessageBoxButtons.OK);
                    return;
                }
                var row = this.dataGridView1.CurrentRow.Index;
                var dbpath = m_Table.Rows[row][1] as string;
                var dict = Dictionary.Create(dbpath) as Dictionary_DB;  //@todo: DB Dictionaryのみに対応（これでもOK?）
                if (dict == null)
                {
                    MessageBox.Show("Cannot export non-database Dictionary", "Error", MessageBoxButtons.OK);
                    return;
                }

                var dlg = new SaveFileDialog();
                dlg.Title = "Export Dictionary contents to...";
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
                                        svc.ExportDictionary(dict, ref cancelFlag, p => { progress.ProgressCount = p; });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        ErrorReportDialog edlg = new ErrorReportDialog("Error while executing dictionary export:", ex);
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
                ErrorReportDialog edlg = new ErrorReportDialog("Error while executing dictionary export:", ex);
                edlg.ShowDialog();
            }
        }
    }
}