using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ChaKi.Common.Widgets
{
    public partial class ProgressDialog : Form, IProgress
    {
        public DoWorkEventHandler Work { get; set; }
        public object WorkerParameter { get; set; }
        public bool Canceled { get; private set; }
        public event EventHandler WorkerCancelled;
        public string Title { get { return this.label1.Text; } set { this.label1.Text = value; } }

        private BackgroundWorker m_Worker;

        public ProgressDialog()
        {
            InitializeComponent();

            this.ProgressMax = 100;
            this.ProgressReset();
            this.Canceled = false;
            this.linkLabel1.ForeColor = this.ForeColor;
            this.linkLabel1.LinkColor = this.ForeColor;
            this.linkLabel1.VisitedLinkColor = this.ForeColor;
            this.linkLabel1.ActiveLinkColor = this.ForeColor;
            this.linkLabel1.DisabledLinkColor = this.ForeColor;
            this.linkLabel1.LinkArea = new LinkArea(0, 0);
            this.linkLabel1.LinkBehavior = LinkBehavior.NeverUnderline;
            this.linkLabel1.Cursor = Cursors.Arrow;
            this.linkLabel1.BackColor = Color.Transparent;
            this.Load += new EventHandler(ProgressDialog_Load);
            m_Worker = null;
        }

        private void DisposeImpl()
        {
            if (m_Worker != null)
            {
                m_Worker.Dispose();
            }
        }

        void ProgressDialog_Load(object sender, EventArgs e)
        {
            if (this.Work != null)
            {
                m_Worker = new BackgroundWorker();
                m_Worker.WorkerReportsProgress = true;
                m_Worker.DoWork += this.Work;
                m_Worker.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                m_Worker.RunWorkerAsync(WorkerParameter);
            }
        }

        void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.ProgressCount = e.ProgressPercentage;
        }

        public bool CancelEnabled
        {
            set
            {
                this.button1.Enabled = value;
            }
        }

        public int ProgressMax
        {
            set
            {
                if (this.IsHandleCreated)
                {
                    var action = new Action<int>(v => this.progressBar1.Maximum = v);
                    if (this.InvokeRequired)
                    {
                        this.Invoke(action, value);
                    }
                    else
                    {
                        action(value);
                    }
                }
            }
        }

        public int ProgressCount
        {
            set
            {
                if (this.IsHandleCreated)
                {
                    var action = new Action<int>(v =>
                        {
                            this.progressBar1.Value = v;
                            this.linkLabel1.Text = string.Format("{0} %", v);
                        });
                    if (this.InvokeRequired)
                    {
                        this.Invoke(action, value);
                    }
                    else
                    {
                        action(value);
                    }
                }
            }
        }

        public int[] ProgressDetail
        {
            set
            {
                if (this.IsHandleCreated)
                {
                    var action = new Action<int[]>(v =>
                    {
                        this.linkLabel1.Text = string.Format("{0}/{1} ({2} %)", v[0], v[1], (int)(v[0]*100.0/v[1]));
                    });
                    if (this.InvokeRequired)
                    {
                        this.Invoke(action, value);
                    }
                    else
                    {
                        action(value);
                    }
                }
            }
        }

        public void ProgressReset()
        {
            if (this.IsHandleCreated)
            {
                var action = new Action(() => { this.progressBar1.Value = 0; this.linkLabel1.Text = string.Empty; });
                if (this.InvokeRequired)
                {
                    this.Invoke(action);
                }
                else
                {
                    action();
                }
            }
        }

        public void EndWork()
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Canceled = true;
            if (WorkerCancelled != null)
            {
                WorkerCancelled(sender, e);
            }
        }
    }
}
