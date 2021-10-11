using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class LexemeEqualityComparer : IEqualityComparer<Lexeme>
    {
        public bool Equals(Lexeme x, Lexeme y)
        {
            string sx = x.ToString();
            string sy = y.ToString();
            return sx.Equals(sy);
        }

        public int GetHashCode(Lexeme obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}
