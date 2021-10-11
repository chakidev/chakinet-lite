using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DependencyEditSLA.Widgets
{
    internal partial class AbridgeButton : FunctionButton
    {
        public int AbridgedIndex { get; set; }
        public StringBuilder TooltipText { get; set; }

        public AbridgeButton(int index)
        {
            InitializeComponent();

            this.AbridgedIndex = index;
            this.TooltipText = new StringBuilder();
        }

        public void EnableTooltip()
        {
            ToolTip tip = new ToolTip();
            tip.SetToolTip(this, this.TooltipText.ToString());
        }
    }
}
