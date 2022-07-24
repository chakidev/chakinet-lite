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
using ChaKi.Properties;
using Svg;
using System.Runtime.InteropServices;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class CorpusPane : UserControl
    {
        private SentenceSearchCondition m_Model;
        private List<Image> m_ImageList;
        private bool m_bSuppressUpdate;
        private ProgressDialog m_Dlg;
        private BackgroundWorker m_Worker;  // Export Corpusワーク
        private ManualResetEvent m_WaitDone;
        private bool m_CancelFlag;
        private int m_LoadingTotal;
        private int m_LoadingDone;
        private Font m_TreeViewBoldFont;

        public bool IsExpanded { get; private set; } = true;
        public int ExpandedWidth { get; private set; } // Expand時 DPI aware width
        public int CollapsedWidth { get; private set; } // Collapsed時 DPI aware width

        public CorpusGroup CorpusGroup => m_Model.CorpusGroup;

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

            this.Load += (e, a) =>
            {
                UpdateView();
            };

            GitRepositories.RepositoryChanged += GitRepositories_RepositoryChanged;

            // DPI微調整
            DpiAdjuster.Adjust(this, (px, py) =>
            {
                this.ExpandedWidth = (int)(250 * px);
                this.CollapsedWidth = (int)(30 * px);
                this.Width = ExpandedWidth;
            });

            this.treeControl1.AfterCheck += TreeControl1_AfterCheck;
            this.AllowDrop = true;
            this.DragEnter += CorpusPane_DragEnter;
            this.DragDrop += CorpusPane_DragDrop;
            // TreeNodeのCheckBoxはCorpusに対応するNodeに限り表示
            this.treeControl1.DrawMode = TreeViewDrawMode.OwnerDrawText;
            this.treeControl1.DrawNode += TreeControl1_DrawNode;
        }

        private void TreeControl1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            // 末端のCorpusを表さない中間ノードのチェックボックスは非表示とする。
            if (this.CorpusGroup.Find(e.Node.Name) == null)
            {
                HideCheckBox(this.treeControl1, e.Node);
            }

            var state = e.State;
            var bounds1 = e.Bounds; // Textの描画領域
            bounds1.X += 3; // Left Margin
            var bounds2 = e.Bounds; // 背景の描画領域
            bounds2.X += 1; // CheckBoxから1pxだけ離す
            var font = e.Node.NodeFont ?? e.Node.TreeView.Font;
            var foreColor = SystemColors.InactiveCaptionText;
            var rectBrush = Brushes.White;
            var isSelected = (state & TreeNodeStates.Selected) == TreeNodeStates.Selected;
            var isFocused = (state & TreeNodeStates.Focused) == TreeNodeStates.Focused;
            var isChecked = e.Node.Checked;
            if (isSelected)
            {
                rectBrush = Brushes.LightSkyBlue;
            }
            if (isChecked)
            {
                foreColor = Color.Red;
                if (m_TreeViewBoldFont == null)
                {
                    m_TreeViewBoldFont = new Font(font, FontStyle.Bold);
                }
                font = m_TreeViewBoldFont;
                bounds1.Width += 20; // Boldにすることで横サイズを拡大しないと収まらない
                bounds2.Width += (20 + 3 - 1); // 上記+Marginを考慮
            }
            e.Graphics.FillRectangle(rectBrush, bounds2);
            TextRenderer.DrawText(e.Graphics, e.Node.Text, font, bounds1, foreColor,
                            TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.SingleLine);
            e.DrawDefault = false;
        }

        private bool supressAfterCheck = false;

        private void TreeControl1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (supressAfterCheck)
            {
                return;
            }
            var c = this.CorpusGroup.Find(e.Node.Name);
            if (c != null)
            {
                c.IsActiveTarget = e.Node.Checked;
            }
        }

        private void RefreshCheckedStates(TreeNode node)
        {
            foreach (TreeNode n in node.Nodes)
            {
                var c = this.CorpusGroup.Find(n.Name);
                if (c != null)
                {
                    supressAfterCheck = true;
                    n.Checked = c.IsActiveTarget;
                    supressAfterCheck = false;
                }
            }
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
            var rootNode = new TreeNode();
            UpdateNodeRecursively(rootNode, this.CorpusGroup);
            treeControl1.Nodes.AddRange(rootNode.Nodes.Cast<TreeNode>().ToArray());
            treeControl1.ExpandAll();
            // 先頭に強制スクロール
            if (treeControl1.Nodes.Count > 0)
            {
                this.treeControl1.SelectedNode = treeControl1.Nodes[0];
                this.treeControl1.SelectedNode.EnsureVisible();
            }
            treeControl1.ResumeLayout();
        }

        private void UpdateNodeRecursively(TreeNode node, CorpusGroup group)
        {
            foreach (var g in group.Groups)
            {
                var childNode = new TreeNode(g.Name) { Name = g.Name };
                node.Nodes.Add(childNode);
                childNode.EnsureVisible();
                UpdateNodeRecursively(childNode, g);
            }
            foreach (var c in group.Corpora)
            {
                var name = c.Name;
                var childNode = new TreeNode(name) { Name = name, Checked = c.IsActiveTarget };
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
                node.Nodes.Add(childNode);
                if (c == ChaKiModel.CurrentCorpus)
                {
                    m_bSuppressUpdate = true;   // AfterSelectイベント誘発による再帰的Updateを抑制
                    this.treeControl1.SelectedNode = node;
                    m_bSuppressUpdate = false;
                }
            }
        }

        public void ModelChangedHandler(object sender, EventArgs e)
        {
            UpdateView();
        }

        /// <summary>
        /// コーパスが選択状態にされたとき、ChaKiModel.CurrentCorpusを更新する.
        /// m_bSuppressUpdateがセットされていれば何も行わない.
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="e"></param>
        private void treeControl1_AfterSelect(object tc, TreeViewEventArgs e)
        {
            if (m_bSuppressUpdate || e.Node == null)
            {
                return;
            }
            var name = e.Node.Name;
            if (name == null)
            {
                return;
            }
            var c = this.CorpusGroup.Find(name);
            if (c == null)
            {
                return;
            }
            ChaKiModel.CurrentCorpus = c;
        }

        private void CorpusPane_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }


        /// <summary>
        /// FolderがDragされたとき、そこに含まれるコーパスファイルでツリーを追加する処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CorpusPane_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
            {
                return;
            }
            var count = CountDBFiles(files);
            m_WaitDone = new ManualResetEvent(false);
            using (m_Dlg = new ProgressDialog())
            {
                m_Dlg.Title = "Loading Corpus From File/Directory...";
                m_Dlg.ProgressMax = count;
                m_Dlg.ProgressReset();
                m_Dlg.WorkerCancelled += OnCancelled;
                using (m_Worker = new BackgroundWorker())
                {
                    m_Worker.WorkerReportsProgress = true;
                    m_Worker.DoWork += (obj, ea) =>
                    {
                        m_LoadingTotal = count;
                        m_LoadingDone = 0;
                        m_CancelFlag = false;
                        LoadCorpusRecursively((string[])ea.Argument, this.CorpusGroup);
                        if (m_Dlg != null) m_Dlg.DialogResult = DialogResult.Cancel;
                        m_WaitDone?.Set();
                    };
                    m_Worker.ProgressChanged += OnWorkerProgressChanged;
                    m_Worker.RunWorkerAsync(files);
                    m_Dlg.ShowDialog();
                    m_WaitDone.WaitOne();
                }
            }
            var c = this.CorpusGroup.AsEnumerable().FirstOrDefault();
            ChaKiModel.CurrentCorpus = c;
            UpdateView();
        }

        private static int CountDBFiles(string[] list)
        {
            var count = 0;
            try
            {
                foreach (var path in list)
                {
                    var attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        count += CountDBFiles(Directory.EnumerateFileSystemEntries(path).ToArray());
                    }
                    else
                    {
                        var ext = Path.GetExtension(path).ToLower();
                        if (ext == ".db" || ext == ".def")
                        {
                            count++;
                        }
                    }
                }
            }
            catch
            {
            }
            return count;
        }

        private void LoadCorpusRecursively(string[] paths, CorpusGroup parentGroup)
        {
            foreach (string path in paths)
            {
                if (m_CancelFlag)
                {
                    break;
                }
                var attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    var childGroup = new CorpusGroup() { Name = Path.GetFileNameWithoutExtension(path) };
                    parentGroup.Groups.Add(childGroup);
                    LoadCorpusRecursively(Directory.EnumerateFileSystemEntries(path).ToArray(), childGroup);
                    if (m_CancelFlag)
                    {
                        break;
                    }
                }
                else
                {
                    var ext = Path.GetExtension(path).ToLower();
                    if (ext == ".db" || ext == ".def")
                    {
                        m_LoadingDone++;
                        m_Worker?.ReportProgress((int)(m_LoadingDone * 100.0 / m_LoadingTotal));
                        try
                        {
                            LoadCorpus(path, parentGroup);
                        }
                        catch (Exception ex)
                        {
                            var err = new ErrorReportDialog(string.Format("Cannot add corpus {0}", path), ex);
                            err.ShowDialog();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Addボタンが押されたとき、新たなコーパスを一覧に追加する処理
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
                    // ロードするファイルの数が10を越えたらProgress dialogを出す
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
                var c = this.CorpusGroup.AsEnumerable().FirstOrDefault();
                ChaKiModel.CurrentCorpus = c;
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
        /// ファイルを指定してコーパスを追加する.
        /// </summary>
        /// <param name="file"></param>
        public void AddCorpus(string file, bool clear)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            if (clear)
            {
                this.CorpusGroup.Clear();
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
            ChaKiModel.CurrentCorpus = this.CorpusGroup.AsEnumerable().FirstOrDefault();
            UpdateView();
        }

        private void LoadCorpus(string path, CorpusGroup parentGroup = null)
        {
            Corpus c = Corpus.CreateFromFile(path);
            // コーパスの基本情報をロードしておく
            DBService dbs = DBService.Create(c.DBParam);

            if (this.CorpusGroup.AsEnumerable().FirstOrDefault(_c => _c.Name == c.Name) != null)
            {
                MessageBox.Show(ChaKi.Properties.Resources.DuplicateCorpusName);
                return;
            }

            // TagSelectorの内容をLoadMandatoryCorpusInfo()のcallbackによって設定する
            TagSelector.ClearAllTags(c.Name);

            dbs.LoadSchemaVersion(c);

            // Schemaチェック
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
            if (parentGroup == null)
            {
                this.CorpusGroup.Add(c);// rootに追加
            }
            else
            {
                parentGroup.Add(c);
            }
        }

        /// <summary>
        /// Delボタンが押されたとき、選択状態のコーパスを一覧から消す処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            var name = treeControl1.SelectedNode?.Name;
            if (name == null)
            {
                return;
            }
            this.CorpusGroup.Remove(name);
            TagSelector.ClearAllTags(name);
            if (this.CorpusGroup.AsEnumerable().FirstOrDefault() == null)
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
                this.CorpusGroup.Clear();
                TagSelector.ClearAllTags();
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("CorpusPane - Invalid Model");
            }
            UpdateView();
        }


        /// <summary>
        /// 「↑」ボタンが押されたとき、選択状態にあるコーパスの優先度を上げる処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
#if false //todo
            var n = treeControl1.SelectedNode;
            if (n == null)
            {
                return;
            }
            int index = n.Index;
            if (index <= 0 || m_Model.CorpusGroup.Count == 0 || index >= m_Model.CorpusGroup.Count)
            {
                return;
            }
            Corpus c = m_Model.CorpusGroup[index];
            m_Model.CorpusGroup.RemoveAt(index);
            m_Model.CorpusGroup.Insert(index - 1, c);
            UpdateView();
#endif
        }

        /// <summary>
        /// 「↓」ボタンが押されたとき、選択状態にあるコーパスの優先度を下げる処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
#if false //todo
            var n = treeControl1.SelectedNode;
            if (n == null)
            {
                return;
            }
            int index = n.Index;
            if (index < 0 || m_Model.CorpusGroup.Count == 0 || index >= m_Model.CorpusGroup.Count - 1)
            {
                return;
            }
            Corpus c = m_Model.CorpusGroup[index];
            m_Model.CorpusGroup.RemoveAt(index);
            m_Model.CorpusGroup.Insert(index + 1, c);
            UpdateView();
#endif
        }

        /// <summary>
        /// Showボタンが押されたとき、選択状態にあるコーパスの詳細を表示する処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            var c = GetSelectedCorpus();
            if (c == null)
            {
                return;
            }

            // Corpus Information ダイアログを表示
            CorpusInfo cdlg = new CorpusInfo();
            MainForm.Instance.AddOwnedForm(cdlg);
            cdlg.Show();
            cdlg.LoadCorpusInfo(c);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            ExportCurrent();
        }

        private static int ExportCorpusLastSelectedFilterIndex = 0;

        /// <summary>
        /// 選択状態にあるコーパスをCabochaEx形式でエクスポート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExportCurrent()
        {
            var c = GetSelectedCorpus();
            if (c == null)
            {
                return;
            }

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
                else //TODO: とりあえずテキストエクスポートは選択できないようにしておく
                {
                    continue;
                }
            }
            dlg.Filter = filterStr.ToString().TrimEnd('|');
            dlg.CheckPathExists = true;
            dlg.Title = string.Format("Export \"{0}\"", c.Name);
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
                m_Worker.RunWorkerAsync(new object[] { dlg.FileName, c, def, proj });
                m_Dlg.ShowDialog();
                m_WaitDone.WaitOne();
                m_Dlg.Dispose();
                m_Worker.Dispose();
            }
        }

        private Corpus GetSelectedCorpus()
        {
            var name = treeControl1.SelectedNode?.Name;
            if (name == null)
            {
                return null;
            }
            return this.CorpusGroup.Find(name);
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

        // Git Repositoryが外部から更新された時のDB Update
        private void GitRepositories_RepositoryChanged(object sender, RepositoryChangedEventArgs e)
        {
            var corpus = this.CorpusGroup.AsEnumerable().FirstOrDefault(c => c.Name == e.RepositoryName);
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

        // パネルを縮小する
        public void Shrink()
        {
            this.Width = this.CollapsedWidth;
            this.panel2.Visible = false;
            this.label2.Visible = false;
            this.button8.Visible = false;
            this.button9.Visible = true;
            this.IsExpanded = false;
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            Shrink();
        }

        // パネルを元に戻す
        public void Expand()
        {
            this.Width = this.ExpandedWidth;
            this.panel2.Visible = true;
            this.label2.Visible = true;
            this.button8.Visible = true;
            this.button9.Visible = false;
            this.IsExpanded = true;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Expand();
        }

        #region PInvoke for TreeView Customization
        private const int TVIF_STATE = 0x8;
        private const int TVIS_STATEIMAGEMASK = 0xF000;
        private const int TV_FIRST = 0x1100;
        private const int TVM_SETITEM = TV_FIRST + 63;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Auto)]
        private struct TVITEM
        {
            public int mask;
            public IntPtr hItem;
            public int state;
            public int stateMask;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszText;
            public int cchTextMax;
            public int iImage;
            public int iSelectedImage;
            public int cChildren;
            public IntPtr lParam;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref TVITEM lParam);

        /// <summary>
        /// Hides the checkbox for the specified node on a TreeView control.
        /// </summary>
        private static void HideCheckBox(TreeView tvw, TreeNode node)
        {
            TVITEM tvi = new TVITEM();
            try
            {
                tvi.hItem = node.Handle;
            }
            catch
            {
                return;
            }
            tvi.mask = TVIF_STATE;
            tvi.stateMask = TVIS_STATEIMAGEMASK;
            tvi.state = 0;
            SendMessage(tvw.Handle, TVM_SETITEM, IntPtr.Zero, ref tvi);
        }
        #endregion
    }
}
