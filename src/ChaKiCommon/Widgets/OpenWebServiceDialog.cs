using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using MessageBox = ChaKi.Common.Widgets.MessageBox;

namespace ChaKi.Common.Widgets
{
    public partial class OpenWebServiceDialog : Form
    {
        static OpenWebServiceDialog()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        }

        public OpenWebServiceDialog()
        {
            InitializeComponent();
        }

        public string Url
        {
            get { return this.textBox1.Text; }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                var client = new WebClient();
                var res = client.DownloadData(this.Url + "hello");
                MessageBox.Show(string.Format("Response: {0}", Encoding.UTF8.GetString(res)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error: {0}", ex));
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            var txt = this.textBox1.Text;
            if (txt.Length > 0 && !txt.EndsWith("/"))
            {
                txt += "/";
            }
            if (txt.Length > 0 && !txt.StartsWith("http"))
            {
                txt = "http://" + txt;
            }
            this.textBox1.Text = txt;
        }
    }
}
