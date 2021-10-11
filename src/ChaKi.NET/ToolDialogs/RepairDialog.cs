using ChaKi.Service.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChaKi.ToolDialogs
{
    public partial class RepairDialog : Form
    {
        public DBService Service { get; set; }

        private  bool m_Processing = false;

        public RepairDialog()
        {
            InitializeComponent();

            this.Load += RepairDialog_Load;
            this.FormClosing += RepairDialog_FormClosing;
        }

        private void RepairDialog_Load(object sender, EventArgs e)
        {
            this.textBox1.Text = this.Service.DBParam?.Name;
            Task.Factory.StartNew(this.RepairTask);
        }

        private void RepairDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_Processing)
            {
                e.Cancel = true;
            }
        }

        private void RepairTask()
        {
            m_Processing = true;
            try
            {
                this.Service.RepairDb(AppendLog);
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => { AppendLog(ex.Message); }));
            }
            finally
            {
                m_Processing = false;
            }
        }

        private void AppendLog(string msg)
        {
            this.Invoke(new Action(() =>
            {
                this.textBox2.AppendText(msg);
            }));
        }
    }
}
