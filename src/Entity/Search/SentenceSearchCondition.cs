using System;
using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using System.Xml.Serialization;

namespace ChaKi.Entity.Search
{
    public class SentenceSearchCondition : ISearchCondition, ICloneable
    {
        public List<Corpus> Corpora { get; set; }

        // ����ID�̕��݂̂��擾����ꍇ�ɃZ�b�g�����.
        public SerializableDictionary<Corpus, List<int>> Ids { get; set; }

        public event EventHandler OnModelChanged;

        public SentenceSearchCondition()
        {
            this.Corpora = new List<Corpus>();
            this.Ids = new SerializableDictionary<Corpus, List<int>>();
        }

        public SentenceSearchCondition(SentenceSearchCondition src)
            : this()
        {
            foreach (Corpus c in src.Corpora)
            {
                // Corpus��Clone�I�u�W�F�N�g�Ԃł����Ă����L����
                this.Corpora.Add(c);
            }
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
            this.Corpora.Clear();
            this.Ids.Clear();
            if (OnModelChanged != null) OnModelChanged(this, null);
        }

        public Corpus Find(string name)
        {
            foreach (Corpus c in this.Corpora)
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
