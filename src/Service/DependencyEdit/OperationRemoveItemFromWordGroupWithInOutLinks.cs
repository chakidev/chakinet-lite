using System;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;
using System.Collections.Generic;

namespace ChaKi.Service.DependencyEdit
{
    // This class is under-construction.
    // 
    internal class OperationRemoveItemFromWordGroupWithInOutLinks : Operation
    {
        private CharRange m_Range;
        private Group m_Group;
        private List<Link> m_Links;
        private int m_SegIndex;  // Groupの中の何番目のSegmentを削除したか
        private string m_TagName;

        public OperationRemoveItemFromWordGroupWithInOutLinks(Group grp, Segment seg)
        {
            m_Group = grp;
            m_SegIndex = m_Group.Tags.IndexOf(seg);
            if (m_SegIndex < 0)
            {
                throw new InvalidOperationException("The segment is not a member of group");
            }
            m_Range = new CharRange(seg.StartChar, seg.EndChar);
        }

        public override void Execute(DepEditContext ctx)
        {
            if (m_Group == null)
            {
                throw new NullReferenceException();
            }
            Segment segToRemove = m_Group.Tags[m_SegIndex] as Segment;
            if (segToRemove == null)
            {
                return;
            }
            m_TagName = segToRemove.Tag.Name;
            m_Group.Tags.RemoveAt(m_SegIndex);

            // segToRemoveのAttributeを先に削除（なぜかAttributeを削除してからでないとSegmentを削除できない. Link, Groupも同じ）
            foreach (var attr in segToRemove.Attributes)
            {
                ctx.Delete(attr);
                ctx.Flush();
            }
            segToRemove.Attributes.Clear();

            ctx.Delete(segToRemove);
            if (m_Group.Tags.Count == 0)
            {
                foreach (var attr in m_Group.Attributes)
                {
                    ctx.Delete(attr);
                    ctx.Flush();
                }
                segToRemove.Attributes.Clear();
                ctx.Delete(m_Group);
            }
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            if (m_Group == null || m_Range == null)
            {
                throw new NullReferenceException();
            }
            if (m_Group.Tags.Count == 0)
            {
                // Execute時にGroup自体も削除されていた場合、同じGroupオブジェクトを復活させる.
                // (CreateWordGroup時と同様。newしてはならない)
                ctx.Save(m_Group);
            }
            Segment seg = new Segment();
            seg.Sentence = ctx.Sen;
            seg.Doc = ctx.Sen.ParentDoc;
            seg.Tag = ctx.Proj.FindTag(Tag.SEGMENT, m_TagName);
            seg.StartChar = m_Range.Start.Value;
            seg.EndChar = m_Range.End.Value;
            seg.Proj = ctx.Proj;
            seg.User = ctx.User;
            seg.Version = seg.Tag.Version;
            ctx.Save(seg);
            m_Group.Tags.Add(seg);
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("#svc.RemoveItemFromWordGroup({0}, {1})", m_Group.ID, m_SegIndex);
        }

        public override string ToString()
        {
            return string.Format("{{OperationRemoveItemFromWordGroup:{0}}}", m_Range);
        }
    }
}
