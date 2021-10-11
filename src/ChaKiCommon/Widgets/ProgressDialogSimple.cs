using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Common.Widgets
{
    public partial class ProgressDialogSimple : Form, IProgress
    {
        public ProgressDialogSimple()
        {
            InitializeComponent();
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = 100;
            this.progressBar1.Value = 0;
        }

        public string Title
        {
            get { return this.label1.Text; }
            set { this.label1.Text = value; }
        }

        public int Percentage
        {
            set
            {
                this.progressBar1.Value = value;
            }
        }

        public int ProgressMax
        {
            set
            {
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() => { this.progressBar1.Maximum = value; }));
                }
                else
                {
                    this.progressBar1.Maximum = value;
                }
            }
        }

        public int ProgressCount
        {
            set
            {
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() => { this.progressBar1.Value = value; }));
                }
                else
                {
                    this.progressBar1.Value = value;
                }
            }
        }

        // does not support cancel
        public bool Canceled => false;

        // does not support cancel
        public event EventHandler WorkerCancelled;

        public void EndWork()
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new Action(() => { this.Close(); }));
            }
            else
            {
                this.Close();
            }
        }

        public void ProgressReset()
        {
            this.progressBar1.Value = 0;
        }
    }
}
