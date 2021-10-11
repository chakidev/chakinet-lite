using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ChaKi.Service.Search;
using System.Threading;
using ChaKi.Entity.Search;
using ChaKi.GUICommon;
using ChaKi.Options;
using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Properties;
using System.Resources;

namespace ChaKi.Panels
{
    /// <summary>
    /// コマンドの実行状態を管理するパネル
    /// コマンド待ち行列(Queue)と単一のスレッドを持ち、
    /// キューに入れられたServiceCommandを順次実行する
    /// </summary>
    public partial class CommandPanel : Form
    {
        public bool[] CommandEnableStates
        {
            get { return m_CommandEnableStates; }
            set { m_CommandEnableStates = value; EnableCommandButtons(true); }
        }

        public BeginSearchDelegate BeginSearch;
        public BeginSearchDelegate NarrowSearch;
        public BeginSearchDelegate AppendSearch;
        public BeginSearchDelegate BeginWordList;
        public BeginSearchDelegate BeginCollocation;

        private Queue<IServiceCommand> m_CommandQueue;
        private Thread m_Thread;
        private bool m_bThreadQuit;
        private CommandProgress m_Progress;
        private bool m_bModelUpdate;
        private bool[] m_CommandEnableStates;

        internal CommandPanel(FilterButton fButton)
        {
            m_CommandQueue = new Queue<IServiceCommand>();
            m_Thread = new Thread(new ThreadStart(this.CommandProcess)) { IsBackground = true };

            m_bModelUpdate = false;
            m_Progress = null;
            m_CommandEnableStates = new bool[5] { true, true, false, false, true };

            InitializeComponent();

            // 与えられたFilter ButtonでToolStrip左端のボタンを置き換える。
            this.filterToolStripButton1 = fButton;
            this.toolStrip1.Items.RemoveAt(0);
            this.toolStrip1.Items.Insert(0, fButton);

            // Grid状態の復元
            GridSettings gs = GUISetting.Instance.CommandPanelGridSettings;
            if (gs == null)
            {
                gs = GUISetting.Instance.CommandPanelGridSettings = new GridSettings();
            }
            gs.BindTo(this.dataGridView1);

            // 更新タイマ開始
            this.timer1.Start();

            m_bThreadQuit = false;
            m_Thread.Start();

            this.dataGridView1.PreviewKeyDown += new PreviewKeyDownEventHandler(dataGridView1_PreviewKeyDown);
            this.dataGridView1.MouseClick += new MouseEventHandler(dataGridView1_MouseClick);
        }

        public void SetModel(CommandProgress model)
        {
            m_Progress = model;
            if (m_Progress != null)
            {
                m_Progress.OnModelChanged += new EventHandler(this.ModelChangedHandler);
            }
            RefreshView();
        }

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
            m_bThreadQuit = true;
            base.Dispose(disposing);
        }


        /// <summary>
        /// スレッドを中断する
        /// </summary>
        /// <param name="restart"></param>
        public void Abort(bool restart)
        {
            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                m_Thread.Abort();
            }
            catch
            {
                MessageBox.Show("Error in Thread Aborting.");
            }
            if (m_CommandQueue.Count > 0)
            {
                IServiceCommand cmd = m_CommandQueue.Peek();
                if (cmd.Aborted != null)
                {
                    cmd.Aborted(null, null);
                }
            }
            if (restart)
            {
                // wait for thread termination
                m_Thread.Join(30000);
                for (int ntry = 0; ntry < 100; ntry++)
                {
                    if (m_Thread.ThreadState == ThreadState.Stopped || m_Thread.ThreadState == ThreadState.Aborted)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                m_CommandQueue.Clear();
                // Threadの再起動
                m_Thread = new Thread(new ThreadStart(this.CommandProcess)) { IsBackground = true };
                m_Thread.Start();
                EnableCommandButtons(true);
            }
            this.Cursor = oldCursor;
        }


        private void button3_Click(object sender, EventArgs e)
        {
            Abort(true);
        }

