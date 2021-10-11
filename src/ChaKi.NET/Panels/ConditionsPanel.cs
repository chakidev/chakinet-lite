using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Crownwood.DotNetMagic.Common;
using ChaKi.Entity.Search;
using ChaKi.Panels;
using ChaKi.Panels.ConditionsPanes;
using ChaKi.Entity.Corpora;

namespace ChaKi.Panels
{
    /// <summary>
    /// タブの種別（タブのindexと一致させること）
    /// </summary>
    public enum ConditionsPanelType
    {
        CP_CORPUS = 0,
        CP_FILTER = 1,
        CP_STRING = 2,
        CP_TAG = 3,
        CP_DEP = 4,
        CP_COLLOCATION = 5,
        CP_DOCUMENTSANALYSIS = 6,
        CP_MINING = 7,
    }

    public partial class ConditionsPanel : Form
    {
        public EventHandler TabChanged;

        private CorpusPane corpusPane;
        private FilterPane filterPane;
        private StringSearchPane stringSearchPane;
        private TagSearchPane tagSearchPane;
        private DepSearchPane depSearchPane;
        private CollocationPane collocationPane;
        private DocumentsAnalysisPane documentsAnalysisPane;
        private AddinPane miningPane;

        private SearchConditions m_Cond;

        public FilterPane FilterPane { get { return this.filterPane; } }

        public ConditionsPanel(SearchConditions model)
        {
            m_Cond = model;

            InitializeComponent();

            var panes = new List<Control>();

            panes.Add(corpusPane = new CorpusPane(model.SentenceCond));
            panes.Add(filterPane = new FilterPane(model.FilterCond, model.SentenceCond));
            panes.Add(stringSearchPane = new StringSearchPane(model.StringCond));
            panes.Add(tagSearchPane = new TagSearchPane(model.TagCond));
            panes.Add(depSearchPane = new DepSearchPane(model.DepCond));
            panes.Add(collocationPane = new CollocationPane(model.CollCond));
            panes.Add(documentsAnalysisPane = new DocumentsAnalysisPane(model.DocumentsAnalysisCond));
            panes.Add(miningPane = new AddinPane(model.MiningCond));

            foreach (var pane in panes)
            {
                pane.AllowDrop = true;
                pane.DragEnter += HandleDragEnter;
                pane.DragDrop += HandleDragDrop;
            }

            corpusPane.Dock = DockStyle.Fill;
            filterPane.Dock = DockStyle.Fill;
            stringSearchPane.Dock = DockStyle.Fill;
            tagSearchPane.Dock = DockStyle.Fill;
            depSearchPane.Dock = DockStyle.Fill;
            collocationPane.Dock = DockStyle.Fill;
            documentsAnalysisPane.Dock = DockStyle.Fill;
            miningPane.Dock = DockStyle.Fill;

            this.corpusTab.Controls.Add(corpusPane);
            this.filterTab.Controls.Add(filterPane);
            this.stringSearchTab.Controls.Add(stringSearchPane);
            this.tagSearchTab.Controls.Add(tagSearchPane);
            this.depSearchTab.Controls.Add(depSearchPane);
            this.collocationTab.Controls.Add(collocationPane);
            //this.documentsAnalysisTab.Controls.Add(documentsAnalysisPane);
            this.addinTab.Controls.Add(miningPane);
        }

        /// <summary>
        /// SearchConditionsをセットする。
        /// SetConditions()と異なり、与えるオブジェクトがコピーされる。
        /// ヒストリから条件内容をセットする場合に用いる
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public SearchConditions ChangeConditions(SearchConditions model)
        {
            m_Cond = new SearchConditions( model );  // この時点でコピーを作成
            this.corpusPane.SetCondition(m_Cond.SentenceCond);
            this.filterPane.SetCondition(m_Cond.FilterCond);
            this.stringSearchPane.SetCondition(m_Cond.StringCond);
            this.tagSearchPane.SetCondition(m_Cond.TagCond);
            this.depSearchPane.SetCondition(m_Cond.DepCond);
            this.collocationPane.SetCondition(m_Cond.CollCond);
            this.documentsAnalysisPane.SetCondition(m_Cond.DocumentsAnalysisCond);
            this.miningPane.SetCondition(m_Cond.MiningCond);
            return m_Cond;
        }

        public VisualStyle Style
        {
            set { this.tabControl1.Style = value; }
        }

        /// <summary>
        /// 条件オブジェクトをコピーして返す。
        /// </summary>
        /// <returns></returns>
        public SearchConditions CreateConditions()
        {
            SearchConditions obj = new SearchConditions(this.GetConditions());
            // カレントタブにより、カレント条件種別を設定
            switch (this.SelectedTab)
            {
                case ConditionsPanelType.CP_CORPUS:
                    obj.ActiveSearch = SearchType.SentenceSearch;
                    break;
                case ConditionsPanelType.CP_STRING:
                    obj.ActiveSearch = SearchType.StringSearch;
                    break;
                case ConditionsPanelType.CP_TAG:
                    obj.ActiveSearch = SearchType.TagSearch;
                    break;
                case ConditionsPanelType.CP_DEP:
                    obj.ActiveSearch = SearchType.DepSearch;
                    break;
            }
            //2021.1.10: このobjの参照がhistoryに保持される. 
            // Viewが保持するConditionインスタンスを次行で変更すると、
            // Viewへの変更（条件変更）操作によりhistoryの持つ条件も同時に変わってしまう.
            // historyの持つべきインスタンスは、Viewが持つもののスナップショット(コピー)であるのが正しい.
            //SetConditions(obj);
            return obj;
        }

