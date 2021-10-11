using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ChaKi.Common.Settings;
using ChaKi.Entity.Corpora.Annotations;
using PopupControl;
using ChaKi.Common.Properties;
using ChaKi.Common.Widgets;
using System.Text.RegularExpressions;
using ChaKi.Entity.Settings;

namespace ChaKi.Common
{
    /// <summary>
    /// DependencyEdit PanelでAnnotation Tagを選択するためのポップアップウィンドウとして
    /// 使用されるユーザコントロール.
    /// Segment, Link, Groupの3種のTagについてあらかじめ準備されたインスタンスを提供するので、
    /// newする必要はない.
    /// </summary>
    public partial class TagSelector : UserControl
    {
        public string TagType { get; set; }
        public Tag Selection { get; private set; }
        private Dictionary<string, List<Tag>> m_Source;  // corpus name --> Tag list hash
        public event EventHandler TagSelected;
        private View m_ListViewMode;
        private Dictionary<string, string> m_ShortCuts;  // keyは"CorpusName:Shortcut"の形式

        public static Dictionary<string, TagSelector> PreparedSelectors = new Dictionary<string, TagSelector>();  // tag type -> TagSelector
        public static Dictionary<string, Popup> PreparedPopups = new Dictionary<string, Popup>();  // tag type -> TagSelector

        static TagSelector()
        {
            string type;
            TagSelector ts;
            type = ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT;
            ts = new TagSelector() { TagType = type };
            PreparedSelectors.Add(type, ts);
            PreparedPopups.Add(type, new Popup(ts) { Resizable = true });
            type = ChaKi.Entity.Corpora.Annotations.Tag.LINK;
            ts = new TagSelector() { TagType = type };
            PreparedSelectors.Add(type, ts);
            PreparedPopups.Add(type, new Popup(ts) { Resizable = true });
            type = ChaKi.Entity.Corpora.Annotations.Tag.GROUP;
            ts = new TagSelector() { TagType = type };
            PreparedSelectors.Add(type, ts);
            PreparedPopups.Add(type, new Popup(ts) { Resizable = true });
        }

