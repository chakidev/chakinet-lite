using System.Windows.Forms;
using ChaKi.Common;
using ChaKi.Entity.Corpora;

namespace ChaKi.GUICommon
{
    public partial class ChakiStatusStrip : StatusStrip
    {
        public ChakiStatusStrip()
        {
            InitializeComponent();

            this.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.toolStripStatusLabel1,
                this.toolStripStatusLabel2});

            ChaKiModel.OnCurrentChanged += new CurrentChangedDelegate(this.CurrentChangedHandler);
        }

        void CurrentChangedHandler(Corpus cps, int senid)
        {
            if (cps != null)
            {
                SetCorpusName(cps.Name);
            }
            else
            {
                SetCorpusName(string.Empty);
            }
            SetSentenceNo(senid);
        }

        public void SetCorpusName(string name)
        {
            if (name.Length > 0)
            {
                this.toolStripStatusLabel1.Text = string.Format("CurrentCorpus=[{0}]", name);
            }
            else
            {
                this.toolStripStatusLabel1.Text = "CurrentCorpus=[ ]";
            }
        }

        public void SetSentenceNo(int senid)
        {
            if (senid >= 0)
            {
                this.toolStripStatusLabel2.Text = string.Format("SentenceID=[{0}]", senid);
            }
            else
            {
                this.toolStripStatusLabel2.Text = "SentenceID=[ ]";
            }
        }
    }
}
