using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora
{
    public abstract class Dictionary
    {
        public abstract string Name { get; }

        public bool CanSearchCompoundWord { get; set; }

        public bool CanUpdateCompoundWord { get; set; }

        public static Dictionary Create(string path, string name = null, bool isCompoundDict = false)
        {
            Dictionary d = null;

            if (path.StartsWith("http:") || path.StartsWith("https:"))
            {
                d = new Dictionary_Cradle(path, name);
            }
            else
            {
                d = new Dictionary_DB(path); // intentionally ignoring name param
            }
            d.CanSearchCompoundWord = isCompoundDict;
            d.CanUpdateCompoundWord = isCompoundDict;

            return d;
        }
    }
}
