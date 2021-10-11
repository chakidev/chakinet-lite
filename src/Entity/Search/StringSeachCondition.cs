using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public class StringSearchCondition : ISearchCondition, ICloneable
    {
        public string Pattern { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool IsRegexp { get; set; }

        public event EventHandler OnModelChanged;

        public StringSearchCondition()
        {
            this.Pattern = "";
            this.IsCaseSensitive = true;
            this.IsRegexp = false;
        }

        public StringSearchCondition(StringSearchCondition src)
        {
            this.Pattern = string.Copy(src.Pattern);
            this.IsCaseSensitive = src.IsCaseSensitive;
            this.IsRegexp = src.IsRegexp;
            this.OnModelChanged = src.OnModelChanged;
        }

        public object Clone()
        {
            return new StringSearchCondition(this);
        }

        public void Reset()
        {
            if (OnModelChanged != null) OnModelChanged(this, null);
        }
    }
}
