using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Readers;
using System.IO;
using System.Xml;
using Regex = System.Text.RegularExpressions.Regex;

namespace ChaKi.Service.Readers
{
    public class ConllReader : CorpusSourceReader
    {
        private static TagSet DefaultTagSet;

        private static TagSet TagSet { get { return CabochaReader.TagSet; } }

        // 拡張Cabochaファイルを読んだとき、Corpusに既に同じDocIDのDocumentがあると異なるIDを割り振るが、
        // #DOCIDの値から、その新しいDocument.IDへのマッピングを記録する.
        // 複数ファイルをひとつのCorpusにする時に必要。通常は同値Mappingとなり意味を持たない。
        private Dictionary<int, int> m_DocIdMapping;

        // 文のDocument単位の出現リスト
        private List<Sentence> m_SentencesInDoc;

        // SEGMENT (_S付きでない)のDocument単位の出現リスト。
        // LINKタグのインデックスはこのリストのインデックスとなる。（エラーのあるSEGMENTはNULLが格納される）
        private List<Segment> m_SegmentsInDoc;

        protected Corpus m_Corpus;

        public LexiconBuilder LexiconBuilder { get; set; }  // 原始Lexicon

        public string EncodingToUse { get; set; }

        public bool FromDictionary { get; set; } // 辞書ファイルを読み込む場合はTrue; テキストを読み込む場合はFalse

        public Lexeme AddLexeme(string s, LexiconBuilder lb)
        {
            throw new NotImplementedException();
        }

        public Lexeme AddLexeme(string s, LexiconBuilder lb, bool baseOnly)
        {
            throw new NotImplementedException();
        }

        static ConllReader()
        {
            // CONLL用のデフォルトTagSet
            DefaultTagSet = new TagSet("CabochaTagSet");
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Bunsetsu"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Nest"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Parallel"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Apposition"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Generic"));
            DefaultTagSet.AddTag(new Tag(Tag.GROUP, "Parallel"));
            DefaultTagSet.AddTag(new Tag(Tag.GROUP, "Apposition"));
            DefaultTagSet.AddTag(new Tag(Tag.GROUP, "Generic"));

            DefaultTagSet.AddVersion(new TagSetVersion("1", 0, true));

            CabochaReader.TagSet = null;
        }

        public ConllReader(Corpus corpus, LexiconBuilder lb)
        {
            m_Corpus = corpus;
            this.LexiconBuilder = lb;

            this.FromDictionary = false;
            this.m_DocIdMapping = new Dictionary<int, int>();

            // TagSetの初期値はCabocha Defaultとし、インポート中に出現したTagを随時加える.
            // 複数ファイルインポートの場合、ファイルを読み込むたびにここを通るが、
            // そのたびごとにstaticなTagSetをリセットすると、Segment, Link等から参照しているTagが
            // 次々と異なるものになってしまうので、既にTagSetに存在しているものを変更しないようにする.
            if (CabochaReader.TagSet == null)
            {
                CabochaReader.TagSet = new TagSet();
            }
            CabochaReader.TagSet.MergeWith(DefaultTagSet);
        }

        public void SetFieldDefs(Field[] fieldDefs)
        {
            this.LexiconBuilder.SetFields(fieldDefs);
        }

        [Obsolete]
        public Document ReadFromFile(string path, string encoding)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// CONLLデータをCorpusに読み込む（係り受け情報はSegment,Linkテーブルに入れる）
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        public Document ReadFromFileSLA(string path, string encoding)
        {
            var newdoc = new Document();
            newdoc.FileName = path;
            using (var streamReader = new StreamReader(path, Encoding.GetEncoding(encoding)))
            {
                ReadFromStreamSLA(streamReader, -1, newdoc);
            }
            return newdoc;
        }

