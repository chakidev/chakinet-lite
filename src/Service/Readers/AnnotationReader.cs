using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using System.IO;
using System.Xml;
using ChaKi.Service.Database;
using ChaKi.Service.Import;
using NHibernate.Exceptions;
using ChaKi.Service.Common;
using System.Data.Common;
using ChaKi.Common;

namespace ChaKi.Service.Readers
{
    public class AnnotationReader
    {
        private ImportOperationContext m_Context;
        private IProgress m_Progress;

        private Dictionary<string, long> m_LastIndex = new Dictionary<string, long>();

        public AnnotationReader(ImportOperationContext context, IProgress progress)
        {
            m_Context = context;
            m_Progress = progress;
        }

        /// <summary>
        /// CabochaデータをCorpusに読み込む（文節情報はSegment,Linkテーブルに入れる）
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        public void ReadFromFileSLA(string path)
        {
            using (var streamReader = new StreamReader(path, Encoding.UTF8))
            {
                // count lines
                var nLine = 0;
                while (streamReader.ReadLine() != null)
                {
                    nLine++;
                }
                if (m_Progress != null) m_Progress.ProgressMax = nLine;
                streamReader.BaseStream.Position = 0; // rewind

                // main part
                ReadFromStreamSLA(streamReader);
            }
        }

        /// <summary>
        /// AnnotationデータをストリームからDBに読み込む。
        /// </summary>
        /// <param name="rdr">ストリームリーダ</param>
        /// <param name="doc">読み込み先Document</param>
        public void ReadFromStreamSLA(TextReader rdr)
        {
            string s;
            int n = 0;
            Sentence currentSentence = null;
            Document currentDoc = null;
            Annotation lastAnnotation = null;
            // 今読んでいるSentenceに付属するBunsetsu以外のSegmentリスト
            //   追記(2020/09/15): ValueをListに変更. 異なるGroup属するに同一位置・タグのSegmentが複数存在する場合を顧慮.
            //       複数の場合、最新(Last)を使う.
            var segmentsOfSentence = new Dictionary<string, List<Segment>>();
            // SEGMENT (_S付きでない)のDocument単位の出現リスト。
            // LINKタグのインデックスはこのリストのインデックスとなる。（エラーのあるSEGMENTはNULLが格納される）
            var segmentsOfDoc = new Dictionary<string, List<Segment>>();

            while ((s = rdr.ReadLine()) != null)
            {
                s = CabochaReader.Cleanup(s);  // ファイル途中のBOM（catした時に残っている場合がある）を削除する
                if (s.StartsWith("#! DOCID"))
                {
                    // Cabocha拡張タグ - 書誌情報
                    string[] fields = s.Split(new char[] { '\t' });
                    if (fields.Length != 3)
                    {
                        throw new Exception($"Invalid DOCID: field length != 3: {s}");
                    }
                    var docid = Int32.Parse(fields[1]);
                    var q = m_Context.Session.CreateQuery($"from Document where ID={docid}");
                    var doc = q.UniqueResult<Document>();
                    if (doc != null)
                    {
//                        AddDocumentTag(doc, fields[2]);
                    }
                    else
                    {
                        throw new Exception($"Invalid DOCID: docid not found in current corpus: id={docid}");
                    }
                }
                else if (s.StartsWith("#! DOC"))
                {
                    string[] fields = s.Split(new char[] { ' ', '\t' });
                    try
                    {
                        var docid = int.Parse(fields[2]);
                        var q = m_Context.Session.CreateQuery($"from Document where ID={docid}");
                        currentDoc = q.UniqueResult<Document>();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Invalid DOC tag (Format Error): {0}...", s);
                    }
                    segmentsOfDoc.Clear();
                }
                else if (s.StartsWith("#! SEGMENT_S"))
                {
                    // Cabocha拡張タグ - 文内Segment
                    lastAnnotation = AddSegment(currentSentence, s, segmentsOfSentence);
                }
                else if (s.StartsWith("#! SEGMENT"))
                {
                    // Cabocha拡張タグ - Document内Segment
                    lastAnnotation = AddSegment(currentDoc, s, segmentsOfDoc);
                }
                else if (s.StartsWith("#! LINK_S"))
                {
                    // Cabocha拡張タグ - 文内Segment
                    lastAnnotation = AddLink(currentSentence, s, segmentsOfSentence);
                }
                else if (s.StartsWith("#! LINK"))
                {
                    // Cabocha拡張タグ - Document内Segment
                    lastAnnotation = AddLink(currentDoc, s, segmentsOfDoc);
                }
                else if (s.StartsWith("#! GROUP_S"))
                {
                    // Cabocha拡張タグ - 文内Group
                    lastAnnotation = AddGroup(currentSentence, s, segmentsOfSentence);
                }
                else if (s.StartsWith("#! GROUP"))
                {
                    // Cabocha拡張タグ - Document内Group
                    lastAnnotation = AddGroup(currentDoc, s, segmentsOfDoc);
                }
                else if (s.StartsWith("#! ATTR"))
                {
                    // Cabocha拡張タグ - Segment/Link/Group属性
                    if (lastAnnotation != null)
                    {
                        AddAttribute(currentSentence, s, lastAnnotation);
                    }
                }
                else if (s.StartsWith("#! NAMESPACE"))
                {
                    // Cabocha拡張タグ - Namespace定義
                    // Namespace定義はCorpus属性として保持する.
                    // 本来はTagSetDefinitionテーブルに入れるべきだが、スキーマに適当なカラムが存在しない.
                    AddNamespaceDefinition(s);
                }
                else if (s.StartsWith("#"))
                {
                    // Ignore
                }
                else if (s.StartsWith("BOS"))
                {
                    string[] fields = s.Split(new char[] { ' ', '\t' });
                    try
                    {
                        var senid = int.Parse(fields[1]);
                        var q = m_Context.Session.CreateQuery($"from Sentence where ID={senid}");
                        currentSentence = q.UniqueResult<Sentence>();
                        segmentsOfSentence.Clear();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ignoring an invalid extension BOS line (Format Error): {0}...", s);
                    }
                }
                else if (s.StartsWith("EOS"))
                {
                    lastAnnotation = null;
                }
                n++;
                if (m_Progress != null) m_Progress.ProgressCount = n;
            }
        }

