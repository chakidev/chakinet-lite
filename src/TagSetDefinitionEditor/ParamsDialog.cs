using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.GUICommon;
using ChaKi.Service.Database;
using ChaKi.Entity.Corpora;

namespace ChaKi.TagSetDefinitionEditor
{
    public partial class ParamsDialog : Form
    {
        public ParamsDialog()
        {
            InitializeComponent();
        }

        public string DbFile
        {
            get
            {
                return this.textBox1.Text;
            }
        }
        private string m_DbFile;

        public string TagSetName
        {
            get
            {
                return this.comboBox1.Text;
            }
        }

        // "..." buttonが押された時の処理
        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            m_DbFile = dlg.FileName;
            if (m_DbFile != this.textBox1.Text)
            {
                this.textBox1.Text = m_DbFile;
                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;
                Application.DoEvents();
                this.comboBox1.Items.Clear();
                // DBにアクセスしてTagSetのリストをComboBoxにセットする.
                try
                {
                    Corpus c = Corpus.CreateFromFile(m_DbFile);
                    DBService svc = DBService.Create(c.DBParam);
                    this.comboBox1.DataSource = svc.LoadTagSetNames();
                }
                catch (Exception ex)
                {
                    ErrorReportDialog edlg = new ErrorReportDialog("Error: ", ex);
                    edlg.ShowDialog();
                }
                this.Cursor = oldCursor;
            }
        }
    }
}
