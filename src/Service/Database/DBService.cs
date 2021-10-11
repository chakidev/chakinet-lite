using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using ChaKi.Service.Properties;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Search;
using NHibernate;
using System.Collections;
using ChaKi.Entity;
using System.Data;
using System.ComponentModel;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Common;


namespace ChaKi.Service.Database
{
    public abstract class DBService
    {
        public DBParameter DBParam { get; set; }

        /// <summary>
        /// Unlockしてよいかどうかを確認するためのcallback.
        /// CorpusをLockしているサービスがある場合はtrueとなる
        /// </summary>
        private static Dictionary<string, UnlockRequestCallback> RegisteredUnlockRequestFunc;

        static DBService()
        {
            RegisteredUnlockRequestFunc = new Dictionary<string, UnlockRequestCallback>();
        }

        static void ReplaceUnlockCabllcak(string corpusName, UnlockRequestCallback callback, Type requestingService)
        {
            UnlockRequestCallback requestRelease;
            if (RegisteredUnlockRequestFunc.TryGetValue(corpusName, out requestRelease))
            {
                if (!requestRelease(requestingService))
                {
                    // callbackにより開放が拒否された
                    throw new Exception("Corpus is locked");
                }
                RegisteredUnlockRequestFunc.Remove(corpusName);
            }
            if (callback != null)
            {
                RegisteredUnlockRequestFunc[corpusName] = callback;
            }

        }

        /// <summary>
        /// DBServiceをパラメータから作成するシンプルファクトリ
        /// </summary>
        /// <param name="dbms"></param>
        /// <returns></returns>
        static public DBService Create(DBParameter dbparam)
        {
            DBService svc = null;
            if (dbparam.DBType.Equals("MySQL"))
            {
                svc = new MySQLDBService();
            }
            else if (dbparam.DBType.Equals("SQLServer"))
            {
                svc = new MSSQLDBService();
            }
            else if (dbparam.DBType.Equals("SQLite"))
            {
                svc = new SQLiteDBService();
            }
            else if (dbparam.DBType.Equals("PostgreSQL"))
            {
                svc = new PgSQLDBService();
            }
            else
            {
                svc = new MySQLDBService();    // Default server
            }
            svc.DBParam = dbparam;
            return svc;
        }
            
        /// <summary>
        /// DBロック解除判定機能付きのCreate
        /// </summary>
        /// <param name="dbparam"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        static public DBService Create(DBParameter dbparam, UnlockRequestCallback callback, Type requestingService)
        {
            ReplaceUnlockCabllcak(dbparam.Name, callback, requestingService);
            return Create(dbparam);
        }

        /// <summary>
        /// DBロックを強制解除する
        /// </summary>
        public void Unlock()
        {
            RegisteredUnlockRequestFunc.Remove(DBParam.Name);
        }

        protected DBService()
        {
        }

        /// <summary>
        /// DBに接続して、データベースの一覧を取得する
        /// </summary>
        /// <param name="server"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="dblist"></param>
        public abstract void GetDatabaseList(ref List<string> dblist);

        /// <summary>
        /// NHibernate用の設定を得る
        /// </summary>
        /// <returns></returns>
        public NHibernate.Cfg.Configuration GetConnection()
        {
            NHibernate.Cfg.Configuration cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            SetupConnection(cfg);
            return cfg;
        }

        /// <summary>
        /// Sessionを開始する
        /// </summary>
        /// <returns></returns>
        public ISession OpenSession()
        {
            NHibernate.Cfg.Configuration cfg = GetConnection();
            ISessionFactory factory = cfg.BuildSessionFactory();
            return factory.OpenSession();
        }

        /// <summary>
        /// NHibernateの接続設定を行う
        /// </summary>
        /// <param name="cfg"></param>
        public abstract void SetupConnection(NHibernate.Cfg.Configuration cfg);

        /// <summary>
        /// ADO.NETの接続文字列を得る
        /// </summary>
        /// <returns></returns>
        public abstract string GetConnectionString();

        /// <summary>
        /// Insert文のデフォルト文字列を得る
        /// </summary>
        /// <returns></returns>
        public virtual string GetDefault()
        {
            return "DEFAULT";
        }

        /// <summary>
        /// コーパス名に基づきデータベースを作成する
        /// </summary>
        public abstract void CreateDatabase();

        /// <summary>
        /// データベースを削除する
        /// </summary>
        public abstract void DropDatabase();

        /// <summary>
        /// データベースが既に存在するかどうかを調べる.
        /// </summary>
        /// <returns></returns>
        public abstract bool DatabaseExists();

        /// <summary>
        /// コーパスを構成するテーブルを全削除する
        /// </summary>
        public virtual void DropAllTables()
        {
            List<string> statements = new List<string>();
            statements.Add(Resources.DropTableCorpusAttributeStatement);
            statements.Add(Resources.DropTablePartOfSpeechStatement);
            statements.Add(Resources.DropTableCTypeStatement);
            statements.Add(Resources.DropTableCFormStatement);
            statements.Add(Resources.DropTableCFormCTypeStatement);
            statements.Add(Resources.DropTableLexemeStatement);
            statements.Add(Resources.DropTableSentenceStatement);
            statements.Add(Resources.DropTableWordStatement);
            statements.Add(Resources.DropTableBunsetsuStatement);
            statements.Add(Resources.DropTableDocumentTagStatement);
            statements.Add(Resources.DropTableSentenceDocumentTagStatement);

            statements.Add(Resources.DropTableDocumentsetStatement);
            statements.Add(Resources.DropTableDocumentStatement);
            statements.Add(Resources.DropTableProjectTagsetStatement);
            statements.Add(Resources.DropTableProjectUserStatement);
            statements.Add(Resources.DropTableUserStatement);
            statements.Add(Resources.DropTableProjectStatement);
            statements.Add(Resources.DropTableTagsetVersionStatement);
            statements.Add(Resources.DropTableTagsetStatement);
            statements.Add(Resources.DropTableTagDefinitionStatement);
            statements.Add(Resources.DropTableGroupMemberStatement);
            statements.Add(Resources.DropTableGroupStatement);
            statements.Add(Resources.DropTableLinkStatement);
            statements.Add(Resources.DropTableSegmentStatement);

            statements.Add(Resources.DropTableSegmentAttributeStatement);
            statements.Add(Resources.DropTableLinkAttributeStatement);
            statements.Add(Resources.DropTableGroupAttributeStatement);

            statements.Add(Resources.DropTableDocumentSetDocumentStatement);
            statements.Add(Resources.DropTableDocumentSetProjectStatement);
            statements.Add(Resources.DropTableDocumentSetProjectUserStatement);
            statements.Add(Resources.DropTableDocumentSetUserStatement);
            statements.Add(Resources.DropTableTagsetUserStatement);
            statements.Add(Resources.DropTableSegmentConstraintStatement);
            statements.Add(Resources.DropTableLinkConstraintStatement);
            statements.Add(Resources.DropTableGroupConstraintStatement);
            statements.Add(Resources.DropTableAttributeConstraintStatement);
            statements.Add(Resources.DropTableTagAppearanceStatement);

            statements.Add(Resources.DropTableWordWordStatement);

            DoStringCommands(statements);
        }