        /// <summary>
        /// 拡張CabochaFormat (旧ChaKiからのimport)に埋め込まれたDOCタグからDocument Tagを生成する。
        /// </summary>
        /// <param name="xmlstr"></param>
        /// <returns></returns>
        private void AddDocumentTag(Document doc, string xmlstr)
        {
            string s;
            int sp = xmlstr.IndexOf('<');
            if (sp >= 0)
            {
                if (sp > 0)
                {
                    s = string.Format("<Root><FilePath>{0}</FilePath>{1}</Root>", xmlstr.Substring(0, sp), xmlstr.Substring(sp));
                }
                else
                {
                    s = string.Format("<Root>{0}</Root>", xmlstr);
                }
            }
            else
            {
                s = string.Format("<Root><FilePath>{0}</FilePath></Root>", xmlstr);
            }
            using (var trdr = new StringReader(s))
            {
                var xrdr = XmlReader.Create(trdr);
                while (xrdr.Read())
                {
                    if (xrdr.Name.Equals("Root")) continue;
                    var dt = new DocumentAttribute();
                    dt.ID = DocumentAttribute.UniqueID++;
                    dt.Key = xrdr.Name;
                    dt.Value = xrdr.ReadString();
                    doc.Attributes.Add(dt);
                }
            }
        }

        public static string GetSegmentHash(Segment seg)
        {
            var s = seg.StartChar;
            var e = seg.EndChar;
            var t = seg.Tag.Name;

            return $"[{s}:{e}:{t}]";
        }

        public static string GetSegmentHash(Segment seg, int senOffset)
        {
            var s = seg.StartChar - senOffset;
            var e = seg.EndChar - senOffset;
            var t = seg.Tag.Name;

            return $"[{s}:{e}:{t}]";
        }


