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
using NHibernate.Transform;
using ChaKi.Service.Common;
using System.Linq;

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
            var qstr = QueryBuilder.Instance.BuildTagSearchQuerySQL(cond, targetSentences);
            // ここでは中心語のIDと、中心および周辺語のPositionリストを結果(object[])として得る。
            // CreateSQLQueryで問い合わせるとobject[2]以降が正しく得られないので、IDbCommandを直接たたく。
            //var query = m_Session.CreateSQLQuery(qstr);
            List<object[]> queryResult = new List<object[]>();
            var cmd = m_Session.Connection.CreateCommand();
            {
                cmd.CommandText = qstr;
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var arr = new object[rdr.FieldCount];
                    rdr.GetValues(arr);
                    queryResult.Add(arr);
                }
                rdr.Close();
            }
            int totalCount = queryResult.Count;
            var filteredResult = (cond.FilterCond.AllEnabled) ?
                cond.FilterCond.ResultsetFilter.CreateEnumerable<object[]>(queryResult) : queryResult;

            // ここまでで検索は終了。

            // 検索されたWordに対して、そのWordを持っているSentenceをたどり、
            // 文内容をKwicに変換・出力する
            int n = 0;
            m_Progress.SetRange(totalCount);
            foreach (var r in filteredResult)
            {
                // 先のクエリ結果の第1カラムが中心語のIDなので、それを用いてWordオブジェクトを取得。
                var centerWord = m_Session.Get<Word>((int)r[0]);
                var secondaryWordPos = r.Skip(1).ToArray(); // r[1]以降が検索に用いたwordのpositionリスト
//                Console.WriteLine($"{(int)r[1]}, {(int)r[2]}");
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

                    var kw_attr = 0; // 中心語・共起語であれあばこのフラグをセット
                    if (pos == position)
                    {
                        kw_attr = KwicWord.KWA_PIVOT;
                    }
                    else if (secondaryWordPos.Contains(pos))
                    {
                        kw_attr = KwicWord.KWA_SECOND;
                    }
                    if (pos < position)
                    {
                        ki.Left.AddLexeme(lex, word, kw_attr);
                    }
                    else if (pos == position)
                    {
                        ki.Center.AddLexeme(lex, word, kw_attr);
                    }
                    else
                    {
                        ki.Right.AddLexeme(lex, word, kw_attr);
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
