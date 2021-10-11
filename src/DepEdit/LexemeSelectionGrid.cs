using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Lexicons;
using System.Diagnostics;

namespace DependencyEdit
{
    public partial class LexemeSelectionGrid : Form
    {
        private Word m_Word;
        private IList<Lexeme> m_LexList;

        public LexemeSelectionGrid(Corpus cps, Word word)
        {
            m_Word = word;

            Sentence sen = m_Word.Sen;
            string str = "";
            foreach (Word w in sen.Words)
            {
                if (w.Pos >= m_Word.Pos)
                {
                    str += w.Lex.Surface;
                }
            }
            SearchLexiconService svc = new SearchLexiconService();
            m_LexList = svc.Search(cps, str);

            InitializeComponent();

            this.dataGridView1.AutoGenerateColumns = true;
            this.dataGridView1.DataSource = m_LexList;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override bool ProcessCmdKey( ref Message msg, Keys keyData )
        {
            if (keyData == Keys.Enter)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
                return true;
            }
            else if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return false;
        }

        public Lexeme GetCurrentSelection()
        {
            if (this.dataGridView1.SelectedRows.Count == 0)
            {
                return null;
            }
            DataGridViewRow row = this.dataGridView1.SelectedRows[0];
            int rowid = row.Index;
            Debug.Assert(rowid >= 0 && rowid < m_LexList.Count);
            return m_LexList[rowid];
        }
    }
}
