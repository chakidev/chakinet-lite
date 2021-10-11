using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class LexemeCorpusBoolLongTuple
    {
        public LexemeCorpusBoolLongTuple(Lexeme item1, Corpus item2, bool item3, long item4)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
        }

        public Lexeme Item1 { get; set; }
        public Corpus Item2 { get; set; }
        public bool Item3 { get; set; }  // used for include/exclude flag
        public long Item4 { get; set; }  // used for lexeme's frequency in the corpus
    }
}