        // 現在のChar Position (Document毎のPos)
        private protected int m_CurCharPos;
        // 現在のSentence番号（通しIDおよびDocument毎のPos)
        private protected int m_CurSenID;
        // 現在のDocument
        private protected Document m_CurDoc;
        // 現在の文
        private protected Sentence m_CurSen;
        // 文節データの一時リスト
        private protected CabochaBunsetsuList m_BunsetsuList;
        //  現在（最後に読み込んだ）文節
        private protected CabochaBunsetsu m_CurBunsetsu;
        // 現在の文内において、係り先が-1であるような文節のリスト
        private protected List<CabochaBunsetsu> m_CurTerminalBunsetsu;
        // Document全体の平文内容
        private protected StringBuilder m_DocumentTextBuilder;
        // 今読んでいるSentenceに付属するBunsetsu以外のSegmentリスト
        private protected List<Segment> m_CurSegments;
        // 現在の複合語Chunk
        private protected CompositeWordChunk m_CompositeWordChunk;

        /// <summary>
        /// データをデータをストリームからDocumentに読み込む。
        /// すべてのDocumentを読み込んだ後、LexiconBuilder中に構築された一時LexiconをCorpusにコピーすること。
        /// </summary>
        /// <param name="rdr">ストリームリーダ</param>
        /// <param name="sentenceCount">最大読み取りSentence数（-1なら制限なくStream末端まで全部読み込む）</param>
        /// <param name="doc">読み込み先Document</param>
        public void ReadFromStreamSLA(TextReader rdr, int sentenceCount, Document doc)
        {
            this.FromDictionary = false;

            string s;

            m_CurDoc = doc;
            m_BunsetsuList = new CabochaBunsetsuList();
            m_CurCharPos = 0;
            m_CurSenID = 0;
            m_CurSen = new Sentence(m_CurDoc) { ID = m_CurSenID++, Pos = 0 };
            m_CurBunsetsu = null;     // 最後に読んだ文節
            m_CurTerminalBunsetsu = new List<CabochaBunsetsu>();    // 現在の文内において、係り先が-1であるような文節
            m_CurSegments = new List<Segment>();  // 今読んでいるSentenceに付属するBunsetsu以外のSegmentリスト
            m_DocumentTextBuilder = new StringBuilder();
            object lastAnnotationTag = m_CurDoc;  // 現在の文内において、最後に読んだDocumentまたはSegment（文節以外）またはLinkまたはGroup
            m_SentencesInDoc = new List<Sentence>();
            m_SegmentsInDoc = new List<Segment>();
            m_CompositeWordChunk = new CompositeWordChunk();

            var currentComposite = string.Empty;

            int n = 0;
            while (true)
            {
                s = rdr.ReadLine();
                if (s == null)
                {
                    if (m_CurSen.Words.Count > 0)  // 正常なファイル末尾なら、m_CurSenはクリアされているはず。
                    {
                        s = string.Empty;// ファイル末尾にEOS行（空行）がないので、空行の存在をシミュレートする.
                    }
                    else
                    {
                        break;
                    }
                }
                s = Cleanup(s);  // ファイル途中のBOM（catした時に残っている場合がある）を削除する

                if (s.StartsWith("#! DOCID"))
                {
                    // Cabocha拡張タグ - 書誌情報
                    var newdoc = new Document();
                    try
                    {
                        string[] fields = s.Split(new char[] { '\t' });
                        if (fields.Length != 3)
                        {
                            throw new Exception();
                        }
                        int newdocid = Int32.Parse(fields[1]);
                        if (m_Corpus.DocumentSet.FindDocument(newdocid) != null)
                        {
                            int newdocid_replace = m_Corpus.DocumentSet.GetUnusedDocumentID();
                            m_DocIdMapping.Add(newdocid, newdocid_replace);
                            newdocid = newdocid_replace;
                        }
                        else
                        {
                            m_DocIdMapping.Add(newdocid, newdocid);
                        }
                        SetupDocumentFromTag(newdoc, newdocid, fields[2]);
                    }
                    catch
                    {
                        if (s.Length > 25)
                        {
                            s = s.Substring(0, 25);
                        }
                        Console.WriteLine("Ignoring an invalid extdata line: {0}...", s);
                    }
                    m_Corpus.DocumentSet.AddDocument(newdoc);
                }
                else if (s.StartsWith("#! DOC"))
                {
                    // Cabocha拡張タグ - Document開始
                    try
                    {
                        string[] fields = s.Split(new char[] { ' ' });
                        if (fields.Length != 3)
                        {
                            throw new Exception();
                        }
                        int docid = Int32.Parse(fields[2]);
                        docid = m_DocIdMapping[docid];
                        if (docid != m_CurDoc.ID)
                        {
                            m_CurDoc.Text = m_DocumentTextBuilder.ToString();
                            m_DocumentTextBuilder.Length = 0;
                            m_CurCharPos = 0;
                            m_CurSen.StartChar = 0;
                            m_CurSen.Pos = 0;
                        }
                        m_CurDoc = m_Corpus.DocumentSet.Documents[docid];    //カレントDocumentを変更
                        lastAnnotationTag = m_CurDoc;
                        m_SentencesInDoc = new List<Sentence>(); // Sentenceリストをリセットする.
                        m_SegmentsInDoc = new List<Segment>(); // SEGMENTのリストをリセットする.
                        m_CurSen.ParentDoc = m_CurDoc;
                    }
                    catch
                    {
                        Console.WriteLine("Ignoring an invalid extdata line: {0}", s);
                    }
                }
                else if (s.StartsWith("#! SEGMENT_S"))
                {
                    // Cabocha拡張タグ - 文内Segment
                    lastAnnotationTag = AddSegment(m_CurSen, s, m_CurSegments);
                }
                else if (s.StartsWith("#! SEGMENT"))
                {
                    // Cabocha拡張タグ - Document内Segment
                    lastAnnotationTag = AddSegment(m_CurDoc, s);
                    m_SegmentsInDoc.Add(lastAnnotationTag as Segment);
                }
                else if (s.StartsWith("#! LINK_S"))
                {
                    // Cabocha拡張タグ - 文内Segment
                    lastAnnotationTag = AddLink(m_CurSen, s, m_CurSegments);
                }
                else if (s.StartsWith("#! LINK"))
                {
                    // Cabocha拡張タグ - Document内Segment
                    lastAnnotationTag = AddLink(s, m_SegmentsInDoc);
                }
                else if (s.StartsWith("#! GROUP_S"))
                {
                    // Cabocha拡張タグ - 文内Group
                    lastAnnotationTag = AddWordGroup(m_CurSen, s, m_CurSegments);
                }
                else if (s.StartsWith("#! GROUP"))
                {
                    // Cabocha拡張タグ - Document内Group
                    lastAnnotationTag = AddWordGroup(s, m_SegmentsInDoc);
                }
                else if (s.StartsWith("#! ATTR"))
                {
                    // Cabocha拡張タグ - Segment/Link/Group属性
                    if (lastAnnotationTag != null)
                    {
                        AddAttribute(m_CurSen, s, lastAnnotationTag);
                    }
                }
                else if (s.StartsWith("#! NAMESPACE"))
                {
                    // Cabocha拡張タグ - Namespace定義
                    // Namespace定義はCorpus属性として保持する.
                    // 本来はTagSetDefinitionテーブルに入れるべきだが、スキーマに適当なカラムが存在しない.
                    AddNamespaceDefinition(s);
                }
                else if (s.StartsWith("#\""))
                {
                    // Cabocha拡張タグ - 語間文字列
                    string filling = s.Trim().Substring(3, s.Length - 4);
                    filling = filling.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"");
                    m_DocumentTextBuilder.Append(filling);
                    m_CurCharPos += filling.Length;
                }
                else if (s.StartsWith("#"))
                {
                    ProcessCommentLine(s);
                }
                else if (s.Trim().Length > 0)
                {
                    // 語を表す1行を処理する.
                    var fields = s.Split('\t');
                    try
                    {
                        ProcessOneLine_2(s, fields);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("At line {0}: Error: {1}", n, ex.Message);
                     }
                }
                else
                {
                    // CONLL: 空行は文の終わり
                    if (m_CurBunsetsu == null)
                    {   // デフォルト文節を追加(入力がChasen/Mecabの場合のため)
                        var buns = new CabochaBunsetsu(m_CurSen, m_CurDoc, m_CurSen.StartChar, 0, String.Empty, -1, 0.0);
                        buns.EndPos = m_CurCharPos;
                        m_BunsetsuList.Add(buns);
                        m_CurBunsetsu = buns;
                        m_CurTerminalBunsetsu.Add(buns);
                    }
                    // 終端ダミー文節を追加
                    var dummy = new CabochaBunsetsu(m_CurSen, m_CurDoc, m_CurCharPos, m_CurBunsetsu.BunsetsuPos + 1, String.Empty, -1, 0.0);
                    m_BunsetsuList.Add(dummy);
                    // 係り先が-1である文節をdummyに係るようにする。
                    if (m_CurTerminalBunsetsu != null && m_CurTerminalBunsetsu.Count > 0)
                    {
                        foreach (var buns in m_CurTerminalBunsetsu)
                        {
                            buns.DependsTo = dummy.BunsetsuPos;
                        }
                    }

                    if (++n % 1000 == 0)
                    {
                        Console.Write("> {0} Sentences.\r", n);
                    }
                    m_CurSen.EndChar = m_CurCharPos;

                    m_SentencesInDoc.Add(m_CurSen);
                    m_Corpus.AddSentence(m_CurSen);
                    if (sentenceCount > 0 && n >= sentenceCount)
                    {
                        break;
                    }
                    // 以降のWordのために、新しいSentenceを初期化して用意する。
                    int lastsenpos = m_CurSen.Pos + 1;
                    m_CurSen = new Sentence(m_CurDoc) { ID = m_CurSenID++, Pos = lastsenpos };
                    m_CurSen.StartChar = m_CurCharPos;
                    m_CurBunsetsu = null;
                    m_CurTerminalBunsetsu.Clear();
                    m_CurSegments = new List<Segment>();
                    m_CompositeWordChunk.Clear();
                    lastAnnotationTag = null;
                }
            }
            m_CurDoc.Text = m_DocumentTextBuilder.ToString();
            Console.Write("> {0} Sentences.\r", n);

