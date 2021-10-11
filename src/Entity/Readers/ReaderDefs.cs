using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Readers
{
    public class ReaderDefs
    {
        [XmlElement]
        public ReaderDef[] ReaderDef { get; set; }

        public ReaderDef Find(string name)
        {
            foreach (ReaderDef def in this.ReaderDef)
            {
                if (def.Name == name)
                {
                    return def;
                }
            }
            return null;
        }
    }
}
