using System;
using System.Collections.Generic;

namespace ChaKi.Entity.Search
{
    public enum FetchType
    {
        Incremental,
        Decremental,
        Random,
    }

    public struct ResultsetFilter
    {
        public FetchType FetchType;
        public int Max;
        public int StartAt;
        public bool IsAutoIncrement;

        public void Reset()
        {
            this.FetchType = FetchType.Incremental;
            this.Max = -1;
            this.StartAt = 0;
            this.IsAutoIncrement = false;
        }

        public IEnumerable<T> CreateEnumerable<T>(IList<T> source) where T : class
        {
            return new FilteredResultset<T>(source, this);
        }
    }

    public class FilteredResultset<T> : IEnumerable<T> where T : class
    {
        private FilteredResultsetEnumerator<T> m_Enum;

        public FilteredResultset(IList<T> source, ResultsetFilter filter)
        {
            if (filter.FetchType == FetchType.Random)
            {
                m_Enum = new RandomResultsetEnumerator<T>(source, filter);
            }
            else
            {
                m_Enum = new FilteredResultsetEnumerator<T>(source, filter);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_Enum;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (System.Collections.IEnumerator)m_Enum;
        }
    }

    public class FilteredResultsetEnumerator<T> : IEnumerator<T> where T : class
    {
        protected IList<T> m_Source;
        protected ResultsetFilter m_Filter;
        protected int m_CurrentIndex;
        protected int m_Count;

        public FilteredResultsetEnumerator(IList<T> source, ResultsetFilter filter)
        {
            m_Source = source;
            m_Filter = filter;
            Reset();
        }

        public virtual void Reset()
        {
            if (m_Filter.StartAt > 0)
            {
                if (m_Filter.FetchType == FetchType.Incremental)
                {
                    m_CurrentIndex = m_Filter.StartAt - 1;
                }
                else
                {
                    m_CurrentIndex = m_Filter.StartAt + 1;
                }
            }
            else
            {
                if (m_Filter.FetchType == FetchType.Incremental)
                {
                    m_CurrentIndex = -1;
                }
                else
                {
                    m_CurrentIndex = m_Source.Count;
                }
            }
            m_Count = 0;
        }

        public virtual T Current
        {
            get
            {
               return (m_CurrentIndex < 0 || m_CurrentIndex >= m_Source.Count) ? null : m_Source[m_CurrentIndex];
            }
        }

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current
        {
            get { return this.Current; }
        }

        public virtual bool MoveNext()
        {
            switch (m_Filter.FetchType)
            {
                case FetchType.Incremental:
                    m_CurrentIndex++;
                    break;
                case FetchType.Decremental:
                    m_CurrentIndex--;
                    break;
            }
            m_Count++;

            if (m_Filter.Max >= 0 && m_Count > m_Filter.Max)
            {
                return false;
            }
            if (m_CurrentIndex < 0 || m_CurrentIndex >= m_Source.Count)
            {
                return false;
            }
            return true;
        }
    }

    public class RandomResultsetEnumerator<T> : FilteredResultsetEnumerator<T> where T: class
    {
        // Randomの場合、乱数インデックス列を生成する
        private List<int> m_RandomSeq;
        private int m_RandomSeqIndex;

        public RandomResultsetEnumerator(IList<T> source, ResultsetFilter filter)
            : base(source, filter)
        {
        }

        public override void Reset()
        {
            m_RandomSeq = new List<int>();
            if (m_Filter.Max >= m_Source.Count)
            {
                for (int i = 0; i < m_Source.Count; i++) m_RandomSeq.Add(i);
            }
            else
            {
                Random rand = new Random();
                while (m_RandomSeq.Count < m_Filter.Max)
                {
                    int r = rand.Next(0, m_Source.Count-1);
                    if (!m_RandomSeq.Contains(r))
                    {
                        m_RandomSeq.Add(r);
                    }
                }
                m_RandomSeq.Sort();
            }
            m_Count = 0;
            m_RandomSeqIndex = -1;
        }

        public override T Current
        {
            get
            {
                return (m_RandomSeqIndex < 0) ? null : m_Source[m_RandomSeq[m_RandomSeqIndex]];
            }
        }

        public override bool MoveNext()
        {
            m_RandomSeqIndex++;
            m_Count++;

            if (m_Filter.Max >= 0 && m_Count > m_Filter.Max)
            {
                return false;
            }
            if (m_RandomSeqIndex >= m_Source.Count)
            {
                return false;
            }
            return true;
        }
    }
}
