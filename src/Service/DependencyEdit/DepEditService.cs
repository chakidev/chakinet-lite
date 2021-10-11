using ChaKi.Common.SequenceMatcher;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Common;
using ChaKi.Service.Database;
using ChaKi.Service.Lexicons;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Service.DependencyEdit
{
    public class DepEditService : IDisposable, IDepEditService, ILexiconService
    {
        protected Corpus m_Corpus;
        internal History m_History;
        private DepEditContext m_Context;
        public OpContext OpContext
        {
            get { return m_Context; }
        }

        private UnlockRequestCallback m_LastUnlockCallback;

        private List<DictionaryAccessor> m_Dictionaries;

        public static string LastScriptingStatements = string.Empty;

        public DepEditService()
        {
            m_Corpus = null;
            m_History = new History();
            m_Context = null;
            m_Dictionaries = new List<DictionaryAccessor>();
            m_LastUnlockCallback = null;
        }

        // 中心語の開始文字位置（Document内絶対位置）
        public int CenterWordStartAt
        {
            get { return m_Context.CharOffset; }
            set { m_Context.CharOffset = value; }
        }

        /// <summary>
        /// 新しい文をDependencyTree Editの対象としてオープンする。
        /// </summary>
        /// <param name="cps">編集対象のコーパス</param>
        /// <param name="sid">編集対象の文番号</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">他の編集が完了していないうちに、二重に編集を行おうとした。</exception>
        public Sentence Open(Corpus cps, int sid, UnlockRequestCallback unlockCallback)
        {
            m_LastUnlockCallback = unlockCallback;
            if (m_Context != null && m_Context.IsTransactionActive())
            {
                throw new InvalidOperationException("Active transaction exists. Close or Commit it first.");
            }

            if (m_Corpus == null || cps != m_Corpus)
            {
                m_Corpus = cps;
            }
            m_Context = StartTransaction(m_Corpus, sid, unlockCallback);
            if (m_Context == null)
            {
                return null;
            }
            m_History.Reset();

            Util.Reset();  // 検索高速化でキャッシュされているSegment Tag Listを取得しなおす.

            EnsureDefaultLexemeExisting();

            return m_Context.Sen;
        }

        /// <summary>
        /// CorpusのLexemeでPos, CForm, CTypeのデフォルト値("Unassigned"用のobject)が存在することを確認し、
        /// なければ追加する.
        /// </summary>
        private void EnsureDefaultLexemeExisting()
        {
            const string UNKNOWN = "Unassigned";
            if (m_Context == null)
            {
                return;
            }
            IQuery query = m_Context.Session.CreateQuery(string.Format("from PartOfSpeech pos where pos.Name1='{0}'", UNKNOWN));
            IList<PartOfSpeech> pos_result = query.List<PartOfSpeech>();
            PartOfSpeech DefaultPos;
            if (pos_result.Count == 0)
            {
                DefaultPos = new PartOfSpeech(UNKNOWN);
                m_Context.Save(DefaultPos);
            }
            else
            {
                DefaultPos = pos_result[0];
            }

            query = m_Context.Session.CreateQuery("from CForm cf where cf.Name=''");
            IList<CForm> cf_result = query.List<CForm>();
            CForm DefaultCForm;
            if (cf_result.Count == 0)
            {
                DefaultCForm = new CForm(string.Empty);
                m_Context.Save(DefaultCForm);
            }
            else
            {
                DefaultCForm = cf_result[0];
            }

            query = m_Context.Session.CreateQuery("from CType ct where ct.Name1='' and ct.Name2=''");
            IList<CType> ct_result = query.List<CType>();
            CType DefaultCType;
            if (ct_result.Count == 0)
            {
                DefaultCType = new CType(string.Empty);
                m_Context.Save(DefaultCType);
            }
            else
            {
                DefaultCType = ct_result[0];
            }

            PartOfSpeech.Default = DefaultPos;
            CForm.Default = DefaultCForm;
            CType.Default = DefaultCType;
            if (!m_Corpus.Lex.PartsOfSpeech.Contains(DefaultPos))
            {
                m_Corpus.Lex.PartsOfSpeech.Add(DefaultPos);
            }
            if (!m_Corpus.Lex.CForms.Contains(DefaultCForm))
            {
                m_Corpus.Lex.CForms.Add(DefaultCForm);
            }
            if (!m_Corpus.Lex.CTypes.Contains(DefaultCType))
            {
                m_Corpus.Lex.CTypes.Add(DefaultCType);
            }
        }

        /// <summary>
        /// Close直後に再度同じ文の編集を開始する。
        /// </summary>
        /// <param name="cps"></param>
        /// <param name="sid"></param>
        /// <returns></returns>
        public Sentence ReOpen()
        {
            if (m_Context != null && m_Context.IsTransactionActive())
            {
                throw new InvalidOperationException("Active transaction exists. Close or Commit it first.");
            }

            if (m_Corpus == null)
            {
                throw new InvalidOperationException("Unknown corpus.");
            }
            if (m_Context.Sen == null)
            {
                throw new InvalidOperationException("Unknown sentence.");
            }
            if (m_Context.Proj == null)
            {
                throw new InvalidOperationException("Unknown project.");
            }
            int sid = m_Context.Sen.ID;
            int projid = m_Context.Proj.ID;

            m_Context = StartTransaction(m_Corpus, sid, m_LastUnlockCallback);
            m_History.Reset();

            EnsureDefaultLexemeExisting();

            SetupProject(projid);

            return m_Context.Sen;
        }

        /// <summary>
        /// 現在のTransactionを捨てて、新しいTransactionを開始する
        /// DB Exceptionが起きた場合に呼び出す
        /// </summary>
        public void ResetTransaction()
        {
            Close();
            ReOpen();
        }

        /// <summary>
        /// DBに存在するProjectをすべて得る.
        /// </summary>
        /// <returns></returns>
        public IList<Project> RetrieveProjects()
        {
            IQuery q = m_Context.Session.CreateQuery("from Project");
            return q.List<Project>();
        }

        /// <summary>
        /// Projectなど、この編集作業に関わるEntityを用意する
        /// </summary>
        public void SetupProject(int projid)
        {
            try
            {
                IQuery q = m_Context.Session.CreateQuery(string.Format("from Project p where p.ID={0}", projid));
                m_Context.Proj = q.UniqueResult<Project>();
                TagSet tset = m_Context.Proj.TagSetList[0];
                q = m_Context.Session.CreateQuery("from TagSetVersion v where v.ID=0");
                TagSetVersion tver = q.UniqueResult<TagSetVersion>();

                new List<string>() { Operation.APPOSITION_TAG_NAME, Operation.PARALLEL_TAG_NAME }
                    .ForEach(new Action<string>(delegate (string tagName)
                {
                    if (tset.FindTag(Tag.SEGMENT, tagName) == null)
                    {
                        Tag t = new Tag(Tag.SEGMENT, tagName) { Parent = tset, Version = tver };
                        m_Context.Save(t);
                        tset.AddTag(t);
                    }
                    if (tset.FindTag(Tag.GROUP, tagName) == null)
                    {
                        Tag t = new Tag(Tag.GROUP, tagName) { Parent = tset, Version = tver };
                        m_Context.Save(t);
                        tset.AddTag(t);
                    }
                }));
                m_Context.TagSet = tset;
                m_Context.TSVersion = tver;
                m_Context.CharOffset = this.CenterWordStartAt;
                m_Context.WordOffset = GetWordOffset(m_Context.Sen.ParentDoc.ID, m_Context.CharOffset);

                // ユーザー認証（ProjectにCurrentUserが属しているかの確認）
                foreach (User u in m_Context.Proj.Users)
                {
                    if (u.Name == User.Current.Name && u.Password == User.Current.Password)
                    {
                        m_Context.User = u;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to Setup Edit Session", ex);
            }
        }

        /// <summary>
        /// m_Sentenceに関連するSegment, Linkのリストをすべて取得する.
        /// </summary>
        /// <param name="segs"></param>
        /// <param name="links"></param>
        public void GetBunsetsuTags(out List<Segment> segs, out List<Link> links)
        {
            if (m_Context == null)
            {
                throw new InvalidOperationException("this.Context is null.");
            }
            if (m_Context.Session == null)
            {
                throw new InvalidOperationException("Session is null.");
            }
            if (m_Context.Sen == null)
            {
                throw new InvalidOperationException("Unknown sentence.");
            }

            //TODO: seg.Tag.IDの比較対象はTagSetからBunsetsu Segment TagのIDを引っ張ってくる必要がある。
            var segquery = m_Context.Session.CreateQuery(
                $"from Segment seg where seg.Doc.ID = {m_Context.Sen.ParentDoc.ID} and seg.Sentence.ID = {m_Context.Sen.ID} and seg.Tag.Name = 'Bunsetsu' and seg.Proj.ID={m_Context.Proj.ID} order by seg.StartChar");
            segs = segquery.List<Segment>().ToList();
            var sids = (from s in segs select s.ID).ToList();

            var q = m_Context.Session.CreateQuery(
                $"from Link lnk where lnk.From.ID in {Util.BuildIDList(sids)} or lnk.To.ID in {Util.BuildIDList(sids)}");
            links = q.List<Link>().ToList();
        }

        /// <summary>
        /// m_Sentenceに関連する、文節以外のSegmentのリストをすべて取得する.
        /// </summary>
        /// <returns></returns>
        public IList<Segment> GetSegmentTags()
        {
            if (m_Context.Sen == null)
            {
                throw new InvalidOperationException("Unknown sentence.");
            }

            IQuery segquery = m_Context.Session.CreateQuery(
                string.Format("from Segment seg where seg.Doc.ID = {0} and seg.Sentence.ID = {1}" +
                    " and seg.Tag.Name != 'Bunsetsu' and seg.Proj.ID={2}" +
                    " order by seg.StartChar",
                    m_Context.Sen.ParentDoc.ID, m_Context.Sen.ID, m_Context.Proj.ID));
            return segquery.List<Segment>();
        }

        /// <summary>
        /// m_Sentenceに関連する、文節に関わらないLinkのリストをすべて取得する.
        /// </summary>
        /// <returns></returns>
        public IList<Link> GetLinkTags()
        {
            if (m_Context.Sen == null)
            {
                throw new InvalidOperationException("Unknown sentence.");
            }
            var links = new List<Link>();

            // まず、この文に関係する文節以外のSegmentを検索
            var segquery = m_Context.Session.CreateQuery(
                string.Format("from Segment seg where seg.Doc.ID = {0} and seg.Sentence.ID = {1} and seg.Tag.Name != 'Bunsetsu' and seg.Proj.ID={2} order by seg.StartChar",
                    m_Context.Sen.ParentDoc.ID, m_Context.Sen.ID, m_Context.Proj.ID));
            var sresults = segquery.List<Segment>();

            // 検索されたSegmentに出入りするLinkをすべて検索
            // 当初ICriteria.List()で一括取得していたが、別のバグにより不完全なLink（ToSegmentのidが見つからないようなもの）が
            // DB内にできることがあり、その場合にList()がExceptionを投げ、不完全なLink以外の正常なLinkも含め
            // すべてLoadに失敗してしまっていた。
            // これに対し、DB内に異常なLinkが残っていることを前提として、最低限正常なLinkだけはLoadできるようにした.
            if (sresults.Count > 0)
            {
                var sids = Util.BuildSegmentIDList(sresults);
                var lquery = m_Context.Session.CreateSQLQuery(
                    $"SELECT id FROM link WHERE from_segment_id in {sids} or to_segment_id in {sids}");
                var linkids = lquery.List();
                foreach (long lid in linkids)
                {
                    try
                    {
                        var link = m_Context.Session.Load<Link>(lid);
                        links.Add(link);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Giving up loading an incomplete Link entity: {lid}");
                    }
                }
            }
            return links;
        }

        public bool HasInOutLink(Segment seg)
        {
            // Segmentに出入りするLinkをすべて検索
            var linkMatching = m_Context.Session.CreateCriteria(typeof(Link))
                .Add(Restrictions.Or(
                    Restrictions.Eq("From", seg),
                    Restrictions.Eq("To", seg)));
            var lresults = linkMatching.List<Link>();
            return (lresults.Count > 0);
        }

        /// <summary>
        /// m_Sentenceに関連する、"Nest"タグを持つSegmentのリストを取得する.
        /// </summary>
        /// <returns></returns>
        public IList<Segment> GetNestTags()
        {
            if (m_Context.Sen == null)
            {
                throw new InvalidOperationException("Unknown sentence.");
            }
            IQuery segquery = m_Context.Session.CreateQuery(
                string.Format("from Segment seg where seg.Doc.ID = {0} and seg.Sentence.ID = {1}" +
                    " and seg.Tag.Name = 'Nest'" +
                    " and seg.Proj.ID={2}" +
                    " order by seg.StartChar",
                    m_Context.Sen.ParentDoc.ID, m_Context.Sen.ID, m_Context.Proj.ID));
            return segquery.List<Segment>();
        }

        public IList<Group> GetGroupTags()
        {
            if (m_Context.Sen == null)
            {
                throw new InvalidOperationException("Unknown sentence.");
            }
            return Util.RetrieveWordGroups(m_Context.Session, m_Context.Sen, m_Context.Proj.ID);
        }

        public TagSet GetTagSet()
        {
            if (m_Context == null) return null;
            return m_Context.TagSet;
        }

        private DepEditContext StartTransaction(Corpus cps, int sid, UnlockRequestCallback unlockCallback)
        {
            DepEditContext ctx = DepEditContext.Create(m_Corpus.DBParam, unlockCallback);

            // sidからSentenceオブジェクトを検索
            ICriteria senMatching = ctx.Session.CreateCriteria(typeof(Sentence))
                .Add(Restrictions.Eq("ID", sid));
            IList<Sentence> results = senMatching.List<Sentence>();
            if (results.Count != 1)
            {
                ctx.Dispose();
                Dispose();
                return null;
            }
            ctx.Sen = results[0];

            IQuery q = ctx.Session.CreateQuery("select max(l.ID) from Lexeme l");
            ctx.MaxLexIDAtBeginning = q.UniqueResult<int>();

            return ctx;
        }

        /// <summary>
        /// 現在の状態をDBにコミットする。
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Commit()
        {
            if (!m_Context.IsTransactionActive())
            {
                throw new InvalidOperationException("No active transaction exists.");
            }
            // Lexemeに対する操作をクリーンアップする
            CleanupLexeme();

            // コミットする
            m_Context.Trans.Commit();

            // 操作記録をScriptの形で得てstaticメンバにセットする.
            try
            {
                LastScriptingStatements = GetScriptingStatements();
            }
            catch (Exception ex)
            {
                LastScriptingStatements = "Error in assembling script statements:" + ex.ToString();
            }

            Dispose();
        }

        /// <summary>
        /// 現在の状態を捨てて、編集状態から抜ける。
        /// </summary>
        public void Close()
        {
            if (m_Context != null && m_Context.IsTransactionActive())
            {
                m_Context.Trans.Rollback();
            }
            Dispose();
        }

        /// <summary>
        /// 現在の状態を捨てて、編集状態から抜ける。
        /// </summary>
        public void Dispose()
        {
            if (m_Context == null)
            {
                return;
            }
            try
            {
                if (m_Context.Trans != null)
                {
                    m_Context.Trans.Dispose();
                }
                if (m_Context.Session != null)
                {
                    m_Context.Session.Dispose();
                }
                foreach (var ctx in m_Dictionaries)
                {
                    ctx.Dispose();
                }
            }
            finally
            {
                m_Context.Trans = null;
                m_Context.Session = null;
                m_Dictionaries.Clear();
            }
        }

        /// <summary>
        /// 指定した文節を指定した語位置で分離する。
        /// </summary>
        /// <param name="bpos">文節の開始文字位置</param>
        /// <param name="epos">文節の終了文字位置</param>
        /// <param name="spos">分割文字位置</param>
        public void SplitBunsetsu(int docid, int bpos, int epos, int spos)
        {
            Operation op = new OperationSplitBunsetsu(docid, bpos, epos, spos);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        /// <summary>
        /// 指定した文節を次の文節と併合する。
        /// </summary>
        /// <param name="bpos">文節の開始文字位置</param>
        /// <param name="epos">文節の終了文字位置</param>
        /// <param name="spos">分割文字位置</param>
        public void MergeBunsetsu(int docid, int bpos, int epos, int spos)
        {
            Operation op = new OperationMergeBunsetsu(docid, bpos, epos, spos);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void ChangeLinkEnd(Link link, Segment oldseg, Segment newseg)
        {
            Operation op = new OperationMoveArrow(link, oldseg, newseg);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void ChangeLinkTag(Link link, string oldtag, string newtag)
        {
            Operation op = new OperationChangeLinkTag(link, oldtag, newtag);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void CreateWordGroup(CharRange range)
        {
            Operation op = new OperationCreateWordGroup(range, Operation.LastTagUsed);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void CreateWordGroup(CharRange range, string newGroup)
        {
            Operation op = new OperationCreateWordGroup(range, newGroup);
            op.Execute(m_Context);
            m_History.Record(op);
            Operation.LastTagUsed = newGroup;
        }

        public void AddItemToWordGroup(Group g, CharRange range)
        {
            Operation op = new OperationAddItemToWordGroup(g, range);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void RemoveItemFromWordGroup(Group grp, Segment seg)
        {
            Operation op = new OperationRemoveItemFromWordGroup(grp, seg);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void RemoveItemFromWordGroupWithInOutLinks(Group grp, Segment seg)
        {
            var op = new OperationRemoveItemFromWordGroupWithInOutLinks(grp, seg);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void ChangeWordGroupTag(Group grp, string oldTag, string newTag)
        {
            Operation op = new OperationChangeWordGroupTag(grp, oldTag, newTag);
            op.Execute(m_Context);
            m_History.Record(op);
            Operation.LastTagUsed = newTag;
        }

        public void ChangeSegmentTag(Segment seg, string oldTag, string newTag)
        {
            Operation op = new OperationChangeSegmentTag(seg, oldTag, newTag);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void ChangeLexeme(int docid, int wpos, Lexeme newlex)
        {
            Operation op = new OperationChangeLexeme(docid, wpos, newlex);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        // 既知のLexemeに置き換える
        public void ChangeLexeme(int docid, int cpos, int lexid)
        {
            Word w = Operation.FindWordStartingAt(m_Context, docid, cpos);
            int wpos = m_Context.Sen.GetWords(m_Context.Proj.ID).IndexOf(w);
            ChangeLexeme(docid, wpos, FindLexeme(lexid));
        }

        public void CreateSegment(int range_start, int range_end, string tag)
        {
            CreateSegment(new CharRange(range_start, range_end), tag);
        }

        public void CreateSegment(CharRange range, string tag)
        {
            Operation op = new OperationCreateSegment(range, tag);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void DeleteSegment(Segment seg)
        {
            Operation op = new OperationDeleteSegment(seg);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void DeleteSegmentWithInOutLinks(Segment seg)
        {
            Operation op = new OperationDeleteSegmentWithInOutLinks(seg);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void ChangeComment(Annotation a, string c)
        {
            Operation op = new OperationChangeComment(a, c);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public IOperation CreateMWEAnnotation(MatchingResult match, bool recordHistory = true)
        {
            var op = new OperationCreateMWEAnnotation(match);
            try
            {
                op.Execute(m_Context);
            }
            catch (DuplicatedAnnotationException)
            {
                // 同じMWE Segmentを付けようとしたときはDBを変更せずnullを返す.
                return null;
            }
            if (recordHistory)
            {
                m_History.Record(op);
            }
            return op;
        }

        public void PushHistories(IEnumerable<IOperation> ops)
        {
            foreach (var op in ops)
            {
                m_History.Record(op);
            }
        }

        /// <summary>
        /// DBのTransaction Savepointを作る
        /// </summary>
        /// <param name="v"></param>
        public void CreateSavePoint(string spname)
        {
            m_Context.CreateSavePoint(spname);
        }

        /// <summary>
        /// SavepointまでRollbackする
        /// </summary>
        /// <param name="v"></param>
        public void Unexecute(List<IOperation> ops)
        {
            for (var i = ops.Count - 1; i >= 0; i--)
            {
                (ops[i] as Operation).UnExecute(m_Context);
            }
        }

        /// <summary>
        /// SavepointをReleaseする
        /// </summary>
        /// <param name="v"></param>
        public void ReleaseSavePoint(string spname)
        {
            try
            {
                m_Context.ReleaseSavePoint(spname);
            }
            catch (Exception ex)
            {
            }
        }

        public void CreateLink(Segment fromseg, Segment toseg, string tagname)
        {
            var op = new OperationCreateLink(fromseg, toseg, tagname);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void DeleteLink(Link link)
        {
            var op = new OperationDeleteLink(link);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void SplitWord(int docid, int bpos, int epos, int spos, string lex1surface, string lex2surface)
        {
            Lexeme lex1 = Lexeme.CreateDefaultUnknownLexeme(lex1surface);
            Lexeme lex2 = Lexeme.CreateDefaultUnknownLexeme(lex2surface);
            m_Context.Save(lex1);
            m_Context.Save(lex2);
            Operation op = new OperationSplitWord(docid, bpos, epos, spos, lex1, lex2);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void MergeWord(int docid, int bpos, int epos, int spos, string lexsurface)
        {
            Lexeme lex = Lexeme.CreateDefaultUnknownLexeme(lexsurface);
            m_Context.Save(lex);

            Operation op = new OperationMergeWord(docid, bpos, epos, spos, lex);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public void UpdateAllBunsetsu(Sentence sen, string input)
        {
            // Cabocha出力を解析する
            var op = new OperationUpdateAllBunsetsu(sen, input);
            op.Execute(m_Context);
            m_History.Record(op);
        }

        public bool Undo()
        {
            var op = m_History.Back() as Operation;
            if (op != null)
            {
                op.UnExecute(m_Context);
                return true;
            }
            return false;
        }

        public bool Redo()
        {
            var op = m_History.Forward() as Operation;
            if (op != null)
            {
                op.Execute(m_Context);
                return true;
            }
            return false;
        }

        public bool CanUndo()
        {
            return m_History.CanUndo();
        }

        public bool CanRedo()
        {
            return m_History.CanRedo();
        }

        public bool CanSave()
        {
            return m_History.CanSave();
        }

        public void CannotSave()
        {
            m_History.Reset();
        }

        public Corpus GetCorpus()
        {
            return m_Corpus;
        }

        public void WriteToDotFile(TextWriter wr)
        {
            if (m_Context.Sen == null)
            {
                throw new InvalidOperationException("Unknown sentence.");
            }
            List<Segment> segs;
            List<Link> links;
            GetBunsetsuTags(out segs, out links);
            Dictionary<long, int> segIdMap = new Dictionary<long, int>();

            wr.WriteLine(string.Format("digraph \"{0}.{1}\" {{", m_Corpus.Name, m_Context.Sen.ID));
            wr.WriteLine("graph [charset = \"utf-8\"];");
            wr.WriteLine("node [fontname = \"sans\"];");
            for (int i = 0; i < segs.Count; i++)
            {
                Segment b = segs[i];
                segIdMap[b.ID] = i + 1;
                string str = string.Format("{0} [label = \"{0}:{1}\"];", i + 1, SegmentToString(b));
                wr.WriteLine(str);
            }
            foreach (Link lnk in links)
            {
                int fromid;
                int toid;
                if (!segIdMap.TryGetValue(lnk.From.ID, out fromid))
                {
                    continue;
                }
                if (!segIdMap.TryGetValue(lnk.To.ID, out toid))
                {
                    continue;
                }
                string str = string.Format("{0} -> {1} [label=\"{2}\"];", fromid, toid, lnk.Tag.Name);
                wr.WriteLine(str);
            }

            wr.WriteLine("}");
        }

        public string GetScriptingStatements()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("s={0}", m_Context.Sen.ID);
            sb.AppendLine();
            sb.AppendFormat("c={0}", m_Context.CharOffset);
            sb.AppendLine();
            sb.AppendFormat("d={0}", m_Context.Sen.ParentDoc.ID);
            sb.AppendLine();
            sb.AppendLine();
            foreach (Operation op in m_History.GetCurrentOperationChain())
            {
                sb.Append(op.ToIronRubyStatement(m_Context));
                sb.AppendLine();
            }
            sb.Append("svc.Commit()");
            sb.AppendLine();
            return sb.ToString();
        }


        public string SegmentToString(Segment seg)
        {
            IDbConnection conn = m_Context.Session.Connection;
            IDbCommand cmd = conn.CreateCommand();
            //!SQL
            cmd.CommandText = string.Format("SELECT substr(document_text,{0},{1}) FROM document WHERE document_id={2}",
                seg.StartChar + 1, seg.EndChar - seg.StartChar, m_Context.Sen.ParentDoc.ID);
            string result = (string)cmd.ExecuteScalar();
            return result;
        }

        /// <summary>
        /// 参照用辞書を追加する
        /// </summary>
        /// <param name="dict"></param>
        public void AddReferenceDictionary(Dictionary dict)
        {
            var ctx = DictionaryAccessor.Create(dict);
            if (ctx.IsConnected)
            {
                m_Dictionaries.Add(ctx);
            }
        }

        /// <summary>
        /// 参照用辞書を含め、全ての使用可能なPOS, CType, CFormタグのリストを得る.
        /// stringキーは辞書名（カレントコーパスについては"Default"という名称とする）.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="ctypes"></param>
        /// <param name="cforms"></param>
        public void GetLexiconTags(
            out Dictionary<string, IList<PartOfSpeech>> pos,
            out Dictionary<string, IList<CType>> ctypes,
            out Dictionary<string, IList<CForm>> cforms)
        {
            GetLexiconTags(m_Context.Session, m_Dictionaries, out pos, out ctypes, out cforms);
        }

        /// <summary>
        /// 参照用辞書を含め、全ての使用可能なPOS, CType, CFormタグのリストを得る.
        /// stringキーは辞書名（カレントコーパスについては"Default"という名称とする）.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="ctypes"></param>
        /// <param name="cforms"></param>
        public static void GetLexiconTags(
            ISession session,
            List<DictionaryAccessor> refdics,
            out Dictionary<string, IList<PartOfSpeech>> pos,
            out Dictionary<string, IList<CType>> ctypes,
            out Dictionary<string, IList<CForm>> cforms)
        {
            pos = new Dictionary<string, IList<PartOfSpeech>>();
            ctypes = new Dictionary<string, IList<CType>>();
            cforms = new Dictionary<string, IList<CForm>>();

            // コーパスのタグ
            pos.Add("Default", GetPOSList(session));
            ctypes.Add("Default", GetCTypeList(session));
            cforms.Add("Default", GetCFormList(session));

            // 参照用辞書のタグ
            foreach (var refdic in refdics)
            {
                var refdicdb = refdic as DictionaryAccessor_DB;
                if (refdicdb != null)
                {
                    pos.Add(refdic.Name, GetPOSList(refdicdb.Session));
                    ctypes.Add(refdic.Name, GetCTypeList(refdicdb.Session));
                    cforms.Add(refdic.Name, GetCFormList(refdicdb.Session));
                }
            }
        }

        private static IList<PartOfSpeech> GetPOSList(ISession session)
        {
            return session.CreateQuery("from PartOfSpeech x order by x.Name asc").List<PartOfSpeech>();
        }
        private static IList<CType> GetCTypeList(ISession session)
        {
            return session.CreateQuery("from CType x order by x.Name asc").List<CType>();
        }
        private static IList<CForm> GetCFormList(ISession session)
        {
            return session.CreateQuery("from CForm x order by x.Name asc").List<CForm>();
        }

        public Lexeme FindLexeme(int lexid)
        {
            return Operation.FindLexeme(m_Context, lexid);
        }

        /// <summary>
        /// 指定したDocument内の絶対文字位置cposで始まるWordの、文内のWord番号を求める.
        /// </summary>
        /// <param name="docid"></param>
        /// <param name="cpos"></param>
        /// <returns></returns>
        public int GetWordOffset(int docid, int cpos)
        {
            Word w = Operation.FindWordStartingAt(m_Context, docid, cpos);
            return Math.Max(0, m_Context.Sen.GetWords(m_Context.Proj.ID).IndexOf(w));
        }

        /// <summary>
        /// Surfaceがstrに一致するLexemeをすべて得る。
        /// 但し、Lexeme.Equals()で同一とみなされるLexemeは除く
        /// （先に見つけられたものが優先される。コーパス内にあるものは最優先。）
        /// </summary>
        /// <param name="str"></param>
        public IList<LexemeCandidate> FindAllLexemeCandidates(string str)
        {
            return FindAllLexemeCandidates(m_Context.Session, m_Dictionaries, str);
        }

        /// <summary>
        /// Surfaceがstrに一致するLexemeをすべて得る。
        /// 但し、Lexeme.Equals()で同一とみなされるLexemeは除く
        /// （先に見つけられたものが優先される。コーパス内にあるものは最優先。）
        /// </summary>
        /// <param name="str"></param>
        public IList<LexemeCandidate> FindAllLexemeCandidates(ISession session, List<DictionaryAccessor> refdics, string str)
        {
            var list = new SortedList<string, LexemeCandidate>();

            // コーパス本体のLexicon
            var query = session.CreateQuery(string.Format("from Lexeme l where l.Surface='{0}'", str));
            var result = query.List<Lexeme>();
            bool needsNewLexeme = true;  // 新語登録に使えるLexeme (Freq=0) が見つからなければtrue.
            foreach (var lex in result)
            {
                string key = lex.ToString3();  // POSを優先してSortさせる
                if (!list.ContainsKey(key))
                {
                    list.Add(key, new LexemeCandidate(lex));
                }
                if (lex.PartOfSpeech == PartOfSpeech.Default)
                {
                    lex.CanEdit = true;
                    needsNewLexeme = false;
                }
                else
                {
                    lex.CanEdit = false;
                }
            }
            // 参照辞書
            foreach (var refdic in refdics)
            {
                result = refdic.FindLexemeBySurface(str);
                foreach (var lex in result)
                {
                    string key = lex.ToString3();
                    if (!list.ContainsKey(key))
                    {
                        var cand = new LexemeCandidate(lex);
                        cand.Url = refdic.Url;
                        list.Add(key, cand);
                    }
                    if (lex.Dictionary != null)
                    {
                        lex.Dictionary = string.Format("{0}[{1}]", refdic.Name, lex.Dictionary);
                    }
                    else
                    {
                        lex.Dictionary = refdic.Name;
                    }
                    lex.CanEdit = false;
                }
            }
            // MWEも検索
            var words = m_Context.Sen.GetWords(m_Context.Proj.ID);
            var matches = this.FindMWECandidates2(str, words);
            foreach (var match in matches)
            {
                var mwe = match.MWE;
                var lex = mwe.Lex;
                if (lex == null) continue;
                var matchranges = string.Join(",", from r in match.RangeList where (r.Start >= 0 && r.End >= 0) select r.ToString());
                var key = $"{lex.ToString3()}@{matchranges}";
                if (!list.ContainsKey(key))
                {
                    var cand = new LexemeCandidate(match);
                    cand.Url = match.Url;
                    list.Add(key, cand);
                }
                if (lex.Dictionary != null && lex.Dictionary.Contains("["))
                {
                    // 同じLexに対して2回以上通る可能性があるので、簡易的にチェックする.
                    // do nothing
                }
                else if (lex.Dictionary != null)
                {
                    lex.Dictionary = $"{mwe.Dictionary}[{lex.Dictionary}]";
                }
                else
                {
                    lex.Dictionary = mwe.Dictionary;
                }
                lex.CanEdit = false;
            }

            var ret = new List<LexemeCandidate>(list.Values);
            // 新語登録のための種となるLexemeをリストに追加する.
            if (needsNewLexeme)
            {
                var lex = Lexeme.CreateDefaultUnknownLexeme(str);
                lex.CanEdit = true;
                ret.Add(new LexemeCandidate(lex));
            }

            return ret;
        }

        /// <summary>
        /// propsで与えられた内容を元に既存のLexemeを更新または新たに生成してDBに登録する.
        /// Base, POS, CForm, CTypeも必要に応じて登録する.
        /// </summary>
        /// <param name="lex">既存ならID >= 0, 新語ならID &lt; 0</param>
        /// <param name="props">LPの順に並べられたProperty文字列の配列</param>
        public void CreateOrUpdateLexeme(ref Lexeme lex, string[] props, string customprop)
        {
            Operation.CreateOrUpdateLexeme(m_Context, ref lex, props, customprop);
        }

        /// Opthis.Context.RelevantLexemesに記録されたLexemeそれぞれについて、
        /// (1) Wordから参照されておらず、他のLexemeのBaseでもなければDBから削除する.
        ///     但し、this.Contextに含まれている「このTransactionを開始する前から存在するLexemeの最大ID」以下のものは削除しない。
        /// (2) 残った語について、Frequencyをupdateする。
        private void CleanupLexeme()
        {
            List<Lexeme> deleteList = new List<Lexeme>();
            List<Lexeme> retainList = new List<Lexeme>();

            m_Context.Flush();
            foreach (Lexeme lex in m_Context.RelevantLexemes)
            {
                IQuery query = m_Context.Session.CreateQuery(string.Format("select count(*) from Word w where w.Lex.ID='{0}'", lex.ID));
                long count = query.UniqueResult<long>();
                if (count == 0)
                {
                    if (!deleteList.Contains(lex))
                    {
                        deleteList.Add(lex);
                    }
                }
                else
                {
                    retainList.Add(lex.BaseLexeme);
                }
                lex.Frequency = (int)count;
                m_Context.Session.SaveOrUpdate(lex);
            }
            foreach (Lexeme lex in deleteList)
            {
                // 他の有効な語の基本形になっていれば残す
                if (retainList.Contains(lex))
                {
                    continue;
                }
                // Transaction開始前からDBに存在する語は残す
                if (lex.ID >= 0 && lex.ID <= m_Context.MaxLexIDAtBeginning)
                {
                    continue;
                }
                ISQLQuery q = m_Context.Session.CreateSQLQuery(string.Format("DELETE FROM lexeme WHERE ID={0}", lex.ID));
                q.ExecuteUpdate();
            }
        }

        /// <summary>
        /// word listのそれぞれの語で開始されるMWEを複合語辞書(Cradle)から検索する.
        /// </summary>
        /// <param name="str"></param>
        public List<MWE> FindMWECandidates(IList<Word> words, Action<string> showMessageCallback = null, Action<MWE, MatchingResult> foundMWECallback = null)
        {
            return FindMWECandidates(m_Dictionaries, words, showMessageCallback, foundMWECallback);
        }

        public static List<MWE> FindMWECandidates(List<DictionaryAccessor> dicts, IList<Word> words, Action<string> showMessageCallback, Action<MWE, MatchingResult> foundMWECallback)
        {
            var result = new List<MWE>();

            int wpos = 0;
            foreach (var word in words)
            {
                if (showMessageCallback != null)
                {
                    showMessageCallback(string.Format("Searching ({0}/{1}) ...", wpos, words.Count));
                }
                foreach (var dict in dicts)
                {
                    if (!dict.CanSearchCompoundWord)
                    {
                        continue;
                    }
                    var list = dict.FindMWEBySurface(word.Lex.Surface);
                    foreach (var mwe in list)
                    {
                        // 見つかったMWEがWord Listと合致するかチェックする.
                        var matches = MWEMatcher.Match(words, wpos, mwe);
                        if (foundMWECallback != null)
                        {
                            foreach (var match in matches)
                            {
                                foundMWECallback(mwe, match);
                            }
                        }
                        result.Add(mwe);
                    }
                }
                wpos++;
            }
            return result;
        }

        /// <summary>
        /// surfaceを含み、word list（本文）の一部になりうるMWEをすべて辞書から検索し、
        /// word listとのマッチング結果を返す.
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        public List<MatchingResult> FindMWECandidates2(string surface, IList<Word> words)
        {
            var result = new List<MatchingResult>();

            int wpos = 0;
            foreach (var dict in m_Dictionaries)
            {
                if (!dict.CanSearchCompoundWord)
                {
                    continue;
                }
                var list = dict.FindMWEBySurface2(surface);
                foreach (var mwe in list)
                {
                    mwe.Dictionary = dict.Name;
                    // 見つかったMWEがWord Listと合致するかチェックする.
                    var matches = MWEMatcher.Match(words, wpos, mwe);
                    foreach (var match in matches)
                    {
                        result.Add(match);
                        match.Url = dict.Url;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 現在のSentenceの持つ "MWE"Group annotationおよびその範囲に付与された係り受けをすべて取得し、
        /// MWEオブジェクトに変換して返す.
        /// </summary>
        /// <param name="showMessageCallback"></param>
        /// <param name="foundMWECallback"></param>
        /// <returns></returns>
        public List<MWE> FindMWEAnnotations(Action<MWE, List<int>, List<int>> foundMWECallback)
        {
            var result = new List<MWE>();

            if (m_Context.Sen == null)
            {
                throw new InvalidOperationException("Unknown sentence.");
            }
            var groups = Util.RetrieveWordGroups(m_Context.Session, m_Context.Sen, m_Context.Proj.ID);
            var mwegroups = from g in groups where g.Tag.Name == "MWE" select g;

            // この文のBunsetsu Segmentをリスト
            var buns = Util.RetrieveBunsetsu(m_Context.Session, m_Context.Sen, m_Context.Proj.ID);
            // この文のWordをリスト
            var words = m_Context.Sen.GetWords(m_Context.Proj.ID);
            if (buns.Count() != words.Count() + 1)
            {
                throw new InvalidOperationException("Bunsetsu:Word must be 1:1 for this operation (count mismatch).");
            }
            // 各Bunsetsuの係り先をwordsへのindexで取得
            var deps = new List<int>();
            for (int i = 0; i < words.Count(); i++)
            {
                if (buns[i].StartChar != words[i].StartChar || buns[i].EndChar != words[i].EndChar)
                {
                    throw new InvalidOperationException("Bunsetsu:Word must be 1:1 for this operation (position mismatch).");
                }
                var toseg = Util.FindDependencyFrom(m_Context.Session, m_Context.Sen, buns[i]);
                deps.Add(buns.IndexOf(toseg));
            }

            foreach (var g in mwegroups)
            {
                var mwe = new MWE();
                var wposlist = new List<int>();
                var deplist = new List<int>();

                // このgroupの子であるSegmentをリストする.
                var segs = (from a in g.Tags where (a is Segment) select a).Cast<Segment>().OrderBy(s => s.StartChar);
                foreach (var seg in segs)
                {
                    var wlist = Util.WordsInRange(words, seg.StartChar, seg.EndChar);
                    wposlist.AddRange(wlist);
                    // Placeholder Segmentを識別
                    var posattr = (from attr in seg.Attributes where attr.Key == "POS" select attr).FirstOrDefault();
                    if (posattr != null)
                    {
                        mwe.Items.Add(new MWENode() { Label = string.Format("[POS:{0}]", posattr.Value) });
                        // Placeholderの内部から外に出る係りを探す(見つからなければ-1とする)
                        var dep = -1;
                        foreach (var p in wlist)
                        {
                            if (!wlist.Contains(deps[p]))
                            {
                                dep = deps[p];
                            }
                        }
                        deplist.Add(dep);
                    }
                    else
                    {
                        foreach (var p in wlist)
                        {
                            mwe.Items.Add(new MWENode() { Label = words[p].Lex.Surface });
                            deplist.Add(deps[p]);
                        }
                    }
                }
                // deplistをwposlistのindex番号で置き換える. wposlist内に係り先がないものはここで自動的に-1になる.
                for (int i = 0; i < deplist.Count; i++)
                {
                    var dep = deplist[i];
                    deplist[i] = wposlist.IndexOf(dep);
                    mwe.Items[i].DependsTo = deplist[i];
                }

                if (foundMWECallback != null)
                {
                    foundMWECallback(mwe, wposlist, deplist);
                }
                result.Add(mwe);
            }
            return result;
        }

        public void RegisterMWEToDictionary(MWE mwe)
        {
            foreach (var dict in m_Dictionaries)
            {
                if (!dict.CanSearchCompoundWord)
                {
                    continue;
                }
                dict.RegisterMWE(mwe);
            }
        }


        // ILexiconServiceの使用しないインターフェイス
        public void Open(Corpus cps)
        {
            throw new NotImplementedException();
        }

        public IList<Lexeme> Search(string str)
        {
            throw new NotImplementedException();
        }

        public int QueryFrequency(Lexeme lex)
        {
            throw new NotImplementedException();
        }
    }
}
