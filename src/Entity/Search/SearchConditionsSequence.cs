using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    /// <summary>
    /// SearchConditionsによる検索の結果に対して集合演算結果を得るための、複合検索条件を表すクラス。
    /// たとえば複合検索条件を、{ P1, ∩P2, ∪P3,... } と表し、Pnの単独検索結果をRnと表したとき、
    /// この複合検索条件により得られる検索結果は、
    /// (R1 ∩ R2) ∪ R3 のように、常に左結合したものとなる。
    /// </summary>
    public class SearchConditionsSequence : List<SearchConditions>
    {
        public SearchConditionsSequence()
        {
        }

        public SearchConditionsSequence(SearchConditions conds)
        {
            this.AddCond(conds);
        }

        /// <summary>
        /// Deep Copy Constructor
        /// </summary>
        /// <param name="src"></param>
        public SearchConditionsSequence(SearchConditionsSequence src)
        {
            foreach (SearchConditions cond in src)
            {
                this.AddCond(new SearchConditions(cond));
            }
        }

        public SearchConditions Last
        {
            get
            {
                return this[Count - 1];
            }
        }

        public void AddCond(SearchConditions cond)
        {
            if (this.Count > 0)
            {
                cond.Parent = this.Last;
            }
            Add(cond);
        }
    }
}
