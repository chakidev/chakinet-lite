using System;
using System.Text;
using System.IO;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Readers;

namespace ChaKi.Service.Readers
{
    public class CabochaMecabReader : CabochaReader
    {
        public CabochaMecabReader(Corpus corpus, LexiconBuilder lb)
            : base(corpus, lb)
        {
        }

        public override Lexeme AddLexeme(string s, LexiconBuilder lb)
        {
            return AddLexeme(s, lb, false);
        }
        public override Lexeme AddLexeme(string s, LexiconBuilder lb, bool baseOnly)
        {
            return lb.AddEntryMecab(s, this.FromDictionary, baseOnly);
        }
    }
}