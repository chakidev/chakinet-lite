using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;

namespace ChaKi.Common.Widgets
{
    public partial class PropertyTreeSelectionDialog : Form
    {
        public PropertyTreeSelectionDialog()
        {
            this.Selection = string.Empty;
            InitializeComponent();
        }

        public string Selection { get; private set; }

        public event EventHandler OnSelectionChanged;

        public void PopulateWithPOSSelections(IList<Corpus> cps, Corpus current)
        {
            this.treeView1.PopulateWithPOSSelections(cps, current);
            this.Selection = string.Empty;
        }

        public void PopulateWithCTypeSelections(IList<Corpus> cps, Corpus current)
        {
            this.treeView1.PopulateWithCTypeSelections(cps, current);
            this.Selection = string.Empty;
        }

        public void PopulateWithCFormSelections(IList<Corpus> cps, Corpus current)
        {
             this.treeView1.PopulateWithCFormSelections(cps, current);
             this.Selection = string.Empty;
        }

        /// <summary>
        /// スクロール位置を先頭にセットする
        /// </summary>
        public void ResetScrollState()
        {
            this.treeView1.ResetScrollState();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            OnSelectionDone(e.Node);
        }

        /// <summary>
        /// 選択されたノードに対応する「ハイフン接続文字列」を得て、Selectionメンバに設定し、
        /// 選択完了のイベント通知を行う。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionDone(TreeNode node)
        {
            if (node == null)
            {
                return;
            }
            TreeNode n = node;
            TreeNode[] nodes = new TreeNode[4] { null, null, null, null };
            while (n != null)
            {
                nodes[n.Level] = n;
                n = n.Parent;
            }
            StringBuilder sb = new StringBuilder();
            foreach (TreeNode nd in nodes)
            {
                if (nd == null)
                {
                    break;
                }
                if (sb.Length > 0)
                {
                    sb.Append("-");
                }
                sb.Append(nd.Text);
            }
            if (node.Nodes.Count > 0)
            {
                // Leafでない
                sb.Append("-.*");  // ワイルドカードを付加
            }
            this.Selection = sb.ToString();

            // イベント通知
            if (OnSelectionChanged != null)
            {
                OnSelectionChanged(this, null);
            }
        }

        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                OnSelectionDone(treeView1.SelectedNode);
            }
        }
    }
}