        /// <summary>
        /// 次の形式の文内SEGMENT_S定義からSegmentを生成する.
        ///  #! SEGMENT_S [segmentname] [StartPos_in_Sentence] [EndPos_in_Sentence] "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="s"></param>
        private Segment AddSegment(Sentence sen, string s, Dictionary<string, List<Segment>> segsOfSentence)
        {
            var fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension SEGMENT_S line (Format Error): {0}...", s);
                return null;
            }
            string tagname = fields[2].Trim('"');
            try
            {
                var seg = new Segment();
                seg.ID = GetNextId("segment");
                seg.Tag = m_Context.Proj.FindTag(Tag.SEGMENT, tagname);
                if (seg.Tag == null)
                {
                    var t = new Tag(Tag.SEGMENT, tagname) { Parent = m_Context.Proj.TagSetList[0], Version = m_Context.TSVersion };
                    m_Context.Proj.TagSetList[0].AddTag(t);
                    seg.Tag = t;
                    SaveTag(t);
                }
                var from = Int32.Parse(fields[3]);
                var to = Int32.Parse(fields[4]);
                seg.Doc = sen.ParentDoc;
                seg.Sentence = sen;
                seg.StartChar = from + sen.StartChar;
                seg.EndChar = to + sen.StartChar;
                seg.Proj = m_Context.Proj;
                seg.User = m_Context.User;
                seg.Version = seg.Tag.Version;
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        var comment = s.Substring(p + 1, s.Length - p - 2);
                        seg.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
                    }
                }
                var hash = GetSegmentHash(seg, sen.StartChar);
                if (!segsOfSentence.TryGetValue(hash, out var list))
                {
                    list = new List<Segment>();
                    segsOfSentence.Add(hash, list);
                }
                segsOfSentence[hash].Add(seg);

