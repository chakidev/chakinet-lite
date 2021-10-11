using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.ToolDialogs
{
    public partial class LoadPresetScriptsDialog : Form
    {
        public string Selection { get; private set; }

        public string SelectionType { get; private set; }

        public LoadPresetScriptsDialog()
        {
            InitializeComponent();

            PopulateItems();

            this.button1.Enabled = false;
            this.listView1.ItemSelectionChanged += ListView1_ItemSelectionChanged;
            this.FormClosing += LoadPresetScriptsDialog_FormClosing;
        }

        private void LoadPresetScriptsDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                var sel = this.listView1.SelectedItems;
                if (sel.Count == 1)
                {
                    this.Selection = Path.Combine(Program.ProgramDir, "Scripts", sel[0].Text);
                    this.SelectionType = Path.GetExtension(sel[0].Text).ToUpper();
                }
                else
                {
                    this.Selection = null;
                    this.SelectionType = null;
                }
            }
        }

        private void ListView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            this.button1.Enabled = (this.listView1.SelectedItems.Count > 0);
        }

        void PopulateItems()
        {
            var path = Path.Combine(Program.ProgramDir, "Scripts");
            var lst = Directory.EnumerateFiles(path, "*.rb");
            foreach (var item in lst)
            {
                this.listView1.Items.Add(Path.GetFileName(item));
            }
            lst = Directory.EnumerateFiles(path, "*.py");
            foreach (var item in lst)
            {
                this.listView1.Items.Add(Path.GetFileName(item));
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
