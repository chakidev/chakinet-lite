using System;
using System.Collections.Generic;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using ChaKi.Service.Readers;
using System.Data.Common;
using ChaKi.Entity.Corpora.Annotations;

namespace CreateCorpus
{
    public class CreateCorpus
    {
        public string path = null;
        public string corpusName = null;
        public string textEncoding = "SHIFT_JIS";
        public string textType = "auto";

        private DBService m_Service = null;
        private Corpus m_Corpus = null;
        private string m_DefaultString;

        public bool ParseArguments(string[] args)
        {
            int n = 0;
            foreach (string arg in args)
            {
                if (arg.Length > 1 && arg.StartsWith("-"))
                {
                    string[] tokens = arg.Substring(1).Split(new char[] { '=' });
                    if (tokens.Length < 2 || (!tokens[0].Equals("e") && !tokens[0].Equals("t")))
                    {
                        Console.WriteLine("Invalid option: {0}", arg);
                        return false;
                    }
                    if (tokens[0].Equals("e"))
                    {
                        this.textEncoding = tokens[1];
                    }
                    else if (tokens[0].Equals("t"))
                    {
                        string token = tokens[1].ToLower();
                        string[] subtypes = token.Split(new char[] { '|' });
                        if (subtypes.Length > 0)
                        {
                            this.textType = subtypes[0];
                        }
                        else
                        {
                            this.textType = token;
                        }
                    }
                }
                else
                {
                    if (n == 0)
                    {
                        this.path = arg;
                    }
                    else if (n == 1)
                    {
                        this.corpusName = arg;
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

        public void ParseInput()
        {
            // Corpusの作成
            m_Corpus = Corpus.CreateFromFile(corpusName);
            if (m_Corpus == null)
            {
                Console.WriteLine("Error: Cannot create Corpus. Maybe <out file>'s extension is neither .db nor .def");
                return;
            }

            // DB初期化のためのサービスを得る
            m_Service = DBService.Create(m_Corpus.DBParam);
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

            // CaboChaファイル読込み
            Console.WriteLine("Reading {0}", path);
            try
            {
                CorpusSourceReader reader = null;
                if (textType.Equals("auto"))
                {
                    reader = CorpusSourceReaderFactory.Create(path, textEncoding, m_Corpus);
                    Console.WriteLine("Using {0}", reader.GetType().Name);
                }
                else if (textType.Equals("chasen"))
                {
                    reader = new CabochaChasenReader(m_Corpus);
                }
                else if (textType.Equals("mecab"))
                {
                    reader = new CabochaMecabReader(m_Corpus);
                }
                else
                {
                    Console.WriteLine("Invalid Reader Type: {0}", textType);
                    return;
                }
                reader.ReadFromFile(path, textEncoding);
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return;
            }
        }

        public void SaveLexicon()
        {
            // Native DB Connection
            Console.WriteLine("\n\nSaving to database {0}", corpusName);
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
                        pos.ID = n++;
                        cmd.CommandText = string.Format("INSERT INTO parts_of_speech VALUES({0},'{1}','{2}','{3}','{4}','{5}')",
                            pos.ID, pos.Name1, pos.Name2, pos.Name3, pos.Name4, pos.Name);
                        cmd.ExecuteNonQuery();
                        Console.Write("> {0}\r", pos.ID+1);
                    }
                    Console.WriteLine("\x0a Saving CTypes...");
                    n = 0;
                    foreach (CType ctype in m_Corpus.Lex.CTypes)
                    {
                        ctype.ID = n++;
                        cmd.CommandText = string.Format("INSERT INTO ctypes VALUES({0},'{1}','{2}','{3}')",
                            ctype.ID, ctype.Name1, ctype.Name2, ctype.Name);
                        cmd.ExecuteNonQuery();
                        Console.Write("> {0}\r", ctype.ID+1);
                    }
                    Console.WriteLine("\x0a Saving CForms...");
                    n = 0;
                    foreach (CForm cform in m_Corpus.Lex.CForms)
                    {
                        cform.ID = n++;
                        cmd.CommandText = string.Format("INSERT INTO cforms VALUES({0},'{1}')",
                            cform.ID, cform.Name);
                        cmd.ExecuteNonQuery();
                        Console.Write("> {0}\r", cform.ID+1);
                    }
                    Console.WriteLine("\x0a Saving Lexemes...");
                    n = 0;
                    foreach (Lexeme lex in m_Corpus.Lex)
                    {
                        lex.ID = n++;
                        cmd.CommandText = string.Format("INSERT INTO lexemes VALUES ({0},'{1}','{2}','{3}',{4},{5},{6},{7},{8},{9})",
                            lex.ID,
                            lex.Surface.Replace("'", "''"),
                            lex.Reading.Replace("'", "''"),
                            lex.Pronunciation.Replace("'", "''"),
                            lex.BaseLexeme.ID,
                            lex.PartOfSpeech.ID, lex.CType.ID, lex.CForm.ID, m_DefaultString, lex.Frequency);
                        cmd.ExecuteNonQuery();
                        Console.Write("> {0}\r", lex.ID+1);
                    }
                    trans.Commit();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            Console.WriteLine("\x0aLexicon written.");
        }

        public void SaveSentences()
        {
            // Sentenceをセーブする
            Console.WriteLine("\nSaving Sentences...");
            SimpleTransactionManager trans = null;
            DbCommand cmd = null;
            try
            {
                trans = new SimpleTransactionManager(m_Service);
                trans.Begin();
                cmd = trans.Cmd;

                int senid = 0;
                int wordid = 0;
                int bunsetsuid = 0;
                foreach (Sentence sen in m_Corpus.Sentences)
                {
                    sen.ID = senid++;
                    cmd.CommandText = string.Format("INSERT INTO sentences VALUES({0},{1},{2},{3})",
                        sen.ID, sen.StartChar, sen.EndChar, sen.ParentDoc);
                    cmd.ExecuteNonQuery();

                    foreach (Bunsetsu buns in sen.Bunsetsus)
                    {
                        // この文に属する文節すべてのIDを先に確定させておく。
                        buns.ID = bunsetsuid++;
                    }

                    foreach (Word word in sen.Words)
                    {
                        word.ID = wordid++;
                        cmd.CommandText = string.Format("INSERT INTO words VALUES ({0},{1},{2},{3},{4},{5},{6},{7})",
                            word.ID, word.Sen.ID, word.StartChar, word.EndChar, word.Lex.ID,
                            word.Bunsetsu.ID, m_DefaultString, word.Pos);
                        cmd.ExecuteNonQuery();
                    }

                    foreach (Bunsetsu buns in sen.Bunsetsus)
                    {
                        try
                        {
                            cmd.CommandText = string.Format("INSERT INTO bunsetsus VALUES({0},{1},{2},{3},'{4}')",
                                buns.ID, buns.Pos, buns.Sen.ID,
                                (buns.DependsTo == null) ? "null" : buns.DependsTo.ID.ToString(),
                                buns.DependsAs);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
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
                int segid = 0;
                Console.WriteLine("\nSaving Segments...");
                foreach (Segment seg in m_Corpus.Segments)
                {
                    seg.ID = segid++;
                    cmd.CommandText = string.Format("INSERT INTO segments VALUES({0},{1},{2},{3},'{4}')",
                       seg.ID, seg.StartChar, seg.EndChar, seg.Tag.ID, seg.Comment);
                    cmd.ExecuteNonQuery();
                    if (segid > 0 && segid % 500 == 0)
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
                int linkid = 0;
                foreach (Link lnk in m_Corpus.Links)
                {
                    lnk.ID = linkid++;
                    cmd.CommandText = string.Format("INSERT INTO links VALUES({0},{1},{2},{3},{4},{5},'{6}')",
                        lnk.ID, lnk.From.ID, lnk.To.ID,
                        lnk.IsDirected ? 1 : 0, lnk.IsTransitive ? 1 : 0,
                        lnk.Tag.ID, lnk.Comment);
                    cmd.ExecuteNonQuery();
                    if (linkid > 0 && linkid % 500 == 0)
                    {
                        trans.CommitAndContinue();
                        cmd = trans.Cmd;
                        Console.Write("> {0} Committed.\r", lnk.ID + 1);
                    }
                }
                Console.WriteLine("> {0} Committed.", m_Corpus.Links.Count);
                trans.Commit();
                Console.Write("\n");
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
                if (trans != null)
                {
                    trans.Dispose();
                }
            }
        }

#if false
            NHibernate.Cfg.Configuration cfg = new Configuration();
            cfg.SetProperties(new Dictionary<string, string>());
            svc.SetupConnection(cfg);
            cfg.AddAssembly("ChaKiEntity");
#endif

        public void UpdateIndex()
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
            Console.WriteLine("Done.");
        }

        public static void PrintException(Exception ex)
        {
            Console.WriteLine("Exception: {0}", ex.ToString());
        }
    }


}
