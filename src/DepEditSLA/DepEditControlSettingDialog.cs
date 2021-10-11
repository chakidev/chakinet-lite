using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Common;
using ChaKi.Common.Settings;

namespace DependencyEditSLA
{
    public partial class DepEditControlSettingDialog : Form
    {
        public SentenceStructure View { get; set; }

        public DepEditControlSettingDialog()
        {
            InitializeComponent();

            DepEditSettings settings = DepEditSettings.Current;
            this.trackBar1.Value = settings.BunsetsuBoxMargin;
            this.trackBar7.Value = settings.WordBoxMargin;
            this.trackBar2.Value = settings.TopMargin;
            this.trackBar3.Value = settings.LeftMargin;
            this.trackBar4.Value = settings.LineMargin;
            this.trackBar5.Value = (int)(settings.CurveParamX * 100);
            this.trackBar6.Value = (int)(settings.CurveParamY * 100);
            this.trackBar8.Value = (int)(settings.SegmentBoxLevelMarginX);
            this.trackBar9.Value = (int)(settings.SegmentBoxLevelMarginY);

            this.checkBox1.Checked = settings.ShowHeadInfo;
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.BunsetsuBoxMargin = this.trackBar1.Value;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.TopMargin = this.trackBar2.Value;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void trackBar3_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.LeftMargin = this.trackBar3.Value;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void trackBar4_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.LineMargin = this.trackBar4.Value;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void trackBar5_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.CurveParamX = (double)this.trackBar5.Value/100.0;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void trackBar6_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.CurveParamY = (double)this.trackBar6.Value / 100.0;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void trackBar7_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.WordBoxMargin = this.trackBar7.Value;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.ShowHeadInfo = this.checkBox1.Checked;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void trackBar8_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.SegmentBoxLevelMarginX = (double)this.trackBar8.Value;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }

        private void trackBar9_ValueChanged(object sender, EventArgs e)
        {
            DepEditSettings.Current.SegmentBoxLevelMarginY = (double)this.trackBar9.Value;
            if (this.View != null)
            {
                this.View.RecalcLayout();
                this.View.Refresh();
            }
        }
    }
}
