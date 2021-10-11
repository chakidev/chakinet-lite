using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Service.Database;
using NHibernate;
using ChaKi.Service.Search;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Common;

namespace ChaKi.Service.Annotations
{
    public class AnnotationService : IAnnotationService
    {
        // FilterはString.Emptyなら全てのTagを対象とする（デフォルト）。
        // nullなら全てのTagを対象外とする。（BuildStringListの返り値）
        private string m_SegmentListFilter;
        private string m_LinkListFilter;
        private string m_GroupListFilter;

        public AnnotationService()
        {
            m_SegmentListFilter = String.Empty;
            m_LinkListFilter = String.Empty;
            m_GroupListFilter = String.Empty;
        }

        /// <summary>
        /// 取得する対象のSeg, Link, Groupの名前を設定する.
        /// </summary>
        /// <param name="seglist"></param>
        /// <param name="linklist"></param>
        /// <param name="grplist"></param>
        public void SetTagNameFilter(IList<string> seglist, IList<string> linklist, IList<string> grplist)
        {
            m_SegmentListFilter = Util.BuildStringList(seglist);
            m_LinkListFilter = Util.BuildStringList(linklist);
            m_GroupListFilter = Util.BuildStringList(grplist);
        }

        /// <summary>
        /// KwicListに含まれるSentenceに関連するAnnotationをすべてCorpusより取得する
        /// </summary>
        /// <param name="src"></param>
        /// <param name="result"></param>
        public void Load(KwicList src, AnnotationList result, Action<int> callback, ref bool cancelFlag)
        {
            Corpus currentCorpus = null;
            foreach (KwicItem ki in src.Records)
            {
                if (currentCorpus != ki.Crps)
                {
                    if (currentCorpus == null)
                    {
                        currentCorpus = ki.Crps;
                    } else {
                        throw new NotSupportedException("Cannot obtain annotations for a KwicList containing multiple corpora.");
                    }
                }
            }
            if (currentCorpus == null) {
                return;
            }

            result.Clear();

            DBService dbs = DBService.Create(currentCorpus.DBParam);
            NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
            ISessionFactory factory = cfg.BuildSessionFactory();
            int total = src.Records.Count;
            using (ISession session = factory.OpenSession())
            {
                List<int> senPosList = new List<int>();
                foreach (KwicItem ki in src.Records)
                {
                    senPosList.Add(ki.SenID);
                }
                if (senPosList.Count < 0)
                {
                    return;
                }
                else if (senPosList.Count == 1)
                {
                    FetchAnnotations(senPosList[0], session, result, callback);
                }
                else
                {
                    FetchAnnotations(senPosList, session, result, callback, ref cancelFlag);
                }
            }
        }

        private void FetchAnnotations(int senPos, ISession session, AnnotationList result, Action<int> callback)
        {
            if (m_SegmentListFilter.Length == 0)
            {
                return;
            }
            string qstr = string.Format("from Segment s where s.Sentence.ID = {0}", senPos);
            if (m_SegmentListFilter != null)
            {
                qstr += string.Format(" and s.Tag.Name in {0}", m_SegmentListFilter);
            }
            IQuery query = session.CreateQuery(qstr);
            IList<Segment> segs = query.List<Segment>();
            result.Segments.AddRange(segs);

            qstr = string.Format("select distinct k from Link k where (k.FromSentence.ID = {0} or k.ToSentence.ID = {0})", senPos);
            if (m_LinkListFilter != null)
            {
                qstr += string.Format(" and k.Tag.Name in {0}", m_LinkListFilter);
            }
            query = session.CreateQuery(qstr);
            IList<Link> links = query.List<Link>();
            result.Links.AddRange(links);
            if (callback != null)
            {
                callback(100);
            }
        }

        private void FetchAnnotations(List<int> senPosList, ISession session, AnnotationList result, Action<int> callback, ref bool cancelFlag)
        {
            int total = senPosList.Count;
            StringBuilder sb = new StringBuilder();
            StringConnector sc = new StringConnector(",");

            for (int i = 0; i < senPosList.Count; i++)
            {
                if (cancelFlag)
                {
                    return;
                }
                int senPos = senPosList[i];
                sb.Append(sc.Get());
                sb.AppendFormat("{0}", senPos);

                if (i % 500 == 499 || i == senPosList.Count - 1)
                {
                    string slist = sb.ToString();

                    if (m_SegmentListFilter != null)
                    {
                        string qstr = string.Format("from Segment s where s.Sentence.ID in ({0})", slist);
                        if (m_SegmentListFilter != null)
                        {
                            qstr += string.Format(" and s.Tag.Name in {0}", m_SegmentListFilter);
                        }
                        IQuery query = session.CreateQuery(qstr);
                        IList<Segment> segs = query.List<Segment>();
                        result.Segments.AddRange(segs);
                    }
                    if (m_LinkListFilter != null)
                    {
                        string qstr = string.Format("select distinct k from Link k where (k.FromSentence.ID in ({0}) or k.ToSentence.ID in ({0}))", slist);
                        if (m_LinkListFilter.Length > 0)
                        {
                            qstr += string.Format(" and k.Tag.Name in {0}", m_LinkListFilter);
                        }
                        IQuery query = session.CreateQuery(qstr);
                        IList<Link> links = query.List<Link>();
                        result.Links.AddRange(links);
                    }

                    if (callback != null)
                    {
                        callback(100 * i / total);
                    }
                    sb.Length = 0;
                    sc.Reset();
                }
            }
        }
    }
}
