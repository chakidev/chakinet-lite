using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Readers;
using Regex=System.Text.RegularExpressions.Regex;

namespace ChaKi.Service.Readers
{
    public abstract class CabochaReader : CorpusSourceReader
    {
        private static TagSet DefaultTagSet;
        public static TagSet TagSet { get; set; }

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

        public abstract Lexeme AddLexeme(string s, LexiconBuilder lb);
        public abstract Lexeme AddLexeme(string s, LexiconBuilder lb, bool baseOnly);

        static CabochaReader()
        {
            // Cabochaから要求されるTagSetを定義しておく
#if false
            DefaultTagSet = new TagSet("CabochaTagSet");
            DefaultTagSet.AddVersion(new TagSetVersion("1", 0, true));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Bunsetsu"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Apposition"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Parallel"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Nest"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "D"));
            //DefaultTagSet.AddTag(new Tag(Tag.LINK, "O"));
            DefaultTagSet.AddTag(new Tag(Tag.GROUP, "Apposition"));
            DefaultTagSet.AddTag(new Tag(Tag.GROUP, "Parallel"));
#else
            DefaultTagSet = new TagSet("CabochaTagSet");
            DefaultTagSet.AddVersion(new TagSetVersion("1", 0, true));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Foreign"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Disfluency"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Nest"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Bunsetsu"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Parallel"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Apposition"));
            DefaultTagSet.AddTag(new Tag(Tag.SEGMENT, "Generic"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "D"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "DX"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "B"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "BX"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "F"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "FX"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "Z"));
            DefaultTagSet.AddTag(new Tag(Tag.LINK, "ZX"));
            DefaultTagSet.AddTag(new Tag(Tag.GROUP, "Parallel"));
            DefaultTagSet.AddTag(new Tag(Tag.GROUP, "Apposition"));
            DefaultTagSet.AddTag(new Tag(Tag.GROUP, "Generic"));
