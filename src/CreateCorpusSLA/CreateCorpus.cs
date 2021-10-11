using System;
using System.Collections.Generic;
using System.Data.Common;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Service.Database;
using ChaKi.Service.Readers;
using Iesi.Collections.Generic;
using MySql.Data.MySqlClient;
using System.IO;
using System.Text;
using ChaKi.Entity;
using ChaKi.Service.Search;
using NHibernate;
using NHibernate.Criterion;
using Iesi.Collections;
using ChaKi.Common;

namespace CreateCorpusSLA
{
    public class CreateCorpus
    {
        public string InputPath = null;
        public string CorpusName = null;
        public string TextEncoding = "Auto";
        public string ReaderType = "Auto";
        public string BibSource = null;
        public string LexSource = null;
        public bool IsCreatingDictionary = false;
        public bool SynchronousOff = false;
        public bool DoNotPauseOnExit = false;
        public bool CreateSeparateDB = false;
        public string DbType = "SQLite";        // SQLite or DBMS
        public bool ForceCheckInputExtension = false;
        public int ProjectId = 0;   // ユーザーが指定したProject ID（無指定なら"0"）

        private DBService m_Service = null;
        private Corpus m_Corpus = null;
        private string m_DefaultString;

        private List<int> m_SentencesInDocuments = new List<int>();
        private DocumentSet m_DocumentSet;
        private DocumentSetProjectMapping m_DocumentSetProjectMapping;
        private User m_User;
        private Project m_Project;
        private LexiconBuilder m_LexBuilder = null;
        private bool m_IsFolderInput;

        public void ResetInternals()
        {
            m_Service = null;
            m_Corpus = null;
            m_DefaultString = null;

            m_SentencesInDocuments = new List<int>();
            m_DocumentSet = null;
            m_DocumentSetProjectMapping = null;
            m_User = null;
            m_Project = null;
            m_LexBuilder = null;

        }

        /// <summary>
        /// CreateCorpusの一連の処理を実行してDatabaseを作成する.
        /// 呼び出し前にパラメータフィールドが適切に設定されている必要がある.
        /// </summary>
        /// <returns></returns>
        public bool DoAllSteps()
        {
            InitializeDocumentSet();
            if (!ParseBibFile()) return false;
            if (!InitializeDB()) return false;
            if (!InitializeLexiconBuilder()) return false;
            if (!ParseInput()) return false;
            if (!SaveProject()) return false;
            if (!SaveLexicon()) return false;
            if (!SaveSentences()) return false;
            if (!SaveSentenceTags()) return false;
            if (!UpdateIndex()) return false;

            return true;
        }

        public void InitializeDocumentSet()
        {
            m_DocumentSetProjectMapping = new DocumentSetProjectMapping() { ID = this.ProjectId };
            m_DocumentSet = new DocumentSet();
            m_DocumentSetProjectMapping.DocumentSet = m_DocumentSet;

            // Create Default Project for the DocumentSet
            m_Project = new Project() { ID = this.ProjectId };
            m_DocumentSet.AddProject(m_Project);
            m_DocumentSetProjectMapping.Proj = m_Project;

            // Create Default TagSet for the Project
            m_Project.TagSetList = new List<TagSet>();

            // Create Default User
            m_User = new User() { ID = 0, Name = User.DefaultName, Password = string.Empty };
            // Assign this User to the Default Project
            m_Project.AddUser(m_User);
        }

        public bool ParseArguments(string[] args)
        {
            int n = 0;
            foreach (string arg in args)
            {
                if (arg.Length > 1 && arg.StartsWith("-"))
                {
                    string[] tokens = arg.Substring(1).Split('=');
                    if (!(tokens.Length == 1 && (tokens[0].Equals("d") || tokens[0].Equals("s") || tokens[0].Equals("p")
                                              || tokens[0].Equals("C")))
                     && !(tokens.Length == 2 && (tokens[0].Equals("e") || tokens[0].Equals("t") || tokens[0].Equals("b")
                                              || tokens[0].Equals("l") || tokens[0].Equals("T")
                                              || tokens[0].Equals("P"))))
                    {
                        Console.WriteLine("Invalid option: {0}", arg);
                        return false;
                    }
                    if (tokens[0].Equals("e"))
                    {
                        this.TextEncoding = tokens[1];
                    }
                    else if (tokens[0].Equals("t"))
                    {
                        this.ReaderType = tokens[1];
                    }
                    else if (tokens[0].Equals("b"))
                    {
                        this.BibSource = tokens[1].Trim(new char[] { '"' });
                    }
                    else if (tokens[0].Equals("l"))
                    {
                        this.LexSource = tokens[1].Trim(new char[] { '"' });
                    }
                    else if (tokens[0].Equals("d"))
                    {
                        this.IsCreatingDictionary = true;
                    }
                    else if (tokens[0].Equals("s"))
                    {
                        this.SynchronousOff = true;
                    }
                    else if (tokens[0].Equals("C"))
                    {
                        this.DoNotPauseOnExit = true;
                    }
                    else if (tokens[0].Equals("p"))
                    {
                        this.CreateSeparateDB = true;
                    }
                    else if (tokens[0].Equals("T"))
                    {
                        this.DbType = tokens[1].Trim(new char[] { '"' });
                    }
                    else if (tokens[0].Equals("P"))
                    {
                        this.ProjectId = 0;
                        if (!Int32.TryParse(tokens[1], out this.ProjectId))
                        {
                            Console.WriteLine("Invalid Project ID: {0}", tokens[1]);
                            return false;
                        }
                    }
                }
                else
                {
                    if (n == 0)
                    {
                        this.InputPath = arg;
                    }
                    else if (n == 1)
                    {
                        this.CorpusName = arg;
                    }
                    n++;
                }
            }
            if (n < 2)
            {
                return false;
            }
            return true;
        }

