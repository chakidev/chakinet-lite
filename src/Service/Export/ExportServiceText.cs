using System;
using System.IO;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using NHibernate;

namespace ChaKi.Service.Export
{
    public class ExportServiceText : ExportServiceBase
    {
        public ExportServiceText(TextWriter wr)
        {
            m_TextWriter = wr;
        }

        public override void ExportItem(KwicItem ki)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            ExportKwicPortion(ki.Left);
            if (ki.Center.Count > 0)
            {
                m_TextWriter.Write(" [ ");
                ExportKwicPortion(ki.Center);
                m_TextWriter.Write("] ");
            }
            ExportKwicPortion(ki.Right);
            m_TextWriter.WriteLine();
        }

        private void ExportKwicPortion(KwicPortion kp)
        {
            foreach (KwicWord w in kp.Words)
            {
                Lexeme lex = w.Lex;
                if (lex != null)
                {
                    m_TextWriter.Write(lex.Surface);
                }
                else
                {
                    m_TextWriter.Write(w.Text);
                }
                m_TextWriter.Write(" ");
            }
        }
    }
}