            Console.WriteLine();
            // BunsetsuをSegment&LinkとしてCorpusに登録
            var bunsetsuTag = TagSet.FindTag(Tag.SEGMENT, "Bunsetsu");
            n = 0;
            foreach (var buns in m_BunsetsuList.Values)
            {
                if (++n % 100 == 0)
                {
                    Console.Write("> {0} Segments.\r", n);
                }
                var seg = new Segment();
                seg.StartChar = buns.StartPos;
                seg.EndChar = buns.EndPos;
                seg.Tag = bunsetsuTag;
                seg.Doc = buns.Doc;
                seg.Sentence = buns.Sen;
                seg.Version = TagSet.CurrentVersion;
                seg.Comment = buns.Comment;
                m_Corpus.AddSegment(seg);
                buns.Seg = seg;
                string uniDicLemma = null;
                foreach (var pair in buns.Attrs)
                {
                    seg.Attributes.Add(new SegmentAttribute() {
                        Proj = seg.Proj,
                        Target = seg,
                        User = seg.User,
                        Version = seg.Version,
                        Key = pair.Key,
                        Value = pair.Value });
                }
                foreach (var w in buns.Words)
                {
                    w.Bunsetsu = seg;
                }
            }
            Console.WriteLine("> {0} Segments.", m_BunsetsuList.Count);
            n = 0;
            foreach (var buns in m_BunsetsuList.Values)
            {
                if (++n % 100 == 0)
                {
                    Console.Write("> {0} Links.\r", n);
                }
                var depBunsetsu = m_BunsetsuList.Find(buns.Sen, buns.DependsTo);
                if (depBunsetsu != null)
                {
                    var link = new Link();
                    link.From = buns.Seg;
                    link.To = depBunsetsu.Seg;
                    link.FromSentence = buns.Sen;
                    link.ToSentence = buns.Sen;
                    link.Tag = TagSet.FindOrAddTag(Tag.LINK, buns.DependsAs);
                    link.Version = TagSet.CurrentVersion;
                    link.Attributes.Add(new LinkAttribute()
                    {
                        Proj = link.Proj,
                        Target = link,
                        User = link.User,
                        Version = link.Version,
                        Key = "Score",
                        Value = buns.Score.ToString()
                    });
                    m_Corpus.AddLink(link);
                }
            }
            Console.WriteLine("> {0} Links.", m_BunsetsuList.Count);
        }