        /// <summary>
        /// 現在の条件オブジェクトを参照により返す。
        /// </summary>
        /// <returns></returns>
        public SearchConditions GetConditions()
        {
            m_Cond.SentenceCond = this.corpusPane.GetCondition();
            m_Cond.FilterCond = this.filterPane.GetCondition();
            m_Cond.StringCond = this.stringSearchPane.GetCondition();
            m_Cond.TagCond = this.tagSearchPane.GetCondition();
            m_Cond.DepCond = this.depSearchPane.GetCondition();
            m_Cond.CollCond = this.collocationPane.GetCondition();
            m_Cond.DocumentsAnalysisCond = this.documentsAnalysisPane.GetCondition();
            m_Cond.MiningCond = this.miningPane.GetCondition();
            return m_Cond;
        }

        /// <summary>
        /// SearchConditionsをセットする。
        /// </summary>
        /// <param name="cond"></param>
        public void SetConditions(SearchConditions cond)
        {
            m_Cond = cond;
            this.corpusPane.SetCondition(m_Cond.SentenceCond);
            this.filterPane.SetCondition(m_Cond.FilterCond);
            this.stringSearchPane.SetCondition(m_Cond.StringCond);
            this.tagSearchPane.SetCondition(m_Cond.TagCond);
            this.depSearchPane.SetCondition(m_Cond.DepCond);
            this.collocationPane.SetCondition(m_Cond.CollCond);
            this.documentsAnalysisPane.SetCondition(m_Cond.DocumentsAnalysisCond);
            this.miningPane.SetCondition(m_Cond.MiningCond);

            // Model Updateを自動的に行わないサブパネルのみUpdateView()を直接呼び出す.
            this.stringSearchPane.UpdateView();
            this.collocationPane.UpdateView();
        }

        public void ResetConditions()
        {
            m_Cond.Reset();
            // Model Updateを自動的に行わないサブパネルのみUpdateView()を直接呼び出す.
            this.stringSearchPane.UpdateView();
            this.documentsAnalysisPane.UpdateView();
            this.collocationPane.UpdateView();
        }

        public int SelectedTabIndex
        {
            get
            {
                return this.tabControl1.SelectedIndex;
            }
            set
            {
                this.tabControl1.SelectedIndex = value;
            }
        }

        public ConditionsPanelType SelectedTab
        {
            get
            {
                return (ConditionsPanelType)this.SelectedTabIndex;
            }
            set
            {
                this.SelectedTabIndex = (int)value;
            }
        }

        private void tabControl1_SelectionChanged(Crownwood.DotNetMagic.Controls.TabControl sender, Crownwood.DotNetMagic.Controls.TabPage oldPage, Crownwood.DotNetMagic.Controls.TabPage newPage)
        {
            if (TabChanged != null)
            {
                TabChanged(this, null);
            }
        }

        /// <summary>
        /// TagSearchパネルのCenter WordにaddCond条件をマージする
        /// (Word Occurrence Searchで語の生起を検索する時に使用する。）
        /// </summary>
        /// <param name="addCond"></param>
        public void MergeTagSearchCondition(List<LexemeCondition> addCond)
        {
            this.tagSearchPane.MergeSearchCondition(addCond);
        }

        public void MergeDepSearchCondition(List<LexemeCondition> addCond)
        {
            this.depSearchPane.MergeSearchCondition(addCond);
        }

        /// <summary>
        /// Filter状態を反映するユーザーコントロールFilterButtonのインスタンスを得る
        /// </summary>
        /// <returns></returns>
        internal FilterButton GetFilterButton()
        {
            return this.filterPane.FilterButton;
        }

        public void PerformFilterAutoIncrement()
        {
            this.filterPane.PerformFilterAutoIncrement();
        }

        public List<Corpus> GetCorpusList()
        {
            return this.corpusPane.CorpusList;
        }

        public void AddCorpus(string file, bool clear)
        {
            this.corpusPane.AddCorpus(file, clear);
        }

        private void HandleDragEnter(object sender, DragEventArgs e)
        {
            var mainForm = Program.MainForm;
            if (mainForm != null)
            {
                mainForm.MainForm_DragEnter(sender, e);
            }
        }

        private void HandleDragDrop(object sender, DragEventArgs e)
        {
            var mainForm = Program.MainForm;
            if (mainForm != null)
            {
                mainForm.MainForm_DragDrop(sender, e);
            }
        }
    }
}
