using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Readers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ChaKi.Common;

namespace ChaKi.Service.Readers
{
    public class CorpusSourceReaderFactory
    {
        public ReaderDefs ReaderDefs { get; private set; }

        private CorpusSourceReaderFactory() { }

        private static CorpusSourceReaderFactory m_Instance;

        public static CorpusSourceReaderFactory CreateInstance(string readerdefpath)
        {
            m_Instance = new CorpusSourceReaderFactory();
            m_Instance.LoadReaderDef(readerdefpath);
            return m_Instance;
        }

        public static CorpusSourceReaderFactory Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new CorpusSourceReaderFactory();
                    m_Instance.LoadReaderDef();
                }
                return m_Instance;
            }
        }

        /// <summary>
        /// ファイルの先頭を読んでフォーマットを自動推定し、
        /// フォーマットに適したCorpusSourceReaderを生成する。
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encodingName"></param>
        /// <param name="cps"></param>
        /// <returns></returns>
        public CorpusSourceReader Create(string path, ref string readerType, string encodingName, Corpus cps, LexiconBuilder lb)
        {
            // Encodingの推定
            if (encodingName == "Auto")
            {
                encodingName = Utility.GuessTextFileEncoding(path);
                if (encodingName == null)
                {
                    Console.WriteLine(" Could not determine encoding for {0}. Using default utf-8.", path);
                    encodingName = "utf-8";
                } else
                {
                    var enc = Encoding.GetEncoding(encodingName);
                    Console.WriteLine(" Estimated Encoding for \"{0}\" is {1}({2})", Path.GetFileName(path), enc.WebName, enc.EncodingName);
                }
            }
            // Formatの推定
            if (readerType == "Auto")
            {
                readerType = Guess(path, encodingName, cps);
            }
            ReaderDef def = this.ReaderDefs.Find(readerType);
            if (def == null)
            {
                throw new Exception(string.Format("Reader type not found in the definition:{0}", readerType));
            }
            CorpusSourceReader rdr = null;
            if (def.LineFormat == "TabSeparatedLine")
            {
                rdr = new CabochaChasenReader(cps, lb);
            }
            else if (def.LineFormat == "MecabLine")
            {
                rdr = new CabochaMecabReader(cps, lb);
            }
            else if (def.LineFormat == "TextLine")
            {
                rdr = new PlainTextReader(cps);
            }
            else if (def.LineFormat == "CONLL")
            {
                rdr = new ConllReader(cps, lb);
            }
            else if (def.LineFormat == "CONLLU")
            {
                rdr = new ConllUReader(cps, lb);
            }
            else
            {
                throw new Exception(string.Format("Invalid Reader Type: {0}", readerType));
            }
            // 既存のrdr.LexiconBuilderの入力フォーマット(pathとreaderTypeから決まる）を変更する.
            rdr.SetFieldDefs(def.Fields);
            // (推定された)Encodingを保存
            rdr.EncodingToUse = encodingName;
            return rdr;
        }

        private string Guess(string path, string encoding, Corpus cps)
        {
            // 最初の100行までを読んで、以下の特徴フラグをセットする。
            int maxTabsInLine = 0;
            int maxCommasInLine = 0;
            int maxLineLength = 0;
            int column11KanaCount = 0;
            bool hasEOSLine = false;
            bool hasCabochaLine = false;   // "* "で始まる行があるか
            int nonNumColumn0 = 0; // Tab区切りとしたとき0番目のカラムが数字でない行の数（空行を除く）
            int maxTsvColumns = 0;
            Regex numChecker = new Regex(@"\d+");

            using (TextReader streamReader = new StreamReader(path, Encoding.GetEncoding(encoding)))
            {
                int n = 0;
                string s;
                while ((s = streamReader.ReadLine()) != null)
                {
                    maxLineLength = Math.Max(maxLineLength, s.Length);
                    if (s.StartsWith("**")) // Ignore Extdata lines of cabocha
                    {
                        continue;
                    }
                    if (s.StartsWith("*"))
                    {
                        hasCabochaLine = true;
                    }
                    else if (s.StartsWith("EOS"))
                    {
                        hasEOSLine = true;
                    }
                    else
                    {
                        int commas = 0;
                        int tabs = 0;
                        for (int i = 0; i < s.Length; i++)
                        {
                            if (s[i] == ',')
                            {
                                commas++;
                            }
                            else if (s[i] == '\t')
                            {
                                tabs++;
                            }
                        }
                        maxTabsInLine = Math.Max(maxTabsInLine, tabs);
                        maxCommasInLine = Math.Max(maxCommasInLine, commas);
                        var columns = s.Split(',', '\t');
                        if (columns.Length > 11 && columns[11].Length > 0)
                        {
                            if (Regex.IsMatch(columns[11], @"\p{IsKatakana}"))
                            {
                                column11KanaCount++;
                            }
                        }
                    }
                    if (s.Length > 0 && !s.StartsWith("#"))
                    {
                        var tsvFields = s.Split('\t');
                        maxTsvColumns = Math.Max(maxTsvColumns, tsvFields.Length);
                        if (tsvFields.Length > 0)
                        {
                            if (!numChecker.IsMatch(tsvFields[0]))
                            {
                                nonNumColumn0 ++;
                            }
                        }
                    }
                    if (n++ > 100)
                    {
                        break;
                    }
                }
            }

            // 判定
            if (maxTsvColumns == 10 && nonNumColumn0 == 0)
            {
                return "CONLL";
            }
            if ((hasEOSLine||hasCabochaLine) && maxTabsInLine > 3)
            {
                return "ChaSen|Cabocha";
            }
            if ((hasEOSLine || hasCabochaLine) && maxTabsInLine > 0 && maxCommasInLine > 15)
            {
                // 11カラム目にカタカナが多く出現 ->  11=kana -> UniDic 1
                //                          else -> 11=orthBase -> UniDic 2
                if (column11KanaCount > 10)
                {
                    return "Mecab|Cabocha|UniDic1";
                }
                return "Mecab|Cabocha|UniDic2";
            }
            if ((hasEOSLine || hasCabochaLine) && maxTabsInLine > 0 && maxCommasInLine > 2)
            {
                return "Mecab|Cabocha";
            }
            return "PlainText";
        }

        public void LoadReaderDef()
        {
            string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\ReaderDefs.xml";
            LoadReaderDef(path);
        }

        public void LoadReaderDef(string path)
        {
            XmlSerializer ser = new XmlSerializer(typeof(ReaderDefs));
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                this.ReaderDefs = (ReaderDefs)ser.Deserialize(fs);
            }
            // Mapped Tagの出現回数をカウントして複数回指定されるTagにPartNoを付ける.
            foreach (ReaderDef def in this.ReaderDefs.ReaderDef)
            {
                if (def.Fields == null) continue;
                Dictionary<string, int> count = new Dictionary<string, int>();
                foreach (Field f in def.Fields)
                {
                    if (f.MappedTo == null) continue;
                    foreach (MappedTo mapping in f.MappedTo)
                    {
                        if (count.ContainsKey(mapping.Tag))
                        {
                            count[mapping.Tag]++;
                        }
                        else
                        {
                            count[mapping.Tag] = 0;  // Default (分解されないProperty)
                        }
                        mapping.PartNo = count[mapping.Tag];
                    }
                }
                foreach (Field f in def.Fields)
                {
                    if (f.MappedTo == null) continue;
                    // 複数回指定されたTagはPartNoをすべて+1する.
                    foreach (MappedTo mapping in f.MappedTo)
                    {
                        if (count[mapping.Tag] > 0)
                        {
                            mapping.PartNo++;
                        }
                    }
                }
            }
        }
    }
}
