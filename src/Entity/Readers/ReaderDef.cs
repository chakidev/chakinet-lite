using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ChaKi.Entity.Readers
{
    public class ReaderDef
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "LineFormat")]
        public string LineFormat { get; set; }

        public Field[] Fields { get; set; }
    }
}
