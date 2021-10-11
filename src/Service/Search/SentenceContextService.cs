using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using NHibernate;
using ChaKi.Service.Database;
using System.Collections;

namespace ChaKi.Service.Search
{
    public class SentenceContextService : SearchServiceBase, IServiceCommand
    {
        private Corpus m_Corpus;
        private int m_SenNo;
        private uint m_Count;  // 前後に取得する文の数(-m_Count ～ +m_Count)
        private SentenceContext m_Result;

        /// <summary>
        /// Word間に無条件に空白を挿入するためのフラグ。
        /// 本来は空白が存在すればそれはDocument.Textに含まれているべきだが、
        /// 現在はChasen/Mecabベースの文書インポート時には空白挿入は行われないため、
        /// 英語等の文書を扱う場合にはオプションにてtrueにセットすることで疑似的に
        /// 空白を生成する。Document.Textをそのまま出力するのがあるべき形である。
        /// </summary>
        public bool UseSpacing { get; set; }

        public new EventHandler Completed { get; set; }    // unused
        public new EventHandler Aborted { get; set; }      // unused

        public SentenceContextService(SentenceContext context)
            : base(null, null)

        {
            this.UseSpacing = false;

            m_Corpus = context.Corpus;
            m_SenNo = context.CenterSentenceID;
            m_Count = context.ContextLineCount;
            m_Result = context;

            // 結果を消去
            m_Result.Clear();
        }

        public new void Begin()
        {
            if (!m_Corpus.Mutex.WaitOne(10, false))
            {
                throw new Exception("SentenceContextService: Could not lock the Corpus");
            }
            try
            {
                // Corpus(DB)の種類に合わせてConfigurationをセットアップする
                DBService dbs = DBService.Create(m_Corpus.DBParam);
                NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
                using (m_Factory = cfg.BuildSessionFactory())
                {
                    IList<Sentence> queryResult;
                    try
                    {
                        m_Session = m_Factory.OpenSession();

                        // Centerの文が属するDocumentを得る
                        Sentence centerSen = null;
                        try
                        {
                            centerSen = m_Session.CreateQuery(string.Format("from Sentence s where s.ID={0}", m_SenNo))
                                        .UniqueResult<Sentence>();
                            if (centerSen == null)
                            {
                                throw new InvalidOperationException("Bad sentence ID. Please try reloading KwicView.");
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("SentenceContextService: Sentence not found for ID={0}.\n{1}", m_SenNo, ex.Message), ex);
                        }

                        // Sentence Listのクエリを生成する
                        string qstr = QueryBuilder.Instance.BuildSentenceContextQuery(centerSen, (int)m_Count);

                        IQuery query = m_Session.CreateQuery(qstr);
                        queryResult = query.List<Sentence>();
                        // ここまでで検索は終了。

                        // 検索結果(=文脈を構成する文集合)に対して、各文をSentenceContextに変換・出力する
                        foreach (Sentence sen in queryResult)
                        {
                            if (sen.ID == m_SenNo)
                            {
                                m_Result.AddItem(m_Corpus, sen, this.UseSpacing, true);
                            }
                            else
                            {
                                m_Result.AddItem(m_Corpus, sen, this.UseSpacing, false);
                            }
                        }
                    }
                    finally
                    {
                        if (m_Session != null)
                        {
                            m_Session.Close();
                        }
                    }
                }
            }
            finally
            {
                m_Corpus.Mutex.ReleaseMutex();
            }
        }

        // dummy
        protected override void ExecuteSearchSession(Corpus c)
        {
        }
    }
}