        /// <summary>
        /// 辞書DBを構成するテーブルを全削除する
        /// </summary>
        public virtual void DropDictionaryTables()
        {
            List<string> statements = new List<string>();
            statements.Add(Resources.DropTablePartOfSpeechStatement);
            statements.Add(Resources.DropTableCTypeStatement);
            statements.Add(Resources.DropTableCFormStatement);
            statements.Add(Resources.DropTableCFormCTypeStatement);
            statements.Add(Resources.DropTableLexemeStatement);

            DoStringCommands(statements);
        }

        /// <summary>
        /// コーパスに必要なテーブルを作成する
        /// </summary>
        public virtual void CreateAllTables()
        {
            List<string> statements = new List<string>();
            statements.Add(Resources.CreateTableCorpusAttributeStatement);
            statements.Add(Resources.CreateTablePartOfSpeechStatement);
            statements.Add(Resources.CreateTableCTypeStatement);
            statements.Add(Resources.CreateTableCFormStatement);
            statements.Add(Resources.CreateTableCFormCTypeStatement);
            statements.Add(Resources.CreateTableLexemeStatement);
            statements.Add(Resources.CreateTableSentenceStatement);
            statements.Add(Resources.CreateTableWordStatement);
            statements.Add(Resources.CreateTableBunsetsuStatement);
            statements.Add(Resources.CreateTableDocumentTagStatement);
            statements.Add(Resources.CreateTableSentenceDocumentTagStatement);

            statements.Add(Resources.CreateTableProjectStatement);
            statements.Add(Resources.CreateTableUserStatement);
            statements.Add(Resources.CreateTableProjectTagsetStatement);
            statements.Add(Resources.CreateTableProjectUserStatement);
            statements.Add(Resources.CreateTableDocumentStatement);
            statements.Add(Resources.CreateTableDocumentsetStatement);
            statements.Add(Resources.CreateTableTagsetVersionStatement);
            statements.Add(Resources.CreateTableTagsetStatement);
            statements.Add(Resources.CreateTableTagDefinitionStatement);
            statements.Add(Resources.CreateTableSegmentStatement);
            statements.Add(Resources.CreateTableLinkStatement);
            statements.Add(Resources.CreateTableGroupStatement);
            statements.Add(Resources.CreateTableGroupMemberStatement);

            statements.Add(Resources.CreateTableSegmentAttributeStatement);
            statements.Add(Resources.CreateTableLinkAttributeStatement);
            statements.Add(Resources.CreateTableGroupAttributeStatement);

            statements.Add(Resources.CreateTableDocumentSetDocumentStatement);
            statements.Add(Resources.CreateTableDocumentSetProjectStatement);
            statements.Add(Resources.CreateTableDocumentSetProjectUserStatement);
            statements.Add(Resources.CreateTableDocumentSetUserStatement);
            statements.Add(Resources.CreateTableTagsetUserStatement);
            statements.Add(Resources.CreateTableSegmentConstraintStatement);
            statements.Add(Resources.CreateTableLinkConstraintStatement);
            statements.Add(Resources.CreateTableGroupConstraintStatement);
            statements.Add(Resources.CreateTableAttributeConstraintStatement);
            statements.Add(Resources.CreateTableTagAppearanceStatement);

            statements.Add(Resources.CreateTableWordWordStatement);

            DoStringCommands(statements);
        }

        /// <summary>
        /// 辞書DBに必要なテーブルを作成する
        /// </summary>
        public virtual void CreateDictionaryTables()
        {
            List<string> statements = new List<string>();
            statements.Add(Resources.CreateTablePartOfSpeechStatement);
            statements.Add(Resources.CreateTableCTypeStatement);
            statements.Add(Resources.CreateTableCFormStatement);
            statements.Add(Resources.CreateTableCFormCTypeStatement);
            statements.Add(Resources.CreateTableLexemeStatement);

            DoStringCommands(statements);
        }

        /// <summary>
        /// テーブルにインデックスを作成する
        /// </summary>
        public virtual void CreateAllIndices()
        {
            List<string> statements = new List<string>();
            statements.Add(Resources.CreateIndexCorpusAttributeStatement);
            statements.Add(Resources.CreateIndexPartOfSpeechStatement);
            statements.Add(Resources.CreateIndexCTypeStatement);
            statements.Add(Resources.CreateIndexCFormStatement);
            statements.Add(Resources.CreateIndexCFormCTypeStatement);
            statements.Add(Resources.CreateIndexLexemeStatement1);
            statements.Add(Resources.CreateIndexLexemeStatement2);
            statements.Add(Resources.CreateIndexLexemeStatement3);
            statements.Add(Resources.CreateIndexLexemeStatement4);
            statements.Add(Resources.CreateIndexLexemeStatement5);
            statements.Add(Resources.CreateIndexLexemeStatement6);
            statements.Add(Resources.CreateIndexLexemeStatement7);
            statements.Add(Resources.CreateIndexSentenceStatement1);
            statements.Add(Resources.CreateIndexSentenceStatement2);
            statements.Add(Resources.CreateIndexBunsetsuStatement);
            statements.Add(Resources.CreateIndexSegmentStatement1);
            statements.Add(Resources.CreateIndexSegmentsStatement2);
            statements.Add(Resources.CreateIndexSegmentStatement3);
            statements.Add(Resources.CreateIndexLinkStatement1);
            statements.Add(Resources.CreateIndexLinkStatement2);
            statements.Add(Resources.CreateIndexLinkStatement3);
            statements.Add(Resources.CreateIndexLinkStatement4);
            statements.Add(Resources.CreateIndexGroupStatement);
            statements.Add(Resources.CreateIndexGroupMemberStatement1);
            statements.Add(Resources.CreateIndexGroupMemberStatement2);
            statements.Add(Resources.CreateIndexSentenceDocumenttagStatement1);
            statements.Add(Resources.CreateIndexWordWordStatement);
            statements.Add(Resources.CreateIndexLinkAttributeLinkStatement);
            statements.Add(Resources.CreateIndexSegmentAttributeSegmentStatement);
            statements.Add(Resources.CreateIndexGroupAttributeGroupStatement);

            DoStringCommands(statements);
        }

