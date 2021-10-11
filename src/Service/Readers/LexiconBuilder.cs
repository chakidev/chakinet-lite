using System;
using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Readers;
using System.Diagnostics;

namespace ChaKi.Service.Readers
{
    public class LexiconBuilder
    {
        private const string BASE_TAG = "基本形";
        private const string BASE_TAG_UNIDIC = "終止形-一般";

        // 各行を読み込んで区切った後の各フィールドの定義(Lexeme Propertyへのマッピング）
        // nullなら単一のフィールドをSurfaceに入れるだけとなる。
        private Field[] m_FieldDefs;

        /// <summary>
        /// Cabochaなどソースを読みながらLexiconを追加していくための連想リスト
        /// </summary>
        public SortedList<string, Lexeme> KeyedEntries { get; set; }
        public Dictionary<string, PartOfSpeech> KeyedPartsOfSpeech { get; set; }
        public Dictionary<string, CType> KeyedCTypes { get; set; }
        public Dictionary<string, CForm> KeyedCForms { get; set; }

        // 基本形検索のためにReading, Pronunciationを除いてキー化した連想リスト
        public SortedList<string, Lexeme> KeyedEntries2 { get; set; }

        // DB上に格納している最後のID （通常は0から開始. Project追加時のみ、各既存IDの最大値を指す.）
        public int LastLexemeID { get; set; }
        public int LastPOSID { get; set; }
        public int LastCTypeID { get; set; }
        public int LastCFormID { get; set; }

        public LexiconBuilder()
        {
            this.KeyedEntries = new SortedList<string, Lexeme>();
            this.KeyedEntries2 = new SortedList<string, Lexeme>();
            this.KeyedPartsOfSpeech = new Dictionary<string, PartOfSpeech>();
            this.KeyedCTypes = new Dictionary<string, CType>();
            this.KeyedCForms = new Dictionary<string, CForm>();
            m_FieldDefs = null;
            LastLexemeID = -1;
            LastPOSID = -1;
            LastCTypeID = -1;
            LastCFormID = -1;
        }

        public void SetFields(Field[] fieldDefs)
        {
            m_FieldDefs = fieldDefs;
            if (fieldDefs != null)
            {
                foreach (Field f in fieldDefs) {
                    if (f.MappedTo != null)
                    {
                        foreach (MappedTo mt in f.MappedTo)
                        {
                            if (mt.Tag == "Surface") mt.TagIndex = 0;
                            else if (mt.Tag == "Reading") mt.TagIndex = 1;
                            else if (mt.Tag == "LemmaForm") mt.TagIndex = 2;
                            else if (mt.Tag == "Pronunciation") mt.TagIndex = 3;
                            else if (mt.Tag == "BaseLexeme") mt.TagIndex = 4;
                            else if (mt.Tag == "Lemma") mt.TagIndex = 5;
                            else if (mt.Tag == "PartOfSpeech") mt.TagIndex = 6;
                            else if (mt.Tag == "CType") mt.TagIndex = 7;
                            else if (mt.Tag == "CForm") mt.TagIndex = 8;
                            else if (mt.Tag == "Custom") mt.TagIndex = 9;
                        }
                    }
                }
            }
        }

        private Lexeme PropsToLexeme(string[] props)
        {
            Debug.Assert(props.Length == 10);

            Lexeme m = new Lexeme() { ID = -1 };
            m.Surface = props[0];
            m.Reading = props[1];
            m.LemmaForm = props[2];
            m.Pronunciation = props[3];
            m.BaseLexeme = m;
            m.Lemma = props[5];
            m.PartOfSpeech = GetPartOfSpeech(props[6]);
            m.CType = GetCType(props[7]);
            m.CForm = GetCForm(props[8]);
            m.CustomProperty = props[9];
            return m;
        }

