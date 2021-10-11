using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using ChaKi.Common;
using ChaKi.Common.Settings;
using System.Text.RegularExpressions;

namespace ChaKi
{
    public partial class SplashScreen : Form
    {
        DoWorkEventHandler m_BackgroundWork;

        public SplashScreen(DoWorkEventHandler backgroundWork)
        {
            InitializeComponent();

            m_BackgroundWork = backgroundWork;
            this.backgroundWorker1.DoWork += new DoWorkEventHandler(m_BackgroundWork);
            this.backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);

            this.button1.Enabled = false;
            this.button2.Enabled = false;

            var versionTokens = AboutChaKiDialog.AssemblyVersion.Split('.');
            if (versionTokens.Length >= 3)
            {
                try {
                    var version = string.Format("{0}.{1:D2}.{2}", 
                        int.Parse(versionTokens[0]),
                        int.Parse(versionTokens[1]),
                        int.Parse(versionTokens[2]));
                    this.label4.Text = version;
                }
                catch
                {
                    // do nothing.
                }
            }

            this.Shrink = true;
        }

        public string UserName
        {
            get { return this.textBox1.Text; }
        }

        public string Password
        {
            get { return this.textBox2.Text; }
        }

        void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                return;
            }
            if (GUISetting.Instance.AutoLogon)
            {
                this.DialogResult = DialogResult.Abort;
            }
            else
            {
                this.button1.Enabled = true;
                this.button2.Enabled = true;
            }
        }

        public string ProgressMsg
        {
            set { this.progressMsg.Text = value; }
        }

        private SizeF currentScaleFactor = new SizeF(1f, 1f);

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            //Record the running scale factor used
            this.currentScaleFactor = new SizeF(
               this.currentScaleFactor.Width * factor.Width,
               this.currentScaleFactor.Height * factor.Height);
        }

        public bool Shrink
        {
            set
            {
                if (value)
                {
                    this.Height = (int)(100 * this.currentScaleFactor.Height);
                }
                else
                {
                    this.Height = (int)(280 * this.currentScaleFactor.Height);
                }
            }
        }

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            this.backgroundWorker1.RunWorkerAsync(this);
        }

        /// <summary>
        /// Logon Clicled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Cancel clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
