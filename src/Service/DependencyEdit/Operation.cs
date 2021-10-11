using System;
using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using NHibernate;
using ChaKi.Service.Common;
using System.Text;
using ChaKi.Service.Database;
using ChaKi.Service.Readers;
using System.Linq;

namespace ChaKi.Service.DependencyEdit
{
    internal abstract class Operation : IOperation
    {
        public abstract void Execute(DepEditContext ctx);
        public abstract void UnExecute(DepEditContext ctx);

        public virtual string ToIronRubyStatement(DepEditContext ctx) { return string.Empty; }
        public virtual string ToIronPythonStatement(DepEditContext ctx) { return string.Empty; }


        // 以下、複数のOperationで共通のメソッド類

        public static string APPOSITION_TAG_NAME = "Apposition";
        public static string PARALLEL_TAG_NAME = "Parallel";
        public static string NEST_TAG_NAME = "Nest";

        public static string LastTagUsed = APPOSITION_TAG_NAME;

        protected void ExecuteSplitBunsetsuOperation(DepEditContext ctx, int docid, int bpos, int epos, int spos, List<ChangeLinkInfo> undoOperations)
        {
            // Split範囲にあるSegmentを取得
            Segment left = FindSegmentBetween(ctx, docid, bpos, epos);
            if (left == null)
            {
                throw new InvalidOperationException("Splitting: Missing segment");
            }

            // leftから出るLink
            Link oldlink = FindLinkFrom(ctx, left);

            if (undoOperations != null)
            {
                // リンクの付け替えに関するUndo Operationをセーブする.
                undoOperations.Add(new ChangeLinkInfo(docid, 
                    bpos, epos, "Bunsetsu",
                    -1, -1, "Bunsetsu",
                    oldlink.To.StartChar, oldlink.To.EndChar, "Bunsetsu"));
            }

            // leftセグメントの終端
            left.EndChar = spos;
            // 新しいセグメント(right)
            Segment right = new Segment();
            right.StartChar = spos;
            right.EndChar = epos;
            right.Tag = left.Tag;
            right.Doc = left.Doc;
            right.Proj = ctx.Proj;
            right.User = ctx.User;
            right.Version = left.Version;
            right.Sentence = left.Sentence;
            // rightをDBに保存
            ctx.Save(right);
            // leftから出ているLinkをrightから出るように変更
            oldlink.From = right;

            // left（既存）からright（新規）SegmentへLinkを張る
            Link newlink = new Link();
            newlink.From = left;
            newlink.To = right;
            newlink.Tag = oldlink.Tag;
            newlink.FromSentence = left.Sentence;
            newlink.ToSentence = right.Sentence;
            newlink.Proj = oldlink.Proj;
            newlink.User = ctx.User;
            newlink.Version = oldlink.Version;
            ctx.Save(newlink);
            ctx.Flush();
        }

