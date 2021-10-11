using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Collocation;
using ChaKi.Entity.Search;
using System.Threading;
using ChaKi.Service.Collocation.FSM;
using ChaKi.Entity.Corpora;
using DictionaryIndex = System.UInt32;
using System.Diagnostics;

namespace ChaKi.Service.Collocation
{
    internal class CounterFSM : Counter
    {
        internal class Node
        {
            public Node(DictionaryIndex w, Projection p)
            {
                this.Word = w;
                this.Projection = p;
            }
            public DictionaryIndex Word;
            public Projection Projection;
        }

        private Dictionary<DictionaryIndex, Lexeme> m_Dictionary;
        private Dictionary<string, DictionaryIndex> m_RDictionary;
        public bool ThreadStopRequested;
        public List<List<DictionaryIndex>> m_Data;
        public List<Node> m_Pattern;

        private uint AddGapFlag(uint idx) { return (0x80000000U | idx); }
        private uint RemoveGapFlag(uint idx) { return (0x7FFFFFFFU & idx); }
        private bool HasGapFlag(uint idx) { return (idx & 0x80000000U) != 0; }


        public CounterFSM(KwicList src, CollocationList result, CollocationCondition cond)
            : base(src, result, cond)
        {
            m_Dictionary = new Dictionary<DictionaryIndex, Lexeme>();
            m_RDictionary = new Dictionary<string, DictionaryIndex>();
            m_Data = new List<List<DictionaryIndex>>();
            m_Pattern = new List<Node>();
            this.ThreadStopRequested = false;

            result.ColumnDefs.Clear();
            result.AddColumn("Frequency", ColumnType.CT_INT, true);
            result.AddColumn("Length", ColumnType.CT_INT, false);
            result.AddColumn("Gaps", ColumnType.CT_INT, false);
            result.AddColumn("IDs", ColumnType.CT_STRING, false);
            result.FirstColTitle = "Sequence";
        }

        public override void Count()
        {
            Projection root = new Projection();

            if (m_Src == null)
            {
                return;
            }
            int sz = m_Src.Records.Count;
            if (sz == 0)
            {
                return;
            }

            // Make Dictionary and Data
            m_Dictionary.Clear();

            // Dictionaryの0番目をgapを表すダミー語とする.
            Lexeme gap = new Lexeme() { Surface = "." };
            m_Dictionary.Add(0, gap);

            var m_AddedSids = new Dictionary<int, bool>();
            for (int i = 0; i < sz; i++)
            {
                if (this.ThreadStopRequested)
                {
                    return;
                }

                List<DictionaryIndex> cur_row = new List<uint>();
                m_Data.Add(cur_row);

                // 既に追加されている文番号のKwicItemは除外
                var sid = m_Src.Records[i].SenID;
                if (m_AddedSids.ContainsKey(sid))
                {
                    continue;
                }
                m_AddedSids.Add(sid, true);

                // KWICの左側単語列
                MakeDictionary(m_Src.Records[i].Left.Words, cur_row);
                // KWICの中央単語列
                MakeDictionary(m_Src.Records[i].Center.Words, cur_row);
                // KWICの右側単語列
                MakeDictionary(m_Src.Records[i].Right.Words, cur_row);
            }
            // 逆index辞書は以下では不要なので開放する
            m_RDictionary.Clear();
            m_RDictionary = null;

            for (int i = 0; i < sz; i++)
            {
                root.Add(new SSequence(i));		// rootは各文に対して１つずつ空のSSequenceを持つ（初期状態）
            }

            Project(root, true);
        }

        /// <summary>
        /// KwicPropertyのリストを辞書indexのリストに変換する。
        /// ・kwic_contextにリストされているKwicPropertyを辞書から検索し、未登録なら追加する.
        /// ・各KwicPropertyの、辞書におけるindex番号をidx_listに追加する
        /// </summary>
        /// <param name="kwic_context"></param>
        /// <param name="idx_list"></param>
        private void MakeDictionary(List<KwicWord> kwic_context, List<DictionaryIndex> idx_list)
        {
            foreach (KwicWord word in kwic_context)
            {
                if (word == null || word.Lex == null)   // Shift操作後、Kwic Contextに空のLexemeが入る場合がある
                {
                    continue;
                }
                Lexeme p = (Lexeme)word.Lex.Clone();

                // Lexemeがストップワードに指定されていないかチェック
                bool is_stopword = false;
                if (m_Cond.Stopwords != null)
                {
                    foreach (string sw in m_Cond.Stopwords)
                    {
                        if (sw == p.Surface)
                        {
                            is_stopword = true;
                            break;
                        }
                    }
                }
                if (is_stopword)
                {
                    idx_list.Add(0);    // Dictionary index 0がstopwordを表す
                    continue;
                }

                // 属性フィルタを適用
                p.ApplyFilter(m_Cond.Filter);

                DictionaryIndex idx;	// index for the word
                string key = p.ToString();
                // 既に辞書に入っているか？
                if (!m_RDictionary.TryGetValue(key, out idx))
                {
                    idx = (uint)m_Dictionary.Count;
                    m_Dictionary.Add(idx, p);
                    m_RDictionary.Add(key, idx);
                }
                idx_list.Add(idx);
            }
        }