        public static void ClearAllTags(string corpusname = null)
        {
            PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT].RemoveTab(corpusname);
            PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.LINK].RemoveTab(corpusname);
            PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.GROUP].RemoveTab(corpusname);
        }

        public TagSelector()
        {
            InitializeComponent();
            this.ResizeRedraw = true;

            this.Selection = null;
            m_Source = new Dictionary<string, List<Tag>>();
            m_ShortCuts = new Dictionary<string, string>();
        }

        public void RemoveTab(string corpusname = null)
        {
            if (corpusname == null)
            {
                // 全てのTabを削除
                this.tabControl1.TabPages.Clear();
                m_Source.Clear();
                return;
            }

            // corpusnameで指定されたTabを削除
            if (m_Source.ContainsKey(corpusname))
            {
                m_Source.Remove(corpusname);
            }
            int index = this.tabControl1.TabPages.IndexOfKey(corpusname);
            if (index >= 0)
            {
                this.tabControl1.TabPages.RemoveAt(index);
            }
        }

        public Dictionary<string, List<Tag>> Source
        {
            get { return m_Source; }
        }

        public List<Tag> GetTagsForCorpus(string corpusname)
        {
            if (this.Source == null)
            {
                return null;
            }
            if (this.Source.ContainsKey(corpusname))
            {
                return this.Source[corpusname];
            }
            return null;
        }

        public View DisplayMode
        {
            set
            {
                m_ListViewMode = value;
                this.toolStripButton1.Checked = (m_ListViewMode == View.LargeIcon);
                this.toolStripButton2.Checked = (m_ListViewMode == View.List);
                this.toolStripButton3.Checked = (m_ListViewMode == View.Details);
                foreach (TabPage tabpage in this.tabControl1.TabPages)
                {
                    var listview = (tabpage.Controls.Count) > 0 ? (tabpage.Controls[0] as ListView) : null;
                    if (listview != null)
                    {
                        listview.View = m_ListViewMode;
                    }
                }
            }
            get
            {
                return m_ListViewMode;
            }
        }

        private static Image m_ToolIconImage = null;

        public void AddTag(Tag t, string corpusname)
        {
            if (corpusname == null)
            {
                var tab = this.tabControl1.SelectedTab;
                if (tab == null)
                {
                    return;
                }
                corpusname = tab.Name;
            }
            if (corpusname == null)
            {
                return;
            }
            if (m_ToolIconImage == null)
            {
                m_ToolIconImage = Resources.Gear.ToBitmap();
            }
            var listview = GetListView(corpusname);
            int i = listview.LargeImageList.Images.Count;
            listview.LargeImageList.Images.Add(m_ToolIconImage);
            listview.SmallImageList.Images.Add(m_ToolIconImage);
            var tagname = t.Name;
            // Shortcutを持っていれば、テーブルへ追加
            TagSettingItem setting = null;
            if (t.Type == Entity.Corpora.Annotations.Tag.SEGMENT)
            {
                TagSetting.Instance.Segment.TryGetValue(t.Name, out setting);
            }
            else if (t.Type == Entity.Corpora.Annotations.Tag.LINK)
            {
                TagSetting.Instance.Link.TryGetValue(t.Name, out setting);
            }
            else if (t.Type == Entity.Corpora.Annotations.Tag.GROUP)
            {
                TagSetting.Instance.Group.TryGetValue(t.Name, out setting);
            }
            if (setting != null && setting.ShortcutKey != '\0')
            {
                tagname += $" ({setting.ShortcutKey})";
                var key = $"{corpusname}:{setting.ShortcutKey}";
                if (!m_ShortCuts.ContainsKey(key))
                {
                    m_ShortCuts.Add(key, t.Name);
                }
            }
            ListViewItem item = new ListViewItem(tagname, i);
            item.SubItems.Add(t.Description);
            listview.Items.Add(item);
            List<Tag> list;
            if (!m_Source.TryGetValue(corpusname, out list))
            {
                list = new List<Tag>();
                m_Source.Add(corpusname, list);
            }
            list.Add(t);
        }

        private ListView GetListView(string corpusname)
        {
            var index = this.tabControl1.TabPages.IndexOfKey(corpusname);
            if (index < 0)
            {
                index = AddTab(corpusname);
            }
            if (index < 0)
            {
                return null;
            }
            var tabpage = this.tabControl1.TabPages[index];
            return (tabpage.Controls.Count > 0) ? (tabpage.Controls[0] as ListView) : null;
        }

        private ListView GetCurrentListView(out string corpusname)
        {
            var tabpage = this.tabControl1.SelectedTab;
            if (tabpage == null)
            {
                corpusname = null;
                return null;
            }
            corpusname = tabpage.Name;
            return (tabpage.Controls.Count > 0) ? (tabpage.Controls[0] as ListView) : null;
        }

        private int AddTab(string corpusname)
        {
            var listview = new ListView();
            listview.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                new ColumnHeader() { Name = "name", Text = "Name", Width=100 },
                new ColumnHeader() { Name = "description", Text="Description", Width=200}
            });
            listview.View = m_ListViewMode;
            listview.FullRowSelect = true;
            listview.Sorting = SortOrder.Ascending;
            listview.Dock = DockStyle.Fill;
            listview.ItemActivate += listView_ItemActivate;
            listview.Click += listView_Click;

            listview.LargeImageList = new ImageList()
            {
                ImageSize = new Size(32, 32),
                ColorDepth = ColorDepth.Depth32Bit
            };
            listview.SmallImageList = new ImageList()
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };

            var tabpage = new TabPage() { Name = corpusname, Text = corpusname };
            tabpage.Controls.Add(listview);
            this.tabControl1.TabPages.Add(tabpage);
            return this.tabControl1.TabPages.IndexOf(tabpage);
        }

        private void listView_ItemActivate(object sender, EventArgs e)
        {
            DetermineResult();
        }

        private void listView_Click(object sender, EventArgs e)
        {
            DetermineResult();
        }

        private void DetermineResult()
        {
            string currentcorpusname;
            var listview = GetCurrentListView(out currentcorpusname);
            if (listview == null)
            {
                return;
            }
            var sel = (listview.SelectedItems.Count > 0) ? listview.SelectedItems[0].Text : null;
            if (m_Source != null)
            {
                var tags = m_Source[currentcorpusname];
                var tag = tags.FirstOrDefault(t => t.Name == sel);
                if (tag != null)
                {
                    this.Selection = tag;
                    if (TagSelected != null)
                    {
                        TagSelected(this, null);
                    }
                }
            }
            Popup p = this.Parent as Popup;
            if (p != null)
            {
                p.Close(ToolStripDropDownCloseReason.ItemClicked);
            }
        }

        // PopupでResizableを有効にした場合は必ず必要
        protected override void WndProc(ref Message m)
        {
            Popup p = Parent as Popup;
            if (p != null && p.ProcessResizing(ref m))
            {
                return;
            }
            base.WndProc(ref m);
        }

        // 表示時に親(Popup)のサイズを保存値に設定.
        private void TagSelector_Load(object sender, EventArgs e)
        {
            Popup p = Parent as Popup;
            if (p != null)
            {
                TagSelectorSettingItem setting = TagSelectorSettings.Current.GetByType(this.TagType);
                p.Width = setting.Size.Width;
                p.Height = setting.Size.Height;
                this.DisplayMode = (View)setting.Style;
                this.panel1.Visible = false;
            }
            RecalcLayoutManually();
            this.SizeChanged += new EventHandler(TagSelector_SizeChanged);
        }

        private void TagSelector_SizeChanged(object sender, EventArgs e)
        {
            Popup p = Parent as Popup;
            if (p != null)
            {
                TagSelectorSettingItem setting = TagSelectorSettings.Current.GetByType(this.TagType);
                setting.Size = new Size(p.Width, p.Height);
            }
            RecalcLayoutManually();
        }

        // High DPI対応
        // Popup（実体はToolStripDropDown）がアンカーの計算においてDPIを正しく反映しないので、手動でレイアウトを再計算する.
        private void RecalcLayoutManually()
        {
            var tab = this.tabControl1;
            tab.Height = this.Height - this.toolStrip1.Height;
            tab.Width = this.Width;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            DisplayMode = View.LargeIcon;
            TagSelectorSettingItem setting = TagSelectorSettings.Current.GetByType(this.TagType);
            setting.Style = (int)this.DisplayMode;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            DisplayMode = View.List;
            TagSelectorSettingItem setting = TagSelectorSettings.Current.GetByType(this.TagType);
            setting.Style = (int)this.DisplayMode;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            DisplayMode = View.Details;
            TagSelectorSettingItem setting = TagSelectorSettings.Current.GetByType(this.TagType);
            setting.Style = (int)this.DisplayMode;
        }

        // Add New Tag
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            this.panel1.Visible = true;
            this.textBox1.Text = string.Empty;
            this.textBox1.KeyDown += textBox1_KeyDown;
            this.textBox1.KeyPress += textBox1_KeyPress;
            this.textBox1.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && this.panel1.Visible)
            {
                DismissAddNewTag();
                return true;
            }
            // Shortcut Keyがテーブルにあれば、即時確定しPopupを終了する
            if (keyData > Keys.A && keyData <= Keys.Z)
            {
                string corpusname;
                var listview = GetCurrentListView(out corpusname);
                var ch = Char.ToLower((Char)keyData);
                string tagname;
                var key = $"{corpusname}:{ch}";
                if (m_ShortCuts.TryGetValue(key, out tagname))
                {
                    if (listview != null)
                    {
                        var tags = m_Source[corpusname];
                        var tag = tags.FirstOrDefault(t => t.Name == tagname);
                        if (tag != null)
                        {
                            this.Selection = tag;
                            if (TagSelected != null)
                            {
                                TagSelected(this, null);
                            }
                        }
                    }
                    Popup p = this.Parent as Popup;
                    if (p != null)
                    {
                        p.Close(ToolStripDropDownCloseReason.ItemClicked);
                    }
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void DismissAddNewTag()
        {
            this.panel1.Visible = false;
            this.textBox1.KeyDown -= textBox1_KeyDown;
            this.textBox1.KeyPress -= textBox1_KeyPress;
        }

        void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            var keycode = e.KeyChar.ToString();
            if (!Regex.IsMatch(keycode, @"[0-9a-zA-Z_:\x08]"))
            {
                // reject the key
                e.Handled = true;
            }
        }

        void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && this.textBox1.Text.Length > 0)
            {
                var tag = new Tag(this.TagType, this.textBox1.Text);
                AddTag(tag, null);
                DismissAddNewTag();
                // 追加されたTagを選択状態にする
                string currentcorpusname;
                var listview = GetCurrentListView(out currentcorpusname);
                if (listview != null)
                {
                    listview.SelectedItems.Clear();
                    listview.Sort();
                    var item = listview.Items.Cast<ListViewItem>().FirstOrDefault(t => t.Text == tag.Name);
                    if (item != null)
                    {
                        item.Selected = true;
                    }
                    listview.Select();
                }
            }
        }

        // Cancel
        private void button1_Click(object sender, EventArgs e)
        {
            DismissAddNewTag();
        }
    }
}
