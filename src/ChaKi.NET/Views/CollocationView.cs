using System;
using System.Windows.Forms;
using ChaKi.GUICommon;
using ChaKi.Entity.Collocation;
using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using System.Drawing;
using ChaKi.Common.Settings;
using ChaKi.Common;

namespace ChaKi.Views
{
    public partial class CollocationView : UserControl, IChaKiView
    {
        private CollocationList m_Model;

        public GridWithTotal Grid { get { return this.dataGridView1; } }

        // Row Header右クリック→Show Occurenceコマンド実行依頼(to MainForm)
        // FSMモード時のみ有効.
        public event EventHandler<SentenceIdsOccurrenceEventArgs> OccurrenceRequested;

        public CollocationView()
        {
            InitializeComponent();

            this.dataGridView1.GridColor = Color.DarkGray;
            this.dataGridView1.RowTemplate.Height = 18;

            ApplyGridFont();
            FontDictionary.Current.FontChanged += HandleFontChanged;
        }

        public void SetModel(object model)
        {
            if (model == null)
            {
                return;
            }
            if (!(model is CollocationList))
            {
                throw new ArgumentException("Assigning invalid model to CollocationView");
            }
            m_Model = (CollocationList)model;
            if (m_Model != null)
            {
                //                m_Model.OnLexemeCountAdded += new AddLexemeCountEventHandler(this.AddLexemeCountHandler);
            }
            this.dataGridView1.ColumnWidthChanged -= dataGridView1_ColumnWidthChanged;
            this.dataGridView1.Columns.Clear();
            this.dataGridView1.ColumnCount = Lexeme.PropertyName.Count + m_Model.NColumns;
            int col = 0;
            for (int i = 0; i < Lexeme.PropertyName.Count; i++)
            {
                this.dataGridView1.Columns[col].Name = Lexeme.PropertyName[(LP)i];

                this.dataGridView1.Columns[col].Width = 70; // Default
                this.dataGridView1.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                this.dataGridView1.Columns[col].DefaultCellStyle.BackColor = Color.Ivory;
                if (m_Model.FirstColTitle != null)
                {
                    this.dataGridView1.Columns[col].Visible = (i == 0);
                    this.dataGridView1.Columns[col].Name = m_Model.FirstColTitle;
                }
                else
                {
                    this.dataGridView1.Columns[col].Visible = !(m_Model.Cond.Filter.IsFiltered((LP)i));
                }
                this.dataGridView1.Columns[col].Tag = "?0";  // for R-Export
                col++;
            }
            for (int i = 0; i < m_Model.NColumns; i++)
            {
                this.dataGridView1.Columns[col].Name = m_Model.ColumnDefs[i].Title;
                this.dataGridView1.Columns[col].Width = 50; // Default
                this.dataGridView1.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                this.dataGridView1.Columns[col].DefaultCellStyle.BackColor = Color.LightGray;
                this.dataGridView1.Columns[col].Tag = string.Format("#{0}", i);  // for R-Export
                col++;
            }
            UpdateView();
            GUISetting.Instance.CollocationViewSettings.ApplyToGrid(this.dataGridView1);
            this.dataGridView1.ColumnWidthChanged += dataGridView1_ColumnWidthChanged;
        }

        public void SetVisible(bool f)
        {
            this.Visible = f;
        }

        /// <summary>
        /// Modelの内容からGridを生成しなおす。
        /// </summary>
        private void UpdateView()
        {
            this.dataGridView1.ResetRows();

            // Totalを初期化
            List<int> total = new List<int>();
            for (int i = 0; i < m_Model.NColumns; i++)
            {
                total.Add(0);
            }

            foreach (KeyValuePair<Lexeme, List<DIValue>> pair in m_Model.Rows)
            {
                int row = this.dataGridView1.Rows.Add();
                int col = 0;
                for (int i = 0; i < Lexeme.PropertyName.Count; i++)
                {
                    this.dataGridView1[col, row].Value = pair.Key.GetStringProperty((LP)i);
                    col++;
                }
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    switch (m_Model.ColumnDefs[i].Type)
                    {
                        case ColumnType.CT_DOUBLE:
                            this.dataGridView1[col, row].Value = pair.Value[i].dval;
                            break;
                        case ColumnType.CT_STRING:
                            this.dataGridView1[col, row].Value = pair.Value[i].sval;
                            break;
                        case ColumnType.CT_INT:
                            this.dataGridView1[col, row].Value = pair.Value[i].ival;
                            // int値かつHasTotalの場合、TOTALを加算
                            if (m_Model.ColumnDefs[i].HasTotal)
                            {
                                total[i] += pair.Value[i].ival;
                            }
                            break;
                    }
                    col++;
                }
            }
            // TOTAL行をセット
            for (int i = 0; i < m_Model.NColumns; i++)
            {
                if (m_Model.ColumnDefs[i].HasTotal)
                {
                    this.dataGridView1[Lexeme.PropertyName.Count + i, 0].Value = total[i];
                }
            }
        }

        /// <summary>
        /// ChaKiOptionのフォントが変更されたときのEvent Handler。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleFontChanged(object sender, EventArgs e)
        {
            ApplyGridFont();
        }

        void ApplyGridFont()
        {
            this.dataGridView1.Font = GUISetting.Instance.GetBaseAnsiFont();
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

        public void ListOccurrences(string ids)
        {
            string[] idstrs = ids.Split(',');
            List<int> idlist = new List<int>();
            foreach (string idstr in idstrs)
            {
                int val;
                if (int.TryParse(idstr, out val))
                {
                    idlist.Add(val);
                }
            }
            SentenceIdsOccurrenceEventArgs ea = new SentenceIdsOccurrenceEventArgs(idlist);
            if (OccurrenceRequested != null)
            {
                OccurrenceRequested(this, ea);
            }
        }

        public bool CanCut { get { return false; } }    // always false

        public bool CanCopy
        {
            get
            {
                return true;
            }
        }

        public bool CanPaste { get { return false; } }    // always false

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

        private void dataGridView1_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            GUISetting.Instance.CollocationViewSettings.FromGrid(this.dataGridView1);
        }

        private void dataGridView1_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenuStrip2.Show(this.PointToScreen(this.dataGridView1.GetCellDisplayRectangle(0, e.RowIndex, true).Location));
                this.dataGridView1.CurrentCell = this.dataGridView1[0, e.RowIndex];
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CopyToClipboard();
        }

        private void listOccurrencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int idcol = -1;
            for (int i = 0; i < this.dataGridView1.Columns.Count; i++)
            {
                if (this.dataGridView1.Columns[i].HeaderText == "IDs")
                {
                    idcol = i;
                    break;
                }
            }
            int r = this.dataGridView1.CurrentRow.Index;
            if (idcol >= 0 && r >= 0 && r < this.dataGridView1.Rows.Count)
            {
                string val = this.dataGridView1[idcol, r].Value as string;
                ListOccurrences(val);
            }
        }
    }

    public class SentenceIdsOccurrenceEventArgs : EventArgs
    {
        public SentenceIdsOccurrenceEventArgs(List<int> idlist)
        {
            this.SentenceIDList = idlist;
        }

        public List<int> SentenceIDList { get; set; }
    }
}