        private void Project(Projection projected, bool init)
        {
            if (ThreadStopRequested)
            {
                return;
            }

            // このイテレーションで追加される新しいノード群
            Dictionary<DictionaryIndex, Projection> counter = new Dictionary<DictionaryIndex, Projection>();

#if true
            Console.WriteLine("Projected count={0}", projected.Count);
#endif
            // projectedには、各文に対する現イテレーションにおける系列候補がリストされている.
            for (int i = 0; i < projected.Count; i++)
            {
                int pos = projected.GetLastPos(i);
                int sid = projected.GetSid(i);
                SSequence curseq = projected.nodes[i];
                int size = m_Data[sid].Count;
                Dictionary<DictionaryIndex, List<uint>> new_nodes = new Dictionary<DictionaryIndex, List<uint>>();

                // 毎イテレーションごとにこの文におけるpos+j (j>1)の位置にある語の出現を調べる
                pos++;
                for (int j = 0; j < size; j++)
                {
                    if (!init && (m_Cond.MaxGapLen >= 0 && j > m_Cond.MaxGapLen))
                    {
                        // 初回のみは各文における異なり語の最初の出現位置を調べるので、最大gap長に関わらず文末まで見る。
                        // 初回でなければ、前回までの系列末尾位置から数えて最大gap長を超えて系列を延長することはしない。
                        // 連続系列はmaxgl=0なので、このループを１回だけ実行する。
                        break;
                    }
                    int pos1 = pos + j;
                    if (pos1 >= size)
                    {
                        break;
                    }
                    DictionaryIndex item = m_Data[sid][pos1];
                    if (item != 0)
                    {	// itemがstopwordでなければノード追加を行う.
                        if (!init && j > 0)
                        {
                            item = AddGapFlag(item);	// 前にgapが存在する語として扱う.
                        }
                        List<uint> val;
                        if (!new_nodes.TryGetValue(item, out val))
                        {
                            // まだノード化されていない語であれば、新規にノード化する。
                            val = new List<uint>();
                            val.Add((uint)pos1);
                            new_nodes.Add(item, val);
                        }
                        else /*if (m_bContinuous) */
                        {
                            // /*連続系列の場合は*/
                            // 各文におけるすべての出現を記録する
                            val.Add((uint)pos1);
                        }
                    }
                }

                // この文中に見つかったすべての系列延長候補がnew_nodesに入っている.
                // これを元にcounterに対して、次の語をキーとしてそれぞれによって延長した系列（複数）を入れる.
                //  ex. 現在の系列(curseq)が"abc"のとき、'd'が続くような系列が2個見つかった
                //    ==> curseq+'d'
                foreach (KeyValuePair<DictionaryIndex, List<uint>> pair in new_nodes)
                {
                    foreach (uint spos in pair.Value)
                    {
                        SSequence newseq = new SSequence(curseq);
                        newseq.Add(spos);
                        if (m_Cond.MaxGapCount >= 0 && newseq.ngaps > m_Cond.MaxGapCount)
                        {
                            // Gap数が最大値を超えたのでこの系列は捨てる.
                            continue;
                        }
                        Projection proj;
                        if (!counter.TryGetValue(pair.Key, out proj))
                        {
                            proj = new Projection();
                            counter.Add(pair.Key, proj);
                        }
                        proj.Add(newseq);	// この系列をカウンタに追加する
                    }
                }
            }

#if DETAILPRINT
	        Debug.WriteLine("=======");
#endif

            // counter（後ろに延長する候補中）の候補の内で、
            //   1.その頻度が最小頻度設定以上
            //   2.自分の前にgapを持たない
            // をすべて満たすような候補が存在すれば、長さ1を超えるgapを持つ候補を採用しない
            // ※gapを持たない候補がgapを持つ候補をカバーするため.
            bool nongap = false;
            bool hasgap = false;
            foreach (KeyValuePair<DictionaryIndex, Projection> pair in counter)
            {
#if DETAILPRINT
                Debug.WriteLine(string.Format("ext={0} -------", GetDictionaryWord(RemoveGapFlag(pair.Key))));
	        	pair.Value.print();
#endif
                uint freq = pair.Value.GetSentenceCount();
                if (!HasGapFlag(pair.Key) && freq >= m_Cond.MinFrequency)
                {
                    nongap = true;
                }
                if (HasGapFlag(pair.Key))
                {
                    hasgap = true;
                }
            }

#if DETAILPRINT
	        Debug.WriteLine( "MaximalSeq Check (Backward)" );
#endif
            // ★counterのすべての前方拡張系列候補について、maximal sequenceチェック
            // counterの候補にgapを持つものがあれば、そのgapからパターンを前にさかのぼって
            // 他の1語を超えるgapまたは先頭にいたる部分系列について、
            // その部分系列を前方に1語延長する部分系列の頻度を調べて、最小頻度以上のものが
            // 存在する場合、gapによる延長候補をcounterから削除する.
            // （例えばa !b c d !e という系列で、現在!eが延長候補の場合、bcdがこの部分系列となる.）
            if (hasgap)
            {
                List<DictionaryIndex> subpattern = new List<DictionaryIndex>();
                GetCurrentSubPattern(subpattern);
                Debug.Assert(subpattern.Count > 0);
                if (IsSubPatternExpandableBackward(subpattern))
                {
#if DETAILPRINT
			        Debug.WriteLine( " true -->" );
#endif
                    // 部分系列が他の系列に含まれることが判明したので、counterに含まれる系列候補のうち、
                    // 部分系列の前のgap幅が1を超えるもののみを削除する.
                    foreach (KeyValuePair<DictionaryIndex, Projection> pair in counter)
                    {
                        Projection prj = pair.Value;
                        List<SSequence> newnodes = new List<SSequence>();
                        // 前方のgap幅が1を超える系列のみをProjectionから削除する.
                        // （幅1のgapは拡張により系列のgap数が変わるため、別の系列として残す必要がある.）
                        foreach (SSequence seq in prj.nodes)
                        {
                            // subpatternの開始位置をkとする（前方延長語はsubpatternに入らないので-1）
                            int k = seq.pos_seq.Count - subpattern.Count - 1;
                            if (k == 0 || seq.pos_seq[k] - seq.pos_seq[k - 1] - 1 > 1)
                            {	// gap幅
                                // この候補を削除
                                continue;
                            }
                            newnodes.Add(seq);
                        }
                        prj.nodes = newnodes;
                        // Projectionのscount を更新する.
                        // (これによりnodesが空のProjectionが発生しうるが、直後のfreqチェックで削除される.）
                        prj.RecalcSentenceCount();
#if DETAILPRINT
//			        	Debug.WriteLine( "ext={0} -------", pair.Key );
//			        	pair.Value.print();
#endif
                    }
                }
            }

#if DETAILPRINT
        	Debug.WriteLine("MaximalSeq Check (Forward)" );
#endif

            // ★counterのすべての後方拡張系列候補について、maximal sequenceチェック
            foreach (KeyValuePair<DictionaryIndex, Projection> pair in counter)
            {
                if (HasGapFlag(pair.Key))
                {
                    bool erase_f = false;
                    // 直前にgapを持つ拡張候補について、maximal sequenceチェックを行う.
                    if (nongap)
                    {
                        // gapをはさんで前方から後方へ1語の拡張が可能（nongapフラグがtrue）なら削除.
                        // この判定はcounterを使用すればできるので、上で1度だけ行う.
                        // （見つかった文字を含む系列がgapを持つ候補をカバーするため.
                        // 　幅1のgapは拡張により系列のgap数が変わるため、別の系列として残す必要がある.）
                        Projection prj = pair.Value;
                        List<SSequence> newnodes = new List<SSequence>();
                        foreach (SSequence seq in prj.nodes)
                        {
                            int k = seq.pos_seq.Count;
                            if (k < 2)
                            {
                                continue;
                            }
                            int gaplen = (int)(seq.pos_seq[k - 1] - seq.pos_seq[k - 2] - 1);
                            Debug.Assert(gaplen > 0);
                            if (gaplen > 1)
                            {
                                // この候補を削除
                                continue;
                            }
                            newnodes.Add(seq);
                        }
                        prj.nodes = newnodes;
                        // Projectionのscount を更新する.
                        // (これによりnodesが空のProjectionが発生しうるが、直後のfreqチェックで削除される.）
                        prj.RecalcSentenceCount();
#if DETAILPRINT
				        Debug.WriteLine("after -->");
			        	prj.print();
#endif
                    }
                }
            }

            // 最後に頻度条件に合わない系列拡張候補をリストより削除
            Dictionary<DictionaryIndex, Projection> newcounter = new Dictionary<DictionaryIndex, Projection>();
            foreach (KeyValuePair<DictionaryIndex, Projection> pair in counter)
            {
                if (pair.Value.GetSentenceCount() < m_Cond.MinFrequency)
                {
                    continue;   // 最小頻度設定に満たない拡張候補は削除
                }
                newcounter.Add(pair.Key, pair.Value);
            }
            counter = newcounter;

            if (counter.Count == 0)
            {
                // これ以上語長を長くしても最小頻度を満たすパターンがなくなったので、再帰呼び出しを終える。
                // ★出力する前に、m_Patternの最も後方の部分系列（gapがない場合は系列全体）について、
                // 　前方に拡張可能かどうかを調べる
                List<DictionaryIndex> subpattern = new List<DictionaryIndex>();
                GetCurrentSubPattern(subpattern);
#if DETAILPRINT
		        Debug.WriteLine( "Last MaximalSeq check (Backward)" );
#endif
#if true
                Console.Write("Backward: {0} ", projected.Count);
#endif
                if (IsSubPatternExpandableBackward(subpattern))
                {
                    // projectedに含まれる系列のうち、最後のgap幅>1ならばその系列を削除
                    // （幅1のgapは拡張により系列のgap数が変わるため、別の系列として残す必要がある.）
                    List<SSequence> newnodes = new List<SSequence>();
                    foreach (SSequence seq in projected.nodes)
                    {
                        int k = seq.pos_seq.Count - subpattern.Count;		// subpatternの開始位置
                        if (k == 0 || seq.pos_seq[k] - seq.pos_seq[k - 1] - 1 > 1)
                        {	// gap幅
                            // この候補を削除
                            continue;
                        }
                        newnodes.Add(seq);
                    }
                    projected.nodes = newnodes;
                    // Projectionのscount を更新する.
                    projected.RecalcSentenceCount();
#if DETAILPRINT
//                    Debug.WriteLine("ext={0} -------", pair.Key);
//		        	pair.Value.print();
#endif
                }
#if true
                Console.WriteLine("--> {0}", projected.Count);
#endif
                // 削除後もminf以上であればその系列を出力可能である.
                if (projected.GetSentenceCount() >= m_Cond.MinFrequency)
                {
                    Report(projected);
                }
                return;
            }

            // みつかったパターンそれぞれについて、語長を+1して再帰呼び出し（深さ優先探索）を行う。
            foreach (KeyValuePair<DictionaryIndex, Projection> pair in counter)
            {
                m_Pattern.Add(new Node(pair.Key, pair.Value));
                Project(pair.Value, false);
                m_Pattern.RemoveAt(m_Pattern.Count - 1);
            }
        }

