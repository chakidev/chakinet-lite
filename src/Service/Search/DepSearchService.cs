using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using NHibernate;
using System.Diagnostics;
using System.Threading;
using System.Data;
using ChaKi.Common.Settings;

namespace ChaKi.Service.Search
{
    public class DepSearchService : SearchServiceBase
    {
        private KwicList m_Model;
        private LexemeResultSet m_LexemeResultSet;  //中間結果としてのLexemeリストのリスト

        public DepSearchService(SearchHistory hist, SearchHistory parent)
            : base(hist, parent)
        {
            m_Model = hist.KwicList;
            m_LexemeResultSet = null;
        }

        /// <summary>
        /// コーパスごとのDepSearch本体
        /// </summary>
        /// <param name="c"></param>
        protected override void ExecuteSearchSession(Corpus c)
        {
            SearchConditions cond = m_CondSeq.Last;

            // Lexemeの検索条件文(HQL)を作成し、検索結果を得る
            m_LexemeResultSet = QueryBunsetsuLexemeResultSet(cond.DepCond);
            if (m_LexemeResultSet == null || m_LexemeResultSet.Count == 0)
            {
                m_Progress.SetRange(0);     // ヒットなし
                return;
            }

            // AND検索（絞り込み）の場合は、tagCondの親のKwicListのSentece集合を条件に加える。
            List<int> targetSentences = null;
            if (cond.Operator == SearchSequenceOperator.And)
            {
                if (cond.Parent != null && m_HistParent != null)
                {
                    targetSentences = m_HistParent.KwicList.MakeSentenceIDListOfCorpus(c);
                }
            }

            IList<Word> queryResult = new List<Word>();
            // まずSeg, Link条件を無視して、Lexeme条件のみからSentenceを絞る。
            string qstr = QueryBuilder.Instance.BuildDepSearchQuery1(m_LexemeResultSet, cond, targetSentences);
            IQuery query = m_Session.CreateQuery(qstr);
            IList<Sentence> initialResult = query.List<Sentence>();

#if true
            qstr = QueryBuilder.Instance.BuildDepSearchQuery(m_LexemeResultSet, cond, initialResult);
            query = m_Session.CreateQuery(qstr);
            queryResult = query.List<Word>();
#else
            // 次に各Sentence毎にSegmentを絞る
            int k = 0;
            Stopwatch sw1 = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            Stopwatch sw3 = new Stopwatch();
            Console.WriteLine("DepSearchService.ExecuteSearchSession: Sentence Count={0}", queryResult.Count);
            StringBuilder sb = new StringBuilder();
            foreach (Sentence sen in initialResult)
            {
                sw1.Start();
                // まず、そのSentenceに含まれるSegmentだけを抽出
                sb.Length = 0;
                // Segment->Sentence関係を使用するので、文節以外の一般のSegment検索は不可。
                sb.AppendFormat("from Segment s where s.Doc.ID = {0} and s.Sentence.ID = {1}", sen.ParentDoc.ID, sen.ID);
                query = m_Session.CreateQuery(sb.ToString());
                IList segmentResultSet = query.List();
                sw1.Stop();
                sw2.Start();
                // 次に、Segmentを絞った上でDepSearch条件により検索を行う
                qstr = QueryBuilder.Instance.BuildDepSearchQuery2(sen, segmentResultSet, m_LexemeResultSet, cond);
                query = m_Session.CreateQuery(qstr);
                IList<Word> aResult = query.List<Word>();
                query = null;
                sw2.Stop();
                sw3.Start();
                queryResult.AddRange(aResult);
                sw3.Stop();
                aResult = null;
//                m_Session.Clear();
//                GC.Collect(2, GCCollectionMode.Forced);
//                GC.Collect(2, GCCollectionMode.Forced);
//                GC.Collect(2, GCCollectionMode.Forced);
            }
            long t1 = sw1.ElapsedMilliseconds;
            long t2 = sw2.ElapsedMilliseconds;
            long t3 = sw3.ElapsedMilliseconds;
#endif
            IEnumerable<Word> filteredResult = (cond.FilterCond.AllEnabled) ?
                cond.FilterCond.ResultsetFilter.CreateEnumerable<Word>(queryResult) : queryResult;
            int totalCount = queryResult.Count;

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
                    var lexid = word.Lex.ID;
                    // CorpusのLexiconへ問い合わせ＆追加
                    Lexeme lex;
                    if (!c.Lex.TryGetLexeme(lexid, out lex))
                    {
                        //!SQL
                        ISQLQuery sq = m_Session.CreateSQLQuery(string.Format("SELECT * from lexeme where id={0}", lexid))
                            .AddEntity(typeof(Lexeme));
                        lex = sq.UniqueResult<Lexeme>();
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
