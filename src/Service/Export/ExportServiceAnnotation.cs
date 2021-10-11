using ChaKi.Common.Settings;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Kwic;
using ChaKi.Service.Common;
using ChaKi.Service.Readers;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Export
{
    public class ExportServiceAnnotation : ExportServiceCabocha
    {
        public ExportServiceAnnotation(TextWriter wr)
            : base(wr)
        {
            if (GitSettings.Current.ExportBunsetsuSegmentsAndLinks)
            {
                // 冒頭にコメント行としてBunsetsu出力を示す
                wr.WriteLine("## ExportBunsetsuSegmentsAndLinks");
            }
        }

        private void Initialize(TextWriter wr)
        {
            m_TextWriter = wr;
            m_CurrentDocId = -1;

            m_DocOffsetCache = new Dictionary<Document, int>();

            Util.Reset();
        }

        public override void ExportItem(KwicItem ki)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            var q = m_Session.CreateQuery(string.Format("from Sentence where ID={0}", ki.SenID));
            var sen = q.UniqueResult<Sentence>();
            if (sen == null)
            {
                throw new Exception(string.Format("Sentence not found. Corpus={0}, senID={1}", ki.Crps.Name, ki.SenID));
            }

            ExportItem(ki.Crps, sen);
        }

        private StringWriter m_TempWriter = new StringWriter();

        public override void ExportItem(Corpus crps, Sentence sen)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

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

            // Annotation単独エクスポートの場合は、Annotationのある文にStart Tagを出力する.
            m_TempWriter.GetStringBuilder().Clear();
            var orgWriter = m_TextWriter;
            m_TextWriter = m_TempWriter;
            // これ以降このメソッドの末尾まで、Sentenceに対するAnnotationの出力は一旦StringWriterに送られる

            // SENTENCETAGの出力
            foreach (var a in sen.Attributes)
            {
                string csa = string.Format("{0}:{1}", crps.Name, a.ID);
                if (m_SentenceTags.TryGetValue(csa, out pair))
                {
                    m_TextWriter.WriteLine("#! SENTENCETAG {0}", pair.Seqid);
                }
            }

            // Annotationの出力
            if (GitSettings.Current.ExportBunsetsuSegmentsAndLinks)
            {
                WriteBunsetsuAndAnnotations(sen);
            }
            else
            {
                WriteAnnotations(sen);
            }

            m_TempWriter.Flush();
            var buf = m_TempWriter.ToString();
            if (buf.Length > 0)
            {
                // 出力があれば BOS ~ EOSを出力する.
                m_TextWriter = orgWriter;
                WriteBos(sen.Pos);
                m_TextWriter.Write(buf);
                WriteEos();
            }
            m_TextWriter = orgWriter;
        }

        // Annotationがある場合のみBOSをsenno付きで出力
        private void WriteBos(int senno)
        {
            m_TextWriter.WriteLine($"BOS {senno}");
        }

        // SegmentIDの出力方式をIndexではなく"Segment指定文字列"に変更
        protected override string GetSegmentHash(Segment seg, int index, int senOffset)
        {
            return AnnotationReader.GetSegmentHash(seg, senOffset);
        }

        // WriteAnnotationsとほぼ同じだが、Bunsetsu seg&linkも出力する.
        private bool WriteBunsetsuAndAnnotations(Sentence sen)
        {
            var hasAnnotation = false;
            var grps = Util.RetrieveWordGroups(m_Session, sen, m_ProjId);

            var senOffset = sen.StartChar;
            var segno = 0; // 本メソッドローカルのSegment番号（文内）
            var seg_index = new Dictionary<long, string>();  // Key=Segment.ID; Value=segno in sentence

            var sb = new StringBuilder();

            // Bunsetsu Segmentを出力する
            var segs = Util.RetrieveBunsetsuRaw(m_Session, sen);   // Seg.ID, Seg.tagname, Seg.StartChar, Seg.EndChar, Seg.Comment
            foreach (var seg in segs)
            {
                m_TextWriter.WriteLine($"#! SEGMENT_S Bunsetsu {seg.StartChar - senOffset} {seg.EndChar - senOffset} \"{seg.GetNormalizedCommentString()}\"");
                seg_index[seg.ID] = GetSegmentHash(seg, segno, senOffset);
                segno++;
                WriteAttributes(seg);
                hasAnnotation = true;
            }
            // Bunsetsuに対するLink(Dependency)を出力する
            foreach (var seg in segs)
            {
                var link = Util.RetrieveBunsetsuLinkRaw(m_Session, sen, seg);
                if (link == null) continue;
                if (seg_index.TryGetValue(link.From.ID, out var from_idx)
                    && seg_index.TryGetValue(link.To.ID, out var to_idx))
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

            // GroupとそのSegmentを出力する
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
            var segids = Util.RetrieveMiscSegments(m_Session, sen, m_ProjId);   // Seg.ID, Seg.tagname, Seg.StartChar, Seg.EndChar, Seg.Comment
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
    }
}