        protected void ExecuteMergeBunsetsuOperation(DepEditContext ctx, int docid, int bpos, int epos, List<ChangeLinkInfo> undoOperations)
        {
            Segment bseg = FindSegmentStartingAt(ctx, docid, bpos);
            if (bseg == null)
            {
                throw new InvalidOperationException("Merging: Missing first segment");
            }
            Segment eseg = FindSegmentEndingAt(ctx, docid, epos);
            if (eseg == null)
            {
                throw new InvalidOperationException("Merging: Missing second segment");
            }
            if (bseg == eseg)
            {
                throw new InvalidOperationException("Merging: Cannot merge single segment");
            }

            // bsegから出るLink
            Link blink = FindLinkFrom(ctx, bseg);
            if (blink == null)
            {
                throw new InvalidOperationException("Merging: Missing link");
            }
            // esegから出るLink
            Link elink = FindLinkFrom(ctx, eseg);
            if (elink == null)
            {
                throw new InvalidOperationException("Merging: Missing link");
            }
            // esegに入るLink
            IList<Link> links = FindLinksTo(ctx, eseg);

            if (undoOperations != null)  // unoの場合、Linkの付け替えはこの後でundoOperationsの記録に従って行うので、特にここで変更する必要はない.
            {
                // bsegから出るリンクの係り先をelinkの係り先に変更.
                // UndoとしてMergeを行う場合はこの処理は必要ない.

                // リンクの付け替えに関するUndo Operationをセーブする.
                // bsegから出るLinkの係り先を戻す.(Undo時はSplit後に行うこと)
                undoOperations.Add(new ChangeLinkInfo(docid, 
                    bseg.StartChar, bseg.EndChar, "Bunsetsu",
                    - 1, -1, "Bunsetsu",
                    blink.To.StartChar, blink.To.EndChar, "Bunsetsu"));
                // esegから出ていたLinkの係り先を戻す.(Undo時はSplit後に行うこと)
                undoOperations.Add(new ChangeLinkInfo(docid, 
                    elink.From.StartChar, elink.From.EndChar, "Bunsetsu",
                    - 1, -1, "Bunsetsu",
                    elink.To.StartChar, elink.To.EndChar, "Bunsetsu"));

                if (elink.To == bseg)
                {
                    // bseg <-- esegとなっていた場合はbsegから出るリンクをそのまま残す.
                    if (blink.To == eseg)
                    {
                        blink.To = FindDummyBunsetsu(ctx, docid, blink.FromSentence.EndChar);
                    }
                }
                else
                {
                    blink.To = elink.To;
                }
            }

            // leftセグメントを右に拡張することでマージする
            bseg.EndChar = epos;

            // esegに入っていたリンクの係り先をすべてbsegに変更
            foreach (object o in links)
            {
                Link lk = o as Link;
                if (lk.From != bseg)
                {
                    if (undoOperations != null)
                    {
                        // リンクの付け替えに関するUndo Operationをセーブする.
                        // esegに入っていたLinkの係り先を戻す.(Undo時はSplit後に行うこと)
                        undoOperations.Add(new ChangeLinkInfo(docid, 
                            lk.From.StartChar, lk.From.EndChar, "Bunsetsu",
                            - 1, -1, "Bunsetsu",
                            lk.To.StartChar, lk.To.EndChar, "Bunsetsu"));
                    }
                    lk.To = bseg;
                }
            }
            // esegから出るリンクをDBから切り離す
            foreach (var linkattr in elink.Attributes)  // Linkの子オブジェクトを先にDeleteする.
            {
                ctx.Delete(linkattr);
                ctx.Flush();
            }
            elink.Attributes.Clear();
            ctx.Delete(elink);

            // 削除された文節をDBから切り離し、Transientなオブジェクトにする。
            ctx.Delete(eseg);
            ctx.Flush();
        }

        /// <summary>
        /// LinkのFrom, Toの付け替えを行う。（Undo操作用）
        /// linkFromSegStartの値により対象のLinkを特定し、
        /// そのLinkのFrom, ToをStartPosにより特定されるSegmentに変更する.
        /// newFromSegStart, newToSegStartは-1ならばそれぞれFrom, Toを変更しないことを示す.
        /// </summary>
        protected void ChangeLink(DepEditContext ctx, ChangeLinkInfo info)
        {
            var bseg = FindSegmentBetween(ctx, info.DocId, info.LinkFromSegStartPos, info.LinkFromSegEndPos, info.LinkFromSegTagName);
            if (bseg == null)
            {
                throw new InvalidOperationException("ChangeLink: Missing linkFromSegStart segment");
            }
            var link = FindLinkFrom(ctx, bseg);
            if (link == null)
            {
                throw new InvalidOperationException("ChangeLink: Missing link");
            }
            if (info.NewFromSegStartPos >= 0)
            {
                var newbseg = FindSegmentBetween(ctx, info.DocId, info.NewFromSegStartPos, info.NewFromSegEndPos, info.NewFromSegTagName);
                if (newbseg == null)
                {
                    throw new InvalidOperationException("ChangeLink: Missing newFromSegStart segment");
                }
                link.From = newbseg;
            }
            if (info.NewToSegStartPos >= 0)
            {
                var neweseg = FindSegmentBetween(ctx, info.DocId, info.NewToSegStartPos, info.NewToSegEndPos, info.NewToSegTagName);
                if (neweseg == null)
                {
                    throw new InvalidOperationException("ChangeLink: Missing newToSegStart segment");
                }
                link.To = neweseg;
            }
            ctx.Flush();
        }

