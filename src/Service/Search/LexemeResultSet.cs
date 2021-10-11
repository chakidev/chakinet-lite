using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using ChaKi.Entity.Search;
using ChaKi.Service.Common;
using ChaKi.Entity.Corpora;

namespace ChaKi.Service.Search
{
    /// ひとつのPropertyBoxをその条件に従って検索した結果得られたLexemeのリストを格納するオブジェクト。
    /// 元になったBoxの番号と検索条件への参照を含む。
    /// 検索の途中結果として使用する。
    public class LexemeResult
    {
        public int No;
        public LexemeCondition Cond;
        public IList<Lexeme> LexemeList;
        // このLexemeが何番目のBunsetsu(Segment)に属するか(TagSearchの場合はすべて0, DepSearchの場合は0..n-1)
        public int BunsetsuNo;

        public string QString;  // query中のin()の括弧部分の文字列に変換したもの

        public string ViewName; // Viewとして保存される場合はそのViewの名称

        public LexemeResult(int no, LexemeCondition cond)
        {
            this.No = no;
            this.Cond = cond;
            this.LexemeList = null;
            this.BunsetsuNo = 0;
            this.QString = null;
            this.ViewName = null;
        }
        public LexemeResult(int no, LexemeCondition cond, IList<Lexeme> results)
            : this(no, cond)
        {
            this.LexemeList = results;
            if (results != null)
            {
                this.QString = Util.BuildLexemeIDList(results);
            }
        }
        public LexemeResult(int no, LexemeCondition cond, string viewName)
            : this(no, cond)
        {
            this.ViewName = viewName;
        }
    }

    internal class LexemeResultComparer : IComparer<LexemeResult>
    {
        public int Compare(LexemeResult x, LexemeResult y)
        {
            if (x.LexemeList == null)
            {
                if (y.LexemeList == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            if (y.LexemeList == null)
            {
                return 1;
            }
            int c1 = x.LexemeList.Count;
            int c2 = y.LexemeList.Count;
            return c1.CompareTo(c2);
        }
    }


    /// <summary>
    /// 複数のPropertyBoxそれぞれを検索した結果得られたLexemeリストを格納するためのオブジェクト。
    /// 検索の途中結果として使用する。
    /// </summary>
    public class LexemeResultSet : IEnumerable
    {
        private List<LexemeResult> m_Results;

        public LexemeResultSet()
        {
            m_Results = new List<LexemeResult>();
        }

        public LexemeResult GetResult(int i)
        {
            return m_Results[i];
        }

        public IEnumerator GetEnumerator()
        {
            return m_Results.GetEnumerator();
        }
        public int Count
        {
            get { return m_Results.Count; }
        }
        public void Clear()
        {
            m_Results.Clear();
        }

        public void Add(int i, LexemeCondition cond, IList<Lexeme> lexemes)
        {
            m_Results.Add( new LexemeResult( i, cond, lexemes ) );
        }

        public void Add(int i, LexemeCondition cond, string viewName)
        {
            m_Results.Add(new LexemeResult(i, cond, viewName));
        }

        public void Add(int i, LexemeCondition cond)
        {
            m_Results.Add(new LexemeResult(i, cond));
        }

        public void Sort()
        {
            LexemeResultComparer lrc = new LexemeResultComparer();
            m_Results.Sort(lrc);
        }
    }
}
