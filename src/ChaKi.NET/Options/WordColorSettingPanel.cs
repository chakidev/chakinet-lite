using System;
using System.Drawing;
using System.Windows.Forms;
using ChaKi.Entity.Settings;

namespace ChaKi.Options
{
    public partial class WordColorSettingPanel : UserControl
    {
        public WordColorSetting Data
        {
            get { return m_Data; }
            set { m_Data = value; FromData(); }
        } private WordColorSetting m_Data;


        public WordColorSettingPanel()
        {
            InitializeComponent();
            this.propertyBox1.CenterizedButtonVisible = false;
            this.propertyBox1.AttachTo(Program.MainForm);
            this.colorPicker1.Color = Color.Black;
            this.colorPicker2.Color = Color.Ivory;
            this.colorPicker1.ColorChanged += new EventHandler(OnColorChanged);
            this.colorPicker2.ColorChanged += new EventHandler(OnColorChanged);
        }

        private void FromData()
        {
            if (this.Data == null)
            {
                return;
            }
            this.checkBox1.Checked = this.Data.IsUsed;
            this.propertyBox1.SetModel(this.Data.MatchingRule);
            this.colorPicker1.Color = Color.FromArgb(this.Data.FgColor);
            this.colorPicker2.Color = Color.FromArgb(this.Data.BgColor);
            this.textBox1.Text = string.Format("{0}", this.Data.MinFrequency);
            this.textBox2.Text = string.Format("{0}", this.Data.MaxFrequency);

            UpdateSampleTextLabel();
        }

        public void UpdateData()
        {
            if (this.Data == null)
            {
                return;
            }
            this.Data.IsUsed = this.checkBox1.Checked;
            // PropertyBoxのDataは自動的に同期される.
            this.Data.FgColor = this.colorPicker1.Color.ToArgb();
            this.Data.BgColor = this.colorPicker2.Color.ToArgb();
            int val;
            if (Int32.TryParse(this.textBox1.Text, out val) == false || val < 0)
            {
                this.Data.MinFrequency = -1;
            }
            else
            {
                this.Data.MinFrequency = val;
            }
            if (Int32.TryParse(this.textBox2.Text, out val) == false || val < 0)
            {
                this.Data.MaxFrequency = -1;
            }
            else
            {
                this.Data.MaxFrequency = val;
            }
        }

        private void OnColorChanged(object o, EventArgs e)
        {
            UpdateSampleTextLabel();
        }

        private void UpdateSampleTextLabel()
        {
            this.labelSampleText.ForeColor = this.colorPicker1.Color;
            this.labelSampleText.BackColor = this.colorPicker2.Color;
        }

        // Reset時の動作
        private void button1_Click(object sender, EventArgs e)
        {
            if (this.Data == null)
            {
                return;
            }
            this.Data.Reset();
            FromData();
        }
    }
}
