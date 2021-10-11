using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using System.IO;
using ChaKi.Service.Search;
using NHibernate;
using System.Data.Common;

namespace Timings
{
    public class Timings
    {
        public string InputPath = null;
        public string CorpusName = null;
        public string TextEncoding = "SHIFT_JIS";
        public bool DoNotPauseOnExit = false;
        public int ProjectId = 0;
        public bool UseSurface = false;

        private DBService m_Service = null;
        private Corpus m_Corpus = null;
        private string m_DefaultString;
        private DbConnection m_Conn = null;

        private WordEnumerator m_Words;

        public bool ParseArguments(string[] args)
        {
            int n = 0;
            foreach (string arg in args)
            {
                if (arg.Length > 1 && arg.StartsWith("-"))
                {
                    string[] tokens = arg.Substring(1).Split(new char[] { '=' });
                    if (!(tokens.Length == 1 && (tokens[0].Equals("C")))
                     && !(tokens.Length == 1 && (tokens[0].Equals("s")))
                     && !(tokens.Length == 2 && (tokens[0].Equals("e")))
                     && !(tokens.Length == 2 && (tokens[0].Equals("p"))))
                    {
                        Console.WriteLine("Invalid option: {0}", arg);
                        return false;
                    }
                    if (tokens[0].Equals("e"))
                    {
                        this.TextEncoding = tokens[1];
                    }
                    else if (tokens[0].Equals("p"))
                    {
                        int projid;
                        if (!Int32.TryParse(tokens[1], out projid))
                        {
                            Console.WriteLine("Invalid number: {0}", tokens[1]);
                            return false;
                        }
                        this.ProjectId = projid;
                    }
                    else if (tokens[0].Equals("s"))
                    {
                        this.UseSurface = true;
                    }
                    else if (tokens[0].Equals("C"))
                    {
                        this.DoNotPauseOnExit = true;
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

                m_Words = new WordEnumerator(m_Conn, this.ProjectId);

                //while(m_Words.MoveNext())
                //{
                //    Console.WriteLine(m_Words.Current.Value);
                //}

                ReadFile(this.InputPath);

                m_Words.Dispose();

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
                using (var rdr = new StreamReader(path, Encoding.GetEncoding(this.TextEncoding)))
                {
                    string line;
                    int ln = 1;
                    while (true)
                    {
                        line = rdr.ReadLine();
                        if (line == null) break;
                        ProcessLine(ln, line, this.UseSurface);
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

        public void ProcessLine(int ln, string line, bool use_surface)
        {
            var tokens = line.Split('\t');
            if ((use_surface && tokens.Length < 3) || (!use_surface && tokens.Length < 2))
            {
                PrintWarning(string.Format("Bad format ({0}) : {1}", ln, line));
                return;
            }
            double st, et, dt;
            int start_col = use_surface ? 1 : 0;
            int end_col = use_surface ? 2 : 1;
            int duration_col = use_surface ? 3 : 2;
            if (!Double.TryParse(tokens[start_col], out st))
            {
                return;
            }
            if (!Double.TryParse(tokens[end_col], out et))
            {
                return;
            }
            if ((use_surface && tokens.Length > 3) || (!use_surface && tokens.Length > 2))
            {
                if (!Double.TryParse(tokens[duration_col], out dt))
                {
                    return;
                }
            }
            else /* if Length == 3 */
            {
                dt = et - st;
            }
            var surface = use_surface? tokens[0]: null;
            if (!AddRecord(surface, st, et, dt))
            {
                PrintWarning(string.Format("Ignoring unmatched entry ({0}) : {1}", ln, line));
            }

            //Console.WriteLine("{0}, {1}", st, et);
        }
        
        // DB上のカレントWordの表層とsurfaceが一致していれば、そのWordにst, etをストアする
        // ストアできればカレントWordを進め、trueを返す。
        // 但し、surface==nullならば表層の不一致は無視する.
        private bool AddRecord(string surface, double st, double et, double duration)
        {
            var dbitem = m_Words.Current;
            if (surface == null || surface == dbitem.Value)
            {
                using (var cmd = m_Conn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = string.Format("UPDATE word SET start_time={0},end_time={1},duration={2} WHERE id={3}",
                        st, et, duration, dbitem.Key);
                    cmd.ExecuteNonQuery();
                }
                m_Words.MoveNext();
                return true;
            }
//            Console.WriteLine("{0} != {1}", surface, dbitem.Value);
            m_Words.MoveNext();
            return false;
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