        protected virtual void ProcessCommentLine(string s)
        {
            // do nothing for Comment line (default).
        }

        // 各単語に１文節を割り当て、係り受けを付与する
        protected virtual void ProcessOneLine_1(string s, string[] fields)
        {
            var originalSurface = fields[1];
            Lexeme m = null;
            try
            {
                m = this.LexiconBuilder.AddEntryConll(s, this.FromDictionary, false);
            }
            catch (Exception)
            {
                Console.WriteLine(string.Format("Lexeme parse error: {0}", s));
            }
            if (m != null)
            {
                try
                {
                    var f0 = Int32.Parse(fields[0]) - 1;
                    var f7 = (fields.Length > 7) ? fields[7] : string.Empty;
                    var f6 = f0 + 1;
                    if (fields.Length > 6 && fields[6] != "_")
                    {
                        f6 = Int32.Parse(fields[6]) - 1;
                    }
                    var buns = new CabochaBunsetsu(m_CurSen, m_CurDoc, m_CurCharPos, f0, f7, f6, 0.0);
                    buns.EndPos = buns.StartPos + originalSurface.Length;
                    m_CurBunsetsu = buns;
                    if (buns.DependsTo == -1)
                    {
                        m_CurTerminalBunsetsu.Add(buns);
                    }
                    m_BunsetsuList.Add(buns);
                }
                catch (Exception)
                {
                    Console.WriteLine(string.Format("Bunsetsu parse error: {0}", s));
                }
                Word w = null;
                var feats = new string[0];
                if (fields.Length > 5 && fields[5] != "_")
                {
                    feats = fields[5].Split('|');
                }
                if (feats.Contains("SP"))
                {
                    // FEATURESで空白を指定している場合
                    // SentenceとBunsetsuにWordを追加.
                    w = m_CurSen.AddWord(m);
                    w.StartChar = m_CurCharPos;
                    w.EndChar = m_CurCharPos + w.CharLength + 1;
                    w.Extras = " ";
                    m_DocumentTextBuilder.Append(originalSurface + w.Extras);
                }
                else
                {
                    // SentenceとBunsetsuにWordを追加.
                    w = m_CurSen.AddWord(m);
                    w.StartChar = m_CurCharPos;
                    w.EndChar = m_CurCharPos + originalSurface.Length; // Word.Lengthを使ってもよいが、空白を含む文字列長であることに注意.
                    // Surfaceの末尾の空白をWordに記録
                    if (!originalSurface.Equals(m.Surface))
                    {
                        w.Extras = GetDiff(originalSurface, m.Surface);
                    }
                    m_DocumentTextBuilder.Append(originalSurface);
                }

                // 文節にこの語を割り当てる
                if (m_CurBunsetsu != null)
                {
                    m_CurBunsetsu.Words.Add(w);
                }
                m_CurCharPos += (w.EndChar - w.StartChar);
            }
        }

