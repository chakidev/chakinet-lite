using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace DependencyEdit
{
    public partial class TagLabel : Label
    {
        public event EventHandler TagChanged;

        public int Index { get; set; }

        public TagLabel(int id)
        {
            InitializeComponent();
            this.Index = id;
        }

        private void TagLabel_MouseUp(object sender, MouseEventArgs e)
        {
            this.contextMenuStrip1.Show(this.PointToScreen(e.Location));
        }

        private void OnTagChanged(object sender)
        {
            string s = ((ToolStripMenuItem)sender).Text;
            s = s.Replace("&", "");
            this.Text = s;
            if (this.TagChanged != null)
            {
                this.TagChanged(this, null);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OnTagChanged(sender);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            OnTagChanged(sender);
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            OnTagChanged(sender);
        }
    }
}
