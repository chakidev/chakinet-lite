using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Crownwood.DotNetMagic.Common;
using ChaKi.Common;
using System.Net.NetworkInformation;
using System.Text;
using ChaKi.GUICommon;
using ChaKi.Common.Settings;

namespace ChaKi.Options
{
    public partial class OptionDialog : Form
    {
        public EventHandler Applied;

        private static Dictionary<string, VisualStyle> m_PredefinedStyles = new Dictionary<string, VisualStyle>
        {
            { "Office 2007 Silver", VisualStyle.Office2007Silver },
            { "Office 2007 Blue", VisualStyle.Office2007Blue },
            { "Office 2007 Black", VisualStyle.Office2007Black },
            { "Office 2003", VisualStyle.Office2003 },
            { "VisualStudio 2005", VisualStyle.IDE2005 },
            { "Plain", VisualStyle.Plain }
        };

        private GUISetting m_RollbackData;

        public OptionDialog()
        {
            InitializeComponent();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            GUISetting.Instance.Visual = m_PredefinedStyles[(string)comboBox1.SelectedItem];
//            Program.MainForm.ChangeStyle(m_PredefinedStyles[comboBox1.SelectedText]);
        }

        /// <summary>
        /// ダイアログ開始時の設定値表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OptionDialog_Shown(object sender, EventArgs e)
        {
            m_RollbackData = new GUISetting(GUISetting.Instance);  // backup

            this.comboBox1.Text = GUISetting.Instance.Visual.ToString();

            this.fontSampleLabel1.Font = GUISetting.Instance.GetBaseTextFont();
            this.fontSampleLabel2.Font = GUISetting.Instance.GetBaseAnsiFont();

            this.checkBox1.Checked = GUISetting.Instance.SearchSettings.UseRemote;
            this.textBox1.Text = GUISetting.Instance.SearchSettings.RemoteAddress;

            this.numericUpDown1.Value = GUISetting.Instance.SearchSettings.ContextRetrievalRange;
            this.comboBox2.SelectedIndex = (int)GUISetting.Instance.SearchSettings.ContextRetrievalUnit;
            this.checkBox4.Checked = GUISetting.Instance.SearchSettings.RetrieveExtraWordProperty;

            this.checkBox2.Checked = GUISetting.Instance.DepEditSettings.ReverseDepArrowDirection;
            this.checkBox3.Checked = GUISetting.Instance.ContextPanelSettings.UseSpacing;

            this.colorPickerButton1.Color = ColorTranslator.FromHtml(GUISetting.Instance.KwicViewBackground);
            this.colorPickerButton2.Color = ColorTranslator.FromHtml(GUISetting.Instance.DepEditBackground);

            this.comboBox3.Items.Add("en-US (English)");
            this.comboBox3.Items.Add("ja-JP (日本語)");
            this.comboBox3.Text = GUISetting.Instance.UILocale;

            this.textBox2.Text = GUISetting.Instance.ExternalEditorPath;

            this.checkBox5.Checked = GUISetting.Instance.SkipDbSchemaConversionDialog;

            this.checkBox6.Checked = GUISetting.Instance.GitSettings.ExportBunsetsuSegmentsAndLinks;
            this.textBox3.Text = GUISetting.Instance.GitSettings.LocalRepositoryBasePath;
        }

        // "OK"
        private void button1_Click(object sender, EventArgs e)
        {
            Apply();
            Close();
        }

        // "Cancel"
        private void button2_Click(object sender, EventArgs e)
        {
            GUISetting settings = GUISetting.Instance;
            settings.CopyFrom(m_RollbackData);
            FontDictionary.Current = settings.Fonts;
            KwicViewSettings.Current = settings.KwicViewSettings;
            DepEditSettings.Current = settings.DepEditSettings;
            SearchSettings.Current = settings.SearchSettings;
            TagSelectorSettings.Current = settings.TagSelectorSettings;
            CollocationViewSettings.Current = settings.CollocationViewSettings;
            ContextPanelSettings.Current = settings.ContextPanelSettings;
            GitSettings.Current = settings.GitSettings;

            GUISetting.Instance.RaiseUpdate();
            Close();
        }

