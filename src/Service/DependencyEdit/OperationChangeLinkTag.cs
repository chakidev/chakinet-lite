using System;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationChangeLinkTag : Operation
    {
        private int m_LinkDocId;
        private int m_LinkFromSegStartChar;
        private int m_LinkFromSegEndChar;
        private string m_LinkFromSegTagName;
        private int m_LinkToSegStartChar;
        private int m_LinkToSegEndChar;
        private string m_LinkToSegTagName;
        private string m_OldTag;
        private string m_NewTag;

        public OperationChangeLinkTag(Link link, string oldTag, string newTag)
        {
            m_LinkDocId = link.From.Doc.ID;
            m_LinkFromSegStartChar = link.From.StartChar;
            m_LinkFromSegEndChar = link.From.EndChar;
            m_LinkFromSegTagName = link.From.Tag.Name;
            m_LinkToSegStartChar = link.To.StartChar;
            m_LinkToSegEndChar = link.To.EndChar;
            m_LinkToSegTagName = link.To.Tag.Name;
            m_OldTag = oldTag;
            m_NewTag = newTag;
        }

        public override void Execute(DepEditContext ctx)
        {
            if (m_OldTag == null || m_NewTag == null)
            {
                throw new NullReferenceException();
            }
            var link = FindTargetLink(ctx, m_OldTag);
            Tag newTag = FindLinkTag(ctx, m_NewTag);
            if (newTag == null)
            {
                newTag = new Tag(Tag.LINK, m_NewTag) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                ctx.Proj.TagSetList[0].AddTag(newTag);
                ctx.Save(newTag);
            }
            link.Tag = newTag;
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            if (m_OldTag == null || m_NewTag == null)
            {
                throw new NullReferenceException();
            }
            var link = FindTargetLink(ctx, m_NewTag);
            Tag oldTag = FindLinkTag(ctx, m_OldTag);
            if (oldTag == null)
            {
                throw new Exception(string.Format("Cannot Find Link Tag: {0}", m_OldTag));
            }
            link.Tag = oldTag;
            ctx.Flush();
        }

        private Link FindTargetLink(DepEditContext ctx, string linktagname)
        {
            var from = FindSegmentBetween(ctx, m_LinkDocId, m_LinkFromSegStartChar, m_LinkFromSegEndChar, m_LinkFromSegTagName);
            var to = FindSegmentBetween(ctx, m_LinkDocId, m_LinkToSegStartChar, m_LinkToSegEndChar, m_LinkToSegTagName);
            if (from == null || to == null)
            {
                throw new Exception($"Segment not found. from=[{from}], to=[{to}]");
            }
            var link = FindLink(ctx, from, to, linktagname);
            if (link == null)
            {
                throw new Exception($"Link not found. from=[{from.ID}], to=[{to.ID}], tag=[{linktagname}]");
            }
            return link;
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return $"#svc.ChangeLinkTag({m_LinkDocId},{m_LinkFromSegStartChar},{m_LinkFromSegEndChar},{m_LinkToSegStartChar},{m_LinkToSegEndChar}\"{m_OldTag}\",\"{m_NewTag}\") : not supported";
        }

        public override string ToString()
        {
            return $"{{OperationChangeTag: docid={m_LinkDocId}, link=[From=[{m_LinkFromSegStartChar},{m_LinkFromSegEndChar}], To=[{m_LinkToSegStartChar},{m_LinkToSegEndChar}]], oldTag={m_OldTag}, newTag={m_NewTag}}}";
        }
    }
}
