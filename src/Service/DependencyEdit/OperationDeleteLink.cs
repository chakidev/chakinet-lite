using ChaKi.Entity.Corpora.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationDeleteLink : Operation
    {
        private int m_LinkDocId;
        private int m_LinkFromSegStartChar;
        private int m_LinkFromSegEndChar;
        private string m_LinkFromSegTagName;
        private int m_LinkToSegStartChar;
        private int m_LinkToSegEndChar;
        private string m_LinkToSegTagName;
        private string m_LinkTagName;
        private Dictionary<string, string[]> m_Attrs;

        public OperationDeleteLink(Link link)
        {
            m_LinkDocId = link.From.Doc.ID;
            m_LinkFromSegStartChar = link.From.StartChar;
            m_LinkFromSegEndChar = link.From.EndChar;
            m_LinkFromSegTagName = link.From.Tag.Name;
            m_LinkToSegStartChar = link.To.StartChar;
            m_LinkToSegEndChar = link.To.EndChar;
            m_LinkToSegTagName = link.To.Tag.Name;
            m_LinkTagName = link.Tag.Name;
            m_Attrs = new Dictionary<string, string[]>();
        }

        public override void Execute(DepEditContext ctx)
        {
            var fromseg = FindSegmentBetween(ctx, m_LinkDocId, m_LinkFromSegStartChar, m_LinkFromSegEndChar, m_LinkFromSegTagName);
            if (fromseg == null)
            {
                throw new Exception($"From segment([{m_LinkFromSegStartChar},{m_LinkFromSegStartChar}]{m_LinkFromSegTagName}) not found");
            }
            var toseg = FindSegmentBetween(ctx, m_LinkDocId, m_LinkToSegStartChar, m_LinkToSegEndChar, m_LinkToSegTagName);
            if (toseg == null)
            {
                throw new Exception($"To segment([{m_LinkToSegStartChar},{m_LinkToSegStartChar}]{m_LinkToSegTagName}) not found");
            }
            var link = FindLink(ctx, fromseg, toseg, m_LinkTagName);
            if (link == null)
            {
                throw new Exception($"Link not found. from=[{fromseg.ID}], to=[{toseg.ID}], tag=[{m_LinkTagName}]");
            }

            // First, Delete SegmentAttribute List from DB
            m_Attrs.Clear();
            foreach (LinkAttribute a in link.Attributes)
            {
                m_Attrs.Add(a.Key, new string[] { a.Value, a.Comment });
                ctx.Delete(a);
                ctx.Flush();
            }
            ctx.Delete(link);
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            var link = new Link();
            link.FromSentence = ctx.Sen;
            link.ToSentence = ctx.Sen;
            var tag = FindLinkTag(ctx, m_LinkTagName);
            if (tag == null)
            {
                tag = new Tag(Tag.LINK, m_LinkTagName) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                ctx.Proj.TagSetList[0].AddTag(tag);
                ctx.Save(tag);
            }
            link.Tag = tag;
            var fromseg = FindSegmentBetween(ctx, m_LinkDocId, m_LinkFromSegStartChar, m_LinkFromSegEndChar, m_LinkFromSegTagName);
            if (fromseg == null)
            {
                throw new Exception($"From segment([{m_LinkFromSegStartChar},{m_LinkFromSegStartChar}]{m_LinkFromSegTagName}) not found");
            }
            link.From = fromseg;
            var toseg = FindSegmentBetween(ctx, m_LinkDocId, m_LinkToSegStartChar, m_LinkToSegEndChar, m_LinkToSegTagName);
            if (toseg == null)
            {
                throw new Exception($"To segment([{m_LinkToSegStartChar},{m_LinkToSegStartChar}]{m_LinkToSegTagName}) not found");
            }
            link.To = toseg;
            link.Proj = ctx.Proj;
            link.User = ctx.User;
            link.Version = link.Tag.Version;
            foreach (var pair in m_Attrs)
            {
                var attr = new LinkAttribute()
                {
                    Target = link,
                    Proj = link.Proj,
                    User = link.User,
                    Version = link.Version,
                    Key = pair.Key,
                    Value = pair.Value[0],
                    Comment = pair.Value[1]
                };
                link.Attributes.Add(attr);
            }
            ctx.Save(link);
            foreach (var attr in link.Attributes)
            {
                attr.Target.ID = link.ID;
                ctx.Save(attr);
            }
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return $"#svc.DeleteLink({m_LinkDocId},{m_LinkFromSegStartChar},{m_LinkFromSegEndChar},{m_LinkToSegStartChar},{m_LinkToSegEndChar}\"{m_LinkTagName}\") : not supported";
        }

        public override string ToString()
        {
            return $"{{DeleteLink:docid={m_LinkDocId},from=[{m_LinkFromSegStartChar},{m_LinkFromSegEndChar}],to=[{m_LinkToSegStartChar},{m_LinkToSegEndChar}], tag={m_LinkTagName}}}";
        }
    }
}
