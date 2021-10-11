using ChaKi.Entity.Corpora;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChaKi.Common.SequenceMatcher
{
    public class MWEMatcher
    {
        public static List<MatchingResult> Match(IList<Word> words, int wpos, MWE pattern)
        {
            var result = new List<MatchingResult>();
            var patlen = pattern.Items.Count;

            Debug.Write("MWE = ");
            for (int i = 0; i < patlen; i++)
            {
                var item = pattern.Items[i];
                var label = (item.NodeType == MWENodeType.Placeholder) ? "*" : pattern.Items[i].Label;
                Debug.Write($"{label} ");
            }
            Debug.WriteLine("");

            var ranges = new List<IndexRange[]>();
            var initial = new IndexRange[patlen + 1]; // +1 for root at [0]
            initial[0] = new IndexRange(wpos);
            for (var i = 0; i < patlen; i++)
            {
                initial[i + 1] = IndexRange.Invalid;
            }
            // 結果の初期値は、[(wpos,wpos), (-1,-1), (-1,-1), ...]という配列1つ(=Root)
            ranges.Add(initial);
            // まずpatternに含まれるWordのみをマッチさせる.
            for (var i = 0; i < patlen; i++)
            {
                var n = pattern.Items[i];
                if (!(n.NodeType == MWENodeType.Word && n.Label != null))
                {
                    continue;
                }
                var newranges = new List<IndexRange[]>();
                foreach (var q in ranges)
                {
                    var lastpos = q.Last(r => r.Start >= 0).Start;
                    var next = FindNexSubMatch(n, words, lastpos, words.Count);
                    foreach (var r in next)
                    {
                        var qq = new IndexRange[patlen + 1];
                        q.CopyTo(qq, 0);
                        qq[i + 1] = r;
                        newranges.Add(qq);
                    }
                }
                ranges = newranges;
            }
            // 次にpatternに含まれるPlaceholderをマッチさせる.
            for (var i = 0; i < patlen; i++)
            {
                var n = pattern.Items[i];
                if (n.NodeType != MWENodeType.Placeholder)
                {
                    continue;
                }
                n.Label = null; // PlaceholderのLabelはFindNexSubMatch()ではnullであることを前提としているため、"*"になっていても強制的にnullにする.
                var newranges = new List<IndexRange[]>();
                foreach (var q in ranges)
                {
                    int left = 0;
                    try
                    {
                        left = q.Take(i + 1).Last(r => r.End >= 0).End;
                    }
                    catch { /* intentionally left blank */ }
                    int right = words.Count - 1;
                    try
                    {
                        right = q.Skip(i + 1).First(r => r.Start > 0).Start;
                    }
                    catch { /* intentionally left blank */ }
                    var next = FindNexSubMatch(n, words, left, right);
                    foreach (var r in next)
                    {
                        var qq = new IndexRange[patlen + 1];
                        q.CopyTo(qq, 0);
                        qq[i + 1] = r;
                        newranges.Add(qq);
                    }
                }
                ranges = newranges;
            }

            for (var i = 0; i < ranges.Count; i++)
            {
                var r = ranges[i].Skip(1).ToArray();  // 先頭のRoot IndexRangeは除外する.
                if (r.Length != patlen)
                {
//                    continue;
                }
                var match = new MatchingResult();
                for (var j = 0; j < r.Length; j++)
                {
                    match.RangeList.Add(r[j]);
                    match.DepList.Add(pattern.Items[j].DependsTo);
                    match.MWE = pattern;
                }
                result.Add(match);
            }
            return result;
        }

        private static List<IndexRange> FindNexSubMatch(MWENode n, IList<Word> words, int start, int end)
        {
            var result = new List<IndexRange>();
            for (var p = start; p < end; p++)
            {
                var word = words[p];
                var lex = word.Lex;
                var pos = n.POS?.Replace("*", ".*");
                if ((string.IsNullOrEmpty(n.Label) || lex.Surface == n.Label)
                 && (string.IsNullOrEmpty(n.POS) || 
                    Regex.IsMatch(lex.PartOfSpeech.Name, pos))
                /* || lex.CType.Name != node_word.CType || lex.CForm.Name != node_word.CForm*/)
                {
                    result.Add(new IndexRange(p));
                }
            }
            return result;
        }
    }
}

