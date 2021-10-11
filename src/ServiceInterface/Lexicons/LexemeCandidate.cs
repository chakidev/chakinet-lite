using ChaKi.Common.SequenceMatcher;
using ChaKi.Entity.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Lexicons
{
    public class LexemeCandidate
    {
        public LexemeCandidate(Lexeme lex)
        {
            this.Lexeme = lex;
        }

        public LexemeCandidate(MatchingResult match)
        {
            this.Lexeme = match.MWE.Lex;
            this.Match = match;
        }

        public Lexeme Lexeme { get; set; }

        public MatchingResult Match { get; set; }
        public Uri Url { get; set; }
    }
}
