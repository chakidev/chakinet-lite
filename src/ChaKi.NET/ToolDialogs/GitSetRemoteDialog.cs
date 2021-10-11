using ChaKi.Service.Git;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.ToolDialogs
{
    public partial class GitSetRemoteDialog : Form
    {
        private const string nvmsg = "URL not validated.";
        private IGitService m_Service;
        public GitSetRemoteDialog(IGitService svc)
        {
            this.m_Service = svc;

            InitializeComponent();

            this.label2.Text = nvmsg;
            this.textBox1.Text = m_Service.GetRemoteUrl();
            this.button1.Enabled = true;
            this.textBox1.TextChanged += TextBox1_TextChanged;
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (this.textBox1.Text.Length == 0)
            {
                // 空文字列にする場合＝URLを削除する場合はOKを有効とする
                this.button1.Enabled = true;
                return;
            }
            this.label2.Text = nvmsg;
        }

        public string RemoteUrl { get { return this.textBox1.Text; } }

        private void button3_Click(object sender, EventArgs e)
        {
            this.label2.Text = nvmsg;
            try
            {
                m_Service.CredentialProvider =
                    site =>
                    {
                        var loginDlg = new GitLoginDialog() { Url = site };
                        if (loginDlg.ShowDialog() == DialogResult.OK)
                        {
                            return new Tuple<string, string>(loginDlg.Username, loginDlg.Password);
                        }
                        return null;
                    };
                if (m_Service.CheckRemoteUrl(this.textBox1.Text))
                {
                    this.label2.Text = "URL validated.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
