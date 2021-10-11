using ChaKi.Entity.Corpora.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationCreateLink : Operation
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

        public OperationCreateLink(Segment fromseg, Segment toseg, string tagname)
        {
            m_LinkDocId = fromseg.Doc.ID;
            m_LinkFromSegStartChar = fromseg.StartChar;
            m_LinkFromSegEndChar = fromseg.EndChar;
            m_LinkFromSegTagName = fromseg.Tag.Name;
            m_LinkToSegStartChar = toseg.StartChar;
            m_LinkToSegEndChar = toseg.EndChar;
            m_LinkToSegTagName = toseg.Tag.Name;
            m_LinkTagName = tagname;
            m_Attrs = new Dictionary<string, string[]>();
        }

        public override void Execute(DepEditContext ctx)
        {
            var link = new Link();
            link.FromSentence = ctx.Sen;
            link.ToSentence = ctx.Sen;
            link.Tag = ctx.Proj.FindTag(Tag.LINK, m_LinkTagName);
            if (link.Tag == null)
            {
                Tag t = new Tag(Tag.LINK, m_LinkTagName) { Parent = ctx.Proj.TagSetList[0], Version = ctx.TSVersion };
                ctx.Proj.TagSetList[0].AddTag(t);
                link.Tag = t;
                ctx.Save(t);
            }
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
                    Proj = ctx.Proj,
                    User = ctx.User,
                    Version = link.Tag.Version,
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

        public override void UnExecute(DepEditContext ctx)
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

            // m_Linkに属性があればUndoの前に保存し、DBからも削除する.
            m_Attrs.Clear();
            foreach (var attr in link.Attributes)
            {
                m_Attrs.Add(attr.Key, new string[] { attr.Value, attr.Comment });
                ctx.Delete(attr);
                ctx.Flush();
            }
            link.Attributes.Clear();
            ctx.Flush();
            ctx.Delete(link);
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return $"#svc.CreateLink(({m_LinkDocId}),({m_LinkFromSegStartChar}),({m_LinkFromSegEndChar}),\"{m_LinkFromSegTagName}\",({m_LinkToSegStartChar}),({m_LinkToSegEndChar}),\"{m_LinkToSegTagName}\",\"{m_LinkTagName}\")";
        }

        public override string ToString()
        {
            return $"{{CreateLink: docid={m_LinkDocId},From=[{m_LinkFromSegStartChar},{m_LinkFromSegEndChar}],To=[{m_LinkToSegStartChar},{m_LinkToSegEndChar}], Tag={m_LinkTagName}}}";
        }
    }
}
