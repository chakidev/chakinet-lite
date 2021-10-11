using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Collocation;
using ChaKi.Entity.Search;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Lexicons;
using ChaKi.Service.Database;
using NHibernate;

namespace ChaKi.Service.Collocation
{
    internal class CounterMI : Counter
    {
        private Corpus m_Corpus;
        private long m_CorpusSize;   // Nc of m_Corpus
        private long m_SampleSize;   // Nc of KWIC results (Nc')

        public CounterMI(KwicList src, CollocationList result, CollocationCondition cond)
            : base(src, result, cond)
        {
            result.ColumnDefs.Clear();
            result.AddColumn("Fn,c", ColumnType.CT_INT, true);
            result.AddColumn("Fc", ColumnType.CT_INT, true);
            result.AddColumn("MI Score", ColumnType.CT_DOUBLE, false);
            result.AddColumn("MI3 Score", ColumnType.CT_DOUBLE, false);
            result.AddColumn("Dice Score", ColumnType.CT_DOUBLE, false);
            result.AddColumn("Log-log Score", ColumnType.CT_DOUBLE, false);
            result.AddColumn("Z Score", ColumnType.CT_DOUBLE, false);
        }

        public override void Count()
        {
            if (m_Src.Records.Count <= 0)
            {
                return;
            }
            // 対象コーパスは単一であること
            m_Corpus = m_Src.Records[0].Crps;
            m_CorpusSize = m_Corpus.NWords;
            m_SampleSize = 0;
            foreach (KwicItem ki in m_Src.Records)
            {
                m_SampleSize += ki.WordCount;
            }

            // 中心語
            if (m_Src.Records[0].Center.Words.Count == 0)
            {
                throw new Exception("CounterMI - Missing Center word!");
            }
            Lexeme keylex = new Lexeme(m_Src.Records[0].Center.Words[0].Lex);
            keylex.Marked = true;   // 中心語以外と別に集計されるようにする
            keylex.ApplyFilter(m_Cond.Filter);

            // (1) Kwicに現れる共起語の頻度を集計する--> m_Clist[s][0]
            foreach (KwicItem item in m_Src.Records)
            {
                if (item.Crps != m_Corpus)
                {
                    throw new Exception("CounterMI - KwicList contains multiple corpus!");
                }
                CountLine(item);
            }

            // (2) 共起語の独立頻度--> m_Clist[s][1]
            var lsvc = new LexiconService();
            lsvc.Open(m_Corpus);
            try
            {
                foreach (KeyValuePair<Lexeme, List<DIValue>> pair in m_Clist.Rows)
                {
                    Lexeme targetlex = new Lexeme(pair.Key);
                    targetlex.ApplyFilter(m_Cond.Filter);
                    pair.Value[1].ival = lsvc.QueryFrequency(targetlex);
                }
            }
            finally
            {
                lsvc.Close();
            }

            // (3) MI Scoreを算出する
            List<DIValue> key_vals = m_Clist.Rows[keylex];
            int wsize = m_Cond.Lwsz + m_Cond.Rwsz + 1;  // +1 for Center word
            long ncorpus = m_CorpusSize;
            if (key_vals.Count < 2)
            {
                throw new Exception("Cannot calculate score.");
            }
            double Fn = (double)key_vals[1].ival;
            foreach (KeyValuePair<Lexeme, List<DIValue>> pair in m_Clist.Rows)
            {
                List<DIValue> vals = pair.Value;
                double Fc = (double)(vals[1].ival);
                double Fnc = (double)(vals[0].ival);
                double p1 = (Fnc * ncorpus/* * ncorpus*/) / (Fc * Fn * wsize/* * m_SampleSize*/);
                vals[2].dval = Math.Log10(p1) / Math.Log10(2.0);
                double Fnc3 = Math.Pow(Fnc, 3.0);
                double p2 = (Fnc3 * ncorpus) / (Fc * Fn * wsize);
                vals[3].dval = Math.Log10(p2) / Math.Log10(2.0);
                double p3 = 2.0 * Fnc / (Fn + Fc);
                vals[4].dval = p3;
                double p4 = Math.Log10(Fnc) / Math.Log10(2.0);
                vals[5].dval = vals[2].dval * p4;
                double p = Fc / (ncorpus - Fn);
                double E = p * Fn * wsize;
                vals[6].dval = (Fnc - E) / Math.Sqrt(E * (1.0 - p));
            }
        }

        /// <summary>
        /// KwicItemに出現する語の頻度を加算する.
        /// CounterRaw.CountLineと類似しているが、
        /// window内に出現する語をすべてm_Clist[s][0]に集計する.
        /// （位置ごとの配列にしない）
        /// </summary>
        /// <param name="item"></param>
        private void CountLine(KwicItem item)
        {
            List<KwicWord> left = item.Left.Words;
            for (int i = left.Count - 1; i >= left.Count - m_Cond.Lwsz && i >= 0; i--)
            {
                KwicWord kw = left[i];
                Lexeme lex = new Lexeme(kw.Lex);
                lex.ApplyFilter(m_Cond.Filter);
                List<DIValue> val = m_Clist.FindRow(lex);
                ++(val[0].ival);
            }

            List<KwicWord> center = item.Center.Words;
            if (center.Count != 0)
            {
                KwicWord kw = center[0];
                Lexeme lex = new Lexeme(kw.Lex);
                // 中心語は特別扱いとする.（値を<<val>>の形として、中心語以外と別に集計されるように)
                lex.Marked = true;
                lex.ApplyFilter(m_Cond.Filter);
                List<DIValue> val = m_Clist.FindRow(lex);
                ++(val[0].ival);
            }

            List<KwicWord> right = item.Right.Words;
            for (int i = 0; i < right.Count && i < m_Cond.Rwsz; i++)
            {
                KwicWord kw = right[i];
                Lexeme lex = new Lexeme(kw.Lex);
                lex.ApplyFilter(m_Cond.Filter);
                List<DIValue> val = m_Clist.FindRow(lex);
                ++(val[0].ival);
            }
        }
    }
}
