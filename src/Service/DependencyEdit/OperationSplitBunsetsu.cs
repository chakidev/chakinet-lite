using System;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;
using System.Collections.Generic;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationSplitBunsetsu : Operation
    {
        private int m_DocId;
        private int m_BPos;
        private int m_EPos;
        private int m_SPos;
        private LinkHandling m_LinkHandling;
        private List<ChangeLinkInfo> m_AdditionalUndoOperationInfo;

        public OperationSplitBunsetsu(int docid, int bpos, int epos, int spos)
        {
            m_DocId = docid;
            m_BPos = bpos;
            m_EPos = epos;
            m_SPos = spos;
            m_LinkHandling = LinkHandling.HeadIsRight;
            m_AdditionalUndoOperationInfo = new List<ChangeLinkInfo>();
        }

        public override void Execute(DepEditContext ctx)
        {
            //PrintAllLink(ctx, m_DocId, m_BPos, m_EPos);
            ExecuteSplitBunsetsuOperation(ctx, m_DocId, m_BPos, m_EPos, m_SPos, m_AdditionalUndoOperationInfo);
            // 重複関連であるWord-Segmentを割り当て直す.
            UpdateWordToSegmentRelations(ctx);
            //PrintAllLink(ctx, m_DocId, m_BPos, m_EPos);
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            ExecuteMergeBunsetsuOperation(ctx, m_DocId, m_BPos, m_EPos, null);
            try
            {
                foreach (var info in m_AdditionalUndoOperationInfo)
                {
                    ChangeLink(ctx, info);
                }
            }
            catch
            {
                // give up to resore links
            }

            // 重複関連であるWord-Segmentを割り当て直す.
            UpdateWordToSegmentRelations(ctx);
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("svc.SplitBunsetsu(d, c+({0}), c+({1}), c+({2}), {3})",
                m_BPos - ctx.CharOffset, m_EPos - ctx.CharOffset, m_SPos - ctx.CharOffset, (int)m_LinkHandling);
        }

        public override string ToString()
        {
            return string.Format("{{OperationSplitBunsetsu:{0}, {1}, {2}, {3}, {4}}}", m_DocId, m_BPos, m_EPos, m_SPos, m_LinkHandling);
        }
    }
}