#endif
            TagSet = null;
        }

        public CabochaReader(Corpus corpus, LexiconBuilder lb)
        {
            m_Corpus = corpus;
            this.LexiconBuilder = lb;

            this.FromDictionary = false;
            this.m_DocIdMapping = new Dictionary<int, int>();

            // TagSetの初期値はCabochaデフォルトTagSetとし、インポート中に出現したTagを随時加える.
            // 複数ファイルインポートの場合、ファイルを読み込むたびにここを通るが、
            // そのたびごとにstaticなTagSetをリセットすると、Segment, Link等から参照しているTagが
            // 次々と異なるものになってしまうので、既にTagSetに存在しているものを変更しないようにする.
            if (TagSet == null)
            {
                TagSet = new TagSet();
            }
            TagSet.MergeWith(DefaultTagSet);
        }

        public void SetFieldDefs(Field[] fieldDefs)
        {
            this.LexiconBuilder.SetFields(fieldDefs);
        }

        /// <summary>
        /// CabochaデータをCorpusに読み込む（文節情報はBunsetsuテーブルに入れる）
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        [Obsolete]
        public Document ReadFromFile(string path, string encoding)
        {
#if true
            throw new NotImplementedException();
#else
            Document newdoc = new Document();

            // 現在のChar Position
            int charPos = 0;

            LexiconBuilder lb = new LexiconBuilder();

            using (TextReader streamReader = new StreamReader(path, Encoding.GetEncoding(encoding)))
            {
                int n = 0;
                string s;
                Sentence sen = new Sentence(newdoc);
                Bunsetsu currentBunsetsu = null;     // 最後に読んだ文節
                StringBuilder sb = new StringBuilder();     // Document全体の平文を格納（\n区切り）
                while ((s = streamReader.ReadLine()) != null)
                {
                    if (s.StartsWith("*"))
                    {
                        //文節の開始
                        try
                        {
                            Bunsetsu buns = sen.AddBunsetsu(s);
                            currentBunsetsu = buns;
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(string.Format("Bunsetsu parse error: {0}", s));
                        }
                    }
                    else if (s.StartsWith("EOS"))
                    {
                        // 文の終わり
                        sen.CheckBunsetsus();   // デフォルト文節を追加。係り受け構造全体の整合性を取る。
                        sb.Append("\n");
                        sen.EndChar = charPos;  // EndCharは文区切り文字の前とする
                        charPos++;
                        m_Corpus.AddSentence(sen);

                        if (++n % 1000 == 0)
                        {
                            Console.Write("> {0}\r", n);
                        }
                        // 以降のWordのために、新しいSentenceを初期化して用意する。
                        sen = new Sentence(newdoc);
                        sen.StartChar = charPos;
                        currentBunsetsu = null;
                    }
                    else if (s.Trim().Length > 0)
                    {
                        Lexeme m = null;
                        try
                        {
                            m = this.AddLexeme(s, lb);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(string.Format("Lexeme parse error: {0}", s));
                        }
                        if (m != null)
                        {
                            Word w = sen.AddWord(m);
                            w.StartChar = charPos;
                            w.EndChar = charPos + w.CharLength;
                            w.Bunsetsu = currentBunsetsu;
                            w.Bunsetsu = currentBunsetsu;   // currentBunsetsu はChaSenの場合はnull。
                            // 日本語の場合：デリミタなしで平文を再現
                            sb.Append(m.Surface);
                            if (m_DelimitBySpace)
                            {
                                // 英語の場合：平文を再現するにはデリミタで単語を区切る必要がある
                                sb.Append(" ");
                                charPos += (m.CharLength+1);
                            }
                            else
                            {
                                charPos += m.CharLength;
                            }
                        }
                    }
                }
                newdoc.Text = sb.ToString();
                Console.Write("> {0} Sentences Found.\r", n);
            }
            return newdoc;
#endif
        }

        /// <summary>
        /// CabochaデータをCorpusに読み込む（文節情報はSegment,Linkテーブルに入れる）
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        public Document ReadFromFileSLA(string path, string encoding)
        {
            Document newdoc = new Document();
            newdoc.FileName = path;
            using (TextReader streamReader = new StreamReader(path, Encoding.GetEncoding(encoding)))
            {
                ReadFromStreamSLA(streamReader, -1, newdoc);
            }
            return newdoc;
        }


        /// <summary>
        /// CabochaデータをストリームからDocumentに読み込む。
        /// すべてのDocumentを読み込んだ後、LexiconBuilder中に構築された一時LexiconをCorpusにコピーすること。
        /// </summary>
        /// <param name="rdr">ストリームリーダ</param>
        /// <param name="sentenceCount">最大読み取りSentence数（-1なら制限なくStream末端まで全部読み込む）</param>
        /// <param name="doc">読み込み先Document</param>
        public void ReadFromStreamSLA(TextReader rdr, int sentenceCount, Document doc)
        {
            this.FromDictionary = false;

            string s;

            // 現在のChar Position (Document毎のPos)
            int charPos = 0;

            // 現在のSentence番号（通しIDおよびDocument毎のPos)
            int senID = 0;

            // 文節データの一時リスト
            CabochaBunsetsuList bunsetsuList = new CabochaBunsetsuList();

            Sentence sen = new Sentence(doc) { ID = senID++, Pos = 0 };
            CabochaBunsetsu currentBunsetsu = null;     // 最後に読んだ文節
            int wordPosInBunsetsu = 0;  // 現在の文節内のwordのindex
            List<CabochaBunsetsu> terminalBunsetsu = new List<CabochaBunsetsu>();    // 現在の文内において、係り先が-1であるような文節
            StringBuilder sb = new StringBuilder();     // Document全体の平文内容
            List<Segment> currentSegments = new List<Segment>();  // 今読んでいるSentenceに付属するBunsetsu以外のSegmentリスト
            object lastAnnotationTag = doc;  // 現在の文内において、最後に読んだDocumentまたはSegment（文節以外）またはLinkまたはGroup
            m_SentencesInDoc = new List<Sentence>();
            m_SegmentsInDoc = new List<Segment>();

            var regex = new Regex(@"^\* [0-9]+");  // Cabochaタグにマッチするパターン

            int n = 0;
            while ((s = rdr.ReadLine()) != null)
            {
                s = Cleanup(s);  // ファイル途中のBOM（catした時に残っている場合がある）を削除する
                if (s.StartsWith("#! DOCID"))
                {
                    // Cabocha拡張タグ - 書誌情報
                    Document newdoc = new Document();
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
                // 2021.1.5 add: DOCIDと似ているが、IDの定義の役割がなく、直前のDocumentに書誌情報(Attribute)を付与するだけのタグ
                else if (s.StartsWith("#! DOCATTR"))
                {
                    if (doc == null)
                    {
                        Console.WriteLine($"No current doc found: {s}");
                    }
                    try
                    {
                        string[] fields = s.Split(new char[] { '\t' });
                        if (fields.Length != 2)
                        {
                            throw new Exception();
                        }
                        AddDocumentAttribute(doc, fields[1]);
                    }
                    catch (Exception ex)
                    {
                        if (s.Length > 25)
                        {
                            s = s.Substring(0, 25);
                        }
                        Console.WriteLine("Ignoring an invalid extdata line: {0}...", s);
                    }
                }
                else if (s.StartsWith("#! DOC"))
                {
                    // Cabocha拡張タグ - Document開始
                    try
                    {
                        string[] fields = s.Split(new char[] { ' ' });
                        if (fields.Length != 3)
                        {
                            fields = s.Split(new char[] { ' ', '\t' });
                            if (fields.Length != 3)
                            {
                                throw new Exception();
                            }
                        }
                        int docid = Int32.Parse(fields[2]);
                        // 存在しないDocumentなら新規作成してDocumentSetに追加する.
                        if (m_Corpus.DocumentSet.FindDocument(docid) == null)
                        {
                            var newdoc = new Document() { ID = docid };
                            m_Corpus.DocumentSet.AddDocument(newdoc);
                            m_DocIdMapping.Add(docid, docid);
                        }
                        m_DocIdMapping.TryGetValue(docid, out docid); // 見つからなければdocidは不変
                        if (docid != doc.ID)
                        {
                            doc.Text = sb.ToString();
                            sb.Length = 0;
                            charPos = 0;
                            sen.StartChar = 0;
                            sen.Pos = 0;
                        }
                        doc = m_Corpus.DocumentSet.Documents[docid];    //カレントDocumentを変更
                        lastAnnotationTag = doc;
                        m_SentencesInDoc = new List<Sentence>(); // Sentenceリストをリセットする.
                        m_SegmentsInDoc = new List<Segment>(); // SEGMENTのリストをリセットする.
                        sen.ParentDoc = doc;
                    }
                    catch
                    {
                        Console.WriteLine("Ignoring an invalid extdata line: {0}", s);
                    }
                }
                else if (s.StartsWith("#! SEGMENT_S"))
                {
                    // Cabocha拡張タグ - 文内Segment
                    lastAnnotationTag = AddSegment(sen, s, currentSegments);
                }
                else if (s.StartsWith("#! SEGMENT"))
                {
                    // Cabocha拡張タグ - Document内Segment
                    lastAnnotationTag = AddSegment(doc, s);
                    m_SegmentsInDoc.Add(lastAnnotationTag as Segment);
                }
                else if (s.StartsWith("#! LINK_S"))
                {
                    // Cabocha拡張タグ - 文内Segment
                    lastAnnotationTag = AddLink(sen, s, currentSegments);
                }
                else if (s.StartsWith("#! LINK"))
                {
                    // Cabocha拡張タグ - Document内Segment
                    lastAnnotationTag = AddLink(s, m_SegmentsInDoc);
                }
                else if (s.StartsWith("#! GROUP_S"))
                {
                    // Cabocha拡張タグ - 文内Group
                    lastAnnotationTag = AddWordGroup(sen, s, currentSegments);
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
                        AddAttribute(sen, s, lastAnnotationTag);
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
                    sb.Append(filling);
                    charPos += filling.Length;
                }
                else if (s.StartsWith("#"))
                {
                    // Ignore
                }
                else if (s.StartsWith("*") && regex.IsMatch(s))
                {
                    //文節の開始
                    try
                    {
                        CabochaBunsetsu buns = ParseBunsetsu(sen, doc, charPos, s);
                        currentBunsetsu = buns;
                        if (buns.DependsTo == -1)
                        {
                            terminalBunsetsu.Add(buns);
                        }
                        bunsetsuList.Add(buns);
                        wordPosInBunsetsu = 0;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(string.Format("Bunsetsu parse error: {0}", s));
                    }
                }
                else if (s.StartsWith("EOS"))
                {
                    // 文の終わり
                    if (currentBunsetsu == null)
                    {   // デフォルト文節を追加(入力がChasen/Mecabの場合のため)
                        CabochaBunsetsu buns = new CabochaBunsetsu(sen, doc, sen.StartChar, 0, String.Empty, -1, 0.0);
                        buns.EndPos = charPos;
                        bunsetsuList.Add(buns);
                        currentBunsetsu = buns;
                        terminalBunsetsu.Add(buns);
                        wordPosInBunsetsu = 0;
                    }
                    // 終端ダミー文節を追加
                    CabochaBunsetsu dummy = new CabochaBunsetsu(sen, doc, charPos, currentBunsetsu.BunsetsuPos + 1, String.Empty, -1, 0.0);
                    bunsetsuList.Add(dummy);
                    // 係り先が-1である文節をdummyに係るようにする。
                    if (terminalBunsetsu != null && terminalBunsetsu.Count > 0)
                    {
                        foreach (CabochaBunsetsu buns in terminalBunsetsu)
                        {
                            buns.DependsTo = dummy.BunsetsuPos;
                            if (buns.DependsAs.Length == 0)
                            {
                                buns.DependsAs = "D";
                            }
                        }
                    }

                    if (++n % 1000 == 0)
                    {
                        Console.Write("> {0} Sentences.\r", n);
                    }
                    sen.EndChar = charPos;

                    // EOSとして'\n'を挿入する
                    //sb.Append("\n");
                    //charPos++;

                    m_SentencesInDoc.Add(sen);
                    m_Corpus.AddSentence(sen);
                    if (sentenceCount > 0 && n >= sentenceCount)
                    {
                        break;
                    }
                    // 以降のWordのために、新しいSentenceを初期化して用意する。
                    int lastsenpos = sen.Pos + 1;
                    sen = new Sentence(doc) { ID = senID++, Pos = lastsenpos };
                    sen.StartChar = charPos;
                    currentBunsetsu = null;
                    wordPosInBunsetsu = 0;
                    terminalBunsetsu.Clear();
                    currentSegments = new List<Segment>();
                    lastAnnotationTag = null;
                }
                else if (s.Trim().Length > 0)
                {
                    Lexeme m = null;
                    try
                    {
                        m = this.AddLexeme(s, this.LexiconBuilder);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(string.Format("Lexeme parse error: {0}", s));
                    }
                    if (m != null)
                    {
                        Word w = sen.AddWord(m);
                        w.StartChar = charPos;
                        w.EndChar = charPos + w.CharLength;
                        if (currentBunsetsu != null)   // currentBunsetsu はChaSenの場合はnull。
                        {
                            currentBunsetsu.AddWord(w);
                            if (wordPosInBunsetsu == currentBunsetsu.HeadInd)
                            {
                                w.HeadInfo |= HeadInfo.Independent;
                            }
                            if (wordPosInBunsetsu == currentBunsetsu.HeadAnc)
                            {
                                w.HeadInfo |= HeadInfo.Ancillary;
                            }
                        }
                        //                            w.Bunsetsu = currentBunsetsu;   // currentBunsetsu はChaSenの場合はnull。
                        sb.Append(m.Surface);
                        charPos += m.CharLength;
                        wordPosInBunsetsu++;
                    }
                }
            }
            doc.Text = sb.ToString();
            Console.Write("> {0} Sentences.\r", n);

            Console.WriteLine();
            // BunsetsuをSegment&LinkとしてCorpusに登録
            Tag bunsetsuTag = TagSet.FindTag(Tag.SEGMENT, "Bunsetsu");
            n = 0;
            foreach (CabochaBunsetsu buns in bunsetsuList.Values)
            {
                if (++n % 100 == 0)
                {
                    Console.Write("> {0} Segments.\r", n);
                }
                Segment seg = new Segment();
                seg.StartChar = buns.StartPos;
                seg.EndChar = buns.EndPos;
                seg.Tag = bunsetsuTag;
                seg.Doc = buns.Doc;
                seg.Sentence = buns.Sen;
                seg.Version = TagSet.CurrentVersion;
                m_Corpus.AddSegment(seg);
                buns.Seg = seg;
                foreach (Word w in buns.Words)
                {
                    w.Bunsetsu = seg;
                }
            }
            Console.WriteLine("> {0} Segments.", bunsetsuList.Count);
            n = 0;
            foreach (CabochaBunsetsu buns in bunsetsuList.Values)
            {
                if (++n % 100 == 0)
                {
                    Console.Write("> {0} Links.\r", n);
                }
                CabochaBunsetsu depBunsetsu = bunsetsuList.Find(buns.Sen, buns.DependsTo);
                if (depBunsetsu != null)
                {
                    Link link = new Link();
                    link.From = buns.Seg;
                    link.To = depBunsetsu.Seg;
                    link.FromSentence = buns.Sen;
                    link.ToSentence = buns.Sen;
                    link.Tag = TagSet.FindOrAddTag(Tag.LINK, buns.DependsAs);
                    link.Version = TagSet.CurrentVersion;
                    link.Attributes.Add(new LinkAttribute() {
                        Proj = link.Proj, Target = link, User = link.User, Version = link.Version,
                        Key = "Score", Value = buns.Score.ToString() });
                    m_Corpus.AddLink(link);
                }
            }
            Console.WriteLine("> {0} Links.", bunsetsuList.Count);
        }

        // 既に出来上がっているSentenceに対して、cabocha解析結果(rdr)から文節・係り受け関係を振りなおすため、
        // CabochaBunsetsuのListを再生成する.(Wordなどは既存のものがそのまま用いられる)
        // cabocha解析結果とSentenceの持つWordの不一致については検査しない.
        static public CabochaBunsetsuList ToBunsetsuList(Sentence sen, TextReader rdr)
        {
            var result = new CabochaBunsetsuList();
            string s;
            var currentWordIndex = 0;
            var charPos = 0;
            CabochaBunsetsu currentBunsetsu = null;
            List<CabochaBunsetsu> terminalBunsetsu = new List<CabochaBunsetsu>();    // 現在の文内において、係り先が-1であるような文節

            var words = sen.GetWords(0);  //TODO: Projectは0で固定.
            while ((s = rdr.ReadLine()) != null)
            {
                if (s.StartsWith("*"))
                {
                    currentBunsetsu = ParseBunsetsu(sen, null, charPos, s);
                    result.Add(currentBunsetsu);
                    if (currentBunsetsu.DependsTo == -1)
                    {
                        terminalBunsetsu.Add(currentBunsetsu);
                    }
                }
                else
                {
                    if (currentWordIndex < words.Count)
                    {
                        var word = words[currentWordIndex++];
                        currentBunsetsu.Words.Add(word);
                        charPos += word.CharLength;
                        currentBunsetsu.EndPos = charPos;
                    }
                }
            }
            // 終端ダミー文節を追加
            var dummy = new CabochaBunsetsu(sen, null, charPos, currentBunsetsu.BunsetsuPos + 1, String.Empty, -1, 0.0);
            result.Add(dummy);
            // 係り先が-1である文節をdummyに係るようにする。
            if (terminalBunsetsu != null && terminalBunsetsu.Count > 0)
            {
                foreach (CabochaBunsetsu buns in terminalBunsetsu)
                {
                    buns.DependsTo = dummy.BunsetsuPos;
                    if (buns.DependsAs.Length == 0)
                    {
                        buns.DependsAs = "D";
                    }
                }
            }


            return result;
        }

        static private CabochaBunsetsu ParseBunsetsu(Sentence sen, Document doc, int charPos, string s)
        {
            char[] bunsetsuSplitPattern = new char[] { ' ' };
            char[] numberPattern = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

            // "* 0 -1D 0/0 0.00000000"の形式の行をパースする
            string[] bunsetsuparams = s.Split(bunsetsuSplitPattern);
            if (bunsetsuparams.Length < 3)
            {
                throw new InvalidDataException();
            }
            int bunsetsuPos = Int32.Parse(bunsetsuparams[1]);
            int pos = bunsetsuparams[2].LastIndexOfAny(numberPattern);
            if (pos < 0 || pos + 1 > bunsetsuparams[2].Length - 1)
            {
                throw new InvalidDataException();
            }
            int depBunsetsuId = Int32.Parse(bunsetsuparams[2].Substring(0, pos + 1));
            string depType = bunsetsuparams[2].Substring(pos + 1, bunsetsuparams[2].Length - pos - 1);

            var heads = bunsetsuparams[3].Split('/');
            int headInd = (heads.Length > 0) ? int.Parse(heads[0]) : -1;  // 自立語主辞の文節内位置
            int headAnc = (heads.Length > 1)? int.Parse(heads[1]) : -1;  // 付属語主辞の文節内位置

            double score = Double.Parse(bunsetsuparams[4]);

            // パラメータが正しければ、文節オブジェクトを作成
            if (bunsetsuPos < 0 || depType == null)
            {
                throw new InvalidDataException();
            }
            return new CabochaBunsetsu(sen, doc, charPos, bunsetsuPos, depType, depBunsetsuId, score, headInd, headAnc);
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
            using (TextReader trdr = new StringReader(s))
            {
                XmlReader xrdr = XmlReader.Create(trdr);
                while (xrdr.Read())
                {
                    if (xrdr.Name.Equals("Root")) continue;
                    DocumentAttribute dt = new DocumentAttribute();
                    dt.ID = DocumentAttribute.UniqueID++;
                    dt.Key = xrdr.Name;
                    dt.Value = xrdr.ReadString();
                    newdoc.Attributes.Add(dt);
                }
            }
        }

        /// <summary>
        /// 拡張CabochaFormatに埋め込まれたDOCATTRタグからDocument Attributeを追加する.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="xmlstr"></param>
        /// <returns></returns>
        private void AddDocumentAttribute(Document targetdoc, string xmlstr)
        {
            string s;
            int sp = xmlstr.IndexOf('<');
            if (sp >= 0)
            {
                if (sp > 0)
                {
                    // XMLパートの前にファイルパスがある場合
                    s = string.Format("<Root><FilePath>{0}</FilePath>{1}</Root>", xmlstr.Substring(0, sp), xmlstr.Substring(sp));
                }
                else
                {
                    // XMLパートのみの場合
                    s = string.Format("<Root>{0}</Root>", xmlstr);
                }
            }
            else
            {
                // ファイルパスのみの場合
                s = string.Format("<Root><FilePath>{0}</FilePath></Root>", xmlstr);
            }
            using (TextReader trdr = new StringReader(s))
            {
                XmlReader xrdr = XmlReader.Create(trdr);
                while (xrdr.Read())
                {
                    if (xrdr.Name.Equals("Root")) continue;
                    DocumentAttribute dt = new DocumentAttribute();
                    dt.ID = DocumentAttribute.UniqueID++;
                    dt.Key = xrdr.Name;
                    dt.Value = xrdr.ReadString();
                    targetdoc.Attributes.Add(dt);
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
            string tagname = fields[2].Trim('"');
            try {
                int from = Int32.Parse(fields[3]);
                int to = Int32.Parse(fields[4]);
                Segment seg = new Segment();
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
                        string comment = s.Substring(p + 1, s.Length - p - 2);
                        seg.Comment = comment.Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n","\n").Replace("\\t","\t");
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
            string tagname = fields[2].Trim('"');
            try
            {
                int from = Int32.Parse(fields[3]);
                int to = Int32.Parse(fields[4]);
                Segment seg = new Segment();
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
                        string comment = s.Substring(p + 1, s.Length - p - 2);
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
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension LINK_S line (Format Error): {0}...", s);
                return null;
            }
            string tagname = fields[2].Trim('"');
            try
            {
                int from = Int32.Parse(fields[3]);
                int to = Int32.Parse(fields[4]);
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
                Link link = new Link();
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
                        string comment = s.Substring(p + 1, s.Length - p - 2);
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
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 5) // Commentは任意
            {
                Console.WriteLine("Ignoring an invalid extension LINK_S line (Format Error): {0}...", s);
                return null;
            }
            string tagname = fields[2].Trim('"');
            try
            {
                int from = Int32.Parse(fields[3]);
                int to = Int32.Parse(fields[4]);
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
                Link link = new Link();
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
                        string comment = s.Substring(p + 1, s.Length - p - 2);
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
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 3)
            {
                Console.WriteLine("Ignoring an invalid extension GROUP_S line (Format Error): {0}...", s);
            }
            string tagname = fields[2].Trim('"');
            try
            {
                Group grp = new Group();
                grp.Tag = TagSet.FindOrAddTag(Tag.GROUP, tagname);
                TagSet.FindOrAddTag(Tag.SEGMENT, tagname);  // 同名のSegment Tagも一緒に定義しておく。
                grp.Version = TagSet.CurrentVersion;

                for (int i = 3; i < fields.Length; i++)
                {
                    if (fields[i].Length == 0) continue;
                    if (fields[i][0] == '"') // Beginning of Comment field
                    {
                        break;
                    }
                    int segid = Int32.Parse(fields[i]);

                    Segment seg = segsOfSentence[segid];
                    grp.Tags.Add(seg);
                }
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        string comment = s.Substring(p + 1, s.Length - p - 2);
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
            string[] fields = s.Split(new char[] { ' ', '\t' });
            if (fields.Length < 3)
            {
                Console.WriteLine("Ignoring an invalid extension GROUP line (Format Error): {0}...", s);
            }
            string tagname = fields[2].Trim('"');
            try
            {
                Group grp = new Group();
                grp.Tag = TagSet.FindOrAddTag(Tag.GROUP, tagname);
                TagSet.FindOrAddTag(Tag.SEGMENT, tagname);  // 同名のSegment Tagも一緒に定義しておく。
                grp.Version = TagSet.CurrentVersion;

                for (int i = 3; i < fields.Length; i++)
                {
                    if (fields[i].Length == 0) continue;
                    if (fields[i][0] == '"') // Beginning of Comment field
                    {
                        break;
                    }
                    int segid = Int32.Parse(fields[i]);

                    if (segid < 0 || segid >= segsOfDocument.Count || segsOfDocument[segid] == null)
                    {
                        Console.WriteLine("Ignoring an invalid extension GROUP line (Invalid seg#{0}): {1}...", i - 3, s);
                        return null;
                    }

                    Segment seg = segsOfDocument[segid];
                    grp.Tags.Add(seg);
                }
                int p;
                if ((p = s.IndexOf('"')) >= 0)
                {
                    if (s.Length > p + 2)
                    {
                        string comment = s.Substring(p + 1, s.Length - p - 2);
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
            if (target is Document)
            {
                var doc = target as Document;
                // Documentの属性については、同一属性を重複読み込みしないようチェックを行う.
                if (doc.Attributes.FirstOrDefault(a => a.Key == key && a.Value == value) == null)
                {
                    doc.Attributes.Add(new DocumentAttribute()
                    {
                        Key = key, Value = value, Comment = comment, Version = TagSet.CurrentVersion,
                        ID = DocumentAttribute.UniqueID++
                    });
                }
            }
            else if (target is Segment)
            {
                var seg = target as Segment;
                seg.Attributes.Add(new SegmentAttribute() {
                    Target = seg, Key = key, Value = value, Comment = comment, Version = TagSet.CurrentVersion, Proj = seg.Proj, User = seg.User
                });
            }
            else if (target is Link)
            {
                var lnk = target as Link;
                lnk.Attributes.Add(new LinkAttribute() {
                    Target = lnk, Key = key, Value = value, Comment = comment, Version = TagSet.CurrentVersion, Proj = lnk.Proj, User = lnk.User
                });
            }
            else if (target is Group)
            {
                var grp = target as Group;
                grp.Attributes.Add(new GroupAttribute() {
                    Target = grp, Key = key, Value = value, Comment = comment, Version = TagSet.CurrentVersion, Proj = grp.Proj, User = grp.User
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
            string[] fields = s.Split(new char[] { ' ', '\t' });
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
            int i = 0;
            int j = sentences.Count - 1;
            Sentence result = null;
            while (j - i > 1 && result == null)
            {
                int m = (i + j) / 2;
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
            bool firstChar = true;
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