        /// <summary>
        /// 現在のm_Patternの末尾より前方にさかのぼってgapまたは先頭までの部分系列を求める. 
        /// </summary>
        /// <param name="pat"></param>
        private void GetCurrentSubPattern(List<DictionaryIndex> pat)
        {
            for (int i = m_Pattern.Count - 1; i >= 0; --i)
            {
                DictionaryIndex idx = m_Pattern[i].Word;
                if (HasGapFlag(idx))
                {
                    pat.Add(RemoveGapFlag(idx));
                    break;
                }
                pat.Add(idx);
            }
            pat.Reverse();
        }

        /// <summary>
        /// 連続パターンpatを直前に１文字拡張した２文字系列の頻度がMinFrequency以上になりうるかどうかを全文について調べる 
        /// </summary>
        /// <param name="pat"></param>
        /// <returns></returns>
        private bool IsSubPatternExpandableBackward(List<DictionaryIndex> pat)
        {
            Dictionary<DictionaryIndex, int> counter = new Dictionary<DictionaryIndex, int>();

            int psz = pat.Count;
            if (psz == 0)
            {
                return false;
            }

            int sz = m_Data.Count;
#if true
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < psz; i++)
            {
                sb.Append(GetDictionaryWord(pat[i]));
                sb.Append(" ");
            }
            Debug.WriteLine(string.Format("IsSubPatternExpandableBackward subpattern=[ {0}]", sb.ToString()));
#endif

