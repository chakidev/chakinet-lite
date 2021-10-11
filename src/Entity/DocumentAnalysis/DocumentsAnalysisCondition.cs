using ChaKi.Entity.Corpora;
using ChaKi.Entity.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Entity.DocumentAnalysis
{
    public class DocumentsAnalysisCondition : ISearchCondition, ICloneable
    {
        public DocumentsAnalysisTypes AnalysisType { get; set; }
        public int N { get; set; }
        public LexemeFilter Filter { get; set; }

        public event EventHandler ModelChanged;

        public DocumentsAnalysisCondition()
        {
            Reset();
        }

        public DocumentsAnalysisCondition(DocumentsAnalysisCondition org)
        {
            this.AnalysisType = org.AnalysisType;
            this.N = org.N;
            this.Filter = new LexemeFilter(org.Filter);
        }

        public void Reset()
        {
            this.AnalysisType = DocumentsAnalysisTypes.WordList;
            this.N = 2;
            this.Filter = new LexemeFilter();
        }

        public object Clone()
        {
            return new DocumentsAnalysisCondition(this);
        }
    }
}