        /// <summary>
        /// 辞書に語を追加する。（辞書ファイルからLexemeを作成する場合に使用）
        /// この場合は２パスとなる（最初は基本形のみKeyedEntries2に集めるだけ、２パス目で基本形を検索しながらLexemeを作成）
        /// 辞書読み込みの場合は、基本形を勝手に作成しない。
        /// 
        /// </summary>
        /// <param name="s">語のPropertyをカンマで区切った文字列</param>
        /// <returns></returns>
        private Lexeme AddEntrySimple(string[] props, bool baseOnly)
        {
            Lexeme m = PropsToLexeme(props);
            m.Frequency = 0;

            bool isBase = false;
            string baseTag = (m.Lemma.Length > 0)?BASE_TAG_UNIDIC:BASE_TAG;  // Lemmaフィールドを持つか否かでUniDic判定
            if (m.CForm.Name.Equals(baseTag)) 
            {
                isBase = true;
            }
            string key2 = m.ToString2();

            // 1パス目ではKeyedEntries2に活用形を持つ語の基本形を登録するのみ
            if (baseOnly)
            {
                if (isBase)
                {
                    this.KeyedEntries2[key2] = m;
                }
                return null;
            }
            // 基本形の検索
            if (m.CForm.Name.Length > 0)
            {
                if (!isBase)
                {
                    Lexeme m_base = PropsToLexeme(props);
                    m_base.CForm = GetCForm(baseTag);
                    m_base.Surface = props[4];
                    m_base.Reading = string.Empty;
                    m_base.Pronunciation = string.Empty;

                    string key_base2 = m_base.ToString2();
                    Lexeme l;
                    if (!this.KeyedEntries2.TryGetValue(key_base2, out l))
                    {
                        throw new Exception("Could not find Base Lexeme");
                    }
                    m.BaseLexeme = l;
                }
                else
                {
                    // 基本形の場合は、KeyedEntries2内の基本形を使用する（そうしないと、基本形以外からの参照が別オブジェクトを指すことになる）
                    string key_base2 = m.ToString2();
                    Lexeme l;
                    if (!this.KeyedEntries2.TryGetValue(key_base2, out l))
                    {
                        throw new Exception("Could not find Base Lexeme");
                    }
                    m = l;
                }
            }

            string key = m.ToString();
            string key_base = m.BaseLexeme.ToString();
            if (!this.KeyedEntries.ContainsKey(key))
            {
                this.KeyedEntries[key] = m;
            }
            if (!this.KeyedEntries.ContainsKey(key_base))
            {
                this.KeyedEntries[key_base] = m.BaseLexeme;
            }
            return m;
        }

        /// <summary>
        /// 辞書に語を追加する。（本文からLexemeを動的に作成する場合に使用）
        /// </summary>
        /// <param name="s">語のPropertyをカンマで区切った文字列</param>
        /// <returns></returns>
        public Lexeme AddEntry(string[] props)
        {
            Lexeme m = PropsToLexeme(props);

            if (props[8].Length > 0 && !props[8].Equals(BASE_TAG))
            {
                // 基本形を検索（なければ登録）
                Lexeme m_base = PropsToLexeme(props);
                m_base.CForm = GetCForm(BASE_TAG);
                m_base.Surface = props[4];
                m_base.Reading = string.Empty;
                m_base.Pronunciation = string.Empty;

                string key_base = m_base.ToString();
                string key_base2 = m_base.ToString2();
                Lexeme l;
                if (!this.KeyedEntries.TryGetValue(key_base, out l))    // Reading, Pronunciationを含めて検索
                {
                    if (!this.KeyedEntries2.TryGetValue(key_base2, out l))    //なければ Reading, Pronunciationを除いて検索
                    {
                        this.KeyedEntries.Add(key_base, m_base);
                        this.KeyedEntries2.Add(key_base2, m_base);
                        l = m_base;
                    }
                }
                m.BaseLexeme = l;
            }

            string key = m.ToString();
            string key2 = m.ToString2();
            Lexeme lex;
            if (!this.KeyedEntries.TryGetValue(key, out lex))
            {
                // Reading, Pronunciationを除いて一致するものが既にあれば、そのReading, Pronunciationを修正するのみでよい
                if (!this.KeyedEntries2.TryGetValue(key2, out lex))
                {
                    this.KeyedEntries.Add(key, m);
                    if (m.CForm.Name.Equals(BASE_TAG))
                    {
                        this.KeyedEntries2.Add(key2, m);
                    }
                    lex = m;
                    lex.Frequency = 0;
                }
                else
                {
                    lex.Reading = m.Reading;
                    lex.Pronunciation = m.Pronunciation;
                }
            }
            lex.Frequency++;
            return lex;
        }


