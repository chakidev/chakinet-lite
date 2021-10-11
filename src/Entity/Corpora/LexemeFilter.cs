using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class LexemeFilter : IEqualityComparer<Lexeme>
    {
        public SerializableDictionary<LP, bool> Filters;

        public LexemeFilter()
        {
            Filters = new SerializableDictionary<LP, bool>();
            foreach (LP tag in Lexeme.PropertyName.Keys)
            {
                Filters.Add(tag, false);
            }
        }

        public LexemeFilter(LexemeFilter org)
        {
            Filters = new SerializableDictionary<LP, bool>();
            foreach (LP tag in Lexeme.PropertyName.Keys)
            {
                Filters.Add(tag, org.Filters[tag]);
            }
        }

        public List<string> GetUnfilteredPropertyNames()
        {
            List<string> result = new List<string>();
            foreach (LP tag in Lexeme.PropertyName.Keys)
            {
                if (!IsFiltered(tag))
                {
                    result.Add(Lexeme.PropertyName[tag]);
                }
            }
            return result;
        }

        public bool IsFiltered(LP lp)
        {
            return Filters[lp];
        }

        public void SetFiltered(LP lp)
        {
            Filters[lp] = true;
        }

        public void ResetFiltered(LP lp)
        {
            Filters[lp] = false;
        }

        public void Reset()
        {
            foreach (LP tag in Lexeme.PropertyName.Keys)
            {
                Filters[tag] = false;
            }
        }

        public void Default()
        {
            foreach (LP tag in Lexeme.PropertyName.Keys)
            {
                Filters[tag] = true;
            }
            Filters[LP.Surface] = false;
        }

        public bool Equals(Lexeme x, Lexeme y)
        {
            foreach (LP tag in Lexeme.PropertyName.Keys)
            {
                if (!Filters[tag])
                {
                    if (!x.GetStringProperty(tag).Equals(y.GetStringProperty(tag)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public int GetHashCode(Lexeme lex)
        {
            int hash = 0;
            foreach (LP tag in Lexeme.PropertyName.Keys)
            {
                if (!Filters[tag])
                {
                    hash += lex.GetStringProperty(tag).GetHashCode();
                }
            }
            return hash;
        }
    }
}
