using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using PopupControl;
using System;
using System.Text;
using System.Collections.Generic;

namespace ChaKi.Common.Widgets
{
    public partial class PropertyTree : UserControl
    {
        private List<TreeView> m_Trees;

        public TreeNode SelectedNode
        {
            get
            {
                int page = this.tabControl1.SelectedIndex;
                if (page >= 0 && page < m_Trees.Count)
                {
                    return m_Trees[page].SelectedNode;
                }
                return null;
            }
        }

        public string Selection
        {
            get
            {
                TreeNode[] nodes = new TreeNode[4] { null, null, null, null };
                TreeNode n = this.SelectedNode;
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
                return sb.ToString();
            }
        }

        public event TreeNodeMouseClickEventHandler NodeMouseClick;
        public event EventHandler NodeHit;  // EnterまたはNodeMouseClickで発生

        public PropertyTree()
        {
            InitializeComponent();

            m_Trees = new List<TreeView>();
            this.ResizeRedraw = true;  // 親PopupをResizableとするのに必要
        }

        private void InitializeTreeView(TreeView vw)
        {
            this.Controls.Add(vw);
            vw.BorderStyle = BorderStyle.None;
            vw.Dock = DockStyle.Fill;
            vw.SelectedNode = null;
            vw.ShowPlusMinus = false;
            //vw.Nodes.AddRange(new System.Windows.Forms.TreeNode[] { new TreeNode { Name = "root", Text = "*" } });
            vw.NodeMouseClick += new TreeNodeMouseClickEventHandler(
                delegate(object o, TreeNodeMouseClickEventArgs e)
                {
                    vw.SelectedNode = e.Node;
                    if (NodeMouseClick != null) NodeMouseClick(o, e);
                    if (NodeHit != null) NodeHit(this, null);
                });
            vw.KeyDown += new KeyEventHandler(
                delegate(object o, KeyEventArgs e)
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (NodeHit != null) NodeHit(this, null);
                    }
                });
            vw.ExpandAll();
        }

        public void PopulateWithPOSSelections(IList<Corpus> cps, Corpus current)
        {
            if (cps == null || cps.Count == 0) return;
            Dictionary<string, IList<PartOfSpeech>> list = new Dictionary<string, IList<PartOfSpeech>>();
            foreach (Corpus c in cps)
            {
                list.Add(c.Name, c.Lex.PartsOfSpeech);
            }
            PopulateWithPOSSelections(list);
            this.tabControl1.SelectedIndex = Math.Max(0, cps.IndexOf(current));
        }

        public void PopulateWithCTypeSelections(IList<Corpus> cps, Corpus current)
        {
            if (cps == null) return;
            Dictionary<string, IList<CType>> list = new Dictionary<string, IList<CType>>();
            foreach (Corpus c in cps)
            {
                list.Add(c.Name, c.Lex.CTypes);
            }
            PopulateWithCTypeSelections(list);
            this.tabControl1.SelectedIndex = Math.Max(0, cps.IndexOf(current));
        }


        public void PopulateWithCFormSelections(IList<Corpus> cps, Corpus current)
        {
            if (cps == null) return;
            Dictionary<string, IList<CForm>> list = new Dictionary<string, IList<CForm>>();
            foreach (Corpus c in cps)
            {
                list.Add(c.Name, c.Lex.CForms);
            }
            PopulateWithCFormSelections(list);
            this.tabControl1.SelectedIndex = Math.Max(0, cps.IndexOf(current));
        }

        public void PopulateWithPOSSelections(IDictionary<string, IList<PartOfSpeech>> list)
        {
            m_Trees.ForEach(v => { this.Controls.Remove(v); });
            m_Trees.Clear();
            this.tabControl1.TabPages.Clear();

            foreach (KeyValuePair<string, IList<PartOfSpeech>> pair in list)
            {
                this.tabControl1.TabPages.Add(pair.Key);
                int page = this.tabControl1.TabPages.Count - 1;
                TreeView tv = new TreeView();

                tv.Nodes.Clear();
                tv.Nodes.Add(".*", ".*");
                foreach (PartOfSpeech pos in pair.Value)
                {
                    string[] tags = new string[4] { pos.Name1, pos.Name2, pos.Name3, pos.Name4 };
                    AddNode(tv, tags);
                }
                InitializeTreeView(tv);
                tv.Parent = this.tabControl1.TabPages[page];
                m_Trees.Add(tv);
            }
        }

        public void PopulateWithCTypeSelections(IDictionary<string, IList<CType>> list)
        {
            m_Trees.ForEach(v => { this.Controls.Remove(v); });
            m_Trees.Clear();
            this.tabControl1.TabPages.Clear();

            foreach (KeyValuePair<string, IList<CType>> pair in list)
            {
                this.tabControl1.TabPages.Add(pair.Key);
                int page = this.tabControl1.TabPages.Count - 1;
                TreeView tv = new TreeView();

                tv.Nodes.Clear();
                tv.Nodes.Add(".*", ".*");
                foreach (CType ctype in pair.Value)
                {
                    string[] tags = new string[2] { ctype.Name1, ctype.Name2 };
                    AddNode(tv, tags);
                }
                InitializeTreeView(tv);
                tv.Parent = this.tabControl1.TabPages[page];
                m_Trees.Add(tv);
            }
        }

        public void PopulateWithCFormSelections(IDictionary<string, IList<CForm>> list)
        {
            m_Trees.ForEach(v => { this.Controls.Remove(v); });
            m_Trees.Clear();
            this.tabControl1.TabPages.Clear();

            foreach (KeyValuePair<string, IList<CForm>> pair in list)
            {
                this.tabControl1.TabPages.Add(pair.Key);
                int page = this.tabControl1.TabPages.Count - 1;
                TreeView tv = new TreeView();

                tv.Nodes.Clear();
                tv.Nodes.Add(".*", ".*");
                foreach (CForm cform in pair.Value)
                {
                    string[] tags = new string[1] { cform.Name };
                    AddNode(tv, tags);
                }
                InitializeTreeView(tv);
                tv.Parent = this.tabControl1.TabPages[page];
                m_Trees.Add(tv);
            }
        }

        /// <summary>
        /// 最大４階層のツリーを作成する
        /// </summary>
        /// <param name="pos"></param>
        private TreeNode AddNode(TreeView vw, string[] tags)
        {
            TreeNode cur = null;
            TreeNodeCollection children = vw.Nodes;
            TreeNode[] found;
            for (int n = 0; n < tags.Length; n++)
            {
                // 第n階層
                if (tags[n] == null || tags[n].Length == 0)
                {
                    break;
                }
                found = children.Find(tags[n], false);
                if (found.Length == 0)  // not found
                {
                    cur = children.Add(tags[n], tags[n]);
                }
                else
                {
                    cur = found[0];
                }
                children = cur.Nodes;
            }
            return cur;
        }

        /// <summary>
        /// スクロール位置を先頭にセットする
        /// </summary>
        public void ResetScrollState()
        {
            foreach (TreeView tree in m_Trees)
            {
                if (tree.Nodes.Count == 0)
                {
                    return;
                }
                TreeNode top = tree.Nodes[0];
                top.EnsureVisible();
            }
        }

        // 親がPopup Controlである場合、そのPopupをResizableとするのに必要
        protected override void WndProc(ref Message m)
        {
            Popup p = Parent as Popup;
            if (p != null && p.ProcessResizing(ref m))
            {
                return;
            }
            base.WndProc(ref m);
        }
    }
}
