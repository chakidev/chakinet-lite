using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;
using ChaKi.Entity.Corpora;
using System.Linq;
using Iesi.Collections.Generic;

namespace ChaKi.Service.Common
{
    internal class Util
    {
        private static readonly List<Segment> EmptySegmentList = new List<Segment>();

        private static int BunsetsuTagID = -1;
        private static List<int> SegmentTagIDForGroup = null;
        private static List<int> GroupTagID = null;
        private static List<int> GroupBunsetsuTagID = null;

        private static int MweSegTag = -1;
        private static int MweGrpTag = -1;

        public static void Reset()
        {
            BunsetsuTagID = -1;
            SegmentTagIDForGroup = null;
            GroupTagID = null;
            GroupBunsetsuTagID = null;
            MweSegTag = -1;
            MweGrpTag = -1;
        }

        public static int GetBunsetsuTagId(ISession sess)
        {
            if (BunsetsuTagID == -1)
            {
                // TagEditでTagSetの新バージョンを追加すると、Version違いのBunsetsuタグができるため、Uniqueとならない。
                // Bunsetsuタグはインポート時すなわちVersion=0によってしか生成されないので、Version.ID=0の条件を追加する。
                IQuery q = sess.CreateQuery("select t.ID from Tag t where t.Name='Bunsetsu' and t.Version.ID=0");
                BunsetsuTagID = q.UniqueResult<int>();
            }
            return BunsetsuTagID;
        }

        private static IList<int> GetGroupTagID(ISession sess)
        {
            if (GroupTagID == null)
            {
                GroupTagID = new List<int>();
                IQuery q = sess.CreateQuery(string.Format("select distinct t.ID from Tag t where t.Type='{0}'", Tag.GROUP));
                GroupTagID.AddRange(q.List<int>());
            }
            return GroupTagID;
        }

        /// <summary>
        /// GROUP TAGとSEGMENT TAGは互いに独立したアノテーションであるが、Parallel, Appositionの
        /// WORD GROUPについては、同じ名前を用いることとしている.
        /// ここでは、GROUP TAGと同じ名前を持つSegment TagのIDを得ている.
        /// 将来的にはこの規則は撤廃したい.
        /// </summary>
        /// <param name="sess"></param>
        /// <returns></returns>
        private static IList<int> GetSegmentTagIDForGroups(ISession sess)
        {
            if (SegmentTagIDForGroup == null)
            {
                SegmentTagIDForGroup = new List<int>();
                IQuery q = sess.CreateQuery(string.Format("select distinct t.Name from Tag t where t.Type='{0}'", Tag.GROUP));
                var names = q.List<string>();
                q = sess.CreateQuery(string.Format("select distinct t.ID from Tag t where t.Type='{0}' and t.Name in {1}", Tag.SEGMENT, BuildStringList(names)));
                SegmentTagIDForGroup.AddRange(q.List<int>());
            }
            return SegmentTagIDForGroup;
        }

        private static int GetGroupTagID(ISession sess, string tagname)
        {
            var q = sess.CreateQuery($"select ID from Tag where Type='{Tag.GROUP}' and Name='{tagname}'");
            try
            {
                return q.UniqueResult<int>();
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static int GetSegmentTagID(ISession sess, string tagname)
        {
            var q = sess.CreateQuery($"select ID from Tag where Type='{Tag.SEGMENT}' and Name='{tagname}'");
            try
            {
                return q.UniqueResult<int>();
            }
            catch (Exception)
            {
                return -1;
            }
        }

        //[Obsolete]
        //private static IList<int> GetBunsetsuTagID(ISession sess)
        //{
        //    if (GroupBunsetsuTagID == null)
        //    {
        //        GroupBunsetsuTagID = new List<int>();
        //        GroupBunsetsuTagID.AddRange(GetSegmentTagIDForGroups(sess));
        //        GroupBunsetsuTagID.Add(GetBunsetsuTagId(sess));
        //    }
        //    return GroupBunsetsuTagID;
        //}

        // Groupは直接文と関連ついていないので、segment->group_member->groupと順にたどって、文に付いているGroupを求める.
        public static IList<Group> RetrieveWordGroups(ISession sess, Sentence sen, int projid)
        {
            var sgtags = GetSegmentTagIDForGroups(sess);  // これはSegmentのタグで、Groupに関係あるもの.
            if (sgtags.Count == 0)
            {
                return Group.EmptyList;
            }
            var gtags = GetGroupTagID(sess);    // これはGroupのタグそのもの.
            if (gtags.Count == 0)
            {
                return Group.EmptyList;
            }

            // m_Sentenceに関連する同格・並列Segmentを検索
            List<long> segs = new List<long>();
            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id from segment" +
                    $" WHERE document_id={sen.ParentDoc.ID} AND sentence_id={sen.ID}" +
                    $" AND tag_definition_id IN {BuildIDList(sgtags)}" +
                    $" AND project_id={projid}";
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    segs.Add((long)rdr[0]);
                }
                rdr.Close();
            }
            if (segs.Count == 0)
            {
                return Group.EmptyList;
            }

            // 次にSQLクエリにより、group_memberテーブルから上で得られたSegmentに関係するGroupIDリストを得る.
            List<long> groupids = new List<long>();
            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText = string.Format("SELECT DISTINCT group_id FROM group_member"
                    + " WHERE member_id IN {0} AND object_Type='{1}' ORDER BY group_id ASC",
                    BuildIDList(segs), Tag.SEGMENT);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    groupids.Add((long)rdr[0]);
                }
                rdr.Close();
            }
            if (groupids.Count == 0)
            {
                return Group.EmptyList;
            }

