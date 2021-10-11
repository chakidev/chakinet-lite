using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PopupControl;

namespace ChaKi.Common.Widgets
{
    public partial class ListSelector : UserControl
    {
        public string Selection { get; private set; }
        public event EventHandler TagSelected;

        public ListSelector()
        {
            InitializeComponent();

            this.ResizeRedraw = true;

            this.Selection = null;
        }

        public void Reset()
        {
            this.tabControl1.TabPages.Clear();
        }

        public void AddTab(string name)
        {
            this.tabControl1.TabPages.Add(name, name);
            var listView = new ListView();
            listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                new ColumnHeader() { Name = "name", Text = "Name" },
            });
            listView.FullRowSelect = true;
            listView.View = View.SmallIcon;
            listView.Dock = DockStyle.Fill;
            listView.Click += listView_Click;
            listView.ItemActivate += listView_ItemActivate;
            var tp = this.tabControl1.TabPages[name];
            tp.Controls.Add(listView);
        }

        public void AddTag(string tabname, string t)
        {
            var tabpage = this.tabControl1.TabPages[tabname];
            if (tabpage == null) {
                return;
            }
            var listView = tabpage.Controls[0] as ListView;
            if (listView == null)
            {
                return;
            }

            var item = new ListViewItem(t, 0);
            listView.Items.Add(item);
        }

        private void listView_ItemActivate(object sender, EventArgs e)
        {
            DetermineResult(sender as ListView);
        }

        private void listView_Click(object sender, EventArgs e)
        {
            DetermineResult(sender as ListView);
        }

        private void DetermineResult(ListView lv)
        {
            if (lv != null && lv.SelectedItems.Count > 0)
            {
                this.Selection = lv.SelectedItems[0].Text;
                if (TagSelected != null)
                {
                    TagSelected(this, null);
                }
                Popup p = this.Parent as Popup;
                if (p != null)
                {
                    p.Close(ToolStripDropDownCloseReason.ItemClicked);
                }
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
        //private void TagSelector_Load(object sender, EventArgs e)
        //{
        //    Popup p = Parent as Popup;
        //    if (p != null)
        //    {
        //        TagSelectorSettingItem setting = TagSelectorSettings.Current.GetByType(this.TagType);
        //        p.Width = setting.Size.Width;
        //        p.Height = setting.Size.Height;
        //        this.DisplayMode = (View)setting.Style;
        //    }
        //    this.SizeChanged += new EventHandler(TagSelector_SizeChanged);
        //}

        //private void TagSelector_SizeChanged(object sender, EventArgs e)
        //{
        //    Popup p = Parent as Popup;
        //    if (p != null)
        //    {
        //        TagSelectorSettingItem setting = TagSelectorSettings.Current.GetByType(this.TagType);
        //        setting.Size = new Size(p.Width, p.Height);
        //    }
        //}
    }
}
