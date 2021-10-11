using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Common;
using ChaKi.Service.Database;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Export
{
    public class ExportServiceMweToConll
    {
        protected TextWriter m_TextWriter;
        protected ISession m_Session;
        protected ISessionFactory m_Factory;
        private const string EMPTY_COL = "_";

        public ExportServiceMweToConll(TextWriter wr)
        {
            m_TextWriter = wr;
        }

        public void ExportMWE(Corpus crps, ref bool cancelFlag, Action<int, object> progressCallback)
        {
            try
            {
                OpenCorpus(crps);

                var q = m_Session.CreateQuery("from Sentence order by ID asc");
                var sentences = q.List<Sentence>();
                int count = sentences.Count;
                int n = 0;
                int last_percent = -1;
                foreach (var sen in sentences)
                {
                    if (cancelFlag)
                    {
                        break;
                    }
                    ExportItem(crps, sen);
                    int percent = (int)((n + 1) * 100 / count);
                    if (progressCallback != null && last_percent != percent)
                    {
                        progressCallback(percent, new int[2] { n, count });
                        last_percent = percent;
                    }
                    n++;
                }
            }
            finally
            {
                Close();
                // TagIDキャッシュを無効化
                Util.Reset();
            }
        }

        private void ExportItem(Corpus crps, Sentence sen, int project_id = 0)
        {
            var groups = Util.RetrieveMWEGroups(m_Session, sen, project_id);

            if (groups.Count > 0)
            {
                var words = sen.GetWords(project_id);
                foreach (var g in groups)
                {
                    var mweparts = new List<MwePart>();
                    var segs = (from s in g.Tags where s is Segment select (Segment)s).ToList();
                    foreach (var s in segs)
                    {
                        var wis = Util.WordsInRange(words, s.StartChar, s.EndChar);
                        var ws = wis.Select(i => words[i]).ToList();

                        if (IsPlaceholder(s))
                        {
                            var mwepart = new MwePart(ws, s);
                            mweparts.Add(mwepart);
                        }
                        else
                        {
                            foreach (var i in wis)
                            {
                                var mwepart = new MwePart(words[i], s);
                                mweparts.Add(mwepart);
                            }
                        }
                    }
                    // Groupの範囲内にあるPlaceholderも追加する.
                    var ping = FindPlaceholderInGroup(sen, segs, words);
                    foreach (var p in ping)
                    {
                        if (!mweparts.Contains(p, MwePart.IsEqual))
                        {
                            mweparts.Add(p);
                        }
                    }

                    // Placeholderに重なるwordから成るmwepartは削除する.
                    mweparts.RemoveAll(p => IsInPlaceholder(mweparts, p));

                    // MWEPartの係り先を決定しつつ、未発見のPlaceholderを追加する
                    var additionalMweParts = new List<MwePart>();
                    foreach (var mwepart in mweparts)
                    {
                        var depmwepart = mwepart.SetDependency(this.m_Session, sen, segs, words, mweparts);
                        if (depmwepart != null)
                        {
                            // 新しいMwePartへの係りだった場合はそのMwePartもリストに追加
                            additionalMweParts.Add(depmwepart);
                        }
                    }
                    mweparts.AddRange(additionalMweParts);
                    // 最後にもう一度すべてのMWEについて、SetDependencyを呼ぶ
                    foreach (var mwepart in mweparts)
                    {
                        mwepart.SetDependency(this.m_Session, sen, segs, words, mweparts);
                    }

                    // ソートして出力
                    mweparts.Sort(MwePart.Comparer);
                    for (var i = 0; i < mweparts.Count; i++)
                    {
                        var mwepart = mweparts[i];
                        var depsto = -1;
                        if (mwepart.DepsTo != null)
                        {
                            var depindex = mweparts.IndexOf(mwepart.DepsTo);
                            if (depindex >= 0)
                            {
                                depsto = depindex;
                            }
                        }
                        m_TextWriter.WriteLine(string.Join("\t",
                            i + 1,  // ID
                            mwepart.Surface,  // FORM
                            mwepart.BaseForm, // LEMMA
                            mwepart.CPos ?? EMPTY_COL,  // CPOSTAG
                            mwepart.Pos ?? EMPTY_COL, // POSTAG
                            EMPTY_COL,    // FEATS
                            (depsto + 1).ToString(),  // HEAD
                            mwepart.DepAs ?? EMPTY_COL,  // DEPREL
                            EMPTY_COL,    // PHEAD
                            EMPTY_COL   // PDEPREL
                            ));
                    }
                    m_TextWriter.WriteLine();
                }
            }

        }

        // p がいずれかのPlaceholderに包含されているかを判定する.
        private bool IsInPlaceholder(List<MwePart> mweparts, MwePart p)
        {
            if (p.IsPlaceholder)
            {
                return false; // Placeholderの再帰構造は考えない.
            }
            var s = p.Words.First().StartChar;
            var e = p.Words.Last().EndChar;
            foreach (var w in p.Words)
            {
                if (mweparts.FirstOrDefault(mwep => 
                    mwep.IsPlaceholder 
                 && mwep.Seg.StartChar <= s && e <= mwep.Seg.EndChar) != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void OpenCorpus(Corpus c)
        {
            Close();

            var dbs = DBService.Create(c.DBParam);
            NHibernate.Cfg.Configuration cfg = dbs.GetConnection();
            m_Factory = cfg.BuildSessionFactory();
            m_Session = m_Factory.OpenSession();
            m_Session.FlushMode = FlushMode.Never;
        }

        protected virtual void Close()
        {
            if (m_Session != null)
            {
                m_Session.Close();
                m_Session = null;
            }
            if (m_Factory != null)
            {
                m_Factory.Close();
                m_Factory = null;
            }
        }

        // Groupに属するsegmentのリストを与え、その範囲内にあるPlaceholder Segmentがあれば
        // それらをMwePart化して返す.
        private IEnumerable<MwePart> FindPlaceholderInGroup(Sentence sen, IList<Segment> segs, IList<Word> words)
        {
            var result = new List<MwePart>();
            var ranges = Util.SegsToRangeList(segs);
            foreach (var range in ranges)
            {
                var psegs = RetrievePlaceholderInRange(sen, range);
                foreach (var pseg in psegs)
                {
                    var indices = Util.WordsInRange(words, pseg.StartChar, pseg.EndChar);
                    result.Add(new MwePart((from i in indices select words[i]).ToList(), pseg));
                }
            }
            return result;
        }

        private IEnumerable<Segment> RetrievePlaceholderInRange(Sentence sen, CharRange range)
        {
            var q = m_Session.CreateQuery(
                string.Format("from Segment seg where seg.Doc.ID = {0} and seg.Sentence.ID = {1} and seg.StartChar >= {2} and seg.EndChar <= {3} order by seg.StartChar",
                    sen.ParentDoc.ID, sen.ID, range.Start.Value, range.End.Value));
            return q.List<Segment>().Where(s => IsPlaceholder(s) && !Util.IsInGroup(m_Session, s, "MWE"));
        }

        public static bool IsPlaceholder(Segment seg)
        {
            if (seg.Tag.Name == "Placeholder")
            {
                return true;
            }
            if (seg.Attributes != null)
            {
                foreach (var a in seg.Attributes)
                {
                    if (a.Key.ToUpper() == "POS" && !string.IsNullOrEmpty(a.Value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        // MWEを構成するWord部品. 通常はword1つに対応するが、
        // アノテーションによっては複数のwordを含むこともあると考える.
        // 特にPlaceholderは複数のwordであることが多くなる.
        // CONLLで1行に出力される部分に相当する.
        class MwePart
        {
            // 構成wordの配列
            public List<Word> Words = new List<Word>();

            // 対応するSegment
            public Segment Seg;

            public bool IsPlaceholder;

            // 文中での開始位置
            public int StartPos;

            public string Surface = string.Empty;

            public string BaseForm = string.Empty;

            public string CPos = null;

            public string Pos = null;

            public MwePart DepsTo = null;

            public string DepAs = null;

            // ひとつのWordからMwePartを作成
            public MwePart(Word w, Segment s)
                : this(new List<Word>() { w }, s)
            {
            }

            // Word列からMwePartを作成
            public MwePart(IList<Word> words, Segment s)
            {
                if (IsPlaceholder(s))
                {
                    CreatePlaceholder(words, s);
                }
                else
                {
                    CreateMwe(words, s);
                }
            }

            private void CreateMwe(IList<Word> words, Segment s)
            {
                if (words.Count == 0)
                {
                    return;
                }
                this.Words.AddRange(words);
                this.StartPos = words[0].StartChar;
                this.Seg = s;
                this.IsPlaceholder = false;
                var surface = new StringBuilder();
                var baseform = new StringBuilder();
                foreach (var w in words)
                {
                    var lex = w.Lex;
                    if (lex == null) continue;
                    surface.Append(lex.Surface);
                    surface.Append(w.Extras);
                    baseform.Append(lex.BaseLexeme.Surface);
                    baseform.Append(w.Extras);
                }
                this.Surface = surface.ToString();
                this.BaseForm = baseform.ToString();
                var pos = words[0].Lex?.PartOfSpeech;
                if (pos != null)
                {
                    this.CPos = pos.Name1;
                    this.Pos = pos.Name2;
                }
            }

            // PlaceholderからMwePartを作成
            public void CreatePlaceholder(IList<Word> words, Segment s)
            {
                this.StartPos = s.StartChar;
                this.Words.AddRange(words);
                this.Seg = s;
                this.IsPlaceholder = true;
                this.Surface = "*";
                this.BaseForm = "*";
                var pos = (from a in s.Attributes where a.Key.ToUpper() == "POS" select a.Value).FirstOrDefault();
                if (pos != null)
                {
                    var tokens = pos.Split('-');
                    this.CPos = tokens[0];
                    this.Pos = (tokens.Length > 1) ? tokens[1] : string.Empty;
                }
                else
                {
                    // Segment AttributeからPOSが取得できなければ、word[0]から取得する.
                    if (words.Count > 0)
                    {
                        var wpos = words[0].Lex?.PartOfSpeech;
                        if (wpos != null)
                        {
                            this.CPos = wpos.Name1;
                            this.Pos = wpos.Name2;
                        }
                    }
                }
            }


            // このPartの係り先を決定(新たにMwePartを作成する必要があった場合はそのMwePartを返す)
            public MwePart SetDependency(ISession sess, Sentence sen, IList<Segment> segs, IList<Word> words, IList<MwePart> mwes)
            {
                if (this.Words.Count == 0) return null;

                var allBunsList = (from w in words select w.Bunsetsu).ToList();
                var partBunsList = (from w in this.Words select w.Bunsetsu).ToList();
                // this.Seg (MWE Segment)からmwesリストにある他のMwePartのMWE Segmentに入るLinkを探す（文節係り受けよりも優先）
                var mwelink = Util.FindLinksFrom(sess, sen, this.Seg).FirstOrDefault();// 複数ある場合はやむをえず１つに絞る
                if (mwelink != null)
                {
                    var to_mwe = mwes.FirstOrDefault(mwe => mwe.Seg == mwelink.To);
                    if (to_mwe != null)
                    {
                        this.DepsTo = to_mwe;
                        this.DepAs = mwelink.Tag.Name;
                        return null;
                    }
                    else
                    {
                        var wis = Util.WordsInRange(words, mwelink.To.StartChar, mwelink.To.EndChar);
                        var ws = wis.Select(i => words[i]).ToList();
                        var mwepart = new MwePart(ws, mwelink.To);
                        this.DepsTo = mwepart;
                        this.DepAs = mwelink.Tag.Name;
                        return mwepart;
                    }
                }

                // partBunsListから自MwePartの外、mwesリストにある他のMwePartに入るLinkを探す
                foreach (var buns in partBunsList)
                {
                    var lnk = Util.FindLinksFrom(sess, sen, buns).FirstOrDefault();
                    if (lnk == null) continue;
                    if (!partBunsList.Contains(lnk.To))
                    {
                        var i = allBunsList.IndexOf(lnk.To);
                        if (i >= 0)
                        {
                            var to_mwe = mwes.FirstOrDefault(mwe => mwe.Words.Contains(words[i]));
                            if (to_mwe != null)  // すでにMwePartになっている場合
                            {
                                this.DepsTo = to_mwe;
                                this.DepAs = lnk.Tag.Name;
                                return null;
                            }
                            else  // まだMwePartができていない場合(Group外Placeholder)
                            {
                                var to_buns = lnk.To;
                                var segs1 = Util.FindSegmentsInRange(sess, sen, to_buns.StartChar, to_buns.EndChar);
                                var placeholder = segs1.Where(s => IsPlaceholder(s) && !Util.IsInGroup(sess, s, "MWE")).FirstOrDefault();
                                if (placeholder != null)
                                {
                                    var wis = Util.WordsInRange(words, to_buns.StartChar, to_buns.EndChar);
                                    var ws = wis.Select(j => words[j]).ToList();
                                    var mwepart = new MwePart(ws, placeholder);
                                    this.DepsTo = mwepart;
                                    this.DepAs = lnk.Tag.Name;
                                    return mwepart;
                                }
                            }
                        }
                    }
                }
                return null;
            }

            public static Comparison<MwePart> Comparer = (x, y) => (x.StartPos - y.StartPos);

            public static IEqualityComparer<MwePart> IsEqual = new MweEqualityComparer();

            public class MweEqualityComparer : IEqualityComparer<MwePart>
            {
                public bool Equals(MwePart x, MwePart y)
                {
                    return (x.Seg == y.Seg);
                }

                public int GetHashCode(MwePart obj)
                {
                    return obj.Seg.GetHashCode();
                }
            }
        }
    }
}
