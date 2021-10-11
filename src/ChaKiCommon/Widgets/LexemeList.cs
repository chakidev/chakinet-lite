using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using PopupControl;
using ChaKi.Common.Settings;

namespace ChaKi.Common.Widgets
{
    public partial class LexemeList : UserControl
    {
        public List<string> Tags { get; private set; }

        private List<LexemeCorpusBoolLongTuple> m_Model;
        private Dictionary<int, Popup> m_Popups;
        private PropertyTree m_POSPropTree;
        private PropertyTree m_CFormPropTree;
        private PropertyTree m_CTypePropTree;
        private static LexemeSelectionSettings m_Settings;
        private DataGridViewCell m_CurrentCell;

        static LexemeList()
        {
            m_Settings = DepEditSettings.Current.LexemeListCheckDialogSettings;
        }


        public LexemeList()
        {
            this.Tags = new List<string>();
            this.Tags.Add("Include");
            foreach (KeyValuePair<LP, string> pair in Lexeme.PropertyName)
            {
                this.Tags.Add(pair.Value);
            }
            this.Tags.Add("Frequency");
            m_Popups = new Dictionary<int, Popup>();
            m_POSPropTree = new PropertyTree();
            m_CFormPropTree = new PropertyTree();
            m_CTypePropTree = new PropertyTree();
            m_CurrentCell = null;

            InitializeComponent();

            // 初期値設定を反映
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = false;
            this.dataGridView1.MultiSelect = true;
            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
        }

        public void InitializeGrid()
        {
            // Setup Columns
            for (int i = 0; i < this.Tags.Count; i++)
            {
                var tag = this.Tags[i];
                DataGridViewColumn column = null;
                if (i == 0)
                {
                    column = new DataGridViewCheckBoxColumn() { Name = tag, HeaderText = tag, ReadOnly = false, Width = 60 };
                    column.DefaultCellStyle.BackColor = Color.WhiteSmoke;
                }
                else if (i < (int)LP.Max + 1)
                {
                    var readOnly = (tag == Lexeme.PropertyName[LP.Surface] || tag == Lexeme.PropertyName[LP.BaseLexeme]);
                    column = new DataGridViewTextBoxColumn() { Name = tag, HeaderText = tag, ReadOnly = readOnly, Width = 120 };
                    column.DefaultCellStyle.BackColor = (readOnly) ? Color.LightGray : Color.WhiteSmoke;
                }
                else if (i == (int)LP.Max + 1)
                {
                    column = new DataGridViewTextBoxColumn() { Name = tag, HeaderText = tag, ReadOnly = true, Width = 80 };
                    column.DefaultCellStyle.BackColor = Color.LightGray;
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                this.dataGridView1.Columns.Add(column);
            }
            m_Popups.Add((int)LP.PartOfSpeech + 1, new Popup(m_POSPropTree) { Resizable = true, DropShadowEnabled = true, Size = m_Settings.POSPropTreeSize });
            m_Popups.Add((int)LP.CType + 1, new Popup(m_CTypePropTree) { Resizable = true, DropShadowEnabled = true, Size = m_Settings.CTypePropTreeSize });
            m_Popups.Add((int)LP.CForm + 1, new Popup(m_CFormPropTree) { Resizable = true, DropShadowEnabled = true, Size = m_Settings.CFormPropTreeSize });

            if (m_Settings.ColumnWidths != null)
            {
                for (int i = 0; i < m_Settings.ColumnWidths.Length; i++)
                {
                    Math.Min(100, this.dataGridView1.Columns[i].Width = m_Settings.ColumnWidths[i]);
                }
            }

            // Setup Rows
            this.dataGridView1.RowCount = m_Model.Count;
        }

        public void SaveSettings()
        {
            int cols = this.dataGridView1.Columns.Count;
            m_Settings.ColumnWidths = new int[cols];
            for (int i = 0; i < cols; i++)
            {
                m_Settings.ColumnWidths[i] = this.dataGridView1.Columns[i].Width;
            }
            m_Settings.POSPropTreeSize = m_POSPropTree.Size;
            m_Settings.CFormPropTreeSize = m_CFormPropTree.Size;
            m_Settings.CTypePropTreeSize = m_CTypePropTree.Size;
        }

        // 全ての使用可能なPOS, CType, CFormタグのリストを得て、PropTreeにセットする.
        public void SetLexiconTags(
            IDictionary<string, IList<PartOfSpeech>> pos,
            IDictionary<string, IList<CType>> ctypes,
            IDictionary<string, IList<CForm>> cforms)
        {
            m_POSPropTree.PopulateWithPOSSelections(pos);
            m_CTypePropTree.PopulateWithCTypeSelections(ctypes);
            m_CFormPropTree.PopulateWithCFormSelections(cforms);
        }

        public List<LexemeCorpusBoolLongTuple> Model
        {
            get { return m_Model; }
            set
            {
                this.dataGridView1.Rows.Clear();
                this.dataGridView1.Columns.Clear();

                m_Model = value;
                if (m_Model == null)
                {
                    return;
                }
                InitializeGrid();
            }
        }

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (m_Model == null)
            {
                return;
            }
            var row = e.RowIndex;
            var col = e.ColumnIndex;
            if (row >= m_Model.Count)
            {
                return;
            }
            if (col == 0)
            {
                e.Value = m_Model[row].Item3;
            }
            else if (col < (int)LP.Max + 1)
            {
                e.Value = m_Model[row].Item1.GetStringProperty((LP)(col - 1));
            }
            else if (col == (int)(LP.Max) + 1)
            {
                e.Value = m_Model[row].Item4;
            }

        }