        public bool CheckInput()
        {
            m_IsFolderInput = PathIsFolder(this.InputPath);
            if (m_IsFolderInput && BibSource != null)
            {
                Console.WriteLine("Cannot use Bib file when input is a folder.");
                return false;
            }
            return true;
        }

        public bool InitializeDB(bool initializeTables = true)
        {
            // Corpusの作成
            m_Corpus = Corpus.CreateFromFile(this.CorpusName);
            m_Corpus.DocumentSet = m_DocumentSet;
            if (m_Corpus == null)
            {
                Console.WriteLine("Error: Cannot create Corpus. Maybe <out file>'s extension is neither .db nor .def");
                return false;
            }

            // DB初期化のためのサービスを得る
            m_Service = DBService.Create(m_Corpus.DBParam);
            m_DefaultString = m_Service.GetDefault(); // INSERT文のDEFAULT文字列

            if (initializeTables)
            {
                try
                {
                    m_Service.DropDatabase();
                    m_Service.CreateDatabase();
                }
                catch (Exception ex)
                {
                    Console.Write("Info: ");
                    Console.WriteLine(ex.Message);
                    // Continue.
                }
            }

            // SQLiteのSynchronous Pragmaを制御
            try
            {
                if (SynchronousOff)
                {
                    m_Service.SetSynchronousMode("OFF");
                }
            }
            catch (Exception ex)
            {
                Console.Write("Info: ");
                Console.WriteLine(ex.Message);
                // Continue.
            }

            if (initializeTables)
            {
                try
                {
                    m_Service.DropAllTables();
                }
                catch (Exception ex)
                {
                    PrintException(ex);
                    // continue
                }
                try
                {
                    m_Service.CreateAllTables();
                    m_Service.CreateAllIndices();
                }
                catch (Exception ex)
                {
                    PrintException(ex);
                    // maybe some trouble in creating indices; continue anyway...
                }
            }
            return true;
        }

        public bool InitializeLexiconBuilder(bool loadFromDB = false)
        {
            if (this.LexSource != null && this.LexSource.Length > 0)
            {
                // 辞書ファイル読込み
                m_LexBuilder = ParseLexicon(this.LexSource);
                return (m_LexBuilder != null);
            }
            m_LexBuilder = new LexiconBuilder();
            if (loadFromDB)
            {
                // 既存のDB上のLexemeを読み込む.
                var cfg = SearchConfiguration.GetInstance().NHibernateConfig;
                m_Service.SetupConnection(cfg);
                using (var factory = cfg.BuildSessionFactory())
                using (var session = factory.OpenSession())
                {
                    var lexs = session.CreateCriteria<Lexeme>().AddOrder(new Order("ID", true)).List<Lexeme>();
                    foreach (var lex in lexs)
                    {
                        m_LexBuilder.AddEntry(lex);
                    }
                    var poses = session.CreateCriteria<PartOfSpeech>().AddOrder(new Order("ID", true)).List<PartOfSpeech>();
                    foreach (var pos in poses)
                    {
                        m_LexBuilder.AddPOS(pos);
                    }
                    var ctypes = session.CreateCriteria<CType>().AddOrder(new Order("ID", true)).List<CType>();
                    foreach (var ctype in ctypes)
                    {
                        m_LexBuilder.AddCType(ctype);
                    }
                    var cforms = session.CreateCriteria<CForm>().AddOrder(new Order("ID", true)).List<CForm>();
                    foreach (var cform in cforms)
                    {
                        m_LexBuilder.AddCForm(cform);
                    }

                    // 以下のIDまでが既存データであり、それらには変更を加えない.
                    m_LexBuilder.LastLexemeID = session.CreateQuery("select max(ID) from Lexeme").UniqueResult<int>();
                    m_LexBuilder.LastPOSID = session.CreateQuery("select max(ID) from PartOfSpeech").UniqueResult<int>();
                    m_LexBuilder.LastCTypeID = session.CreateQuery("select max(ID) from CType").UniqueResult<int>();
                    m_LexBuilder.LastCFormID = session.CreateQuery("select max(ID) from CForm").UniqueResult<int>();
                }
            }
            return true;
        }

        /// <summary>
        /// 現在のコーパスに存在するTag定義を CabochaReaderのDefaultTagSetにセットする.
        /// Project 追加時に呼ばれる. （追加の場合は、新しいTagSetをその場で登録することは不可とする）
        /// </summary>
        /// <returns></returns>
        public bool InitializeTagSet()
        {
            var cfg = m_Service.GetConnection();
            using (var factory = cfg.BuildSessionFactory())
            using (var session = factory.OpenSession())
            {
                CabochaReader.TagSet = new TagSet();
                var tags = session.CreateCriteria<Tag>().List<Tag>();
                foreach (var tag in tags)
                {
                    CabochaReader.TagSet.AddVersion(new TagSetVersion("1", 0, true));
                    CabochaReader.TagSet.AddTag(tag);
                }
            }

            return true;
        }

        public bool ParseInput()
        {
            // Inputファイル読込み（単一ファイル・または指定フォルダ以下すべて）
            if (ReadFilesRecursively(this.InputPath, ReadSingleInput))
            {
                // 原始LexiconをCorpusのLexiconへコピー
                m_LexBuilder.CopyToCorpusLexicon(m_Corpus.Lex);

                // CabochaReaderに定義されるTagSetをProjectに結びつける(CabochaのTagSetが最大セット）
                m_Project.AddTagSet(CabochaReader.TagSet);

                return true;
            }
            return false;
        }

