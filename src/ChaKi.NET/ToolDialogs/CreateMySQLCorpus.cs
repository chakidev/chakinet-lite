using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ChaKi.Entity.Readers;
using ChaKi.Entity.Settings;
using ChaKi.GUICommon;
using ChaKi.Service.Readers;

namespace ChaKi
{
    public partial class CreateMySQLCorpus : Form
    {
        public List<string> DatabaseCandidates { get; set; }

        public string DBMS { get; set; }
        public string Server { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        public CreateMySQLCorpus()
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

        private static int ImportFileLastSelectedFilterIndex = 0;

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileFolderDialog dlg = new OpenFileFolderDialog();
                dlg.Title = "Select Input File/Folder";
                dlg.FilterSpec = "Cabocha files (*.cabocha)|*.cabocha|ChaSen files (*.chasen)|*.chasen|MeCab files (*.mecab)|*.mecab|CONLL files (*.conll)|*.conll|CONLLU files (*.conllu)|*.conllu|Text files (*.txt)|*.txt|All files (*.*)|*.*|Folders|*.*";
                if (ImportFileLastSelectedFilterIndex > 0)
                {
                    dlg.FilterIndex = ImportFileLastSelectedFilterIndex;
                }

                if (dlg.DoModal() == DialogResult.OK)
                {
                    this.textBox1.Text = dlg.FileName;
                    ImportFileLastSelectedFilterIndex = dlg.FilterIndex;
                }
            }
            catch (BadImageFormatException)
            {
                // 64bitでOpenFileFolderDialog.dllのロードに失敗した場合は通常のOpenFileDialogで代用
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Select Input File";
                dlg.Filter = "Cabocha files (*.cabocha)|*.cabocha|ChaSen files (*.chasen)|*.chasen|MeCab files (*.mecab)|*.mecab|CONLL files (*.conll)|*.conll|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                dlg.CheckPathExists = true;
                dlg.CheckFileExists = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.textBox2.Text = dlg.FileName;
                }
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
                this.textBox3.Text = dlg.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ShowDatabaseListDialog dlg = new ShowDatabaseListDialog();
            dlg.Data = DatabaseCandidates;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox2.Text = dlg.DatabaseName;
            }
            dlg.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Create Def File to Store DB Connection Parameters";
            dlg.Filter = "Def files (*.def)|*.def|All files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string def_path = dlg.FileName;
                // Defファイルを作成
                using (StreamWriter st = new StreamWriter(def_path))
                {
                    st.WriteLine(string.Format("db={0}", this.DBMS));
                    st.WriteLine(string.Format("corpusname={0}", this.textBox2.Text));
                    st.WriteLine(string.Format("server={0}", this.Server));
                    st.WriteLine(string.Format("user={0}", this.User));
                    st.WriteLine(string.Format("password={0}", this.Password));
                    st.WriteLine(string.Format("source=\"{0}\"", this.textBox1.Text));
                    st.WriteLine(string.Format("bibsource=\"{0}\"", this.textBox3.Text));
//                    st.WriteLine(string.Format("dictsource=\"{0}\"", this.textBox4.Text));
                }
                // CabochaファイルとDefファイルを指定してCreateCorpus.exeを起動
                string args = "";
                if (this.comboBox1.Text.Length > 0)
                {
                    args += string.Format("-e={0} ", this.comboBox1.Text);
                }
                if (this.comboBox2.Text.Length > 0)
                {
                    args += string.Format("-t=\"{0}\" ", this.comboBox2.Text);
                }
                if (this.textBox3.Text.Length > 0)
                {
                    args += string.Format("-b=\"{0}\" ", this.textBox3.Text);
                }
                if (this.checkBox2.Checked)
                {
                    args += "-p ";
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
                args += "-T=\"DBMS\" ";
                //if (this.textBox4.Text.Length > 0)
                //{
                //    args += string.Format("-l=\"{0}\" ", this.textBox4.Text);
                //}
                args += string.Format("\"{0}\" \"{1}\"", this.textBox1.Text, def_path);
                this.process1.StartInfo.Arguments = args; 
                this.process1.StartInfo.WorkingDirectory = Program.ProgramDir;
                this.process1.Start();
            }
        }

        private void process1_Exited(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CreateMySQLCorpus_FormClosing(object sender, FormClosingEventArgs e)
        {
            UserSettings.GetInstance().DefaultCorpusSourceEncoding = this.comboBox1.Text;
            UserSettings.GetInstance().DefaultCorpusSourceType = this.comboBox2.Text;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            this.checkBox2.Enabled = false;
            try
            {
                var filename = textBox1.Text;
                FileAttributes attr = File.GetAttributes(filename);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    this.checkBox2.Enabled = true;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