        private void dataGridView1_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (m_Model == null)
            {
                return;
            }
            var row = e.RowIndex;
            var col = e.ColumnIndex;
            if (row >= m_Model.Count)
            {
                return;
            }
            if (col >= 1 && col < (int)(LP.Max)+1)
            {
                var s = (string)e.Value;
                if (s == null)
                {
                    s = string.Empty;
                }
                var lex = m_Model[row].Item1;
                col--;
                switch (col)
                {
                    case (int)LP.Reading:
                        lex.Reading = s;
                        break;
                    case (int)LP.LemmaForm:
                        lex.LemmaForm = s;
                        break;
                    case (int)LP.Pronunciation:
                        lex.Pronunciation = s;
                        break;
                    case (int)LP.Lemma:
                        lex.Lemma = s;
                        break;
                    case (int)LP.PartOfSpeech:
                        if (lex.PartOfSpeech.Name != s)
                        {
                            lex.PartOfSpeech = new PartOfSpeech(s);
                        }
                        break;
                    case (int)LP.CType:
                        if (lex.CType.Name != s)
                        {
                            lex.CType = new CType(s);
                        }
                        break;
                    case (int)LP.CForm:
                        if (lex.CForm.Name != s)
                        {
                            lex.CForm = new CForm(s);
                        }
                        break;
                }
            }
        }

        public void EndEdit()
        {
            this.dataGridView1.EndEdit();
        }

        public void CheckAll()
        {
            for (int i = 0; i < m_Model.Count; i++)
            {
                m_Model[i].Item3 = true;
            }
            this.dataGridView1.Refresh();
        }

        public void ClearAll()
        {
            for (int i = 0; i < m_Model.Count; i++)
            {
                m_Model[i].Item3 = false;
            }
            this.dataGridView1.Refresh();
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Intentionally left blank
        }

        // カラム0のCheckBoxの値変更はこのハンドラで行う. (CellValuePushedでは扱わない)
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex < m_Model.Count)
            {
                this.dataGridView1[e.ColumnIndex, e.RowIndex].Selected = true;
                var b = !(bool)m_Model[e.RowIndex].Item3;
                this.dataGridView1.EndEdit();
                foreach (DataGridViewCell cell in this.dataGridView1.SelectedCells)
                {
                    if (cell.ColumnIndex == 0)
                    {
                        int r = cell.RowIndex;
                        if (r < m_Model.Count)
                        {
                            m_Model[r].Item3 = b;
                        }
                    }
                }
                this.dataGridView1.Refresh();
            }
        }

        // "Copy" Context menu selected
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var data = this.dataGridView1.GetClipboardContent();
            if (data != null)
            {
                Clipboard.SetDataObject(data);
            }
        }

        // "Select from List" Context menu selected
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            var current = this.dataGridView1.CurrentCell;
            if (current == null)
            {
                return;
            }

            Popup popup;
            if (m_Popups.TryGetValue(current.ColumnIndex, out popup))
            {
                ((PropertyTree)(popup.Content)).NodeHit += new EventHandler(LexemeList_NodeHit);
                popup.Show(Cursor.Position.X, Cursor.Position.Y);
            }
        }

        void LexemeList_NodeHit(object sender, EventArgs e)
        {
            PropertyTree tree = sender as PropertyTree;
            if (tree == null)
            {
                return;
            }

            var current = this.dataGridView1.CurrentCell;
            if (current != null)
            {
                current.Value = tree.Selection;
            }

            tree.NodeHit -= LexemeList_NodeHit;
            Popup p = (Popup)(tree.Parent);
            p.Close();
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hti = this.dataGridView1.HitTest(e.X, e.Y);
                this.dataGridView1.CurrentCell = this.dataGridView1[hti.ColumnIndex, hti.RowIndex];
            }
        }
    }
}
