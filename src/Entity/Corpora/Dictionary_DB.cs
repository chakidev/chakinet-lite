using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora
{
    public class Dictionary_DB : Dictionary
    {
        public override string Name
        {
            get { return DBParam.Name; }
        }

        /// <summary>
        /// コーパス定義ファイル
        /// または（SQLiteの場合）DBファイルそのもののパス
        /// </summary>
        public string Source { get; set; }

        public DBParameter DBParam { get; set; }

        public CorpusSchema Schema { get; set; }

        // 各種単純統計値(DBService.LoadCorpusInfo()で取得される)
        [XmlIgnore]
        public long NLexemes { get; set; }

        // Lexiconの管理
        [XmlIgnore]
        public Lexicon Lex { get; set; }

        public string LexiconID
        {
            get { return this.Lex.UniqueID; }
            set { this.Lex.UniqueID = value; }
        }

        public Dictionary_DB()
        {
            this.DBParam = new DBParameter();
            this.Lex = new Lexicon();
            this.Schema = new CorpusSchema();
        }

        public Dictionary_DB(string path)
            : this()
        {
            this.Source = path;
            string ext = Path.GetExtension(path).ToUpper();
            // nameはSQLiteの場合DB名(パス）、通常はDB定義ファイル
            if (ext.Equals(".DEF"))
            {
                //defファイルを読み込んで各種パラメータを得る
                this.DBParam.ParseDefFile(path);
            }
            else if (ext.Equals(".DB") || ext.Equals(".DDB"))
            {
                this.DBParam.DBType = "SQLite";
                this.DBParam.DBPath = path;
                this.DBParam.Name = Path.GetFileNameWithoutExtension(path);
            }
        }
    }
}
