using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Collocation;
using ChaKi.Entity.Search;
using ChaKi.Entity.Corpora;

namespace ChaKi.Service.Collocation
{
    internal class CounterRaw : Counter
    {
        public CounterRaw(KwicList src, CollocationList result, CollocationCondition cond)
            : base(src, result, cond)
        {
            int ncols = m_Cond.Lwsz + m_Cond.Rwsz + 1; // +1 for Center position
            result.ColumnDefs.Clear();
            for (int i = 0; i < ncols; i++)
            {
                result.AddColumn(string.Format("{0}", i - m_Cond.Lwsz), ColumnType.CT_INT, true);
            }
        }

        public override void Count()
        {
            int last_percent = -1;
            int sz = m_Src.Records.Count;
            for (int i = 0; i < sz; i++)
            {
                CountLine(m_Src.Records[i]);
                int percent = (int)((i + 1) * 100.0 / sz);
                if (percent != last_percent)
                {
                    //TODO: Report percentage here

                    last_percent = percent;
                }
            }
        }

        private void CountLine(KwicItem item)
        {
            List<KwicWord> left = item.Left.Words;
            for (int i = left.Count-1; i >= left.Count-m_Cond.Lwsz && i >= 0; i--)
            {
                KwicWord kw = left[i];
                Lexeme lex = new Lexeme( kw.Lex );
                lex.ApplyFilter( m_Cond.Filter );
                List<DIValue> val = m_Clist.FindRow(lex);
                ++ (val[i-left.Count+m_Cond.Lwsz].ival);
            }

            List<KwicWord> center = item.Center.Words;
            if (center.Count != 0) {
                KwicWord kw = center[0];
                Lexeme lex = new Lexeme( kw.Lex );
                lex.ApplyFilter( m_Cond.Filter );
                List<DIValue> val = m_Clist.FindRow(lex);
                ++(val[m_Cond.Lwsz].ival);
            }

            List<KwicWord> right = item.Right.Words;
            for (int i = 0; i < right.Count && i < m_Cond.Rwsz; i++)
            {
                KwicWord kw = right[i];
                Lexeme lex = new Lexeme( kw.Lex );
                lex.ApplyFilter( m_Cond.Filter );
                List<DIValue> val = m_Clist.FindRow(lex);
                ++(val[m_Cond.Lwsz+i+1].ival);
            }
        }
    }
}