            // 最後にGroupIDからGroupオブジェクトを得る
            var q = sess.CreateQuery(
                string.Format("from Group g where g.Tag.ID in {0} and g.ID in {1} order by g.ID",
                    BuildIDList(gtags), BuildIDList(groupids)));
            return q.List<Group>();
        }

        public static IList<Group> RetrieveMWEGroups(ISession sess, Sentence sen, int projid)
        {
            if (MweSegTag < 0)
            {
                MweSegTag = GetSegmentTagID(sess, "MWE");
            }
            if (MweGrpTag < 0)
            {
                MweGrpTag = GetGroupTagID(sess, "MWE");
            }
            // m_Sentenceに関連するMWE Segmentを検索
            List<long> segs = new List<long>();
            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id from segment"
                    + $" WHERE document_id={sen.ParentDoc.ID} AND sentence_id={sen.ID} "
                    + $"AND tag_definition_id={MweSegTag} AND project_id={projid}";
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    segs.Add((long)rdr[0]);
                }
                rdr.Close();
            }
            if (segs.Count == 0)
            {
                return Group.EmptyList;
            }

            // 次にSQLクエリにより、group_memberテーブルから上で得られたSegmentに関係するGroupIDリストを得る.
            List<long> groupids = new List<long>();
            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT group_id FROM group_member" +
                    $" WHERE member_id IN {BuildIDList(segs)} " +
                    $"AND object_Type='{Tag.SEGMENT}' ORDER BY group_id ASC";
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    groupids.Add((long)rdr[0]);
                }
                rdr.Close();
            }
            if (groupids.Count == 0)
            {
                return Group.EmptyList;
            }

            // 最後にGroupIDからGroupオブジェクトを得る
            var q = sess.CreateQuery(
                $"from Group g where g.Tag.ID={MweGrpTag} and g.ID in {BuildIDList(groupids)} order by g.ID");
            return q.List<Group>();
        }

        /// <summary>
        /// 文節以外のSEGMENT IDリストを得る
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <returns></returns>
        public static IList<long> RetrieveMiscSegments(ISession sess, Sentence sen, int projid)
        {
            var gtag = GetBunsetsuTagId(sess);

            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText = 
                     "SELECT s.id FROM segment s" +
                     " INNER JOIN tag_definition t ON s.tag_definition_id=t.id" +
                    $" WHERE s.document_id={sen.ParentDoc.ID} AND s.sentence_id={sen.ID}" +
                    $" and s.tag_definition_id!={gtag} and s.project_id={projid}";
                var rdr = cmd.ExecuteReader();
                var result = new List<long>();
                while (rdr.Read())
                {
                    result.Add((long)rdr[0]);
                }
                rdr.Close();
                return result;
            }
        }

        /// <summary>
        /// 指定した文節Segmentから出るLINKを得る
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <returns></returns>
        public static Link RetrieveBunsetsuLink(ISession sess, Sentence sen, Segment seg)
        {
            var q = sess.CreateQuery(
                $"from Link l where l.FromSentence.ID = {sen.ID} and l.From.ID = {seg.ID}");
            return q.UniqueResult<Link>();
        }

        /// <summary>
        /// 指定した文節Segmentから出るLINKを得る(高速版)
        /// SQLクエリでTag, Attribute以外にJOINせずに得られるプロパティのみを得る.
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <returns></returns>
        public static Link RetrieveBunsetsuLinkRaw(ISession sess, Sentence sen, Segment seg)
        {
            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText =
                     "SELECT * FROM link inner join tag_definition t on tag_definition_id=t.id" +
                    $" WHERE from_sentence_id={sen.ID}" +
                    $" AND from_segment_id={seg.ID}";
                var rdr = cmd.ExecuteReader();
                var dummy = new Tag(Tag.LINK, "Bunsetsu");
                while (rdr.Read())
                {
                    var lnk = new Link() { Tag = new Tag(Tag.LINK, rdr.GetString(15)) };
                    lnk.ID = rdr.GetInt32(0);
                    lnk.From = new Segment() { ID = rdr.GetInt64(3) };
                    lnk.To = new Segment() { ID = rdr.GetInt64(4) };
                    lnk.Attributes = RetrieveLinkAttributes(sess, lnk.ID);
                    lnk.Comment = rdr.IsDBNull(8) ? null : rdr.GetString(8);
                    return lnk;
                }
                return null;
            }
        }

        // LinkAttributeの主要部(Key,Value,Comment)のみを読み込む高速版
        private static Iesi.Collections.Generic.ISet<LinkAttribute> RetrieveLinkAttributes(ISession sess, long linkid)
        {
            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM link_attribute WHERE link_id={linkid}";
                var rdr = cmd.ExecuteReader();
                var result = new Iesi.Collections.Generic.HashedSet<LinkAttribute>();
                while (rdr.Read())
                {
                    var attr = new LinkAttribute();
                    attr.Key = rdr.GetString(2);
                    attr.Value = rdr.GetString(3);
                    attr.Comment = rdr.IsDBNull(10) ? null : rdr.GetString(8).Trim('"');
                    result.Add(attr);
                }
                return result;
            }
        }

        /// <summary>
        /// 文節へ係るもの以外のLINK IDリストを得る
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <returns></returns>
        public static IList<long> RetrieveMiscLinks(ISession sess, Sentence sen, int projid)
        {
            long bunsetsuid = GetBunsetsuTagId(sess);

            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText =
                      "SELECT l.id FROM link l" +
                    " INNER JOIN tag_definition t ON t.id=l.tag_definition_id" +
                    " INNER JOIN segment s1 ON s1.id=l.from_segment_id" +
                    " INNER JOIN segment s2 ON s2.id=l.to_segment_id" +
                    $" WHERE l.from_sentence_id={sen.ID} AND s1.tag_definition_id !={bunsetsuid}" +
                    $" AND s2.tag_definition_id != {1} AND l.project_id={projid}";
                var rdr = cmd.ExecuteReader();
                var result = new List<long>();
                while (rdr.Read())
                {
                    result.Add((long)rdr[0]);
                }
                rdr.Close();
                return result;
            }
        }

        /// <summary>
        /// startPos~endPosにある文節Segmentを順に得る.
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        /// <returns></returns>
        public static IList<Segment> RetrieveBunsetsuInRange(ISession sess, Sentence sen, int startPos, int endPos)
        {
            long bunsetsuid = GetBunsetsuTagId(sess);

            var q = sess.CreateQuery(
                string.Format("from Segment seg where seg.Doc.ID = {0} and seg.Sentence.ID = {1} and seg.Tag.ID = {2} and seg.StartChar >= {3} and seg.EndChar <= {4} order by seg.StartChar",
                    sen.ParentDoc.ID, sen.ID, bunsetsuid, startPos, endPos));
            return q.List<Segment>();
        }

        /// <summary>
        /// 文に属する文節Segmentを順に得る.
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        /// <returns></returns>
        public static IList<Segment> RetrieveBunsetsu(ISession sess, Sentence sen, int projid)
        {
            long bunsetsuid = GetBunsetsuTagId(sess);

            var q = sess.CreateQuery(
                $"from Segment seg where seg.Doc.ID = {sen.ParentDoc.ID}" +
                $" and seg.Sentence.ID = {sen.ID}" +
                $" and seg.Tag.ID = {bunsetsuid}" +
                $" and seg.Proj.ID = {projid} order by seg.StartChar");
            return q.List<Segment>();
        }

        /// <summary>
        /// 文に属する文節Segmentを順に得る.(高速版)
        /// JOINしないSQLクエリで直接情報のみを得る.(Attributeは含む. 他の得られないプロパティは空なので注意)
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        /// <returns></returns>
        public static IList<Segment> RetrieveBunsetsuRaw(ISession sess, Sentence sen)
        {
            long bunsetsuid = GetBunsetsuTagId(sess);
            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText =
                    $"SELECT * FROM segment WHERE document_id={sen.ParentDoc.ID}" +
                    $" AND sentence_id={sen.ID}" +
                    $" AND tag_definition_id={bunsetsuid}" +
                    $" ORDER BY start_char ASC";
                var segs = new List<Segment>();
                var rdr = cmd.ExecuteReader();
                var dummy = new Tag(Tag.SEGMENT, "Bunsetsu");
                while(rdr.Read())
                {
                    var seg = new Segment() { Tag = dummy };
                    seg.ID = rdr.GetInt64(0);
                    seg.StartChar = rdr.GetInt32(4);
                    seg.EndChar = rdr.GetInt32(5);
                    seg.Comment = rdr.IsDBNull(10)? null: rdr.GetString(10);
                    seg.Attributes = RetrieveSegmentAttributes(sess, seg.ID);
                    segs.Add(seg);
                }
                return segs;
            }
        }

        // SegmentAttributeの主要部(Key,Value,Comment)のみを読み込む高速版
        private static Iesi.Collections.Generic.ISet<SegmentAttribute> RetrieveSegmentAttributes(ISession sess, long segid)        {
            using (var cmd = sess.Connection.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM segment_attribute WHERE segment_id={segid}";
                var rdr = cmd.ExecuteReader();
                var result = new Iesi.Collections.Generic.HashedSet<SegmentAttribute>();
                while (rdr.Read())
                {
                    var attr = new SegmentAttribute();
                    attr.Key = rdr.GetString(2);
                    attr.Value = rdr.GetString(3);
                    attr.Comment = rdr.IsDBNull(10) ? null : rdr.GetString(8).Trim('"');
                    result.Add(attr);
                }
                return result;
            }
        }

        /// <summary>
        /// startpos~endposに完全に含まれるwordを与えられたwordsから探し出し、そのwordsにおけるindex(word番号)をリストにして返す.
        /// </summary>
        /// <param name="words"></param>
        /// <param name="startpos"></param>
        /// <param name="endpos"></param>
        /// <returns></returns>
        public static IList<int> WordsInRange(IList<Word> words, int startpos, int endpos)
        {
            var result = new List<int>();
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].StartChar >= startpos && words[i].EndChar <= endpos)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public static IList<Segment> FindSegmentsInRange(ISession sess, Sentence sen, int startChar, int endChar)
        {
            var q = sess.CreateQuery(
                $"from Segment seg where seg.Sentence.ID={sen.ID} and seg.StartChar >= {startChar} and seg.EndChar <= {endChar}");
            return q.List<Segment>();
        }

        /// <summary>
        /// Segment s がgrouptagで示されるGroupに属していればtrue.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="grouptag"></param>
        /// <returns></returns>
        public static bool IsInGroup(ISession sess, Segment s, string grouptag)
        {
            var q = sess.CreateSQLQuery(
                $"SELECT group_element.ID FROM group_member INNER JOIN group_element ON group_element.id=group_member.group_id WHERE object_type='Segment' AND member_id={s.ID}");
            var gids = q.List<long>();
            foreach (var gid in gids)
            {
                if (sess.Get<Group>(gid).Tag.Name == grouptag)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// bunsから出る係り受けの係り先Bunsetsuを得る.
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <param name="buns"></param>
        /// <returns></returns>
        public static Segment FindDependencyFrom(ISession sess, Sentence sen, Segment buns)
        {
            var q = sess.CreateQuery(
                string.Format("from Link lnk where lnk.FromSentence.ID = {0} and lnk.ToSentence.ID = {0} and lnk.From.ID={1}",
                    sen.ID, buns.ID));
            var lnk = q.UniqueResult<Link>();
            return lnk.To;
        }

        /// <summary>
        /// segから出る文内Linkをすべて得る.
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="sen"></param>
        /// <param name="buns"></param>
        /// <returns></returns>
        public static IList<Link> FindLinksFrom(ISession sess, Sentence sen, Segment seg)
        {
            var q = sess.CreateQuery(
                string.Format("from Link lnk where lnk.FromSentence.ID = {0} and lnk.ToSentence.ID = {0} and lnk.From.ID={1}",
                    sen.ID, seg.ID));
            return q.List<Link>();
        }

        /// <summary>
        /// 与えられたLexemeのリストからLexeme IDを抜き出して
        /// INクエリ条件の右辺："(id1,id2,..)" 形式に変換する。
        /// </summary>
        /// <param name="lexemeList"></param>
        /// <returns></returns>
        public static string BuildLexemeIDList(IList<Lexeme> lexemeList)
        {
            if (lexemeList == null)
            {
                return string.Empty;    // 空の条件Boxにより全Lexemeにマッチした場合は""を返す。
            }
            StringBuilder sb = new StringBuilder("(");
            StringConnector connector = new StringConnector(",");
            foreach (Lexeme lex in lexemeList)
            {
                sb.Append(connector.Get());
                sb.Append(lex.ID);
            }
            sb.Append(")");
            return sb.ToString();
        }

        public static string BuildSentenceIDList(IList<Sentence> sentences)
        {
            StringBuilder sb = new StringBuilder("(");
            StringConnector connector = new StringConnector(",");
            foreach (Sentence sen in sentences)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("{0}", sen.ID);
            }
            sb.Append(")");
            return sb.ToString();
        }

        public static string BuildSegmentIDList(IList<Segment> segments)
        {
            StringBuilder sb = new StringBuilder("(");
            StringConnector connector = new StringConnector(",");
            foreach (Segment seg in segments)
            {
                sb.Append(connector.Get());
                sb.Append(seg.ID);
            }
            sb.Append(")");
            return sb.ToString();
        }

        public static string BuildIDList(IList<int> ids)
        {
            StringBuilder sb = new StringBuilder("(");
            StringConnector connector = new StringConnector(",");
            foreach (int id in ids)
            {
                sb.Append(connector.Get());
                sb.Append(id);
            }
            sb.Append(")");
            return sb.ToString();
        }

        public static string BuildIDList(IList<long> ids)
        {
            StringBuilder sb = new StringBuilder("(");
            StringConnector connector = new StringConnector(",");
            foreach (int id in ids)
            {
                sb.Append(connector.Get());
                sb.Append(id);
            }
            sb.Append(")");
            return sb.ToString();
        }

        public static string BuildStringList(IList<string> strs)
        {
            if (strs == null || strs.Count == 0)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder("(");
            StringConnector connector = new StringConnector(",");
            foreach (string s in strs)
            {
                sb.Append(connector.Get());
                sb.AppendFormat("'{0}'", s);
            }
            sb.Append(")");
            return sb.ToString();
        }

        public static string EscapeQuote(string s)
        {
            if (s == null)
            {
                return string.Empty;
            }
            else
            {
                return s.Replace("'", "''");
            }
        }

        public static long GetNextAvailableID(ISession session, string table)
        {
            long id;
            try
            {
                id = session.CreateSQLQuery(string.Format("SELECT min(id)+1 FROM {0} WHERE id+1 NOT IN (SELECT id FROM {0})", table))
                    .UniqueResult<long>();
            }
            catch (Exception ex)
            {
                return 0;
            }
            return id;
        }

        /// <summary>
        /// true if x is subset of y (including x==y)
        /// List can contain duplicate numbers.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool IsSubset(IEnumerable<long> x, IEnumerable<long> y)
        {
            var tmp = new HashSet<long>(y);
            foreach (var i in x)
            {
                if (!tmp.Contains(i))
                {
                    return false;
                }
            }
            return true;
        }

        public static IEnumerable<CharRange> SegsToRangeList(IEnumerable<Segment> segs)
        {
            var result = new List<CharRange>();
            var curRange = new CharRange();
            foreach (var s in segs)
            {
                if (curRange.Valid)
                {
                    if (curRange.End == s.StartChar) // 連続するRangeならマージする.
                    {
                        curRange.End = s.EndChar;
                    }
                    else // 独立するRangeが見つかったのでresultに追加し、curRangeを更新
                    {
                        result.Add(curRange);
                        curRange = new CharRange(s.StartChar, s.EndChar);
                    }
                }
                else
                {
                    curRange = new CharRange(s.StartChar, s.EndChar);
                }
            }
            if (curRange.Valid)
            {
                result.Add(curRange);
            }
            return result;
        }
    }
}
