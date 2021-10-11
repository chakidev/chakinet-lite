using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.ToolDialogs
{
    public partial class CreateDictionary : Form
    {
        public CreateDictionary()
        {
            InitializeComponent();
            this.comboBox2.Items.Add("Auto");
            this.comboBox2.Items.Add("Mecab|Cabocha");
            this.comboBox2.Items.Add("Mecab|Cabocha|UniDic2");
            this.comboBox2.SelectedIndex = 0;
        }

        /// <summary>
        /// Launch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            string args = "-d";
            if (this.comboBox2.Text.Length > 0)
            {
                args += string.Format(" -t=\"{0}\" ", this.comboBox2.Text);
            }
            if (this.comboBox1.Text.Length > 0)
            {
                args += string.Format(" -e=\"{0}\"", this.comboBox1.Text);
            }
            if (this.checkBox1.Checked)
            {
                args += " -s";
            }
            args += string.Format(" \"{0}\" \"{1}\"", this.textBox4.Text, this.textBox2.Text);
            this.process1.StartInfo.Arguments = args;
            this.process1.StartInfo.WorkingDirectory = Program.ProgramDir;
            this.process1.Start();
        }

        /// <summary>
        /// Browse Source File
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Title = "Select Dictionary File";
            dlg.Filter = "Input files (*.dic)|*.dic|All files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox4.Text = dlg.FileName;
            }
        }

        /// <summary>
        /// Browse Output DB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = false;
            dlg.Title = "Select Output Dictionary File";
            dlg.Filter = "Dictionary files (*.ddb)|*.ddb|All files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.textBox2.Text = dlg.FileName;
            }
        }
    }
}
