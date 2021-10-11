using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;
using System.Collections;
using Iesi.Collections;
using ChaKi.Entity.Corpora.Annotations;

namespace ChaKi.Service.Search
{
    public class SentenceTagService : IDisposable
    {
        private Corpus m_Corpus;
        private int m_SenNo;
        private ISession m_Session = null;

        public Sentence Sen;

        public SentenceTagService(Corpus cps, int senNo)
        {
            m_Corpus = cps;
            m_SenNo = senNo;

            if (!m_Corpus.Mutex.WaitOne(10, false))
            {
                throw new Exception("SentenceTagService: Could not lock the Corpus");
            }

            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            ISessionFactory factory = null;
            try
            {
                var dbs = DBService.Create(m_Corpus.DBParam);
                NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
                factory = cfg.BuildSessionFactory();
                m_Session = factory.OpenSession();

                // Sentenceを取得するクエリ
                var qstr = QueryBuilder.Instance.BuildSentenceQuery(m_SenNo);
                var query = m_Session.CreateQuery(qstr);
                this.Sen = query.UniqueResult<Sentence>();
            }
            finally
            {
                if (factory != null) factory.Close();
            }
        }


        public void Dispose()
        {
            if (m_Session != null)
            {
                m_Session.Close();
                m_Session = null;
            }
            m_Corpus.Mutex.ReleaseMutex();
        }
    }
}
