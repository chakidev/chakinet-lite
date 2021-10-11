using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Service.Database;
using NHibernate;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Readers;
using ChaKi.Service.Common;
using System.Diagnostics;

namespace ChaKi.Service.Export
{
    public abstract class ExportServiceBase : IExportService
    {
        protected struct SeqIDTagPair { public int Seqid; public string Tag; }
        protected ISession m_Session;
        protected ISessionFactory m_Factory;
        protected TextWriter m_TextWriter;
        protected XmlWriter m_XmlWriter;
        protected Dictionary<string, SeqIDTagPair> m_DocumentTags;
        protected Dictionary<string, SeqIDTagPair> m_SentenceTags;
        protected HashSet<Namespace> m_Namespaces = new HashSet<Namespace>();
        protected ReaderDef m_Def;

        protected int m_ProjId = 0;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExportServiceBase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing) { /* do nothing by default */ }

        public void Export(List<KwicItem> items, int projid)
        {
            m_ProjId = projid;

            // 本体のExportに先だって、ドキュメントリストを作成・Exportする.
            CreateDocumentList(items);
            // SentenceTagリストを作成・Exportする
            CreateSentenceTagList(items);

            // 次に本体(KwicItem)をそれぞれExportする.
            Corpus currentCorpus = null;
            try
            {
                foreach (KwicItem ki in items)
                {
                    Corpus c = ki.Crps;
                    if (c != currentCorpus)
                    {
                        OpenCorpus(c);
                        currentCorpus = c;
                    }
                    ExportItem(ki);
                }
            }
            finally
            {
                Close();
            }
        }

