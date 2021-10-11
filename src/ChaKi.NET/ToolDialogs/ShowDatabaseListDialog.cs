using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChaKi
{
    public partial class ShowDatabaseListDialog : Form
    {
        public ShowDatabaseListDialog()
        {
            this.DatabaseName = "";
            InitializeComponent();
        }

        public string DatabaseName { get; private set; }

        public List<string> Data
        {
            set
            {
                foreach (string s in value) {
                    this.listBox1.Items.Add(s);
                }
            }
        }

        private void Done()
        {
            if (this.listBox1.SelectedIndex >= 0)
            {
                this.DatabaseName = (string)this.listBox1.SelectedItem;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Done();
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            Done();
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Done();
            }
        }
    }
}
