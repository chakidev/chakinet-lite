using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using ChaKi.Entity.Search;

namespace ChaKi.Entity.Kwic
{
    public class AddLexemeCountEventArgs : EventArgs
    {
        public AddLexemeCountEventArgs(int index, LexemeList lex, Corpus cps, long count)
        {
            this.Index = index;
            this.Lex = lex;
            this.Cps = cps;
            this.Count = count;
        }
        public int Index;
        public LexemeList Lex;
        public Corpus Cps;
        public long Count;
    }

    public delegate void AddLexemeCountEventHandler(object sender, AddLexemeCountEventArgs e);

    public class LexemeCount
    {
        [XmlIgnore]
        public LexemeList LexList;

        [XmlIgnore]
        public Dictionary<Corpus, long> Counts;

        public LexemeCount()
        {
            this.LexList = null;
            this.Counts = new Dictionary<Corpus, long>();
        }

        public LexemeCount(LexemeList lex, Corpus cps, long count)
        {
            this.LexList = lex;
            this.Counts = new Dictionary<Corpus, long>();
            this.Counts.Add(cps, count);
        }
    }


    /// <summary>
    /// WordListFormに対するModelオブジェクト。
    /// Modelオブジェクトではあるが、データの管理はView=DataGridViewも独自に
    /// 行っているため、二重に管理することになる。
    /// </summary>
    public class LexemeCountList : KeyedCollection<LexemeList, LexemeCount>
    {
        public event AddLexemeCountEventHandler OnLexemeCountAdded;

        // AddするLexemeList（Gridで横に並ぶLexeme）のサイズ. 検索条件のLexeme Boxの数に等しい.
        public int LexSize { get; set; }

        // Lexemeの並びのうち、条件のpivotに対応する位置（なければ-1）
        public int PivotPos { get; set; }

        public List<Corpus> CorpusList { get; set; }

        public LexemeListFilter LexListFilter { get; set; }

        public LexemeCountList(TagSearchCondition cond)
            : base(new LexemeListFilter(cond.Count))
        {
            this.CorpusList = new List<Corpus>();
            this.LexListFilter = this.Comparer as LexemeListFilter;
        }
        public LexemeCountList(DepSearchCondition cond)
            : base(new LexemeListFilter(cond.GetLexemeCondListCount()))
        {
            this.CorpusList = new List<Corpus>();
            this.LexListFilter = this.Comparer as LexemeListFilter;
        }
        public LexemeCountList(LexemeListFilter comp)
            : base(comp)
        {
            this.CorpusList = new List<Corpus>();
            this.LexListFilter = comp;
        }

        protected override LexemeList GetKeyForItem(LexemeCount lc)
        {
            return lc.LexList;
        }

        public void Add(LexemeList lexList, Corpus cps, long count)
        {
            int index = -1;
            LexemeCount lc;
            if (TryGetValue(lexList, out lc)) {
                index = IndexOf(lc);
            }
            if (index < 0)
            {
                // 新しいLexemeListなので、リストに追加する
                lc = new LexemeCount(lexList, cps, count);
                this.Add(lc);
            } else {
                // 既にリストにあるLexemeなので、カウントのみ更新する
                // カウントを加算
                if (lc.Counts.ContainsKey(cps))
                {
                    lc.Counts[cps] += count;
                }
                else
                {
                    lc.Counts.Add(cps, count);
                }
            }

            // リスナにモデル更新を通知
            if (OnLexemeCountAdded != null)
            {
                OnLexemeCountAdded(this, new AddLexemeCountEventArgs(index, lexList, cps, count));
            }
        }

        public bool TryGetValue(LexemeList key, out LexemeCount val)
        {
            if (this.Dictionary == null)
            {
                val = null;
                return false;
            }
            return this.Dictionary.TryGetValue(key, out val);
        }

    }
}
