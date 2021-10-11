using System;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Corpora;
using NHibernate;
using System.Collections.Generic;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationDeleteSegment : Operation
    {
        private int m_DocId;
        private CharRange m_Range;
        private Tag m_Tag;
        private Dictionary<string, string[]> m_Attrs;

        public OperationDeleteSegment(Segment seg)
        {
            m_DocId = seg.Doc.ID;
            int offs = seg.Sentence.StartChar;
            m_Range = new CharRange(seg.StartChar - offs, seg.EndChar - offs);
            m_Tag = seg.Tag;
            m_Attrs = new Dictionary<string, string[]>();
        }

        public override void Execute(DepEditContext ctx)
        {
            var from_pos = m_Range.Start.Value + ctx.Sen.StartChar;
            var to_pos = m_Range.End.Value + ctx.Sen.StartChar;
            var seg = FindSegmentBetween(ctx, m_DocId, from_pos, to_pos, m_Tag.Name);
            if (seg == null)
            {
                throw new Exception($"Segment([{from_pos},{to_pos}]{m_Tag.Name}) not found");
            }

            // First, Delete SegmentAttribute List from DB
            m_Attrs.Clear();
            foreach (SegmentAttribute a in seg.Attributes)
            {
                m_Attrs.Add(a.Key, new string[] { a.Value, a.Comment });
                ctx.Delete(a);
            }
            ctx.Session.Refresh(seg);
            ctx.Flush();
            // Then, delete the Segment
            ctx.Delete(seg);
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            Segment seg = new Segment();
            seg.Sentence = ctx.Sen;
            seg.Doc = ctx.Sen.ParentDoc;
            seg.Tag = m_Tag;
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

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("#svc.DeleteSegment(c+({0}), c+({1}), \"{2}\") : not supported",
                m_Range.Start.Value + ctx.Sen.StartChar - ctx.CharOffset,
                m_Range.End.Value + ctx.Sen.StartChar - ctx.CharOffset,
                m_Tag.Name);
        }

        public override string ToString()
        {
            return string.Format("{{DeleteSegment:{0}}}", m_Range);
        }
    }
}
