using ChaKi.Entity.Corpora;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Panels
{
    public class ProjectSelector : ToolStripComboBox
    {
        public ProjectSelector()
        {
            ChaKiModel.OnCurrentChanged += CurrentCorpusChangedHandler;
        }

        private void CurrentCorpusChangedHandler(Corpus cps, int senid)
        {
            this.Items.Clear();
            if (cps != null && cps.DocumentSet != null)
            {
                this.Items.AddRange((from p in cps.DocumentSet.Projects select p.ID.ToString()).ToArray());
            }
        }
    }
}
