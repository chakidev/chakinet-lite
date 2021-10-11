using System.Text;
using System.Collections;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Search;
using NHibernate;
using NHibernate.Criterion;
using System.Diagnostics;
using System;
using ChaKi.Entity.Corpora.Annotations;
using System.Collections.Generic;
using ChaKi.Service.Common;

namespace ChaKi.Service.Search
{
    /// <summary>
    /// 検索条件EntityよりHQLクエリ文字列を作成するためのユーティリティークラス
    /// </summary>
    public class QueryBuilder
    {
        public static QueryBuilder Instance { get; set; }
        public string LastQuery { get; set; }

        private static StringBuilder mySb = null;
        private static StringBuilder mySb2 = null;

        static QueryBuilder()
        {
            Instance = new QueryBuilderSLA();
            mySb = new StringBuilder();
            mySb2 = new StringBuilder();
        }

        [Obsolete("For reference only", true)]  // HQL版は使わない --> NHibernate2.1対応のためSQL版へ移行
        public virtual string BuildLexemeQueryHQL(LexemeCondition lexcond)
        {
            throw new NotImplementedException("Obsolete");
#if false
            StringBuilder sb = new StringBuilder();
            StringConnector conn = new StringConnector(" and ");

            foreach (PropertyPair item in lexcond.PropertyPairs)
            {
                string op = (item.Value.IsRegEx)? " regexp ": "=";

                if (item.Key == Lexeme.PropertyName[LP.PartOfSpeech])
                {
                    sb.Append(conn.Get());
                    sb.AppendFormat("l.PartOfSpeech.Name{0}'{1}'", op, item.Value.StrVal);
                }
                else if (item.Key == Lexeme.PropertyName[LP.CType])
                {
                    sb.Append(conn.Get());
                    sb.AppendFormat("l.CType.Name{0}'{1}'", op, item.Value.StrVal);
                }
                else if (item.Key == Lexeme.PropertyName[LP.CForm])
                {
                    sb.Append(conn.Get());
                    sb.AppendFormat("l.CForm.Name{0}'{1}'", op, item.Value.StrVal);
                }
                else if (item.Key == Lexeme.PropertyName[LP.BaseLexeme])
                {
                    sb.Append(conn.Get());
                    sb.AppendFormat("l.BaseLexeme.Surface{0}'{1}'", op, item.Value.StrVal);
                }
                else
                {
                    sb.Append(conn.Get());
                    sb.AppendFormat("l.{0}{1}'{2}'", item.Key, op, item.Value.StrVal);
                }
            }
            if (sb.Length > 0)
            {
                this.LastQuery = "from Lexeme l where " + sb.ToString();
                return this.LastQuery;
            }

            // 全検索（空Box）の場合
            return string.Empty;
#endif
        }

        public virtual string BuildLexemeQuerySQL(LexemeCondition lexcond)
        {
            StringBuilder sb_from;
            StringBuilder sb_where;
            BuildLexemeQuerySQLFromWhere(lexcond, out sb_from, out sb_where);
            if (sb_where.Length > 0)
            {
                this.LastQuery = string.Format("SELECT l.* FROM {0} WHERE {1}", sb_from, sb_where);
                return this.LastQuery;
            }

            // 全検索（空Box）の場合
            return string.Empty;
        }

        public virtual string BuildLexemeIDQuerySQL(LexemeCondition lexcond)
        {
            StringBuilder sb_from;
            StringBuilder sb_where;
            BuildLexemeQuerySQLFromWhere(lexcond, out sb_from, out sb_where);
            if (sb_where.Length > 0)
            {
                this.LastQuery = string.Format("SELECT l.ID FROM {0} WHERE {1}", sb_from, sb_where);
                return this.LastQuery;
            }

            // 全検索（空Box）の場合
            return string.Empty;
        }