        /// <summary>
        /// コマンドをキューに受け入れる
        /// </summary>
        /// <param name="cmd"></param>
        public void QueueCommand(IServiceCommand cmd)
        {
            m_CommandQueue.Enqueue(cmd);
        }

        /// <summary>
        /// スレッドによるコマンドディスパッチ処理本体
        /// </summary>
        private void CommandProcess()
        {
            while (!m_bThreadQuit)
            {
                if (m_CommandQueue.Count > 0)
                {
                    IServiceCommand cmd = m_CommandQueue.Dequeue();
                    EnableCommandButtons(false);
                    try
                    {
                        cmd.Begin();
                        if (cmd.Completed != null)
                        {
                            cmd.Completed(null, null);
                        }
                    }
                    catch (QueryBuilderException e)
                    {
                        this.Invoke(new Action(() =>
                        {
                            var msg = Resources.ResourceManager.GetString(e.Message);
                            msg = msg?? e.Message;
                            MessageBox.Show(msg, "Error");
                        }));
                    }
                    catch (Exception e)
                    {
                        var t = e.GetType();
                        Console.WriteLine(t.FullName);
                        if (!(e is ThreadAbortException) && e.GetType().FullName != "NHibernate.Exceptions.GenericADOException")
                        {
                            this.Invoke(new Action(() =>
                            {
                                ErrorReportDialog dlg = new ErrorReportDialog("Error while executing query commands:", e);
                                dlg.ShowDialog();
                            }));
                        }
                    }
                    EnableCommandButtons(true);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        public bool CanBeginSearch
        {
            get
            {
                return this.searchToolStripButton.Enabled;
            }
        }

        public bool CanNarrowSearch
        {
            get
            {
                return this.narrowToolStripMenuItem.Enabled;
            }
        }

        public bool CanBeginWordList
        {
            get
            {
                return this.wordListToolStripButton.Enabled;
            }
        }

        public bool CanBeginCollocation
        {
            get
            {
                return this.collocationToolStripButton.Enabled;
            }
        }

        private delegate void ButtonEnableDelegate(int index, bool enabled);

        private void EnableCommandButtons(bool enabled)
        {
            ButtonEnableDelegate dele = (int i, bool f) => { this.toolStrip1.Items[i].Enabled = f; };
            if (this.toolStrip1.InvokeRequired)
            {
                this.toolStrip1.Invoke(dele, new object[] { 1, enabled ? m_CommandEnableStates[1] : false });
                this.toolStrip1.Invoke(dele, new object[] { 2, enabled ? m_CommandEnableStates[2] : false });
                this.toolStrip1.Invoke(dele, new object[] { 3, enabled ? m_CommandEnableStates[3] : false });
                // Splitterが挟まるのでindexをひとつ飛ばす
                this.toolStrip1.Invoke(dele, new object[] { 5, enabled ? m_CommandEnableStates[4] : true });
            }
            else
            {
                dele(1, enabled ? m_CommandEnableStates[1] : false);
                dele(2, enabled ? m_CommandEnableStates[2] : false);
                dele(3, enabled ? m_CommandEnableStates[3] : false);
                // Splitterが挟まるのでindexをひとつ飛ばす
                dele(5, enabled ? m_CommandEnableStates[4] : true);
            }
        }

        public CommandProgress Model
        {
            get { return m_Progress; }
        }

        public void ModelChangedHandler(object sender, EventArgs e)
        {
            m_bModelUpdate = true;
        }

        /// <summary>
        /// グリッド内容の非同期再描画処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (m_bModelUpdate)
            {
                if (m_Progress.Items.Count != this.dataGridView1.RowCount)
                {
                    // 行が追加されたので、全体を描画しなおし
                    RefreshView();
                }
                else
                {
                    CommandProgressItem item = m_Progress.Current;
                    int row;
                    if (item != null)
                    {
                        row = item.Row;
                        this.dataGridView1.Rows[row].Cells[3].Value =
                            (!item.NhitIsUnknown) ? string.Format("{0}", item.Nhit) : "--";
                        this.dataGridView1.Rows[row].Cells[4].Value =
                            (!item.NhitIsUnknown && item.Nc > 0) ? string.Format("{0:F4}", item.NhitP) : "--";
                        this.dataGridView1.Rows[row].Cells[5].Value =
                            (!item.NhitIsUnknown) ? string.Format("{0}", item.Nret) : "--";
                        this.dataGridView1.Rows[row].Cells[6].Value =
                            (!item.NhitIsUnknown && item.Nhit > 0) ? string.Format("{0:F1}", item.NretP) : "--";
                    }
                    item = m_Progress.Total;
                    row = item.Row;
                    this.dataGridView1.Rows[row].Cells[3].Value =
                        (!item.NhitIsUnknown) ? string.Format("{0}", item.Nhit) : "--";
                    this.dataGridView1.Rows[row].Cells[4].Value =
                        (!item.NhitIsUnknown && item.Nc > 0) ? string.Format("{0:F4}", item.NhitP) : "--";
                    this.dataGridView1.Rows[row].Cells[5].Value =
                        (!item.NhitIsUnknown) ? string.Format("{0}", item.Nret) : "--";
                    this.dataGridView1.Rows[row].Cells[6].Value =
                        (!item.NhitIsUnknown && item.Nhit > 0) ? string.Format("{0:F1}", item.NretP) : "--";
                }
                m_bModelUpdate = false;
            }
        }

        private void RefreshView()
        {
            this.dataGridView1.SuspendLayout();
            this.dataGridView1.Rows.Clear();
            foreach (CommandProgressItem item in m_Progress.Items)
            {
                object[] rowdata = new object[] {
                            item.Title,
                            string.Format( "{0}", item.Nc ),
                            string.Format( "{0}", item.Nd ),
                            (!item.NhitIsUnknown) ? string.Format( "{0}", item.Nhit ) : "--",
                            (!item.NhitIsUnknown && item.Nc > 0) ? string.Format( "{0:F4}", item.NhitP ) : "--",
                            (!item.NhitIsUnknown) ? string.Format( "{0}", item.Nret ) : "--",
                            (!item.NhitIsUnknown && item.Nhit > 0)? string.Format( "{0:F1}", item.NretP ) : "--"
                };
                if (item.Title.Equals("TOTAL"))
                {
                    rowdata[2] = string.Empty;
                }
                this.dataGridView1.Rows.Add(rowdata);
            }
            this.dataGridView1.ResumeLayout();
        }

        /// <summary>
        /// Searchボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void searchStripSplitButton_ButtonClick(object sender, EventArgs e)
        {
            if (BeginSearch != null)
            {
                BeginSearch();
            }
        }

        /// <summary>
        /// Search-Newボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (BeginSearch != null)
            {
                BeginSearch();
            }
        }

        /// <summary>
        /// Search-Narrowボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void narrowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (NarrowSearch != null)
            {
                NarrowSearch();
            }
        }

