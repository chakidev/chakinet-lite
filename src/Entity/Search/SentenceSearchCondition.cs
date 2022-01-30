using System;
using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using System.Xml.Serialization;

namespace ChaKi.Entity.Search
{
    public class SentenceSearchCondition : ISearchCondition, ICloneable
    {
        public CorpusGroup CorpusGroup { get; set; }

        // ì¡íËIDÇÃï∂ÇÃÇ›ÇéÊìæÇ∑ÇÈèÍçáÇ…ÉZÉbÉgÇ≥ÇÍÇÈ.
        public SerializableDictionary<Corpus, List<int>> Ids { get; set; }

        public event EventHandler OnModelChanged;

        public SentenceSearchCondition()
        {
            this.CorpusGroup = new CorpusGroup();
            this.Ids = new SerializableDictionary<Corpus, List<int>>();
        }

        public SentenceSearchCondition(SentenceSearchCondition src)
            : this()
        {
            this.CorpusGroup = src.CorpusGroup.Clone();

            foreach (KeyValuePair<Corpus, List<int>> pair in src.Ids)
            {
                List<int> ids = new List<int>();
                foreach (int id in pair.Value)
                {
                    ids.Add(id);
                }
                this.Ids.Add(pair.Key, ids);
            }
            OnModelChanged = src.OnModelChanged;
        }

        public object Clone()
        {
            return new SentenceSearchCondition(this);
        }

        public void Reset()
        {
            this.CorpusGroup.Clear();
            this.Ids.Clear();
            if (OnModelChanged != null) OnModelChanged(this, null);
        }

        public Corpus Find(string name)
        {
            foreach (var c in this.CorpusGroup.AsEnumerable())
            {
                if (c.Name == name)
                {
                    return c;
                }
            }
            return null;
        }
    }
}
