using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Service.Database;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;
using ChaKi.Service.Common;

namespace ChaKi.Service.AttributeEditService
{
    public class AttributeEditService : IAttributeEditService
    {
        private Corpus m_Corpus;
        private OpContext m_Context;
        private object m_Target;

        public object Target
        {
            get { return m_Target; }
            set
            {
                m_Target = value;
            }
        }

        public AttributeEditService()
        {
            m_Context = null;
            m_Corpus = null;
            m_Target = null;
        }

        public void Open(Corpus corpus, object targetEntity, UnlockRequestCallback callback)
        {
            if (m_Context != null && m_Context.IsTransactionActive())
            {
                throw new InvalidOperationException("Active transaction exists. Close or Commit it first.");
            }

            m_Corpus = corpus;
            m_Target = targetEntity;
            m_Context = OpContext.Create(m_Corpus.DBParam, callback, typeof(IAttributeEditService));
        }

        /// <summary>
        /// 既存のOpContext(Session, Transaction)上でAttributeEditを開始する.
        /// </summary>
        /// <param name="corpus"></param>
        /// <param name="targetEntity"></param>
        /// <param name="context"></param>
        public void Open(Corpus corpus, object targetEntity, OpContext context)
        {
            m_Corpus = corpus;
            m_Target = targetEntity;
            m_Context = context;
        }
        public void ContinueWithDifferntTarget(Corpus corpus, object targetEntity)
        {
            m_Corpus = corpus;
            m_Target = targetEntity;
        }

        public void Close()
        {
            if (m_Context != null)
            {
                m_Context.Dispose();
                m_Context = null;
                m_Corpus = null;
            }
        }

        public IList<AttributeBase32> GetSentenceTagList()
        {
            return m_Context.Session.CreateQuery("from SentenceAttribute where DocumentID=-1 order by Key").List<AttributeBase32>();
        }

        public void UpdateAttributesForSentence(Dictionary<string, string> newSenAttrs, Dictionary<string, string> newDocAttrs)
        {
            Sentence sen = this.m_Target as Sentence;
            if (sen == null)
            {
                throw new InvalidOperationException("AttributeEditService - Target is not Sentence");
            }
            // Document Attributeを比較し、完全に一致していれば何も変更しない.
            bool identical = true;
            foreach (DocumentAttribute a in sen.ParentDoc.Attributes)
            {
                if (!newDocAttrs.ContainsKey(a.Key) || newDocAttrs[a.Key] != a.Value)
                {
                    identical = false;
                    break;
                }
            }
            foreach (var pair in newDocAttrs)
            {
                string v = sen.ParentDoc.GetAttribute(pair.Key);
                if (v == null || v != pair.Value)
                {
                    identical = false;
                    break;
                }
            }
            //!SQL
            ISQLQuery q;
            if (!identical)
            {
                q = m_Context.Session.CreateSQLQuery(string.Format("DELETE from documenttag WHERE document_id={0}", sen.ParentDoc.ID));
                q.ExecuteUpdate();
                foreach (var a in newDocAttrs)
                {
                    var id = (int)Util.GetNextAvailableID(m_Context.Session, "documenttag");
                    q = m_Context.Session.CreateSQLQuery(string.Format("INSERT INTO documenttag VALUES({0},'{1}','{2}',{3},{4})",
                                id, a.Key, Util.EscapeQuote(a.Value), sen.ParentDoc.ID, m_Context.DBService.GetDefault()));
                    q.ExecuteUpdate();
                }
            }

            // Sentence Attribute
            q = m_Context.Session.CreateSQLQuery(string.Format("DELETE from sentence_documenttag WHERE sentence_id={0}", sen.ID));
            q.ExecuteUpdate();
            foreach (var a in newSenAttrs)
            {
                q = m_Context.Session.CreateSQLQuery(string.Format("SELECT id FROM documenttag WHERE tag='{0}' AND description='{1}' AND document_id=-1",
                            a.Key, Util.EscapeQuote(a.Value)));
                var result = q.List<int>();
                int id;
                if (result.Count > 0)
                {
                    id = (int)result[0];
                }
                else
                {
                    id = (int)Util.GetNextAvailableID(m_Context.Session, "documenttag");
                    q = m_Context.Session.CreateSQLQuery(string.Format("INSERT INTO documenttag VALUES({0},'{1}','{2}',{3},{4})",
                            id, a.Key, Util.EscapeQuote(a.Value), -1, m_Context.DBService.GetDefault()));
                    q.ExecuteUpdate();
                }
                q = m_Context.Session.CreateSQLQuery(string.Format("INSERT INTO sentence_documenttag VALUES({0},{1})", sen.ID, id));
                q.ExecuteUpdate();
            }
            m_Context.Session.Flush();
            m_Context.Session.Refresh(sen.ParentDoc);
            m_Context.Session.Refresh(sen);
            foreach (var attr in sen.ParentDoc.Attributes)
            {
                m_Context.Session.Refresh(attr);
            }
            foreach (var attr in sen.Attributes)
            {
                m_Context.Session.Refresh(attr);
            }
        }

