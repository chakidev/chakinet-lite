using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChaKi.Common.Properties;
using ChaKi.Common.Widgets;
using ChaKi.Entity.Corpora;
using PopupControl;
using MessageBox = ChaKi.Common.Widgets.MessageBox;
using ChaKi.Common.Settings;
using System.Runtime.InteropServices;

namespace ChaKi.Common
{
    /// <summary>
    /// Segment, Link, Groupに対する属性編集グリッド
    /// 兼 Lexeme属性編集グリッド.
    /// もともと前者用に作ったものを後者と統合したため、若干の無理がある.
    /// </summary>
    public class AttributeGrid : DataGridView
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        private const int WM_SETREDRAW = 0xB;

        public void SuspendDrawing()
        {
            SendMessage(this.Handle, WM_SETREDRAW, 0, 0);
        }

        public void ResumeDrawing() { ResumeDrawing(true); }
        public void ResumeDrawing(bool redraw)
        {
            SendMessage(this.Handle, WM_SETREDRAW, 1, 0);

            if (redraw)
            {
                this.Refresh();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AttributeGridData Model
        {
            get
            {
                return m_Model;
            }
            set
            {
                m_Model = value;
                UpdateData();
            }
        } private AttributeGridData m_Model;

        public bool IsEditing
        {
            get
            {
                return m_IsEditing;
            }
            set
            {
                m_IsEditing = value;
                UpdateData();
                FireUpdateUIState();
            }
        } private bool m_IsEditing;

        public ContextMenuStrip ContextMenu
        {
            get
            {
                return this.contextMenuStrip1;
            }
        }

        public event EventHandler UpdateUIState;
        public Action<Control> AddFromListHandler { get; set; }

        private bool m_SuppressCellValueChangedEvent;
        private Stack<AttributeGridData> m_UndoHistory;
        private Stack<AttributeGridData> m_RedoHistory;

        private ContextMenuStrip contextMenuStrip1;
        private IContainer components;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolStripMenuItem2;
        private ToolStripMenuItem toolStripMenuItem3;
        private ToolStripMenuItem toolStripMenuItem4;
        private ToolStripMenuItem toolStripMenuItem5;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripDropDownButton toolStripMenuItem6;
        private ToolStripDropDownButton toolStripMenuItem7;
        private ToolStripSeparator toolStripSeparator1;

        private Dictionary<string, Popup> m_Popups;
        private PropertyTree m_POSPropTree;
        private PropertyTree m_CFormPropTree;
        private PropertyTree m_CTypePropTree;

        private GridSettings m_Settings;
        public GridSettings Settings
        {
            get { return m_Settings; }
            set
            {
                if (m_Settings != value)
                {
                    m_Settings = value;
                    m_Settings.BindTo(this);
                }
            }
        }

        private bool m_CanAddRemoveRows = true;

        public AttributeGrid()
        {
            this.IsEditing = false;
            this.Model = null;
            m_UndoHistory = new Stack<AttributeGridData>();
            m_RedoHistory = new Stack<AttributeGridData>();
            m_SuppressCellValueChangedEvent = true;

            m_Popups = new Dictionary<string, Popup>();
            m_POSPropTree = new PropertyTree();
            m_CFormPropTree = new PropertyTree();
            m_CTypePropTree = new PropertyTree();

            InitializeComponent();

            this.AllowUserToAddRows = false;
            this.toolStripMenuItem1.Click += new System.EventHandler(HandleInsertRow);
            this.toolStripMenuItem2.Click += new System.EventHandler(HandleRemoveRow);
            this.toolStripMenuItem3.Click += new System.EventHandler(HandleCut);
            this.toolStripMenuItem4.Click += new System.EventHandler(HandleCopy);
            this.toolStripMenuItem5.Click += new System.EventHandler(HandlePaste);
            this.toolStripMenuItem6.Click += new EventHandler(toolStripMenuItem6_Click);
            this.toolStripMenuItem7.Click += new EventHandler(toolStripMenuItem7_Click);
            FireUpdateUIState();
        }

        void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            if (AddFromListHandler != null)
            {
                AddFromListHandler(this.contextMenuStrip1);
            }
        }

        public void SetLexiconTags(
            IDictionary<string, IList<PartOfSpeech>> pos,
            IDictionary<string, IList<CType>> ctypes,
            IDictionary<string, IList<CForm>> cforms)
        {
            var Settings = DepEditSettings.Current.LexemeSelectionSettings;

            m_POSPropTree.PopulateWithPOSSelections(pos);
            m_CTypePropTree.PopulateWithCTypeSelections(ctypes);
            m_CFormPropTree.PopulateWithCFormSelections(cforms);

            m_Popups.Clear();
            m_Popups.Add(Lexeme.PropertyName[LP.PartOfSpeech],
                new Popup(m_POSPropTree) { Resizable = true, DropShadowEnabled = true, Size = Settings.POSPropTreeSize });
            m_Popups.Add(Lexeme.PropertyName[LP.CType],
                new Popup(m_CTypePropTree) { Resizable = true, DropShadowEnabled = true, Size = Settings.CTypePropTreeSize });
            m_Popups.Add(Lexeme.PropertyName[LP.CForm],
                new Popup(m_CFormPropTree) { Resizable = true, DropShadowEnabled = true, Size = Settings.CFormPropTreeSize });
        }

        #region InitializeComponent() - VS Created
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripDropDownButton();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Font = new System.Drawing.Font("Lucida Sans Unicode", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripSeparator1,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripSeparator2,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(164, 171);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(163, 22);
            this.toolStripMenuItem1.Text = "&Insert Row";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(163, 22);
            this.toolStripMenuItem2.Text = "&Remove Row";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(160, 6);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.ShortcutKeyDisplayString = "Ctrl+X";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(163, 22);
            this.toolStripMenuItem3.Text = "Cu&t";
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.ShortcutKeyDisplayString = "Ctrl+C";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(163, 22);
            this.toolStripMenuItem4.Text = "&Copy";
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.ShortcutKeyDisplayString = "Ctrl+V";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(163, 22);
            this.toolStripMenuItem5.Text = "&Paste";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(160, 6);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(99, 20);
            this.toolStripMenuItem6.Text = "Add from List";
            this.toolStripMenuItem6.ToolTipText = "Add from List";
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(110, 20);
            this.toolStripMenuItem7.Text = "Select from List";
            this.toolStripMenuItem7.Visible = false;
            // 
            // AttributeGrid
            // 
            this.ColumnHeadersVisible = false;
            this.ContextMenuStrip = this.contextMenuStrip1;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.Linen;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.DefaultCellStyle = dataGridViewCellStyle1;
            this.RowHeadersVisible = false;
            this.RowTemplate.Height = 21;
            this.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.AttributeGrid_CellClick);
            this.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.AttributeGrid_CellMouseUp);
            this.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.AttributeGrid_CellValueChanged);
            this.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.AttributeGrid_RowPostPaint);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        public void SetWordAttributeMode()
        {
            this.contextMenuStrip1.Items.Remove(this.toolStripMenuItem1);  // Insert Row
            this.contextMenuStrip1.Items.Remove(this.toolStripMenuItem2);  // Remove Row
            this.toolStripMenuItem6.Visible = false;
            this.toolStripMenuItem7.Visible = true;

            m_CanAddRemoveRows = false;
            this.Columns[0].ReadOnly = true;
        }

        void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            var row = this.CurrentRow;
            if (row == null)
            {
                return;
            }
            var ti = this.toolStripMenuItem7;
            Popup popup = null;
            if (!m_Popups.TryGetValue((string)row.Cells[0].Value, out popup))
            {
                return;
            }
            ((PropertyTree)(popup.Content)).NodeHit += new EventHandler(PropertyTree_NodeHit);
            popup.Show(Cursor.Position.X, Cursor.Position.Y);
        }

        void PropertyTree_NodeHit(object sender, EventArgs e)
        {
            PropertyTree tree = sender as PropertyTree;
            if (tree == null)
            {
                return;
            }

            var row = this.CurrentRow;
            row.Cells[1].Value = tree.Selection;
            UpdateData();

            tree.NodeHit -= PropertyTree_NodeHit;
            Popup p = (Popup)(tree.Parent);
            p.Close();
        }

        public void UpdateData()
        {
            if (m_Model == null) return;

            // セルの編集モードから抜ける.
            this.EndEdit();

            SuspendDrawing();

            if (!DesignMode)
            {
                this.Columns.Clear();
                this.Columns.Add("Key", "Key");
                this.Columns.Add("Value", "Value");
            }
            m_SuppressCellValueChangedEvent = true;

            this.ReadOnly = false;
            this.Rows.Clear();
            int row = 0;
            bool currentGroupExpanded = true;

            foreach (AttributeGridRowData rd in m_Model.Rows)
            {
                if (rd is GroupData)
                {
                    GroupData gd = (GroupData)rd;
                    // Group先頭Row
                    this.Rows.Add();
                    this[0, row].Value = gd.Name;
                    this[0, row].Style.Font = new Font(this.DefaultCellStyle.Font, FontStyle.Bold);
                    this[0, row].ReadOnly = this[1, row].ReadOnly = true;
                    currentGroupExpanded = gd.IsExpanded;
                }
                else if (rd is AttributeData)
                {
                    AttributeData attr = (AttributeData)rd;
                    // 各Groupの Attribute Row
                    this.Rows.Add();
                    this[0, row].Value = attr.Key;
                    this[0, row].Style.Padding = new Padding(15, 0, 0, 0);
                    this[1, row].Value = attr.Value;
                    if (attr.RowType == AttributeGridRowType.ReadOnly)
                    {
                        this[0, row].Style.Font = new Font(this.DefaultCellStyle.Font, FontStyle.Italic);
                    }
                    if (attr.RowType == AttributeGridRowType.ReadOnly || !this.IsEditing)
                    {
                        this[0, row].ReadOnly = true;
                        this[1, row].ReadOnly = true;
                        this[0, row].Style.BackColor = this.DefaultCellStyle.BackColor;
                        this[1, row].Style.BackColor = this.DefaultCellStyle.BackColor;
                    }
                    else
                    {
                        if (attr.RowType == AttributeGridRowType.ValueWritable)
                        {
                            this[0, row].ReadOnly = true;
                            this[0, row].Style.BackColor = this.DefaultCellStyle.BackColor;
                        }
                        else
                        {
                            this[0, row].ReadOnly = false;
                            this[0, row].Style.BackColor = Color.White;
                        }
                        this[1, row].ReadOnly = false;
                        this[1, row].Style.BackColor = Color.White;
                    }
                    this.Rows[row].Visible = currentGroupExpanded;
                }
                else
                {
                    continue;
                }
                this.Rows[row].Selected = rd.IsSelected;
                row++;
            }
            m_SuppressCellValueChangedEvent = false;

            // Grid Settingの再適用
            if (this.Settings != null)
            {
                this.m_Settings.BindTo(this);
            }

            ResumeDrawing(true);
        }

        void FireUpdateUIState()
        {
            if (this.UpdateUIState != null)
            {
                UpdateUIState(this, EventArgs.Empty);
            }
        }

        void AttributeGrid_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (m_Model == null) return;
            Graphics g = e.Graphics;
            int row = e.RowIndex;
            if (row >= m_Model.Rows.Count)
            {
                return;
            }
            var group = m_Model.Rows[row] as GroupData;
            if (group == null)
            {
                return;
            }

            Pen pen = new Pen(Brushes.Black);
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Near;
            sf.LineAlignment = StringAlignment.Center;
            sf.Trimming = StringTrimming.EllipsisCharacter;

            string strText = this[0, row].Value.ToString();

            Rectangle textArea = e.RowBounds;
            textArea.X = /*this.RowHeadersWidth + */15;
            textArea.Width -= (/*this.RowHeadersWidth + */15);
            textArea.X -= this.HorizontalScrollingOffset;
            textArea.Width += this.HorizontalScrollingOffset;
            textArea.Height -= 1;

            RectangleF clip = textArea;
            clip.Width = this.Columns[0].Width + this.Columns[1].Width - 1;
            clip.X = /*this.RowHeadersWidth + */2;
            clip.X -= this.HorizontalScrollingOffset;
            clip.Width += this.HorizontalScrollingOffset;

            RectangleF oldClip = e.Graphics.ClipBounds;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.SetClip(clip);
            g.FillRectangle(new SolidBrush(Color.PowderBlue), e.RowBounds);
            if (group.IsExpanded)
            {
                g.DrawImage(Resources.Expanded, new Rectangle(e.RowBounds.X + 2, e.RowBounds.Y + 3, 12, 12));
            }
            else
            {
                g.DrawImage(Resources.Shrinked, new Rectangle(e.RowBounds.X + 2, e.RowBounds.Y + 3, 12, 12));
            }
            g.DrawString(strText, new Font(this.DefaultCellStyle.Font, FontStyle.Bold), Brushes.Black, textArea, sf);
            g.SetClip(oldClip);

        }

        void AttributeGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (m_Model == null) return;

            if (m_Model.ToggleGroupExpansion(e.RowIndex))
            {
                UpdateData();
            }
        }

        // マウス右クリックでコンテクストメニューが表示される直前に行を選択状態にする.
        // その後、コンテクストメニューの状態を更新する
        private void AttributeGrid_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                Rows[e.RowIndex].Selected = true;
                this.CurrentCell = this[e.ColumnIndex, e.RowIndex];
            }
            bool editable = false;

            // ContextMenuの状態更新
            if (m_IsEditing)
            {
                editable = true;
            }
            foreach (ToolStripItem item in this.contextMenuStrip1.Items)
            {
                item.Enabled = editable;
            }
            toolStripMenuItem4.Enabled = this.CanCopy;  // CopyはEditでなくても使える.

            // Sentence Rowが選択されている場合のみリスト選択が有効Item6(Select from List)はDisabled.
            this.toolStripMenuItem6.Enabled = false;
            if (this.CurrentCell != null && this.CurrentCell.RowIndex >= 0)
            {
                var row = this.CurrentCell.RowIndex;
                if (row < m_Model.Rows.Count)
                {
                    var group = m_Model.FindGroupOf(row);
                    if (group != null && group.Name == GroupData.SENTENCE)
                    {
                        this.toolStripMenuItem6.Enabled = true;
                    }
                }
            }
        }

        public void StartEditing()
        {
            m_UndoHistory.Clear();
            m_RedoHistory.Clear();

            this.IsEditing = true;
            FireUpdateUIState();
        }

        public void EndEditing()
        {
            this.IsEditing = false;

            m_UndoHistory.Clear();
            m_RedoHistory.Clear();
            FireUpdateUIState();
        }

        private void RegisterHistory()
        {
            m_UndoHistory.Push(new AttributeGridData(m_Model));
            m_RedoHistory.Clear();
            FireUpdateUIState();
        }

        public void Undo()
        {
            if (m_UndoHistory.Count == 0)
            {
                return;
            }
            var node = m_UndoHistory.Pop();
            m_RedoHistory.Push(this.Model);
            this.Model = node;
            FireUpdateUIState();
        }

        public void RewindUndo()
        {
            if (m_UndoHistory.Count == 0)
            {
                return;
            }
            AttributeGridData node = null;
            while (m_UndoHistory.Count > 0)
            {
                node = m_UndoHistory.Pop();
            }
            this.Model = node;
            FireUpdateUIState();
        }

        public void ClearUndoRedo()
        {
            m_UndoHistory.Clear();
            m_RedoHistory.Clear();
            FireUpdateUIState();
        }

        public void Redo()
        {
            if (m_RedoHistory.Count == 0) return;

            var node = m_RedoHistory.Pop();
            m_UndoHistory.Push(this.Model);
            this.Model = node;
            FireUpdateUIState();
        }

        public bool CanUndo { get { return m_UndoHistory.Count > 0; } }

        public bool CanRedo { get { return m_RedoHistory.Count > 0; } }

        public void InsertItem(string key, string value)
        {
            if (m_Model == null)
            {
                return;
            }
            m_SuppressCellValueChangedEvent = true;
            DataGridViewCell cell = this.CurrentCell;
            if (cell != null)
            {
                try
                {
                    RegisterHistory();
                    m_Model.InsertRow(cell.RowIndex, key, value, AttributeGridRowType.KeyValueWritable);
                    UpdateData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Cannot Insert Row: \n{0}", ex.ToString()));
                }
            }
            m_SuppressCellValueChangedEvent = false;
        }

        public void Copy()
        {
            var data = this.GetClipboardContent();
            if (data != null)
            {
                Clipboard.SetDataObject(data);
            }
        }

        public void Cut()
        {
            if (!m_IsEditing)
            {
                return;
            }
            var data = this.GetClipboardContent();
            if (data != null)
            {
                Clipboard.SetDataObject(data);
                RemoveRow();
            }
        }

        public void Cut2()
        {
            if (!m_IsEditing)
            {
                return;
            }
            var data = this.GetClipboardContent();
            if (data != null)
            {
                Clipboard.SetDataObject(data);
                foreach (DataGridViewRow row in this.SelectedRows)
                {
                    row.Cells[1].Value = string.Empty;  // "Value"のみクリア
                }
            }
        }

        public bool CanCut
        {
            get { return m_IsEditing && GetClipboardContent() != null; }
        }

        public bool CanCopy
        {
            get { return GetClipboardContent() != null; }
        }

        public bool CanPaste
        {
            get { return m_IsEditing && Clipboard.GetText() != null; }
        }

        public void Paste()
        {
            if (!m_IsEditing || m_Model == null || this.CurrentCell == null)
            {
                return;
            }
            m_SuppressCellValueChangedEvent = true;

            int insertRowIndex = this.CurrentCell.RowIndex;

            // Get Clipboard content and split it to lines
            try
            {
                string pasteText = Clipboard.GetText();
                if (string.IsNullOrEmpty(pasteText)) return;
                pasteText = pasteText.Replace("\r\n", "\n");
                pasteText = pasteText.Replace('\r', '\n');
                pasteText = pasteText.TrimEnd(new char[] { '\n' });
                string[] lines = pasteText.Split('\n');

                RegisterHistory();
                m_Model.PasteData(lines, insertRowIndex);
                UpdateData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Cannot Paste Data: \n{0}", ex.ToString()));
            }
            m_SuppressCellValueChangedEvent = false;
        }

        public void Paste2()
        {
            if (!m_IsEditing || this.CurrentCell == null)
            {
                return;
            }
            if (this.CurrentCell.ColumnIndex != 1)
            {
                return;
            }
            try
            {
                string pasteText = Clipboard.GetText();
                this.CurrentCell.Value = pasteText;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Cannot Paste Data: \n{0}", ex.ToString()));
            }
        }

        public void RemoveRow()
        {
            if (m_Model == null)
            {
                return;
            }
            m_SuppressCellValueChangedEvent = true;
            try
            {
                RegisterHistory();
                m_Model.RemoveRows(from DataGridViewRow r in this.SelectedRows orderby r.Index descending select r.Index);
                UpdateData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Cannot Delete Row: \n{0}", ex.ToString()));
            }
            m_SuppressCellValueChangedEvent = false;
        }

        private void HandleRemoveRow(object sender, System.EventArgs e)
        {
            RemoveRow();
        }

        private void HandleCopy(object sender, System.EventArgs e)
        {
            Copy();
        }

        private void HandleCut(object sender, System.EventArgs e)
        {
            if (m_CanAddRemoveRows)
            {
                Cut();
            }
            else
            {
                Cut2();
            }
        }

        private void HandlePaste(object sender, System.EventArgs e)
        {
            if (m_CanAddRemoveRows)
            {
                Paste();
            }
            else
            {
                Paste2();
            }
        }

        private void HandleInsertRow(object sender, System.EventArgs e)
        {
            if (m_Model == null)
            {
                return;
            }
            m_SuppressCellValueChangedEvent = true;
            DataGridViewCell cell = this.CurrentCell;
            if (cell != null)
            {
                try
                {
                    RegisterHistory();
                    m_Model.InsertRow(cell.RowIndex);
                    UpdateData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Cannot Insert Row: \n{0}", ex.ToString()));
                }
            }
            m_SuppressCellValueChangedEvent = false;
        }

        private void AttributeGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!m_SuppressCellValueChangedEvent)
            {
                var col = e.ColumnIndex;
                var row = e.RowIndex;
                if (col < 0 || col >= this.Columns.Count || row < 0 || row >= this.Rows.Count)
                {
                    return;
                }
                try
                {
                    RegisterHistory();
                    m_Model.ChangeValue(e.RowIndex, e.ColumnIndex, (string)(this[col, row].Value));
                    UpdateData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Cannot Change Value: \n{0}", ex.ToString()));
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.C && (keyData & Keys.Modifiers) == Keys.Control)
            {
                if (this.CanCopy)
                {
                    HandleCopy(this, EventArgs.Empty);
                }
                return true;
            }
            if ((keyData & Keys.KeyCode) == Keys.V && (keyData & Keys.Modifiers) == Keys.Control)
            {
                if (this.CanPaste)
                {
                    HandlePaste(this, EventArgs.Empty);
                }
                return true;
            }
            if ((keyData & Keys.KeyCode) == Keys.X && (keyData & Keys.Modifiers) == Keys.Control)
            {
                if (this.CanCut)
                {
                    HandleCut(this, EventArgs.Empty);
                }
                return true;
            }
            if ((keyData & Keys.KeyCode) == Keys.Z && (keyData & Keys.Modifiers) == Keys.Control)
            {
                if (this.CanUndo)
                {
                    Undo();
                }
                return true;
            }
            if ((keyData & Keys.KeyCode) == Keys.Y && (keyData & Keys.Modifiers) == Keys.Control)
            {
                if (this.CanRedo)
                {
                    Redo();
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
