using ChaKi.Entity.Corpora.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationChangeSegmentTag : Operation
    {
        private Segment m_Segment;
        private int m_SegDocId;
        private int m_SegStartChar;
        private int m_SegEndChar;
        private string m_OldTagName;
        private string m_NewTagName;

        public OperationChangeSegmentTag(Segment seg, string oldTagName, string newTagName)
        {
            m_Segment = seg;
            m_SegDocId = seg.Doc.ID;
            m_SegStartChar = seg.StartChar;
            m_SegEndChar = seg.EndChar;
            m_OldTagName = oldTagName;
            m_NewTagName = newTagName;
        }


        public override void Execute(DepEditContext ctx)
        {
            if (m_OldTagName == null || m_NewTagName == null)
            {
                throw new NullReferenceException();
            }
            var seg = FindTargetSegment(ctx, m_OldTagName);
            var newTag = FindSegmentTag(ctx, m_NewTagName);
            if (newTag == null)
            {
                newTag = new Tag(Tag.SEGMENT, m_NewTagName) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                ctx.Proj.TagSetList[0].AddTag(newTag);
                ctx.Save(newTag);
            }
            seg.Tag = newTag;
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            if (m_OldTagName == null || m_NewTagName == null)
            {
                throw new NullReferenceException();
            }
            var seg = FindTargetSegment(ctx, m_NewTagName);
            var oldTag = FindSegmentTag(ctx, m_OldTagName);
            if (oldTag == null)
            {
                throw new Exception(string.Format("Cannot Find Segment Tag: {0}", m_OldTagName));
            }
            seg.Tag = oldTag;
            ctx.Flush();
        }

        private Segment FindTargetSegment(DepEditContext ctx, string tagname)
        {
            var seg = FindSegmentBetween(ctx, m_SegDocId, m_SegStartChar, m_SegEndChar, tagname);
            if (seg == null)
            {
                throw new Exception($"Segment not found. from=[{m_SegStartChar}], to=[{m_SegEndChar}], tag=[{tagname}]");
            }
            return seg;
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format($"#svc.ChangeSegmentTag({m_SegDocId},{m_SegStartChar},{m_SegEndChar},\"{m_OldTagName}\",\"{m_NewTagName}\") : not supported");
        }
    }
}