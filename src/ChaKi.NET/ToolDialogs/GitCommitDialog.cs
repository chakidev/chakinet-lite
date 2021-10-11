using ChaKi.Common;
using ChaKi.Common.Widgets;
using ChaKi.Entity.Corpora;
using ChaKi.GUICommon;
using ChaKi.Service.Export;
using ChaKi.Service.Git;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using MessageBox = ChaKi.Common.Widgets.MessageBox;

namespace ChaKi.ToolDialogs
{
    public partial class GitCommitDialog : Form
    {
        private Corpus m_Corpus;
        private bool m_ExportCancelFlag;
        private string m_BackupFile;
        private IGitService m_Service;

        public string Message { get { return this.textBox4.Text; } }

        public bool AutoPush { get { return this.checkBox1.Checked; } }

        public GitCommitDialog(IGitService svc, Corpus crps)
        {
            InitializeComponent();

            this.m_Service = svc;
            this.m_Corpus = crps;
            this.textBox1.Text = svc.Name;
            this.textBox2.Text = svc.RepositoryPath;
            var remoteurl = svc.GetRemoteUrl();
            this.textBox3.Text = remoteurl;
            this.checkBox1.Enabled = (remoteurl.Length > 0);
            string name, email;
            svc.GetIdentity(out name, out email);
            this.textBox5.Text = $"{name} ({email})";
            this.m_ExportCancelFlag = false;

            this.Load += GitCommitDialog_Load;
            this.FormClosed += GitCommitDialog_FormClosed;
        }

        private void GitCommitDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                File.Delete(this.m_BackupFile);
            }
            catch (Exception ex)
            {
                var dlg = new ErrorReportDialog("Error:", ex);
                dlg.ShowDialog();
            }
        }

        private void GitCommitDialog_Load(object sender, EventArgs e)
        {
            var orgFile = $"{Path.Combine(this.textBox2.Text, this.textBox1.Text + ".ann")}";
            this.m_BackupFile = orgFile + ".bak";
            GitRepositories.Disable();
            try
            {
                if (!File.Exists(orgFile))
                {
                    File.Create(orgFile).Close();
                }
                File.Delete(this.m_BackupFile);
                File.Move(orgFile, this.m_BackupFile);

                var dlg = new ProgressDialogSimple() { Title = "Checking for updates..." };
                Task exportTask = null;
                dlg.Load += (_e, _a) => 
                {
                    exportTask = ExportAsync(dlg);
                };
                dlg.ShowDialog();
                exportTask?.Wait(); // Task終了前にProgressDialogが閉じられた時には終了までブロック
                if (m_Service.IsClean())
                {
                    MessageBox.Show(ChaKi.Properties.Resources.RepositoryIsClean, "Git Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    {
                        this.Close();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                var dlg = new ErrorReportDialog("Error:", ex);
                dlg.ShowDialog();
                Close();
            }
            finally
            {
                GitRepositories.Enable();
            }
        }

        private Task ExportAsync(IProgress progress)
        {
            return Task.Factory.StartNew(() =>
            {
                var file = Path.Combine(this.textBox2.Text, $"{this.m_Corpus.Name}.ann");
                using (var wr = new StreamWriter(file, false))
                {
                    var exporter = new ExportServiceAnnotation(wr);
                    try
                    {
                        exporter.ExportCorpus(
                            this.m_Corpus, 
                            Program.MainForm.GetCurrentProjectId(),
                            ref this.m_ExportCancelFlag, 
                            (p, o) => { progress.ProgressCount = p; });
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    finally
                    {
                        progress.EndWork();
                    }
                }
            });
        }

        // Set Remote
        private void button3_Click(object sender, EventArgs e)
        {
            var dlg = new GitSetRemoteDialog(this.m_Service);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox3.Text = dlg.RemoteUrl;
                try
                {
                    m_Service.SetRemoteUrl(dlg.RemoteUrl);
                }
                catch
                {
                    // ignore duplication error
                }
                this.checkBox1.Enabled = (dlg.RemoteUrl.Length > 0);
            }
        }

        // Set Author
        private void button5_Click(object sender, EventArgs e)
        {
            var dlg = new GitSetIdentityDialog(this.m_Service);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox5.Text = $"{dlg.Username} ({dlg.Email})";
                m_Service.SetIdentity(dlg.Username, dlg.Email);
            }
        }
    }
}