        public void UpdateAttributesForSegment(Dictionary<string, string> newData, Segment tag = null)
        {
            if (tag == null)
            {
                tag = m_Target as Segment;
                if (tag == null)
                {
                    throw new InvalidOperationException("AttributeEditService - Target is not Segment");
                }
            }

            //!SQL
            ISQLQuery q;
            string table = "segment_attribute";

            q = m_Context.Session.CreateSQLQuery(string.Format("DELETE from {0} WHERE segment_id={1}", table, tag.ID));
            q.ExecuteUpdate();
            foreach (var a in newData)
            {
                var id = Util.GetNextAvailableID(m_Context.Session, table);
                q = m_Context.Session.CreateSQLQuery(string.Format("INSERT INTO {0} VALUES({1},{2},'{3}','{4}',{5},{6},{7},{8},'{9}')",
                            table, id, tag.ID, a.Key, Util.EscapeQuote(a.Value),
                            0, 0, 0,
                            m_Context.DBService.GetDefault(), string.Empty));
                q.ExecuteUpdate();
            }
            // ISQLQueryでUpdateしたPersistentオブジェクトのメモリ上のキャッシュ値を更新する.
            m_Context.Session.Flush();
            try
            {
                m_Context.Session.Refresh(tag);
                foreach (var attr in tag.Attributes)
                {
                    m_Context.Session.Refresh(attr);
                }
            }
            catch
            {
                // SegmentをDepEditで削除したとき、ここに来るが、無視する.
                Console.WriteLine("Could not update Segment attribute cache.");
            }
        }

        public void UpdateAttributesForLink(Dictionary<string, string> newData, Link tag = null)
        {
            if (tag == null)
            {
                tag = m_Target as Link;
                if (tag == null)
                {
                    throw new InvalidOperationException("AttributeEditService - Target is not Link");
                }
            }

            //!SQL
            ISQLQuery q;
            string table = "link_attribute";

            q = m_Context.Session.CreateSQLQuery(string.Format("DELETE from {0} WHERE link_id={1}", table, tag.ID));
            q.ExecuteUpdate();
            foreach (var a in newData)
            {
                var id = Util.GetNextAvailableID(m_Context.Session, table);
                q = m_Context.Session.CreateSQLQuery(string.Format("INSERT INTO {0} VALUES({1},{2},'{3}','{4}',{5},{6},{7},{8},'{9}')",
                            table, id, tag.ID, a.Key, Util.EscapeQuote(a.Value),
                            0, 0, 0,
                            m_Context.DBService.GetDefault(), string.Empty));
                q.ExecuteUpdate();
            }
            // ISQLQueryでUpdateしたPersistentオブジェクトのメモリ上のキャッシュ値を更新する.
            m_Context.Session.Flush();
            try
            {
                m_Context.Session.Refresh(tag);
                foreach (var attr in tag.Attributes)
                {
                    m_Context.Session.Refresh(attr);
                }
            }
            catch
            {
                // LinkをDepEditで削除したとき、ここに来るが、無視する.
                Console.WriteLine("Could not update Link attribute cache.");
            }
        }

        public void UpdateAttributesForGroup(Dictionary<string, string> newData, Group tag = null)
        {
            if (tag == null)
            {
                tag = m_Target as Group;
                if (tag == null)
                {
                    throw new InvalidOperationException("AttributeEditService - Target is not Group");
                }
            }

            //!SQL
            ISQLQuery q;
            string table = "group_attribute";

            q = m_Context.Session.CreateSQLQuery(string.Format("DELETE from {0} WHERE group_id={1}", table, tag.ID));
            q.ExecuteUpdate();
            foreach (var a in newData)
            {
                var id = Util.GetNextAvailableID(m_Context.Session, table);
                q = m_Context.Session.CreateSQLQuery(string.Format("INSERT INTO {0} VALUES({1},{2},'{3}','{4}',{5},{6},{7},{8},'{9}')",
                            table, id, tag.ID, a.Key, Util.EscapeQuote(a.Value),
                            0, 0, 0,
                            m_Context.DBService.GetDefault(), string.Empty));
                q.ExecuteUpdate();
            }
            // ISQLQueryでUpdateしたPersistentオブジェクトのメモリ上のキャッシュ値を更新する.
            m_Context.Session.Flush();
            try
            {
                m_Context.Session.Refresh(tag);
                foreach (var attr in tag.Attributes)
                {
                    m_Context.Session.Refresh(attr);
                }
            }
            catch
            {
                // GroupをDepEditで削除したとき、ここに来るが、無視する.
                Console.WriteLine("Could not update Group attribute cache.");
            }
        }

        public void Flush()
        {
            m_Context.Flush();
        }

        public void Commit()
        {
            if (!m_Context.IsTransactionActive())
            {
                throw new InvalidOperationException("No active transaction exists.");
            }

            // コミットする
            m_Context.Trans.Commit();
            m_Context.Trans.Dispose();
            m_Context.Trans = null;

            // 操作記録をScriptの形で得てstaticメンバにセットする.
            //@todo

        }
    }
}
