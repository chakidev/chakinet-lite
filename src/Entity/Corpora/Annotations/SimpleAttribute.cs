using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class SimpleAttribute : AttributeBase
    {
        public SimpleAttribute(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}
