using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Views.KwicView
{
    public partial class KwicViewSettingDialog : Form
    {
        public KwicViewPanel View { get; set; }

        public KwicViewSettingDialog()
        {
            InitializeComponent();
        }

        private void KwicViewSettingDialog_Load(object sender, EventArgs e)
        {
            this.checkBox1.Checked = KwicView.Settings.ShowSegments;
            this.checkBox2.Checked = KwicView.Settings.ShowLinks;
            this.checkBox3.Checked = KwicView.Settings.ShowGroups;
            this.trackBar1.Value = KwicView.Settings.LineMargin;
            this.trackBar2.Value = KwicView.Settings.WordMargin;
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            KwicView.Settings.LineMargin = this.trackBar1.Value;
            this.View.CalculateLineHeight();
            this.View.RecalcLayout();
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            KwicView.Settings.WordMargin = this.trackBar2.Value;
            this.View.RecalcLayout();
        }

        private void KwicViewSettingDialog_Leave(object sender, EventArgs e)
        {
            this.button1.PerformClick();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            KwicView.Settings.ShowSegments = this.checkBox1.Checked;
            this.View.Invalidate();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            KwicView.Settings.ShowLinks = this.checkBox2.Checked;
            this.View.Invalidate();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            KwicView.Settings.ShowGroups = this.checkBox3.Checked;
            this.View.Invalidate();
        }
    }
}
