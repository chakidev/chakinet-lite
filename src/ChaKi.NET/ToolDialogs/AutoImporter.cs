using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ChaKi.ToolDialogs
{
    public partial class AutoImporter : Form
    {
        public AutoImporter()
        {
            InitializeComponent();

        }

        public void AddInpautFilePath(string path)
        {
            this.bindingSource1.Add(path);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var files = dlg.FileNames;
                foreach (var file in files)
                {
                    this.bindingSource1.Add(file);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var path = dlg.SelectedPath;
                foreach (var file in Directory.GetFiles(path))
                {
                    this.bindingSource1.Add(file);
                }
            }
        }
    }
}