        /// <summary>
        /// 辞書テーブルにインデックスを作成する
        /// </summary>
        public virtual void CreateDictionaryIndices()
        {
            List<string> statements = new List<string>();
            statements.Add(Resources.CreateIndexPartOfSpeechStatement);
            statements.Add(Resources.CreateIndexCTypeStatement);
            statements.Add(Resources.CreateIndexCFormStatement);
            statements.Add(Resources.CreateIndexCFormCTypeStatement);
            statements.Add(Resources.CreateIndexLexemeStatement1);
            statements.Add(Resources.CreateIndexLexemeStatement2);
            statements.Add(Resources.CreateIndexLexemeStatement3);
            statements.Add(Resources.CreateIndexLexemeStatement4);
            statements.Add(Resources.CreateIndexLexemeStatement5);
            statements.Add(Resources.CreateIndexLexemeStatement6);
            statements.Add(Resources.CreateIndexLexemeStatement7);

            DoStringCommands(statements);
        }

        public virtual void CreateFinalIndices()
        {
            List<string> statements = new List<string>();
            statements.Add(Resources.CreateIndexWordStatement1);
            statements.Add(Resources.CreateIndexWordStatement2);
            statements.Add(Resources.CreateIndexWordStatement3);
            statements.Add(Resources.CreateIndexWordStatement4);
            statements.Add(Resources.CreateIndexWordStatement5);
            statements.Add(Resources.CreateIndexWordStatement6);

            DoStringCommands(statements);
        }

        /// <summary>
        /// 単純なSQLコマンド(複数)を順次発行する
        /// </summary>
        /// <param name="statements"></param>
        protected void DoStringCommands(List<string> statements)
        {
            using (DbConnection cnn = this.GetDbConnnection())
            using (DbCommand cmd = cnn.CreateCommand())
            {
                cnn.Open();
                foreach (string statement in statements)
                {
                    if (statement.Length == 0)
                    {
                        continue;
                    }
                    cmd.CommandText = statement;
                    cmd.CommandTimeout = 600;   // 10 minutes
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// ADO.NETのデータベース接続を得る。
        /// </summary>
        /// <returns></returns>
        public abstract DbConnection GetDbConnnection();

        /// <summary>
        /// コーパスの持つSchema Versionをロードする.
        /// </summary>
        /// <param name="cps"></param>
        public virtual void LoadSchemaVersion(Corpus cps)
        {
            NHibernate.Cfg.Configuration cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            this.SetupConnection(cfg);

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {

                CorpusAttribute attr;
                string q = string.Format("from CorpusAttribute where Name='{0}'", CorpusSchema.KeyName);
                if ((attr = session.CreateQuery(q).UniqueResult<CorpusAttribute>()) != null)
                {
                    cps.Schema.Version = Int32.Parse(attr.Value);
                }
            }
        }

        /// <summary>
        /// コーパスを使用するのに必須の情報（POSList等）をロードする.
        /// Corpus選択時に実行する.
        /// </summary>
        /// <param name="cps"></param>
        public virtual void LoadMandatoryCorpusInfo(Corpus cps, RegisterTagCallback callback)
        {
            NHibernate.Cfg.Configuration cfg = SearchConfiguration.GetInstance().NHibernateConfig;

            cps.Lex.Reset();

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                IList lst = session.CreateCriteria(typeof(PartOfSpeech)).List();
                foreach (object obj in lst)
                {
                    PartOfSpeech pos = (PartOfSpeech)obj;
                    cps.Lex.PartsOfSpeech.Add(pos);
                }
                lst = session.CreateCriteria(typeof(CType)).List();
                foreach (object obj in lst)
                {
                    CType ctype = (CType)obj;
                    cps.Lex.CTypes.Add(ctype);
                }
                lst = session.CreateCriteria(typeof(CForm)).List();
                foreach (object obj in lst)
                {
                    CForm cform = (CForm)obj;
                    cps.Lex.CForms.Add(cform);
                }
                CorpusAttribute attr;
                string q = "from CorpusAttribute where Name='LexiconID'";
                if ((attr = session.CreateQuery(q).UniqueResult<CorpusAttribute>()) != null)
                {
                    cps.Lex.UniqueID = attr.Value;
                }
                q = "from CorpusAttribute where Name='Namespaces'";
                if ((attr = session.CreateQuery(q).UniqueResult<CorpusAttribute>()) != null)
                {
                    cps.SetNamespaces(attr.Value);
                }

                // TagDefinitionを取得
                Project proj = session.CreateQuery("from Project p where p.ID=0").UniqueResult<Project>();
                if (proj != null && callback != null)
                {
                    TagSet tset = proj.TagSetList[0];
                    if (tset != null)
                    {
                        foreach (Tag tag in tset.Tags.OrderBy(t => t?.Name))
                        {
                            if (tag != null && tag.Version.IsCurrent)  // Current Versionのみを追加
                            {
                                callback(tag.Type, tag);
                            }
                        }
                    }
                }

                // Project ID Listを取得
                var projects = session.CreateQuery("from Project").List<Project>();
                if (cps.DocumentSet == null)
                {
                    cps.DocumentSet = new DocumentSet();
                }
                cps.DocumentSet.Projects = projects;
            }
        }

        //@todo Service/Search/CorpusServiceを作ってService/Lexicon/SearchLexiconServiceと統合するべき
        /// <summary>
        /// コーパスの基本情報をロードする
        /// </summary>
        /// <param name="cps"></param>
        public virtual void LoadCorpusInfo(Corpus cps)
        {
            NHibernate.Cfg.Configuration cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            this.SetupConnection(cfg);

            cps.Lex.Reset();

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                object o;
                int nAttrs = (int)(long)session.CreateQuery("select count(*) from CorpusAttribute").UniqueResult();

                if ((o = session.CreateQuery("from CorpusAttribute where Name='NLexemes'").UniqueResult()) == null)
                {
                    cps.NLexemes = (int)(long)(session.CreateQuery("select count(*) from Lexeme").UniqueResult());
                    session.Save(new CorpusAttribute(nAttrs++, "NLexemes", cps.NLexemes.ToString()));
                }
                else
                {
                    cps.NLexemes = Int32.Parse(((CorpusAttribute)o).Value);
                }
                if ((o = session.CreateQuery("from CorpusAttribute where Name='NWords'").UniqueResult()) == null)
                {
                    cps.NWords = (int)(long)(session.CreateQuery("select count(*) from Word").UniqueResult());
                    session.Save(new CorpusAttribute(nAttrs++, "NWords", cps.NWords.ToString()));
                }
                else
                {
                    cps.NWords = Int32.Parse(((CorpusAttribute)o).Value);
                }
                if ((o = session.CreateQuery("from CorpusAttribute where Name='NSentences'").UniqueResult()) == null)
                {
                    cps.NSentences = (int)(long)(session.CreateQuery("select count(*) from Sentence").UniqueResult());
                    session.Save(new CorpusAttribute(nAttrs++, "NSentences", cps.NSentences.ToString()));
                }
                else
                {
                    cps.NSentences = Int32.Parse(((CorpusAttribute)o).Value);
                }
                cps.NDocuments = (int)(long)(session.CreateQuery("select count(*) from Document").UniqueResult());
                cps.NSegments = (int)(long)(session.CreateQuery("select count(*) from Segment").UniqueResult());
                cps.NLinks = (int)(long)(session.CreateQuery("select count(*) from Link").UniqueResult());
                cps.NGroups = (int)(long)(session.CreateQuery("select count(*) from Group").UniqueResult());

                string q = "from CorpusAttribute where Name='LexiconID'";
                CorpusAttribute attr;
                if ((attr = session.CreateQuery(q).UniqueResult<CorpusAttribute>()) != null)
                {
                    cps.Lex.UniqueID = attr.Value;
                }
            }
        }

        //@todo Service/Search/CorpusServiceを作ってService/Lexicon/SearchLexiconServiceと統合するべき
        /// <summary>
        /// コーパスのLexicon情報をロードする
        /// </summary>
        /// <param name="cps"></param>
        public virtual void LoadLexicon(Corpus cps)
        {
            NHibernate.Cfg.Configuration cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            this.SetupConnection(cfg);

            cps.Lex.Reset();

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                IList lst = session.CreateCriteria(typeof(PartOfSpeech)).List();
                foreach (object obj in lst)
                {
                    PartOfSpeech pos = (PartOfSpeech)obj;
                    cps.Lex.PartsOfSpeech.Add(pos);
                }
                lst = session.CreateCriteria(typeof(CType)).List();
                foreach (object obj in lst)
                {
                    CType ctype = (CType)obj;
                    cps.Lex.CTypes.Add(ctype);
                }
                lst = session.CreateCriteria(typeof(CForm)).List();
                foreach (object obj in lst)
                {
                    CForm cform = (CForm)obj;
                    cps.Lex.CForms.Add(cform);
                }
                lst = session.CreateCriteria(typeof(Lexeme)).List();
                foreach (object obj in lst)
                {
                    Lexeme lex = (Lexeme)obj;
                    try
                    {
                        cps.Lex.Add(lex);
                    }
                    catch
                    {
                        //@todo
                    }
                }
            }
        }

