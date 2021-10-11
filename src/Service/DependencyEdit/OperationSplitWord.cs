using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationSplitWord : Operation
    {
        private int m_DocId;
        private int m_BPos;
        private int m_EPos;
        private int m_SPos;
        private Lexeme m_LexOrg;
        private Lexeme m_Lex1;
        private Lexeme m_Lex2;

        public OperationSplitWord(int docid, int bpos, int epos, int spos, Lexeme lex1, Lexeme lex2)
        {
            m_DocId = docid;
            m_BPos = bpos;
            m_EPos = epos;
            m_SPos = spos;
            m_Lex1 = lex1;
            m_Lex2 = lex2;
            m_LexOrg = null;
        }

        public override void Execute(DepEditContext ctx)
        {
            ExecuteSplitWordOperation(ctx, m_DocId, m_BPos, m_EPos, m_SPos, m_Lex1, m_Lex2, ref m_LexOrg);
            ctx.Flush();
        }

        public override void UnExecute(DepEditContext ctx)
        {
            ExecuteMergeWordOperation(ctx, m_DocId, m_BPos, m_EPos, m_LexOrg, ref m_Lex1, ref m_Lex2);
            ctx.Flush();
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("svc.SplitWord(d, c+({0}), c+({1}), c+({2}), \"{3}\", \"{4}\")",
                m_BPos - ctx.CharOffset, m_EPos - ctx.CharOffset, m_SPos - ctx.CharOffset, m_Lex1.Surface, m_Lex2.Surface);
        }

        public override string ToString()
        {
            return string.Format("{{OperationSplitWord:{0}, {1}, {2}, {3}, {4}, {5}}}", m_DocId, m_BPos, m_EPos, m_SPos, m_Lex1.Surface, m_Lex2.Surface);
        }
    }
}
