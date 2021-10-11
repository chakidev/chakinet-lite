using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PopupControl;
using ChaKi.Common.Settings;
using System.Runtime.InteropServices;

namespace ChaKi.Common.Widgets
{
    public partial class DocumentSelector : UserControl
    {
        private bool m_Dragging;
        private Size m_DragOffset;
        private const string m_AddNewLabel = "Add New";

        public event Action<int> DocidSelected;

        public DocumentSelector()
        {
            this.ResizeRedraw = true;
            m_Dragging = false;

            InitializeComponent();
        }

        public void Populate(List<string[]> documentList)
        {
            this.dataGridView1.Rows.Clear();
            foreach (var item in documentList)
            {
                var r = this.dataGridView1.Rows.Add(item[0]);
                int id;
                if (Int32.TryParse(item[0], out id))
                {
                    this.dataGridView1.Rows[r].Tag = id;
                }
            }
            int rr = this.dataGridView1.Rows.Add(m_AddNewLabel);
            this.dataGridView1[0, rr].Style.Font = new Font(this.dataGridView1.DefaultCellStyle.Font, FontStyle.Italic);
        }

        public void SelectDocument(int docIdToSelect)
        {
            var dg = this.dataGridView1;
            dg.ClearSelection();
            for (int i = 0; i < dg.RowCount; i++)
            {
                int id;
                if (Int32.TryParse((dg[0, i].Value as string), out id))
                {
                    if (id == docIdToSelect)
                    {
                        dg.Rows[i].Selected = true;
                        // scroll so that selected row comes center
                        int halfWay = dg.DisplayedRowCount(false) / 2;
                        if (dg.FirstDisplayedScrollingRowIndex + halfWay > i ||
                            (dg.FirstDisplayedScrollingRowIndex + dg.DisplayedRowCount(false) - halfWay) <= i)
                        {
                            int targetRow = i;
                            targetRow = Math.Max(targetRow - halfWay, 0);
                            dg.FirstDisplayedScrollingRowIndex = targetRow;

                        }
                        break;
                    }
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
        private void DocumentSelector_Load(object sender, EventArgs e)
        {
            Popup p = Parent as Popup;
            if (p != null)
            {
                var setting = DocumentSelectorSettings.Current;
                p.Width = setting.Size.Width;
                p.Height = setting.Size.Height;
            }
            this.SizeChanged += new EventHandler(DocumentSelector_SizeChanged);
        }

        private void DocumentSelector_SizeChanged(object sender, EventArgs e)
        {
            Popup p = Parent as Popup;
            if (p != null)
            {
                var setting = DocumentSelectorSettings.Current;
                setting.Size = new Size(p.Width, p.Height);
            }
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
        private void DocumentSelector_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                m_Dragging = true;
                m_DragOffset = new Size(e.Location.X, e.Location.Y);
            }
        }


        private void DocumentSelector_MouseMove(object sender, MouseEventArgs e)
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

        private void DocumentSelector_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                m_Dragging = false;
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (this.dataGridView1.SelectedCells.Count != 1)
            {
                return;
            }
            string cellvalue = this.dataGridView1.SelectedCells[0].Value as string;
            int docid = -1;
            if (cellvalue == m_AddNewLabel)
            {
                var nrow = this.dataGridView1.Rows.Count;
                for (int i = 0; i < nrow; i++)
                {
                    int val;
                    if (Int32.TryParse(this.dataGridView1[0, i].Value as string, out val))
                    {
                        if (val > docid)
                        {
                            docid = val;
                        }
                    }
                }
                if (docid >= 0)
                {
                    docid++; // Max of docid + 1
                }
            }
            else
            {
                docid = Int32.Parse(cellvalue);
            }
            if (DocidSelected != null && docid >= 0)
            {
                DocidSelected(docid);
            }
            // Request closing of the parent Popup window.
            Popup p = this.Parent as Popup;
            if (p != null)
            {
                p.Close(ToolStripDropDownCloseReason.ItemClicked);
            }
        }
    }
}
