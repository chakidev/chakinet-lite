using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Search;
using System;

namespace ChaKi.Entity.Settings
{
    public class WordColorSetting : ICloneable
    {
        // 条件
        public bool IsUsed { get; set; }
        public LexemeCondition MatchingRule { get; set; }
        public int MinFrequency { get; set; }
        public int MaxFrequency { get; set; }

        // 条件に合った場合の色設定(Argb)
        public int BgColor { get; set; }
        public int FgColor { get; set; }


        public WordColorSetting()
        {
            Reset();
        }

        public void Reset()
        {
            this.IsUsed = false;
            this.MatchingRule = new LexemeCondition();
            this.MinFrequency = -1;
            this.MaxFrequency = -1;
            unchecked
            {
                this.BgColor = (int)0xFFFFFFF0;    // Ivory
                this.FgColor = (int)0xFF000000;    // Black
            }
        }

        public object Clone()
        {
            WordColorSetting newobj = new WordColorSetting();
            newobj.CopyFrom(this);
            return newobj;
        }

        public void CopyFrom(WordColorSetting org)
        {
            this.IsUsed = org.IsUsed;
            this.MatchingRule = (LexemeCondition)(org.MatchingRule.Clone());
            this.MinFrequency = org.MinFrequency;
            this.MaxFrequency = org.MaxFrequency;
            this.BgColor = org.BgColor;
            this.FgColor = org.FgColor;
        }

        public bool Match(Lexeme lex)
        {
            if (lex == null)
            {
                return false;
            }
            if (!this.IsUsed)
            {
                return false;
            }
            if (this.MinFrequency >= 0 && lex.Frequency < this.MinFrequency)
            {
                return false;
            }
            if (this.MaxFrequency >= 0 && lex.Frequency > this.MaxFrequency)
            {
                return false;
            }
            if (!this.MatchingRule.MatchRegexp(lex))
            {
                return false;
            }
            return true;
        }
    }
}
