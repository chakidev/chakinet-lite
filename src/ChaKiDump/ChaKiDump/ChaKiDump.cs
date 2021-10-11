using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace ChaKiDump
{
    class ChaKiDump
    {
        public ChaKiDump(string cstr, string corpusname)
        {
            m_Connection = new MySqlConnection(cstr);
            m_Connection.Open();

            string qstr = string.Format("Use {0}", corpusname);
            DbCommand cmd = new MySqlCommand(qstr, m_Connection);
            cmd.ExecuteNonQuery();

            m_Lexicon = new Dictionary<int, string>();
//            m_LexemeLengths = new Dictionary<int, int>();
//            m_SequencePositions = new Dictionary<int, int>();

            m_Bibs = new Dictionary<int, string>();
        }

        public void Close()
        {
            if (m_Connection != null)
            {
                m_Connection.Dispose();
            }
        }

        public void DumpLexicon()
        {
            StringBuilder sb = new StringBuilder();
            sb.Length = 0;
            sb.AppendFormat("SELECT id,morph,reading,pronunciation,base,pos,ctype,cform FROM MORPHS ORDER BY id");
            DbCommand cmd = new MySqlCommand(sb.ToString(), m_Connection);
            using (DbDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    int morphid = (int)rdr[0];
                    string surface = (string)rdr[1];
                    if (surface.Equals("BOS") || surface.Equals("EOS"))
                    {
                        surface = "";
                    }
                    if (surface.Length > 0)
                    {
                        string morph =
                            string.Format("{0}\t{1},{2},{3},{4},{5},{6}", 
                                surface,
                                MakePOS( (string)rdr[5] ),
                                (string)rdr[6],
                                (string)rdr[7],
                                (string)rdr[4], 
                                (string)rdr[2],
                                (string)rdr[3]);
                        m_Lexicon.Add(morphid, morph);
                    }
                }
            }
        }

        public void DumpBibs()
        {
            StringBuilder sb = new StringBuilder();
            sb.Length = 0;
            sb.AppendFormat("SELECT id,bib FROM bibs ORDER BY id");
            DbCommand cmd = new MySqlCommand(sb.ToString(), m_Connection);
            using (DbDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    int id = (int)rdr[0];
                    string bib = (string)rdr[1];
                    if (bib == null)
                    {
                        continue;
                    }
                    m_Bibs.Add(id, bib);
                    Console.WriteLine("#! DOCID\t{0}\t{1}", id, bib);
                }
            }
        }

        private static char[] separator = new char[] { '-' };

        public static string MakePOS(string txt)
        {
            string s = txt+"-*-*-*-*";
            string[] tags = s.Split(separator);
            return string.Join(",", tags, 0, 4);
        }

        public void DumpSentence()
        {
            StringBuilder sb = new StringBuilder();
            sb.Length = 0;
            sb.AppendFormat("SELECT DISTINCT senid FROM sequence ORDER BY senid ASC");
            DbCommand cmd = new MySqlCommand(sb.ToString(), m_Connection);
            List<int> senids = new List<int>();
            using (DbDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    senids.Add((int)rdr[0]);
                }
            }

            int curSeq = 0;
            foreach (int senid in senids)
            {
                int curBunsetsu = -1;

                // get bibid for each sentence
                sb.Length = 0;
                sb.AppendFormat("SELECT bibid FROM sens WHERE id={0}", senid);
                cmd = new MySqlCommand(sb.ToString(), m_Connection);
                int bibid = (int)cmd.ExecuteScalar();
                Console.WriteLine("#! DOC {0}", bibid);

                // get bunsetsu list in this sentence
                sb.Length = 0;
                sb.AppendFormat("SELECT morphid,bid,depid,deprel FROM sequence WHERE senid={0} ORDER BY id ASC", senid);
                cmd = new MySqlCommand(sb.ToString(), m_Connection);
                using (DbDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        // 文節の処理
                        int bid = (int)rdr[1];
                        if (bid != -1 && curBunsetsu != bid)
                        {
                            Console.WriteLine( "* {0} {1}{2} 0/0 0.0", (int)rdr[1]-1, (int)rdr[2]-1, (string)rdr[3] );
                            curBunsetsu = bid;
                        }

                        // 文＝LexemeID列 の出力
                        int morphid = (int)rdr[0];
                        string morph;
                        if (!m_Lexicon.TryGetValue(morphid, out morph) || morph.Length == 0)
                        {
                            continue;
                        }
                        curSeq++;

                        Console.WriteLine(morph);
                    }
                    Console.WriteLine("EOS");
                }
            }
        }


        private MySqlConnection m_Connection = null;

        // Cache table from morphid to its length (of chars, not bytes)
//        private Dictionary<int, int> m_LexemeLengths;

        // Cache table from seqid to position (of chars)
//        private Dictionary<int, int> m_SequencePositions;

        private Dictionary<int, string> m_Lexicon;

        private Dictionary<int, string> m_Bibs;

    }
}
