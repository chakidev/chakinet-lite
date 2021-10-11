using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Common;
using ChaKi.Common.Settings;

namespace ChaKi.ToolDialogs
{
    public partial class EditLexemeFilter : Form
    {
        private LexemeListFilter m_Source;
        private int m_ColNoForAll;
        private int m_RowNoForAll;
        private bool m_Initializing;

        private List<string> m_DisplayNames;
        private Dictionary<int, LP> m_ToLPMapping;

        public EditLexemeFilter(LexemeListFilter srcFilter)
        {
            m_Source = srcFilter;

            InitializeComponent();

            m_Initializing = true;

            // Initialize TagName Mapping
            m_DisplayNames = new List<string>();
            m_ToLPMapping = new Dictionary<int, LP>();
            int index = 0;
            foreach(PropertyBoxItemSetting setting in PropertyBoxSettings.Instance.Settings)
            {
                if (!setting.IsVisible) continue;
                var lp = Lexeme.FindProperty(setting.TagName);
                if (!lp.HasValue)
                {
                    continue;
                }
                m_DisplayNames.Add(setting.DisplayName);
                m_ToLPMapping.Add(index++, lp.Value);
            }

            this.dataGridView1.Rows.Clear();
            this.dataGridView1.Columns.Clear();
            // Columns
            for (int i = 0; i < srcFilter.Count; i++)
            {
                int c = dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn());
                this.dataGridView1.Columns[c].HeaderCell.Value = string.Format("Lex_{0}", i);
                this.dataGridView1.Columns[c].Width = 60;
            }
            m_ColNoForAll = dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn());
            this.dataGridView1.Columns[m_ColNoForAll].HeaderCell.Value = "ALL";
            this.dataGridView1.Columns[m_ColNoForAll].Width = 60;
            this.dataGridView1.Columns[m_ColNoForAll].DefaultCellStyle.BackColor = Color.LightGray;
            // Rows
            this.dataGridView1.RowHeadersWidth = 120;
            foreach (string name in m_DisplayNames)
            {
                int r = this.dataGridView1.Rows.Add();
                this.dataGridView1.Rows[r].HeaderCell.Value = name;
            }
            m_RowNoForAll = this.dataGridView1.Rows.Add();
            this.dataGridView1.Rows[m_RowNoForAll].HeaderCell.Value = "ALL";
            this.dataGridView1.Rows[m_RowNoForAll].DefaultCellStyle.BackColor = Color.LightGray;

            // Load values
            for (int r = 0; r < m_DisplayNames.Count; r++) {
                LP lp;
                if (!m_ToLPMapping.TryGetValue(r, out lp))
                {
                    continue;
                }
                for (int c = 0; c < srcFilter.Count; c++)
                {
                    this.dataGridView1[c, r].Value = !srcFilter[c].IsFiltered(lp);
                }
                this.dataGridView1[m_ColNoForAll, r].Value = false;
            }
            for (int c = 0; c < srcFilter.Count; c++)
            {
                this.dataGridView1[c, m_RowNoForAll].Value = false;
            }
            m_Initializing = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int r = 0; r < m_DisplayNames.Count; r++)
            {
                LP lp;
                if (!m_ToLPMapping.TryGetValue(r, out lp))
                {
                    continue;
                }
                for (int c = 0; c < m_Source.Count; c++)
                {
                    if ((bool)this.dataGridView1[c, r].Value)
                    {
                        m_Source.ResetFiltered(c, lp);
                    }
                    else
                    {
                        m_Source.SetFiltered(c, lp);
                    }
                }
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int r = e.RowIndex;
            int c = e.ColumnIndex;
            if (m_Initializing || r < 0 || c < 0
                || r >= this.dataGridView1.Rows.Count || c >= this.dataGridView1.Columns.Count)
            {
                return;
            }
            bool b = (bool)this.dataGridView1[c, r].Value;

            if (r == m_RowNoForAll)
            {
                for (int i = 0; i < m_RowNoForAll; i++)
                {
                    this.dataGridView1[c, i].Value = b;
                }
            }
            if (c == m_ColNoForAll)
            {
                for (int i = 0; i < m_ColNoForAll; i++)
                {
                    this.dataGridView1[i, r].Value = b;
                }
            }
        }

        // Checkbox状態を即時反映してCellValueChanged Eventを起こさせるにはこれが必要
        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
    }
}
