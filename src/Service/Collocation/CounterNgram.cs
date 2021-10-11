using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Collocation;
using ChaKi.Entity.Search;
using ChaKi.Entity.Corpora;

namespace ChaKi.Service.Collocation
{
	public enum NgramType
	{
		Left,
		Right
	}

	internal class CounterNgram : Counter
	{
		private List<KwicItem> m_RecordCopy;
		private List<int> m_LCPs;
		private NgramType m_Type; // true if N-gram for Right-Context; false if for Left-Context

		public CounterNgram(KwicList src, CollocationList result, CollocationCondition cond, NgramType ngramType)
			: base(src, result, cond)
		{
			m_Type = ngramType;

			result.ColumnDefs.Clear();
			result.AddColumn("Frequency", ColumnType.CT_INT, true);
			result.AddColumn("Length", ColumnType.CT_INT, false);
			result.FirstColTitle = "N-gram";
		}

		public override void Count()
		{
			int sz = m_Src.Records.Count;
			if (sz == 0)
			{
				return;
			}
			m_RecordCopy = new List<KwicItem>(m_Src.Records);

			// 左文脈または右文脈でソートを実行
			if (m_Type == NgramType.Left)
			{
				KwicItemComparer cmp = new KwicItemComparer(6, true);   // Leftでソート
				m_RecordCopy.Sort(cmp);
			}
			else
			{
				KwicItemComparer cmp = new KwicItemComparer(8, true);   // Rightでソート
				m_RecordCopy.Sort(cmp);
			}

			// LCP配列を求める
			m_LCPs = new List<int>();
			m_LCPs.Add(0);
			for (int i = 1; i < sz; i++) 
			{
				KwicItem item1 = m_RecordCopy[i-1];
				KwicItem item2 = m_RecordCopy[i];
				if (m_Type == NgramType.Right) {
					m_LCPs.Add(LCP(item1.Right, item2.Right, false));
				} else {
					m_LCPs.Add(LCP(item1.Left, item2.Left, true));
				}
			}
			m_LCPs.Add(0);
			
			FindClass(0, 0);

			// 最小頻度・語長に達しない項目を削除
			Dictionary<Lexeme, List<DIValue>> newrows = new Dictionary<Lexeme, List<DIValue>>();
			foreach (KeyValuePair<Lexeme, List<DIValue>> pair in m_Clist.Rows)
			{
				string s = pair.Key.Surface;
#if false // -- MinLengthを「文字数」と解釈する場合
				// スペースを除く文字列長を求める
				int		len = 0;
				for (int j = 0; j < s.Length; j++) {
					if (s[j] == ' ')
					{
						continue;
					}
					len ++;
				}
#else    // -- MinLengthを「語数」と解釈する場合
				int len = 0;
				for (int j = 0; j < s.Length; j++)
				{
					if (s[j] == ' ')
					{
						len ++;
					}
				}
#endif
				List<DIValue> val = pair.Value;
				if (len < m_Cond.MinLength || val[0].ival < m_Cond.MinFrequency)
				{
					continue; // remove
				}
				val[1].ival = len;
				newrows.Add(pair.Key, pair.Value);
			}
			m_Clist.Rows = newrows;
		}

		/// <summary>
		/// LCPを元にして、2回以上生起する(=trivialでない)連続系列をすべて求める。
		/// LBL(Longest bounding lcp), SIL(shortest interior lcp)の定義については、
		/// 
		/// M.Yamamoto and K.W.Church, 2001, "Using Suffix Arrays to Compute Term Frequency 
		/// and Document Frequency for All Substrings in a Corpus",
		/// Computational Linguistics, Vol.27.No.1, p.1-30
		/// 
		/// を参照。
		/// </summary>
		/// <param name="i"></param>
		/// <param name="k"></param>
		/// <returns></returns>
		private int FindClass(int i, int k)
		{
			int j = k;
			while (m_LCPs[k] <= m_LCPs[j + 1] && j < m_LCPs.Count-2)
			{
				j = FindClass(k, j + 1);
			}
			int LBL = (m_LCPs[i] > m_LCPs[j + 1]) ? m_LCPs[i] : m_LCPs[j + 1];
			int SIL = m_LCPs[k];
			if (LBL < SIL)
			{
				// [i,j]にFreq>1のクラスを発見
//                Console.WriteLine("Class: <{0}, {1}> k={2}", i, j, k);
				string s;
				if (m_Type == NgramType.Right)
				{
					s = CreateRightNgram(m_RecordCopy[i], m_LCPs[k]);
				}
				else
				{
					s = CreateLeftNgram(m_RecordCopy[i], m_LCPs[k]);
				}
				Lexeme p = new Lexeme() { Surface = s };
				List<DIValue> val = m_Clist.FindRow(p);
				val[0].ival = j - i + 1;
			}
			return j;
		}


		private int LCP(KwicPortion a, KwicPortion b, bool reverse)
		{
			int i = 0;
			int asz = a.Count;
			int bsz = b.Count;
			for (int j = 0; j < Math.Min(asz, bsz); j++)
			{
				string wa;
				string wb;
				if (!reverse)
				{
					wa = a.Words[j].Lex.Surface;
					wb = b.Words[j].Lex.Surface;
				}
				else
				{
					wa = a.Words[asz - j - 1].Lex.Surface;
					wb = b.Words[bsz - j - 1].Lex.Surface;
				}
				if (wa == null || wb == null)
				{
					break;
				}
				if (wa != wb)
				{
					break;
				}
				i++;
			}
			return i;
		}

		private string CreateRightNgram(KwicItem a, int cnt )
		{
			StringBuilder sb = new StringBuilder();
			foreach (KwicWord kw in a.Center.Words)
			{
				sb.Append(kw.Lex.Surface);
				sb.Append(" ");
			}
			for (int i = 0; i < cnt; i++)
			{
				sb.Append(a.Right.Words[i].Lex.Surface);
				sb.Append(" ");
			}
			return sb.ToString();
		}

		private string CreateLeftNgram(KwicItem a, int cnt)
		{
			StringBuilder sb = new StringBuilder();
			int lsz = a.Left.Words.Count;
			cnt = Math.Min(lsz, cnt);
			for (int i = 0; i < cnt; i++)
			{
				sb.Append(a.Left.Words[lsz - (cnt-i)].Lex.Surface);
				sb.Append(" ");
			}
			foreach (KwicWord kw in a.Center.Words)
			{
				sb.Append(kw.Lex.Surface);
				sb.Append(" ");
			}
			return sb.ToString();
		}
	}
}