        protected struct ChangeLinkInfo
        {
            public int DocId;
            // 付け替え対象のLinkを同定するための文字位置範囲
            public int LinkFromSegStartPos;
            public int LinkFromSegEndPos;
            public string LinkFromSegTagName;
            // Linkの始点Segmentを同定するための文字位置範囲とTagName
            public int NewFromSegStartPos;
            public int NewFromSegEndPos;
            public string NewFromSegTagName;
            // Linkの終点Segmentを同定するための文字位置範囲とTagName
            public int NewToSegStartPos;
            public int NewToSegEndPos;
            public string NewToSegTagName;
            // Link Tag
            public string OldLinkTagName;
            public string NewLinkTagName;

            public ChangeLinkInfo(int docid,
                int linkFromSegStartPos, int linkFromSegEndPos, string linkFromSegTagName,
                int newFromSegStartPos, int newFromSegEndPos, string newFromSegTagName,
                int newToSegStartPos, int newToSegEndPos, string newToSegTagName,
                string oldLinkTagName = null, string newLinkTagName = null
                )
            {
                this.DocId = docid;
                this.LinkFromSegStartPos = linkFromSegStartPos;
                this.LinkFromSegEndPos = linkFromSegEndPos;
                this.LinkFromSegTagName = linkFromSegTagName;
                this.NewFromSegStartPos = newFromSegStartPos;
                this.NewFromSegEndPos = newFromSegEndPos;
                this.NewFromSegTagName = newFromSegTagName;
                this.NewToSegStartPos = newToSegStartPos;
                this.NewToSegEndPos = newToSegEndPos;
                this.NewToSegTagName = newToSegTagName;
                this.OldLinkTagName = oldLinkTagName;
                this.NewLinkTagName = newLinkTagName;
            }
        }

        protected void ExecuteSplitWordOperation(DepEditContext ctx, int docid, int bpos, int epos, int spos, Lexeme lex1, Lexeme lex2, ref Lexeme orglex)
        {
            // Split範囲にあるWordを取得
            Word left = FindWordBetween(ctx, docid, bpos, epos);
            if (left == null)
            {
                throw new InvalidOperationException("Splitting: Missing word");
            }
            // 元のLexeme IDを保存
            orglex = left.Lex;

            // left Wordの終端
            left.EndChar = spos;
            // 元の表層形
            left.Lex = lex1;
            // 新しい Word(right)
            Word right = new Word();
            right.StartChar = spos;
            right.EndChar = epos;
            right.Bunsetsu = left.Bunsetsu;
            right.Lex = lex2;
            right.Sen = left.Sen;
            right.Project = left.Project;

            // 語末空白の処理
            // rightについては、leftを引き継ぐ。leftはチェックが必要。
            right.Extras = left.Extras;
            left.Extras = ConllReader.GetDiff(orglex.Surface.Substring(0, left.EndChar - left.StartChar), left.Lex.Surface);

            ctx.Save(right);

            // Sentence-Wordの関係を調整.
            // Word.Posを付け直す.
            int pos = 0;
            var words = left.Sen.GetWords(ctx.Proj.ID);
            foreach (Word w in words)
            {
                w.Pos = pos++;
                if (w == left)
                {
                    right.Pos = pos++;
                }
            }
            ctx.Flush();
            // Word.Posを元に、メモリ内のSentence.Wordsリストを同期する
            ctx.Session.Refresh(left.Sen);

            ctx.AddRelevantLexeme(orglex);
            ctx.AddRelevantLexeme(lex1);
            ctx.AddRelevantLexeme(lex2);
        }