        public bool ParseDictionaryInput()
        {
            // Corpusの作成
            m_Corpus = new Corpus();

            DBParameter dbParam = new DBParameter();  // default=SQLite
            dbParam.DBPath = this.CorpusName;
            dbParam.Name = Path.GetFileNameWithoutExtension(this.CorpusName);

            // DB初期化のためのサービスを得る
            m_Service = DBService.Create(dbParam);
            m_DefaultString = m_Service.GetDefault(); // INSERT文のDEFAULT文字列
            try
            {
                m_Service.DropDatabase();
                m_Service.CreateDatabase();
            }
            catch (Exception ex)
            {
                Console.Write("Info: ");
                Console.WriteLine(ex.Message);
                // Continue.
            }
            try
            {
                m_Service.DropDictionaryTables();
            }
            catch (Exception ex)
            {
                PrintException(ex);
                // continue
            }
            try
            {
                m_Service.CreateDictionaryTables();
                m_Service.CreateDictionaryIndices();
            }
            catch (Exception ex)
            {
                PrintException(ex);
                // maybe some trouble in creating indices; continue anyway...
            }

            // 辞書ファイル読込み
            m_LexBuilder = ParseLexicon(this.InputPath);
            if (m_LexBuilder == null)
            {
                return false;
            }
            // 原始LexiconをCorpusのLexiconへコピー
            m_LexBuilder.CopyToCorpusLexicon(m_Corpus.Lex);

            return true;
        }

        public bool ReadFilesRecursively(string path, Func<string, bool> executor)
        {
            if (!PathIsFolder(path))
            {
                return executor(path);
            }
            else
            {
                string[] files = Directory.GetFileSystemEntries(path);
                foreach (string s in files)
                {
                    if (!ReadFilesRecursively(s, executor)) return false;
                }
            }
            return true;
        }

