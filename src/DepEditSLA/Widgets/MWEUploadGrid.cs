using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.GUICommon;
using ChaKi.Service.DependencyEdit;
using ChaKi.Service.Lexicons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DependencyEditSLA.Widgets
{
    /// <summary>
    /// Cradle辞書のAPIを用いてwords listとマッチするMWEを検索する.
    /// 結果はGridに表示し、OKが押されたらSentenceStructure（呼び出し側）で MWE Groupを作成する.
    /// </summary>
    public partial class MWEUploadGrid : Form
    {
        private Corpus m_Corpus;
        private ILexiconService m_LexiconService;
        private IDepEditService m_DepEditService;

        public IList<Word> Words { get; set; }
        public IList<Group> MWEGroups { get; set; }
        // Sentenceに含まれるMWE Groupから生成した（Cradle登録のための）MWEリスト用Grid　(ChaKi -> Cradle)
        public DataTable Table { get; set; }
        public List<Tuple<MWE, List<int>, List<int>>> Results { get; set; }

        public MWEUploadGrid(Corpus cps, ILexiconService lexsvc, IDepEditService depsvc)
        {
            m_LexiconService = lexsvc;
            m_DepEditService = depsvc;
            m_Corpus = cps;
            this.Results = new List<Tuple<MWE, List<int>, List<int>>>();
            this.Table = new DataTable();
            this.Table.Columns.Add("Apply", typeof(bool));
            this.Table.Columns.Add("Surface", typeof(string));
            this.Table.Columns.Add("WordPositions", typeof(string));
            this.Table.Columns.Add("Dependencies", typeof(string));

            InitializeComponent();

            var dg = this.dataGridView2;
            dg.DataSource = this.Table;
        }

        private void MWESelectionGrid_Load(object sender, EventArgs e)
        {
            if (this.Words == null)
            {
                return;
            }

            // Auto Detectionを自動開始
            Task.Factory.StartNew(() =>
            {
                try
                {
                    BeginInvoke(new Action(() => { this.Table.Clear(); }));
                    m_DepEditService.FindMWEAnnotations(this.AddCandidate);
                    UpdateMessage("Done.");
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() =>
                    {
                        var edlg = new ErrorReportDialog("Error while detecting:", ex);
                        edlg.ShowDialog();
                    }));
                }
            });
        }

        private void UpdateMessage(string msg)
        {
            BeginInvoke(new Action(() => { this.textBox1.Text = msg; }));
        }

        private void AddCandidate(MWE mwe, List<int> wposlist, List<int> deplist)
        {
            BeginInvoke(new Action(() => {
                this.Table.Rows.Add(true, mwe.ToString(), string.Join(",", wposlist), string.Join(",", deplist));
                this.Results.Add(new Tuple<MWE, List<int>, List<int>>(mwe, wposlist, deplist));
            }));
        }

    }
}