        // FEATSフィールドの IOB2 タグに応じて複数の行に1語を割り当て、さらにその1語に1文節と係り受けを付与する.
        // 複合語内の係り受けは破棄する.
        protected virtual void ProcessOneLine_2(string s, string[] fields)
        {
            var feats = new string[0];
            if (fields.Length > 5 && fields[5] != "_")
            {
                feats = fields[5].Split('|');
            }
            var btag = feats.FirstOrDefault(f => f.StartsWith("B-"));  // 複数のBタグがあった場合、最初のもののみ有効.
            var itag = feats.FirstOrDefault(f => f.StartsWith("I-"));

            // 複合語の終了か
            if (itag == null)
            {
                // 現在までに収集したCompositeWordChunkがあれば先に出力
                if (!m_CompositeWordChunk.IsEmpty())
                {
                    try
                    {
                        var scw = m_CompositeWordChunk.ToConllSingleLine();
                        var scwf = scw.Split('\t');
                        ProcessOneLine_1(scw, scwf);
                    }
                    finally
                    {
                        m_CompositeWordChunk.Clear();
                    }
                }
            }
            if (btag != null)  // BならChunkを初期化
            {
                m_CompositeWordChunk.Clear();
                m_CompositeWordChunk.ChunkPOS = btag.Substring(2);
            }
            if (btag != null || itag != null)  // BまたはI
            {
                // 現在の語をCompositeWordChunkに追加し、出力は行わない.
                var f0 = Int32.Parse(fields[0]) - 1;
                var f5 = (fields.Length > 5) ? fields[5] : "_";
                var f6 = f0 + 1;
                if (fields.Length > 6 && fields[6] != "_")
                {
                    f6 = Int32.Parse(fields[6]) - 1;
                }
                var f7 = (fields.Length > 7) ? fields[7] : string.Empty;
                m_CompositeWordChunk.Add(f0, fields[1], fields[2], f5, f6, f7);
            }
            else  // BでもIでもない
            {
                // 現在の行を出力
                ProcessOneLine_1(s, fields);
            }
        }