            for (int i = 0; i < sz; i++)
            {
                int ssz = m_Data[i].Count;
                for (int j = 1; j < ssz; j++)
                {	// 各文の先頭は前方に拡張できないので調べなくてよい.
                    if (m_Data[i][j] == pat[0])
                    {
                        bool match = true;
                        for (int k = 1; k < psz; k++)
                        {
                            if (j + k >= ssz)
                            {
                                match = false;
                                break;
                            }
                            if (m_Data[i][j + k] != pat[k])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            DictionaryIndex exindex = m_Data[i][j - 1];	// 系列を前方に拡張する語
                            if (exindex != 0)
                            {	// Stopwordは数えない
                                int val;
                                if (!counter.TryGetValue(exindex, out val))
                                {
                                    counter[exindex] = 1;
                                }
                                else
                                {
                                    counter[exindex]++;
                                }
                                if (counter[exindex] >= m_Cond.MinFrequency)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        string GetDictionaryWord(uint idx)
        {
            return m_Dictionary[idx].ToFilteredString(m_Cond.Filter);
        }

        private void Report(Projection projected)
        {
            if (m_Cond.MinLength > m_Pattern.Count)
            {
                return;
            }

#if DETAILPRINT
	        //StringBuilder sb0 = new StringBuilder();
         //   foreach (KeyValuePair<DictionaryIndex, uint> pair in m_Pattern)
         //   {
		       // if (HasGapFlag( pair.Key ))
         //       {
			      //  sb0.Append("!");
		       // }
         //       sb0.Append(GetDictionaryWord(RemoveGapFlag(pair.Key)));
		       // sb0.Append(" ");
	        //}
         //   Debug.WriteLine(string.Format("Reporting: {{ {0}}}", sb0.ToString()));
#endif

            int nGaps = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append("[ ");
            for (int i = 0; i < m_Pattern.Count; i++)
            {
                if (sb[sb.Length - 1] != ' ')
                {
                    sb.Append(" ");
                }
                DictionaryIndex idx = m_Pattern[i].Word;
                if (HasGapFlag(idx))
                {
                    sb.Append("] [ ");
                    nGaps++;
                }
                idx = RemoveGapFlag(idx);
                sb.Append(GetDictionaryWord(idx));
            }
            sb.Append(" ]");

            // Gap数厳密一致の場合、数が一致しない系列は出力しない.
            if (m_Cond.ExactGC && nGaps != m_Cond.MaxGapCount)
            {
                return;
            }

            Lexeme p = new Lexeme();
            p.Surface = String.Copy(sb.ToString());

            List<DIValue> val = m_Clist.FindRow(p);
            if (false/*TODO: val is NOT empty*/)
            {
                throw new Exception("Error: CollocationCounterFSM - Duplicated value asignment.");
            }
            else
            {
                val[0].ival = (int)projected.Count/*m_Pattern[m_Pattern.Count-1].Projection.Count*/;
                val[1].ival = m_Pattern.Count;
                val[2].ival = nGaps;
                val[3].sval = IDListToString(projected/*m_Pattern[m_Pattern.Count-1].Projection*/);
            }
        }

        private string IDListToString(Projection projected)
        {
            StringBuilder sb = new StringBuilder();
            int sz = projected.Count;
            int last_sid = -1;
            for (int i = 0; i < sz; i++)
            {
                int cur_sid = projected.GetSid(i);
                if (cur_sid == last_sid)
                {
                    continue;
                }
                last_sid = cur_sid;
                // sidからKWICの文IDへ変換して表示
                List<KwicItem> kwiclist = m_Src.Records;
                if (cur_sid >= kwiclist.Count)
                {
                    throw new Exception("Error: CollocationCounterFSM - Invalid SentenceID.");
                }
                string tmp = string.Format("{0}", kwiclist[cur_sid].SenID);
                if (sb.Length == 0)
                {
                    sb.Append(tmp);
                }
                else
                {
                    sb.Append(",");
                    sb.Append(tmp);
                }
            }
            return sb.ToString();
        }

        /*
        void IDListFromString( Corpus cps, string str, List<SentenceIdentifier> ids )
        {
            string	resToken;
            int	curPos = 0;
            string[] tokens = str.Split(new char[]{','});
            foreach (string token in tokens)
            {
                int spos;
                if (!Int32.TryParse(token, out spos))
                {
                    continue;
                }
                SentenceIdentifier id = new SentenceIdentifier(cps, spos);
                ids.Add( id );
            }
        }
         */
    }
}