        /// <summary>
        /// 辞書に語を追加する。
        /// 同じ語が既にあればそのエントリを返す。
        /// </summary>
        /// <param name="s">表層形で表された１語のデータ。</param>
        /// <returns></returns>
        public Lexeme AddEntry(string plaintext)
        {
            string[] props = new string[] { plaintext, string.Empty, string.Empty, string.Empty, string.Empty,
                                            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            return AddEntrySimple(props, false);
        }

        /// <summary>
        /// 辞書に語を追加する
        /// 既に存在するコーパス内辞書から逆にLexiconBuilderを再現する場合に用いる.
        /// （Projectを追加する時に使用）
        /// </summary>
        /// <param name="lex"></param>
        /// <returns></returns>
        public Lexeme AddEntry(Lexeme lex)
        {
            string key = lex.ToString();
            string key2 = lex.ToString2();
            if (!this.KeyedEntries.ContainsKey(key))
            {
                this.KeyedEntries.Add(key, lex);
            }
            else
            {
                Console.WriteLine("Duplicated lexeme detected in KeyedEntries: {0}", key);
            }
            if (!this.KeyedEntries2.ContainsKey(key2))
            {
                this.KeyedEntries2.Add(key2, lex);
            }
            else
            {
                Console.WriteLine("Duplicated lexeme detected in KeyedEntries2: {0}", key2);
            }
            return lex;
        }

        public void AddPOS(PartOfSpeech pos)
        {
            if (!this.KeyedPartsOfSpeech.ContainsKey(pos.Name))
            {
                this.KeyedPartsOfSpeech.Add(pos.Name, pos);
            }
            else
            {
                Console.WriteLine("Duplicated POS detected in KeyedEntries: {0}", pos.Name);
            }
        }

        public void AddCType(CType ctype)
        {
            if (!this.KeyedCTypes.ContainsKey(ctype.Name))
            {
                this.KeyedCTypes.Add(ctype.Name, ctype);
            }
            else
            {
                Console.WriteLine("Duplicated CType detected in KeyedEntries: {0}", ctype.Name);
            }
        }

        public void AddCForm(CForm cform)
        {
            if (!this.KeyedCForms.ContainsKey(cform.Name))
            {
                this.KeyedCForms.Add(cform.Name, cform);
            }
            else
            {
                Console.WriteLine("Duplicated CForm detected in KeyedEntries: {0}", cform.Name);
            }
        }

        /// <summary>
        /// 辞書に語を追加する。
        /// 同じ語が既にあればそのエントリを返す。
        /// </summary>
        /// <param name="s">Tab区切りフォーマットで表された１語のデータ。</param>
        /// <returns></returns>
        public Lexeme AddEntryChasen(string s, bool fromDictionary, bool baseOnly)
        {
            string[] props = SplitChasenFormat(s);
            if (fromDictionary)
            {
                return AddEntrySimple(props, baseOnly);
            }
            else
            {
                return AddEntry(props);
            }
        }

        public Lexeme AddEntryMecab(string s, bool fromDictionary, bool baseOnly)
        {
            string[] props = SplitMecabFormat(s);
            if (fromDictionary)
            {
                return AddEntrySimple(props, baseOnly);
            }
            else
            {
                return AddEntry(props);
            }
        }

        public string[] SplitChasenFormat(string s)
        {
            Lexeme lex = new Lexeme();

            string[] fields = s.Split('\t');
            if (fields.Length < m_FieldDefs.Length)
            {
                throw new Exception(string.Format("Mismatch in field count. Required={0}; Seen={1}", m_FieldDefs.Length, fields.Length));
            }
            string[] ret = new string[10] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            for (int i = 0; i < m_FieldDefs.Length; i++)
            {
                if (m_FieldDefs[i].MappedTo != null)
                {
                    foreach (MappedTo mt in m_FieldDefs[i].MappedTo)
                    {
                        string f = ret[mt.TagIndex];
                        if (f.Length > 0)
                        {
                            if (mt.Tag == "Custom")
                            {
                                ret[mt.TagIndex] = string.Format("{0}\n{1}\t{2}", f, mt.CustomTagName, fields[i]);
                            }
                            else if (fields[i].Length > 0)
                            {
                                ret[mt.TagIndex] = string.Format("{0}-{1}", f, fields[i]);
                            }
                        }
                        else
                        {
                            if (mt.Tag == "Custom")
                            {
                                ret[mt.TagIndex] = string.Format("{0}\t{1}", mt.CustomTagName, fields[i]);
                            }
                            else
                            {
                                ret[mt.TagIndex] = fields[i];
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public string[] SplitMecabFormat(string s)
        {
            string[] topFields = s.Split('\t');
            if (topFields.Length < 2)
            {
                throw new ArgumentException();
            }
            List<string> subFields = CsvTextToFields(topFields[1].Replace("*", ""));
            // fieldをstring[]に一本化
            string[] fields = new string[m_FieldDefs.Length];
            fields[0] = topFields[0];
            for (int i = 1; i < m_FieldDefs.Length; i++)
            {
                if (i-1 >= subFields.Count)
                {
                    fields[i] = string.Empty;
                    continue;
                }
                fields[i] = subFields[i - 1];
            }

            string[] ret = new string[10] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            for (int i = 0; i < m_FieldDefs.Length; i++)
            {
                if (m_FieldDefs[i].MappedTo != null)
                {
                    foreach (MappedTo mt in m_FieldDefs[i].MappedTo)
                    {
                        string f = ret[mt.TagIndex];
                        if (f.Length > 0)
                        {
                            if (mt.Tag == "Custom")
                            {
                                ret[mt.TagIndex] = string.Format("{0}\n{1}\t{2}", f, mt.CustomTagName, fields[i]);
                            }
                            else if (fields[i].Length > 0)
                            {
                                ret[mt.TagIndex] = string.Format("{0}-{1}", f, fields[i]);
                            }
                        }
                        else
                        {
                            if (mt.Tag == "Custom")
                            {
                                ret[mt.TagIndex] = string.Format("{0}\t{1}", mt.CustomTagName, fields[i]);
                            }
                            else
                            {
                                ret[mt.TagIndex] = fields[i];
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public PartOfSpeech GetPartOfSpeech(string name)
        {
            PartOfSpeech obj = null;
            if (!this.KeyedPartsOfSpeech.TryGetValue(name, out obj))
            {
                obj = new PartOfSpeech(name) { ID = -1 };
                this.KeyedPartsOfSpeech.Add(name, obj);
            }
            return obj;
        }

        public CType GetCType(string name)
        {
            CType obj = null;
            if (!this.KeyedCTypes.TryGetValue(name, out obj))
            {
                obj = new CType(name) { ID = -1 };
                this.KeyedCTypes.Add(name, obj);
            }
            return obj;
        }

        public CForm GetCForm(string name)
        {
            CForm obj = null;
            if (!this.KeyedCForms.TryGetValue(name, out obj))
            {
                obj = new CForm(name) { ID = -1 };
                this.KeyedCForms.Add(name, obj);
            }
            return obj;
        }

        public void CopyToCorpusLexicon(Lexicon lexicon)
        {
            foreach (Lexeme lex in KeyedEntries.Values)
            {
                if (lex.ID < 0)
                {
                    lex.ID = ++ this.LastLexemeID;
                    lexicon.Add(lex);
                }
            }
            foreach (PartOfSpeech pos in KeyedPartsOfSpeech.Values)
            {
                if (pos.ID < 0)
                {
                    pos.ID = ++this.LastPOSID;
                    lexicon.PartsOfSpeech.Add(pos);
                }
            }
            foreach (CType ctype in KeyedCTypes.Values)
            {
                if (ctype.ID < 0)
                {
                    ctype.ID = ++this.LastCTypeID;
                    lexicon.CTypes.Add(ctype);
                }
            }
            foreach (CForm cform in KeyedCForms.Values)
            {
                if (cform.ID < 0)
                {
                    cform.ID = ++this.LastCFormID;
                    lexicon.CForms.Add(cform);
                }
            }
        }

        // based on the source code "http://dobon.net/vb/dotnet/file/readcsvfile.html"
        public static List<string> CsvTextToFields(string text)
        {
            text = text.Trim(new char[] { '\r', '\n' });
            List<string> fields = new List<string>();

            int csvTextLength = text.Length;
            int startPos = 0, endPos = 0;
            string field = "";

            while (true)
            {
                //空白を飛ばす
                while (startPos < csvTextLength && (text[startPos] == ' ' || text[startPos] == '\t'))
                {
                    startPos++;
                }

                //データの最後の位置を取得
                if (startPos < csvTextLength && text[startPos] == '"')
                {
                    //"で囲まれているとき
                    //最後の"を探す
                    endPos = startPos;
                    while (true)
                    {
                        endPos = text.IndexOf('"', endPos + 1);
                        if (endPos < 0)
                        {
                            throw new Exception("CSV Format error (quotation mark).");
                        }
                        //"が2つ続かない時は終了
                        if (endPos + 1 == csvTextLength || text[endPos + 1] != '"')
                        {
                            break;
                        }
                        //"が2つ続く
                        endPos++;
                    }

                    //一つのフィールドを取り出す
                    field = text.Substring(startPos, endPos - startPos + 1);
                    //""を"にする
                    field = field.Substring(1, field.Length - 2).Replace("\"\"", "\"");

                    endPos++;
                    //空白を飛ばす
                    while (endPos < csvTextLength && text[endPos] != ',' && text[endPos] != '\n')
                    {
                        endPos++;
                    }
                }
                else
                {
                    //"で囲まれていない --> 次のカンマを見つける
                    endPos = startPos;
                    while (endPos < csvTextLength && text[endPos] != ',')
                    {
                        endPos++;
                    }

                    //一つのフィールドを取り出す
                    field = text.Substring(startPos, endPos - startPos);
                    //後の空白を削除
                    field = field.TrimEnd();
                }

                //フィールドの追加
                fields.Add(field);

                //行の終了か調べる
                if (endPos >= csvTextLength)
                {
                    break;
                }

                //次のデータの開始位置
                startPos = endPos + 1;
            }

            return fields;
        }


        public Lexeme AddEntryConll(string s, bool fromDictionary, bool baseOnly)
        {
            string[] props = SplitConllFormat(s);
            if (fromDictionary)
            {
                return AddEntrySimple(props, baseOnly);
            }
            else
            {
                return AddEntry(props);
            }
        }

        public string[] SplitConllFormat(string s)
        {
            var lex = new Lexeme();

            var fields = s.Split('\t');
            if (fields.Length < 4)
            {
                throw new Exception(string.Format("Mismatch in field count. Required=4~10; Seen={0}",  fields.Length));
            }
            var ret = new string[10] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            // 0: ID in a sentence
            // 1: Surface Form
            ret[0] = fields[1].Trim();  // ここで末尾の空白を取る。インポート時に削除するのは原則末尾の空白のみ。
            // 2: Lemma Form
            if (!fields[2].Equals("_"))
            {
                ret[5] = fields[2];
            }
            // 3-4: POS
            if (!fields[3].Equals("_"))
            {
                ret[6] = fields[3];
            }
            if (fields.Length > 4 && !fields[4].Equals("_"))
            {
                ret[6] = string.Format("{0}-{1}", ret[6], fields[4]);
            }
            // 5: Features
            // 6: Head
            // 7: DepRel
            // 8: PHead
            // 9: PDepRel

            return ret;
        }

        public Lexeme AddEntryConllU(string s, bool fromDictionary, bool baseOnly)
        {
            string[] props = SplitConllUFormat(s);
            if (fromDictionary)
            {
                return AddEntrySimple(props, baseOnly);
            }
            else
            {
                return AddEntry(props);
            }
        }

        public string[] SplitConllUFormat(string s)
        {
            var lex = new Lexeme();

            var fields = s.Split('\t');
            if (fields.Length < 4)
            {
                throw new Exception(string.Format("Mismatch in field count. Required=4~10; Seen={0}", fields.Length));
            }
            var ret = new string[10] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            // 0: ID in a sentence
            // 1: Surface Form
            ret[0] = fields[1].Trim();  // ここで末尾の空白を取る。インポート時に削除するのは原則末尾の空白のみ。
            // 2: Lemma Form
            if (!fields[2].Equals("_"))
            {
                ret[5] = fields[2];
            }
            // 3-4: POS
            if (!fields[3].Equals("_"))
            {
                ret[6] = fields[3];
            }
            if (fields.Length > 4 && !fields[4].Equals("_"))
            {
                ret[6] = string.Format("{0}-{1}", ret[6], fields[4]);
            }
            // 5: Features
            // 6: Head
            // 7: DepRel
            // 8: PHead
            // 9: PDepRel

            return ret;
        }
    }
}
