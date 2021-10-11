using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class DocumentAttribute : AttributeBase32
    {
        public static int UniqueID;

        public virtual Document Doc { get; set; }
    }
}
