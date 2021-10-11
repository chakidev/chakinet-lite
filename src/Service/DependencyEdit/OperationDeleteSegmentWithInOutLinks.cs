using System;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Corpora;
using NHibernate;
using System.Collections.Generic;

namespace ChaKi.Service.DependencyEdit
{
    // This class is under-construction.
    // 
    internal class OperationDeleteSegmentWithInOutLinks : Operation
    {
        private CharRange m_Range;
        private Segment m_Segment;
        private Tag m_Tag;
        private List<Link> m_Links;

        public OperationDeleteSegmentWithInOutLinks(Segment seg)
        {
            m_Segment = seg;
            int offs = seg.Sentence.StartChar;
            m_Range = new CharRange(seg.StartChar - offs, seg.EndChar - offs);
            m_Tag = seg.Tag;
        }

        public override void Execute(DepEditContext ctx)
        {
            // First, Delete SegmentAttribute List from DB (This cannot be undone)
            foreach (SegmentAttribute a in m_Segment.Attributes)
            {
                ctx.Delete(a);
            }
            ctx.Session.Refresh(m_Segment);
            ctx.Flush();
            // Then, delete the Segment
            ctx.Delete(m_Segment);
            m_Segment = null;
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
            ctx.Save(seg);
            m_Segment = seg;
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
