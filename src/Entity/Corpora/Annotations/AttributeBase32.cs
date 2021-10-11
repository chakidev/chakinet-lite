using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace ChaKi.Entity.Corpora.Annotations
{
    public abstract class AttributeBase32
    {
        public virtual Int32 ID { get; set; }
        public virtual string Key { get; set; }
        public virtual string Value { get; set; }
        public virtual string Comment { get; set; }
        public virtual Project Proj { get; set; }
        public virtual User User { get; set; }
        public virtual TagSetVersion Version { get; set; }

        public string GetAttributeStringAsXmlFragment()
        {
            try
            {
                using (TextWriter twr = new StringWriter())
                {
                    using (XmlWriter wr = new XmlTextWriter(twr))
                    {
                        if (this.Key.StartsWith("@"))
                        {
                            wr.WriteElementString(this.Key.Substring(1), this.Value);
                        }
                        else
                        {
                            wr.WriteElementString(this.Key, this.Value);
                        }
                        return twr.ToString();
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
