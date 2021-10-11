using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DependencyEditSLA.Widgets
{
    internal partial class AbridgedRangePanel : UserControl
    {
        public int Index { get; private set; }
        public AbridgedRange Range { get; private set; }

        public new event EventHandler Click;

        public AbridgedRangePanel(int index, AbridgedRange range, string text)
        {
            InitializeComponent();

            this.Index = index;
            this.Range = range;
            this.toolTip1.SetToolTip(this.button1, text);

            this.button1.Click += new EventHandler(button1_Click);
        }

        void button1_Click(object sender, EventArgs e)
        {
            if (Click != null)
            {
                Click(this, e);
            }
        }
    }
}
