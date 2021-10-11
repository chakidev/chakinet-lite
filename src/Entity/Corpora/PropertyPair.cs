using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class PropertyPair : ICloneable
    {
        public PropertyPair()
        {
        }

        public PropertyPair(PropertyPair src)
        {
        	if (src.Key != null)
        	{
                Key = string.Copy(src.Key);
            }
            Value = (Property)src.Value.Clone();
        }

        public object Clone()
        {
            return new PropertyPair(this);
        }

        public PropertyPair(string key, Property value)
        {
            Key = key;
            Value = value;
        }
        public string Key { get; set; }
        public Property Value { get; set; }
    }
}
