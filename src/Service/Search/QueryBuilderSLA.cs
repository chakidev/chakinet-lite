using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Search;
using System.Diagnostics;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Common;

namespace ChaKi.Service.Search
{
    public class QueryBuilderSLA : QueryBuilder
    {
        /// <summary>
        /// Dependency Search用のクエリを作成する
        /// 
        /// Document Filter条件は、与えるtargetSentencesに既に反映されている。(cf. BuildDepSearchQuery1)
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="cond"></param>
        /// <returns></returns>
        public override string BuildDepSearchQuery(LexemeResultSet lexemeResultSet, SearchConditions cond, IList<Sentence> targetSentences)
        {
            Debug.Assert(lexemeResultSet != null);
            Debug.Assert(lexemeResultSet.Count > 0);

            DepSearchCondition depCond = cond.DepCond;

            StringBuilder sb = new StringBuilder();
            int iPivot = depCond.GetPivotPos();
            if (iPivot < 0)
            {
                throw new QueryBuilderException("PivotNotFound");
            }
            sb.AppendFormat("select w{0} ", iPivot);
            sb.Append("from ");

            StringConnector connector = new StringConnector(",");
            foreach (LexemeResult res in lexemeResultSet)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Word w{0}", res.No);
            }
            sb.Append(connector.Get());
            sb.AppendFormat("Sentence sen");
            for (int i = 0; i < depCond.BunsetsuConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Segment2 s{0}", i);
            }
            for (int i = 0; i < depCond.LinkConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Link2 k{0}", i);
            }

            sb.Append(" where ");
            sb.Append(BuildDepSearchQueryWhereClauseForBunsetsu(iPivot, depCond, targetSentences, lexemeResultSet, cond.FilterCond.TargetProjectId));

            this.LastQuery = sb.ToString();
            return this.LastQuery;
        }
        
        /// <summary>
        /// BuildDepSearchQueryWhereClauseと目的は同じだが
        /// Bunsetsu Segmentを仮定してパフォーマンスを上げたもの
        /// </summary>
        /// <param name="iPivot"></param>
        /// <param name="depCond"></param>
        /// <param name="targetSentences"></param>
        /// <param name="lexResults"></param>
        /// <returns></returns>
        private string BuildDepSearchQueryWhereClauseForBunsetsu(int iPivot, DepSearchCondition depCond, IList<Sentence> targetSentences, LexemeResultSet lexResults, int targetProjectId)
        {
            // 検索の効率のため、lexResultsをLexemeのヒット数の少ない順に並べ替える。
            lexResults.Sort();

            StringBuilder sb = new StringBuilder();
            StringConnector connector = new StringConnector(" and ");

            // 絞り込み範囲のSentece集合を条件に加える。
            if (targetSentences != null && targetSentences.Count > 0)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("sen.ID in {0}", Util.BuildSentenceIDList(targetSentences));
            }

