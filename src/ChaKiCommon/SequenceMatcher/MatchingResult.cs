using ChaKi.Entity.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Common.SequenceMatcher
{
    public class MatchingResult
    {
        public List<IndexRange> RangeList { get; set; }
        public List<int> DepList { get; set; }
        public MWE MWE { get; set; }
        public List<CharRange> CharRangeList { get; set; }
        public Uri Url { get; set; }  // 外部辞書でマッチした場合はそのサーバーのURL（Word IDは含まない）

        public MatchingResult()
        {
            this.RangeList = new List<IndexRange>();
            this.DepList = new List<int>();
        }

        public void CalcCharRangeList(IList<Word> words, int startChar)
        {
            this.CharRangeList =
                (from r in this.RangeList
                 where (r.Start >= 0 && r.End >= 0)
                 select new CharRange(words[r.Start].StartChar - startChar, words[r.End - 1].EndChar - startChar))
                .ToList();
        }

        public static string MWEToString(MWE mwe, IList<Word> words, MatchingResult match)
        {
            if (words == null) return string.Empty;

            var tokens = new List<string>();
            for (var i = 0; i < mwe.Items.Count; i++)
            {
                switch (mwe.Items[i].NodeType)
                {
                    case MWENodeType.Word:
                        tokens.Add(mwe.Items[i].Label);
                        break;
                    case MWENodeType.Placeholder:
                        var w = new List<string>();
                        for (var j = match.RangeList[i].Start; j < match.RangeList[i].End; j++)
                        {
                            w.Add(words[j].Lex.Surface);
                        }
                        tokens.Add($"({string.Join(" ", w)})");
                        break;
                    default:
                        tokens.Add("*");
                        break;
                }
            }
            return string.Join(" ", tokens);
        }
    }
}
