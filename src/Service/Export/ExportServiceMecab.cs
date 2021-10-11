using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ChaKi.Entity.Readers;
using ChaKi.Service.Readers;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using NHibernate;

namespace ChaKi.Service.Export
{
    public class ExportServiceMecab : ExportServiceBase
    {
        private Action<Lexeme> m_LexemeWriter;

        public ExportServiceMecab(TextWriter wr)
        {
            ReaderDef def = CorpusSourceReaderFactory.Instance.ReaderDefs.Find("Mecab|Cabocha");
            Initialize(wr, def);
        }

        public ExportServiceMecab(TextWriter wr, ReaderDef def)
        {
            Initialize(wr, def);
        }

        private void Initialize(TextWriter wr, ReaderDef def)
        {
            m_TextWriter = wr;
            m_Def = def;
            if (m_Def.LineFormat == "TabSeparatedLine")
            {
                m_LexemeWriter = this.WriteChasenLexeme;
            }
            else if (m_Def.LineFormat == "MecabLine")
            {
                m_LexemeWriter = this.WriteMecabLexeme;
            }
            else
            {
                throw new NotImplementedException(string.Format("Export format '{0}' is not supported yet.", m_Def.LineFormat));
            }
        }

        public override void ExportItem(KwicItem ki)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            IQuery q = m_Session.CreateQuery(string.Format("from Sentence where ID={0}", ki.SenID));
            Sentence sen = q.UniqueResult<Sentence>();
            if (sen == null)
            {
                throw new Exception(string.Format("Sentence not found. Corpus={0}, senID={1}", ki.Crps.Name, ki.SenID));
            }

            foreach (Word w in sen.Words)
            {
                if (w.Lex != null)
                {
                    m_LexemeWriter(w.Lex);
                }
            }
            m_TextWriter.WriteLine("EOS");
        }

        public override void ExportItem(Corpus crps, Sentence sen)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            var words = sen.GetWords(m_ProjId);
            foreach (var w in words)
            {
                if (w.Lex != null)
                {
                    m_LexemeWriter(w.Lex);
                }
            }
            m_TextWriter.WriteLine("EOS");
        }

        static readonly List<string> OpenParen = new List<string>() 
            { "\"", "\'", "“", "‘", "（", "「", "＜", "『", "〔", "｛", "【", "《" };
        static readonly List<string> CloseParen = new List<string>()
            { "\"", "\'", "”", "’", "）", "」", "＞", "』", "〕", "｝", "】", "》" };

        // CabochaRunnerから使用される、セグメントリープ(null)ありのWord ListからMecab出力を得るWriter
        public override void ExportItem(Corpus crps, IList<Word> wlist)
        {

            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            for (int i = 0; i < wlist.Count; i++)
            {
                var w = wlist[i];
                if (w == null)
                {
                    m_TextWriter.WriteLine("…	記号,一般");
                }
                else if (w != null && w.Lex != null)
                {
                    // リープの前後にある括弧類も"…"に置き換える.
                    if ((OpenParen.Contains(w.Lex.Surface) && i < wlist.Count - 1 && wlist[i + 1] == null)
                        ||
                        (CloseParen.Contains(w.Lex.Surface) && i > 0 && wlist[i - 1] == null))
                    {
                        m_TextWriter.WriteLine("…	記号,一般");
                    }
                    else
                    {
                        m_LexemeWriter(w.Lex);
                    }
                }
            }
            m_TextWriter.WriteLine("EOS");
        }

        public string ExportLexeme(Lexeme lex)
        {
            var oldWriter = m_TextWriter;
            var sb = new StringBuilder();
            using (m_TextWriter = new StringWriter(sb))
            {
                m_LexemeWriter(lex);
            }
            m_TextWriter = oldWriter;
            return sb.ToString();
        }

        public override void ExportLexeme(Dictionary d, Lexeme lex)
        {
            WriteMecabLexeme(lex);
        }
    }
}
