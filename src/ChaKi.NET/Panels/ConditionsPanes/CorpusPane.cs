using ChaKi.Common;
using ChaKi.Common.Widgets;
using ChaKi.Entity;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Readers;
using ChaKi.Entity.Search;
using ChaKi.GUICommon;
using ChaKi.Service.Database;
using ChaKi.Service.Export;
using ChaKi.Service.Git;
using ChaKi.Service.Import;
using ChaKi.Service.Readers;
using ChaKi.ToolDialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class CorpusPane : UserControl
    {
        private SentenceSearchCondition m_Model;
        private List<Image> m_ImageList;
        private bool m_bSuppressUpdate;
        private ProgressDialog m_Dlg;
        private BackgroundWorker m_Worker;  // Export Corpus���[�N
        private ManualResetEvent m_WaitDone;
        private bool m_CancelFlag;

        public List<Corpus> CorpusList
        {
            get
            {
                return m_Model.Corpora;
            }
        }

        public CorpusPane(SentenceSearchCondition model)
        {
            InitializeComponent();

            m_ImageList = new List<Image>();
            using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChaKi.Resources.SQLite_S.png"))
            {
                m_ImageList.Add(new Bitmap(st));
            }
            using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChaKi.Resources.MySQL_S.png"))
            {
                m_ImageList.Add(new Bitmap(st));
            }
            using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChaKi.Resources.MSSQL_S.png"))
            {
                m_ImageList.Add(new Bitmap(st));
            }

            m_Model = model;
            m_bSuppressUpdate = false;
            m_Model.OnModelChanged += new EventHandler(this.ModelChangedHandler);

            UpdateView();

            GitRepositories.RepositoryChanged += GitRepositories_RepositoryChanged;
        }

#if false
        public CorpusPane(IContainer container)
        {
            container.Add(this);
            InitializeComponent();

            m_Model = model;
            m_Model.OnModelChanged += this.ModelChangedHandler;

            UpdateView();
        }
#endif
        public void SetCondition(SentenceSearchCondition cond)
        {
            m_Model = cond;
            m_Model.OnModelChanged += new EventHandler(this.ModelChangedHandler);
            UpdateView();
        }

        public SentenceSearchCondition GetCondition()
        {
            return m_Model;
        }

        private void UpdateView()
        {
            treeControl1.SuspendLayout();
            treeControl1.Nodes.Clear();
            foreach (Corpus c in m_Model.Corpora)
            {
                string name = c.Name;
                TreeNode node = new TreeNode(name);
                if (c.DBParam.DBType.Equals("SQLite"))
                {
                    node.ImageIndex = 0;
                }
                else if (c.DBParam.DBType.Equals("MySQL"))
                {
                    node.ImageIndex = 1;
                }
                else if (c.DBParam.DBType.Equals("SQLServer"))
                {
                    node.ImageIndex = 2;
                }
                treeControl1.Nodes.Add(node);
                if (c == ChaKiModel.CurrentCorpus)
                {
                    m_bSuppressUpdate = true;   // AfterSelect�C�x���g�U���ɂ��ċA�IUpdate��}��
                    this.treeControl1.SelectedNode = node;
                    m_bSuppressUpdate = false;
                }
            }
            treeControl1.ResumeLayout();
        }

        public void ModelChangedHandler(object sender, EventArgs e)
        {
            UpdateView();
        }

        /// <summary>
        /// �R�[�p�X���I����Ԃɂ��ꂽ�Ƃ��AChaKiModel.CurrentCorpus���X�V����.
        /// m_bSuppressUpdate���Z�b�g����Ă���Ή����s��Ȃ�.
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="e"></param>
        private void treeControl1_AfterSelect(object tc, TreeViewEventArgs e)
        {
            if (m_bSuppressUpdate || e.Node == null)
            {
                return;
            }
            int index = e.Node.Index;
            if (index < 0 || index >= m_Model.Corpora.Count)
            {
                return;
            }
            ChaKiModel.CurrentCorpus = m_Model.Corpora[index];
        }

        /// <summary>
        /// Add�{�^���������ꂽ�Ƃ��A�V���ȃR�[�p�X���ꗗ�ɒǉ����鏈��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Database files (*.db;*.def;*.dblist)|*.db;*.def;*.dblist|SQLite database files (*.db)|*.db|Corpus definition files (*.def)|*.def|DB List (*.dblist)|*.dbs|All files (*.*)|*.*";
                dlg.Title = "Select Corpus to Open";
                dlg.CheckFileExists = true;
                dlg.Multiselect = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    // ���[�h����t�@�C���̐���10���z������Progress dialog���o��
                    if (dlg.FileNames.Length > 10)
                    {
                        m_WaitDone = new ManualResetEvent(false);
                        using (m_Dlg = new ProgressDialog())
                        {
                            m_Dlg.Title = $"Loading Corpus List (Count={dlg.FileNames.Length})...";
                            m_Dlg.ProgressMax = 100;
                            m_Dlg.ProgressReset();
                            m_Dlg.WorkerCancelled += new EventHandler(OnCancelled);
                            using (m_Worker = new BackgroundWorker())
                            {
                                m_Worker.WorkerReportsProgress = true;
                                m_Worker.DoWork += (obj, ea) => { LoadCorpusList((string[])ea.Argument); };
                                m_Worker.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                                m_Worker.RunWorkerAsync(dlg.FileNames);
                                m_Dlg.ShowDialog();
                                m_WaitDone.WaitOne();
                            }
                        }
                    }
                    else
                    {
                        var oldCursor = this.Cursor;
                        this.Cursor = Cursors.WaitCursor;
                        m_Worker = null;
                        LoadCorpusList(dlg.FileNames);
                        this.Cursor = oldCursor;
                    }
                }
                if (m_Model.Corpora.Count > 0)
                {
                    ChaKiModel.CurrentCorpus = m_Model.Corpora[0];
                }
                else
                {
                    ChaKiModel.CurrentCorpus = null;
                }
            }
            UpdateView();
        }

        private void LoadCorpusList(string[] paths)
        {
            m_CancelFlag = false;
            int i = 0;
            foreach (string path in paths)
            {
                if (m_CancelFlag)
                {
                    break;
                }
                if (m_Worker != null)
                {
                    m_Worker.ReportProgress((int)(i * 100.0 / paths.Length));
                }
                i++;
                try
                {
                    if (Path.GetExtension(path).ToUpper() == ".DBLIST")
                    {
                        using (var rdr = new StreamReader(path))
                        {
                            string s;
                            while ((s = rdr.ReadLine()) != null)
                            {
                                LoadCorpus(s);
                            }
                        }
                    }
                    else
                    {
                        LoadCorpus(path);
                    }
                }
                catch (Exception ex)
                {
                    var err = new ErrorReportDialog(string.Format("Cannot add corpus {0}", path), ex);
                    err.ShowDialog();
                }
            }
            if (m_Dlg != null)
            {
                m_Dlg.DialogResult = DialogResult.Cancel;
            }
            if (m_WaitDone != null)
            {
                m_WaitDone.Set();
            }
        }

        /// <summary>
        /// �t�@�C�����w�肵�ăR�[�p�X��ǉ�����.
        /// </summary>
        /// <param name="file"></param>
        public void AddCorpus(string file, bool clear)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            if (clear)
            {
                m_Model.Corpora.Clear();
            }
            try
            {
                LoadCorpus(file);
            }
            catch (Exception ex)
            {
                ErrorReportDialog err = new ErrorReportDialog(string.Format("Cannot add corpus {0}", file), ex);
                err.ShowDialog();
            }
            this.Cursor = oldCursor;
            if (m_Model.Corpora.Count > 0)
            {
                ChaKiModel.CurrentCorpus = m_Model.Corpora[0];
            }
            else
            {
                ChaKiModel.CurrentCorpus = null;
            }
            UpdateView();
        }

        private void LoadCorpus(string path)
        {
            Corpus c = Corpus.CreateFromFile(path);
            // �R�[�p�X�̊�{�������[�h���Ă���
            DBService dbs = DBService.Create(c.DBParam);

            if (m_Model.Corpora.FirstOrDefault(_c => _c.Name == c.Name) != null)
            {
                MessageBox.Show(ChaKi.Properties.Resources.DuplicateCorpusName);
                return;
            }

            // TagSelector�̓��e��LoadMandatoryCorpusInfo()��callback�ɂ���Đݒ肷��
            TagSelector.ClearAllTags(c.Name);

            dbs.LoadSchemaVersion(c);

            // Schema�`�F�b�N
            if (c.Schema.Version < CorpusSchema.CurrentVersion)
            {
                DBSchemaConversion dlg = new DBSchemaConversion(dbs, c);
                dlg.DoConversion();
            }

            dbs.LoadMandatoryCorpusInfo(c, (s, t) =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => { TagSelector.PreparedSelectors[s].AddTag(t, c.Name); }));
                }
                else
                {
                    TagSelector.PreparedSelectors[s].AddTag(t, c.Name);
                }
            });
            m_Model.Corpora.Add(c);
        }

        /// <summary>
        /// Del�{�^���������ꂽ�Ƃ��A�I����Ԃ̃R�[�p�X���ꗗ�����������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            var n = treeControl1.SelectedNode;
            if (n == null)
            {
                return;
            }
            int index = n.Index;
            if (index < 0 || index >= m_Model.Corpora.Count)
            {
                return;
            }
            try
            {
                var name = m_Model.Corpora[index].Name;
                m_Model.Corpora.RemoveAt(index);
                TagSelector.ClearAllTags(name);
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("CorpusPane - Invalid Model");
            }
            if (m_Model.Corpora.Count == 0)
            {
                ChaKiModel.CurrentCorpus = null;
            }
            UpdateView();
        }

        /// <summary>
        /// "Del All"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                m_Model.Corpora.Clear();
                TagSelector.ClearAllTags();
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("CorpusPane - Invalid Model");
            }
            UpdateView();
        }


        /// <summary>
        /// �u���v�{�^���������ꂽ�Ƃ��A�I����Ԃɂ���R�[�p�X�̗D��x���グ�鏈��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            var n = treeControl1.SelectedNode;
            if (n == null)
            {
                return;
            }
            int index = n.Index;
            if (index <= 0 || m_Model.Corpora.Count == 0 || index >= m_Model.Corpora.Count)
            {
                return;
            }
            Corpus c = m_Model.Corpora[index];
            m_Model.Corpora.RemoveAt(index);
            m_Model.Corpora.Insert(index - 1, c);
            UpdateView();
        }

        /// <summary>
        /// �u���v�{�^���������ꂽ�Ƃ��A�I����Ԃɂ���R�[�p�X�̗D��x�������鏈��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            var n = treeControl1.SelectedNode;
            if (n == null)
            {
                return;
            }
            int index = n.Index;
            if (index < 0 || m_Model.Corpora.Count == 0 || index >= m_Model.Corpora.Count - 1)
            {
                return;
            }
            Corpus c = m_Model.Corpora[index];
            m_Model.Corpora.RemoveAt(index);
            m_Model.Corpora.Insert(index + 1, c);
            UpdateView();
        }

        /// <summary>
        /// Show�{�^���������ꂽ�Ƃ��A�I����Ԃɂ���R�[�p�X�̏ڍׂ�\�����鏈��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            var n = treeControl1.SelectedNode;
            if (n == null)
            {
                return;
            }
            int index = n.Index;
            if (index < 0 || index >= m_Model.Corpora.Count)
            {
                return;
            }

            // Corpus Information �_�C�A���O��\��
            CorpusInfo cdlg = new CorpusInfo();
            MainForm.Instance.AddOwnedForm(cdlg);
            cdlg.Show();
            cdlg.LoadCorpusInfo(m_Model.Corpora[index]);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            ExportCurrent();
        }

        private static int ExportCorpusLastSelectedFilterIndex = 0;

        /// <summary>
        /// �I����Ԃɂ���R�[�p�X��CabochaEx�`���ŃG�N�X�|�[�g
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExportCurrent()
        {
            var n = treeControl1.SelectedNode;
            if (n == null)
            {
                return;
            }
            int index = n.Index;
            if (index < 0 || index >= m_Model.Corpora.Count)
            {
                return;
            }
            Corpus crps = m_Model.Corpora[index];

            CorpusSourceReaderFactory factory = CorpusSourceReaderFactory.Instance;
            FileDialog dlg = new SaveFileDialog();
            StringBuilder filterStr = new StringBuilder();
            foreach (ReaderDef def in factory.ReaderDefs.ReaderDef)
            {
                if (def.Name.Contains("Cabocha"))
                {
                    filterStr.AppendFormat("{0}|*.cabocha|", def.Name.Replace('|', '-'));
                }
                else if (def.Name == "CONLL")
                {
                    filterStr.AppendFormat("{0}|*.conll|", def.Name);
                }
                else if (def.Name == "CONLLU")
                {
                    filterStr.AppendFormat("{0}|*.conllu|", def.Name);
                }
                else //TODO: �Ƃ肠�����e�L�X�g�G�N�X�|�[�g�͑I���ł��Ȃ��悤�ɂ��Ă���
                {
                    continue;
                }
            }
            dlg.Filter = filterStr.ToString().TrimEnd('|');
            dlg.CheckPathExists = true;
            dlg.Title = string.Format("Export \"{0}\"", crps.Name);
            if (ExportCorpusLastSelectedFilterIndex > 0)
            {
                dlg.FilterIndex = ExportCorpusLastSelectedFilterIndex;
            }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ExportCorpusLastSelectedFilterIndex = dlg.FilterIndex;
                if (dlg.FilterIndex <= 0 || dlg.FilterIndex > factory.ReaderDefs.ReaderDef.Length)
                {
                    MessageBox.Show("Invalid ReaderDef index");
                    return;
                }
                ReaderDef def = factory.ReaderDefs.ReaderDef[dlg.FilterIndex - 1];
                var proj = Program.MainForm.GetCurrentProjectId();
                m_CancelFlag = false;
                m_WaitDone = new ManualResetEvent(false);
                m_Dlg = new ProgressDialog();
                m_Dlg.Title = "Querying...";
                m_Dlg.ProgressMax = 100;
                m_Dlg.ProgressReset();
                m_Dlg.WorkerCancelled += new EventHandler(OnCancelled);
                m_Worker = new BackgroundWorker();
                m_Worker.WorkerReportsProgress = true;
                m_Worker.DoWork += new DoWorkEventHandler(OnExportCorpus);
                m_Worker.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                m_Worker.RunWorkerAsync(new object[] { dlg.FileName, crps, def, proj });
                m_Dlg.ShowDialog();
                m_WaitDone.WaitOne();
                m_Dlg.Dispose();
                m_Worker.Dispose();
            }
        }

        void OnExportCorpus(object sender, DoWorkEventArgs e)
        {
            object[] args = (object[])e.Argument;
            string filename = (string)args[0];
            Corpus crps = (Corpus)args[1];
            ReaderDef def = (ReaderDef)args[2];
            var proj = (int)args[3];
            try
            {
                using (TextWriter wr = new StreamWriter(filename, false))
                {
                    IExportService svc;
                    if (def.Name == "CONLL")
                    {
                        svc = new ExportServiceConll(wr);
                    }
                    else if (def.Name == "CONLLU")
                    {
                        svc = new ExportServiceConllU(wr);
                    }
                    else
                    {
                        svc = new ExportServiceCabocha(wr, def);
                    }
                    svc.ExportCorpus(crps, proj, ref m_CancelFlag, new Action<int, object>((v, o) => { m_Worker.ReportProgress(v, o); }));
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog err = new ErrorReportDialog(string.Format("Cannot export corpus {0}", crps.Name), ex);
                err.ShowDialog();
            }
            finally
            {
                m_Dlg.DialogResult = DialogResult.Cancel;
                m_WaitDone.Set();
            }
        }

        void OnCancelled(object sender, EventArgs e)
        {
            m_CancelFlag = true;
        }

        void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            m_Dlg.ProgressCount = e.ProgressPercentage;
            var detail = e.UserState as int[];
            if (detail != null && detail.Length == 2)
            {
                m_Dlg.ProgressDetail = detail;
            }
        }

        // Repair selected Corpus DB
        private void button8_Click(object sender, EventArgs e)
        {
            var dbparam = ChaKiModel.CurrentCorpus?.DBParam;
            if (dbparam == null)
            {
                return;
            }
            var dbs = DBService.Create(dbparam);
            var dlg = new RepairDialog() { Service = dbs };
            dlg.ShowDialog();
        }

        private bool m_RepositoryUpdateLock = false;

        // Git Repository���O������X�V���ꂽ����DB Update
        private void GitRepositories_RepositoryChanged(object sender, RepositoryChangedEventArgs e)
        {
            var corpus = this.CorpusList.FirstOrDefault(c => c.Name == e.RepositoryName);
            if (corpus != null)
            {
                BeginInvoke(new Action<Corpus>(c =>
                {
                    if (m_RepositoryUpdateLock)
                    {
                        return;
                    }
                    m_RepositoryUpdateLock = true;
                    try
                    {
                        if (MessageBox.Show(this, $"Annotation of '{c.Name}' has been modified externally. Update database?",
                            "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            var cursor = Cursor.Current;
                            try
                            {
                                Cursor.Current = Cursors.WaitCursor;
                                // Import annotations from file
                                var svc = new ImportService();
                                var dlg = new ProgressDialogSimple() { Title = "Importing Annotations..." };
                                Task task = null;
                                dlg.Load += (_e, _a) =>
                                {
                                    task = svc.ImportAnnotationAsync(corpus, e.FileName, MainForm.Instance.CurrentProject, dlg);
                                };
                                dlg.ShowDialog();
                                task?.Wait();
                            }
                            catch (Exception ex)
                            {
                                ErrorReportDialog err = new ErrorReportDialog($"Import operation failed: Corpus={corpus.Name}", ex);
                                err.ShowDialog();
                            }
                            finally
                            {
                                Cursor.Current = cursor;
                            }
                        }
                    }
                    finally
                    {
                        m_RepositoryUpdateLock = false;
                    }
                }), corpus);
            }
        }

        private void UpdateAnnotation(Corpus corpus, string repositoryName)
        {

        }

    }
}
