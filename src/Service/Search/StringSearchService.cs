using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using ChaKi.Entity.Corpora;
using NHibernate;
using System.Collections;
using System.Text.RegularExpressions;
using ChaKi.Common;
using System.Net;
using System.IO;
using System.Data;
using ChaKi.Service.Common;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Common.Settings;

namespace ChaKi.Service.Search
{
    public class StringSearchService : SearchServiceBase
    {

        private KwicList m_Model;

        public StringSearchService(SearchHistory hist, SearchHistory parent)
            : base(hist, parent)
        {
            m_Model = hist.KwicList;
        }

        /// <summary>
        /// コーパスごとのStringSearch本体
        /// StringSearchは検索方法が特殊で、まずCorpusに属するDocumentのTextをstring検索/or/正規表現検索して、
        /// Document内の出現位置Offsetを得る。（この部分はSuffixArray等に置き換え可能なので、メソッドを分離している）
        /// その後、見つかった位置からSentenceをQueryにより得て結果とする。
        /// </summary>
        /// <param name="c"></param>
        protected override void ExecuteSearchSession(Corpus c)
        {
            SearchConditions cond = m_CondSeq.Last;
            SearchSettings settings = SearchSettings.Current;

            IList lst = m_Session.CreateCriteria(typeof(DocumentSet)).List();
            if (lst.Count == 1)
            {
                c.DocumentSet = (DocumentSet)lst[0];
            }
            else
            {
                throw new Exception("Corpus:DocumentSet is not 1:1");
            }

            // AND検索（絞り込み）の場合は、tagCondの親のKwicListのSentece集合をフィルタ条件とする。
            List<int> targetSentences = null;
            if (cond.Operator == SearchSequenceOperator.And)
            {
                if (cond.Parent != null && m_HistParent != null)
                {
                    targetSentences = m_HistParent.KwicList.MakeSentenceIDListOfCorpus(c);
                }
            }
            FilterCondition fCond = cond.FilterCond;

            int totalCount = 0;
            //まず、DocumentごとにrangeListを求め、その結果から総hit数を得る
            Dictionary<Document, List<CharRange>> docRangeList = new Dictionary<Document, List<CharRange>>();
            foreach (Document doc in c.DocumentSet.Documents)
            {
                // Document Filter条件の適用
                if (fCond.AllEnabled && fCond.DocumentFilterValue.Length > 0)
                {
                    string filterValue = string.Empty;
                    foreach (DocumentAttribute attr in doc.Attributes)
                    {
                        if (attr.Key == fCond.DocumentFilterKey)
                        {
                            filterValue = attr.Value;
                            break;
                        }
                    }
                    if (filterValue.Length == 0 || fCond.DocumentFilterValue != filterValue)
                    {
                        continue;  // This document is filtered out.
                    }
                }
                List<CharRange> rangeList;
                if (settings.UseRemote)
                {
                    rangeList = SearchRemote(settings.RemoteAddress, c.Name, doc.ID, targetSentences, 
                        cond.StringCond.Pattern, cond.StringCond.IsCaseSensitive, cond.StringCond.IsRegexp);
                }
                else
                {
                    doc.Text = QueryDocumentText(doc.ID);
                    if (cond.StringCond.IsRegexp)
                    {
                        rangeList = RegexMatch(doc.Text, cond.StringCond.Pattern, cond.StringCond.IsCaseSensitive);
                    }
                    else
                    {
                        rangeList = StringMatch(doc.Text, cond.StringCond.Pattern, cond.StringCond.IsCaseSensitive);
                    }
                }
                if (rangeList.Count > 0)
                {
                    docRangeList[doc] = rangeList;
                }
                totalCount += rangeList.Count;
            }
            m_Progress.SetRange(totalCount);

            // 次にrangeList中の各CharPosからSentenceを検索して求める
            foreach (KeyValuePair<Document, List<CharRange>> pair in docRangeList)
            {
                Document doc = pair.Key;
                List<CharRange> rangeList = pair.Value;
                foreach (CharRange range in rangeList)
                {
                    string qstr = string.Format("from Sentence s where s.ParentDoc.ID={0} and s.StartChar <= {1} and s.EndChar > {1}",
                                doc.ID, range.Start);
                    IQuery query = m_Session.CreateQuery(qstr);
                    IList queryResult = query.List();
                    foreach (object o in queryResult)
                    {
                        Sentence sen = (Sentence)o;
                        if (targetSentences == null || 
                            targetSentences.Contains(sen.ID))
                        {

                            KwicItem ki = new KwicItem(c, doc, sen.ID, sen.StartChar, sen.EndChar, sen.Pos);
                            string s;
                            s = QuerySubstringFromDocText(doc, sen.StartChar, (int)range.Start - sen.StartChar);
                            ki.Left.AddText(s, 0);
                            s = QuerySubstringFromDocText(doc, (int)range.Start, range.Length);
                            ki.Center.AddText(s, KwicWord.KWA_PIVOT);
                            s = QuerySubstringFromDocText(doc, (int)range.End, sen.EndChar - (int)range.End);
                            ki.Right.AddText(s, 0);
                            m_Model.AddKwicItem(ki);
                            m_Progress.Increment();
                        }
                    }
                }
            }
        }

