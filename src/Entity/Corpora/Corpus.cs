using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.Common;
using ChaKi.Entity.Properties;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Settings;
using System.Threading;

namespace ChaKi.Entity.Corpora
{
    public class Corpus : Dictionary_DB
    {
        // 各種単純統計値(DBService.LoadCorpusInfo()で取得される)
        [XmlIgnore]
        public long NWords { get; set; }
        [XmlIgnore]
        public int NSentences { get; set; }
        [XmlIgnore]
        public int NBunsetsus { get; set; }
        [XmlIgnore]
        public int NSegments { get; set; }
        [XmlIgnore]
        public int NLinks { get; set; }
        [XmlIgnore]
        public int NGroups { get; set; }
        [XmlIgnore]
        public int NDocuments { get; set; }


        /// <summary>
        /// 同期オブジェクト
        /// </summary>
        [XmlIgnore]
        public Mutex Mutex { get; set; }

        public Corpus()
            : base()
        {
            this.Sentences = new List<Sentence>();
            this.Segments = new List<Segment>();
            this.Links = new List<Link>();
            this.Groups = new List<Group>();
            this.Namespaces = new List<Namespace>();
            this.Schema = new CorpusSchema();
            this.Mutex = new Mutex();
        }

        public static Corpus CreateFromFile(string path)
        {
            Corpus c = new Corpus();
            c.Source = path;
            string ext = Path.GetExtension(path).ToUpper();
            // nameはSQLiteの場合DB名(パス）、通常はDB定義ファイル
            if (ext.Equals(".DEF"))
            {
                //defファイルを読み込んで各種パラメータを得る
                c.DBParam.ParseDefFile(path);
            }
            else if (ext.Equals(".DB"))
            {
                c.DBParam.DBType = "SQLite";
                c.DBParam.DBPath = path;
                c.DBParam.Name = Path.GetFileNameWithoutExtension(path);
            }
            return c;
        }

        // このコーパスに対応するDocumentSetの管理
        [XmlIgnore]
        public DocumentSet DocumentSet { get; set; }

        // 文の管理
        [XmlIgnore]
        public List<Sentence> Sentences { get; set; }

        public void AddSentence(Sentence sen)
        {
            this.Sentences.Add(sen);
        }

        // Segmentの管理
        [XmlIgnore]
        public List<Segment> Segments { get; set; }

        // Linkの管理
        [XmlIgnore]
        public List<Link> Links { get; set; }

        // Groupの管理
        [XmlIgnore]
        public List<Group> Groups { get; set; }

        // Namespaceの管理
        // Namespaceは、インポート時にはCabochaReaderの"#! NAMESPACE"タグ読み込みによりここにセットされると共に、
        //  CreateCorpus.SaveProject()内でDBのCorpusAttributeテーブルに保存される.
        // コーパスロード時は、DBService.LoadMandatoryCorpusInfo()を呼ぶことでDBからロードされ、ここにセットされる.
        // エクスポート時には、ExportServiceBase.ExportDocumentList()およびExportServiceCabocha.ExportDocumentList()で書き出される.
        [XmlIgnore]
        public List<Namespace> Namespaces { get; set; }


        public virtual void AddSegment(Segment seg)
        {
            this.Segments.Add(seg);
        }

        public virtual void AddLink(Link lnk)
        {
            this.Links.Add(lnk);
        }

        public virtual void AddGroup(Group grp)
        {
            this.Groups.Add(grp);
        }

        public virtual void AddNamespace(Namespace ns)
        {
            this.Namespaces.Add(ns);
        }

        public void SetNamespaces(string nsdefs)
        {
            this.Namespaces.Clear();
            foreach (var ns in nsdefs.Split('\n'))
            {
                var pair = ns.Split('=');
                if (pair.Length == 2)
                {
                    var key = pair[0];
                    if (key.StartsWith("xmlns:"))
                    {
                        key = key.Substring(6);
                    }
                    AddNamespace(new Namespace(key, pair[1]));
                }
            }
        }

        public string NamespacesToString()
        {
            var sb = new StringBuilder();
            foreach (var ns in this.Namespaces)
            {
                sb.AppendFormat("{0}={1}\n", ns.Key, ns.Value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// コーパスの基本情報をCorpusPropertyのリストとして返す.
        /// </summary>
        /// <returns></returns>
        public List<CorpusProperty> GetCorpusProperties()
        {
            List<CorpusProperty> cprops = new List<CorpusProperty>();
            cprops.Add(new CorpusProperty("SchemaVersion", this.Schema.Version.ToString()));
            cprops.Add(new CorpusProperty("Name", this.Name));
            cprops.Add(new CorpusProperty("DBType", this.DBParam.DBType));
            cprops.Add(new CorpusProperty("Path", this.DBParam.DBPath));
            cprops.Add(new CorpusProperty("Server", this.DBParam.Server));
            cprops.Add(new CorpusProperty("Login", this.DBParam.Login));
            string dummy = new string('*', this.DBParam.Password.Length);
            cprops.Add(new CorpusProperty("Password", dummy));
            cprops.Add(new CorpusProperty("#Documents", this.NDocuments.ToString()));
            cprops.Add(new CorpusProperty("#Words", this.NWords.ToString()));
            cprops.Add(new CorpusProperty("#Lexemes", this.NLexemes.ToString()));
            cprops.Add(new CorpusProperty("#Sentences", this.NSentences.ToString()));
//            cprops.Add(new CorpusProperty("#Bunsetsu", this.NBunsetsus.ToString()));
            cprops.Add(new CorpusProperty("#Segments", this.NSegments.ToString()));
            cprops.Add(new CorpusProperty("#Links", this.NLinks.ToString()));
            cprops.Add(new CorpusProperty("#Groups", this.NGroups.ToString()));
            cprops.Add(new CorpusProperty("Lexicon ID", this.Lex.UniqueID));
            cprops.Add(new CorpusProperty("Namespaces", NamespacesToString()));
            return cprops;
        }
    }
}
