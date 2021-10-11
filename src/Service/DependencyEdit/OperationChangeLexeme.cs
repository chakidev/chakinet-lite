using System;
using ChaKi.Entity.Corpora;
using NHibernate;

namespace ChaKi.Service.DependencyEdit
{
    internal class OperationChangeLexeme : Operation
    {
        private int m_DocID;
        private int m_WPos;
        private Word m_Word;
        private Lexeme m_OldLex;
        private Lexeme m_NewLex;

        public OperationChangeLexeme(int docid, int wpos, Lexeme newLex)
        {
            m_DocID = docid;
            m_WPos = wpos;
            m_NewLex = newLex;
            m_OldLex = null;
        }

        public override void Execute(DepEditContext ctx)
        {
            if (m_NewLex == null)
            {
                throw new NullReferenceException();
            }
            string[] props = m_NewLex.ToPropertyArray();
            string customprop = m_NewLex.CustomProperty;
            if (m_NewLex.Dictionary != null) // 他のDB内のオブジェクト
            {
                m_NewLex = null;  // 一旦nullにしてCreateOrUpdateLexeme()で生成させる.
            }

            // Lexemeの依存オブジェクト(Base, POS, CForm, CType)を登録すべきかの判定と処理
            CreateOrUpdateLexeme(ctx, ref m_NewLex, props, customprop);
            ctx.Flush();
            m_Word = ctx.Sen.GetWords(ctx.Proj.ID)[m_WPos];
            m_OldLex = m_Word.Lex;
            m_Word.Lex = m_NewLex;
            ctx.AddRelevantLexeme(m_OldLex);
            ctx.AddRelevantLexeme(m_NewLex);
            ctx.SaveOrUpdate(m_Word);
            ctx.Flush();
        }


        public override void UnExecute(DepEditContext ctx)
        {
            if (m_OldLex == null)
            {
                throw new NullReferenceException();
            }
            var w = ctx.Sen.GetWords(ctx.Proj.ID)[m_WPos];
            w.Lex = m_OldLex;
        }

        public override string ToIronRubyStatement(DepEditContext ctx)
        {
            return string.Format("svc.ChangeLexeme(d, c+({0}), {1})", m_Word.StartChar - ctx.CharOffset, m_NewLex.ID);
        }

        public override string ToString()
        {
            return string.Format("{{OperationChangeLexeme:wpos={0}, newLex=[{1}], oldLex=[{2}]}}", m_WPos, m_NewLex, m_OldLex);
        }
    }
}
