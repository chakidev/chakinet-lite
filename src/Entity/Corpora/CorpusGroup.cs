using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    /// <summary>
    /// Corpusのツリーにおける枝を表す
    /// CorpusまたはCorpusGroupを複数リストとして保持することができる
    /// Corpusタブ/SentenceSearchのモデルは、ルートとしてこのツリーを指す。
    /// </summary>
    public class CorpusGroup : IEnumerable<Corpus>
    {
        public List<CorpusGroup> Groups { get; } = new List<CorpusGroup>();

        public List<Corpus> Corpora { get; } = new List<Corpus>();

        public CorpusGroup()
        {
        }

        // 1つだけのコーパスを含む単純なCorpusGroupを生成する
        public CorpusGroup(Corpus c)
        {
            this.Corpora.Add(c);
        }

        public void Clear()
        {
            foreach (var g in this.Groups)
            {
                g.Clear();
            }
            this.Groups.Clear();
            this.Corpora.Clear();
        }

        // Groupに対するAddは未実装
        public void Add(Corpus c)
        {
            this.Corpora.Add(c);
        }

        public void Remove(string name)
        {
            foreach (var g in this.Groups)
            {
                g.Remove(name);
            }
            this.Corpora.RemoveAll(c => c.Name == name);
        }

        public Corpus Find(string name)
        {
            foreach (var g in this.Groups)
            {
                var c = g.Find(name);
                if (c != null)
                {
                    return c;
                }
            }
            return this.Corpora.Find(c => c.Name == name);
        }

        public int Count
        {
            get
            {
                var count = 0;
                foreach (var g in this.Groups)
                {
                    count += g.Count;
                }
                return count + this.Corpora.Count();
            }
        }

        public CorpusGroup Clone()
        {
            var obj = new CorpusGroup();
            foreach (var g in this.Groups)
            {
                obj.Groups.Add(g.Clone());
            }
            foreach (var c in this.Corpora)
            {
                // CorpusはCloneオブジェクト間であっても共有する
                obj.Corpora.Add(c);
            }
            return obj;
        }

        public IEnumerator<Corpus> GetEnumerator()
        {
            foreach (var g in this.Groups)
            {
                foreach (var c in g)
                {
                    yield return c;
                }
            }
            foreach (var c in this.Corpora)
            {
                yield return c;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
    }
}
