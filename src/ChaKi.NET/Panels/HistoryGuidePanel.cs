using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChaKi.Common;
using ChaKi.Entity.Search;
using ChaKi.Properties;
using ChaKi.Common.Widgets;
using MessageBox = ChaKi.Common.Widgets.MessageBox;

namespace ChaKi.Panels
{
    public partial class HistoryGuidePanel : Form
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        public event NavigateHistoryDelegate HistoryNavigating;
        public event EventHandler HistorySaveRequested;
        public event EventHandler DeleteHistoryRequested;

        public SearchHistory Current;

        private SearchHistory m_Model;
        private bool m_SuspendAfterSelect;  // TreeViewのAfterSelectイベント処理実行抑制フラグ
        private TreeNode m_ContextMenuClickedAt;

        private DpiAdjuster m_DpiAdjuster;

        public HistoryGuidePanel(SearchHistory model)
        {
            InitializeComponent();

            this.treeView1.StateImageList = new ImageList();
            this.treeView1.StateImageList.Images.Add(Resources.Unsaved);
            this.treeView1.StateImageList.Images.Add(Resources.Saved);

            m_Model = model;
            m_Model.OnUpdateModel += new EventHandler(this.UpdateModelHandler);
            UpdateView();

            Current = null;
            m_SuspendAfterSelect = false;
            m_ContextMenuClickedAt = null;

            // High-dpi対応
            m_DpiAdjuster = new DpiAdjuster((xscale, yscale) => {
                    this.tableLayoutPanel1.RowStyles[1].Height = (int)((float)this.tableLayoutPanel1.RowStyles[1].Height * yscale);
            });
            this.Paint += (e, a) => m_DpiAdjuster.Adjust(a.Graphics);
        }

        public void UpdateView()
        {
            this.treeView1.Nodes.Clear();

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.treeView1.BeginUpdate();

            PopulateViewRecursively(this.treeView1.Nodes, m_Model);

            this.treeView1.EndUpdate();
            this.Cursor = oldCursor;
        }

        private void PopulateViewRecursively(TreeNodeCollection view, SearchHistory model)
        {
            foreach (SearchHistory node in model.Children)
            {
                TreeNode viewnode = new TreeNode( node.Name );
                viewnode.Tag = (object)node;
                if (node.FilePath != null)
                {
                    viewnode.StateImageIndex = 1;
                    viewnode.ToolTipText = node.FilePath;
                }
                else
                {
                    viewnode.StateImageIndex = 0;
                }
                view.Add(viewnode);
                PopulateViewRecursively( viewnode.Nodes, node );
            }
            treeView1.ExpandAll();
        }

        public SearchHistory GetLastKwicListNode()
        {
            SearchHistory n = this.Current;

            while (n != null)
            {
                if (n.KwicList != null) {
                    return n;
                }
                n = n.Parent;
            }
            return n;
        }

        public void UpdateModelHandler(object src, EventArgs e)
        {
            if (e is SearchHistoryResetEventArgs)
            {
                UpdateView();
            }
            else if (e is SearchHistoryAddNodeEventArgs)
            {
                SearchHistoryAddNodeEventArgs ane = e as SearchHistoryAddNodeEventArgs;

                //TODO: 部分的な更新とする
                UpdateView();

                //TODO:仮：追加されたノードを選択状態にするにする
                this.Current = ane.Hist;
                foreach (TreeNode n in this.treeView1.Nodes)
                {
                    if (n.Text.Equals(ane.Hist.Name))
                    {
                        m_SuspendAfterSelect = true; // Add操作によるAfterSelectイベントは処理しないようにする
                        this.treeView1.SelectedNode = n;
                        m_SuspendAfterSelect = false;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// ツリーノードが選択されたら、
        /// 1. そのヒストリを元に条件パネルを再構成する。
        /// 2. Kwic/WordList/Collocationのビューを切り替える。
        /// 3. ビューの中身をクリアする。
        /// 4. ヒストリがセーブデータへのパスを持っていればViewにロードする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SearchHistory hist = (SearchHistory)e.Node.Tag;
            if (hist == null)
            {
                MessageBox.Show("Missing History Name: " + e.Node.Text);
                return;
            }
            Current = hist;

            if (!m_SuspendAfterSelect && this.HistoryNavigating != null)
            {
                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                this.HistoryNavigating(hist);

                this.Cursor = oldCursor;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            MEMORYSTATUSEX ms = new MEMORYSTATUSEX();
            ms.dwLength = (uint)Marshal.SizeOf(ms);  // 準備
            GlobalMemoryStatusEx(ms);        // メモリ情報を取得

            float memoryUsedPercent = 100.0F - ms.ullAvailPhys * 100.0F / ms.ullTotalPhys;
            using (Graphics g = this.pictureBox1.CreateGraphics())
            {
                Rectangle r = this.pictureBox1.ClientRectangle;
                g.FillRectangle(Brushes.LightBlue, r);
                r.Width = (int)(r.Width * memoryUsedPercent / 100.0);
                g.FillRectangle(Brushes.Orange, r);
                StringFormat sf = StringFormat.GenericDefault;
                sf.Alignment = StringAlignment.Center;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.DrawString(string.Format("{0:F0}%", memoryUsedPercent), this.Font, Brushes.Black,
                    new RectangleF(0F, 0F, (float)this.pictureBox1.Width, (float)this.pictureBox1.Height), sf);
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                m_ContextMenuClickedAt = e.Node;
                this.contextMenuStrip1.Show(PointToScreen(e.Location));
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_ContextMenuClickedAt == null)
            {
                return;
            }
            SearchHistory hist = (SearchHistory)m_ContextMenuClickedAt.Tag;
            if (this.HistorySaveRequested != null && hist != null)
            {
                this.HistorySaveRequested(hist, e);
                if (hist.FilePath != null)
                {
                    m_ContextMenuClickedAt.StateImageIndex = 1; // Change Node state
                    m_ContextMenuClickedAt.ToolTipText = hist.FilePath;
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_ContextMenuClickedAt == null)
            {
                return;
            }
            SearchHistory hist = (SearchHistory)m_ContextMenuClickedAt.Tag;
            if (this.DeleteHistoryRequested != null && hist != null)
            {
                this.DeleteHistoryRequested(hist, e);
            }
        }

        private void treeView1_BeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            try
            {
                if (e.Label.Trim().Length == 0)
                {
                    e.CancelEdit = true;
                    return;
                }
                SearchHistory hist = (SearchHistory)e.Node.Tag;
                if (hist == null)
                {
                    return;
                }
                hist.Name = e.Label;
            }
            catch
            { /* ignore exceptions */ }
        }
    }
}
