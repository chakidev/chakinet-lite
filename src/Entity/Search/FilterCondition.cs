using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public class FilterCondition : ISearchCondition, ICloneable
    {
        public bool AllEnabled { get; set; }
        public string DocumentFilterKey { get; set; }
        public string DocumentFilterValue { get; set; }

        public ResultsetFilter ResultsetFilter;

        // すべての検索対象Corpusで共通に用いられる、検索対象Project ID（デフォルトは0）
        // これにより、Word tableを各wordの属するProjectにより区切って検索することが可能となる.
        public int TargetProjectId { get; set; }

        public event EventHandler OnModelChanged;

        public FilterCondition()
        {
            Reset();
        }

        public FilterCondition(FilterCondition src)
        {
            this.AllEnabled = src.AllEnabled;
            this.DocumentFilterKey = src.DocumentFilterKey;
            this.DocumentFilterValue = src.DocumentFilterValue;
            this.ResultsetFilter = src.ResultsetFilter;
            this.OnModelChanged = src.OnModelChanged;
            this.TargetProjectId = src.TargetProjectId;
        }

        public object Clone()
        {
            return new FilterCondition(this);
        }

        public void Reset()
        {
            this.AllEnabled = false;
            this.DocumentFilterKey = "Bib_ID";
            this.DocumentFilterValue = string.Empty;
            this.ResultsetFilter.Reset();
            this.TargetProjectId = 0;

            if (OnModelChanged != null) OnModelChanged(this, null);
        }
    }
}