        protected void ExecuteMergeWordOperation(DepEditContext ctx, int docid, int bpos, int epos, Lexeme lex, ref Lexeme lex1org, ref Lexeme lex2org)
        {
            Word bword = FindWordStartingAt(ctx, docid, bpos);
            if (bword == null)
            {
                throw new InvalidOperationException("Merging: Missing first word");
            }
            Word eword = FindWordEndingAt(ctx, docid, epos);
            if (eword == null)
            {
                throw new InvalidOperationException("Merging: Missing second word");
            }
            if (bword == eword)
            {
                throw new InvalidOperationException("Merging: Cannot merge single word");
            }

            lex1org = bword.Lex;
            lex2org = eword.Lex;

            // left Wordを右に拡張することでマージする
            bword.EndChar = epos;
            bword.Lex = lex;
            // 語末空白等の処理
            if (eword.Extras != null && eword.Extras.Length > 0)
            {
                bword.Extras = eword.Extras;
            }
            // 削除されたWordをDBから切り離し、Transientなオブジェクトにする。
            ctx.Delete(eword);
            ctx.Flush();

            // Sentence-Wordの関係を調整.
            // Word.Posを付け直す.
            int pos = 0;
            var words = bword.Sen.GetWords(ctx.Proj.ID);
            foreach (Word w in words)
            {
                if (w == null || w == eword)
                {
                    continue;
                }
                w.Pos = pos++;
            }
            ctx.Flush();
            // Word.Posを元に、メモリ内のSentence.Wordsリストを同期する
            ctx.Session.Refresh(bword.Sen);

            ctx.AddRelevantLexeme(lex);
            ctx.AddRelevantLexeme(lex1org);
            ctx.AddRelevantLexeme(lex2org);
        }

        protected void PrintAllLink(DepEditContext ctx, int docid, int bpos, int epos)
        {
            Console.WriteLine("---- PrintAllLink ----");
            IQuery query = ctx.Session.CreateQuery(string.Format("from Sentence s where s.ParentDoc.ID={0} and s.StartChar<={1} and s.EndChar>={2}",
                docid, bpos, epos));
            var sen = query.UniqueResult<Sentence>();
            query = ctx.Session.CreateQuery(         //TODO: Seg.TagはTagSetからBunsetsuのIDを得なければならない
                string.Format("from Segment seg where seg.Doc.ID={0} and seg.StartChar>={1} and seg.EndChar<={2} and seg.Tag.Name='Bunsetsu'", docid, sen.StartChar, sen.EndChar));
            var segs = query.List<Segment>();
            foreach (var seg in segs)
            {
                query = ctx.Session.CreateQuery(string.Format("select lk from Link lk, Segment seg where seg.ID={0} and lk.From=seg", seg.ID));
                var lst = query.List<Link>();
                foreach (var lnk in lst)
                {
                    Console.WriteLine("[{0},{1}]-[{2},{3}]", lnk.From.StartChar, lnk.From.EndChar, lnk.To.StartChar, lnk.To.EndChar);
                }
            }
            Console.WriteLine("----------------------");
        }

        protected static Segment FindSegmentBetween(DepEditContext ctx, int docid, int bpos, int epos, string tagname = "Bunsetsu")
        {
            IQuery query;
            if (tagname == null)
            {
                query = ctx.Session.CreateQuery(
                    string.Format("from Segment seg where seg.Doc.ID={0} and seg.StartChar={1} and seg.EndChar={2} and seg.Proj.ID={3}",
                    docid, bpos, epos, ctx.Proj.ID));
            }
            else
            {
                query = ctx.Session.CreateQuery(         //TODO: Seg.TagはTagSetからBunsetsuのIDを得なければならない
                    string.Format("from Segment seg where seg.Doc.ID={0} and seg.StartChar={1} and seg.EndChar={2} and seg.Tag.Name='{4}' and seg.Proj.ID={3}",
                    docid, bpos, epos, ctx.Proj.ID, tagname));
            }
            return query.UniqueResult<Segment>();
        }

