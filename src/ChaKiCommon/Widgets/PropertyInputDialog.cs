using System;
using System.Windows.Forms;

namespace ChaKi.Common.Widgets
{
    public partial class PropertyInputDialog : Form
    {
        private DpiAdjuster m_DpiAdjuster;

        public PropertyInputDialog()
        {
            InitializeComponent();

            this.toolTip1.SetToolTip(this.checkBox1, "Use RegExp");
            this.toolTip1.SetToolTip(this.checkBox2, "Case Sensitive");

            // High-dpi‘Î‰ž
            m_DpiAdjuster = new DpiAdjuster((xscale, yscale) => {
                this.checkBox1.Width = (int)((float)this.checkBox1.Width * xscale);
                this.checkBox1.Height = (int)((float)this.checkBox1.Height * yscale);
                this.checkBox2.Width = (int)((float)this.checkBox2.Width * xscale);
                this.checkBox2.Height = (int)((float)this.checkBox2.Height * yscale);
                this.checkBox2.Location = new System.Drawing.Point((int)(6 + 22*xscale), 5);
                this.textBox1.Location = new System.Drawing.Point((int)(6 + 47 * xscale), 5);
                this.textBox1.Width = (int)(53 + 163 * xscale);
                this.Width = (int)(276 * xscale);
                this.Height = (int)(this.Height / yscale);
            });
            this.Paint += (e, a) => m_DpiAdjuster.Adjust(a.Graphics);
        }

        public event PropertyInputDone OnInputDone;
        public event PropertyInputCancel OnInputCancel;

        public int Index { get; set; }

        public string EditText
        {
            get { return this.textBox1.Text; }
            set { this.textBox1.Text = value; }
        }

        public bool IsRegEx
        {
            get { return this.checkBox1.Checked; }
            set { this.checkBox1.Checked = value; }
        }

        public bool IsCaseSensitive
        {
            get { return this.checkBox2.Checked; }
            set { this.checkBox2.Checked = value; }
        }

        public void Done()
        {
            OnInputDone(this, new InputDoneEventArgs(this.Index, this.EditText, false, false));
        }

        private void PropertyInputDialog_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.textBox1.Focus();
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                Done();
            }
            else if (e.KeyChar == '\x1b')
            {
                OnInputCancel(this, e);
            }
        }
    }
}