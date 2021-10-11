using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iesi.Collections.Generic;

namespace ChaKi.Views.KwicView
{
    public class WordMappingHilightEventArgs : EventArgs
    {
        public WordMappingHilightEventArgs(string corpusname, int from_wordid, Iesi.Collections.Generic.ISet<int> to_wordids)
        {
            this.CorpusName = corpusname;
            this.FromWordId = from_wordid;
            this.ToWordIds = to_wordids;
        }

        public string CorpusName { get; set; }
        public int FromWordId { get; set; }
        public Iesi.Collections.Generic.ISet<int> ToWordIds { get; set; }
    }
}
