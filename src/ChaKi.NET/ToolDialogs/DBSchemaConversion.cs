using System;
using System.Windows.Forms;
using ChaKi.Entity;
using ChaKi.Entity.Corpora;
using ChaKi.GUICommon;
using ChaKi.Service.Database;

namespace ChaKi.ToolDialogs
{
    public partial class DBSchemaConversion : Form
    {
        private DBService m_DbService;
        private Corpus m_Corpus;

        public DBSchemaConversion(DBService dbs, Corpus c)
        {
            m_DbService = dbs;
            m_Corpus = c;

            InitializeComponent();

            // high-DPI (FormのAutoSizeModeだけでは解決しない箇所)
            this.richTextBox1.Height = Math.Max(80, this.button1.Top - this.richTextBox1.Top - 5);
        }

        public void DoConversion()
        {
            this.textBox1.Text = m_Corpus.Name;
            this.textBox2.Text = m_DbService.DBParam.DBType;
            this.textBox3.Text = m_Corpus.Schema.Version.ToString();
            this.textBox4.Text = CorpusSchema.CurrentVersion.ToString();
            this.checkBox1.Checked = GUISetting.Instance.SkipDbSchemaConversionDialog;

            if (GUISetting.Instance.SkipDbSchemaConversionDialog)
            {
                DoConversionImmediately();
            }
            else
            {
                ShowDialog();
            }

            GUISetting.Instance.SkipDbSchemaConversionDialog = this.checkBox1.Checked;
        }

        private void DoConversionImmediately()
        {
            var oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            try
            {
                m_DbService.ConvertSchema(m_Corpus, AppendMessage);
            }
            finally
            {
                this.Cursor = oldCursor;
            }
        }

        private void DoConversionImpl()
        {
            var oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                Application.DoEvents();
                if (m_DbService.ConvertSchema(m_Corpus, AppendMessage))
                {
                    AppendMessage("Done!\n");
                }
            }
            finally
            {
                this.Cursor = oldCursor;
            }
        }

        public void AppendMessage(string msg)
        {
            this.richTextBox1.AppendText(msg);
            this.richTextBox1.AppendText("\n");
        }

        private void DBSchemaConversion_Shown(object sender, EventArgs e)
        {
            if (ChaKi.Common.Widgets.MessageBox.Show(
                string.Format("The database '{0}' must be auto-converted{1}to the latest schema before loading.{1}{1}It is highly recommended that you make a backup copy,{1}then press 'OK'.", m_Corpus.Name, System.Environment.NewLine),
                "Confirm",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                this) == System.Windows.Forms.DialogResult.OK)
            {
                DoConversionImpl();
            }
            else
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            }
        }
    }
}