        private List<CharRange> StringMatch(string text, string pattern, bool isCaseSensitive)
        {
            List<CharRange> result = new List<CharRange>();

            int patternLength = pattern.Length;
            int p = -1;
            int txtlen = text.Length;
            while (true)
            {
                p = text.IndexOf(pattern, p + 1,
                    (isCaseSensitive?StringComparison.CurrentCulture:StringComparison.CurrentCultureIgnoreCase));
                if (p < 0)
                {
                    break;
                }
                result.Add(new CharRange(p, p + patternLength));
                string tmp = text.Substring(p, Math.Min(txtlen-p, 20));
//                Console.WriteLine("{0}", tmp);
            }
            return result;
        }

        private List<CharRange> RegexMatch(string text, string pattern, bool isCaseSensitive)
        {
            string[] texts = text.Split(new char[] { '\n' });
            List<CharRange> result = new List<CharRange>();

            int offset = 0;
            Regex regex = new Regex(pattern);
            foreach (string s in texts)
            {
                Match m = regex.Match(s);
                while (m.Success)
                {
                    result.Add(new CharRange(offset + m.Index, offset + m.Index + m.Length));
                    m = m.NextMatch();
                }
                offset += (s.Length+1); // +1 for '\n'
            }
            return result;
        }

        private List<CharRange> SearchRemote(string address, string corpusname, int docid, List<int> targetSenteces,
            string pattern, bool isCaseSensitive, bool isRegExp )
        {
            List<CharRange> result = new List<CharRange>();
            Uri uri = new Uri(string.Format("http://{0}/StringSearch", address));
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            req.Method = "POST";
            StringBuilder sb = new StringBuilder();
            sb.Append("corpus=");
            sb.Append(corpusname);
            sb.Append("&docid=");
            sb.Append(docid.ToString());
            sb.Append("&pattern=");
            sb.Append(pattern.Replace("&","%26"));
            if (isCaseSensitive)
            {
                sb.Append("&caseSensitive=on");
            }
            if (isRegExp)
            {
                sb.Append("&regexp=on");
            }
            if (targetSenteces != null && targetSenteces.Count > 0)
            {
                sb.Append("&senids=");
                sb.Append(Util.BuildIDList(targetSenteces));
            }

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = data.Length;

            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
            }

            // レスポンスの取得と読み込み
            WebResponse res = req.GetResponse();
            using (Stream resStream = res.GetResponseStream())
            using (StreamReader sr = new StreamReader(resStream, Encoding.UTF8))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    string[] fields = s.Split(new char[] { ',' });
                    if (fields.Length != 2)
                    {
                        throw new Exception(string.Format("Invalid Response Format: {0}", s));
                    }
                    int sPos, ePos;
                    if (!Int32.TryParse(fields[0], out sPos) || !Int32.TryParse(fields[1], out ePos))
                    {
                        throw new Exception(string.Format("Invalid Response Format: {0}", s));
                    }
                    result.Add(new CharRange(sPos, ePos));
                }
            }
            return result;
        }
    }
}
