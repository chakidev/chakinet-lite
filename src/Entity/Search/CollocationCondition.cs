using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Settings;

namespace ChaKi.Entity.Search
{
    public class CollocationCondition : ISearchCondition, ICloneable
    {
        public CollocationType CollType { get; set; }
        public int Lwsz { get; set; }
        public int Rwsz { get; set; }
        public LexemeFilter Filter { get; set; }

        public int MaxGapLen { get; set; }
        public int MaxGapCount { get; set; }
        public uint MinFrequency { get; set; }
        public int MinLength { get; set; }
        public bool ExactGC { get; set; }

        public string[] Stopwords {
            get
            {
                return m_Stopwords;
            }
            set
            {
                m_Stopwords = value;
                if (value != null)
                {
                    UserSettings.GetInstance().DefaultStopwords = (string[])value.Clone();
                }
                else
                {
                    UserSettings.GetInstance().DefaultStopwords = null;
                }
            }
        } private string[] m_Stopwords;

        public event EventHandler ModelChanged;


        public CollocationCondition()
        {
            Reset();
        }

        public CollocationCondition(CollocationCondition org)
        {
            this.CollType = org.CollType;
            this.Lwsz = org.Lwsz;
            this.Rwsz = org.Rwsz;
            this.Filter = new LexemeFilter(org.Filter);
            this.MaxGapLen = org.MaxGapLen;
            this.MaxGapCount = org.MaxGapCount;
            this.MinFrequency = org.MinFrequency;
            this.MinLength = org.MinLength;
            this.ExactGC = org.ExactGC;
            this.Stopwords = (string[])org.Stopwords.Clone();
        }

        public void Reset()
        {
            this.CollType = CollocationType.Raw;
            this.Lwsz = 5;
            this.Rwsz = 5;
            this.Filter = new LexemeFilter();
            this.MaxGapLen = -1;
            this.MaxGapCount = -1;
            this.MinFrequency = 0;
            this.MinLength = 0;
            this.ExactGC = false;
            this.Stopwords = UserSettings.GetInstance().DefaultStopwords;
            if (this.Stopwords == null) {
                this.Stopwords = new string[]{};
            }
        }

        public object Clone()
        {
            return new CollocationCondition(this);
        }
    }
}