                using (var cmd = m_Context.Session.Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO segment VALUES("
                        + $"{seg.ID},{seg.Tag.ID},{seg.Version.ID},"
                        + $"{seg.Doc.ID},{seg.StartChar},{seg.EndChar},'',"
                        + $"{m_Context.Proj.ID},{m_Context.User?.ID??0},"
                        + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(seg.Comment)}',"
                        + $"{seg.Sentence.ID},{seg.Lex?.ID??0})";
                    cmd.ExecuteNonQuery();
                }
                if (tagname == "Bunsetsu")
                {
                    UpdateWordBunsetsuRelations(seg);
                }
                return seg;
            }
            catch (DbException)
            {
                throw;
            }
            catch
            {
                Console.WriteLine("Ignoring an invalid extension SEGMENT_S line (Parse Error): {0}...", s);
                return null;
            }
        }

        private void SaveTag(Tag t)
        {
            t.ID = (int)GetNextId("tag_definition");
            using (var cmd = m_Context.Session.Connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO tag_definition VALUES("
                    + $"{t.ID},{m_Context.TagSet.ID},"
                    + $"'{t.Type}','{t.Name}','{t.Description}',{t.Version.ID})";
                cmd.ExecuteNonQuery();
            }
        }

        private long GetNextId(string tablename)
        {
            if (!m_LastIndex.TryGetValue(tablename, out var lastid))
            {
                lastid = -1;
                using (var q = m_Context.Session.Connection.CreateCommand())
                {
                    q.CommandText = $"SELECT MAX(ID) FROM {tablename}";
                    using (var rdr = q.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr.IsDBNull(0))
                            {
                                break;
                            }
                            lastid = rdr.GetInt64(0);
                            break;
                        }
                    }
                    m_LastIndex.Add(tablename, lastid);
                }
            }
            lastid++;
            m_LastIndex[tablename] = lastid;
            return lastid;
        }

        /// <summary>
        /// 次の形式の文内SEGMENT定義からSegmentを生成する.
        ///  #! SEGMENT [segmentname] [StartPos_in_Document] [EndPos_in_Document] "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="s"></param>
        private Segment AddSegment(Document doc, string s, Dictionary<string, List<Segment>> segsOfDoc)
        {
            Segment seg = null;
            try
            {
                string[] fields = s.Split(new char[] { ' ', '\t' });
                if (fields.Length < 5) // Commentは任意
                {
                    Console.WriteLine("Ignoring an invalid extension SEGMENT line (Format Error): {0}...", s);
                    return null;
                }
                string tagname = fields[2].Trim('"');
                try
                {
                    int from = Int32.Parse(fields[3]);
                    int to = Int32.Parse(fields[4]);
                    seg = new Segment();
                    seg.ID = GetNextId("segment");
                    seg.Tag = m_Context.Proj.FindTag(Tag.SEGMENT, tagname);
                    if (seg.Tag == null)
                    {
                        var t = new Tag(Tag.SEGMENT, tagname) { Parent = m_Context.Proj.TagSetList[0], Version = m_Context.TSVersion };
                        m_Context.Proj.TagSetList[0].AddTag(t);
                        seg.Tag = t;
                        m_Context.Save(t);
                    }
                    seg.Doc = doc;
                    seg.Sentence = FindSentenceFromPosition(from, to);
                    seg.StartChar = from;
                    seg.EndChar = to;
                    seg.Proj = m_Context.Proj;
                    seg.User = m_Context.User;
                    seg.Version = seg.Tag.Version;
                    int p;
                    if ((p = s.IndexOf('"')) >= 0)
                    {
                        if (s.Length > p + 2)
                        {
                            string comment = s.Substring(p + 1, s.Length - p - 2);
                            seg.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
                        }
                    }
                    using (var cmd = m_Context.Session.Connection.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO segment VALUES("
                            + $"{seg.ID},{seg.Tag.ID},{seg.Version.ID},"
                            + $"{seg.Doc.ID},{seg.StartChar},{seg.EndChar},'',"
                            + $"{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},"
                            + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(seg.Comment)}',"
                            + $"{seg.Sentence.ID},{seg.Lex?.ID ?? 0})";
                        cmd.ExecuteNonQuery();
                    }
                    if (tagname == "Bunsetsu")
                    {
                        UpdateWordBunsetsuRelations(seg);
                    }
                    return seg;
                }
                catch (DbException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ignoring an invalid extension SEGMENT line (Parse Error:{0}): {1}...", ex.Message, s);
                    seg = null;
                    return null;
                }
            }
            finally
            {
                var hash = GetSegmentHash(seg);
                if (!segsOfDoc.TryGetValue(hash, out var list))
                {
                    list = new List<Segment>();
                    segsOfDoc.Add(hash, list);
                }
                segsOfDoc[hash].Add(seg);
            }
        }

        // Wordの持つBunsetsuへの関連をupdate
        private void UpdateWordBunsetsuRelations(Segment seg)
        {
            // segの範囲にあるWordのbunsetsu_segment_idをupdateする
            using (var cmd = m_Context.Session.Connection.CreateCommand())
            {
                cmd.CommandText = "UPDATE word "
                    + $"SET bunsetsu_segment_id={seg.ID} WHERE "
                    + $"sentence_id={seg.Sentence.ID} AND "
                    + $"start_char>={seg.StartChar} AND "
                    + $"end_char<={seg.EndChar} AND "
                    + $"project_id={seg.Proj.ID}";
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 次の形式の文内LINK_S定義からLinkを生成する.
        ///  #! LINK_S [linkname] [StartSegSeq] [EndSegSeq] "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="s"></param>
        private Link AddLink(Sentence sen, string s, Dictionary<string, List<Segment>> segsOfSentence)
        {
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension LINK_S line (Format Error): {0}...", s);
                return null;
            }
            string tagname = fields[2].Trim('"');
            try
            {
                var link = new Link();
                link.ID = GetNextId("link");
                link.Tag = m_Context.Proj.FindTag(Tag.LINK, tagname);
                if (link.Tag == null)
                {
                    var t = new Tag(Tag.LINK, tagname) { Parent = m_Context.Proj.TagSetList[0], Version = m_Context.TSVersion };
                    m_Context.Proj.TagSetList[0].AddTag(t);
                    link.Tag = t;
                    m_Context.Save(t);
                }
                var from = fields[3];
                if (!segsOfSentence.TryGetValue(from, out var list1))
                {
                    Console.WriteLine("Ignoring an invalid extension LINK_S line (Invalid from_seg): {0}...", s);
                    return null;
                }
                link.From = list1.Last();
                var to = fields[4];
                if (!segsOfSentence.TryGetValue(to, out var list2))
                {
                    Console.WriteLine("Ignoring an invalid extension LINK_S line (Invalid to_seg): {0}...", s);
                    return null;
                }
                link.To = list2.Last();
                link.Proj = m_Context.Proj;
                link.User = m_Context.User;
                link.Version = link.Tag.Version;
                link.FromSentence = sen;
                link.ToSentence = sen;
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        var comment = s.Substring(p + 1, s.Length - p - 2);
                        link.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
                    }
                }
                using (var cmd = m_Context.Session.Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO link VALUES("
                        + $"{link.ID},{link.Tag.ID},{link.Version.ID},"
                        + $"{link.From.ID},{ link.To.ID},"
                        + $"{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},"
                        + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(link.Comment)}',"
                        + $"{link.FromSentence.ID},{link.ToSentence.ID},{link.Lex?.ID??0})";
                    cmd.ExecuteNonQuery();
                }
                return link;
            }
            catch (DbException)
            {
                throw;
            }
            catch
            {
                Console.WriteLine("Ignoring an invalid extension LINK_S line (Parse Error): {0}...", s);
                return null;
            }
        }

        /// <summary>
        /// 次の形式の文内LINK定義からLinkを生成する.
        ///  #! LINK [linkname] [StartSegSeq] [EndSegSeq] "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="s"></param>
        private Link AddLink(Document doc, string s, Dictionary<string, List<Segment>> segsOfDocument)
        {
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension LINK_S line (Format Error): {0}...", s);
                return null;
            }
            string tagname = fields[2].Trim('"');
            try
            {
                var from = fields[3];
                var to = fields[4];
                var link = new Link();
                link.ID = GetNextId("link");
                link.Tag = m_Context.Proj.FindTag(Tag.LINK, tagname);
                if (link.Tag == null)
                {
                    var t = new Tag(Tag.LINK, tagname) { Parent = m_Context.Proj.TagSetList[0], Version = m_Context.TSVersion };
                    m_Context.Proj.TagSetList[0].AddTag(t);
                    link.Tag = t;
                    m_Context.Save(t);
                }
                if (!segsOfDocument.TryGetValue(from, out var list1))
                {
                    Console.WriteLine("Ignoring an invalid extension LINK line (Invalid from_seg): {0}...", s);
                    return null;
                }
                link.From = list1.LastOrDefault();
                if (!segsOfDocument.TryGetValue(to, out var list2))
                {
                    Console.WriteLine("Ignoring an invalid extension LINK line (Invalid to_seg): {0}...", s);
                    return null;
                }
                link.To = list2.LastOrDefault();
                link.Proj = m_Context.Proj;
                link.User = m_Context.User;
                link.Version = link.Tag.Version;
                link.FromSentence = link.From.Sentence;
                link.ToSentence = link.To.Sentence;
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        var comment = s.Substring(p + 1, s.Length - p - 2);
                        link.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
                    }
                }
                using (var cmd = m_Context.Session.Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO link VALUES("
                        + $"{link.ID},{link.Tag.ID},{link.Version.ID},"
                        + $"{link.From.ID},{ link.To.ID},"
                        + $"{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},"
                        + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(link.Comment)}',"
                        + $"{link.FromSentence.ID},{link.ToSentence.ID},{link.Lex?.ID ?? 0})";
                    cmd.ExecuteNonQuery();
                }
                return link;
            }
            catch (DbException)
            {
                throw;
            }
            catch
            {
                Console.WriteLine("Ignoring an invalid extension LINK line (Parse Error): {0}...", s);
                return null;
            }
        }

        /// <summary>
        /// 次の形式の文内GROUP定義からGroupを生成する.
        ///  #! GROUP_S [groupname] [SegID_in_Sentence1] [SegID_in_Sentence2] ... "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="fields"></param>
        private Group AddGroup(Sentence sen, string s, Dictionary<string, List<Segment>> segsOfSentence)
        {
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 3)
            {
                Console.WriteLine("Ignoring an invalid extension GROUP_S line (Format Error): {0}...", s);
            }
            string tagname = fields[2].Trim('"');
            try
            {
                var grp = new Group();
                grp.ID = GetNextId("group_element");
                grp.Tag = m_Context.Proj.FindTag(Tag.GROUP, tagname);
                if (grp.Tag == null)
                {
                    var t = new Tag(Tag.GROUP, tagname) { Parent = m_Context.Proj.TagSetList[0], Version = m_Context.TSVersion };
                    m_Context.Proj.TagSetList[0].AddTag(t);
                    grp.Tag = t;
                    m_Context.Save(t);
                }
                grp.Proj = m_Context.Proj;
                grp.User = m_Context.User;
                grp.Version = grp.Tag.Version;

                for (int i = 3; i < fields.Length; i++)
                {
                    if (fields[i].Length == 0) continue;
                    if (fields[i][0] == '"') // Beginning of Comment field
                    {
                        break;
                    }
                    var segid = fields[i];

                    if (!segsOfSentence.TryGetValue(segid, out var list) || list.Count == 0)
                    {
                        Console.WriteLine("Ignoring an invalid extension GROUP_S line (Invalid member): {0}...", segid);
                        continue;
                    };
                    grp.Tags.Add(list.Last());  // 複数の同一Segmnetが見つかるかもしれないが、直近のものを追加
                }
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        var comment = s.Substring(p + 1, s.Length - p - 2);
                        grp.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
                    }
                }
                using (var cmd = m_Context.Session.Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO group_element VALUES("
                        + $"{grp.ID},{grp.Tag.ID},{grp.Version.ID},"
                        + $"{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},"
                        + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(grp.Comment)}')";
                    cmd.ExecuteNonQuery();
                    foreach (var ann in grp.Tags)
                    {
                        if (!(ann is Segment seg)) continue;
                        cmd.CommandText = "INSERT INTO group_member VALUES("
                            + $"{grp.ID},'{Tag.SEGMENT}',{seg.ID},"
                            + $"{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},{m_Context.DBService.GetDefault()})";
                        cmd.ExecuteNonQuery();
                    }
                }
                return grp;
            }
            catch (DbException)
            {
                throw;
            }
            catch
            {
                Console.WriteLine("Ignoring an invalid extension GROUP_S line (Parse Error): {0}...", s);
                return null;
            }
        }

        /// <summary>
        /// 次の形式のDocument内GROUP定義からGroupを生成する.
        ///  #! GROUP [groupname] [SegID_in_Document1] [SegID_in_Document2] ... "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="fields"></param>
        private Group AddGroup(Document doc, string s, Dictionary<string, List<Segment>> segsOfDocument)
        {
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 3)
            {
                Console.WriteLine("Ignoring an invalid extension GROUP line (Format Error): {0}...", s);
            }
            string tagname = fields[2].Trim('"');
            try
            {
                var grp = new Group();
                grp.ID = GetNextId("group_element");
                grp.Tag = m_Context.Proj.FindTag(Tag.GROUP, tagname);
                if (grp.Tag == null)
                {
                    var t = new Tag(Tag.GROUP, tagname) { Parent = m_Context.Proj.TagSetList[0], Version = m_Context.TSVersion };
                    m_Context.Proj.TagSetList[0].AddTag(t);
                    grp.Tag = t;
                    m_Context.Save(t);
                }
                grp.Proj = m_Context.Proj;
                grp.User = m_Context.User;
                grp.Version = grp.Tag.Version;

                for (int i = 3; i < fields.Length; i++)
                {
                    if (fields[i].Length == 0) continue;
                    if (fields[i][0] == '"') // Beginning of Comment field
                    {
                        break;
                    }
                    var segid = fields[i];

                    if (!segsOfDocument.TryGetValue(segid, out var list) || list.Count == 0)
                    {
                        Console.WriteLine("Ignoring an invalid extension GROUP line (Invalid member seg#{0}): {0}...", i - 3, segid);
                        continue;
                    };
                    grp.Tags.Add(list.Last());  // 複数の同一Segmnetが見つかるかもしれないが、直近のものを追加
                }
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        var comment = s.Substring(p + 1, s.Length - p - 2);
                        grp.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
                    }
                }
                using (var cmd = m_Context.Session.Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO group_element VALUES("
                        + $"{grp.ID},{grp.Tag.ID},{grp.Version.ID},"
                        + $"{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},"
                        + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(grp.Comment)}')";
                    cmd.ExecuteNonQuery();
                    foreach (var ann in grp.Tags)
                    {
                        if (!(ann is Segment seg)) continue;
                        cmd.CommandText = "INSERT INTO group_member VALUES("
                            + $"{grp.ID},'{Tag.SEGMENT}',{seg.ID},"
                            + $"{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},{m_Context.DBService.GetDefault()})";
                        cmd.ExecuteNonQuery();
                    }
                }
                return grp;
            }
            catch (DbException)
            {
                throw;
            }
            catch
            {
                Console.WriteLine("Ignoring an invalid extension GROUP line (Parse Error): {0}...", s);
                return null;
            }
        }

        /// <summary>
        /// 次の形式の属性定義からSeg, Link, Groupに対する属性を生成する.
        ///  #! ATTR key value [comment]
        /// </summary>
        private void AddAttribute(Sentence sen, string s, object target)
        {
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 4)
            {
                Console.WriteLine("Ignoring an invalid extension ATTR line (Format Error): {0}...", s);
            }
            var key = fields[2].TrimStart('\"').TrimEnd('\"');
            var value = fields[3].TrimStart('\"').TrimEnd('\"');
            string comment = null;
            if (fields.Length > 4)
            {
                comment = fields[4];
            }
            if (target is Document doc)
            {
                // Documentの属性については、同一属性を重複読み込みしないようチェックを行う.
                if (doc.Attributes.FirstOrDefault(a => a.Key == key && a.Value == value) == null)
                {
                    using (var cmd = m_Context.Session.Connection.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO documenttag VALUES("
                            + $"{(int)GetNextId("documenttag")},"
                            + $"'{key}','{Util.EscapeQuote(value)}',"
                            + $"{doc.ID},{m_Context.DBService.GetDefault()})";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            else if (target is Segment)
            {
                var seg = target as Segment;
                using (var cmd = m_Context.Session.Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO segment_attribute VALUES("
                        + $"{(int)GetNextId("segment_attribute")},{seg.ID},"
                        + $"'{Util.EscapeQuote(key)}','{Util.EscapeQuote(value)}',"
                        + $"{seg.Version.ID},{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},"
                        + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(comment)}')";
                    cmd.ExecuteNonQuery();
                }
            }
            else if (target is Link)
            {
                var lnk = target as Link;
                using (var cmd = m_Context.Session.Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO link_attribute VALUES("
                        + $"{(int)GetNextId("link_attribute")},{lnk.ID},"
                        + $"'{Util.EscapeQuote(key)}','{Util.EscapeQuote(value)}',"
                        + $"{lnk.Version.ID},{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},"
                        + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(comment)}')";
                    cmd.ExecuteNonQuery();
                }
            }
            else if (target is Group)
            {
                var grp = target as Group;
                using (var cmd = m_Context.Session.Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO group_attribute VALUES("
                        + $"{(int)GetNextId("group_attribute")},{grp.ID},"
                        + $"'{Util.EscapeQuote(key)}','{Util.EscapeQuote(value)}',"
                        + $"{grp.Version.ID},{m_Context.Proj.ID},{m_Context.User?.ID ?? 0},"
                        + $"{m_Context.DBService.GetDefault()},'{Util.EscapeQuote(comment)}')";
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                throw new Exception(string.Format("Reading an attribute of some invalid object type={0}", target.GetType().Name));
            }
        }

        /// <summary>
        /// 次の形式の属性定義からNamespace定義を生成する.
        ///  #! NAMESPACE key value
        /// </summary>
        private void AddNamespaceDefinition(string s)
        {
            //string[] fields = s.Split(new char[] { ' ', '\t' });
            //if (fields.Length != 4)
            //{
            //    Console.WriteLine("Ignoring an invalid extension NAMESPACE line (Format Error): {0}...", s);
            //}
            //var key = fields[2].TrimStart('\"').TrimEnd('\"');
            //if (!key.StartsWith("xmlns:"))
            //{
            //    key = "xmlns:" + key;
            //}
            //var value = fields[3].TrimStart('\"').TrimEnd('\"');
            //string comment = null;
            //if (fields.Length > 4)
            //{
            //    comment = fields[4];
            //}
            //m_Corpus.AddNamespace(new Namespace(key, value));
        }

        private Sentence FindSentenceFromPosition(int from, int to)
        {
            //todo
            return null;
        }
    }
}
