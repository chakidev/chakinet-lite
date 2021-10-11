using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ChaKi.Common.Settings;
using ChaKi.Text2Corpus.Helpers;
using ChaKi.Text2Corpus.Properties;
using System.Threading;
using System.Globalization;
using System.Diagnostics;
using ChaKi.GUICommon;

namespace ChaKi.Text2Corpus
{
    public partial class TextToCorpus : Form
    {
        public bool DoneSuccessfully { get; private set; }

        private bool m_ShowDetail = false;

        public TextToCorpus()
        {
            InitializeComponent();
            DoneSuccessfully = false;
            MecabHelper.Setup(this.comboBox1.SelectedItem as string);
            UnidicHelper.Setup();
            UpdateMecabFormatList();
        }

        public void SetInputFile(string file)
        {
            this.textBox1.Text = file;
            SetOutput();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            SetOutput();
        }

        private void SetOutput()
        {
            var file = this.textBox1.Text;
            if (Directory.Exists(file))
            {
#if CHAMAME
                this.textBox2.Text = Path.Combine(file, "out");
#else
                this.textBox2.Text = Path.Combine(file,
                    Path.Combine("out", Path.ChangeExtension(Path.GetFileName(file), ".db")));
#endif

            }
            else if (File.Exists(file))
            {
#if CHAMAME
                this.textBox2.Text = Path.Combine(Path.GetDirectoryName(file), "out");
#else
                this.textBox2.Text = Path.Combine(Path.GetDirectoryName(file),
                    Path.Combine("out", Path.ChangeExtension(Path.GetFileName(file), ".db")));
#endif
            }
        }

