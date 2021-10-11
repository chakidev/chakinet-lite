using System;
using ChaKi.Entity.Corpora.Annotations;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationChangeWordGroupTag : Operation
    {
        private Group m_Group;
        private string m_OldTagName;
        private string m_NewTagName;

        public OperationChangeWordGroupTag(Group grp, string oldTagName, string newTagName)
        {
            m_Group = grp;
            m_OldTagName = oldTagName;
            m_NewTagName = newTagName;
        }

        public override void Execute(DepEditContext ctx)
        {
            ChangeTag(ctx, m_NewTagName);
        }

        public override void UnExecute(DepEditContext ctx)
        {
            ChangeTag(ctx, m_OldTagName);
        }

        private void ChangeTag(DepEditContext ctx, string tagName)
        {
            if (m_Group == null)
            {
                throw new NullReferenceException();
            }
            Tag groupTag = ctx.TagSet.FindTag(Tag.GROUP, tagName);
            if (groupTag == null) {
                throw new Exception(string.Format("Cannot find Group tag: {0}", tagName));
            }
            Tag segTag = ctx.TagSet.FindTag(Tag.SEGMENT, tagName);
            if (segTag == null) {
                throw new Exception(string.Format("Cannot find Segment tag: {0}", tagName));
            }
            m_Group.Tag = groupTag;
            foreach (Annotation ann in m_Group.Tags)
            {
                if (!(ann is Segment)) continue;
                ann.Tag = segTag;
            }
            Operation.LastTagUsed = tagName;
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("#svc.ChangeWordGroupTag({0},\"{1}\",\"{2}\") : not supported", m_Group.ID, m_OldTagName, m_NewTagName);
        }

    }
}