        /// <summary>
        /// Search-Appendボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void appendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AppendSearch != null)
            {
                AppendSearch();
            }
        }

        /// <summary>
        /// WordCountボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wordListToolStripButton_Click(object sender, EventArgs e)
        {
            if (BeginWordList != null)
            {
                BeginWordList();
            }
        }

        /// <summary>
        /// Collocationボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void collocationToolStripButton_Click(object sender, EventArgs e)
        {
            if (BeginCollocation != null)
            {
                BeginCollocation();
            }
        }

        /// <summary>
        /// Abortボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void abortToolStripButton_Click(object sender, EventArgs e)
        {
            Abort(true);
        }

        // 特殊キー処理
        void dataGridView1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
            {
                CopyToClipboard();
            }
        }

        // Copy (& Paste) 関係の処理
        private void dataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenuStrip1.Show(this.PointToScreen(e.Location));
            }
        }
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyToClipboard();
        }

        public void CutToClipboard()
        {
            // intentionally left blank
        }

        public void CopyToClipboard()
        {
            var data = this.dataGridView1.GetClipboardContent();
            if (data != null)
            { 
            Clipboard.SetDataObject(data);
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
                return (this.dataGridView1.GetClipboardContent() != null);
            }
        }

        public bool CanPaste { get { return false; } }    // always false

    }
}