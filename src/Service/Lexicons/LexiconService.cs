using ChaKi.Common.SequenceMatcher;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Common;
using ChaKi.Service.Database;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Service.Lexicons
{
    public class LexiconService : ILexiconService, IDisposable
    {
        private ISession m_Session;

        public LexiconService()
        {
        }

        public void Dispose()
        {
            if (m_Session != null)
            {
                m_Session.Close();
                m_Session = null;
            }
        }

        public void Open(Corpus cps)
        {
            DBService dbs = DBService.Create(cps.DBParam);
            NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
            ISessionFactory factory = cfg.BuildSessionFactory();
            m_Session = factory.OpenSession();
        }

        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Surfaceがstrに一致するLexemeをすべて得る。
        /// </summary>
        /// <param name="str"></param>
        public IList<Lexeme> Search(string str)
        {
            IList<Lexeme> list = new List<Lexeme>();

            //todo: for each Corpus (connection) do:
            IQuery query = m_Session.CreateQuery(string.Format("from Lexeme l where l.Surface = '{0}'", str));
            IList<Lexeme> result = query.List<Lexeme>();
            foreach (Lexeme lex in result)
            {
                list.Add(lex);
            }

            return list;
        }

        /// <summary>
        /// 与えられたLexemeと一致するLexemeの数を返す。
        /// 完全なLexemdを与えた場合は結果はFrequencyフィールドそのものである。
        /// 一部属性がnullのLexemeを与えた場合は、マッチするLexemeすべてのFrequencyを合計したものである。
        /// </summary>
        /// <param name="lex"></param>
        /// <returns></returns>
        public int QueryFrequency(Lexeme lex)
        {
            int freq = 0;
            StringBuilder sb = new StringBuilder();
            StringConnector conn = new StringConnector(" and ");

            if (lex.Surface != null)
            {
                sb.Append(conn.Get());
                sb.AppendFormat("l.Surface='{0}'", lex.Surface);
            }
            if (lex.Pronunciation != null)
            {
                sb.Append(conn.Get());
                sb.AppendFormat("l.Pronunciation='{0}'", lex.Pronunciation);
            }
            if (lex.Reading != null)
            {
                sb.Append(conn.Get());
                sb.AppendFormat("l.Reading='{0}'", lex.Reading);
            }
            if (lex.PartOfSpeech != null)
            {
                sb.Append(conn.Get());
                sb.AppendFormat("l.PartOfSpeech.Name='{0}'", lex.PartOfSpeech.Name);
            }
            if (lex.CType != null)
            {
                sb.Append(conn.Get());
                sb.AppendFormat("l.CType.Name='{0}'", lex.CType.Name);
            }
            if (lex.CForm != null)
            {
                sb.Append(conn.Get());
                sb.AppendFormat("l.CForm.Name='{0}'", lex.CForm.Name);
            }
            if (lex.BaseLexeme != null)
            {
                sb.Append(conn.Get());
                sb.AppendFormat("l.BaseLexeme.Surface='{0}'", lex.BaseLexeme.Surface);
            }
            if (sb.Length == 0)
            {
                return 0;
            }
            string qstr = "from Lexeme l where " + sb.ToString();

            IQuery query = m_Session.CreateQuery(qstr);
            IList<Lexeme> result = query.List<Lexeme>();
            foreach (Lexeme l in result)
            {
                freq += l.Frequency;
            }
            return freq;
        }

        //
        // 以下は今のところ使用していない。DepEditServiceで実装されている。
        //
        public IList<LexemeCandidate> FindAllLexemeCandidates(string str)
        {
            throw new NotImplementedException();
        }

        public void CreateOrUpdateLexeme(ref Lexeme lex, string[] props, string customprop)
        {
            throw new NotImplementedException();
        }

        public void GetLexiconTags(out Dictionary<string, IList<PartOfSpeech>> pos, out Dictionary<string, IList<CType>> ctypes, out Dictionary<string, IList<CForm>> cforms)
        {
            throw new NotImplementedException();
        }

        public List<MWE> FindMWECandidates(IList<Word> words, Action<string> showMessageCallback, Action<MWE, MatchingResult> foundMWECallback)
        {
            throw new NotImplementedException();
        }

        public List<MatchingResult> FindMWECandidates2(string surface, IList<Word> words)
        {
            throw new NotImplementedException();
        }
    }
}
