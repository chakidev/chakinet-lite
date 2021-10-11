using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Service.Database;
using ChaKi.Entity.Corpora;
using System.Data.Common;
using ChaKi.Service.Search;
using System.IO;

namespace ImportWordRelation
{
    /// <summary>
    /// テキストファイル（Tab区切り）で提供されるword -> word の関係を word_word Tableに展開する.
    /// </summary>
    class ImportWordRelation
    {
        public string InputPath = null;
        public string CorpusName = null;
        public bool MakeBidirectional = false;
        public bool NoRemoveBeforeInsert = false;
        public bool DoNotPauseOnExit = false;

        private DBService m_Service = null;
        private Corpus m_Corpus = null;
        private string m_DefaultString;
        private DbConnection m_Conn = null;

        public bool ParseArguments(string[] args)
        {
            int n = 0;
            foreach (string arg in args)
            {
                if (arg.Length > 1 && arg.StartsWith("-"))
                {
                    string[] tokens = arg.Substring(1).Split(new char[] { '=' });
                    if (!(tokens.Length == 1 && (tokens[0].Equals("C")))
                     && !(tokens.Length == 1 && (tokens[0].Equals("b")))
                     && !(tokens.Length == 1 && (tokens[0].Equals("a"))))
                    {
                        Console.WriteLine("Invalid option: {0}", arg);
                        return false;
                    }
                    if (tokens[0].Equals("C"))
                    {
                        this.DoNotPauseOnExit = true;
                    }
                    else if (tokens[0].Equals("b"))
                    {
                        this.MakeBidirectional = true;
                    }
                    else if (tokens[0].Equals("a"))
                    {
                        this.NoRemoveBeforeInsert = true;
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

        public void Process()
        {
            // Corpusの作成
            m_Corpus = Corpus.CreateFromFile(this.CorpusName);
            if (m_Corpus == null)
            {
                PrintError("Cannot create Corpus. Maybe <out file>'s extension is neither .db nor .def");
                return;
            }

            // DBアクセスのためのサービスを得る
            m_Service = DBService.Create(m_Corpus.DBParam);
            m_DefaultString = m_Service.GetDefault(); // INSERT文のDEFAULT文字列
            var cfg = SearchConfiguration.GetInstance().NHibernateConfig;
            // Corpus(DB)の種類に合わせてConfigurationをセットアップする
            m_Service.SetupConnection(cfg);

            using (m_Conn = m_Service.GetDbConnnection())
            {
                m_Conn.Open();
                var trans = m_Conn.BeginTransaction();

                using (var cmd = m_Conn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = "DELETE FROM word_word";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE INDEX IF NOT EXISTS word_word_index on word_word (from_word)";
                    cmd.ExecuteNonQuery();
                }

                ReadFile(this.InputPath);

                trans.Commit();

                if (this.DoNotPauseOnExit)
                {
                    return;
                }
            }
            Console.WriteLine("\nPress Enter to exit.");
            Console.ReadLine();
        }

        public bool ReadFile(string path)
        {
            Console.WriteLine("File: {0}", path);
            try
            {
                using (var rdr = new StreamReader(path))
                {
                    string line;
                    int ln = 1;
                    while (true)
                    {
                        line = rdr.ReadLine();
                        if (line == null) break;
                        ProcessLine(ln, line);
                        ln++;
                    }
                    Console.WriteLine("{0} lines processed.", ln - 1);
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            return true;
        }

        public void ProcessLine(int ln, string line)
        {
            var tokens = line.Split('\t');
            if (tokens.Length != 6)
            {
                PrintWarning(string.Format("Bad format ({0}) : {1}", ln, line));
                return;
            }
            int proj1, sen1, wpos1, proj2, sen2, wpos2;
            if (!Int32.TryParse(tokens[0], out proj1)
             || !Int32.TryParse(tokens[1], out sen1)
             || !Int32.TryParse(tokens[2], out wpos1)
             || !Int32.TryParse(tokens[3], out proj2)
             || !Int32.TryParse(tokens[4], out sen2)
             || !Int32.TryParse(tokens[5], out wpos2))
            {
                PrintWarning(string.Format("Parse error ({0}) : {1}", ln, line));
                return;
            }
            int wid1 = FindWordId(proj1, sen1, wpos1);
            int wid2 = FindWordId(proj2, sen2, wpos2);

            using (var cmd = m_Conn.CreateCommand())
            {
                cmd.CommandTimeout = 600;
                cmd.CommandText = string.Format("INSERT INTO word_word (from_word,to_word) VALUES ({0},{1})",
                    wid1, wid2);
                cmd.ExecuteNonQuery();
                if (this.MakeBidirectional)
                {
                    cmd.CommandText = string.Format("INSERT INTO word_word (from_word,to_word) VALUES ({1},{0})",
                        wid1, wid2);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private int FindWordId(int projid, int senid, int wpos)
        {
            using (var cmd = m_Conn.CreateCommand())
            {
                cmd.CommandTimeout = 600;
                cmd.CommandText = string.Format("SELECT id FROM word WHERE project_id={0} AND sentence_id={1} AND position={2}",
                    projid, senid, wpos);
                return (int)(cmd.ExecuteScalar());
            }
        }

        public static void PrintWarning(string s)
        {
            Console.WriteLine("Warning: {0}", s);
        }

        public static void PrintError(string s)
        {
            Console.WriteLine("Error: {0}", s);
        }

        public static void PrintException(Exception ex)
        {
            Console.WriteLine("Exception: {0}", ex.ToString());
        }
    }
}
