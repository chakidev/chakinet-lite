using System.Collections;
using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Search;
using NHibernate;
using NHibernate.Criterion;
using ChaKi.Entity.Kwic;
using System;
using System.Text;
using System.Diagnostics;
using System.Data;
using ChaKi.Common.Settings;

namespace ChaKi.Service.Search
{
    public class TagSearchService : SearchServiceBase
    {
        private KwicList m_Model;
        private LexemeResultSet m_LexemeResultSet;  //中間結果としてのLexemeリストのリスト

        public TagSearchService(SearchHistory hist, SearchHistory parent)
            : base(hist, parent)
        {
            m_Model = hist.KwicList;
            m_LexemeResultSet = null;
        }

        /// <summary>
        /// コーパスごとのTagSearch本体
        /// </summary>
        /// <param name="c">The c.</param>
        protected override void ExecuteSearchSession(Corpus c)
        {
            SearchConditions cond = m_CondSeq.Last;

            // AND検索（絞り込み）の場合は、tagCondの親のKwicListのSentece集合を条件に加える。
            List<int> targetSentences = null;
            if (cond.Operator == SearchSequenceOperator.And)
            {
                if (cond.Parent != null && m_HistParent != null)
                {
                    targetSentences = m_HistParent.KwicList.MakeSentenceIDListOfCorpus(c);
                }
            }

            // Wordの検索条件文(SQL)を作成し、検索を実行する。
            string qstr = QueryBuilder.Instance.BuildTagSearchQuerySQL(cond, targetSentences);
            IQuery query = m_Session.CreateSQLQuery(qstr).AddEntity(typeof(Word));
            IList<Word> queryResult = query.List<Word>();
            int totalCount = queryResult.Count;
            IEnumerable<Word> filteredResult = (cond.FilterCond.AllEnabled) ?
                cond.FilterCond.ResultsetFilter.CreateEnumerable<Word>(queryResult) : queryResult;

            // ここまでで検索は終了。

            // 検索されたWordに対して、そのWordを持っているSentenceをたどり、
            // 文内容をKwicに変換・出力する
            int n = 0;
            m_Progress.SetRange(totalCount);
            foreach (Word centerWord in filteredResult)
            {
                int position = centerWord.Pos;   // KWICのcenter wordとなる語の位置
                Sentence sen = centerWord.Sen;
                KwicItem ki = new KwicItem(c, sen.ParentDoc, sen.ID, sen.StartChar, sen.EndChar, sen.Pos);
                int pos = 0;

                // sen.Words 以下のアクセスは、Hibernateではなく直接SQLで行う（Maxパフォーマンスのため）
                List<int> lexids = new List<int>();
                //!SQL
                ISQLQuery wq;
                if (cond.FilterCond.TargetProjectId >= 0)
                {
                    wq = m_Session.CreateSQLQuery(string.Format("SELECT * from word where sentence_id={0} and project_id={1} ORDER BY position", 
                        sen.ID, cond.FilterCond.TargetProjectId))
                            .AddEntity(typeof(Word));
                }
                else
                {
                    wq = m_Session.CreateSQLQuery(string.Format("SELECT * from word where sentence_id={0} ORDER BY position", sen.ID))
                            .AddEntity(typeof(Word));
                }
                var wlist = wq.List<Word>();
                foreach (var word in wlist)
                {
                    int lexid = word.Lex.ID;
                    // CorpusのLexiconへ問い合わせ＆追加
                    Lexeme lex;
                    if (!c.Lex.TryGetLexeme(lexid, out lex))
                    {
                        //!SQL
                        ISQLQuery lq = m_Session.CreateSQLQuery(string.Format("SELECT * from lexeme where id={0}", lexid))
                            .AddEntity(typeof(Lexeme));
                        lex = lq.UniqueResult<Lexeme>();
                        c.Lex.Add(lex);
                    }

                    if (SearchSettings.Current.RetrieveExtraWordProperty)
                    {
                        QueryWordMappings(word);
                    }

                    if (pos < position)
                    {
                        ki.Left.AddLexeme(lex, word, 0);
                    }
                    else if (pos == position)
                    {
                        ki.Center.AddLexeme(lex, word, KwicWord.KWA_PIVOT);
                    }
                    else
                    {
                        ki.Right.AddLexeme(lex, word, 0);
                    }
                    pos++;
                }
                m_Model.AddKwicItem(ki);
                m_Progress.Increment();
                n++;
            }
        }
    }
}
