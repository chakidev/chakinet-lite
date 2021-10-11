using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Settings;

namespace ChaKi.Options
{
    public partial class WordColorSettingDialog : Form
    {
        public Corpus Corpus {
            get { return m_Corpus; }
            set
            {
                if (value != null)
                {
                    m_Corpus = value;
                    this.textBox1.Text = m_Corpus.Name;
                }
            }
        } private Corpus m_Corpus;

        private WordColorSetting[] m_OriginalData;

        public WordColorSettingDialog()
        {
            InitializeComponent();
        }

        public WordColorSettingDialog(Corpus cps)
            : this()
        {
            this.Corpus = cps;
            WordColorSettings wcs = WordColorSettings.GetInstance();
            m_OriginalData = wcs.FindOrCreate(cps.Name);
            this.wordAttributeColorSettingPanel1.Data = (WordColorSetting)m_OriginalData[0].Clone();
            this.wordAttributeColorSettingPanel2.Data = (WordColorSetting)m_OriginalData[1].Clone();
            this.wordAttributeColorSettingPanel3.Data = (WordColorSetting)m_OriginalData[2].Clone();
        }


        /// <summary>
        /// 変更を反映する 
        /// </summary>
        public void UpdateModel()
        {
            this.wordAttributeColorSettingPanel1.UpdateData();
            this.wordAttributeColorSettingPanel2.UpdateData();
            this.wordAttributeColorSettingPanel3.UpdateData();
            m_OriginalData[0].CopyFrom(this.wordAttributeColorSettingPanel1.Data);
            m_OriginalData[1].CopyFrom(this.wordAttributeColorSettingPanel2.Data);
            m_OriginalData[2].CopyFrom(this.wordAttributeColorSettingPanel3.Data);
        }
    }
}
