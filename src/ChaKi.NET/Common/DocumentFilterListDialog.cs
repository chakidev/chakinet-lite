using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Annotation;

namespace ChaKi.GUICommon
{
    public partial class DocumentFilterListDialog : Form
    {
        public string SelectedValue;

        public DocumentFilterListDialog()
        {
            this.SelectedValue = string.Empty;
            InitializeComponent();
        }

        public void LoadTags()
        {
            this.listBox1.Items.Clear();
        }

        private void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            this.SelectedValue = (string)(this.listBox1.SelectedItem);
            this.DialogResult = DialogResult.OK;
        }
    }
}