        protected static Segment FindSegmentStartingAt(DepEditContext ctx, int docid, int pos, string tagname = "Bunsetsu")
        {
            // 文の開始終了位置に、長さ0のDummy Segmentが存在する。Segmentを同定するクエリでこれをヒットしないように注意。
            IQuery query;
            if (tagname == null)
            {
                query = ctx.Session.CreateQuery(
                    string.Format("from Segment seg where seg.Doc.ID={0} and seg.StartChar={1} and seg.EndChar>{1} and seg.Proj.ID={2}",
                    docid, pos, ctx.Proj.ID));
            }
            else
            {
                query = ctx.Session.CreateQuery(         //TODO: Seg.TagはTagSetからBunsetsuのIDを得なければならない
                    string.Format("from Segment seg where seg.Doc.ID={0} and seg.StartChar={1} and seg.EndChar>{1} and seg.Tag.Name='{3}' and seg.Proj.ID={2}",
                    docid, pos, ctx.Proj.ID, tagname));
            }
            return query.UniqueResult<Segment>();
        }

        protected static Segment FindSegmentEndingAt(DepEditContext ctx, int docid, int pos, string tagname = "Bunsetsu")
        {
            IQuery query;
            if (tagname == null)
            {
                query = ctx.Session.CreateQuery(
                    string.Format("from Segment seg where seg.Doc.ID={0} and seg.EndChar={1} and seg.StartChar<{1} and seg.Proj.ID={2}",
                    docid, pos, ctx.Proj.ID));
            }
            else
            {
                query = ctx.Session.CreateQuery(         //TODO: Seg.TagはTagSetからBunsetsuのIDを得なければならない
                    string.Format("from Segment seg where seg.Doc.ID={0} and seg.EndChar={1} and seg.StartChar<{1} and seg.Tag.Name='{3}' and seg.Proj.ID={2}",
                    docid, pos, ctx.Proj.ID, tagname));
            }
            return query.UniqueResult<Segment>();
        }

        public static Segment FindDummyBunsetsu(DepEditContext ctx, int docid, int pos, string tagname = "Bunsetsu")
        {
            IQuery query;
            if (tagname == null)
            {
                query = ctx.Session.CreateQuery(
                    string.Format("from Segment seg where seg.Doc.ID={0} and seg.StartChar={1} and seg.EndChar={1} and seg.Proj.ID={2}",
                    docid, pos, ctx.Proj.ID));
            }
            else
            {
                query = ctx.Session.CreateQuery(
                    string.Format("from Segment seg where seg.Doc.ID={0} and seg.StartChar={1} and seg.EndChar={1} and seg.Tag.Name='{3}' and seg.Proj.ID={2}",
                    docid, pos, ctx.Proj.ID, tagname));
            }
            return query.UniqueResult<Segment>();
        }


        /// <summary>
        /// 重複関連であるWord-Segmentを割り当て直す.
        /// </summary>
        protected void UpdateWordToSegmentRelations(DepEditContext ctx)
        {
            IQuery query = ctx.Session.CreateQuery(
                string.Format("from Segment seg where seg.Sentence.ID={0} and seg.Tag.Name='Bunsetsu' and seg.Proj.ID={1}", ctx.Sen.ID, ctx.Proj.ID));
            IList<Segment> segs = query.List<Segment>();
            query = ctx.Session.CreateQuery(
                string.Format("from Word w where w.Sen.ID={0} and w.Project.ID={1} order by w.ID ", ctx.Sen.ID, ctx.Proj.ID));
            IList<Word> words = query.List<Word>();

            foreach (Word w in words)
            {
                int startChar = w.StartChar;
                int endChar = w.EndChar - (w.Extras?.Length) ?? 0;
                w.Bunsetsu = null;
                foreach (Segment seg in segs)
                {
                    if (startChar >= seg.StartChar && endChar <= seg.EndChar)
                    {
                        w.Bunsetsu = seg;
                        break;
                    }
                }
                if (w.Bunsetsu == null)
                {
                    throw new Exception($"Segment not found for word[{w.StartChar}-{w.EndChar}]({w.Lex?.Surface})");
                }
            }
        }

