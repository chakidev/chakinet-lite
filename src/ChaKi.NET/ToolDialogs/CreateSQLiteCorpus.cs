using System;
using System.Windows.Forms;
using ChaKi.Entity.Readers;
using ChaKi.Entity.Settings;
using ChaKi.GUICommon;
using ChaKi.Service.Readers;
using System.IO;

namespace ChaKi
{
    public partial class CreateSQLiteCorpus : Form
    {
        public CreateSQLiteCorpus()
        {
            InitializeComponent();

            // ReaderDef.xmlからType Comboboxの内容を作成
            try
            {
                CorpusSourceReaderFactory factory = CorpusSourceReaderFactory.Instance;
                this.comboBox2.Items.Add("Auto");
                foreach (ReaderDef def in factory.ReaderDefs.ReaderDef)
                {
                    this.comboBox2.Items.Add(def.Name);
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error while reading ReaderDef.xml:", ex);
                edlg.ShowDialog();
            }

            this.comboBox1.SelectedItem = UserSettings.GetInstance().DefaultCorpusSourceEncoding;
            this.comboBox2.SelectedItem = UserSettings.GetInstance().DefaultCorpusSourceType;
            this.textBox4.Text = "0";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string args = "";
            if (this.comboBox1.Text.Length > 0)
            {
                args += string.Format("-e={0} ", this.comboBox1.Text);
            }
            if (this.comboBox2.Text.Length > 0)
            {
                args += string.Format("-t=\"{0}\" ", this.comboBox2.Text);
            }
            if (this.textBox1.Text.Length > 0)
            {
                args += string.Format("-b=\"{0}\" ", this.textBox1.Text);
            }
            if (this.checkBox1.Checked)
            {
                args += "-s ";
            }
            if (this.textBox4.Text.Length > 0)
            {
                int val;
                if (!Int32.TryParse(this.textBox4.Text, out val) || val < 0)
                {
                    MessageBox.Show("Project ID must be zero or a positive number.");
                    return;
                }
                if (val != 0)
                {
                    args += string.Format("-P={0} ", val);
                }
            }
            try
            {
                var attr = File.GetAttributes(this.textBox3.Text);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    args += "-p ";
                }
            }
            catch
            {
            }
            args += "-T=\"SQLite\" ";
            args += string.Format("\"{0}\" \"{1}\"", this.textBox2.Text, this.textBox3.Text);
            this.process1.StartInfo.Arguments = args;
            this.process1.StartInfo.WorkingDirectory = Program.ProgramDir;
            this.process1.Start();
        }

        private void process1_Exited(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private static int ImportFileLastSelectedFilterIndex = 0;

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileFolderDialog dlg = new OpenFileFolderDialog();
            dlg.Title = "Select Input File/Folder";
            dlg.FilterSpec = "Cabocha files (*.cabocha)|*.cabocha|ChaSen files (*.chasen)|*.chasen|MeCab files (*.mecab)|*.mecab|CONLL files (*.conll)|*.conll|CONLLU files (*.conllu)|*.conllu|Text files (*.txt)|*.txt|All files (*.*)|*.*|Folders|*.*";
            dlg.FileMustExist = true;
            if (ImportFileLastSelectedFilterIndex > 0)
            {
                dlg.FilterIndex = ImportFileLastSelectedFilterIndex;
            }
            if (dlg.DoModal() == DialogResult.OK)
            {
                this.textBox2.Text = dlg.FileName;
                ImportFileLastSelectedFilterIndex = dlg.FilterIndex;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Title = "Select Bib File";
            dlg.Filter = "Input files (*.bib)|*.bib|All files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = dlg.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileFolderDialog dlg = new OpenFileFolderDialog();
            dlg.Title = "Select Output SQLite File or Folder";
            dlg.FilterSpec = "SQLite database files (*.db)|*.db|All files (*.*)|*.*|Folders|*.*";
            dlg.DefExt = ".db";
            if (dlg.DoModal() == DialogResult.OK)
            {
                this.textBox3.Text = dlg.FileName;
            }
        }

        private void CreateSQLiteCorpus_FormClosing(object sender, FormClosingEventArgs e)
        {
            UserSettings.GetInstance().DefaultCorpusSourceEncoding = this.comboBox1.Text;
            UserSettings.GetInstance().DefaultCorpusSourceType = this.comboBox2.Text;
        }
    }
}