        protected virtual void BuildLexemeQuerySQLFromWhere(LexemeCondition lexcond, out StringBuilder sb_from, out StringBuilder sb_where)
        {
            sb_from = new StringBuilder("lexeme l");
            sb_where = new StringBuilder();
            StringConnector conn = new StringConnector(" and ");

            foreach (PropertyPair item in lexcond.PropertyPairs)
            {
                string op = (item.Value.IsRegEx) ? " regexp " : "=";

                if (item.Key == Lexeme.PropertyName[LP.PartOfSpeech])
                {
                    sb_from.Append(",part_of_speech pos");
                    sb_where.Append(conn.Get());
                    sb_where.AppendFormat("l.part_of_speech_id=pos.id and pos.part_of_speech{0}'{1}'", op, Util.EscapeQuote(item.Value.StrVal));
                }
                else if (item.Key == Lexeme.PropertyName[LP.CType])
                {
                    sb_from.Append(",ctype ct");
                    sb_where.Append(conn.Get());
                    sb_where.AppendFormat("l.ctype_id=ct.id and ct.ctype{0}'{1}'", op, Util.EscapeQuote(item.Value.StrVal));
                }
                else if (item.Key == Lexeme.PropertyName[LP.CForm])
                {
                    sb_from.Append(",cform cf");
                    sb_where.Append(conn.Get());
                    sb_where.AppendFormat("l.cform_id=cf.id and cf.cform{0}'{1}'", op, Util.EscapeQuote(item.Value.StrVal));
                }
                else if (item.Key == Lexeme.PropertyName[LP.BaseLexeme])
                {
                    sb_from.Append(",lexeme lbase");
                    sb_where.Append(conn.Get());
                    sb_where.AppendFormat("l.base_lexeme_ref=lbase.id and lbase.surface{0}'{1}'", op, Util.EscapeQuote(item.Value.StrVal));
                }
                else
                {
                    var lp = Lexeme.FindProperty(item.Key);
                    if (lp.HasValue)
                    {
                        sb_where.Append(conn.Get());
                        sb_where.AppendFormat("l.{0}{1}'{2}'", Lexeme.PropertyColumnName[lp.Value], op, Util.EscapeQuote(item.Value.StrVal));
                    }
                }
            }
        }