        public static Link FindLinkFrom(DepEditContext ctx, Segment seg)
        {
            IQuery query = ctx.Session.CreateQuery(string.Format("select lk from Link lk, Segment seg where seg.ID={0} and lk.From=seg and lk.Proj.ID={1}",
                seg.ID, ctx.Proj.ID));
            var lst = query.List<Link>();
            if (lst.Count > 1)
            {
                Console.WriteLine("[{0},{1}]-[{2},{3}]", lst[0].From.StartChar, lst[0].From.EndChar, lst[0].To.StartChar, lst[0].To.EndChar);
                Console.WriteLine("[{0},{1}]-[{2},{3}]", lst[1].From.StartChar, lst[1].From.EndChar, lst[1].To.StartChar, lst[1].To.EndChar);
                return lst[0];
            }
            return query.UniqueResult<Link>();
        }

        public static IList<Link> FindLinksFrom(DepEditContext ctx, Segment seg)
        {
            IQuery query = ctx.Session.CreateQuery(string.Format("select lk from Link lk, Segment seg where seg.ID={0} and lk.From=seg and lk.Proj.ID={1}",
                seg.ID, ctx.Proj.ID));
            var lst = query.List<Link>();
            return query.List<Link>();
        }

        public static IList<Link> FindLinksTo(DepEditContext ctx, Segment seg)
        {
            IQuery query = ctx.Session.CreateQuery(string.Format("select lk from Link lk, Segment seg where seg.ID={0} and lk.To=seg and lk.Proj.ID={1}",
                seg.ID, ctx.Proj.ID));
            return query.List<Link>();
        }

        public static Link FindLink(DepEditContext ctx, Segment from, Segment to, string tagname)
        {
            IQuery query = ctx.Session.CreateQuery($"from Link lk where lk.Proj.ID={ctx.Proj.ID} and lk.From.ID={from.ID} and lk.To.ID={to.ID} and lk.Tag.Name='{tagname}'");
            var lst = query.List<Link>();
            if (lst.Count > 1)
            {
                Console.WriteLine("[{0},{1}]-[{2},{3}]", lst[0].From.StartChar, lst[0].From.EndChar, lst[0].To.StartChar, lst[0].To.EndChar);
                Console.WriteLine("[{0},{1}]-[{2},{3}]", lst[1].From.StartChar, lst[1].From.EndChar, lst[1].To.StartChar, lst[1].To.EndChar);
                return lst[0];
            }
            return query.UniqueResult<Link>();
        }



        public static Tag FindBunsetsuTag(DepEditContext ctx)
        {
            IQuery query = ctx.Session.CreateQuery("from Tag tag where tag.Type='Segment' and tag.Name='Bunsetsu'");
            return query.UniqueResult<Tag>();
        }

        public static Tag FindSegmentTag(DepEditContext ctx, string segTagName)
        {
            IQuery query = ctx.Session.CreateQuery(string.Format("from Tag tag where tag.Type='Segment' and tag.Name='{0}'", segTagName));
            return query.UniqueResult<Tag>();
        }

        public static Tag FindLinkTag(DepEditContext ctx, string linkTagName)
        {
            IQuery query = ctx.Session.CreateQuery(string.Format("from Tag tag where tag.Type='Link' and tag.Name='{0}'", linkTagName));
            return query.UniqueResult<Tag>();
        }

