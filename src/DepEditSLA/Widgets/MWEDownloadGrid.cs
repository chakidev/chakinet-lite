using ChaKi.Common.SequenceMatcher;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.GUICommon;
using ChaKi.Service.DependencyEdit;
using ChaKi.Service.Lexicons;
using System;
using System.Collections.Generic;
using System.Data;
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
    public partial class MWEDownloadGrid : Form
    {
        private Corpus m_Corpus;
        private ILexiconService m_LexiconService;
        private IDepEditService m_DepEditService;

        public IList<Word> Words { get; set; }
        public IList<Group> MWEGroups { get; set; }
        // Cradleに登録済みで、Sentenceに合致する部分のあるMWEのリスト用Grid (Cradle -> ChaKi)
        public DataTable Table { get; set; }
        public List<Tuple<MWE, MatchingResult>> Results { get; set; }

        public event Func<MatchingResult[], bool[], bool[]> OnApplyMweMatches;

        public MWEDownloadGrid(Corpus cps, ILexiconService lexsvc, IDepEditService depsvc)
        {
            m_LexiconService = lexsvc;
            m_DepEditService = depsvc;
            m_Corpus = cps;
            this.Results = new List<Tuple<MWE, MatchingResult>>();
            this.Table = new DataTable();
            this.Table.Columns.Add("Apply", typeof(bool));
            this.Table.Columns.Add("Surface", typeof(string));
            this.Table.Columns.Add("WordPositions", typeof(string));

            InitializeComponent();

            this.dataGridView1.DataSource = this.Table;

            this.Load += MWEDownloadGrid_Load;
            this.FormClosed += MWEDownloadGrid_FormClosed;
        }

        private void MWEDownloadGrid_Load(object sender, EventArgs e)
        {
            var dg = this.dataGridView1;
            dg.CellValueChanged += dg_CellValueChanged;
            dg.CellMouseUp += dg_CellMouseUp;
        }

        private void MWEDownloadGrid_FormClosed(object sender, FormClosedEventArgs e)
        {
            var dg = this.dataGridView1;
            dg.CellValueChanged -= dg_CellValueChanged;
            dg.CellMouseUp -= dg_CellMouseUp;
        }

        private void dg_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                // mweリストを生成
                var mwes = from r in this.Results select r.Item2;
                // checkedリストを生成
                var checks = from r in this.Table.Rows.Cast<DataRow>() select (bool)r[0];

                var checks_update = this.OnApplyMweMatches(mwes.ToArray(), checks.ToArray());
                for (var i = 0; i < checks_update.Length; i++)
                {
                    this.Table.Rows[i][0] = checks_update[i];
                }
            }
        }

        private void dg_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex != -1)
            {
                this.dataGridView1.EndEdit();
            }
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
                    BeginInvoke(new Action(() => { this.Table.Clear(); this.Results.Clear(); }));
                    m_LexiconService.FindMWECandidates(this.Words, this.UpdateMessage, this.AddMWE);
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

        private void AddMWE(MWE mwe, MatchingResult match)
        {
            BeginInvoke(new Action(() => {
            this.Table.Rows.Add(
                false,
                MatchingResult.MWEToString(mwe, this.Words, match),
                string.Join(",", from r in match.RangeList where (r.Start >= 0 && r.End >= 0) select r.ToString()));
                this.Results.Add(new Tuple<MWE, MatchingResult>(mwe, match));
            }));
        }

    }
}