        public virtual string BuildWordPropertyQuerySQLWhere(int wordindex, StringConnector conn, LexemeCondition lexcond)
        {
            var sb = new StringBuilder();

            foreach (PropertyPair item in lexcond.PropertyPairs)
            {
                var wp = Word.FindProperty(item.Key);
                if (!wp.HasValue)
                {
                    continue;
                }
                if (item.Value.StrVal == null && item.Value.StrVal.Length == 0)
                {
                    continue;
                }
                switch (wp.Value)
                {
                    case WP.Duration:
                    case WP.StartTime:
                    case WP.EndTime:
                        {
                            double val1, val2;
                            var tokens = item.Value.StrVal.Split(',');
                            if (tokens.Length != 2)
                            {
                                throw new Exception(string.Format("Illegal condition:{0}", item.Value.StrVal));
                            }
                            if (!double.TryParse(tokens[0], out val1) || !double.TryParse(tokens[1], out val2))
                            {
                                throw new Exception(string.Format("Illegal condition:{0}", item.Value.StrVal));
                            }
                            sb.Append(conn.Get());
                            sb.AppendFormat("w{0}.{1} BETWEEN {2} AND {3}", wordindex, Word.PropertyColumnName[wp.Value], val1, val2);
                        }
                        break;
                    case WP.HeadInfo:
                        {
                            var ival = Int32.Parse(item.Value.StrVal);
                            sb.Append(conn.Get());
                            sb.AppendFormat("w{0}.{1}={2}", wordindex, Word.PropertyColumnName[wp.Value], ival);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        public virtual string BuildWordPropertyQueryWhere(int wordindex, StringConnector conn, LexemeCondition lexcond)
        {
            var sb = new StringBuilder();

            foreach (PropertyPair item in lexcond.PropertyPairs)
            {
                var wp = Word.FindProperty(item.Key);
                if (!wp.HasValue)
                {
                    continue;
                }
                if (item.Value.StrVal == null && item.Value.StrVal.Length == 0)
                {
                    continue;
                }
                switch (wp.Value)
                {
                    case WP.Duration:
                    case WP.StartTime:
                    case WP.EndTime:
                        {

                            double val1, val2;
                            var tokens = item.Value.StrVal.Split(',');
                            if (tokens.Length != 2)
                            {
                                throw new Exception(string.Format("Illegal condition:{0}", item.Value.StrVal));
                            }
                            if (!double.TryParse(tokens[0], out val1) || !double.TryParse(tokens[1], out val2))
                            {
                                throw new Exception(string.Format("Illegal condition:{0}", item.Value.StrVal));
                            }
                            sb.Append(conn.Get());
                            sb.AppendFormat("w{0}.{1} between {2} and {3}", wordindex, item.Key, val1, val2);
                        }
                        break;
                    case WP.HeadInfo:
                        {
                            var ival = Int32.Parse(item.Value.StrVal);
                            sb.Append(conn.Get());
                            sb.AppendFormat("w{0}.{1}={2}", wordindex, Word.PropertyColumnName[wp.Value], ival);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        [Obsolete("NHibernateのHQLではregexがサポートされていないため、LexemeQueryはHQLではなくSQLを用いる必要がある.", true)]
        public virtual string BuildLexemeQuery(LexemeCondition lexcond)
        {
            StringBuilder sb_from = new StringBuilder("Lexeme l");
            StringBuilder sb_where = new StringBuilder();
            StringConnector conn = new StringConnector(" and ");

            foreach (PropertyPair item in lexcond.PropertyPairs)
            {
                string op = (item.Value.IsRegEx) ? " regexp " : "=";

                if (item.Key == Lexeme.PropertyName[LP.PartOfSpeech])
                {
                    sb_from.Append(",PartOfSpeech pos");
                    sb_where.Append(conn.Get());
                    sb_where.AppendFormat("l.PartOfSpeech.ID=pos.ID and pos.Name{0}'{1}'", op, item.Value.StrVal);
                }
                else if (item.Key == Lexeme.PropertyName[LP.CType])
                {
                    sb_from.Append(",CType ct");
                    sb_where.Append(conn.Get());
                    sb_where.AppendFormat("l.CType.ID=ct.ID and ct.Name{0}'{1}'", op, item.Value.StrVal);
                }
                else if (item.Key == Lexeme.PropertyName[LP.CForm])
                {
                    sb_from.Append(",CForm cf");
                    sb_where.Append(conn.Get());
                    sb_where.AppendFormat("l.CForm.ID=cf.ID and cf.Name{0}'{1}'", op, item.Value.StrVal);
                }
                else if (item.Key == Lexeme.PropertyName[LP.BaseLexeme])
                {
                    sb_from.Append(",Lexeme lbase");
                    sb_where.Append(conn.Get());
                    sb_where.AppendFormat("l.BaseLexeme.ID=lbase.ID and lbase.Surface{0}'{1}'", op, item.Value.StrVal);
                }
                else
                {
                    var lp = Lexeme.FindProperty(item.Key);
                    if (lp.HasValue)
                    {
                        sb_where.Append(conn.Get());
                        sb_where.AppendFormat("l.{0}{1}'{2}'", Lexeme.PropertyName[lp.Value], op, item.Value.StrVal);
                    }
                }
            }
            if (sb_where.Length > 0)
            {
                this.LastQuery = string.Format("select l.ID from {0} where {1}", sb_from, sb_where);
                return this.LastQuery;
            }

            // 全検索（空Box）の場合
            return string.Empty;
        }
        
        /// <summary>
        /// TagSearch用のクエリ文字列構築
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="tagCond"></param>
        /// <returns></returns>
        public virtual string BuildTagSearchQuery(LexemeResultSet lexemeResultSet, SearchConditions cond, List<int> targetSentences)
        {
            Debug.Assert(lexemeResultSet != null);
            Debug.Assert(lexemeResultSet.Count > 0);

            StringBuilder sb = new StringBuilder();
            int iPivot = cond.TagCond.GetPivotPos();
            if (iPivot < 0)
            {
                throw new QueryBuilderException("PivotNotFound");
            }
            sb.Append("select wp from ");

            StringConnector connector = new StringConnector(",");
            sb.Append(connector.Get());
            sb.Append("Word wp");
            foreach (LexemeResult res in lexemeResultSet)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Word w{0}", res.No);
            }

            FilterCondition fCond = (cond.FilterCond.AllEnabled && cond.FilterCond.DocumentFilterValue.Length > 0) ? cond.FilterCond : null;
            if (fCond != null)
            {
                sb.Append(" inner join wp.Sen.ParentDoc.Attributes atr");
            }                

            string whereClause = BuildTagSearchQueryWhereClause(iPivot, cond.TagCond, targetSentences, lexemeResultSet, fCond, cond.FilterCond.TargetProjectId);
            if (whereClause.Length > 0)
            {
                sb.Append(" where ");
                sb.Append(whereClause);
            }

            string limitClause = BuildLimitClause(cond.FilterCond);

            this.LastQuery = sb.ToString();
            return this.LastQuery;
        }

        /// <summary>
        /// TagSearchクエリのwhere節を作成する関数
        /// </summary>
        /// <param name="iPivot"></param>
        /// <param name="tagCond"></param>
        /// <param name="lexResults"></param>
        /// <returns></returns>
        private string BuildTagSearchQueryWhereClause(int iPivot, TagSearchCondition tagCond, List<int> targetSentences, LexemeResultSet lexResults, FilterCondition fCond, int targetProjectId)
        {
            // 検索の効率のため、lexResultsをLexemeのヒット数の少ない順に並べ替える。
            lexResults.Sort();

            StringBuilder sb = new StringBuilder();
            StringConnector connector = new StringConnector(" and ");

            // DocumentTag Filter条件
            if (fCond != null)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("atr.Key='{0}' and atr.Value='{1}'", fCond.DocumentFilterKey, fCond.DocumentFilterValue);
            }
            if (targetProjectId >= 0)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("wp.Project.ID={0}", targetProjectId);
            }

            // 絞り込み範囲のSentece集合を条件に加える。
            if (targetSentences != null && targetSentences.Count > 0)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("w0.Sen.ID in {0}", Util.BuildIDList(targetSentences));
            }

            // WordとSentenceの関係に関する条件
            foreach (LexemeResult result in lexResults)
            {
                // Pivotと同じ文に出現していて、相対位置が指定範囲内であること。
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.Sen.ID = wp.Sen.ID", result.No);
                Range range = result.Cond.RelativePosition;
                if (range.Start == range.End)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos = wp.Pos + ({1})", result.No, range.Start);
                }
                else if (range.Start < range.End)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos >= wp.Pos + ({1})", result.No, range.Start);
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos <= wp.Pos + ({1})", result.No, range.End);
                }
                sb.Append(BuildWordPropertyQueryWhere(result.No, connector, tagCond.LexemeConds[result.No]));

                if (result.LexemeList != null)  // 空Boxに対するLexemeListはnullである(cf. SearchServiceBase.QueryLexemeResultSet)
                {
                    sb.Append(connector.Get());
                    // かつ、候補LexemeのどれかにIDが一致していること。
                    sb.AppendFormat("w{0}.Lex.ID in {1}", result.No, Util.BuildLexemeIDList(result.LexemeList));
                }

                if (targetProjectId >= 0)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Project.ID={1}", result.No, targetProjectId);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// TagSearch用のクエリ文字列構築(SQL)
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="tagCond"></param>
        /// <returns></returns>
        public virtual string BuildTagSearchQuerySQL(SearchConditions cond, List<int> targetSentences)
        {
            StringBuilder sb = new StringBuilder();
            int iPivot = cond.TagCond.GetPivotPos();
            if (iPivot < 0)
            {
                throw new QueryBuilderException("PivotNotFound");
            }
            sb.Append("SELECT wp.* FROM ");

            StringConnector connector = new StringConnector(",");
            sb.Append(connector.Get());
            sb.Append("word wp");
            for (int i = 0; i < cond.TagCond.LexemeConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("word w{0}", i);
            }

            FilterCondition fCond = (cond.FilterCond.AllEnabled && cond.FilterCond.DocumentFilterValue.Length > 0) ? cond.FilterCond : null;
            if (fCond != null)
            {
                sb.Append(" INNER JOIN sentence sen ON sen.ID=wp.sentence_id");
                sb.Append(" INNER JOIN document doc ON doc.document_id=sen.document_id");
                sb.Append(" INNER JOIN documenttag atr ON atr.document_id=doc.document_id");
            }

            string whereClause = BuildTagSearchQueryWhereClauseSQL(iPivot, cond.TagCond, targetSentences, fCond, cond.FilterCond.TargetProjectId);
            if (whereClause.Length > 0)
            {
                sb.Append(" WHERE ");
                sb.Append(whereClause);
            }
            sb.Append(" ORDER BY wp.ID ASC ");

            string limitClause = BuildLimitClause(cond.FilterCond);

            this.LastQuery = sb.ToString();
            return this.LastQuery;
        }

        /// <summary>
        /// TagSearchクエリのwhere節を作成する関数 (SQL)
        /// </summary>
        /// <param name="iPivot"></param>
        /// <param name="tagCond"></param>
        /// <param name="lexResults"></param>
        /// <returns></returns>
        private string BuildTagSearchQueryWhereClauseSQL(int iPivot, TagSearchCondition tagCond, List<int> targetSentences, FilterCondition fCond, int targetProjectId)
        {
            StringBuilder sb = new StringBuilder();
            StringConnector connector = new StringConnector(" AND ");

            // DocumentTag Filter条件
            if (fCond != null)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("atr.tag='{0}' and atr.description='{1}'", fCond.DocumentFilterKey, fCond.DocumentFilterValue);
            }
            if (targetProjectId >= 0)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("wp.project_id={0}", targetProjectId);
            }

            // 絞り込み範囲のSentece集合を条件に加える。
            if (targetSentences != null && targetSentences.Count > 0)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("w0.sentence_id in {0}", Util.BuildIDList(targetSentences));
            }

            // WordとSentenceの関係に関する条件
            for (int i = 0; i < tagCond.LexemeConds.Count; i++)
            {
                // Pivotと同じ文に出現していて、相対位置が指定範囲内であること。
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.sentence_id = wp.sentence_id", i);
                Range range = tagCond.LexemeConds[i].RelativePosition;
                if (range.Start == range.End)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.position = wp.position + ({1})", i, range.Start);
                }
                else if (range.Start < range.End)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.position >= wp.position + ({1})", i, range.Start);
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.position <= wp.position + ({1})", i, range.End);
                }
                // Word属性に対する制約条件
                sb.Append(QueryBuilder.Instance.BuildWordPropertyQuerySQLWhere(i, connector, tagCond.LexemeConds[i]));

                string lq = QueryBuilder.Instance.BuildLexemeIDQuerySQL(tagCond.LexemeConds[i]);
                if (lq.Length > 0)  // 空Boxに対するLexemeQueryは""である.
                {
                    sb.Append(connector.Get());
                    // かつ、候補LexemeのどれかにIDが一致していること。
                    sb.AppendFormat("w{0}.lexeme_id in ({1})", i, lq);
                }

                if (targetProjectId >= 0)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.project_id={1}", i, targetProjectId);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// WordList用のクエリ文字列構築
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="tagCond"></param>
        /// <returns></returns>
        public virtual string BuildWordListQuery(LexemeResultSet lexemeResultSet, SearchConditions cond, List<int> targetSentences)
        {
            Debug.Assert(lexemeResultSet != null);
            Debug.Assert(lexemeResultSet.Count > 0);

            TagSearchCondition tagCond = cond.TagCond;

            int iPivot = tagCond.GetPivotPos();
            if (iPivot < 0)
            {
                throw new QueryBuilderException("PivotNotFound");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("select count(*)");
            for (int i = 0; i < lexemeResultSet.Count; i++)
            {
                sb.AppendFormat(",w{0}.Lex", i);
            }
            sb.Append(" from ");

            StringConnector connector = new StringConnector(",");
            sb.Append(connector.Get());
            sb.Append("Word wp");
            foreach (LexemeResult res in lexemeResultSet)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Word w{0}", res.No);
            }
            FilterCondition fCond = (cond.FilterCond.AllEnabled && cond.FilterCond.DocumentFilterValue.Length > 0) ? cond.FilterCond : null;
            if (fCond != null)
            {
                sb.Append(" inner join wp.Sen.ParentDoc.Attributes atr");
            }                

            string whereClause = BuildTagSearchQueryWhereClause(iPivot, tagCond, targetSentences, lexemeResultSet, fCond, cond.FilterCond.TargetProjectId);
            if (whereClause.Length > 0)
            {
                sb.Append(" where ");
                sb.Append(whereClause);
            }

            sb.AppendFormat(" group by ");
            connector = new StringConnector(",");
            for (int i = 0; i < lexemeResultSet.Count; i++) {
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.Lex.ID", i);
            }

            this.LastQuery = sb.ToString();
            return this.LastQuery;
        }

        /// <summary>
        /// StringSearch用のクエリ文字列構築
        /// Obsolete
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="tagCond"></param>
        /// <returns></returns>
        [Obsolete("For reference only", true)]
        public virtual string BuildStringSearchQuery(Corpus c, SearchConditions cond)
        {
            StringSearchCondition strCond = cond.StringCond;
            if (strCond.Pattern.Length == 0)
            {
                BuildSentenceListQuery(c, cond);
                return this.LastQuery;
            }
            string pattern;
            if (cond.StringCond.IsRegexp)
            {
                pattern = string.Format("s.Text regexp '^.*{0}.*$'", strCond.Pattern);
            }
            else
            {
                pattern = string.Format("s.Text like '%{0}%'", strCond.Pattern);
            }
            FilterCondition fCond = cond.FilterCond;
            if (fCond.AllEnabled && fCond.DocumentFilterValue.Length > 0)
            {
                this.LastQuery = string.Format("select s from Sentence s inner join s.ParentDoc.Attributes atr where atr.Key='{0}' and atr.Value='{1}' and {2}",
                    fCond.DocumentFilterKey, fCond.DocumentFilterValue, pattern);
            }
            else
            {
                this.LastQuery = string.Format("from Sentence s where {0}", pattern);
            }
            return this.LastQuery;
        }

        public virtual string BuildSentenceListQuery(Corpus c, SearchConditions cond)
        {
            FilterCondition fCond = cond.FilterCond;
            List<int> ids = null;
            cond.SentenceCond.Ids.TryGetValue(c, out ids);
            if (fCond.AllEnabled && fCond.DocumentFilterValue.Length > 0)
            {
                this.LastQuery = string.Format("select s from Sentence s inner join s.ParentDoc.Attributes atr where atr.Key='{0}' and atr.Value='{1}'",
                    fCond.DocumentFilterKey, fCond.DocumentFilterValue);
            }
            else
            {
                if (ids != null)
                {
                    this.LastQuery = string.Format("from Sentence s where s.ID in {0}", Util.BuildIDList(ids));
                }
                else
                {
                    this.LastQuery = "from Sentence";
                }
                this.LastQuery += " order by ID asc";
            }
            return this.LastQuery;
        }

        public virtual string BuildSentenceContextQuery(Sentence sen, int contextCount)
        {
            int minID = Math.Max(0, sen.ID - contextCount);
            int maxID = sen.ID + contextCount;

            this.LastQuery = string.Format("from Sentence s where s.ParentDoc.ID={0} and s.ID>={1} and s.ID<={2} order by s.ID", sen.ParentDoc.ID, minID, maxID);
            return this.LastQuery;
        }

        public virtual string BuildSentenceQuery(int index)
        {
            this.LastQuery = string.Format("from Sentence s where s.ID = {0}", index);
            return this.LastQuery;
        }

        /// <summary>
        /// Dependency Search用のクエリを作成する
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="cond"></param>
        /// <returns></returns>
        [Obsolete("QueryBuilderSLAのオーバーライドを使用してください.")]
        public virtual string BuildDepSearchQuery(LexemeResultSet lexemeResultSet, SearchConditions cond, IList<Sentence> targetSentences)
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
                sb.AppendFormat("Bunsetsu b{0}", i);
            }