        public static Word FindWordBetween(DepEditContext ctx, int docid, int bpos, int epos)
        {
            IQuery query = ctx.Session.CreateQuery(
                string.Format("from Word w where w.Sen.ParentDoc.ID={0} and w.StartChar={1} and w.EndChar={2} and w.Project.ID={3}", docid, bpos, epos, ctx.Proj.ID));
            //IList<Word> result = query.List<Word>();
            return query.UniqueResult<Word>();
        }

        public static Word FindWordStartingAt(DepEditContext ctx, int docid, int pos)
        {
            IQuery query = ctx.Session.CreateQuery(
                string.Format("from Word w where w.Sen.ParentDoc.ID={0} and w.StartChar={1} and w.EndChar>{1} and w.Project.ID={2}", docid, pos, ctx.Proj.ID));
            return query.UniqueResult<Word>();
        }

        public static Word FindWordEndingAt(DepEditContext ctx, int docid, int pos)
        {
            IQuery query = ctx.Session.CreateQuery(
                string.Format("from Word w where w.Sen.ParentDoc.ID={0} and w.EndChar={1} and w.StartChar<{1} and w.Project.ID={2}", docid, pos, ctx.Proj.ID));
            return query.UniqueResult<Word>();
        }

        public static Lexeme FindLexeme(DepEditContext ctx, int id)
        {
            IQuery query = ctx.Session.CreateQuery(string.Format("from Lexeme l where l.ID={0}", id));
            return query.UniqueResult<Lexeme>();
        }

        public static Group FindGroupCovering(DepEditContext ctx, HashSet<Segment> segs, string tag)
        {
            if (segs.Count == 0)
            {
                throw new Exception("FindGroupCovering - Needs at least 1 segment.");
            }
            var segids = from s in segs select s.ID;
            var query = ctx.Session.CreateQuery($"from Group g where g.Proj.ID={ctx.Proj.ID} and g.Tag.Name='{tag}'");
            //HQL v3.0では以下が使えない： and {segs.First()} member of g.Tags");
            var lst = query.List<Group>();
            foreach (var g in lst)
            {
                // Group Memberが存在しないことがあるのでtryを追加
                try
                {
                    var gsegs = (from t in g.Tags where t is Segment select ((Segment)t).ID);
                    if (Util.IsSubset(gsegs, segids))
                    {
                        // 最初に見つかったものを返す
                        return g;
                    }
                }
                catch
                {
                    // just continue
                }
            }
            return null;
        }

        /// <summary>
        /// propsで与えられた内容と完全に（空の("")Propertyを含め）一致するLexemeが既にLexiconに存在すれば、一致するLexemeを返す.
        /// 該当するLexemeが複数ある場合は最初に見つかったものを返す.
        /// なければnullを返す.
        /// </summary>
        public static Lexeme FindLexeme(DepEditContext ctx, string[] props)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("from Lexeme l where");
            sb.AppendFormat(" l.Surface='{0}'", props[(int)LP.Surface]);
            sb.AppendFormat(" and l.Reading='{0}'", props[(int)LP.Reading]);
            sb.AppendFormat(" and l.LemmaForm='{0}'", props[(int)LP.LemmaForm]);
            sb.AppendFormat(" and l.Pronunciation='{0}'", props[(int)LP.Pronunciation]);
            sb.AppendFormat(" and l.BaseLexeme.Surface='{0}'", props[(int)LP.BaseLexeme]);
            sb.AppendFormat(" and l.Lemma='{0}'", props[(int)LP.Lemma]);
            sb.AppendFormat(" and l.PartOfSpeech.Name='{0}'", props[(int)LP.PartOfSpeech]);
            sb.AppendFormat(" and l.CType.Name='{0}'", props[(int)LP.CType]);
            sb.AppendFormat(" and l.CForm.Name='{0}'", props[(int)LP.CForm]);

            IQuery q = ctx.Session.CreateQuery(sb.ToString());
            IList<Lexeme> result = q.List<Lexeme>();
            if (result.Count > 0)
            {
                return result[0];
            }
            return null;
        }

