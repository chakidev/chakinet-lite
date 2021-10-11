using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;
using System.IO;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Search;
using ChaKi.Service.Common;
using ChaKi.Entity.Readers;
using ChaKi.Service.Readers;
using System.Collections;
using System.Data.Common;
using System.Data;
using NHibernate.Criterion;

namespace ChaKi.Service.Export
{
    public class ExportServiceCabocha : ExportServiceBase
    {
        protected Dictionary<long, int> m_Segs;  // Key=Segment.ID;  Value=bunsetsu_pos in a sentence
        protected Dictionary<long, object[]> m_Links;  // Key=link.FromSeg.ID;  Value={Link.ID, Link.ToSeg.ID, Link.Tag}
        protected int m_CurrentDocId;
        protected Action<Lexeme> m_LexemeWriter;
        protected Dictionary<Document, int> m_DocOffsetCache;
        protected bool m_OutputSentencetagids = true;

        public ExportServiceCabocha(TextWriter wr)
        {
            ReaderDef def = CorpusSourceReaderFactory.Instance.ReaderDefs.Find("Mecab|Cabocha");
            Initialize(wr, def);
        }

        public ExportServiceCabocha(TextWriter wr, ReaderDef def)
        {
            Initialize(wr, def);
        }

        private void Initialize(TextWriter wr, ReaderDef def)
        {
            m_TextWriter = wr;
            m_Def = def;
            m_CurrentDocId = -1;
            if (m_Def.LineFormat == "TabSeparatedLine")
            {
                m_LexemeWriter = this.WriteChasenLexeme;
            }
            else if (m_Def.LineFormat == "MecabLine")
            {
                m_LexemeWriter = this.WriteMecabLexeme;
            }
            else
            {
                throw new NotImplementedException(string.Format("Export format '{0}' is not supported yet.", m_Def.LineFormat));
            }
            m_DocOffsetCache = new Dictionary<Document, int>();

            Util.Reset();

            m_LexemeCache.Clear();
        }

        protected override void Close()
        {
            base.Close();
            m_LexemeCache.Clear();
        }

        public override void ExportItem(KwicItem ki)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            IQuery q = m_Session.CreateQuery(string.Format("from Sentence where ID={0}", ki.SenID));
            Sentence sen = q.UniqueResult<Sentence>();
            if (sen == null)
            {
                throw new Exception(string.Format("Sentence not found. Corpus={0}, senID={1}", ki.Crps.Name, ki.SenID));
            }
            var segs = m_Session.CreateQuery(
                     "from Segment s" +
                    $" where s.Sentence.ID={ki.SenID}" +
                    $" and s.Tag.ID={Util.GetBunsetsuTagId(m_Session)}" +
                    $" and s.Proj.ID={m_ProjId}" +
                    $" order by s.StartChar")
                .List<Segment>();
            var nsegs = segs.Count;
            m_Segs = new Dictionary<long, int>();
            if (nsegs > 0)
            {
                for (int i = 0; i < nsegs - 1; i++)  // 最後のsegmentはdummy
                {
                    m_Segs.Add(segs[i].ID, i);
                }
            }
            var links = m_Session.CreateQuery(
                    "from Link l " +
                    $"l.Proj.ID={m_ProjId}" +
                    $"where l.FromSentence.ID={ ki.SenID}")
                .List<Link>();
            m_Links = new Dictionary<long, object[]>();
            foreach (var link in links)
            {
                if (link.From.Tag.Name == "Bunsetsu" && link.To.Tag.Name == "Bunsetsu")
                {
                    m_Links.Add(link.From.ID, new object[] { link.ID, link.To.ID, link.Tag.Name, 0.0 });
                }
            }

            // DOCタグの出力
            string cdoc = string.Format("{0}:{1}", ki.Crps.Name, sen.ParentDoc.ID);
            SeqIDTagPair pair;
            if (m_DocumentTags.TryGetValue(cdoc, out pair))
            {
                if (pair.Seqid != m_CurrentDocId)
                {
                    m_TextWriter.WriteLine("#! DOC {0}", pair.Seqid);
                    WriteAttributes(sen.ParentDoc);
                }
                m_CurrentDocId = pair.Seqid;
            }

