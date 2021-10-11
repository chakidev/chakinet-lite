using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Readers
{
    public class MappedTo
    {
        [XmlAttribute(AttributeName = "Tag")]
        public string Tag { get; set; }

        [XmlIgnore]
        public int TagIndex { get; set; }

        [XmlAttribute(AttributeName = "CustomTagName")]
        public string CustomTagName { get; set; }

        [XmlIgnore]
        public int PartNo { get; set; }
    }
}
