using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using System.Xml;
using System.IO;
using System.Data.Common;
using ChaKi.Entity.Corpora.Annotations;

namespace AttachBib
{
    public class AttachBib
    {
        public string InputPath = null;
        public string CorpusName = null;

        private DBService m_Service = null;
        private Corpus m_Corpus = null;
        private string m_DefaultString;

        private Dictionary<string, int> m_DocTable = new Dictionary<string, int>();
        private int m_tagid = 0;

        public bool ParseArguments(string[] args)
        {
            int n = 0;
            foreach (string arg in args)
            {
                if (arg.Length > 1 && arg.StartsWith("-"))
                {
                    string[] tokens = arg.Substring(1).Split(new char[] { '=' });
                    if (true/*!(tokens.Length == 1 && tokens[0].Equals("d"))
                     && !(tokens.Length == 2 && (tokens[0].Equals("e")))*/)
                    {
                        Console.WriteLine("Invalid option: {0}", arg);
                        return false;
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

        public void Execute()
        {
            if (this.InputPath == null || this.CorpusName == null)
            {
                return;
            }
            if (!OpenDatabase())
            {
                return;
            }
            if (!AssignBibs())
            {
                return;
            }
        }

        private bool OpenDatabase()
        {
            try
            {
                m_Corpus = Corpus.CreateFromFile(this.CorpusName);
                if (m_Corpus == null)
                {
                    Console.WriteLine("Error: Cannot Open Corpus. Maybe <out file>'s extension is neither .db nor .def");
                    return false;
                }

                // DB直接操作のためのサービスを得る
                m_Service = DBService.Create(m_Corpus.DBParam);
                m_DefaultString = m_Service.GetDefault(); // INSERT文のDEFAULT文字列

                //  Documentをすべて取得し、Name -> IDのHashtableを作成する
                using (DbConnection conn = m_Service.GetDbConnnection())
                {
                    conn.Open();
                    DbCommand cmd = conn.CreateCommand();
                    int n = 0;
                    // FileName属性があればそれをキーとする
                    cmd.CommandText = "SELECT document_id, description FROM documenttag where tag='FileName'";
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int docid = reader.GetInt32(0);
                            string val = reader.GetString(1);
                            if (!m_DocTable.ContainsKey(val))
                            {
                                m_DocTable[val] = docid;
                                n++;
                            }
                            else
                            {
                                Console.WriteLine("Conflicting value in documenttag: FileName='{0}': Ignored.", val);
                            }
                        }
                    }
                    // FileName属性がなく、Bib_ID属性がある場合は後者をキーとする
                    // （次に続く書き込み処理時に、Bib_ID値をFileName属性へコピーする.）
                    cmd.CommandText = "SELECT document_id, description FROM documenttag where tag='Bib_ID'";
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int docid = reader.GetInt32(0);
                            string val = reader.GetString(1);
                            if (!m_DocTable.ContainsKey(val))
                            {
                                m_DocTable[val] = docid;
                                n++;
                            }
                            else
                            {
                                Console.WriteLine("Conflicting value in documenttag: Bib_ID='{0}': Ignored.", val);
                            }
                        }
                    }
                    Console.WriteLine("Number of Tagged Documents = {0}", n);

                    // documenttagの次のID番号を得ておく.
                    cmd.CommandText = "SELECT MAX(id) from documenttag";
                    m_tagid = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
//                    Console.WriteLine("Current ID of DocumentTag = {0}", m_tagid);
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            return true;
        }

        private bool AssignBibs()
        {
            DbConnection conn = null;
            DbTransaction trans = null;
            try
            {
                // DB Transactionを開始する
                conn = m_Service.GetDbConnnection();
                conn.Open();
                trans = conn.BeginTransaction();

                // XMLファイルを開く
                XmlReaderSettings setting = new XmlReaderSettings()
                {
                    IgnoreWhitespace = true,
                    IgnoreComments = true,
                    ConformanceLevel = ConformanceLevel.Document
                };
                using (Stream stream = new FileStream(this.InputPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                using (XmlReader xrdr = XmlReader.Create(stream, setting))
                {
                    xrdr.MoveToContent();
                    string docname;
                    int n = 0;
                    while (!xrdr.EOF)
                    {
                        if (xrdr.NodeType == XmlNodeType.Element && xrdr.Name == "Bib")
                        {
                            docname = xrdr.GetAttribute("FileName");
                            string fragmentData = xrdr.ReadInnerXml();
                            Console.Write("FileName: {0} ...", docname);
                            List<DocumentAttribute> attrs = XmlFragmentToAttributes(fragmentData);
                            n++;
                            int docid;
                            if (docname == null)
                            {
                                Console.WriteLine("\n> Warning: Missing FileName attribute; at Bib Element #{0}", n);
                                continue;
                            }
                            if (!m_DocTable.TryGetValue(docname, out docid))
                            {
                                Console.WriteLine("\n> Warning: Cannot find Document for the FileName attribute; at Bib Element #{0}", n);
                                continue;
                            }
                            attrs.Add(new DocumentAttribute() { Key = "FileName", Value = docname });
                            AssignBibToDocument(conn, docid, attrs);
                            Console.WriteLine("OK");
                            continue;
                        }
                        if (!xrdr.Read()) break;
                    }
                }

                trans.Commit();
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            finally
            {
                if (trans != null) trans.Dispose();
                if (conn != null) conn.Close();
            }
            return true;
        }

        private void AssignBibToDocument(DbConnection conn, int docid, List<DocumentAttribute> attrs)
        {
            foreach (DocumentAttribute dt in attrs)
            {
                dt.ID = m_tagid++;
                try
                {
                    DbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = string.Format("DELETE FROM documenttag WHERE document_id={0} AND tag='{1}'", docid, dt.Key);
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = string.Format("INSERT INTO documenttag VALUES({0},'{1}','{2}',{3},{4})",
                                    dt.ID, dt.Key, dt.Value.Replace("'", "''"), docid, m_DefaultString);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    PrintException(ex);
                }
            }
        }

        private List<DocumentAttribute> XmlFragmentToAttributes(string fragmentData)
        {
            List<DocumentAttribute> res = new List<DocumentAttribute>();

            string s = string.Format("<Root>{0}</Root>", fragmentData);
            using (TextReader trdr = new StringReader(s))
            {
                XmlReader xrdr = XmlReader.Create(trdr);
                while (xrdr.Read())
                {
                    if (xrdr.Name.Equals("Root")) continue;
                    DocumentAttribute dt = new DocumentAttribute();
                    dt.ID = 0;
                    dt.Key = xrdr.Name;
                    dt.Value = xrdr.ReadString();
                    res.Add(dt);
                }
                xrdr.Close();
            }
            return res;
        }

        public static void PrintException(Exception ex)
        {
            Console.WriteLine("Exception: {0}", ex.ToString());
        }
    }
}