        // "Apply"
        private void button4_Click(object sender, EventArgs e)
        {
            Apply();
        }

        private void Apply()
        {
            GUISetting.Instance.SearchSettings.UseRemote = this.checkBox1.Checked;
            GUISetting.Instance.SearchSettings.RemoteAddress = string.Copy(this.textBox1.Text);

            GUISetting.Instance.SearchSettings.ContextRetrievalRange = (uint)this.numericUpDown1.Value;
            GUISetting.Instance.SearchSettings.ContextRetrievalUnit = (ContextCountUnit)this.comboBox2.SelectedIndex;
            GUISetting.Instance.SearchSettings.RetrieveExtraWordProperty = this.checkBox4.Checked;

            GUISetting.Instance.DepEditSettings.ReverseDepArrowDirection = this.checkBox2.Checked;
            GUISetting.Instance.ContextPanelSettings.UseSpacing = this.checkBox3.Checked;

            GUISetting.Instance.KwicViewBackground = ColorTranslator.ToHtml(this.colorPickerButton1.Color);
            GUISetting.Instance.DepEditBackground = ColorTranslator.ToHtml(this.colorPickerButton2.Color);

            GUISetting.Instance.ExternalEditorPath = this.textBox2.Text;

            GUISetting.Instance.SkipDbSchemaConversionDialog = this.checkBox5.Checked;

            GUISetting.Instance.GitSettings.ExportBunsetsuSegmentsAndLinks = this.checkBox6.Checked;
            GUISetting.Instance.GitSettings.LocalRepositoryBasePath = this.textBox3.Text;

            string localestr;
            int i;
            if ((i = this.comboBox3.Text.IndexOf(' ')) < 0)
            {
                localestr = this.comboBox3.Text;
            }
            else
            {
                localestr = this.comboBox3.Text.Substring(0, i);
            }
            GUISetting.Instance.UILocale = localestr;

            GUISetting.Instance.RaiseUpdate();

            if (Applied != null)
            {
                Applied(this, null);
            }
        }

        /// <summary>
        /// Base Text Font変更ボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            DoChangeFontDialog("BaseText", this.fontSampleLabel1);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DoChangeFontDialog("BaseAnsi", this.fontSampleLabel2);
        }

        private void DoChangeFontDialog(string tag, FontSampleLabel label)
        {
            FontDialog fd = new FontDialog();
            fd.Font = m_RollbackData.Fonts[tag];
            if (fd.ShowDialog() == DialogResult.OK)
            {
                // Regular Styleをサポートしているフォントかを調べる
                Font tryFontCreation = null;
                try
                {
                    tryFontCreation = new Font(fd.Font.Name, 10f);
                }
                catch (Exception ex)
                {
                    ErrorReportDialog edlg = new ErrorReportDialog("Invalid Font:", ex);
                    edlg.ShowDialog();
                    return;
                }
                finally
                {
                    if (tryFontCreation != null) tryFontCreation.Dispose();
                }
                GUISetting.Instance.SetFont(tag, fd.Font);
                label.Font = fd.Font;
            }
        }

        /// <summary>
        /// Remote Addressの接続テスト
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            string host = this.textBox1.Text;
            string[] fields = host.Split(new char[] { ':' });
            if (fields.Length > 0)
            {
                host = fields[0];
            }
            try
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                PingReply reply = pingSender.Send(host, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    MessageBox.Show(string.Format("Ping: Successfully received a reply from {0}", reply.Address.ToString()));
                }
                else
                {
                    MessageBox.Show("Ping failed.");
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Ping failed:", ex);
                edlg.ShowDialog();
            }
            this.Cursor = oldCur;
        }

        private void OptionDialog_Load(object sender, EventArgs e)
        {
            this.tabControl1.SelectedTab = this.tabPage1;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Exe files (*.exe)|*.exe|All files (*.*)|*.*";
            dlg.CheckFileExists = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox2.Text = dlg.FileName;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MainForm.Instance.ResetLayout();
            //Application.DoEvents();
        }
    }
}
