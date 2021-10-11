using System;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Corpora;
using NHibernate;
using System.Collections.Generic;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationCreateSegment : Operation
    {
        private int m_DocId;
        private CharRange m_Range;
        private string m_TagName;
        private Dictionary<string, string[]> m_Attrs;

        public OperationCreateSegment(CharRange range, string tagName)
        {
            m_Range = range;
            m_TagName = tagName;
            m_Attrs = new Dictionary<string, string[]>();
        }

        public override void Execute(DepEditContext ctx)
        {
            Segment seg = new Segment();
            seg.Sentence = ctx.Sen;
            seg.Doc = ctx.Sen.ParentDoc;
            m_DocId = seg.Doc.ID;
            seg.Tag = ctx.Proj.FindTag(Tag.SEGMENT, m_TagName);
            if (seg.Tag == null)
            {
                Tag t = new Tag(Tag.SEGMENT, m_TagName) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                ctx.Proj.TagSetList[0].AddTag(t);
                seg.Tag = t;
                ctx.Save(t);
            }
            seg.StartChar = m_Range.Start.Value + ctx.Sen.StartChar;
            seg.EndChar = m_Range.End.Value + ctx.Sen.StartChar;
            seg.Proj = ctx.Proj;
            seg.User = ctx.User;
            seg.Version = seg.Tag.Version;
            foreach (var pair in m_Attrs)
            {
                var attr = new SegmentAttribute()
                {
                    Target = seg,
                    Proj = seg.Proj,
                    User = seg.User,
                    Version = seg.Version,
                    Key = pair.Key,
                    Value = pair.Value[0],
                    Comment = pair.Value[1]
                };
                seg.Attributes.Add(attr);
            }
            ctx.Save(seg);
            foreach (var attr in seg.Attributes)
            {
                attr.Target.ID = seg.ID;
                ctx.Save(attr);
            }
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            var from_pos = m_Range.Start.Value + ctx.Sen.StartChar;
            var to_pos = m_Range.End.Value + ctx.Sen.StartChar;
            var seg = FindSegmentBetween(ctx, m_DocId, from_pos, to_pos, m_TagName);
            if (seg == null)
            {
                throw new Exception($"Segment([{from_pos},{to_pos}]{m_TagName}) not found");
            }

            // segに属性があればUndoの前に保存し、DBからも削除する.
            m_Attrs.Clear();
            foreach (var attr in seg.Attributes)
            {
                m_Attrs.Add(attr.Key, new string[] { attr.Value, attr.Comment });
                ctx.Delete(attr);
                ctx.Flush();
            }
            ctx.Delete(seg);
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("#svc.CreateSegment(c+({0}), c+({1}), \"{2}\") : not supported",
                m_Range.Start + ctx.Sen.StartChar - ctx.CharOffset,
                m_Range.End + ctx.Sen.StartChar - ctx.CharOffset,
                m_TagName);
        }

        public override string ToString()
        {
            return string.Format("{{CreateSegment:{0}}}", m_Range);
        }
    }
}
