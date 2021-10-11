using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using NHibernate;
using ChaKi.Entity.Corpora.Annotations;

namespace ChaKi.Service.Readers
{
    public class CabochaBunsetsuList : Dictionary<string, CabochaBunsetsu>
    {
        public void Add(CabochaBunsetsu obj)
        {
            string key = string.Format("{0}-{1}", obj.Sen.ID, obj.BunsetsuPos);
            if (!ContainsKey(key))
                base.Add(key, obj);
        }

        public CabochaBunsetsu Find(Sentence sen, int bunsetsuPos)
        {
            string key = string.Format("{0}-{1}", sen.ID, bunsetsuPos);
            CabochaBunsetsu ret;
            this.TryGetValue(key, out ret);
            return ret;
        }

        /// <summary>
        /// 現在のSentenceに付いているSegment/LinkからCabochaBunsetsuListを逆生成する.
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="sess"></param>
        /// <returns></returns>
        static public CabochaBunsetsuList CreateFromSentence(Sentence sen, ISession sess)
        {
            var result = new CabochaBunsetsuList();

            IQuery q = sess.CreateQuery(string.Format("from Segment where Tag.Name='Bunsetsu' and Sentence.ID={0} order by StartChar", sen.ID));
            var segs = q.List<Segment>();
            int n = 0;
            int sentenceStartChar = sen.StartChar;
            foreach (var seg in segs)
            {
                IQuery q1 = sess.CreateQuery(string.Format("from Link l where l.From.ID={0}", seg.ID));
                var links = q1.List<Link>();
                int to_bunsetsu_no = -1;
                string tag_name = "D";
                double score = 0.0;
                if (links.Count > 1)
                {
                    throw new Exception(string.Format("Segment does not have unique outgoing Link: segment id={0}", seg.ID));
                }
                if (links.Count == 1)
                {
                   // 文内での係り先文節番号を得る
                    to_bunsetsu_no = segs.IndexOf(links[0].To);
                    tag_name = links[0].Tag.Name;
                    var s = (from a in links[0].Attributes where a.Key == "Score" select a.Value).DefaultIfEmpty(string.Empty).First();
                    Double.TryParse(s, out score);
                }
                var cb = new CabochaBunsetsu(sen, null, seg.StartChar - sentenceStartChar, seg.EndChar - sentenceStartChar, n, tag_name, to_bunsetsu_no, score);
                IQuery q2 = sess.CreateQuery(string.Format("from Word where Bunsetsu.ID={0} order by StartChar", seg.ID));
                var words = q2.List<Word>();
                foreach (Word w in words)
                {
                    cb.Words.Add(w);
                }
                result.Add(cb);
                n++;
            }
            return result;
        }
    }
}
