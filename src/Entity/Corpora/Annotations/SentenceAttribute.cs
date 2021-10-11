using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Iesi.Collections;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class SentenceAttribute : AttributeBase32
    {
        public SentenceAttribute()
        {
            this.DocumentID = -1;
        }

        [XmlIgnore]
        public virtual int DocumentID { get; set; }
    }
}
