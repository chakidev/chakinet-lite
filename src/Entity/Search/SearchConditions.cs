using ChaKi.Entity.DocumentAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public class SearchConditions : ICloneable
    {
        /// <summary>
        /// このConditionsとSequenceの直前のConditionsとを結合する検索の集合演算子(None,And,Or)
        /// 検索結果の演算については、SearchConditionsSequenceクラスを参照
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
//            this.CorpusCond.Reset();  // Corpusタブの条件は保持する。
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
