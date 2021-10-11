using ChaKi.Entity.DocumentAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public class SearchConditions : ICloneable
    {
        /// <summary>
        /// ����Conditions��Sequence�̒��O��Conditions�Ƃ��������錟���̏W�����Z�q(None,And,Or)
        /// �������ʂ̉��Z�ɂ��ẮASearchConditionsSequence�N���X���Q��
        /// </summary>
        public SearchSequenceOperator Operator { get; set; }
        public SearchType ActiveSearch { get; set; }
        public SentenceSearchCondition SentenceCond { get; set; }
        public FilterCondition FilterCond { get; set; }
        public StringSearchCondition StringCond { get; set; }
        public TagSearchCondition TagCond { get; set; }
        public DepSearchCondition DepCond { get; set; }
        public CollocationCondition CollCond { get; set; }
        public MiningCondition MiningCond { get; set; }
        public SearchConditions Parent { get; set; }
        public DocumentsAnalysisCondition DocumentsAnalysisCond { get; set; }

        public SearchConditions()
        {
            this.Operator = SearchSequenceOperator.None;
            this.ActiveSearch = SearchType.Undefined;
            this.SentenceCond = new SentenceSearchCondition();
            this.FilterCond = new FilterCondition();
            this.StringCond = new StringSearchCondition();
            this.TagCond = new TagSearchCondition();
            this.TagCond.Reset();
            this.DepCond = new DepSearchCondition();
            this.CollCond = new CollocationCondition();
            this.DocumentsAnalysisCond = new DocumentsAnalysisCondition();
            this.Parent = null;
        }

        public SearchConditions(SearchConditions src)
        {
            this.Operator = src.Operator;
            this.ActiveSearch = src.ActiveSearch;
            this.SentenceCond = new SentenceSearchCondition(src.SentenceCond);
            this.FilterCond = new FilterCondition(src.FilterCond);
            this.StringCond = new StringSearchCondition(src.StringCond);
            this.TagCond = new TagSearchCondition(src.TagCond);
            this.DepCond = new DepSearchCondition(src.DepCond);
            this.CollCond = new CollocationCondition(src.CollCond);
            this.DocumentsAnalysisCond = new DocumentsAnalysisCondition(src.DocumentsAnalysisCond);
            this.Parent = src.Parent;
        }

        public void Reset()
        {
            this.Operator = SearchSequenceOperator.None;
            this.ActiveSearch = SearchType.Undefined;
//            this.CorpusCond.Reset();  // Corpus�^�u�̏����͕ێ�����B
            this.FilterCond.Reset();
            this.StringCond.Reset();
            this.TagCond.Reset();
            this.DepCond.Reset();
            this.CollCond.Reset();
            this.DocumentsAnalysisCond.Reset();
            this.Parent = null;
        }

        public object Clone()
        {
            SearchConditions obj = new SearchConditions(this);
            return obj;
        }
    }
}