            // SENTENCETAGの出力
            foreach (var a in sen.Attributes)
            {
                string csa = string.Format("{0}:{1}", ki.Crps.Name, a.ID);
                WriteSentenceTag(csa);
            }

            WriteWords(sen.GetWords(0));
            WriteAnnotations(sen);
            WriteEos();
        }

        protected virtual void WriteSentenceTag(string csa)
        {
            if (m_SentenceTags.TryGetValue(csa, out var pair))
            {
                m_TextWriter.WriteLine("#! SENTENCETAG {0}", pair.Seqid);
            }
        }

        static public long t1 = 0;
        static public long t2 = 0;
        static public long t3 = 0;
        static public long t4 = 0;
        static public long t5 = 0;
        static public long t6 = 0;

        private Dictionary<long, Lexeme> m_LexemeCache = new Dictionary<long, Lexeme>();

        public override void ExportItem(Corpus crps, Sentence sen)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            var t0 = DateTime.Now.Ticks;
#if false // NHibernateでアクセスするとn^2オーダーの時間がかかる.
            var segs = m_Session.CreateSQLQuery(
                string.Format("SELECT id FROM segment WHERE sentence_id={0} and tag_definition_id={1} ORDER BY start_char",
                sen.ID, Util.GetBunsetsuTagId(m_Session))).List();

            var nsegs = segs.Count;
            m_Segs = new Dictionary<long, int>();
            if (nsegs > 0)
            {
                for (int i = 0; i < nsegs - 1; i++)  // 最後のsegmentはdummy
                {
                    m_Segs.Add((long)segs[i], i);
                }
            }
#else
            int tagid = Util.GetBunsetsuTagId(m_Session);
            m_Segs = new Dictionary<long, int>();
            using (var cmd = m_Session.Connection.CreateCommand())
            {
                cmd.CommandText = $"SELECT id FROM segment WHERE sentence_id={sen.ID} and tag_definition_id={tagid} and project_id={m_ProjId} ORDER BY start_char";
                var rdr = cmd.ExecuteReader();
                long last = -1;
                int i = 0;
                while (rdr.Read())
                {
                    var segid = (long)rdr[0];
                    m_Segs.Add(segid, i++);
                    last = segid;
                }
                rdr.Close();
                m_Segs.Remove(last);
            }
#endif
            t1 += (DateTime.Now.Ticks - t0);
            t0 = DateTime.Now.Ticks;

#if false // NHibernateでアクセスするとn^2オーダーの時間がかかる.
            var links = m_Session.CreateSQLQuery(
                string.Format("SELECT * FROM link WHERE from_sentence_id={0}", sen.ID))
                .AddEntity(typeof(Link)).List<Link>();
            m_Links = new Dictionary<long, Link>();
            foreach (var link in links)
            {
                m_Links.Add(link.From.ID, link);
            }
#else
            m_Links = new Dictionary<long, object[]>();
            using (var cmd = m_Session.Connection.CreateCommand())
            {
                cmd.CommandText = string.Format(
                "SELECT l.id,l.from_segment_id,l.to_segment_id,t.tag_name,attr.attribute_value FROM link l "
                + "INNER JOIN tag_definition t ON t.id=l.tag_definition_id "
                + "INNER JOIN segment s1 ON l.from_segment_id=s1.id "
                + "INNER JOIN segment s2 ON l.to_segment_id=s2.id "
                + "LEFT OUTER JOIN link_attribute attr ON l.id=attr.link_id AND attr.attribute_key='Score'"
                + "WHERE l.from_sentence_id={0} AND s1.tag_definition_id={1} AND s2.tag_definition_id={1}", sen.ID, tagid);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var linkid = (long)rdr[0];
                    var fromsegid = (long)rdr[1];
                    var tosegid = (long)rdr[2];
                    var linktagname = (string)rdr[3];
                    var score = 0.0;
                    Double.TryParse((rdr[4] as string) ?? string.Empty, out score);
                    m_Links.Add(fromsegid, new object[] { linkid, tosegid, linktagname, score });
                }
                rdr.Close();
            }