        static public string GetDiff(string orig, string lex)
        {
            if (orig.Length <= lex.Length)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();
            int i = 0;
            int j = 0;
            for (; i < orig.Length; i++)
            {
                if (i >= lex.Length || orig[i] != lex[j])
                {
                    sb.Append(orig[i]);
                }
                else
                {
                    j++;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 拡張CabochaFormat (旧ChaKiからのimport)に埋め込まれたDOCタグからDocumentを生成する。
        /// .bibファイルを使用する場合は呼ばれない。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="xmlstr"></param>
        /// <returns></returns>
        private void SetupDocumentFromTag(Document newdoc, int id, string xmlstr)
        {
            newdoc.ID = id;

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
                    newdoc.Attributes.Add(dt);
                }
            }
        }


        /// <summary>
        /// Lexiconデータをストリームから一時Lexiconに読み込む。
        /// すべてのDocumentを読み込んだ後、LexiconBuilder中に構築された一時LexiconをCorpusにコピーすること。
        /// </summary>
        /// <param name="rdr">ストリームリーダ</param>
        public void ReadLexiconFromStream(TextReader rdr)
        {
            ReadLexiconFromStream(rdr, false);
        }

        public void ReadLexiconFromStream(TextReader rdr, bool baseOnly)
        {
            this.FromDictionary = true;

            string s;

            int n = 0;
            while ((s = rdr.ReadLine()) != null)
            {
                if (s.Trim().Length > 0 && !s.StartsWith(";"))
                {
                    Lexeme m = null;
                    try
                    {
                        m = this.AddLexeme(s, this.LexiconBuilder, baseOnly);
                    }
                    catch (Exception ex)
                    {
                        string msg = (ex.Message != null && ex.Message.Length > 0) ? ex.Message : "Error";
                        Console.WriteLine("{0}: {1}", msg, s);
                    }
                    n++;
                    if (n % 500 == 0)
                    {
                        Console.Write("> {0}\r", n);
                    }
                }
            }
            Console.Write("> {0} Entries.\n", n);
        }

        /// <summary>
        /// 次の形式の文内SEGMENT_S定義からSegmentを生成する.
        ///  #! SEGMENT_S [segmentname] [StartPos_in_Sentence] [EndPos_in_Sentence] "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="s"></param>
        private Segment AddSegment(Sentence sen, string s, List<Segment> segsOfSentence)
        {
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension SEGMENT_S line (Format Error): {0}...", s);
                return null;
            }
            string tagname = fields[2];
            try
            {
                var from = Int32.Parse(fields[3]);
                var to = Int32.Parse(fields[4]);
                var seg = new Segment();
                seg.Tag = TagSet.FindOrAddTag(Tag.SEGMENT, tagname);
                seg.Doc = sen.ParentDoc;
                seg.Sentence = sen;
                seg.StartChar = from + sen.StartChar;
                seg.EndChar = to + sen.StartChar;
                seg.Version = TagSet.CurrentVersion;
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        var comment = s.Substring(p + 1, s.Length - p - 2);
                        seg.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
                    }
                }
                m_Corpus.AddSegment(seg);
                segsOfSentence.Add(seg);
                return seg;
            }
            catch
            {
                Console.WriteLine("Ignoring an invalid extension SEGMENT_S line (Parse Error): {0}...", s);
                return null;
            }
        }

        /// <summary>
        /// 次の形式の文内SEGMENT定義からSegmentを生成する.
        ///  #! SEGMENT [segmentname] [StartPos_in_Document] [EndPos_in_Document] "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="s"></param>
        private Segment AddSegment(Document doc, string s)
        {
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension SEGMENT_S line (Format Error): {0}...", s);
                return null;
            }
            string tagname = fields[2];
            try
            {
                var from = Int32.Parse(fields[3]);
                var to = Int32.Parse(fields[4]);
                var seg = new Segment();
                seg.Tag = TagSet.FindOrAddTag(Tag.SEGMENT, tagname);
                seg.Doc = doc;
                seg.Sentence = FindSentenceFromPosition(from, to, m_SentencesInDoc);
                seg.StartChar = from;
                seg.EndChar = to;
                seg.Version = TagSet.CurrentVersion;
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        var comment = s.Substring(p + 1, s.Length - p - 2);
                        seg.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
                    }
                }
                m_Corpus.AddSegment(seg);
                return seg;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ignoring an invalid extension SEGMENT line (Parse Error:{0}): {1}...", ex.Message, s);
                return null;
            }
        }
        /// <summary>
        /// 次の形式の文内LINK_S定義からLinkを生成する.
        ///  #! LINK_S [linkname] [StartSegSeq] [EndSegSeq] "[Comment]"
        /// </summary>
        /// <param name="sen"></param>
        /// <param name="s"></param>
        private Link AddLink(Sentence sen, string s, List<Segment> segsOfSentence)
        {
            var fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension LINK_S line (Format Error): {0}...", s);
                return null;
            }
            var tagname = fields[2];
            try
            {
                var from = Int32.Parse(fields[3]);
                var to = Int32.Parse(fields[4]);
                if (from < 0 || from >= segsOfSentence.Count)
                {
                    Console.WriteLine("Ignoring an invalid extension LINK_S line (Invalid from_seg): {0}...", s);
                    return null;
                }
                if (to < 0 || to >= segsOfSentence.Count)
                {
                    Console.WriteLine("Ignoring an invalid extension LINK_S line (Invalid to_seg): {0}...", s);
                    return null;
                }
                var link = new Link();
                link.Tag = TagSet.FindOrAddTag(Tag.LINK, tagname);
                link.From = segsOfSentence[from];
                link.To = segsOfSentence[to];
                link.Version = TagSet.CurrentVersion;
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
                m_Corpus.AddLink(link);
                return link;
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
        private Link AddLink(string s, List<Segment> segsOfDocument)
        {
            var fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension LINK_S line (Format Error): {0}...", s);
                return null;
            }
            var tagname = fields[2];
            try
            {
                var from = Int32.Parse(fields[3]);
                var to = Int32.Parse(fields[4]);
                if (from < 0 || from >= segsOfDocument.Count || segsOfDocument[from] == null)
                {
                    Console.WriteLine("Ignoring an invalid extension LINK line (Invalid from_seg): {0}...", s);
                    return null;
                }
                if (to < 0 || to >= segsOfDocument.Count || segsOfDocument[to] == null)
                {
                    Console.WriteLine("Ignoring an invalid extension LINK line (Invalid to_seg): {0}...", s);
                    return null;
                }
                var link = new Link();
                link.Tag = TagSet.FindOrAddTag(Tag.LINK, tagname);
                link.From = segsOfDocument[from];
                link.To = segsOfDocument[to];
                link.Version = TagSet.CurrentVersion;
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
                m_Corpus.AddLink(link);
                return link;
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
        private Group AddWordGroup(Sentence sen, string s, List<Segment> segsOfSentence)
        {
            var fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 3)
            {
                Console.WriteLine("Ignoring an invalid extension GROUP_S line (Format Error): {0}...", s);
            }
            var tagname = fields[2];
            try
            {
                var grp = new Group();
                grp.Tag = TagSet.FindOrAddTag(Tag.GROUP, tagname);
                TagSet.FindOrAddTag(Tag.SEGMENT, tagname);  // 同名のSegment Tagも一緒に定義しておく。
                grp.Version = TagSet.CurrentVersion;

                for (var i = 3; i < fields.Length; i++)
                {
                    if (fields[i].Length == 0) continue;
                    if (fields[i][0] == '"') // Beginning of Comment field
                    {
                        break;
                    }
                    var segid = Int32.Parse(fields[i]);

                    var seg = segsOfSentence[segid];
                    grp.Tags.Add(seg);
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
                m_Corpus.AddGroup(grp);
                return grp;
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
        private Group AddWordGroup(string s, List<Segment> segsOfDocument)
        {
            var fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 3)
            {
                Console.WriteLine("Ignoring an invalid extension GROUP line (Format Error): {0}...", s);
            }
            var tagname = fields[2];
            try
            {
                var grp = new Group();
                grp.Tag = TagSet.FindOrAddTag(Tag.GROUP, tagname);
                TagSet.FindOrAddTag(Tag.SEGMENT, tagname);  // 同名のSegment Tagも一緒に定義しておく。
                grp.Version = TagSet.CurrentVersion;

                for (var i = 3; i < fields.Length; i++)
                {
                    if (fields[i].Length == 0) continue;
                    if (fields[i][0] == '"') // Beginning of Comment field
                    {
                        break;
                    }
                    var segid = Int32.Parse(fields[i]);

                    if (segid < 0 || segid >= segsOfDocument.Count || segsOfDocument[segid] == null)
                    {
                        Console.WriteLine("Ignoring an invalid extension GROUP line (Invalid seg#{0}): {1}...", i - 3, s);
                        return null;
                    }

                    var seg = segsOfDocument[segid];
                    grp.Tags.Add(seg);
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
                m_Corpus.AddGroup(grp);
                return grp;
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
            var fields = s.Split(new char[] { ' ', '\t' });
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
            if (target is Document)
            {
                var doc = target as Document;
                // Documentの属性については、同一属性を重複読み込みしないようチェックを行う.
                if (doc.Attributes.FirstOrDefault(a => a.Key == key && a.Value == value) == null)
                {
                    doc.Attributes.Add(new DocumentAttribute()
                    {
                        Key = key,
                        Value = value,
                        Comment = comment,
                        Version = TagSet.CurrentVersion,
                        ID = DocumentAttribute.UniqueID++
                    });
                }
            }
            else if (target is Segment)
            {
                var seg = target as Segment;
                seg.Attributes.Add(new SegmentAttribute()
                {
                    Target = seg,
                    Key = key,
                    Value = value,
                    Comment = comment,
                    Version = TagSet.CurrentVersion,
                    Proj = seg.Proj,
                    User = seg.User
                });
            }
            else if (target is Link)
            {
                var lnk = target as Link;
                lnk.Attributes.Add(new LinkAttribute()
                {
                    Target = lnk,
                    Key = key,
                    Value = value,
                    Comment = comment,
                    Version = TagSet.CurrentVersion,
                    Proj = lnk.Proj,
                    User = lnk.User
                });
            }
            else if (target is Group)
            {
                var grp = target as Group;
                grp.Attributes.Add(new GroupAttribute()
                {
                    Target = grp,
                    Key = key,
                    Value = value,
                    Comment = comment,
                    Version = TagSet.CurrentVersion,
                    Proj = grp.Proj,
                    User = grp.User
                });
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
            var fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length != 4)
            {
                Console.WriteLine("Ignoring an invalid extension NAMESPACE line (Format Error): {0}...", s);
            }
            var key = fields[2].TrimStart('\"').TrimEnd('\"');
            if (!key.StartsWith("xmlns:"))
            {
                key = "xmlns:" + key;
            }
            var value = fields[3].TrimStart('\"').TrimEnd('\"');
            string comment = null;
            if (fields.Length > 4)
            {
                comment = fields[4];
            }
            m_Corpus.AddNamespace(new Namespace(key, value));
        }

        private Sentence FindSentenceFromPosition(int from, int to, List<Sentence> sentences)
        {
            var i = 0;
            var j = sentences.Count - 1;
            Sentence result = null;
            while (j - i > 1 && result == null)
            {
                var m = (i + j) / 2;
                if (sentences[m].StartChar < from)
                {
                    i = m;
                }
                else
                {
                    j = m;
                }
            }
            if (sentences[j].StartChar <= from)
            {
                result = sentences[j];
            }
            else
            {
                result = sentences[i];
            }
            if (from < result.StartChar || to > result.EndChar)
            {
                throw new Exception("Segment over sentences.");
            }
            return result;
        }

        /// <summary>
        /// 行頭のBOMを除去する
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Cleanup(string input)
        {
            var sb = new StringBuilder();
            var firstChar = true;
            foreach (var c in input)
            {
                if (c != 0xFEFF)
                {
                    sb.Append(c);
                    if (firstChar)
                    {
                        return input;
                    }
                }
                firstChar = false;
            }
            return sb.ToString();
        }
    }
}
