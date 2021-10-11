using System;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;
using NHibernate.Mapping;
using System.Collections.Generic;
using System.Text;
using ChaKi.Service.Readers;
using System.IO;
using ChaKi.Service.Common;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationUpdateAllBunsetsu : Operation
    {
        private Sentence m_Sentence;
        private string m_InputString;
        private CabochaBunsetsuList m_BunsetsuListOld;
        private CabochaBunsetsuList m_BunsetsuListNew;

        public OperationUpdateAllBunsetsu(Sentence sen, string inputString)
        {
            m_Sentence = sen;

            m_BunsetsuListOld = null;

            // inputStringを元に、編集後の形に対応するCabochaBunsetsuListを生成・保存
            m_InputString = inputString;
            using (var rdr = new StringReader(inputString))
            {
                m_BunsetsuListNew = CabochaReader.ToBunsetsuList(sen, rdr);
            }
            if (m_BunsetsuListNew == null)
            {
                throw new Exception("OperationUpdateAllBunsetsu: Error while parsing Cabocha output string.");
            }
        }

        public override void Execute(DepEditContext ctx)
        {
            if (m_BunsetsuListOld == null)
            {
                // 現在のBunsetsu Segment/LinkからCabochaBunsetsuListを生成してセーブする.
                m_BunsetsuListOld = CabochaBunsetsuList.CreateFromSentence(m_Sentence, ctx.Session);
            }
            // 現在の文節Segment/係り受けLinkをすべて削除する.
            RemoveBunsetsuSegmentLink(ctx);

            // m_BunsetsuListNewに基づいて新しい文節Segment/係り受けLinkを生成する.
            ApplyBunsetsuList(ctx, m_Sentence, m_BunsetsuListNew);

            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            // 現在の文節Segment/係り受けLinkをすべて削除する.
            RemoveBunsetsuSegmentLink(ctx);

            // m_BunsetsuListOldに基づいて新しい文節Segment/係り受けLinkを生成する.
            ApplyBunsetsuList(ctx, m_Sentence, m_BunsetsuListOld);

            ctx.Flush();
        }

        // m_Sentenceに付加されているBunsetsu Segment/Linkをすべて削除する.
        private void RemoveBunsetsuSegmentLink(DepEditContext ctx)
        {
            // まずBunsetsu Segmentをすべて得る.
            var q = ctx.Session.CreateQuery($"from Segment where Tag.Name='Bunsetsu' and Sentence.ID={m_Sentence.ID} and Proj.ID={ctx.Proj.ID}");
            var segs = q.List<Segment>();
            var segids = Util.BuildSegmentIDList(segs);

            // Bunsetsu Segmentに関連するLinkを得て削除する.
            var q2 = ctx.Session.CreateQuery($"from Link l where l.From.ID in {segids} and l.Proj.ID={ctx.Proj.ID}");
            var links = q2.List<Link>();
            foreach (var link in links)
            {
                // LinkAttributeを先に削除
                foreach (var attr in link.Attributes)
                {
                    ctx.Delete(attr);
                    ctx.Flush();
                }
                link.Attributes.Clear();
                ctx.Flush();
                ctx.Delete(link);
                ctx.Flush();
            }

            // Bunsetsu Segmentをすべて削除する.
            foreach (var seg in segs)
            {
                // SegmentAttributeを先に削除
                foreach (var attr in seg.Attributes)
                {
                    ctx.Delete(attr);
                    ctx.Flush();
                }
                seg.Attributes.Clear();
                ctx.Flush();
                ctx.Delete(seg);
                ctx.Flush();
            }

            var words = q.List<Word>();
            foreach (var w in words)
            {
                w.Bunsetsu = null;
            }
        }

        // bunsetsuListに基づいて新たにSegment/Linkを生成し、WordにBunsetsuを設定する
        private void ApplyBunsetsuList(DepEditContext ctx, Sentence sen, CabochaBunsetsuList bunsetsuList)
        {
            var bunsetsuTag = FindBunsetsuTag(ctx);
            var segments = new Dictionary<int, Segment>();

            int sentenceStartChar = sen.StartChar;
            foreach (var b in bunsetsuList.Values)
            {
                // 新しいSegment
                Segment seg = new Segment();
                seg.StartChar = sentenceStartChar + b.StartPos;
                seg.EndChar = sentenceStartChar + b.EndPos;
                seg.Tag = bunsetsuTag;
                seg.Doc = sen.ParentDoc;
                seg.Proj = ctx.Proj;
                seg.User = ctx.User;
                seg.Version = ctx.TSVersion;
                seg.Sentence = sen;
                // segをDBに保存
                ctx.Save(seg);
                foreach (var w in b.Words)
                {
                    w.Bunsetsu = seg;
                }
                segments.Add(b.BunsetsuPos, seg);
            }

            // SegmentがそろったのでLinkを生成する
            foreach (var b in bunsetsuList.Values)
            {
                Link newlink = new Link();
                if (b.DependsTo < 0)
                {
                    continue; 
                }
                newlink.From = segments[b.BunsetsuPos];
                newlink.To = segments[b.DependsTo];
                newlink.Tag = FindLinkTag(ctx, b.DependsAs);
                newlink.FromSentence = sen;
                newlink.ToSentence = sen;
                newlink.Proj = ctx.Proj;
                newlink.User = ctx.User;
                newlink.Version = ctx.TSVersion;
                ctx.Save(newlink);
            }
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("#svc.OperationUpdateAllBunsetsu : not supported.");
        }

        public override string ToString()
        {
            return string.Format("{{OperationUpdateAllBunsetsu \"{0}\"}}\n", m_InputString);
        }
    }
}
