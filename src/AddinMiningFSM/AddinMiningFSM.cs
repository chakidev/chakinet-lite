using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.AddIn.Pipeline;
using System.AddIn;
using System.Windows.Forms;
using ChaKi.Addins.AddinViews;
using System.IO;

namespace ChaKi.Addin.Mining.FSM
{
    [AddIn("FSM",
        Version = "1.0.0.0",
        Description = "Frequent Sequence Mining",
        Publisher = "Toshio Morita")]
    public class AddinMiningFSM : ChaKiAddinView
    {
        public override void Begin()
        {
            FSMDialog dlg = new FSMDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ChaKi.Common.Widgets.MessageBox.Show("Sorry, not implemented yet.");
            }
        }
    }
}
