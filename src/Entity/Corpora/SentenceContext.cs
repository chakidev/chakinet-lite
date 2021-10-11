using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class SentenceContextItem
    {
        public Sentence Sen;
        public string Text;
        public bool IsCenter;
        public int Length;
    }

    /// <summary>
    /// 文脈情報（中心となる文とその前後の文を表現する）.
    /// コンテクストパネルのモデルとなる.
    /// </summary>
    public class SentenceContext
    {
        public Corpus Corpus { get; private set; }
        public uint ContextLineCount { get; private set; }
        public int CenterSentenceID { get; private set; }
        public List<SentenceContextItem> Items { get; private set; }

        public int CenterOffset { get; set; }
        public int CenterLength { get; set; }

        public SentenceContext()
        {
            this.Items = new List<SentenceContextItem>();
            this.Corpus = null;
        }

        public void SetTarget(Corpus crps, int centerSentenceID, uint contextLineCount)
        {
            this.Corpus = crps;
            this.CenterSentenceID = centerSentenceID;
            this.ContextLineCount = contextLineCount;
        }

        public void AddItem(Corpus cps, Sentence sen, bool useSpacing, bool isCenter)
        {
            this.Corpus = cps;
            SentenceContextItem item = new SentenceContextItem() { Sen = sen, Text = sen.GetText(useSpacing), IsCenter = isCenter };
            item.Length = item.Text.Length;
            this.Items.Add(item);
        }

        public void Clear()
        {
            this.Items.Clear();
            this.CenterOffset = 0;
            this.CenterLength = 0;
        }
    }
}
