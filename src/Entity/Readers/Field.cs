using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Readers
{
    public class Field
    {
        [XmlElement]
        public MappedTo[] MappedTo { get; set; }
    }
}
