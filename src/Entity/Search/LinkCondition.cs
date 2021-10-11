using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public class LinkCondition : ICloneable
    {
        public int SegidFrom { get; set; }
        public int SegidTo { get; set; }
        public string Text { get; set; }

        public SerializableDictionary<string, string> LinkAttrs { get; set; }

        public bool TextIsValid
        {
            get
            {
                return (Text.Length > 0 && !Text.Equals("*"));
            }
        }

        public LinkCondition()
        {
            SegidFrom = 0;
            SegidTo = 0;
            Text = string.Empty;
            LinkAttrs = new SerializableDictionary<string, string>();
        }

        public LinkCondition(int segid_from, int segid_to, string text)
        {
            SegidFrom = segid_from;
            SegidTo = segid_to;
            Text = text;
            LinkAttrs = new SerializableDictionary<string, string>();
        }

        public LinkCondition(LinkCondition src)
        {
            SegidFrom = src.SegidFrom;
            SegidTo = src.SegidTo;
            Text = string.Copy(src.Text);
            LinkAttrs = new SerializableDictionary<string, string>();
            foreach (var pair in src.LinkAttrs)
            {
                this.LinkAttrs.Add(pair.Key, pair.Value);
            }
        }

        public object Clone()
        {
            return new LinkCondition(this);
        }
    }
}
