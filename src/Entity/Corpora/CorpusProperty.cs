using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class CorpusProperty
    {
        public CorpusProperty(string p, string v)
        {
            Property = p;
            Value = v;
        }
        public string Property { get; set; }
        public string Value { get; set; }
    }
}