            string whereClause = BuildDepSearchQueryWhereClause(iPivot, depCond, targetSentences, lexemeResultSet);
            if (whereClause.Length > 0)
            {
                sb.Append(" where ");
                sb.Append(whereClause);
            }

            this.LastQuery = sb.ToString();
            return this.LastQuery;
        }

        [Obsolete("QueryBuilderSLAのオーバーライドを使用してください.")]
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
                sb.AppendFormat("w0.Sen.ID in {0}", Util.BuildSentenceIDList(targetSentences));
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
                // かつ、その語がBunsetsu(i)に属していること
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.Bunsetsu.ID = b{1}.ID", result.No, result.BunsetsuNo);

                // Word属性による検索条件を追加する.
                sb.Append(BuildWordPropertyQueryWhere(result.No, connector, result.Cond));
                
                // 語の間の順序関係
                if (result.Cond.LeftConnection == '-')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos+1 = w{1}.Pos", result.No-1, result.No);
                }
                else if (result.Cond.LeftConnection == '<')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Pos < w{1}.Pos", result.No-1, result.No);
                }
                else if (result.Cond.LeftConnection == '^')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.StartChar = b{1}.StartChar", result.No, result.BunsetsuNo);
                }
                if (result.Cond.RightConnection == '$')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.EndChar = b{1}.EndChar", result.No, result.BunsetsuNo);
                }
            }
            // Segment間の順序関係
            for (int i = 0; i < depCond.BunsetsuConds.Count; i++) {
                TagSearchCondition tcond = depCond.BunsetsuConds[i];
                if (tcond.LeftConnection == '^')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.Pos=0", i);
                }
                else if (tcond.LeftConnection == '-')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.Pos = s{1}.Pos-1", i-1, i);
                }
                else if (tcond.LeftConnection == '<')
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("s{0}.Pos < s{1}.Pos", i-1, i);
                }
                if (tcond.RightConnection == '$')
                {
                    throw new NotImplementedException("'$' is not implemented yet");
//                    sb.Append(connector.Get());
//                    sb.AppendFormat("s{0}.EndChar = sen.EndChar", i);
                }
            }
            // Segment間のLink条件
            for (int i = 0; i < depCond.LinkConds.Count; i++)
            {
                /*
                LinkCondition kcond = depCond.LinkConds[i];
                sb.Append(connector.Get());
                sb.AppendFormat("k{0}.From = s{1} and k{0}.To = s{2}", i, kcond.SegidFrom, kcond.SegidTo);
                if (kcond.Text.Length > 0)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("k{0}.Text = '{1}'", i, kcond.Text);
                }
                 */
            }
            return sb.ToString();
        }

        /// <summary>
        /// Dependency WordList用のクエリを作成する
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="cond"></param>
        /// <returns></returns>
        public virtual string BuildDepWordListQuery(LexemeResultSet lexemeResultSet, SearchConditions cond, IList<Sentence> targetSentences)
        {
            throw new NotImplementedException("Invalid call to base-class implementation. Instance must be a QueryBuilderSLA.");
        }

        /// <summary>
        /// Dependency Search用のクエリを作成する(1)
        /// </summary>
        /// <param name="lexemeResultSet"></param>
        /// <param name="cond"></param>
        /// <returns></returns>
        public string BuildDepSearchQuery1(LexemeResultSet lexemeResultSet, SearchConditions cond, List<int> targetSentences)
        {
            Debug.Assert(lexemeResultSet != null);
            Debug.Assert(lexemeResultSet.Count > 0);

            DepSearchCondition depCond = cond.DepCond;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("select distinct sen from ");

            StringConnector connector = new StringConnector(",");
            foreach (LexemeResult res in lexemeResultSet)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Word w{0}", res.No);
            }
            sb.Append(connector.Get());
            sb.AppendFormat("Sentence sen");
            FilterCondition fCond = (cond.FilterCond.AllEnabled && cond.FilterCond.DocumentFilterValue.Length > 0) ? cond.FilterCond : null;
            if (fCond != null)
            {
                sb.Append(" inner join sen.ParentDoc.Attributes atr");
            }

            sb.Append(" where ");
            sb.Append(BuildDepSearchQueryWhereClause1(depCond, targetSentences, lexemeResultSet, fCond, cond.FilterCond.TargetProjectId));

            this.LastQuery = sb.ToString();
            return this.LastQuery;
        }

        private string BuildDepSearchQueryWhereClause1(DepSearchCondition depCond, List<int> targetSentences, LexemeResultSet lexResults, FilterCondition fCond, int targetProjectId)
        {
            // 検索の効率のため、lexResultsをLexemeのヒット数の少ない順に並べ替える。
            lexResults.Sort();

            StringBuilder sb = new StringBuilder();
            StringConnector connector = new StringConnector(" and ");

            // DocumentTag Filter条件
            if (fCond != null)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("atr.Key='{0}' and atr.Value='{1}'", fCond.DocumentFilterKey, fCond.DocumentFilterValue);
            }

            // 絞り込み範囲のSentece集合を条件に加える。
            if (targetSentences != null && targetSentences.Count > 0)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("w0.Sen.ID in {0}", Util.BuildIDList(targetSentences));
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
                    sb.AppendFormat("w{0}.Lex.ID in {1}", result.No, result.QString);
                }
                if (targetProjectId >= 0)
                {
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Project.ID={1}", result.No, targetProjectId);
                }
            }
            return sb.ToString();
        }

        [Obsolete("For reference only", true)]
        public string BuildDepSearchQuery2(Sentence sen, IList<Segment> segments, LexemeResultSet lexemeResultSet, SearchConditions cond)
        {
            Debug.Assert(lexemeResultSet != null);
            Debug.Assert(lexemeResultSet.Count > 0);

            DepSearchCondition depCond = cond.DepCond;

            StringBuilder sb = mySb;// new StringBuilder();
            sb.Length = 0;
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
            for (int i = 0; i < depCond.BunsetsuConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Segment s{0}", i);
            }
            for (int i = 0; i < depCond.LinkConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("Link k{0}", i);
            }
            sb.Append(" where ");
            sb.Append(BuildDepSearchQueryWhereClause2(sen, segments, iPivot, depCond, lexemeResultSet));

            this.LastQuery = sb.ToString();
            return this.LastQuery;
        }

        [Obsolete("For reference only")]
        private string BuildDepSearchQueryWhereClause2(Sentence sen, IList<Segment> segments, int iPivot, DepSearchCondition depCond, LexemeResultSet lexResults)
        {
            // 検索の効率のため、lexResultsをLexemeのヒット数の少ない順に並べ替える。
//            lexResults.Sort();

            StringBuilder sb = mySb2;   // new StringBuilder();
            sb.Length = 0;
            StringConnector connector = new StringConnector(" and ");
            string seglist = Util.BuildSegmentIDList(segments);
            for (int i = 0; i < depCond.BunsetsuConds.Count; i++)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("s{0}.ID in {1}", i, seglist);
            }
            foreach (LexemeResult result in lexResults)
            {
                // 同じ文に出現していること。
                sb.Append(connector.Get());
                sb.AppendFormat("w{0}.Sen.ID = {1}", result.No, sen.ID);
                if (result.LexemeList != null)
                {
                    // かつ、候補LexemeのどれかにIDが一致していること。
                    sb.Append(connector.Get());
                    sb.AppendFormat("w{0}.Lex.ID in {1}", result.No, result.QString);
                }
                // かつ、その語がSegment(i)に属していること
                sb.Append(connector.Get());
#if false
                // 文節に限定しているので、一般Segmentの検索は不可。
                sb.AppendFormat("w{0}.Bunsetsu.ID = s{1}.ID", result.No, result.BunsetsuNo);
#else
                sb.AppendFormat("w{0}.StartChar >= s{1}.StartChar and w{0}.StartChar < s{1}.EndChar", result.No, result.BunsetsuNo);
#endif
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
                    sb.AppendFormat("w{0}.StartChar = s{1}.StartChar", result.No, result.BunsetsuNo);
                }
                if (result.Cond.RightConnection == '$')
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
            }
            return sb.ToString();
        }

        public string BuildLimitClause(FilterCondition cond)
        {
            StringBuilder sb = new StringBuilder();

            if (cond.ResultsetFilter.FetchType == FetchType.Incremental)
            {

            }

            return sb.ToString();
        }
    }
}
