using System;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;
using System.Collections.Generic;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationAddItemToWordGroup : Operation
    {
        private CharRange m_Range;
        private Group m_Group;
        private Segment m_Segment;
        private Dictionary<string, string[]> m_Attrs;

        public OperationAddItemToWordGroup(Group g, CharRange range)
        {
            m_Range = range;
            m_Group = g;
            m_Attrs = new Dictionary<string, string[]>();
        }

        public override void Execute(DepEditContext ctx)
        {
            if (m_Group == null || m_Range == null)
            {
                throw new NullReferenceException();
            }
            Segment seg = new Segment();
            seg.Sentence = ctx.Sen;
            seg.Doc = ctx.Sen.ParentDoc;
            seg.Tag = ctx.Proj.FindTag(Tag.SEGMENT, m_Group.Tag.Name);
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
            m_Segment = seg;
            m_Group.Tags.Add(seg);
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            if (m_Group == null || m_Segment == null)
            {
                throw new NullReferenceException();
            }

            // m_Segmentに属性があればUndoの前に保存し、DBからも削除する.
            m_Attrs.Clear();
            foreach (var attr in m_Segment.Attributes)
            {
                m_Attrs.Add(attr.Key, new string[] { attr.Value, attr.Comment });
                ctx.Delete(attr);
                ctx.Flush();
            }

            m_Group.Tags.Remove(m_Segment);
            ctx.Delete(m_Segment);
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("#svc.AddItemToWordGroup({0}, c+({1}), c+({2})) : not supported.",
                m_Group.ID, m_Range.Start + ctx.Sen.StartChar - ctx.CharOffset, m_Range.End + ctx.Sen.StartChar - ctx.CharOffset);
        }

        public override string ToString()
        {
            return string.Format("{{OperationAddItemToWordGroup:{0}}}", m_Range);
        }
    }
}