#endif
            t2 += (DateTime.Now.Ticks - t0);
            t0 = DateTime.Now.Ticks;

            // DOCタグの出力
            string cdoc = string.Format("{0}:{1}", crps.Name, sen.ParentDoc.ID);
            SeqIDTagPair pair;
            if (m_DocumentTags.TryGetValue(cdoc, out pair))
            {
                if (pair.Seqid != m_CurrentDocId)
                {
                    m_TextWriter.WriteLine("#! DOC {0}", pair.Seqid);
                    WriteAttributes(sen.ParentDoc);
                }
                m_CurrentDocId = pair.Seqid;
            }
            // SENTENCETAGの出力
            foreach (var a in sen.Attributes)
            {
                string csa = string.Format("{0}:{1}", crps.Name, a.ID);
                WriteSentenceTag(csa);
            }

            t3 += (DateTime.Now.Ticks - t0);
            t0 = DateTime.Now.Ticks;

            // GetWordsだとここでは不要なクエリまで行い非常に遅くなってしまうため、最小限必要なカラムのみを直接SQLクエリによって取得する。
            // また、Lexemeの取得にローカルCacheを用いて高速化を図る。
#if false
            var words = sen.GetWords(project_id);
#else
            var words = new List<Word>();
            using (var cmd = m_Session.Connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT w.lexeme_id, w.bunsetsu_segment_id, w.head_info, w.extra_chars" +
                    " FROM word w" +
                    $" WHERE w.sentence_id={sen.ID} AND w.project_id={m_ProjId} ORDER BY w.position ASC";
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var w = new Word();
                    w.HeadInfo = (HeadInfo)rdr[2];
                    w.Bunsetsu = new Segment() { ID = (Int64)rdr[1] };
                    w.Extras = (string)rdr[3];
                    w.Lex = new Lexeme() { ID = (Int32)rdr[0] };  // このループをいったん抜けてから下で正しいLexemeにセットしなおす.
                    words.Add(w);
                }
            }
            foreach (var w in words)
            {
                Lexeme lex;
                if (!m_LexemeCache.TryGetValue(w.Lex.ID, out lex))
                {
                    lex = m_Session.CreateQuery($"from Lexeme l where l.ID={w.Lex.ID}").UniqueResult<Lexeme>();
                    if (lex != null)
                    {
                        m_LexemeCache.Add(lex.ID, lex);
                    }
                }
                w.Lex = lex;
            }
