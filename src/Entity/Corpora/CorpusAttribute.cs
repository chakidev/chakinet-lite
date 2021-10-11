using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class CorpusAttribute
    {
        public virtual int ID { get; set; }
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }

        public CorpusAttribute()
        {
        }

        public CorpusAttribute(int id, string name, string value)
        {
            this.ID = id;
            this.Name = name;
            this.Value = value;
        }
    }
}
