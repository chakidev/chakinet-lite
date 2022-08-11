using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Search;
using ChaKi.Panels;
using ChaKi.Panels.ConditionsPanes;
using ChaKi.Entity.Corpora;
using ChaKi.Common.Widgets;

namespace ChaKi.Panels
{
    /// <summary>
    /// タブの種別（タブのindexと一致させること）
    /// </summary>
    public enum ConditionsPanelType
    {
        CP_CORPUS = -1,
        CP_STRING = 0,
        CP_TAG = 1,
        CP_DEP = 2,
        CP_FILTER = 3,
        CP_COLLOCATION = 4,
        CP_DOCUMENTSANALYSIS = 5,
        CP_MINING = 6,
    }

    public partial class ConditionsPanel : Panel
    {
        public EventHandler TabChanged;

        public CorpusPane CorpusPane { get; private set; }

        public CollocationDialog CollocationDialog { get; private set; }

        private FilterPane filterPane;
        private StringSearchPane stringSearchPane;
        private TagSearchPane tagSearchPane;
        private DepSearchPane depSearchPane;
        private CollocationPane collocationPane;
        private DocumentsAnalysisPane documentsAnalysisPane;

        private SearchConditions m_Cond;

        public FilterPane FilterPane { get { return this.filterPane; } }

        public TabControl TabControl => this.tabControl1;

        private FlowLayoutPanel m_FlowLayoutPanel = new FlowLayoutPanel();

        private Button m_ResetAllSearchSettiongsButton = new Button() { Width = 120 };


        public ConditionsPanel(SearchConditions model)
        {
            m_Cond = model;

            InitializeComponent();

            CorpusPane = new CorpusPane(model.SentenceCond) { Dock = DockStyle.Fill };
            collocationPane = new CollocationPane(model.CollCond) { Dock = DockStyle.Fill };

            var panes = new List<Control>();
            panes.Add(stringSearchPane = new StringSearchPane(model.StringCond));
            panes.Add(tagSearchPane = new TagSearchPane(model.TagCond));
            panes.Add(depSearchPane = new DepSearchPane(model.DepCond));
            panes.Add(filterPane = new FilterPane(model.FilterCond, model.SentenceCond));
            panes.Add(documentsAnalysisPane = new DocumentsAnalysisPane(model.DocumentsAnalysisCond));

            foreach (var pane in panes)
            {
                pane.AllowDrop = true;
                pane.DragEnter += HandleDragEnter;
                pane.DragDrop += HandleDragDrop;
            }

            filterPane.Dock = DockStyle.Fill;
            stringSearchPane.Dock = DockStyle.Fill;
            tagSearchPane.Dock = DockStyle.Fill;
            depSearchPane.Dock = DockStyle.Fill;
            documentsAnalysisPane.Dock = DockStyle.Fill;

            this.filterTab.Controls.Add(filterPane);
            this.stringSearchTab.Controls.Add(stringSearchPane);
            this.tagSearchTab.Controls.Add(tagSearchPane);
            this.depSearchTab.Controls.Add(depSearchPane);
            //this.documentsAnalysisTab.Controls.Add(documentsAnalysisPane);
            //this.addinTab.Controls.Add(miningPane);

            // Collocation Paneをダイアログ化
            this.CollocationDialog = new CollocationDialog();
            this.CollocationDialog.Panel1.Controls.Add(this.collocationPane);

            // Resetボタン
            this.button1.Click += (o, e) => ResetConditions();
        }

        private DpiAdjuster m_DpiAdjuster;

        public void PrepareControlButtons(Button searchButton, Button wordListButton)
        {
            // CommandPanelからのボタンをFlowLayoutPanelに追加する
            m_DpiAdjuster = new DpiAdjuster((xscale, yscale) => {
                m_FlowLayoutPanel.BackColor = Color.Transparent;
                m_FlowLayoutPanel.Location = new Point((int)(10 * xscale), (int)(this.Height - 40 * yscale));
                m_FlowLayoutPanel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
                m_FlowLayoutPanel.Width = (int)(400 * xscale);
                m_FlowLayoutPanel.Height = (int)(34 * yscale);
                searchButton.Height = (int)(28 * yscale);
                searchButton.Width = (int)(120 * xscale);
                wordListButton.Height = (int)(28 * yscale);
                wordListButton.Width = (int)(120 * xscale);
                this.button1.Height = (int)(28 * yscale);
                this.button1.Width = (int)(100 * xscale);
                m_DpiAdjuster = null;
            });
            this.Paint += (e, a) => m_DpiAdjuster?.Adjust(a.Graphics);
            m_FlowLayoutPanel.Controls.Add(searchButton);
            m_FlowLayoutPanel.Controls.Add(wordListButton);
            m_FlowLayoutPanel.Controls.Add(this.button1);   // "Reset" button
            this.Controls.Add(m_FlowLayoutPanel);
            this.Controls.SetChildIndex(m_FlowLayoutPanel, 0);
            this.Controls.SetChildIndex(tabControl1, 1);
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
            this.CorpusPane.SetCondition(m_Cond.SentenceCond);
            this.filterPane.SetCondition(m_Cond.FilterCond);
            this.stringSearchPane.SetCondition(m_Cond.StringCond);
            this.tagSearchPane.SetCondition(m_Cond.TagCond);
            this.depSearchPane.SetCondition(m_Cond.DepCond);
            this.collocationPane.SetCondition(m_Cond.CollCond);
            this.documentsAnalysisPane.SetCondition(m_Cond.DocumentsAnalysisCond);
            return m_Cond;
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
                case ConditionsPanelType.CP_FILTER:
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
            m_Cond.SentenceCond = this.CorpusPane.GetCondition();
            m_Cond.FilterCond = this.filterPane.GetCondition();
            m_Cond.StringCond = this.stringSearchPane.GetCondition();
            m_Cond.TagCond = this.tagSearchPane.GetCondition();
            m_Cond.DepCond = this.depSearchPane.GetCondition();
            m_Cond.CollCond = this.collocationPane.GetCondition();
            m_Cond.DocumentsAnalysisCond = this.documentsAnalysisPane.GetCondition();
            return m_Cond;
        }

        /// <summary>
        /// SearchConditionsをセットする。
        /// </summary>
        /// <param name="cond"></param>
        public void SetConditions(SearchConditions cond)
        {
            m_Cond = cond;
            this.CorpusPane.SetCondition(m_Cond.SentenceCond);
            this.filterPane.SetCondition(m_Cond.FilterCond);
            this.stringSearchPane.SetCondition(m_Cond.StringCond);
            this.tagSearchPane.SetCondition(m_Cond.TagCond);
            this.depSearchPane.SetCondition(m_Cond.DepCond);
            this.collocationPane.SetCondition(m_Cond.CollCond);
            this.documentsAnalysisPane.SetCondition(m_Cond.DocumentsAnalysisCond);

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

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs args)
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

        public CorpusGroup GetCorpusGroup()
        {
            return this.CorpusPane.CorpusGroup;
        }

        public void AddCorpus(string file, bool clear)
        {
            this.CorpusPane.AddCorpus(file, clear);
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
