using System.Windows.Forms;
using ChaKi.Service.Collocation;
using System;
using ChaKi.GUICommon;

namespace ChaKi.ToolDialogs
{
    public partial class CollocationProgressDialog : Form
    {
        public ICollocationService Service { get; set; }

        public CollocationProgressDialog()
        {
            InitializeComponent();

            this.Service = null;
        }

        private void CollocationProgressDialog_Load(object sender, System.EventArgs e)
        {
            if (this.Service != null)
            {
                this.progressBar1.Value = 0;
                this.Service.NotifyAsyncProgress += new ProgressEventHandler(Service_NotifyAsyncProgress);
                AsyncCallback callback = new AsyncCallback(this.EndAsyncHandler);
                this.Service.ExecAsync(callback);
            }
        }

        private delegate void SetProgressDelegate(int v);

        private void Service_NotifyAsyncProgress(object src, ProgressEventArgs args)
        {
            SetProgressDelegate dele = (int v) => { this.progressBar1.Value = v; };
            this.progressBar1.Invoke(dele, new object[] { args.Progress });
        }

        private void EndAsyncHandler(IAsyncResult result)
        {
            object state = result.AsyncState;
            if (state != null)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Unexpected Error", (Exception)state);
                dlg.ShowDialog();
            }
            this.DialogResult = DialogResult.Cancel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Service != null)
            {
                Service.Abort();
            }
        }
    }
}
