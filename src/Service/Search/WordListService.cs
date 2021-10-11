using ChaKi.Entity.Search;
using NHibernate;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Corpora;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ChaKi.Service.Search
{
    public class WordListService : SearchServiceBase
    {
        private LexemeCountList m_Model;
        private LexemeResultSet m_LexemeResultSet;  //中間結果としてのLexemeリストのリスト
        private SearchType m_SearchType;

        public WordListService(SearchHistory hist, SearchHistory parent,SearchType st)
            : base(hist, parent)
        {
            m_Model = hist.LexemeList;
            m_LexemeResultSet = null;
            m_SearchType = st;
        }

        protected override void ExecuteSearchSession(Corpus c)
        {
            SearchConditions cond = m_CondSeq.Last;

            IQuery query;
            if (m_SearchType == SearchType.TagWordList)
            {
                // Lexemeの検索条件文(HQL)を作成し、検索結果を得る
                m_LexemeResultSet = QueryLexemeResultSet(m_CondSeq.Last.TagCond.LexemeConds, true);
                if (m_LexemeResultSet == null || m_LexemeResultSet.Count == 0)
                {
                    m_Progress.SetRange(0);     // ヒットなし
                    return;
                }

                // Wordの検索条件文(HQL)を作成し、検索を実行する。
                string qstr = QueryBuilder.Instance.BuildWordListQuery(m_LexemeResultSet, cond, null); // WordListは当面単一検索のみ扱うので第3引数はnullとする
                query = m_Session.CreateQuery(qstr);
            }
            else if (m_SearchType == SearchType.DepWordList)
            {
                // Lexemeの検索条件文(HQL)を作成し、検索結果を得る
                m_LexemeResultSet = QueryBunsetsuLexemeResultSet(m_CondSeq.Last.DepCond);
                if (m_LexemeResultSet == null || m_LexemeResultSet.Count == 0)
                {
                    m_Progress.SetRange(0);     // ヒットなし
                    return;
                }

                IList<Word> finalResult = new List<Word>();
                // まずSeg, Link条件を無視して、Lexeme条件のみからSentenceを絞る。
                string qstr = QueryBuilder.Instance.BuildDepSearchQuery1(m_LexemeResultSet, cond, null);
                query = m_Session.CreateQuery(qstr);
                IList<Sentence> sentences = query.List<Sentence>();
                // 絞った後の正式なクエリ
                qstr = QueryBuilder.Instance.BuildDepWordListQuery(m_LexemeResultSet, cond, sentences);
                query = m_Session.CreateQuery(qstr);
            }
            else
            {
                throw new Exception("WordList Service - Invalid SearchType.");
            }
            IList<object[]> queryResult = query.List<object[]>();
            int totalCount = queryResult.Count;

            // ここまでで検索は終了。

            // LexemeFilderの効果をResetする
            ((LexemeListFilter)(m_Model.Comparer)).Reset();

            // 検索されたWordListをLexemeCountListに変換・出力する
            m_Model.LexSize = m_LexemeResultSet.Count;
            int n = 0;
            m_Progress.SetRange(totalCount);
            foreach (object[] result in queryResult)
            {
                long count = (long)result[0];
                LexemeList lexList = new LexemeList();
                for (int i = 1; i < result.Length; i++)
                {
                    lexList.Add((Lexeme)result[i]);
                }
                m_Model.Add(lexList, c, (int)count);
                m_Progress.Increment();
                n++;
            }
        }
    }
}
