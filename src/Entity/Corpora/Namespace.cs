using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class Namespace
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public Namespace(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}
