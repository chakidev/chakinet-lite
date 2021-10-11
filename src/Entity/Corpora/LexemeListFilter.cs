using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    /// <summary>
    /// LexemeFilterのリスト
    /// WordListにおいて、複数のLexemeの組み合わせ出現リストの
    /// それぞれのLex属性に対してフィルタを行うために使用する。
    /// このクラス自体が、フィルタ状態を考慮したLexemeListの同値比較
    /// を行うインターフェイスを備えている。
    /// </summary>
    public class LexemeListFilter : List<LexemeFilter>, IEqualityComparer<LexemeList>
    {
        public LexemeListFilter(int size)
        {
            for (int i = 0; i < size; i++)
            {
                this.Add(new LexemeFilter());
            }
        }

        public bool IsFiltered(int lexGroup, LP lp)
        {
            return this[lexGroup].IsFiltered(lp);
        }

        public void SetFiltered(int lexGroup, LP lp)
        {
            this[lexGroup].SetFiltered(lp);
        }

        public void ResetFiltered(int lexGroup, LP lp)
        {
            this[lexGroup].ResetFiltered(lp);
        }

        public void Reset()
        {
            foreach (LexemeFilter filter in this)
            {
                filter.Reset();
            }
        }

        public void Default()
        {
            foreach (LexemeFilter filter in this)
            {
                filter.Default();
            }
        }

        public bool Equals(LexemeList x, LexemeList y)
        {
            if (x.Count != y.Count) return false;
            for (int i = 0; i < x.Count; i++)
            {
                Lexeme x_lex = x[i];
                Lexeme y_lex = y[i];
                LexemeFilter filter = this[i];
                foreach (LP tag in Lexeme.PropertyName.Keys)
                {
                    if (!filter.IsFiltered(tag))
                    {
                        if (!x_lex.GetStringProperty(tag).Equals(y_lex.GetStringProperty(tag)))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public int GetHashCode(LexemeList obj)
        {
            int hash = 0;
            for (int i = 0; i < obj.Count; i++)
            {
                if (i >= this.Count) break;
                Lexeme lex = obj[i];
                LexemeFilter filter = this[i];
                foreach (LP tag in Lexeme.PropertyName.Keys)
                {
                    if (!filter.IsFiltered(tag))
                    {
                        hash += lex.GetStringProperty(tag).GetHashCode();
                    }
                }
            }
            return hash;
        }
    }
}