        public IList<string> LoadTagSetNames()
        {
            List<string> result = new List<string>();

            SearchConfiguration sc = SearchConfiguration.GetInstance();
            NHibernate.Cfg.Configuration cfg = sc.NHibernateConfig;
            this.SetupConnection(cfg);

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                IList<TagSet> lst = session.CreateCriteria(typeof(TagSet)).List<TagSet>();
                foreach (TagSet ts in lst)
                {
                    result.Add(ts.Name);
                }
            }
            return result;
        }

        public void LoadTags(out DataTable segs, out DataTable links, out DataTable groups)
        {
            segs = CreateTagDataTable();
            links = CreateTagDataTable();
            groups = CreateTagDataTable();

            NHibernate.Cfg.Configuration cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            this.SetupConnection(cfg);

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                IList<Tag> lst = session.CreateCriteria(typeof(Tag)).List<Tag>();
                foreach (Tag t in lst)
                {
                    DataTable table = null;
                    if (t.Type == Tag.SEGMENT)
                    {
                        table = segs;
                    }
                    else if (t.Type == Tag.LINK)
                    {
                        table = links;
                    }
                    else if (t.Type == Tag.GROUP)
                    {
                        table = groups;
                    }
                    if (table == null) continue;
                    DataRow row = table.NewRow();
                    row["ID"] = t.ID;
                    row["Name"] = t.Name;
                    row["Description"] = t.Description;
                    row["Version"] = t.Version.Version;
                    row["IsCurrent"] = t.Version.IsCurrent;
                    table.Rows.Add(row);
                }
            }
        }

        private DataTable CreateTagDataTable()
        {
            DataTable dt = new DataTable();
            List<DataColumn> columns = new List<DataColumn>() {
                new DataColumn() { DataType = typeof(int), ColumnName = "ID", ReadOnly = true },
                new DataColumn() { DataType = typeof(string), ColumnName = "Name", ReadOnly = true },
                new DataColumn() { DataType = typeof(string), ColumnName = "Description", ReadOnly = true },
                new DataColumn() { DataType = typeof(string), ColumnName = "Version", ReadOnly = true },
                new DataColumn() { DataType = typeof(bool), ColumnName = "IsCurrent", ReadOnly = true }
            };
            foreach (DataColumn c in columns)
            {
                dt.Columns.Add(c);
            }
           return dt;
        }

        /// <summary>
        /// コーパスのDocument情報をDataGridViewに表示するための一時的(On Memoryの)DataSetに格納する
        /// </summary>
        /// <param name="cps"></param>
        public virtual IListSource LoadDocumentInfo()
        {
            DataTable table = new DataTable("DocumentInfo");
            // 必ず存在するColumnn
            DataColumn column = new DataColumn() { DataType = typeof(int), ColumnName = "ID", ReadOnly = true };
            table.Columns.Add(column);
            column = new DataColumn() { DataType = typeof(string), ColumnName = "Text", ReadOnly = true };
            table.Columns.Add(column);

            NHibernate.Cfg.Configuration cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            this.SetupConnection(cfg);

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                IList list = session.CreateQuery("from Document").List();
                foreach (object o in list)
                {
                    Document doc = (Document)o;
                    DataRow row = table.NewRow();
                    row["ID"] = doc.ID;
                    row["Text"] = QueryShortenedText(session, doc);

                    foreach (DocumentAttribute attr in doc.Attributes)
                    {
                        if (!table.Columns.Contains(attr.Key))
                        {
                            column = new DataColumn() { DataType = typeof(String), ColumnName = attr.Key, ReadOnly = true };
                            table.Columns.Add(column);
                        }
                        row[attr.Key] = attr.Value;
                    }
                    table.Rows.Add(row);
                }
            }
            return table;
        }

        public IList<string> LoadDocumentAttributeKeys()
        {
            var cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            this.SetupConnection(cfg);

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                return session.CreateQuery("select distinct Key from DocumentAttribute").List<string>();
            }
        }

        public IList<string> LoadDocumentAttributeValues(string key)
        {
            var cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            this.SetupConnection(cfg);

            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                return session.CreateQuery(string.Format("select distinct Value from DocumentAttribute where Key='{0}'", key)).List<string>();
            }
        }

        /// <summary>
        /// Corpus DBのスキーマを変換により最新化する.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="callback"></param>
        public bool ConvertSchema(Corpus c, Action<string> callback)
        {
            int oldVer = c.Schema.Version;
            if (oldVer == 0)
            {
                if (callback != null) callback("Corpus has no schema information. (Too old)");
                return false;
            }
            using (DbConnection cnn = this.GetDbConnnection())
            using (DbCommand cmd = cnn.CreateCommand())
            {
                cnn.Open();
                DbTransaction trans = cnn.BeginTransaction();
                cmd.CommandTimeout = 600;   // 10 minutes

                // 古いVersionから新しいVersionへと順次スキーマ変換を行う
                if (oldVer <= 1)    // 1-->2 Conversion
                {
                    ConvertDbFrom1(cmd, callback);
                }
                if (oldVer <= 2)   // 2-->3 Conversion
                {
                    ConvertDbFrom2To3(cmd, callback);
                }
                if (oldVer <= 3)   // 3-->4 Conversion
                {
                    ConvertDbFrom3To4(cmd, callback);
                }
                if (oldVer <= 4)   // 4-->5 Conversion
                {
                    ConvertDbFrom4To5(cmd, callback);
                }
                if (oldVer <= 5)   // 5-->6 Conversion
                {
                    ConvertDbFrom5To6(cmd, callback);
                }
                if (oldVer <= 6)   // 6-->7 Conversion
                {
                    ConvertDbFrom6To7(cmd, callback);
                }
                if (oldVer <= 7)   // 7-->8 Conversion
                {
                    ConvertDbFrom7To8(cmd, callback);
                }
                if (oldVer <= 8)   // 8-->9 Conversion
                {
                    ConvertDbFrom8To9(cmd, callback);
                }
                if (oldVer <= 9)   // 9-->10 Conversion
                {
                    ConvertDbFrom9To10(cmd, callback);
                }
                if (oldVer <= 10)   // 10-->11 Conversion
                {
                    ConvertDbFrom10To11(cmd, callback);
                }
                if (oldVer <= 11)   // 11-->12 Conversion
                {
                    ConvertDbFrom11To12(cmd, callback);
                }
                if (oldVer <= 12)   // 12-->13 Conversion
                {
                    ConvertDbFrom12To13(cmd, callback);
                }
                //              if (oldVer <= n)   // n-->(n+1) Conversion
                //              {
                //              }
                //              etc.

                // 最後に最新Versionをセット
                cmd.CommandText = string.Format("UPDATE corpus_attribute SET value={0} WHERE name='{1}'", CorpusSchema.CurrentVersion, CorpusSchema.KeyName);
                if (DoDbCommand(cmd, callback))
                {
                    trans.Commit();
                    if (callback != null) callback(string.Format("Corpus schema updated to {0}.", CorpusSchema.CurrentVersion));
                    c.Schema.Version = CorpusSchema.CurrentVersion;
                    return true;
                }
                trans.Rollback();
                return false;
            }
        }

        #region --- DB Schema のVersion Migrator ---

        private void ConvertDbFrom1(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <1> to <2>...");
            cmd.CommandText = "ALTER TABLE lexeme ADD lemmaform VARCHAR(255) DEFAULT ''";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'lemmaform' added to table 'lexeme'.");
            }

            cmd.CommandText = "ALTER TABLE lexeme ADD lemma VARCHAR(255) DEFAULT ''";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'lemma' added to table 'lexeme'.");
            }
        }

        private void ConvertDbFrom2To3(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <2> to <3>...");
            cmd.CommandText = "ALTER TABLE lexeme ADD custom TEXT";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'custom' added to table 'lexeme'.");
            }
        }

        private void ConvertDbFrom3To4(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <3> to <4>...");
            cmd.CommandText = "ALTER TABLE tagset_version ADD tagset_id INT NOT NULL DEFAULT 0";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'tagset_id' added to table 'tagset_version'.");
            }
            // ALTER TABLE ALTER コマンドはSQLiteでは使用できない。
            // Version 3のDBではまだgroup_memberの中身は空なので、テーブルを作り直すことで対応
            //                    cmd.CommandText = "ALTER TABLE group_member ALTER object_type SET DEFAULT 'Segment'";
            cmd.CommandText = "DROP TABLE IF EXISTS group_member";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> Table 'group_member' dropped.");
            }
            cmd.CommandText = "CREATE TABLE group_member "
                + "(group_id bigint not null, object_type varchar(8) not null default \"Segment\","
                + " member_id bigint not null, createtime timestamp)";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> Table 'group_member' created and modified.");
            }
            // Tagの追加
            int tagid = 6; // 既に6個存在している.
            new List<Tag>() {
                        new Tag(Tag.SEGMENT, "Apposition"),
                        new Tag(Tag.SEGMENT, "Parallel"),
                        new Tag(Tag.SEGMENT, "Nest"),
                        new Tag(Tag.GROUP, "Apposition"),
                        new Tag(Tag.GROUP, "Parallel") }
            .ForEach(new Action<Tag>(delegate(Tag t)
            {
                cmd.CommandText = string.Format("INSERT INTO tag_definition "
                    + "VALUES({0},{1},'{2}','{3}','{4}',{5})", tagid++, 0, t.Type, t.Name, string.Empty, 0);
                if (DoDbCommand(cmd, callback) && callback != null)
                {
                    callback(string.Format("> {0} '{1}' inserted to Table tag_defintiion.", t.Type, t.Name));
                }
            }));
        }

        // SLAT 2009.12版との統合
        private void ConvertDbFrom4To5(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <4> to <5>...");
            // DocumentSet - document, project等の関係.
            cmd.CommandText = Resources.CreateTableDocumentSetDocumentStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'document_set_document' created.");
            cmd.CommandText = Resources.CreateTableDocumentSetProjectStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'document_set_project' created.");
            cmd.CommandText = Resources.CreateTableDocumentSetProjectUserStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'document_set_project_user' created.");
            cmd.CommandText = Resources.CreateTableDocumentSetUserStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'document_set_user' created.");
            cmd.CommandText = Resources.CreateTableTagsetUserStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'tagset_user' created.");
            cmd.CommandText = Resources.CreateTableSegmentConstraintStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'segment_constraint' created.");
            cmd.CommandText = Resources.CreateTableLinkConstraintStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'link_constraint' created.");
            cmd.CommandText = Resources.CreateTableGroupConstraintStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'group_constraint' created.");
            cmd.CommandText = Resources.CreateTableAttributeConstraintStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'attribute_constraint' created.");
            cmd.CommandText = Resources.CreateTableTagAppearanceStatement;
            if (DoDbCommand(cmd, callback) && callback != null) callback("> Table 'tag_appearance' created.");
            cmd.CommandText = "ALTER TABLE document_set ADD comments text";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'comments' added to table 'document_set'.");
            cmd.CommandText = "ALTER TABLE group_member ADD project_id int";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'project_id' added to table 'group_member'.");
            cmd.CommandText = "ALTER TABLE group_member ADD user_id int";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'user_id' added to table 'group_member'.");
            cmd.CommandText = "ALTER TABLE project ADD comments text";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'comments' added to table 'project'.");
            cmd.CommandText = "ALTER TABLE project_tagset ADD tagset_role int";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'tagset_role' added to table 'project_tagset'.");
            cmd.CommandText = "ALTER TABLE project_user ADD user_privilege int";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'user_privilege' added to table 'project_user'.");
            cmd.CommandText = "ALTER TABLE segment ADD string_value text";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'string_value' added to table 'segment'.");
            cmd.CommandText = "ALTER TABLE user ADD user_privilege int";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'user_privilege' added to table 'user'.");
            cmd.CommandText = "ALTER TABLE user ADD comments text";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> New column 'comments' added to table 'user'.");

            // 新しい関連の値を挿入
            // DocumentSet : Document
            cmd.CommandText = "SELECT document_id FROM document";
            List<int> documentIDs = new List<int>();
            using (DbDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read()) documentIDs.Add(rdr.GetInt32(0));
            }
            int icount = 0;
            foreach (int id in documentIDs)
            {
                cmd.CommandText = string.Format("INSERT INTO document_set_document VALUES({0},0)", id);
                if (DoDbCommand(cmd, callback)) icount++;
            }
            if (icount > 0 && callback != null) callback(string.Format("> {0} Row(s) inserted into table 'document_set_document'.", icount));
            // その他の関連
            cmd.CommandText = "INSERT INTO document_set_project VALUES(0,0,0)";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> 1 Row inserted into table 'document_set_project'.");
            cmd.CommandText = "INSERT INTO document_set_project_user VALUES(0,0,0)";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> 1 Row inserted into table 'document_set_project_user'.");
            cmd.CommandText = "INSERT INTO document_set_user VALUES(0,0,0)";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> 1 Row inserted into table 'document_set_user'.");
            cmd.CommandText = "INSERT INTO tagset_user VALUES(0,0,0)";
            if (DoDbCommand(cmd, callback) && callback != null) callback("> 1 Row inserted into table 'tagset_user'.");

            // Tagの削除 (Segmentの'Quote'およびLinkのA,P,I)
            long count;
            cmd.CommandText = "SELECT COUNT(*) FROM segment s, tag_definition t WHERE s.tag_definition_id=t.id AND t.tag_name='Quote'";
            if (DoDbGetInt(cmd, out count, callback) && count == 0)
            {
                cmd.CommandText = "DELETE from tag_definition WHERE tag_name='Quote'";
                if (DoDbCommand(cmd, callback) && callback != null) callback("> Row 'Quote' removed from table 'tag_definition'.");
            }
            new List<string>() { "A", "P", "I" }.ForEach(new Action<string>(s =>
                {
                    cmd.CommandText = string.Format("SELECT COUNT(*) FROM Link l, tag_definition t WHERE l.tag_definition_id=t.id AND t.tag_name='{0}'", s);
                    if (DoDbGetInt(cmd, out count, callback) && count == 0)
                    {
                        cmd.CommandText = string.Format("DELETE from tag_definition WHERE tag_name='{0}'", s);
                        if (DoDbCommand(cmd, callback) && callback != null) callback(string.Format("> Row '{0}' removed from table 'tag_definition'.", s));
                    }
                }));
        }

        // データ修正
        // - Sentence Tableのend_charのみがDocumentオフセットになっていた。Corpus絶対位置に修正。
        // - Bunsetsu以外のSegmentのend_char, start_charがCorpus絶対位置になっていた。Documentオフセット位置に修正。
        private void ConvertDbFrom5To6(DbCommand cmd, Action<string> callback)
        {
            List<int> documentList = new List<int>();
            Dictionary<int, int> startSentenceId = new Dictionary<int, int>();
            Dictionary<int, int> documentOffsets = new Dictionary<int, int>();
            if (callback != null) callback("Updating version <5> to <6>...");
            cmd.CommandText = "SELECT DISTINCT document_id FROM sentence ORDER BY document_id ASC";
            using (DbDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    documentList.Add(rdr.GetInt32(0));
                }
            }
            if (documentList.Count < 2)
            {
                callback("> Nothing to do. (Corpus has only 1 document)");
                return;
            }
            // 各Documentの先頭Sentenceを得る.
            foreach (int docid in documentList)
            {
                cmd.CommandText = string.Format("SELECT id, start_char FROM sentence WHERE document_id={0} ORDER BY id ASC", docid);
                using (DbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        startSentenceId[docid] = rdr.GetInt32(0);
                        documentOffsets[docid] = rdr.GetInt32(1);
                        break;
                    }
                }
            }
            foreach (KeyValuePair<int, int> pair in documentOffsets)
            {
                if (pair.Value > 0)
                {
                    cmd.CommandText = string.Format("UPDATE sentence SET start_char=(start_char+{0}) where document_id={1} and id!={2}", pair.Value, pair.Key, startSentenceId[pair.Key]); // 先頭SentenceのみStartが絶対位置であったというバグのため.
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = string.Format("UPDATE sentence SET end_char=(end_char+{0}) where document_id={1}", pair.Value, pair.Key);
                    if (DoDbCommand(cmd, callback) && callback != null) callback(string.Format("> Fixed 'end_char' columns of 'sentence' table (DOCID={0}).", pair.Key));
                }
            }
        }

        /// <summary>
        /// 6 --> 7
        /// Sentence Tableにposカラム（ドキュメント相対文番号）を追加.
        /// Sentenceのstart_pos, end_posをドキュメント相対に変更.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        private void ConvertDbFrom6To7(DbCommand cmd, Action<string> callback)
        {
            List<int> documentList = new List<int>();

            if (callback != null) callback("Updating version <6> to <7>...");

            // sentence.pos カラムを追加
            cmd.CommandText = "ALTER TABLE sentence ADD pos INT";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'pos' added to table 'sentence'.");
            }
            // Documentのリストを得る.
            cmd.CommandText = "SELECT DISTINCT document_id FROM sentence ORDER BY document_id ASC";
            using (DbDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    documentList.Add(rdr.GetInt32(0));
                }
            }
            // 各Documentに対して...
            foreach (int docid in documentList)
            {
                int startSentenceID = -1;
                int documentOffset = -1;
                List<int> senids = new List<int>();
                // 先頭Sentenceを得る.
                cmd.CommandText = string.Format("SELECT id, start_char FROM sentence WHERE document_id={0} ORDER BY id ASC", docid);
                using (DbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        int id = rdr.GetInt32(0);
                        senids.Add(id);
                        if (startSentenceID < 0)
                        {
                            startSentenceID = id;
                            documentOffset = rdr.GetInt32(1);
                        }
                    }
                }
                if (startSentenceID < 0 || documentOffset < 0)
                {
                    continue;
                }
                // sentence.pos カラムをセット
                // sentence.{start_char, end_char} を変更
                int pos = 0;
                long lastcharpos = -1;
                cmd.CommandText = string.Format("SELECT id FROM sentence WHERE document_id={0} ORDER BY id ASC", docid);
                foreach (int senid in senids)
                {
                    cmd.CommandText = string.Format("SELECT MIN(start_char) FROM word WHERE sentence_id={0}", senid);
                    string tmp = cmd.ExecuteScalar().ToString();
                    if (tmp.Length > 0)
                    {
                        var start_char = Int64.Parse(tmp);
                        cmd.CommandText = string.Format("SELECT MAX(end_char) FROM word WHERE sentence_id={0}", senid);
                        tmp = cmd.ExecuteScalar().ToString();
                        var end_char = Int64.Parse(tmp);
                        cmd.CommandText = string.Format("UPDATE sentence SET start_char={0}, end_char={1}, pos={2} where id={3}", start_char, end_char, pos, senid);
                        cmd.ExecuteNonQuery();
                        lastcharpos = end_char;
                    }
                    else // Wordを含まない文は、前の文のend_char+1をstart_char, end_charに設定する. (+1はEOSを意味する.)
                    {
                        lastcharpos++;
                        cmd.CommandText = string.Format("UPDATE sentence SET start_char={0}, end_char={0}, pos={1} where id={2}", lastcharpos, pos, senid);
                        cmd.ExecuteNonQuery();
                    }
                    pos++;
                }
            }
        }

        /// <summary>
        /// 7 --> 8
        /// Word Tableにstart_time, end_time, durationカラムを追加.
        /// それぞれindexも追加.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        private void ConvertDbFrom7To8(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <7> to <8>...");
            cmd.CommandText = "ALTER TABLE word ADD start_time REAL";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'start_time' added to table 'word'.");
            }
            cmd.CommandText = "ALTER TABLE word ADD end_time REAL";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'end_time' added to table 'word'.");
            }
            cmd.CommandText = "ALTER TABLE word ADD duration REAL";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'duration' added to table 'word'.");
            }
            cmd.CommandText = Resources.CreateIndexWordStatement4;
            DoDbCommand(cmd, callback);
            cmd.CommandText = Resources.CreateIndexWordStatement5;
            DoDbCommand(cmd, callback);
            cmd.CommandText = Resources.CreateIndexWordStatement6;
            DoDbCommand(cmd, callback);
        }

        /// <summary>
        /// 8 --> 9
        /// Word Tableにextra_charsカラムを追加.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        private void ConvertDbFrom8To9(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <8> to <9>...");
            cmd.CommandText = "ALTER TABLE word ADD extra_chars VARCHAR(255) default ''";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'extra_chars' added to table 'word'.");
            }
        }

        /// <summary>
        /// 9 --> 10
        /// Word_Word Tableを追加.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        private void ConvertDbFrom9To10(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <9> to <10>...");
            cmd.CommandText = "ALTER TABLE word ADD project_id int default 0";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'project_id' added to table 'word'.");
            }
            cmd.CommandText = "DROP TABLE IF EXISTS word_word ";
            DoDbCommand(cmd, callback);
            cmd.CommandText = "CREATE TABLE word_word (from_word int, to_word int, comment  varchar(255) default '')";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New table 'word_word' added.");
            }
        }

        /// <summary>
        /// 10 --> 11
        /// Word Tableにhead_infoカラムを追加.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        private void ConvertDbFrom10To11(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <10> to <11>...");
            cmd.CommandText = "ALTER TABLE word ADD head_info int default 0";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'head_info' added to table 'word'.");
            }
        }

        /// <summary>
        /// 11 --> 12
        /// LinkAttribute Tableにindexを追加.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        private void ConvertDbFrom11To12(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <11> to <12>...");
            cmd.CommandText = "CREATE INDEX link_attribute_link_index ON link_attribute (link_id,attribute_key)";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New index 'link_attribute_link_index' added to table 'link_attribute'.");
            }
            cmd.CommandText = "CREATE INDEX segment_attribute_segment_index ON segment_attribute(segment_id)";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New index 'segment_attribute_segment_index' added to table 'segment_attribute'.");
            }
            cmd.CommandText = "CREATE INDEX group_attribute_group_index ON group_attribute(group_id)";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New index 'group_attribute_group_index' added to table 'group_attribute'.");
            }

        }

        /// <summary>
        /// 12 --> 13
        /// Segment, Link Tableにlexeme_idカラムを追加.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        private void ConvertDbFrom12To13(DbCommand cmd, Action<string> callback)
        {
            if (callback != null) callback("Updating version <12> to <13>...");
            cmd.CommandText = "ALTER TABLE segment ADD lexeme_id int";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'lexeme_id' added to table 'segment'.");
            }
            cmd.CommandText = "ALTER TABLE link ADD lexeme_id int";
            if (DoDbCommand(cmd, callback) && callback != null)
            {
                callback("> New column 'lexeme_id' added to table 'link'.");
            }
        }
        #endregion

        private bool DoDbCommand(DbCommand cmd, Action<string> callback)
        {
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (callback != null) callback(string.Format("{0} - {1}.", cmd, ex.Message));
                return false;
            }
            return true;
        }

        private bool DoDbGetInt(DbCommand cmd, out long ival, Action<string> callback)
        {
            try
            {
                ival = (long)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                if (callback != null) callback(string.Format("{0} - {1}.", cmd, ex.Message));
                ival = 0;
                return false;
            }
            return true;
        }

        protected string QueryShortenedText(ISession session, Document doc)
        {
            IDbConnection conn = session.Connection;
            IDbCommand cmd = conn.CreateCommand();
            //!SQL
            cmd.CommandText = string.Format("SELECT substr(document_text,1,{0}) FROM document WHERE document_id={1}", 20, doc.ID);
            string result = (string)cmd.ExecuteScalar();
            return result + "...";
        }

        public virtual string GetBooleanString(bool b)
        {
            return b ? "true" : "false";
        }

        /// <summary>
        /// SQLiteで有効なSynchoronous Pragma設定
        /// </summary>
        /// <param name="mode"></param>
        public virtual void SetSynchronousMode(string mode)
        {
            // No-op by default
        }

        public abstract DbParameter CreateParameter();

        public void RepairDb(Action<string> logger, bool checkonly = false)
        {
            var nl = Environment.NewLine;
            var percent = -1;
            var cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            using (ISessionFactory factory = cfg.BuildSessionFactory())
            using (ISession session = factory.OpenSession())
            {
                // 全Linkについて、from_segment_id, to_segment_idに対応するSegmentが存在するかをチェック
                logger("Checking Links...");
                {
                    var q = session.CreateSQLQuery("select id, from_segment_id, to_segment_id from link");
                    var result = q.List<object[]>();
                    var n = result.Count;
                    var i = 0;
                    foreach (var r in result)
                    {
                        var id = (long)r[0];
                        var from_id = (long)r[1];
                        var to_id = (long)r[2];
                        var q2 = session.CreateSQLQuery($"select id from segment where id={from_id}");
                        if (q2.UniqueResult() == null)
                        {
                            logger(nl + $"Link ID={id}: corrupted. from_segment (ID={from_id}) not found.");
                        }
                        q2 = session.CreateSQLQuery($"select id from segment where id={to_id}");
                        if (q2.UniqueResult() == null)
                        {
                            logger(nl + $"Link ID={id}: corrupted. from_segment (ID={to_id}) not found.");
                        }
                        if (percent != (++i) * 100 / n)
                        {
                            percent = i * 100 / n;
                            if (percent % 10 == 0)
                            {
                                logger($"{percent}%..");
                            }
                        }
                    }
                    logger("100%");
                }

                // 全Groupについて、memberに対応するAnnotationが存在するかチェック
                logger(nl + "Checking Groups...");
                {
                    var q = session.CreateSQLQuery($"select id from group_element");
                    var result = q.List<long>();
                    var n = result.Count;
                    var i = 0;
                    foreach (var gid in result)
                    {
                        var q2 = session.CreateSQLQuery($"select member_id, object_type from group_member where group_id={gid}");
                        var result2 = q2.List<object[]>();
                        foreach (var r in result2)
                        {
                            var aid = (long)r[0];
                            var t = (string)r[1];
                            if (t == "Segment")
                            {
                                var q3 = session.CreateSQLQuery($"select id from segment where id={aid}");
                                if (q3.UniqueResult() == null)
                                {
                                    logger(nl + $"Group ID={gid}: corrupted. member[Segment] (ID={aid}) not found.");
                                }
                            } else if (t == "Link")
                            {
                                var q3 = session.CreateSQLQuery($"select id from link where id={aid}");
                                if (q3.UniqueResult() == null)
                                {
                                    logger(nl + $"Group ID={gid}: corrupted. member[Link] (ID={aid}) not found.");
                                }
                            } else
                            {
                                logger(nl + $"Group ID={gid}: corrupted. object_type is {t}.");
                            }
                        }
                        if (percent != (++i) * 100 / n)
                        {
                            percent = i * 100 / n;
                            if (percent % 10 == 0)
                            {
                                logger($"{percent}%..");
                            }
                        }
                    }
                    logger("100%" + nl);
                }
                logger(nl + "Done.");
            }

        }
    }

    public delegate void RegisterTagCallback(string tagType, Tag tag);
}
