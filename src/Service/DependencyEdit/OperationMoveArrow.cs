using System;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationMoveArrow : Operation
    {
        private Link m_Link;
        private Segment m_SegmentOld;
        private Segment m_SegmentNew;
        private ChangeLinkInfo? m_UndoOperation;
        private ChangeLinkInfo? m_RedoOperation;

        public OperationMoveArrow(Link lnk, Segment segOld, Segment segNew)
        {
            m_Link = lnk;
            m_SegmentOld = segOld;
            m_SegmentNew = segNew;
            m_UndoOperation = null;
        }

        public override void Execute(DepEditContext ctx)
        {
            if (m_RedoOperation != null && m_RedoOperation.HasValue)
            {
                // Redoの場合
                ChangeLink(ctx, m_RedoOperation.Value);
                ctx.Flush();
                return;
            }
            // 初回実行時
            if (m_Link == null || m_SegmentNew == null)
            {
                throw new NullReferenceException();
            }

            m_UndoOperation = new ChangeLinkInfo(m_Link.From.Doc.ID,
                m_Link.From.StartChar, m_Link.From.EndChar, m_Link.From.Tag.Name,
                -1, -1, null,
                m_Link.To.StartChar, m_Link.To.EndChar, m_Link.To.Tag.Name);
            m_RedoOperation = new ChangeLinkInfo(m_Link.From.Doc.ID, 
                m_Link.From.StartChar, m_Link.From.EndChar, m_Link.From.Tag.Name,
                -1, -1, null, 
                m_SegmentNew.StartChar, m_SegmentNew.EndChar, m_SegmentNew.Tag.Name);

            m_Link.To = m_SegmentNew;
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            if (m_UndoOperation == null || !m_UndoOperation.HasValue)
            {
                throw new Exception("OperationMoveArrow: undoing an invalid operation (m_UndoOperation has no value).");
            }
            ChangeLink(ctx, m_UndoOperation.Value);
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("#svc.ChangeLinkEnd({0}, {1}, {2}) : not supported.",
                m_Link.ID, m_SegmentOld.ID, m_SegmentNew.ID);
        }

        public override string ToString()
        {
            return string.Format("{{OperationMoveArrow:link={0}, segold={1}, segnew={2}}}", m_Link.ID, m_SegmentOld.ID, m_SegmentNew.ID);
        }
    }
}