        public static bool PathIsFolder(string path)
        {
            try
            {
                Directory.GetFiles(path);
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }

        public bool CheckExtension(string path)
        {
            string ext = Path.GetExtension(path).ToUpper();
            if (this.ReaderType == "Auto")
            {
                if (ext == ".CABOCHA" || ext == ".CHASEN" || ext == ".MECAB" || ext == ".TXT" || ext == ".CONLL") return true;
            }
            else if (this.ReaderType.StartsWith("ChaSen"))
            {
                if (ext == ".CABOCHA" || ext == ".CHASEN") return true;
            }
            else if (this.ReaderType.StartsWith("Mecab"))
            {
                if (ext == ".CABOCHA" || ext == ".MECAB") return true;
            }
            else if (this.ReaderType.StartsWith("CONLL"))
            {
                if (ext == ".CONLL" || ext == ".TXT") return true;
            }
            else if (this.ReaderType.StartsWith("English"))
            {
                if (ext == ".TXT") return true;
            }
            else if (this.ReaderType.StartsWith("PlainText"))
            {
                if (ext == ".TXT") return true;
            }
            return false;
        }

        private bool ReadSingleInput(string path)
        {
            if (m_IsFolderInput && !CheckExtension(path))  // ユーザーが明示的に単一ファイル指定した場合は、拡張子をチェックしない
            {
                Console.WriteLine("File Extension Check - Input is ignored.");
                return true; // just ignore
            }
            if (ForceCheckInputExtension && !CheckExtension(path))  // 自動的に入力フォルダからファイルを抽出した場合は、拡張子をチェックしない
            {
                Console.WriteLine("File Extension Check - Input is ignored.");
                return false;  // do not process forward for this file.
            }
            Console.WriteLine("Reading {0}", path);
            try
            {
                CorpusSourceReader reader =
                    CorpusSourceReaderFactory.Instance.Create(path, ref this.ReaderType, this.TextEncoding, m_Corpus, m_LexBuilder);
                Console.WriteLine("Using {0}({1})", reader.GetType().Name, this.ReaderType);

                using (TextReader istr = new StreamReader(path, Encoding.GetEncoding(reader.EncodingToUse)))
                {
                    if (m_SentencesInDocuments.Count > 0)
                    {  // 複数ファイル入力のときはここには来ない。
                        for (int i = 0; i < m_SentencesInDocuments.Count; i++)
                        {
                            Console.WriteLine("[Document {0:D4}] {1}", i + 1, m_DocumentSet.Documents[i].FileName);
                            reader.ReadFromStreamSLA(istr, m_SentencesInDocuments[i], m_DocumentSet.Documents[i]);
                        }
                    }
                    else
                    {
                        Document doc = new Document();
                        doc.ID = m_DocumentSet.GetUnusedDocumentID();
                        m_DocumentSet.AddDocument(doc);
                        doc.FileName = path;
                        Console.WriteLine("[Document] {0}", doc.FileName);
                        reader.ReadFromStreamSLA(istr, -1, doc);
                        // DocumentのFilePath属性に相対パスを格納する.
                        doc.Attributes.Add(new DocumentAttribute()
                        {
                            ID = DocumentAttribute.UniqueID++,
                            Key = "FilePath",
                            Value = path
                        });
                        if (m_IsFolderInput)
                        {
                            // フォルダ指定の場合はBib_ID属性に拡張子を除いたファイル名を格納する.
                            doc.Attributes.Add(new DocumentAttribute()
                            {
                                ID = DocumentAttribute.UniqueID++,
                                Key = "Bib_ID",
                                Value = Path.GetFileNameWithoutExtension(path)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            return true;
        }

        public LexiconBuilder ParseLexicon(string path)
        {
            Console.WriteLine("Reading Dictionary: {0}", path);
            CorpusSourceReader reader = null;
            LexiconBuilder lb = new LexiconBuilder();
            try
            {
                string readerTypelex = this.ReaderType;

                CorpusSourceReaderFactory factory = CorpusSourceReaderFactory.Instance;
                reader = factory.Create(this.InputPath, ref readerTypelex, this.TextEncoding, m_Corpus, lb);
                if (reader == null)
                {
                    Console.WriteLine("Invalid Reader Type: {0}", this.ReaderType);
                    return null;
                }
                Console.WriteLine("Using {0}({1}) for the Dictionary.", reader.GetType().Name, this.ReaderType);

                using (TextReader rdr = new StreamReader(path, Encoding.GetEncoding(reader.EncodingToUse)))
                {
                    reader.ReadLexiconFromStream(rdr, true);
                }
                using (TextReader rdr = new StreamReader(path, Encoding.GetEncoding(reader.EncodingToUse)))
                {
                    reader.ReadLexiconFromStream(rdr);
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return null;
            }
            return lb;
        }

        public bool ParseBibFile()
        {
            if (this.BibSource == null)
            {
                return true;
            }
            Console.WriteLine("Reading Bib File{0}...", this.BibSource);
            // Check encoding
            var encodingToUse = this.TextEncoding;
            if (encodingToUse == "Auto")
            {
                encodingToUse = Utility.GuessTextFileEncoding(this.BibSource);
                if (encodingToUse == null)
                {
                    Console.WriteLine(" Could not determine encoding for {0}. Using default utf-8.", this.BibSource);
                    encodingToUse = "utf-8";
                }
            }
            using (TextReader streamReader = new StreamReader(this.BibSource, Encoding.GetEncoding(encodingToUse)))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line.Length == 0) continue;
                    int sentencesInDoc = 0;
                    Document newdoc = null;
                    try
                    {
                        newdoc = BibParser.Parse(line, out sentencesInDoc);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    if (newdoc != null)
                    {
                        m_DocumentSet.AddDocument(newdoc);
                        m_SentencesInDocuments.Add(sentencesInDoc);
                    }
                }
            }
            Console.WriteLine("  {0} Documents found.", m_DocumentSet.Documents.Count);
            return true;
        }

        public bool SaveProject()
        {
            Console.Write("Saving Project...");
            try
            {
                using (DbConnection conn = m_Service.GetDbConnnection())
                {
                    conn.Open();
                    DbCommand cmd = conn.CreateCommand();
                    DbTransaction trans = conn.BeginTransaction();
                    cmd.Connection = conn;
                    cmd.Transaction = trans;

                    cmd.CommandText = string.Format("INSERT INTO corpus_attribute VALUES({0}, '{1}','{2}')",
                        0, CorpusSchema.KeyName, CorpusSchema.CurrentVersion);
                    cmd.ExecuteNonQuery();

                    m_Corpus.Lex.UniqueID = Guid.NewGuid().ToString();
                    cmd.CommandText = string.Format("INSERT INTO corpus_attribute VALUES({0}, 'LexiconID','{1}')",
                        1, m_Corpus.Lex.UniqueID);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = string.Format("INSERT INTO corpus_attribute VALUES({0}, 'Namespaces','{1}')",
                        2, m_Corpus.NamespacesToString());
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = string.Format("INSERT INTO project VALUES({0},null,{1},{2})",
                        m_Project.ID, 0, 0);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = string.Format("INSERT INTO user VALUES({0},'{1}','{2}',0,null)",
                        m_User.ID, m_User.Name, m_User.Password);
                    cmd.ExecuteNonQuery();

                    foreach (User user in m_Project.Users)
                    {
                        cmd.CommandText = string.Format("INSERT INTO project_user VALUES({0},{1},0)",
                            user.ID, m_Project.ID);
                        cmd.ExecuteNonQuery();
                    }

                    int n = 0;
                    foreach (TagSet ts in m_Project.TagSetList)
                    {
                        ts.ID = n;
                        cmd.CommandText = string.Format("INSERT INTO tagset VALUES({0},'{1}')",
                            ts.ID, ts.Name);
                        cmd.ExecuteNonQuery();

                        int m = 0;
                        foreach (Tag t in ts.Tags)
                        {
                            t.ID = m;
                            t.Version = ts.CurrentVersion;
                            cmd.CommandText = string.Format("INSERT INTO tag_definition VALUES({0},{1},'{2}','{3}','{4}',{5})",
                                t.ID, ts.ID, t.Type, t.Name, t.Description, t.Version.ID);
                            cmd.ExecuteNonQuery();
                            m++;
                        }

                        cmd.CommandText = string.Format("INSERT INTO project_tagset VALUES({0},{1},0)",
                            m_Project.ID, ts.ID);
                        cmd.ExecuteNonQuery();
                        n++;

                        foreach (TagSetVersion tsv in ts.Versions)
                        {
                            cmd.CommandText = string.Format("INSERT INTO tagset_version VALUES({0},{1},'{2}',{3},{4})",
                                tsv.ID, ts.ID, tsv.Version, tsv.Revision, m_Service.GetBooleanString(tsv.IsCurrent));
                            cmd.ExecuteNonQuery();
                        }

                        // 各Userに対するこのTagSetに関するPrivilege: すべての組み合わせに対してDefault 0をセット.
                        foreach (User user in m_Project.Users)
                        {
                            cmd.CommandText = string.Format("INSERT INTO tagset_user VALUES({0},{1},0)",
                                ts.ID, user.ID);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    cmd.CommandText = string.Format("INSERT INTO document_set VALUES({0},'{1}',null)",
                        m_DocumentSet.ID, m_DocumentSet.Name);
                    cmd.ExecuteNonQuery();
                    n = 0;
                    int tagid = 0;
                    foreach (Document doc in m_DocumentSet.Documents)
                    {
                        doc.ID = n;
                        doc.Order = n;
                        cmd.CommandText = string.Format("INSERT INTO document VALUES({0},{1},'{2}','{3}')",
                            doc.ID, doc.Order, doc.FileName, EscapeQuote(doc.Text));
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = string.Format("INSERT INTO document_set_document VALUES({0},{1})",
                            doc.ID, m_DocumentSet.ID);
                        cmd.ExecuteNonQuery();
                        foreach (DocumentAttribute attr in doc.Attributes)
                        {
                            attr.ID = tagid;
                            cmd.CommandText = string.Format("INSERT INTO documenttag VALUES({0},'{1}','{2}',{3},{4})",
                                attr.ID, attr.Key, EscapeQuote(attr.Value), doc.ID, m_DefaultString);
                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch
                            {
                                Console.WriteLine("\nInvalid data: query={0}", cmd.CommandText); ;
                            }
                            tagid++;
                        }
                        n++;
                    }

                    cmd.CommandText = string.Format("INSERT INTO document_set_project VALUES({0},{1},{2})",
                        m_DocumentSetProjectMapping.ID, m_DocumentSetProjectMapping.DocumentSet.ID, m_DocumentSetProjectMapping.Proj.ID);
                    cmd.ExecuteNonQuery();
                    foreach (User user in m_Project.Users)
                    {
                        cmd.CommandText = string.Format("INSERT INTO document_set_project_user VALUES({0},{1},0)",
                            m_DocumentSetProjectMapping.ID, user.ID);
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = string.Format("INSERT INTO document_set_user VALUES({0},{1},0)",
                            user.ID, m_DocumentSet.ID);
                        cmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            Console.WriteLine("written.");
            return true;
        }

        public bool AddProject()
        {
            Console.Write("Saving Project...");
            try
            {
                using (DbConnection conn = m_Service.GetDbConnnection())
                {
                    conn.Open();
                    DbCommand cmd = conn.CreateCommand();
                    DbTransaction trans = conn.BeginTransaction();
                    cmd.Connection = conn;
                    cmd.Transaction = trans;

                    cmd.CommandText = string.Format("INSERT INTO project VALUES({0},null,{1},{2})",
                        m_Project.ID, 0, 0);
                    cmd.ExecuteNonQuery();

                    foreach (User user in m_Project.Users)
                    {
                        cmd.CommandText = string.Format("INSERT INTO project_user VALUES({0},{1},0)",
                            user.ID, m_Project.ID);
                        cmd.ExecuteNonQuery();
                    }

                    int n = 0;
                    foreach (TagSet ts in m_Project.TagSetList)
                    {
                        ts.ID = n;
                        cmd.CommandText = string.Format("INSERT INTO project_tagset VALUES({0},{1},0)",
                            m_Project.ID, ts.ID);
                        cmd.ExecuteNonQuery();
                        n++;
                    }

                    cmd.CommandText = string.Format("INSERT INTO document_set_project VALUES({0},{1},{2})",
                        m_DocumentSetProjectMapping.ID, m_DocumentSetProjectMapping.DocumentSet.ID, m_DocumentSetProjectMapping.Proj.ID);
                    cmd.ExecuteNonQuery();
                    foreach (User user in m_Project.Users)
                    {
                        cmd.CommandText = string.Format("INSERT INTO document_set_project_user VALUES({0},{1},0)",
                            m_DocumentSetProjectMapping.ID, user.ID);
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = string.Format("INSERT INTO document_set_user VALUES({0},{1},0)",
                            user.ID, m_DocumentSet.ID);
                        cmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            Console.WriteLine("written.");
            return true;
        }

        public bool SaveLexicon()
        {
            // Native DB Connection
            Console.WriteLine("\n\nSaving to database {0}", this.CorpusName);
            try
            {
                using (DbConnection conn = m_Service.GetDbConnnection())
                {
                    int n;
                    conn.Open();
                    DbCommand cmd = conn.CreateCommand();
                    DbTransaction trans = conn.BeginTransaction();
                    cmd.Connection = conn;
                    cmd.Transaction = trans;

                    Console.WriteLine("Saving Lexicon...");
                    Console.WriteLine("Saving PartsOfSpeech...");
                    n = 0;
                    foreach (PartOfSpeech pos in m_Corpus.Lex.PartsOfSpeech)
                    {
                        if (pos.ID < 0)
                        {
                            pos.ID = ++m_LexBuilder.LastLexemeID;
                        }
                        cmd.CommandText = string.Format("INSERT INTO part_of_speech VALUES({0},'{1}','{2}','{3}','{4}','{5}')",
                            pos.ID, EscapeQuote(pos.Name1), EscapeQuote(pos.Name2), EscapeQuote(pos.Name3), EscapeQuote(pos.Name4), EscapeQuote(pos.Name));
                        cmd.ExecuteNonQuery();
                        Console.Write("> {0}\r", ++n);
                    }
                    Console.WriteLine("\x0a Saving CTypes...");
                    n = 0;
                    foreach (CType ctype in m_Corpus.Lex.CTypes)
                    {
                        if (ctype.ID < 0)
                        {
                            ctype.ID = ++m_LexBuilder.LastCTypeID;
                        }
                        cmd.CommandText = string.Format("INSERT INTO ctype VALUES({0},'{1}','{2}','{3}')",
                            ctype.ID, EscapeQuote(ctype.Name1), EscapeQuote(ctype.Name2), EscapeQuote(ctype.Name));
                        cmd.ExecuteNonQuery();
                        Console.Write("> {0}\r", ++n);
                    }
                    Console.WriteLine("\x0a Saving CForms...");
                    n = 0;
                    foreach (CForm cform in m_Corpus.Lex.CForms)
                    {
                        if (cform.ID < 0)
                        {
                            cform.ID = ++m_LexBuilder.LastCFormID;
                        }
                        cmd.CommandText = string.Format("INSERT INTO cform VALUES({0},'{1}')",
                            cform.ID, EscapeQuote(cform.Name));
                        cmd.ExecuteNonQuery();
                        Console.Write("> {0}\r", ++n);
                    }
                    Console.WriteLine("\x0a Saving Lexemes...");
                    n = 0;
                    foreach (Lexeme lex in m_Corpus.Lex)
                    {
                        if (lex.ID < 0)
                        {
                            lex.ID = ++m_LexBuilder.LastLexemeID;   // Lexiconは後方参照もあるので、先に全部にIDを振っておく
                        }
                    }
                    n = 0;
                    foreach (Lexeme lex in m_Corpus.Lex)
                    {
                        if (lex.BaseLexeme.ID == -1)
                        {
                            Console.WriteLine("Invalid BaseLexeme reference: {0}", lex);
                            continue;
                        }
                        cmd.CommandText = string.Format("INSERT INTO lexeme VALUES ({0},'{1}','{2}','{3}','{4}',{5},'{6}',{7},{8},{9},{10},{11},'{12}')",
                            lex.ID,
                            EscapeQuote(lex.Surface).Replace("\\", ""),
                            EscapeQuote(lex.Reading),
                            EscapeQuote(lex.LemmaForm),
                            EscapeQuote(lex.Pronunciation),
                            lex.BaseLexeme.ID,
                            EscapeQuote(lex.Lemma),
                            lex.PartOfSpeech.ID, lex.CType.ID, lex.CForm.ID, m_DefaultString, lex.Frequency,
                            EscapeQuote(lex.CustomProperty));
                        cmd.ExecuteNonQuery();
                        if (n % 500 == 0)
                        {
                            Console.Write("> {0}\r", n + 1);
                        }
                        n++;
                    }
                    Console.WriteLine("> {0}", n);
                    trans.Commit();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            Console.WriteLine("\x0aLexicon written.");
            return true;
        }

        public bool SaveSentenceTags()
        {
            Console.WriteLine("\nSaving SentenceTags ({0})...", m_Corpus.Sentences.Count);
            try
            {
                using (DbConnection conn = m_Service.GetDbConnnection())
                {
                    conn.Open();
                    DbCommand cmd = conn.CreateCommand();
                    DbTransaction trans = conn.BeginTransaction();
                    cmd.Connection = conn;
                    cmd.Transaction = trans;

                    foreach (Sentence sen in m_Corpus.Sentences)
                    {
                        Iesi.Collections.Generic.ISet<SentenceAttribute> list = sen.Attributes;
                        foreach (SentenceAttribute attr in list)
                        {
                            cmd.CommandText = string.Format("INSERT INTO sentence_documenttag VALUES({0},{1})",
                                sen.ID, attr.ID);
                            cmd.ExecuteNonQuery();
                        }
                        Console.Write("> {0}\r", sen.ID + 1);
                    }
                    trans.Commit();
                    cmd.Dispose();
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            return true;
        }

        public bool SaveSentences()
        {
            // Sentenceをセーブする
            Console.WriteLine("\nSaving Sentences...");
            SimpleTransactionManager trans = null;
            DbCommand cmd = null;

            // Segment, LinkのIDをSentence,Wordのセーブ前に付け直す
            int attrid = 0;
            int segid = 0;
            foreach (Segment seg in m_Corpus.Segments)
            {
                seg.ID = segid++;
                // 元々ここでattridをseg.Attributesのそれぞれにforeachでアサインしていたが、
                // iesiのISet(.NET 2.0)と.NET 4.0の最適化の相性の問題か、しばらく進んだところで（不定）
                // ブレークの利かないアプリケーションハングを起こしてしまう。
                // そのため、後ろのDB Saveループ内に移した。
            }
            int linkid = 0;
            foreach (Link lnk in m_Corpus.Links)
            {
                lnk.ID = linkid++;
            }
            int groupid = 0;
            foreach (Group grp in m_Corpus.Groups)
            {
                grp.ID = groupid++;
            }

            try
            {
                trans = new SimpleTransactionManager(m_Service);
                trans.Begin();
                cmd = trans.Cmd;

                int senid = 0;
                int wordid = 0;
                foreach (Sentence sen in m_Corpus.Sentences)
                {
                    sen.ID = senid++;
                    cmd.CommandText = string.Format("INSERT INTO sentence VALUES({0},{1},{2},{3},{4})",
                            sen.ID, sen.StartChar, sen.EndChar, sen.ParentDoc.ID, sen.Pos);
                    cmd.ExecuteNonQuery();
                    foreach (Word word in sen.Words)
                    {
                        word.ID = wordid++;
                        cmd.CommandText = string.Format("INSERT INTO word VALUES ({0},{1},{2},{3},{4},{5},{6},{7},null,null,null,'{8}',{9},{10})",
                            word.ID, word.Sen.ID, word.StartChar, word.EndChar, word.Lex.ID,
                            (word.Bunsetsu == null) ? 0 : word.Bunsetsu.ID, m_DefaultString, word.Pos,
                            word.Extras,
                            m_Project.ID,
                            (int)word.HeadInfo);
                        cmd.ExecuteNonQuery();
                    }
                    if (senid > 0 && senid % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", sen.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Sentences.Count);
                trans.CommitAndContinue();
                cmd = trans.Cmd;

                // Segment, Linkをセーブする
                Console.WriteLine("\nSaving Segments...");
                foreach (Segment seg in m_Corpus.Segments)
                {
                    cmd.CommandText = string.Format("INSERT INTO segment VALUES({0},{1},{2},{3},{4},{5},'',{6},{7},{8},'{9}',{10},'')",
                       seg.ID, seg.Tag.ID, seg.Version.ID, seg.Doc.ID, seg.StartChar, seg.EndChar,
                       m_Project.ID, 0/*user_id*/,
                       m_Service.GetDefault(), EscapeQuote(seg.Comment), seg.Sentence.ID);
                    cmd.ExecuteNonQuery();
                    // Attributes
                    foreach (SegmentAttribute attr in seg.Attributes)
                    {
                        cmd.CommandText = string.Format("INSERT INTO segment_attribute VALUES({0},{1},'{2}','{3}',{4},{5},{6},{7},'{8}')",
                            attrid++, seg.ID, EscapeQuote(attr.Key), EscapeQuote(attr.Value),
                            seg.Version.ID, m_Project.ID, 0/*user_id*/,
                            m_Service.GetDefault(), EscapeQuote(attr.Comment));
                        cmd.ExecuteNonQuery();
                    }

                    if (seg.ID > 0 && seg.ID % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", seg.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Segments.Count);
                trans.CommitAndContinue();
                cmd = trans.Cmd;

                Console.WriteLine("\nSaving Links...");
                foreach (Link lnk in m_Corpus.Links)
                {
                    cmd.CommandText = string.Format("INSERT INTO link VALUES({0},{1},{2},{3},{4},{5},{6},{7},'{8}',{9},{10},'')",
                        lnk.ID, lnk.Tag.ID, lnk.Version.ID, lnk.From.ID, lnk.To.ID,
                        m_Project.ID, 0/*user_id*/, m_Service.GetDefault(), EscapeQuote(lnk.Comment),
                        lnk.FromSentence.ID, lnk.ToSentence.ID);
                    cmd.ExecuteNonQuery();

                    // Attributes
                    foreach (LinkAttribute attr in lnk.Attributes)
                    {
                        cmd.CommandText = string.Format("INSERT INTO link_attribute VALUES({0},{1},'{2}','{3}',{4},{5},{6},{7},'{8}')",
                            attrid++, lnk.ID, EscapeQuote(attr.Key), EscapeQuote(attr.Value),
                            lnk.Version.ID, m_Project.ID, 0/*user_id*/,
                            m_Service.GetDefault(), EscapeQuote(attr.Comment));
                        cmd.ExecuteNonQuery();
                    }

                    if (lnk.ID > 0 && lnk.ID % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", lnk.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Links.Count);
                trans.CommitAndContinue();
                cmd = trans.Cmd;

                Console.WriteLine("\nSaving Groups...");
                foreach (Group grp in m_Corpus.Groups)
                {
                    cmd.CommandText = string.Format("INSERT INTO group_element VALUES({0},{1},{2},{3},{4},{5},'{6}')",
                        grp.ID, grp.Tag.ID, grp.Version.ID, m_Project.ID, 0/*user_id*/, m_Service.GetDefault(), EscapeQuote(grp.Comment));
                    cmd.ExecuteNonQuery();
                    foreach (Annotation ann in grp.Tags)
                    {
                        if (!(ann is Segment)) continue;
                        Segment seg = ann as Segment;
                        cmd.CommandText = string.Format("INSERT INTO group_member VALUES({0},'{1}',{2},{3},{4},{5})",
                            grp.ID, Tag.SEGMENT, seg.ID, m_Project.ID, 0/*grp.User.ID*/, m_Service.GetDefault());
                        cmd.ExecuteNonQuery();
                    }
                    if (grp.ID > 0 && grp.ID % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", grp.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Segments.Count);
                trans.Commit();
                Console.Write("\n");
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            finally
            {
                if (trans != null)
                {
                    trans.Dispose();
                }
            }
            return true;
        }

        /// <summary>
        /// 既存のコーパスに対し、新たなProjectを導入、WordとAnnotationを追加する.
        /// 既存のSentence, Wordはそのまま. Sentence別Projectに属するWordが追加される.
        /// Projectは新規であること（そのProjectに属する要素が呼び出し時点でコーパスにないこと）を前提とする.
        /// </summary>
        /// <returns></returns>
        public bool UpdateSentences()
        {
            Console.WriteLine("\nSaving Sentences...");
            SimpleTransactionManager trans = null;
            DbCommand cmd = null;

            int wordid = 0;

            // Segment, LinkのIDをSentence,Wordのセーブ前に付け直す
            long segid = 0;
            long linkid = 0;
            long groupid = 0;
            long attrid = 0;
            // DBに既にSegment, Linkがある場合、その最大IDの次から始める.(Project追加の場合)
            var cfg = m_Service.GetConnection();
            using (var factory = cfg.BuildSessionFactory())
            using (var session = factory.OpenSession())
            {
                segid = session.CreateQuery("select max(ID) from Segment").UniqueResult<long>() + 1;
                linkid = session.CreateQuery("select max(ID) from Link").UniqueResult<long>() + 1;
                groupid = session.CreateQuery("select max(ID) from Group").UniqueResult<long>() + 1;
                // attridは、過去の互換性のため、seg, link, groupで通し番号とし、その最大の次から付加する.
                attrid = Math.Max(attrid, session.CreateQuery("select max(ID) from SegmentAttribute").UniqueResult<long>() + 1);
                attrid = Math.Max(attrid, session.CreateQuery("select max(ID) from LinkAttribute").UniqueResult<long>() + 1);
                attrid = Math.Max(attrid, session.CreateQuery("select max(ID) from GroupAttribute").UniqueResult<long>() + 1);
                // word idも一緒に求めて置く
                wordid = session.CreateQuery("select max(ID) from Word").UniqueResult<int>() + 1;
            }
            foreach (Segment seg in m_Corpus.Segments)
            {
                seg.ID = segid++;
                foreach (var attr in seg.Attributes)
                {
                    attr.ID = attrid++;
                }
            }
            foreach (Link lnk in m_Corpus.Links)
            {
                lnk.ID = linkid++;
                foreach (var attr in lnk.Attributes)
                {
                    attr.ID = attrid++;
                }
            }
            foreach (Group grp in m_Corpus.Groups)
            {
                grp.ID = groupid++;
                foreach (var attr in grp.Attributes)
                {
                    attr.ID = attrid++;
                }
            }

            try
            {
                trans = new SimpleTransactionManager(m_Service);
                trans.Begin();
                cmd = trans.Cmd;

                int n = 0;
                foreach (Sentence sen in m_Corpus.Sentences)
                {
                    n++;
                    foreach (Word word in sen.Words)
                    {
                        word.ID = wordid++;
                        cmd.CommandText = string.Format("INSERT INTO word VALUES ({0},{1},{2},{3},{4},{5},{6},{7},null,null,null,'{8}',{9},{10})",
                            word.ID, word.Sen.ID, word.StartChar, word.EndChar, word.Lex.ID,
                            (word.Bunsetsu == null) ? 0 : word.Bunsetsu.ID, m_DefaultString, word.Pos,
                            word.Extras,
                            m_Project.ID,
                            (int)word.HeadInfo);
                        cmd.ExecuteNonQuery();
                    }
                    if (n > 0 && n % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", sen.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Sentences.Count);
                trans.CommitAndContinue();
                cmd = trans.Cmd;

                // Segment, Linkをセーブする
                Console.WriteLine("\nSaving Segments...");
                foreach (Segment seg in m_Corpus.Segments)
                {
                    cmd.CommandText = string.Format("INSERT INTO segment VALUES({0},{1},{2},{3},{4},{5},'',{6},{7},{8},'{9}',{10},'')",
                       seg.ID, seg.Tag.ID, seg.Version.ID, seg.Doc.ID, seg.StartChar, seg.EndChar,
                       m_Project.ID, 0/*user_id*/,
                       m_Service.GetDefault(), EscapeQuote(seg.Comment), seg.Sentence.ID);
                    cmd.ExecuteNonQuery();
                    // Attributes
                    foreach (SegmentAttribute attr in seg.Attributes)
                    {
                        cmd.CommandText = string.Format("INSERT INTO segment_attribute VALUES({0},{1},'{2}','{3}',{4},{5},{6},{7},'{8}')",
                            attr.ID, seg.ID, EscapeQuote(attr.Key), EscapeQuote(attr.Value),
                            seg.Version.ID, m_Project.ID, 0/*user_id*/,
                            m_Service.GetDefault(), EscapeQuote(attr.Comment));
                        cmd.ExecuteNonQuery();
                    }

                    if (seg.ID > 0 && seg.ID % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", seg.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Segments.Count);
                trans.CommitAndContinue();
                cmd = trans.Cmd;

                Console.WriteLine("\nSaving Links...");
                foreach (Link lnk in m_Corpus.Links)
                {
                    cmd.CommandText = string.Format("INSERT INTO link VALUES({0},{1},{2},{3},{4},{5},{6},{7},'{8}',{9},{10},'')",
                        lnk.ID, lnk.Tag.ID, lnk.Version.ID, lnk.From.ID, lnk.To.ID,
                        m_Project.ID, 0/*user_id*/, m_Service.GetDefault(), EscapeQuote(lnk.Comment),
                        lnk.FromSentence.ID, lnk.ToSentence.ID);
                    cmd.ExecuteNonQuery();

                    // Attributes
                    foreach (LinkAttribute attr in lnk.Attributes)
                    {
                        cmd.CommandText = string.Format("INSERT INTO link_attribute VALUES({0},{1},'{2}','{3}',{4},{5},{6},{7},'{8}')",
                            attr.ID, lnk.ID, EscapeQuote(attr.Key), EscapeQuote(attr.Value),
                            lnk.Version.ID, m_Project.ID, 0/*user_id*/,
                            m_Service.GetDefault(), EscapeQuote(attr.Comment));
                        cmd.ExecuteNonQuery();
                    }

                    if (lnk.ID > 0 && lnk.ID % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", lnk.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Links.Count);
                trans.CommitAndContinue();
                cmd = trans.Cmd;

                Console.WriteLine("\nSaving Groups...");
                foreach (Group grp in m_Corpus.Groups)
                {
                    cmd.CommandText = string.Format("INSERT INTO group_element VALUES({0},{1},{2},{3},{4},{5},'{6}')",
                        grp.ID, grp.Tag.ID, grp.Version.ID, m_Project.ID, 0/*user_id*/, m_Service.GetDefault(), EscapeQuote(grp.Comment));
                    cmd.ExecuteNonQuery();
                    foreach (Annotation ann in grp.Tags)
                    {
                        if (!(ann is Segment)) continue;
                        Segment seg = ann as Segment;
                        cmd.CommandText = string.Format("INSERT INTO group_member VALUES({0},'{1}',{2},{3},{4},{5})",
                            grp.ID, Tag.SEGMENT, seg.ID, m_Project.ID, 0/*grp.User.ID*/, m_Service.GetDefault());
                        cmd.ExecuteNonQuery();
                    }
                    if (grp.ID > 0 && grp.ID % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", grp.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Segments.Count);
                trans.Commit();
                Console.Write("\n");
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            finally
            {
                if (trans != null)
                {
                    trans.Dispose();
                }
            }
            return true;
        }

        public bool UpdateIndex()
        {
            Console.WriteLine("\nUpdating Index...");
            try
            {
                m_Service.CreateFinalIndices();
            }
            catch (Exception ex)
            {
                PrintException(ex);
                // maybe some trouble in creating indices; continue anyway...
            }
            Console.WriteLine();
            return true;
        }

        public static void PrintException(Exception ex)
        {
            Console.WriteLine("Exception: {0}", ex.ToString());
        }

        private string EscapeQuote(string s)
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
    }
}
