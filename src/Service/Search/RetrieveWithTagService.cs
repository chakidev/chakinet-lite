using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;
using System.Collections;
using Iesi.Collections;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Kwic;
using System.Data;

namespace ChaKi.Service.Search
{
    public class RetrieveWithTagService : IServiceCommand
    {
        private class KwicItemInfo
        {
            // KwicItemの再検索に必要な情報
            public Corpus cps;
            public int senId;
            public int cenOffset;
            // 以下はKwicItemを再生成するのに必要な情報
            public Document doc;
            public int startChar;
            public int endChar;
            public int senPos;

            public KwicItemInfo(KwicItem src)
            {
                cps = src.Crps;
                senId = src.SenID;
                cenOffset = src.GetCenterCharOffset();
                doc = src.Document;
                startChar = src.StartCharPos;
                endChar = src.EndCharPos;
                senPos = src.SenPos;
            }
        }

        public EventHandler Completed { get; set; }    // unused
        public EventHandler Aborted { get; set; }      // unused

        private KwicList m_KwicList;
        private List<KwicItemInfo> m_SrcList;

        /// <summary>
        /// LexemeTag付きでKWIC結果を再取得する.
        /// </summary>
        /// <param name="cps"></param>
        /// <param name="kwiclist">in/out: 終了後Tag付きKwicListに置き換わる</param>
        public RetrieveWithTagService(KwicList kwiclist)
        {
            m_KwicList = kwiclist;
            m_SrcList = new List<KwicItemInfo>();
            foreach (KwicItem ki in kwiclist.Records)
            {
                m_SrcList.Add(new KwicItemInfo(ki));
            }
        }

        public void Begin()
        {
            if (m_SrcList.Count == 0)
            {
                return;
            }
            m_KwicList.Records.Clear();

            Corpus currentCorpus = m_SrcList[0].cps;
            DBService dbs = DBService.Create(currentCorpus.DBParam);
            NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
            ISessionFactory factory = cfg.BuildSessionFactory();
            ISession session = factory.OpenSession();

            foreach (KwicItemInfo info in m_SrcList)
            {
                if (info.cps != currentCorpus)
                {
                    session.Close();
                    factory.Dispose();
                    currentCorpus = info.cps;
                    dbs = DBService.Create(currentCorpus.DBParam);
                    factory = dbs.GetConnection().BuildSessionFactory();
                    session = factory.OpenSession();
                }
                if (!currentCorpus.Mutex.WaitOne(10, false))
                {
                    throw new Exception("RetrieveWithTagService: Could not lock the Corpus");
                }
                try
                {
                    KwicItem ki = new KwicItem(currentCorpus, info.doc, info.senId, info.startChar, info.endChar, info.senPos);

                    // sen.Words 以下のアクセスは、Hibernateではなく直接SQLで行う（Maxパフォーマンスのため）
                    List<int> lexids = new List<int>();
                    List<int> wordids = new List<int>();
                    IDbConnection conn = session.Connection;
                    IDbCommand cmd = conn.CreateCommand();
                    //!SQL
                    cmd.CommandText = string.Format("SELECT lexeme_id,id from word where sentence_id={0} ORDER BY position", info.senId);
                    IDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        lexids.Add((int)rdr[0]);
                        wordids.Add((int)rdr[1]);
                    }
                    rdr.Close();

                    int currentCharPos = 0;
                    for (int i = 0; i < lexids.Count; i++)
                    {
                        var lexid = lexids[i];
                        // CorpusのLexiconへ問い合わせ＆追加
                        ISQLQuery sq;
                        Lexeme lex;
                        if (!currentCorpus.Lex.TryGetLexeme(lexid, out lex))
                        {
                            //!SQL
                            sq = session.CreateSQLQuery(string.Format("SELECT * from lexeme where id={0}", lexid))
                                .AddEntity(typeof(Lexeme));
                            lex = sq.UniqueResult<Lexeme>();
                            currentCorpus.Lex.Add(lex);
                        }
                        sq = session.CreateSQLQuery(string.Format("SELECT * from word where id={0}", wordids[i]))
                            .AddEntity(typeof(Word));
                        Word word = sq.UniqueResult<Word>();

                        currentCharPos += lex.CharLength;
                        if (currentCharPos <= info.cenOffset)
                        {
                            ki.Left.AddLexeme(lex, word, 0);
                        }
                        else if (ki.Center.Count == 0)
                        {
                            ki.Center.AddLexeme(lex, word, KwicWord.KWA_PIVOT);
                        }
                        else
                        {
                            ki.Right.AddLexeme(lex, word, 0);
                        }
                    }
                    m_KwicList.AddKwicItem(ki);
                }
                finally
                {
                    currentCorpus.Mutex.ReleaseMutex();
                }
            }
            session.Close();
            factory.Dispose();
        }
    }
}
