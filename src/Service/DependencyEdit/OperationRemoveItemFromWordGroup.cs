using System;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;
using System.Collections.Generic;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationRemoveItemFromWordGroup : Operation
    {
        private CharRange m_Range;
        private Group m_Group;
        private int m_SegIndex;  // Groupの中の何番目のSegmentを削除したか
        private string m_TagName;
        private Dictionary<string, string[]> m_Attrs;

        public OperationRemoveItemFromWordGroup(Group grp, Segment seg)
        {
            m_Group = grp;
            m_SegIndex = m_Group.Tags.IndexOf(seg);
            if (m_SegIndex < 0)
            {
                throw new InvalidOperationException("The segment is not a member of group");
            }
            m_Range = new CharRange(seg.StartChar, seg.EndChar);
            m_Attrs = new Dictionary<string, string[]>();
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
            m_Attrs.Clear();
            foreach (var attr in segToRemove.Attributes) 
            {
                m_Attrs.Add(attr.Key, new string[] { attr.Value, attr.Comment });
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
                foreach (var attr in m_Group.Attributes)
                {
                    ctx.Save(attr);
                }
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
            foreach (var pair in m_Attrs)
            {
                var attr = new SegmentAttribute()
                {
                    Target = seg, Proj = seg.Proj, User = seg.User, Version = seg.Version,
                    Key = pair.Key, Value = pair.Value[0], Comment = pair.Value[1]
                };
                seg.Attributes.Add(attr);
            }
            ctx.Save(seg);
            m_Group.Tags.Add(seg);
            foreach (var attr in seg.Attributes)
            {
                attr.Target.ID = seg.ID;
                ctx.Save(attr);
            }
            m_SegIndex = m_Group.Tags.IndexOf(seg); // Group.Tagsの順序が最初のExecute時から変化する可能性があるため更新する.
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