#endif
            t4 += (DateTime.Now.Ticks - t0);
            t0 = DateTime.Now.Ticks;

            WriteWords(words);
            t5 += (DateTime.Now.Ticks - t0);
            t0 = DateTime.Now.Ticks;

            WriteAnnotations(sen);

            WriteEos();
        }

        protected virtual void WriteWords(IList<Word> words)
        {
            // ここで渡されるwordsには、Bunsetsu.ID, HeadInfo, Lex, Extras しかセットされていないtransient objectなので注意. 上のExportItem()を参照.

            Dictionary<long, string> headInfoStrs = new Dictionary<long, string>();

            // 先に各文節のHead情報からWriteCabochaBunsetsu()に渡すHead Stringを作成する.
            int currentHeadInd = 0;
            int currentHeadAnc = 0;
            int wordPos = 0;
            long currentBunsetsuID = -1;
            for (var i = 0; i < words.Count; i++)
            {
                var w = words[i];
                long b = w.Bunsetsu.ID;
                if (currentBunsetsuID != b)
                {
                    var s = string.Format("{0}/{1}", currentHeadInd, currentHeadAnc);
                    headInfoStrs.Add(currentBunsetsuID, s);
                    wordPos = 0;
                    currentHeadInd = currentHeadAnc = 0;
                    currentBunsetsuID = b;
                }
                if ((w.HeadInfo & HeadInfo.Independent) != 0)
                {
                    currentHeadInd = wordPos;
                }
                if ((w.HeadInfo & HeadInfo.Ancillary) != 0)
                {
                    currentHeadAnc = wordPos;
                }
                wordPos++;
            }
            headInfoStrs.Add(currentBunsetsuID, string.Format("{0}/{1}", currentHeadInd, currentHeadAnc));

            // その後、Bunsetsu-Wordsを出力
            int bunsetsuPos = 0;
            currentBunsetsuID = -1;
            foreach (var w in words)
            {
                long b = w.Bunsetsu.ID;
                if (currentBunsetsuID != b)
                {
                    // Output Bunsetsu tag
                    WriteCabochaBunsetsu(b, headInfoStrs[b]);
                    currentBunsetsuID = b;
                    bunsetsuPos++;
                }
                if (w.Lex != null)
                {
                    m_LexemeWriter(w.Lex);
                }
            }
        }

        protected virtual void WriteEos()
        {
            m_TextWriter.WriteLine("EOS");
        }

        protected override void ExportDocumentList()
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            foreach (var pair in m_Namespaces)
            {
                m_TextWriter.Write(string.Format("#! NAMESPACE {0} \"{1}\"\n", pair.Key, pair.Value));
            }
            foreach (KeyValuePair<string, SeqIDTagPair> pair in m_DocumentTags)
            {
                m_TextWriter.Write(pair.Value.Tag);
            }
        }

        protected override void ExportSentenceTagList()
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            if (m_OutputSentencetagids)
            {
                foreach (KeyValuePair<string, SeqIDTagPair> pair in m_SentenceTags)
                {
                    m_TextWriter.Write(pair.Value.Tag);
                }
            }
        }

        private void WriteCabochaBunsetsu(long segid, string headInfo = "0/0")
        {
            if (m_Segs == null || m_Links == null)
            {
                throw new Exception(string.Format("Attempt to write Bunsetsu but no Segment found: SegID={0}", segid));
            }
            int buns_pos;
            if (!m_Segs.TryGetValue(segid, out buns_pos))
            {
                m_TextWriter.Write("* 0");       // 文節が1つ（デフォルト文節）しかない場合
            }
            else
            {
                m_TextWriter.Write("* {0}", buns_pos);
            }
            // segの係り先segを求める
            long toSegID = -1;
            object[] link = null;
            if (!m_Links.TryGetValue(segid, out link))
            {
                m_TextWriter.Write(" -1D {0} {1}", headInfo, 0.0);
            }
            else
            {
                toSegID = (long)link[1];
                int to_index;
                if (!m_Segs.TryGetValue(toSegID, out to_index))
                {
                    to_index = -1;
                }
                m_TextWriter.Write(" {0}{1}", to_index, (string)link[2]);
                m_TextWriter.Write(" {0} {1}", headInfo, link[3]);
            }
            m_TextWriter.WriteLine();
        }

        protected bool WriteAnnotations(Sentence sen)
        {
            bool hasAnnotation = false;
            IList<Group> grps = Util.RetrieveWordGroups(m_Session, sen, m_ProjId);

            int senOffset = sen.StartChar;
            int segno = 0; // 本メソッドローカルのSegment番号（文内）
            var seg_index = new Dictionary<long, string>();  // Key=Segment.ID; Value=segno in sentence

            StringBuilder sb = new StringBuilder();
            foreach (var g in grps)
            {
                sb.Length = 0;
                foreach (Segment s in g.Tags)
                {
                    m_TextWriter.WriteLine("#! SEGMENT_S {0} {1} {2} \"{3}\"", s.Tag.Name, s.StartChar - senOffset, s.EndChar - senOffset,
                        s.GetNormalizedCommentString());
                    sb.AppendFormat("{0} ", GetSegmentHash(s, segno, senOffset));
                    seg_index[s.ID] = GetSegmentHash(s, segno, senOffset);
                    segno++;
                    WriteAttributes(s);
                }
                m_TextWriter.WriteLine("#! GROUP_S {0} {1} \"{2}\"", g.Tag.Name, sb.ToString(), g.GetNormalizedCommentString());
                WriteAttributes(g);
                hasAnnotation = true;
            }

            // Groupの一部およびCabochaで表現される以外のSegmentを出力する
            IList<long> segids = Util.RetrieveMiscSegments(m_Session, sen, m_ProjId);   // Seg.ID, Seg.tagname, Seg.StartChar, Seg.EndChar, Seg.Comment
            foreach (var segid in segids)
            {
                if (seg_index.ContainsKey(segid))  // 既にGroupの一部としてExportされたSegmentは除外する.
                {
                    continue;
                }
                var seg = m_Session.CreateCriteria<Segment>().Add(Expression.Eq("ID", segid)).UniqueResult<Segment>();

                if (string.IsNullOrEmpty(seg.Tag.Name))
                {
                    m_TextWriter.WriteLine("#! SEGMENT_S \"{0}\" {1} {2} \"{3}\"", string.Empty, seg.StartChar - senOffset, seg.EndChar - senOffset,
                    seg.GetNormalizedCommentString());
                }
                else
                {
                    m_TextWriter.WriteLine("#! SEGMENT_S {0} {1} {2} \"{3}\"", seg.Tag.Name, seg.StartChar - senOffset, seg.EndChar - senOffset,
                    seg.GetNormalizedCommentString());
                }
                seg_index[segid] = GetSegmentHash(seg, segno, senOffset);
                segno++;
                WriteAttributes(seg);
                hasAnnotation = true;
            }

            // Cabochaで表現される以外のLinkを出力する
            IList<long> linkids = Util.RetrieveMiscLinks(m_Session, sen, m_ProjId);  // fromsegid, tosegid, tagname, comment
            foreach (var linkid in linkids)
            {
                try
                {
                    string from_idx, to_idx;
                    // 不完全データ対応（from_segment_id, to_segment_idが不正）
                    // HibernateでLoadし、try~catchで捕捉した場合、その後のLoadに影響（Tagがロードできない）がある。
                    // そのため、不完全データの判定基準であるfrom_segment_id, to_segment_idのみを最初に読み込む.
                    {
                        var res = m_Session.CreateSQLQuery($"SELECT from_segment_id,to_segment_id FROM link WHERE id={linkid}")
                            .UniqueResult<object[]>();
                        if (!seg_index.TryGetValue((long)(res[0]), out from_idx)
                         || !seg_index.TryGetValue((long)(res[1]), out to_idx))
                        {
                            continue;
                        }
                    }
                    var link = m_Session.Load<Link>(linkid);
                    if (seg_index.TryGetValue(link.From.ID, out from_idx)
                     && seg_index.TryGetValue(link.To.ID, out to_idx))
                    {
                        if (string.IsNullOrEmpty(link.Tag.Name))
                        {
                            m_TextWriter.WriteLine("#! LINK_S \"{0}\" {1} {2} \"{3}\"", string.Empty, from_idx, to_idx,
                                link.GetNormalizedCommentString());
                        }
                        else
                        {
                            m_TextWriter.WriteLine("#! LINK_S {0} {1} {2} \"{3}\"", link.Tag.Name, from_idx, to_idx,
                                link.GetNormalizedCommentString());
                        }
                        WriteAttributes(link);
                    }
                    hasAnnotation = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Giving up loading an incomplete Link entity: {linkid}");
                }
            }
            return hasAnnotation;
        }

        protected virtual string GetSegmentHash(Segment seg, int index, int senOffset)
        {
            return index.ToString();
        }

        protected void WriteAttributes(object ann)
        {
            if (ann == null)
            {
                return;
            }
            {
                var doc = ann as Document;
                if (doc != null)
                {
                    foreach (var attr in doc.Attributes)
                    {
                        if (!attr.Key.StartsWith("@"))   // "@"で始まるのはSentenceAttributeなのでDocumentAttributeとしては出力しない(SENTENCETAGIDとして出力される)
                        {
                            m_TextWriter.WriteLine("#! ATTR \"{0}\" \"{1}\" \"{2}\"", attr.Key, attr.Value, Util.EscapeQuote(attr.Comment));
                        }
                    }
                    return;
                }
            }
            {
                var seg = ann as Segment;
                if (seg != null)
                {
                    foreach (var attr in seg.Attributes)
                    {
                        m_TextWriter.WriteLine("#! ATTR \"{0}\" \"{1}\" \"{2}\"", attr.Key, attr.Value, Util.EscapeQuote(attr.Comment));
                    }
                    return;
                }
            }
            {
                var lnk = ann as Link;
                if (lnk != null)
                {
                    foreach (var attr in lnk.Attributes)
                    {
                        m_TextWriter.WriteLine("#! ATTR \"{0}\" \"{1}\" \"{2}\"", attr.Key, attr.Value, Util.EscapeQuote(attr.Comment));
                    }
                    return;
                }
            }
            {
                var grp = ann as Group;
                if (grp != null)
                {
                    foreach (var attr in grp.Attributes)
                    {
                        m_TextWriter.WriteLine("#! ATTR \"{0}\" \"{1}\" \"{2}\"", attr.Key, attr.Value, Util.EscapeQuote(attr.Comment));
                    }
                    return;
                }
            }
            throw new InvalidOperationException("Try to output attributes of a non-annotation object.");
        }

    }
}
