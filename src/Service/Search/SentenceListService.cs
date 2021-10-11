using System.Collections;
using System.Data;
using System.Data.Common;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using NHibernate;
using System.Collections.Generic;
using System;
using ChaKi.Common.Settings;

namespace ChaKi.Service.Search
{
    public class SentenceListService : SearchServiceBase
    {
        private KwicList m_Model;

        public SentenceListService(SearchHistory hist, SearchHistory parent)
            : base(hist, parent)
        {
            m_Model = hist.KwicList;
        }

        /// <summary>
        /// コーパスごとのSentenceList本体
        /// </summary>
        /// <param name="c"></param>
        protected override void ExecuteSearchSession(Corpus c)
        {
            SearchConditions cond = m_CondSeq.Last;  //TODO: 絞り込み等ではCondSeqを順次検索しなければならない

            // Sentence Listのクエリを生成する
            string qstr = QueryBuilder.Instance.BuildSentenceListQuery(c, cond);
            IQuery query = m_Session.CreateQuery(qstr);
            IList<Sentence> queryResult = query.List<Sentence>();
            int totalCount = queryResult.Count;
            IEnumerable<Sentence> filteredResult = (cond.FilterCond.AllEnabled) ?
                cond.FilterCond.ResultsetFilter.CreateEnumerable<Sentence>(queryResult) : queryResult;

            // ここまでで検索は終了。

            // 検索されたSentenceに対して、文内容をKwicに変換・出力する
            int n = 0;
            m_Progress.SetRange(totalCount);

            foreach (Sentence sen in filteredResult)
            {
                KwicItem ki = new KwicItem(c, sen.ParentDoc, sen.ID, sen.StartChar, sen.EndChar, sen.Pos);

                // sen.Words 以下のアクセスは、Hibernateではなく直接SQLで行う（Maxパフォーマンスのため）
                List<int> lexids = new List<int>();
                List<int> wordids = new List<int>();
                IDbConnection conn = m_Session.Connection;
                IDbCommand cmd = conn.CreateCommand();
                //!SQL
                if (cond.FilterCond.TargetProjectId < 0)
                {
                    cmd.CommandText = string.Format("SELECT lexeme_id,id from word where sentence_id={0} ORDER BY position", sen.ID);
                }
                else
                {
                    cmd.CommandText = string.Format("SELECT lexeme_id,id from word where sentence_id={0} and project_id={1} ORDER BY position",
                        sen.ID, cond.FilterCond.TargetProjectId);
                }
                IDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    lexids.Add((int)rdr[0]);
                    wordids.Add((int)rdr[1]);
                }
                rdr.Close();

                for (int i = 0; i < lexids.Count; i++)
                {
                    var lexid = lexids[i];
                    // CorpusのLexiconへ問い合わせ＆追加
                    ISQLQuery sq;
                    Lexeme lex;
                    if (!c.Lex.TryGetLexeme(lexid, out lex))
                    {
                        //!SQL
                        sq = m_Session.CreateSQLQuery(string.Format("SELECT * from lexeme where id={0}", lexid))
                            .AddEntity(typeof(Lexeme));
                        lex = sq.UniqueResult<Lexeme>();
                        c.Lex.Add(lex);
                    }
                    Word word = null;
                    // WordをSELECTすると低速になるため、処理をオプションとする（デフォルトでは読まない）
                    // (WordをSELECTするのは、時間データをKWICに持たせるため. また、word-wordマッピングはHibernateでは定義していないので手動で読む.)
                    if (SearchSettings.Current.RetrieveExtraWordProperty)
                    {
                        // load all word properties
                        sq = m_Session.CreateSQLQuery(string.Format("SELECT * from word where id={0}", wordids[i]))
                            .AddEntity(typeof(Word));
                        word = sq.UniqueResult<Word>();
                        // load word-word mapping
                        QueryWordMappings(word);

                        // Wordへの参照をKwicListに入れないと、Copy操作でWord.ExtraCharを参照できなくなるが、ここは速度を取る。
                        if (!word.StartTime.HasValue && word.MappedTo.IsEmpty)
                        {
                            ki.Right.AddLexeme(lex, null, 0);
                        }
                        else
                        {
                            ki.Right.AddLexeme(lex, word, 0);
                        }
                    }
                    else
                    {
                        ki.Right.AddLexeme(lex, null, 0);
                    }
                }
                m_Model.AddKwicItem(ki);
                m_Progress.Increment();
                n++;
            }
        }
    }
}