        /// <summary>
        /// propsで与えられた内容を元に既存のLexemeを更新または新たに生成してDBに登録する.
        /// Base, POS, CForm, CTypeも必要に応じて登録する.
        /// </summary>
        /// <param name="lex">既存ならID >= 0, 新語ならID &lt; 0</param>
        /// <param name="props">LPの順に並べられたProperty文字列の配列</param>
        public static void CreateOrUpdateLexeme(DepEditContext ctx, ref Lexeme lex, string[] props, string customprop)
        {
            IQuery q;

            if (lex == null)
            {
                lex = new Lexeme();
            }

            // POS
            PartOfSpeech pos = null;
            q = ctx.Session.CreateQuery(string.Format("from PartOfSpeech p where p.Name='{0}' order by ID asc", props[(int)LP.PartOfSpeech]));
            var poses = q.List<PartOfSpeech>();
            if (poses.Count == 0)
            {
                pos = new PartOfSpeech(props[(int)LP.PartOfSpeech]);
                ctx.Save(pos);
            }
            else
            {
                pos = poses[0];
            }
            lex.PartOfSpeech = pos;
            // CType
            CType ctype = null;
            q = ctx.Session.CreateQuery(string.Format("from CType t where t.Name='{0}' order by ID asc", props[(int)LP.CType]));
            var ctypes = q.List<CType>();
            if (ctypes.Count == 0)
            {
                ctype = new CType(props[(int)LP.CType]);
                ctx.Save(ctype);
            }
            else
            {
                ctype = ctypes[0];
            }
            lex.CType = ctype;
            // CForm
            CForm cform = null;
            q = ctx.Session.CreateQuery(string.Format("from CForm f where f.Name='{0}' order by ID asc", props[(int)LP.CForm]));
            var cforms = q.List<CForm>();
            if (cforms.Count == 0)
            {
                cform = new CForm(props[(int)LP.CForm]);
                ctx.Save(cform);
            }
            else
            {
                cform = cforms[0];
            }
            lex.CForm = cform;

            // その他のProperty
            lex.Surface = props[(int)LP.Surface];
            lex.Reading = props[(int)LP.Reading];
            lex.Pronunciation = props[(int)LP.Pronunciation];
            lex.Lemma = props[(int)LP.Lemma];
            lex.LemmaForm = props[(int)LP.LemmaForm];
            lex.CustomProperty = customprop;

            // Base
            string basestr = props[(int)LP.BaseLexeme];
            Lexeme baselex = lex; // default
            if (basestr.Length > 0 && basestr != props[(int)LP.Surface])
            {
                // 基本形に一致するLexemeが既に存在するか?
                props[(int)LP.Surface] = basestr;
                baselex = FindLexeme(ctx, props);
                if (baselex == null)
                {
                    baselex = new Lexeme(lex);
                    baselex.Surface = basestr;
                    baselex.BaseLexeme = baselex;
                    ctx.Save(baselex);
                    ctx.AddRelevantLexeme(baselex);
                }
            }
            lex.BaseLexeme = baselex;


            // LexemeをSave
            if (lex.ID < 0)
            {
                ctx.Save(lex);
                lex.Dictionary = null;
            }
            else
            {
                ctx.SaveOrUpdate(lex);
            }
            ctx.AddRelevantLexeme(lex);
        }
    }

    /// <summary>
    /// OperationMergeBunsetsu, OperationSplitBunsetsuにおいてのLinkの扱い
    /// ExecuteMergeBunsetsuOperation()内で決定される.
    /// </summary>
    enum LinkHandling
    {
        /// <summary>
        /// デフォルト. 
        /// Mergeで右文節から出るリンクを残す.
        /// Splitでは左から右にかかるリンクを新規に作る.
        /// </summary>
        HeadIsRight,

        /// <summary>
        /// Mergeで左文節から出るリンクを残す.
        /// Splitでは右から左にかかるリンクを新規に作る.
        /// </summary>
        HeadIsLeft,
    }
}
