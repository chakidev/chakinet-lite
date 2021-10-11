using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Common.Settings;
using PopupControl;
using ChaKi.Common.Properties;
using System.Runtime.InteropServices;

namespace ChaKi.Common
{
    /// <summary>
    /// Sentence Attribute編集時のタグ選択
    /// 設定(TagSelectorSettings)のみTagSelectorと共有している
    /// </summary>
    public partial class AttributeListSelector : UserControl
    {
        public string TagType { get; set; }
        private List<AttributeBase32> m_Source;
        public AttributeBase32 Selection { get; private set; }
        public event EventHandler TagSelected;
        private View m_ListViewMode;
        private bool m_Dragging;
        private Size m_DragOffset;

        public AttributeListSelector(string tagType)
        {
            InitializeComponent();
            this.ResizeRedraw = true;
            this.TagType = tagType;

            listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                new ColumnHeader() { Name = "key", Text = "Key", Width=100 },
                new ColumnHeader() { Name = "value", Text="Value", Width=100}
            });
            listView1.FullRowSelect = true;
            this.Selection = null;
            m_Source = new List<AttributeBase32>();
            m_Dragging = false;
        }

        public View DisplayMode
        {
            set
            {
                m_ListViewMode = value;
                this.toolStripButton1.Checked = (m_ListViewMode == View.LargeIcon);
                this.toolStripButton2.Checked = (m_ListViewMode == View.List);
                this.toolStripButton3.Checked = (m_ListViewMode == View.Details);
                this.listView1.View = m_ListViewMode;
            }
        }

        public void Reset()
        {
            this.listView1.Items.Clear();
            if (this.listView1.LargeImageList == null)
            {
                this.listView1.LargeImageList = new ImageList()
                {
                    ImageSize = new Size(32, 32),
                    ColorDepth = ColorDepth.Depth32Bit
                };
                this.listView1.SmallImageList = new ImageList()
                {
                    ImageSize = new Size(16, 16),
                    ColorDepth = ColorDepth.Depth32Bit
                };
            }
            else
            {
                this.listView1.LargeImageList.Images.Clear();
            }
            m_Source.Clear();
        }

        public void AddTag(AttributeBase32 t)
        {
            Image img = ChaKi.Common.Properties.Resources.Gear.ToBitmap();
            int i = listView1.LargeImageList.Images.Count;
            listView1.LargeImageList.Images.Add(img);
            listView1.SmallImageList.Images.Add(img);
            ListViewItem item = new ListViewItem(t.Key, i);
            item.SubItems.Add(t.Value);
            listView1.Items.Add(item);
            m_Source.Add(t);
        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            DetermineResult();
        }

        private void listView1_Click(object sender, EventArgs e)
        {
            DetermineResult();
        }

        private void DetermineResult()
        {
            int index = (this.listView1.SelectedIndices.Count > 0) ? this.listView1.SelectedIndices[0] : -1;
            if (m_Source != null && index >= 0 && index < m_Source.Count)
            {
                this.Selection = m_Source[index];
                if (TagSelected != null)
                {
                    TagSelected(this, null);
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
                var setting = AttributeSelectorSettings.Current.GetByType(this.TagType);
                p.Width = setting.Size.Width;
                p.Height = setting.Size.Height;
                this.DisplayMode = (View)setting.Style;
            }
            this.SizeChanged += new EventHandler(TagSelector_SizeChanged);
        }

        private void TagSelector_SizeChanged(object sender, EventArgs e)
        {
            Popup p = Parent as Popup;
            if (p != null)
            {
                var setting = AttributeSelectorSettings.Current.GetByType(this.TagType);
                setting.Size = new Size(p.Width, p.Height);
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            DisplayMode = View.LargeIcon;
            var setting = AttributeSelectorSettings.Current.GetByType(this.TagType);
            setting.Style = (int)this.listView1.View;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            DisplayMode = View.List;
            var setting = AttributeSelectorSettings.Current.GetByType(this.TagType);
            setting.Style = (int)this.listView1.View;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            DisplayMode = View.Details;
            var setting = AttributeSelectorSettings.Current.GetByType(this.TagType);
            setting.Style = (int)this.listView1.View;
        }

        // 親であるPopupの位置をドラッグで動かす.
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hWnd, ref winRECT rect);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
        internal static extern void MoveWindow(IntPtr hwnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        internal struct winRECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        private void AttributeListSelector_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                m_Dragging = true;
                m_DragOffset = new Size(e.Location.X, e.Location.Y);
            }
        }


        private void AttributeListSelector_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_Dragging)
            {
                var p = PointToScreen(e.Location);
                winRECT rect = new winRECT();
                var handle = this.Parent.Handle;
                GetWindowRect(handle, ref rect);
                MoveWindow(handle, p.X - m_DragOffset.Width, p.Y - m_DragOffset.Height, rect.right - rect.left, rect.bottom - rect.top, true);
            }
        }

        private void AttributeListSelector_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                m_Dragging = false;
            }
        }
    }
}