        public void ExportCorpus(Corpus crps, int projid, ref bool cancelFlag, Action<int, object> progressCallback)
        {
            m_ProjId = projid;
            try
            {
                OpenCorpus(crps);

                // 本体のExportに先だって、ドキュメントリストを作成・Exportする.
                CreateDocumentList(crps);
                // SentenceTagリストを作成・Exportする
                CreateSentenceTagList(crps);

                // 次に本体(KwicItem)をそれぞれExportする.
                IQuery q = m_Session.CreateQuery("from Sentence order by ID asc");
                IList<Sentence> sentences = q.List<Sentence>();
                int count = sentences.Count;
                int n = 0;
                int last_percent = -1;
                foreach (Sentence sen in sentences)
                {
                    if (cancelFlag)
                    {
                        break;
                    }
                    ExportItem(crps, sen);
                    int percent = (int)((n + 1) * 100 / count);
                    if (progressCallback != null && n % 10 == 0)
                    {
                        progressCallback(percent, new int[2] { n, count });
                        last_percent = percent;
                    }
                    n++;

                    // for performance measurement
                    if (n % 100 == 10)
                    {
                        Trace.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                            (double)ExportServiceCabocha.t1 / 100,
                            (double)ExportServiceCabocha.t2 / 100,
                            (double)ExportServiceCabocha.t3 / 100,
                            (double)ExportServiceCabocha.t4 / 100,
                            (double)ExportServiceCabocha.t5 / 100,
                            (double)ExportServiceCabocha.t6 / 100));
                        ExportServiceCabocha.t1 = 0;
                        ExportServiceCabocha.t2 = 0;
                        ExportServiceCabocha.t3 = 0;
                        ExportServiceCabocha.t4 = 0;
                        ExportServiceCabocha.t5 = 0;
                        ExportServiceCabocha.t6 = 0;
                    }
                }
            }
            finally
            {
                Close();
                Util.Reset();  // 安全のため、高速化のためのキャッシュデータをクリアする.
            }
        }

        public void ExportDictionary(Dictionary_DB dict, ref bool cancelFlag, Action<int> progressCallback)
        {
            try
            {
                OpenDictionary(dict);

                IQuery q = m_Session.CreateQuery("from Lexeme order by ID asc");
                IList<Lexeme> lexemes = q.List<Lexeme>();
                int count = lexemes.Count;
                int n = 0;
                int last_percent = 50; // ここまででかなり時間を取るため 50%とする.
                progressCallback(last_percent);
                foreach (var lex in lexemes)
                {
                    if (cancelFlag)
                    {
                        break;
                    }
                    ExportLexeme(dict, lex);
                    int percent = 50 + (int)(((n + 1) * 100.0 / count)/2.0);
                    if (progressCallback != null && last_percent != percent)
                    {
                        progressCallback(percent);
                        last_percent = percent;
                    }
                    n++;
                }
            }
            finally
            {
                Close();
                Util.Reset();  // 安全のため、高速化のためのキャッシュデータをクリアする.
            }
        }

        public virtual void ExportItem(Corpus crps, IList<Word> wlist)
        {
            throw new NotImplementedException();  // must be implemented in each derived Exporter
        }

        public virtual void ExportLexeme(Dictionary dict, Lexeme lex)
        {
            throw new NotImplementedException();  // must be implemented in each derived Exporter
        }

        public virtual void ExportGrid(int nRows, int nCols, GridAccessor accessor, GridHeaderAccessor rhaccessor, GridHeaderAccessor chaccessor, string[] chtags = null)
        {
            throw new NotImplementedException();  // must be implemented in each derived Excel or CSV Exporter
        }

        private void CreateDocumentList(List<KwicItem> items)
        {
            m_DocumentTags = new Dictionary<string, SeqIDTagPair>();
            Corpus currentCorpus = null;
            Document currentDocument = null;
            int seqno = 1;  // DOC ID は1から始める（0からでも問題はないがCabochaReaderがそうしているので合わせる。）
            try
            {
                foreach (KwicItem ki in items)
                {
                    Corpus c = ki.Crps;
                    if (c != currentCorpus)
                    {
                        OpenCorpus(c);
                        currentCorpus = c;
                        // Namespace定義の出力
                        foreach (var ns in c.Namespaces)
                        {
                            if (!m_Namespaces.Contains(ns))
                            {
                                m_Namespaces.Add(ns);
                            }
                        }
                    }

                    // Document定義の出力
                    Document doc = FindDocument(ki.Document.ID);
                    if (doc != null && currentDocument != doc)
                    {
                        string cdoc = string.Format("{0}:{1}", c.Name, doc.ID);
                        if (!m_DocumentTags.ContainsKey(cdoc))
                        {
                            m_DocumentTags.Add(cdoc,
                                new SeqIDTagPair()
                                {
                                    Seqid = seqno,
                                    Tag = string.Format("#! DOCID\t{0}\t{1}\n", seqno, doc.GetAttributeStringAsXmlFragment())
                                });
                            seqno++;
                        }
                        currentDocument = ki.Document;
                    }
                }
            }
            finally
            {
                Close();
            }
            ExportDocumentList();
        }

        private void CreateDocumentList(Corpus crps)
        {
            // Namespace定義の出力
            foreach (var ns in crps.Namespaces)
            {
                if (!m_Namespaces.Contains(ns))
                {
                    m_Namespaces.Add(ns);
                }
            }

            // Document定義の出力
            m_DocumentTags = new Dictionary<string, SeqIDTagPair>();
            IQuery q = m_Session.CreateQuery("from Document order by ID asc");
            IList<Document> docs = q.List<Document>();
            foreach (Document doc in docs)
            {
                string cdoc = string.Format("{0}:{1}", crps.Name, doc.ID);
                if (!m_DocumentTags.ContainsKey(cdoc))
                {
                    m_DocumentTags.Add(cdoc,
                        new SeqIDTagPair()
                        {
                            Seqid = doc.ID,
                            Tag = string.Format("#! DOCID\t{0}\t{1}\n", doc.ID, doc.GetAttributeStringAsXmlFragment())
                        });
                }
            }

            ExportDocumentList();
        }

        private void CreateSentenceTagList(List<KwicItem> items)
        {
            m_SentenceTags = new Dictionary<string, SeqIDTagPair>();
            Corpus currentCorpus = null;
            int seqno = 0;  // SentenceTag ID は0から始める
            try
            {
                foreach (KwicItem ki in items)
                {
                    Corpus c = ki.Crps;
                    if (c != currentCorpus)
                    {
                        OpenCorpus(c);
                        currentCorpus = c;
                    }
                    Sentence sen = FindSentence(ki.SenID);
                    foreach (SentenceAttribute a in sen.Attributes)
                    {
                        string cdoc = string.Format("{0}:{1}", c.Name, a.ID);
                        if (!m_SentenceTags.ContainsKey(cdoc))
                        {
                            m_SentenceTags.Add(cdoc,
                                new SeqIDTagPair()
                                {
                                    Seqid = seqno,
                                    Tag = string.Format("#! SENTENCETAGID\t{0}\t{1}\n", seqno, a.GetAttributeStringAsXmlFragment())
                                });
                            seqno++;
                        }
                    }
                }
            }
            finally
            {
                Close();
            }
            ExportSentenceTagList();
        }

        private void CreateSentenceTagList(Corpus crps)
        {
            m_SentenceTags = new Dictionary<string, SeqIDTagPair>();
            IQuery q = m_Session.CreateQuery("from SentenceAttribute order by ID asc");
            var alist = q.List<SentenceAttribute>();
            int seqno = 0;
            foreach (SentenceAttribute a in alist)
            {
                string cattr = string.Format("{0}:{1}", crps.Name, a.ID);
                if (!m_SentenceTags.ContainsKey(cattr))
                {
                    m_SentenceTags.Add(cattr,
                        new SeqIDTagPair()
                        {
                            Seqid = seqno,
                            Tag = string.Format("#! SENTENCETAGID\t{0}\t{1}\n", seqno, a.GetAttributeStringAsXmlFragment())
                        });
                    seqno++;
                }
            }
            ExportSentenceTagList();
        }

        public virtual void ExportItem(KwicItem ki)
        {
            // must be implemented at derived class if it assumes a default writer
            throw new NotImplementedException();
        }

        public virtual void ExportItem(Corpus crps, Sentence sen)
        {
            // must be implemented at derived class if it assumes a default writer
            throw new NotImplementedException();
        }

        protected virtual void ExportDocumentList() { /* do nothing by default */ }

        protected virtual void ExportSentenceTagList() { /* do nothing by default */ }

        private void OpenCorpus(Corpus c)
        {
            Close();

            DBService dbs = DBService.Create(c.DBParam);
            NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
            m_Factory = cfg.BuildSessionFactory();
            m_Session = m_Factory.OpenSession();
            m_Session.FlushMode = FlushMode.Never;
        }

        private void OpenDictionary(Dictionary_DB d)
        {
            Close();

            DBService dbs = DBService.Create(d.DBParam);
            NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
            m_Factory = cfg.BuildSessionFactory();
            m_Session = m_Factory.OpenSession();
            m_Session.FlushMode = FlushMode.Never;
        }

        protected virtual void Close()
        {
            if (m_Session != null)
            {
                m_Session.Close();
                m_Session = null;
            }
            if (m_Factory != null)
            {
                m_Factory.Close();
                m_Factory = null;
            }
        }

        protected Document FindDocument(int docid)
        {
            if (m_Session == null) return null;
            IQuery q = m_Session.CreateQuery(
                string.Format("from Document where ID={0}", docid));
            return q.UniqueResult<Document>();
        }

        protected Sentence FindSentence(int senid)
        {
            if (m_Session == null) return null;
            IQuery q = m_Session.CreateQuery(
                string.Format("from Sentence where ID={0}", senid));
            return q.UniqueResult<Sentence>();
        }

        protected void WriteMecabLexeme(Lexeme lex)
        {
            for (int i = 0; i < m_Def.Fields.Length; i++)
            {
                if (m_Def.Fields[i] != null && m_Def.Fields[i].MappedTo != null && m_Def.Fields[i].MappedTo.Length > 0)
                {
                    MappedTo mapping = m_Def.Fields[i].MappedTo[0];
                    string prop = SelectLexemeProperty(lex, mapping);
                    if (prop == null || prop.Length == 0) prop = "*";
                    m_TextWriter.Write((prop != null) ? prop : string.Empty);
                }
                if (i == 0 || i == m_Def.Fields.Length - 1)
                {
                    m_TextWriter.Write("\t");
                }
                else
                {
                    m_TextWriter.Write(",");
                }
            }
            m_TextWriter.Write("O");
            m_TextWriter.WriteLine();
        }

        protected void WriteChasenLexeme(Lexeme lex)
        {
            for (int i = 0; i < m_Def.Fields.Length; i++)
            {
                if (m_Def.Fields[i].MappedTo.Length > 0)
                {
                    MappedTo mapping = m_Def.Fields[i].MappedTo[0];
                    string prop = SelectLexemeProperty(lex, mapping);
                    m_TextWriter.Write((prop != null) ? prop : string.Empty);
                }
                m_TextWriter.Write("\t");
            }
            m_TextWriter.Write("O");
            m_TextWriter.WriteLine();
        }

        protected string SelectLexemeProperty(Lexeme lex, MappedTo mapping)
        {
            if (mapping.Tag == "Custom")
            {
                string res = lex.GetCustomProperty(mapping.CustomTagName);
                if (res == null) return res;
                if (res.IndexOf(',') >= 0) res = "\"" + res + "\"";
                return res.Replace("\n", string.Empty);
            }
            LP? prop = Lexeme.FindProperty(mapping.Tag);
            if (!prop.HasValue)
            {
                return string.Empty;
            }
            if (prop.Value == LP.PartOfSpeech)   // 分解可能なPropertyはMappingのPartNoを見て判断する
            {
                string res = "";
                switch (mapping.PartNo)
                {
                    case 0:
                        res = lex.PartOfSpeech.Name; break;
                    case 1:
                        res = lex.PartOfSpeech.Name1; break;
                    case 2:
                        res = lex.PartOfSpeech.Name2; break;
                    case 3:
                        res = lex.PartOfSpeech.Name3; break;
                    case 4:
                        res = lex.PartOfSpeech.Name4; break;
                }
                return res;
            }
            else if (prop.Value == LP.CType)
            {
                string res = "";
                switch (mapping.PartNo)
                {
                    case 0:
                        res = lex.CType.Name; break;
                    case 1:
                        res = lex.CType.Name1; break;
                    case 2:
                        res = lex.CType.Name2; break;
                }
                return res;
            }
            else if (prop.Value == LP.CForm)
            {
                return lex.CForm.Name;
            }
            return lex.GetStringProperty(prop.Value);
        }

    }
}
