using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using NHibernate;
using ChaKi.Entity.Corpora.Annotations;
using System.Diagnostics;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationCreateWordGroup : Operation
    {
        private CharRange m_Range;
        private Group m_Group;
        private Segment m_Segment;
        private string m_TagName;
        private Dictionary<string, string[]> m_SegAttrs;
        private Dictionary<string, string[]> m_GroupAttrs;

        public OperationCreateWordGroup(CharRange range, string tagName)
        {
            m_Range = range;
            m_TagName = tagName;
            m_SegAttrs = new Dictionary<string, string[]>();
            m_GroupAttrs = new Dictionary<string, string[]>();
        }

        public override void Execute(DepEditContext ctx)
        {
            Segment seg = new Segment();
            seg.Sentence = ctx.Sen;
            seg.Doc = ctx.Sen.ParentDoc;
            seg.Tag = ctx.Proj.FindTag(Tag.SEGMENT, m_TagName);
            if (seg.Tag == null)
            {
                var t = new Tag(Tag.SEGMENT, m_TagName) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                ctx.Proj.TagSetList[0].AddTag(t);
                seg.Tag = t;
                ctx.Save(t);
            }
            seg.StartChar = m_Range.Start.Value + ctx.Sen.StartChar;
            seg.EndChar = m_Range.End.Value + ctx.Sen.StartChar;
            seg.Proj = ctx.Proj;
            seg.User = ctx.User;
            seg.Version = seg.Tag.Version;
            foreach (var pair in m_SegAttrs)
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
            if (m_Group == null)
            {
                Group grp = new Group();
                grp.Tag = ctx.Proj.FindTag(Tag.GROUP, m_TagName);
                if (grp.Tag == null)
                {
                    var t = new Tag(Tag.GROUP, m_TagName) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                    ctx.Proj.TagSetList[0].AddTag(t);
                    grp.Tag = t;
                    ctx.Save(t);
                }
                grp.Proj = ctx.Proj;
                grp.User = ctx.User;
                grp.Version = grp.Tag.Version;
                // ここは初回生成時しか通らないので、grp.Attributeを復活させる必要はない.
                m_Group = grp;
            }
            // Undo->Redoの場合は再生成せずにセーブされていたGroupを復活させる.
            // (Createしなおすと、AddItemのRedoで親Groupが異なってしまう）
            m_Group.Tags.Add(seg);
            foreach (var pair in m_GroupAttrs)
            {
                var attr = new GroupAttribute()
                {
                    Target = m_Group,
                    Proj = m_Group.Proj,
                    User = m_Group.User,
                    Version = m_Group.Tag.Version,
                    Key = pair.Key,
                    Value = pair.Value[0],
                    Comment = pair.Value[1]
                };
                m_Group.Attributes.Add(attr);
                ctx.Save(attr);
            }
            ctx.Save(m_Group);
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            if (m_Group == null || m_Segment == null)
            {
                throw new NullReferenceException();
            }
            Debug.Assert(m_Group.Tags.Count == 1);
            // m_Segment/m_Groupに属性があればUndoの前に保存し、DBからも削除する.
            m_SegAttrs.Clear();
            foreach (var attr in m_Segment.Attributes)
            {
                m_SegAttrs.Add(attr.Key, new string[] { attr.Value, attr.Comment });
                ctx.Delete(attr);
                ctx.Flush();
            }
            m_Segment = m_Group.Tags[0] as Segment;
            ctx.Delete(m_Segment);
            m_Group.Tags.Clear();
            m_GroupAttrs.Clear();
            foreach (var attr in m_Group.Attributes)
            {
                m_GroupAttrs.Add(attr.Key, new string[] { attr.Value, attr.Comment });
                ctx.Delete(attr);
                ctx.Flush();
            }
            m_Group.Attributes.Clear();
            ctx.Delete(m_Group);
            m_Segment = null;
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("#svc.CreateWordGroup(c+({0}), c+({1}), \"{2}\") : not supported",
                m_Range.Start + ctx.Sen.StartChar - ctx.CharOffset,
                m_Range.End + ctx.Sen.StartChar - ctx.CharOffset,
                m_TagName);
        }

        public override string ToString()
        {
            return string.Format("{{CreateWordGroup:{0}}}", m_Range);
        }
    }
}
