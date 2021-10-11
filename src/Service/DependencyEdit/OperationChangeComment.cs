using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora.Annotations;

namespace ChaKi.Service.DependencyEdit
{
    class OperationChangeComment : Operation
    {
        private Annotation m_Ann;
        private string m_OldComment;
        private string m_NewComment;

        public OperationChangeComment(Annotation a, string newComment)
        {
            m_OldComment = a.Comment;
            m_NewComment = newComment;
            m_Ann = a;
        }

        public override void Execute(DepEditContext ctx)
        {
            m_Ann.Comment = m_NewComment;
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            m_Ann.Comment = m_OldComment;
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            if (m_Ann is Segment)
            {
                Segment seg = (Segment)m_Ann;
                return string.Format("#svc.ChangeSegmentComment({0},\"{1}\") : not supported.", seg.ID, m_NewComment);
            }
            else if (m_Ann is Link)
            {
                Link lnk = (Link)m_Ann;
                return string.Format("#svc.ChangeLinkComment({0},\"{1}\") : not supported.", lnk.ID, m_NewComment);
            }
            else if (m_Ann is Group)
            {
                Group grp = (Group)m_Ann;
                return string.Format("#svc.ChangeGroupComment({0},\"{1}\") : not supported.", grp.ID, m_NewComment);
            }
            return string.Empty;
        }
    }
}
