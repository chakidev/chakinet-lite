using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationMergeWord : Operation
    {
        private int m_DocId;
        private int m_BPos;
        private int m_EPos;
        private int m_SPos;
        private Lexeme m_Lex;
        private Lexeme m_Lex1Org;
        private Lexeme m_Lex2Org;

        public OperationMergeWord(int docid, int bpos, int epos, int spos, Lexeme lex)
        {
            m_DocId = docid;
            m_BPos = bpos;
            m_EPos = epos;
            m_SPos = spos;
            m_Lex = lex;
            m_Lex1Org = null;
            m_Lex2Org = null;
        }

        public override void Execute(DepEditContext ctx)
        {
            ExecuteMergeWordOperation(ctx, m_DocId, m_BPos, m_EPos, m_Lex, ref m_Lex1Org, ref m_Lex2Org);
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            ExecuteSplitWordOperation(ctx, m_DocId, m_BPos, m_EPos, m_SPos, m_Lex1Org, m_Lex2Org, ref m_Lex);
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("svc.MergeWord(d, c+({0}), c+({1}), c+({2}), \"{3}\")",
                m_BPos - ctx.CharOffset, m_EPos - ctx.CharOffset, m_SPos - ctx.CharOffset, m_Lex.Surface);
        }

        public override string ToString()
        {
            return string.Format("{{OperationMergeWord:{0}, {1}, {2}, {3}, {4}}}", m_DocId, m_BPos, m_EPos, m_SPos, m_Lex.Surface);
        }    }
}
