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

namespace ChaKi.Panels
{
    /// <summary>
    /// �^�u�̎�ʁi�^�u��index�ƈ�v�����邱�Ɓj
    /// </summary>
    public enum ConditionsPanelType
    {
        CP_CORPUS = -1,
        CP_FILTER = 0,
        CP_STRING = 1,
        CP_TAG = 2,
        CP_DEP = 3,
        CP_COLLOCATION = 4,
        CP_DOCUMENTSANALYSIS = 5,
        CP_MINING = 6,
    }

    public partial class ConditionsPanel : Panel
    {
        public EventHandler TabChanged;

        public CorpusPane CorpusPane { get; private set; }

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

            CorpusPane = new CorpusPane(model.SentenceCond) { Dock = DockStyle.Fill };

            var panes = new List<Control>();
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

            filterPane.Dock = DockStyle.Fill;
            stringSearchPane.Dock = DockStyle.Fill;
            tagSearchPane.Dock = DockStyle.Fill;
            depSearchPane.Dock = DockStyle.Fill;
            collocationPane.Dock = DockStyle.Fill;
            documentsAnalysisPane.Dock = DockStyle.Fill;
            miningPane.Dock = DockStyle.Fill;

            this.filterTab.Controls.Add(filterPane);
            this.stringSearchTab.Controls.Add(stringSearchPane);
            this.tagSearchTab.Controls.Add(tagSearchPane);
            this.depSearchTab.Controls.Add(depSearchPane);
            this.collocationTab.Controls.Add(collocationPane);
            //this.documentsAnalysisTab.Controls.Add(documentsAnalysisPane);
            this.addinTab.Controls.Add(miningPane);
        }

        /// <summary>
        /// SearchConditions���Z�b�g����B
        /// SetConditions()�ƈقȂ�A�^����I�u�W�F�N�g���R�s�[�����B
        /// �q�X�g������������e���Z�b�g����ꍇ�ɗp����
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public SearchConditions ChangeConditions(SearchConditions model)
        {
            m_Cond = new SearchConditions( model );  // ���̎��_�ŃR�s�[���쐬
            this.CorpusPane.SetCondition(m_Cond.SentenceCond);
            this.filterPane.SetCondition(m_Cond.FilterCond);
            this.stringSearchPane.SetCondition(m_Cond.StringCond);
            this.tagSearchPane.SetCondition(m_Cond.TagCond);
            this.depSearchPane.SetCondition(m_Cond.DepCond);
            this.collocationPane.SetCondition(m_Cond.CollCond);
            this.documentsAnalysisPane.SetCondition(m_Cond.DocumentsAnalysisCond);
            this.miningPane.SetCondition(m_Cond.MiningCond);
            return m_Cond;
        }

        /// <summary>
        /// �����I�u�W�F�N�g���R�s�[���ĕԂ��B
        /// </summary>
        /// <returns></returns>
        public SearchConditions CreateConditions()
        {
            SearchConditions obj = new SearchConditions(this.GetConditions());
            // �J�����g�^�u�ɂ��A�J�����g������ʂ�ݒ�
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
            //2021.1.10: ����obj�̎Q�Ƃ�history�ɕێ������. 
            // View���ێ�����Condition�C���X�^���X�����s�ŕύX����ƁA
            // View�ւ̕ύX�i�����ύX�j����ɂ��history�̎������������ɕς���Ă��܂�.
            // history�̎��ׂ��C���X�^���X�́AView�������̂̃X�i�b�v�V���b�g(�R�s�[)�ł���̂�������.
            //SetConditions(obj);
            return obj;
        }

        /// <summary>
        /// ���݂̏����I�u�W�F�N�g���Q�Ƃɂ��Ԃ��B
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
            m_Cond.MiningCond = this.miningPane.GetCondition();
            return m_Cond;
        }

        /// <summary>
        /// SearchConditions���Z�b�g����B
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
            this.miningPane.SetCondition(m_Cond.MiningCond);

            // Model Update�������I�ɍs��Ȃ��T�u�p�l���̂�UpdateView()�𒼐ڌĂяo��.
            this.stringSearchPane.UpdateView();
            this.collocationPane.UpdateView();
        }

        public void ResetConditions()
        {
            m_Cond.Reset();
            // Model Update�������I�ɍs��Ȃ��T�u�p�l���̂�UpdateView()�𒼐ڌĂяo��.
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

        private void tabControl1_SelectionChanged(TabControl sender, TabPage oldPage, TabPage newPage)
        {
            if (TabChanged != null)
            {
                TabChanged(this, null);
            }
        }

        /// <summary>
        /// TagSearch�p�l����Center Word��addCond�������}�[�W����
        /// (Word Occurrence Search�Ō�̐��N���������鎞�Ɏg�p����B�j
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
        /// Filter��Ԃ𔽉f���郆�[�U�[�R���g���[��FilterButton�̃C���X�^���X�𓾂�
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
            return this.CorpusPane.CorpusList;
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