            foreach (LexemeResult result in lexResults)
            {
                // 同じ文に出現していること。
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.Sen.ID = sen.ID", result.No);
                if (result.LexemeList != null)
                {
                    // かつ、候補LexemeのどれかにIDが一致していること。
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Lex.ID in {1}", result.No, Util.BuildLexemeIDList(result.LexemeList));
                }
                // かつ、その語がSegment(i)に属していること
                sb.Append(connector.Get());
                TagSearchCondition tcond = depCond.BunsetsuConds[result.BunsetsuNo];
                if (tcond.SegmentTag == "Bunsetsu")
                {
                    // 文節の場合のみ、速度を稼ぐために、Bunsetsu.IDによるマッチングを使用
                    sb.AppendFormat("w{0}.Bunsetsu.ID = s{1}.ID", result.No, result.BunsetsuNo);
                }
                else
                {
                    sb.AppendFormat("(w{0}.EndChar > s{1}.StartChar and w{0}.StartChar < s{1}.EndChar)", result.No, result.BunsetsuNo);
                }

                // Word属性による検索条件を追加する.
                sb.Append(BuildWordPropertyQueryWhere(result.No, connector, result.Cond));
 
                // 語の間の順序関係
                // 左接続条件
                if (result.Cond.LeftConnection == '-')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos+1 = w{1}.Pos", result.No - 1, result.No);
                }
                else if (result.Cond.LeftConnection == '<')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos < w{1}.Pos", result.No - 1, result.No);
                }
                else if (result.Cond.LeftConnection == '^')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.StartChar = s{1}.StartChar", result.No, result.BunsetsuNo);
                }
                // 右接続条件
                if (result.Cond.RightConnection == '$')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.EndChar = s{1}.EndChar", result.No, result.BunsetsuNo);
                }
                if (targetProjectId >= 0)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Project.ID={1}", result.No, targetProjectId);
                }
            }
            // Segment間の順序関係
            for (int i = 0; i < depCond.BunsetsuConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("s{0}.Sentence.ID = sen.ID", i);

                TagSearchCondition tcond = depCond.BunsetsuConds[i];

                sb.Append(connector.Get());
                sb.AppendFormat("s{0}.Tag.Name = '{1}'", i, tcond.SegmentTag);

                // Segment Attributes
                foreach (var attr in tcond.SegmentAttrs)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.Attributes2['{1}'] = '{2}'", i, attr.Key, Util.EscapeQuote(attr.Value));
                }

                // 左接続条件
                if (tcond.LeftConnection == '^')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.StartChar = sen.StartChar", i);
                }
                else if (tcond.LeftConnection == '-')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.EndChar = s{1}.StartChar", i - 1, i);
                }
                else if (tcond.LeftConnection == '<')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.StartChar < s{1}.StartChar", i - 1, i);
                }
                // 右接続条件
                if (tcond.RightConnection == '$')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.EndChar = sen.EndChar", i);
                }
            }
            // Segment間のLink条件
            for (int i = 0; i < depCond.LinkConds.Count; i++)
            {
                LinkCondition kcond = depCond.LinkConds[i];
                sb.Append(connector.Get());
                sb.AppendFormat("k{0}.From = s{1} and k{0}.To = s{2}", i, kcond.SegidFrom, kcond.SegidTo);
                if (kcond.TextIsValid)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("k{0}.Tag.Name = '{1}'", i, kcond.Text);
                }
                // Link Attributes
                foreach (var attr in kcond.LinkAttrs)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("k{0}.Attributes2['{1}'] = '{2}'", i, attr.Key, Util.EscapeQuote(attr.Value));
                }

            }
            return sb.ToString();
        }

        /// <summary>
        /// Dependency WordList用のクエリを作成する
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="cond"></param>
        /// <returns></returns>
        public override string BuildDepWordListQuery(LexemeResultSet lexemeResultSet, SearchConditions cond, IList<Sentence> targetSentences)
        {
            Debug.Assert(lexemeResultSet != null);
            Debug.Assert(lexemeResultSet.Count > 0);

            DepSearchCondition depCond = cond.DepCond;

            StringBuilder sb = new StringBuilder();
            int iPivot = depCond.GetPivotPos();
            if (iPivot < 0)
            {
                throw new QueryBuilderException("PivotNotFound");
            }
            sb.Append("select count(*)");
            for (int i = 0; i < lexemeResultSet.Count; i++)
            {
                sb.AppendFormat(",w{0}.Lex", i);
            }
            sb.Append(" from ");

            StringConnector connector = new StringConnector(",");
            foreach (LexemeResult res in lexemeResultSet)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Word w{0}", res.No);
            }
            sb.Append(connector.Get());
            sb.AppendFormat("Sentence sen");
            for (int i = 0; i < depCond.BunsetsuConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Segment2 s{0}", i);
            }
            for (int i = 0; i < depCond.LinkConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Link k{0}", i);
            }

            sb.Append(" where ");
            sb.Append(BuildDepSearchQueryWhereClauseForBunsetsu(iPivot, depCond, targetSentences, lexemeResultSet, cond.FilterCond.TargetProjectId));

            sb.AppendFormat(" group by ");
            connector = new StringConnector(",");
            for (int i = 0; i < lexemeResultSet.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.Lex.ID", i);
            }

            this.LastQuery = sb.ToString();
            return this.LastQuery;
        }

        [Obsolete("For reference only", true)]
        private string BuildDepSearchQueryWhereClause(int iPivot, DepSearchCondition depCond, IList<Sentence> targetSentences, LexemeResultSet lexResults)
        {
            // 検索の効率のため、lexResultsをLexemeのヒット数の少ない順に並べ替える。
            lexResults.Sort();

            StringBuilder sb = new StringBuilder();
            StringConnector connector = new StringConnector(" and ");

            // 絞り込み範囲のSentece集合を条件に加える。
            if (targetSentences != null && targetSentences.Count > 0)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("sen.ID in {0}", Util.BuildSentenceIDList(targetSentences));
            }

            foreach (LexemeResult result in lexResults)
            {
                // 同じ文に出現していること。
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.Sen.ID = sen.ID", result.No);
                if (result.LexemeList != null)
                {
                    // かつ、候補LexemeのどれかにIDが一致していること。
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Lex.ID in {1}", result.No, Util.BuildLexemeIDList(result.LexemeList));
                }
                // かつ、その語がSegment(i)に属していること
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.StartChar >= s{1}.StartChar and w{0}.StartChar < s{1}.EndChar", result.No, result.BunsetsuNo);
                // 語の間の順序関係
                if (result.Cond.LeftConnection == '-')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos+1 = w{1}.Pos", result.No - 1, result.No);
                }
                else if (result.Cond.LeftConnection == '<')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos < w{1}.Pos", result.No - 1, result.No);
                }
                else if (result.Cond.LeftConnection == '^')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos = 0", result.No);
                }
                else if (result.Cond.RightConnection == '$')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.EndChar = s{1}.EndChar", result.No, result.BunsetsuNo);
                }
            }
            // Segment間の順序関係
            for (int i = 0; i < depCond.BunsetsuConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("s{0}.Tag.Name = 'Bunsetsu'", i);

                TagSearchCondition tcond = depCond.BunsetsuConds[i];
                if (tcond.LeftConnection == '^')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.StartChar = sen.StartChar", i);
                }
                else if (tcond.LeftConnection == '-')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.EndChar = s{1}.StartChar", i - 1, i);
                }
                else if (tcond.LeftConnection == '<')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.StartChar < s{1}.StartChar", i - 1, i);
                }
                else if (tcond.RightConnection == '$')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.EndChar = sen.EndChar", i);
                }
            }
            // Segment間のLink条件
            for (int i = 0; i < depCond.LinkConds.Count; i++)
            {
                LinkCondition kcond = depCond.LinkConds[i];
                sb.Append(connector.Get());
                sb.AppendFormat("k{0}.From = s{1} and k{0}.To = s{2}", i, kcond.SegidFrom, kcond.SegidTo);
                if (kcond.TextIsValid)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("k{0}.Tag.Name = '{1}'", i, kcond.Text);
                }
            }
            return sb.ToString();
        }
    }
}
