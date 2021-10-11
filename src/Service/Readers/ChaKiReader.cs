using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;
using System.ComponentModel;

namespace ChaKi.Service.Readers
{
    public delegate bool ConfirmationCallback(string msg);

    /// <summary>
    /// Chaki形式でセーブされたファイルから検索結果(KWIC LIST)を復元するサービス.
    /// セーブ自体は単純なXmlSerializeによって行うので、ここには含まれない。
    /// </summary>
    public class ChaKiReader
    {
        public const string CURRENT_VERSION = "1";

        public event EventHandler<ProgressChangedEventArgs> Progress;
        public ConfirmationCallback ConfirmationCallback;

        private SearchHistory m_Hist;
        private Corpus m_CurrentCorpus;
        private ISession m_Session;
        private ISessionFactory m_Factory;


        public ChaKiReader()
        {
            m_Hist = null;
            m_CurrentCorpus = null;
            m_Session = null;
            m_Factory = null;
            ConfirmationCallback = null;
        }

        public SearchHistory Read(string filename)
        {
            SearchHistory hist = null;
            XmlReaderSettings settings = new XmlReaderSettings() { IgnoreWhitespace = true, IgnoreComments = true };

            try
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    // 最初にKwicItem部分を除いてSearchHistoryオブジェクトを復元する
                    using (XmlReader rdr = XmlReader.Create(fs, settings))
                    {
                        rdr.ReadToFollowing("Chaki");
                        string version = rdr.GetAttribute("version");
                        if (version != CURRENT_VERSION)
                        {
                            throw new Exception(string.Format("Unsupported Version: {0}", version));
                        }
                        rdr.Read();
                        XmlSerializer ser = new XmlSerializer(typeof(SearchHistory));
                        hist = (SearchHistory)ser.Deserialize(rdr);
                    }
                    if (hist.CondSeq.Count == 0) throw new Exception("Data has no SearchCondition.");
                    m_Hist = hist;

                    // もう一度ファイルの先頭に戻り、KwicItemを読み取る。
                    // ここからはDBアクセスが必要になる。
                    fs.Seek(0, SeekOrigin.Begin);
                    using (XmlReader rdr = XmlReader.Create(fs, settings))
                    {
                        rdr.ReadToFollowing("Records");
                        rdr.ReadToDescendant("KwicItem");
                        int i = 0;
                        int lastPercent = 0;
                        do
                        {
                            string txt = rdr.ReadString();
                            ReadKwicItem(hist.KwicList.Records[i], txt);
                            i++;
                            int percent = 100 * i / hist.KwicList.Records.Count;
                            if (percent != lastPercent && this.Progress != null)
                            {
                                Progress(this, new ProgressChangedEventArgs(percent, null));
                            }
                        } while (rdr.ReadToNextSibling("KwicItem"));
                    }
                }
                hist.FilePath = filename;
            }
            finally
            {
                CloseSession();
            }
            return hist;
        }

        public void ReadKwicItem(KwicItem ki, string data)
        {
            string[] fields = data.Split(new char[] { ',' });
            if (fields.Length < 11)
            {
                ReadKwicItemPreV2(ki, data);
                return;
            }
            ki.ID = Int32.Parse(fields[0]);
            ki.Crps = m_Hist.CondSeq[0].SentenceCond.Find(fields[1]);
            ki.SenID = Int32.Parse(fields[5]);
            ki.SenPos = Int32.Parse(fields[6]);
            if (OpenSession(ki.Crps))
            {
                string lid = ki.Crps.LexiconID;
                ISQLQuery sq1 = m_Session.CreateSQLQuery("SELECT * from corpus_attribute where name='LexiconID'")
                                .AddEntity(typeof(CorpusAttribute));
                CorpusAttribute attr = sq1.UniqueResult<CorpusAttribute>();
                if (attr == null || lid != attr.Value)
                {
                    if (ConfirmationCallback == null || 
                        !ConfirmationCallback(string.Format("Mismatching Lexicon ID in corpus {0}. Continue anyway?", ki.Crps.Name)))
                    {
                        throw new Exception("Lexicon ID Mismatch; Maybe corpus has been modified since this data was saved.");
                    }
                }
            }
            // Sentence ID からSentence-->Documentとオブジェクトを辿る
            ISQLQuery sq = m_Session.CreateSQLQuery(string.Format("SELECT * from sentence where id={0}", ki.SenID))
                            .AddEntity(typeof(Sentence));
            Sentence sen = sq.UniqueResult<Sentence>();
            ki.Document = sen.ParentDoc;
            int docid = Int32.Parse(fields[2]);
            if (sen.ParentDoc.ID != docid)
            {
                throw new Exception("Document ID Mismatch; Maybe corpus has been modified since this data was saved.");
            }

            ki.StartCharPos = Int32.Parse(fields[4]);
            ki.Checked = Boolean.Parse(fields[7]);

            SetLexemes(fields[8], ki.Crps, ki.Left, 0);
            SetLexemes(fields[9], ki.Crps, ki.Center, KwicWord.KWA_PIVOT);
            SetLexemes(fields[10], ki.Crps, ki.Right, 0);
        }

        public void ReadKwicItemPreV2(KwicItem ki, string data)
        {
            string[] fields = data.Split(new char[] { ',' });
            if (fields.Length != 10)
            {
                throw new Exception("Invalid field count while reading KwicItem.");
            }
            ki.ID = Int32.Parse(fields[0]);
            ki.Crps = m_Hist.CondSeq[0].SentenceCond.Find(fields[1]);
            ki.SenID = Int32.Parse(fields[5]);
            if (OpenSession(ki.Crps))
            {
                string lid = ki.Crps.LexiconID;
                ISQLQuery sq1 = m_Session.CreateSQLQuery("SELECT * from corpus_attribute where name='LexiconID'")
                                .AddEntity(typeof(CorpusAttribute));
                CorpusAttribute attr = sq1.UniqueResult<CorpusAttribute>();
                if (attr == null || lid != attr.Value)
                {
                    if (ConfirmationCallback == null ||
                        !ConfirmationCallback(string.Format("Mismatching Lexicon ID in corpus {0}. Continue anyway?", ki.Crps.Name)))
                    {
                        throw new Exception("Lexicon ID Mismatch; Maybe corpus has been modified since this data was saved.");
                    }
                }
            }
            // Sentence ID からSentence-->Documentとオブジェクトを辿る
            ISQLQuery sq = m_Session.CreateSQLQuery(string.Format("SELECT * from sentence where id={0}", ki.SenID))
                            .AddEntity(typeof(Sentence));
            Sentence sen = sq.UniqueResult<Sentence>();
            ki.Document = sen.ParentDoc;
            int docid = Int32.Parse(fields[2]);
            if (sen.ParentDoc.ID != docid)
            {
                throw new Exception("Document ID Mismatch; Maybe corpus has been modified since this data was saved.");
            }

            ki.StartCharPos = Int32.Parse(fields[4]);
            ki.Checked = Boolean.Parse(fields[6]);

            SetLexemes(fields[7], ki.Crps, ki.Left, 0);
            SetLexemes(fields[8], ki.Crps, ki.Center, KwicWord.KWA_PIVOT);
            SetLexemes(fields[9], ki.Crps, ki.Right, 0);
        }

        private void SetLexemes(string data, Corpus c, KwicPortion kp, int attr)
        {
            string[] lexids;
            lexids = data.Split(new char[] { ';' });
            foreach (string id in lexids)
            {
                if (id.Length > 0)
                {
                    int lexid = Int32.Parse(id);
                    Lexeme lex;
                    if (!c.Lex.TryGetLexeme(lexid, out lex))
                    {
                        ISQLQuery sq = m_Session.CreateSQLQuery(string.Format("SELECT * from lexeme where id={0}", lexid))
                                        .AddEntity(typeof(Lexeme));
                        lex = sq.UniqueResult<Lexeme>();
                        c.Lex.Add(lex);
                    }
                    kp.AddLexeme(lex, null, attr); //@TODO - load Word from .Chaki file.
                }
            }
        }

        private bool OpenSession(Corpus cps)
        {
            if (m_Session != null && cps == m_CurrentCorpus)
            {
                return false; // do nothing; continue to use current sesssion
            }

            CloseSession();

            if (!cps.Mutex.WaitOne(0, false))
            {
                throw new Exception("ChaKiReader: Could not lock the Corpus");
            }
            m_CurrentCorpus = cps;
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            DBService dbs = DBService.Create(cps.DBParam);
            NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
            using (m_Factory = cfg.BuildSessionFactory())
            {
                m_Session = m_Factory.OpenSession();
            }
            return true;
        }

        private void CloseSession()
        {
            if (m_Session != null)
            {
                m_Session.Close();
                m_Session = null;
            }
            if (m_CurrentCorpus != null)
            {
                m_CurrentCorpus.Mutex.ReleaseMutex();
                m_CurrentCorpus = null;
            }
        }
    }
}
