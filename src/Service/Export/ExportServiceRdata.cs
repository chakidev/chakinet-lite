using ChaKi.Entity.Settings;
using RDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Export
{
    public class ExportServiceRdata : ExportServiceBase
    {
        private ExportSetting m_Setting;

        public ExportServiceRdata(ExportSetting settings, TextWriter wr)
        {
            m_Setting = settings;
            m_TextWriter = wr;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources.
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gridの内容をTextWriterにCSV形式で出力する.
        /// </summary>
        /// <param name="nRows"></param>
        /// <param name="nCols"></param>
        /// <param name="accessor"></param>
        /// <param name="rhaccessor"></param>
        /// <param name="chaccessor"></param>
        /// <param name="wr"></param>
        public override void ExportGrid(int nRows, int nCols, GridAccessor accessor, GridHeaderAccessor rhaccessor, GridHeaderAccessor chaccessor, string[] chtags = null)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");
            if (chtags == null) throw new InvalidOperationException("Cannot retrieve column info.");

            // Col Headers
            // corpusカラムのTagは、"#" + コーパスNoが付与されている.
            // wordカラムのTagは、"?" + Word Noが付与されている.（これら以外はnull.  cf. WordListView.cs ）
            var tags_t = new char[nCols];
            var tags_n = new int[nCols];
            var ncorpus = 0;
            var nword = 0;

            // カラムのTagを元に、各カラムの意味を解析し、上記の変数に結果を格納する.
            for (int i = 0; i < nCols; i++)
            {
                tags_t[i] = ' ';
                tags_n[i] = -1;
                if (chtags[i] != null)
                {
                    if (chtags[i].Length > 1)
                    {
                        tags_t[i] = chtags[i][0];
                        tags_n[i] = Int32.Parse(chtags[i].Substring(1));
                        if (tags_t[i] == '#')
                        {
                            ncorpus = Math.Max(ncorpus, tags_n[i] + 1);
                        }
                        else if (tags_t[i] == '?')
                        {
                            nword = Math.Max(nword, tags_n[i] + 1);
                        }
                    }
                }
            }

            var word_tokens = new string[nword];
            var corpus_tokens = new string[ncorpus];

            using (var engine = REngine.GetInstance())
            {
                IEnumerable[] columns = new IEnumerable[nCols];
                var df = engine.CreateDataFrame(columns);

                // データ行を出力
                for (int r = 0; r < nRows; r++)
                {
                    for (int i = 0; i < nword; i++) word_tokens[i] = string.Empty;
                    for (int i = 0; i < ncorpus; i++) corpus_tokens[i] = string.Empty;

                    if ("TOTAL".Equals(rhaccessor(r) as string))
                    {
                        continue;
                    }
                    for (int c = 0; c < nCols; c++)
                    {
                        if (tags_t[c] == '#')
                        {
                            corpus_tokens[tags_n[c]] = string.Format("{0}", accessor(r, c));
                        }
                        else if (tags_t[c] == '?')
                        {
                            if (word_tokens[tags_n[c]].Length > 0)
                            {
                                word_tokens[tags_n[c]] += "/";
                            }
                            var s = accessor(r, c) as string;
                            word_tokens[tags_n[c]] += (s == "*") ? string.Empty : s;
                        }
                    }
                    var ss = string.Join("\t", corpus_tokens);
                    Console.WriteLine(ss);
                    m_TextWriter.Write(string.Join("\t", corpus_tokens));
                    m_TextWriter.Write("\t");
                    foreach (var w in word_tokens)
                    {
                        m_TextWriter.Write(string.Format("[{0}]", w));
                    }
                    m_TextWriter.WriteLine();
                }
            }
        }
    }
}