        public string GetOutputFile()
        {
            return this.textBox2.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog() 
            {
                CheckFileExists = true,
                Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = false
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = dlg.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = dlg.SelectedPath;
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
#if CHAMAME
            var folder = this.textBox2.Text;
            if (Directory.Exists(folder))
            {
                Process.Start(folder);
            }
#else
            var dlg = new SaveFileDialog() 
            {
                DefaultExt = "db",
                Filter="SQLite Database Files (*.db)|*.db|All Files (*.*)|*.*", 
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox2.Text = dlg.FileName;
            }
#endif
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var worker = new TextToCorpusWorker();
            worker.ExceptionOccurred += new EventHandler<ThreadExceptionEventArgs>(worker_ExceptionOccurred);
            worker.StateChanged += new EventHandler<StateChangedEventArgs>(worker_StateChanged);
            worker.ProgressChanged += new EventHandler<ProgressEventArgs>(worker_ProgressChanged);
            worker.DetailedLog += Worker_DetailedLog;
            worker.Done += new EventHandler(worker_Done);
            worker.Aborted += new EventHandler(worker_Aborted);
            EnableControls(false);
            DoneSuccessfully = false;
            Text2CorpusSettings.Instance.CloseOnDone = this.checkBox4.Checked;
            try
            {
                var inFile = this.textBox1.Text;
                string outdir;
                if (Directory.Exists(inFile))
                {
                    outdir = Path.Combine(inFile, "out");
                }
                else
                {
                    outdir = Path.Combine(Path.GetDirectoryName(inFile), "out");
                }
                var outdb = this.textBox2.Text;
                TextFileHelper.Setup(this.textBox4.Text, (CRLFModes)this.comboBox3.SelectedIndex);
                MecabHelper.Setup(this.comboBox1.SelectedItem as string);
                CabochaHelper.Setup();

                worker.Convert(inFile, outdir, outdb,
                    s =>
                    {
                        bool result = false;
                        this.Invoke(new Action(() =>
                            {
                                result = (MessageBox.Show(s.Replace("\\n", Environment.NewLine), Resources.S020, MessageBoxButtons.YesNo) == DialogResult.Yes);
                            }));
                        return result;
                    },
                    this.checkBox1.Checked,
                    this.checkBox3.Checked,
                    this.checkBox5.Checked ? ((NormalizationForm?)NormalizationForm.FormKC) : null,
                    this.checkBox7.Checked,
                    (BunruiOutputFormat)this.comboBox5.SelectedIndex,
                    this.comboBox1.SelectedItem as string,
                    this.comboBox4.SelectedItem as string,
                    this.comboBox2.SelectedItem as string
                    );
            }
            catch (Exception ex)
            {
                var dlg = new ErrorReportDialog("Error", ex);
                dlg.ShowDialog();
                EnableControls(true);
            }
        }

        void EnableControls(bool f)
        {
            foreach (var child in this.Controls)
            {
                var control = child as Control;
                if (control != null)
                {
                    control.Enabled = f;
                }
            }
        }

        void worker_Done(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                this.progressBar1.Value = 100;
                EnableControls(true);
                DoneSuccessfully = true;
                if (Text2CorpusSettings.Instance.CloseOnDone)
                {
                    Close();
                }
            }));
        }

        void worker_Aborted(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                EnableControls(true);
            }));
        }

        void worker_ProgressChanged(object sender, ProgressEventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                this.progressBar1.Value = e.Progress;
                Application.DoEvents();
            }));
        }

        void worker_StateChanged(object sender, StateChangedEventArgs e)
        {
            this.Invoke(new Action(() =>
                {
                    this.textBox3.Text = e.State;
                    Application.DoEvents();
                }));
        }

        void worker_ExceptionOccurred(object sender, ThreadExceptionEventArgs e)
        {
            this.Invoke(new Action(() =>
                {
                    var dlg = new ErrorReportDialog("Error", e.Exception);
                    dlg.ShowDialog();
                    this.textBox3.Text = "Error";
                }));
        }

        private void Worker_DetailedLog(object sender, DetailedLogEventArgs e)
        {
            if (e.Message == null)
            {
                this.Invoke(new Action(() =>
                {
                    this.textBox5.Text = string.Empty;
                }));
                return;
            }
            this.Invoke(new Action(() =>
            {
                this.textBox5.Text += (e.Message + Environment.NewLine);
            }));
        }



        private void TextToCorpus_Load(object sender, EventArgs e)
        {
            var settings = Text2CorpusSettings.Instance;
            this.checkBox1.Checked = settings.DoSentenceSeparation;
            this.checkBox2.Checked = true/*settings.DoWordAnalysis*/;
            this.checkBox3.Checked = settings.DoChunkAnalysis;
            this.comboBox1.Items.Clear();
            this.comboBox1.Items.Add("default");
            this.comboBox1.Items.AddRange(MecabHelper.DictNames);
            this.comboBox1.SelectedItem = settings.MecabModel;
            this.comboBox2.SelectedItem = settings.CabochaModel;
            this.comboBox3.SelectedIndex = (int)settings.CRLFMode;
            this.textBox4.Text = settings.SeparatorChars;
            this.checkBox4.Checked = Text2CorpusSettings.Instance.CloseOnDone;
            this.button6.Text = Resources.ShowDetailedLog;
            this.checkBox5.Checked = (settings.UnicodeNormalization == NormalizationForm.FormKC);
            this.checkBox7.Checked = settings.DoZenkakuConversion;
            this.comboBox4.SelectedItem = settings.MecabOutputFormat;
#if CHAMAME
            this.Text = "ChaMame";
            this.Icon = Resources.ChaMame;
            this.label5.Enabled = true;
            this.comboBox4.Enabled = true;
            this.label6.Enabled = true;
            this.comboBox5.Enabled = true;
            this.comboBox5.SelectedIndex = (int)Text2CorpusSettings.Instance.BunruiOutputFormat;
            this.groupBox5.Text = Resources.OutputLabel;
            this.button3.Text = Resources.ShowFolder;
            this.textBox2.ReadOnly = true;
            textBox2_TextChanged(this, EventArgs.Empty);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.HelpButton = true;
#endif
            this.MaximumSize = new Size(SystemInformation.MaxWindowTrackSize.Width, this.Size.Height);
            var w = settings.WindowWidth;
            if (w > 400 && w < SystemInformation.MaxWindowTrackSize.Width)
            {
                this.Width = w;
            }
        }

        private void TextToCorpus_FormClosing(object sender, FormClosingEventArgs e)
        {
            var settings = Text2CorpusSettings.Instance;
            settings.WindowWidth = this.Width;
            settings.DoSentenceSeparation = this.checkBox1.Checked;
            //settings.DoWordAnalysis = this.checkBox2.Checked;
            settings.DoChunkAnalysis = this.checkBox3.Checked;
            settings.MecabModel = this.comboBox1.SelectedItem as string;
            settings.CabochaModel = this.comboBox2.SelectedItem as string;
            settings.SeparatorChars = this.textBox4.Text;
            settings.OutputFile = this.textBox2.Text;
            settings.BunruiOutputFormat = (BunruiOutputFormat)this.comboBox5.SelectedIndex;
            try
            {
                settings.CRLFMode = (CRLFModes)this.comboBox3.SelectedIndex;
            }
            catch
            {
                settings.CRLFMode = CRLFModes.MultipleOnly;
            }
            settings.UnicodeNormalization = this.checkBox5.Checked ? (NormalizationForm?)NormalizationForm.FormKC : null;
            settings.DoZenkakuConversion = this.checkBox7.Checked;
            settings.MecabOutputFormat = this.comboBox4.SelectedItem as string;

            Text2CorpusSettings.Instance.CloseOnDone = this.checkBox4.Checked;

            try
            {
                settings.Save(Program.SettingsFile);
            }
            catch (Exception ex)
            {
                var dlg = new ErrorReportDialog("Error", ex);
                dlg.ShowDialog();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkBox2_Click(object sender, EventArgs e)
        {
            // Word Analysis checkは常に選択状態にする.
            this.checkBox2.Checked = true;
        }

        private void TextToCorpus_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void TextToCorpus_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                SetInputFile(files[0]);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (!m_ShowDetail)
            {
                // Detailを表示する
                var sz = this.Size;
                var dy = this.textBox5.Bottom - this.button6.Bottom;
                this.MaximumSize = new Size(0, 0);
                this.Size = new Size(sz.Width, sz.Height + dy);
                this.textBox5.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                this.button6.Text = Resources.HideDetailedLog;
                m_ShowDetail = true;
            }
            else
            {
                // Detailを非表示にする
                var sz = this.Size;
                var height = this.button6.Bottom + 54;
                this.textBox5.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                this.MaximumSize = new Size(SystemInformation.MaxWindowTrackSize.Width, height);
                this.Size = new Size(sz.Width, height);
                this.button6.Text = Resources.ShowDetailedLog;
                m_ShowDetail = false;
            }
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdateMecabFormatList();
        }

        private void UpdateMecabFormatList()
        {
            this.comboBox4.Items.Clear();
            this.comboBox4.Items.Add("default");
            var dicname = comboBox1.SelectedItem as string;
            if (dicname != null)
            {
                this.comboBox4.Items.AddRange(MecabHelper.ListOutputFormats(dicname));
            }
            this.comboBox4.SelectedIndex = 0;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            var file = this.textBox2.Text;
            this.button3.Enabled = Directory.Exists(file);
        }

        private void TextToCorpus_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            var helpFile = Path.Combine(Program.ProgramDir, "ChaMameHelp.html");
            try
            {
                Process.Start(helpFile);
            }
            catch (Exception ex)
            {
                new ErrorReportDialog(helpFile, ex).ShowDialog();
            }
            e.Cancel = true;
        }
    }
}
