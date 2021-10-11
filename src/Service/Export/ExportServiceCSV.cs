using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;
using System.IO;
using ChaKi.Entity.Settings;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Reflection;

namespace ChaKi.Service.Export
{
    public class ExportServiceCSV : ExportServiceBase
    {
        private ExportSetting m_Setting;
        private int m_CurRow;

        private List<string> m_Labels = new List<string>() { "Index", "Corpus", "Document", "CharPos", "SentenceID" };

        public ExportServiceCSV(ExportSetting settings, TextWriter wr)
        {
            m_Setting = settings;
            m_TextWriter = wr;

            // Setup output Columns
            switch (m_Setting.ExportFormat)
            {
                case ExportFormat.Sentence:
                    m_Labels.Add("Sentence");
                    break;
                case ExportFormat.Portion:
                    m_Labels.Add("Left");
                    m_Labels.Add("Center");
                    m_Labels.Add("Right");
                    break;
                case ExportFormat.Word:
                    for (int i = 0; i < 21; i++)	// Left 10 words, Center 1 word, Right 10 words
                    {
                        m_Labels.Add(string.Format("{0}", i - 10));
                    }
                    break;
            }
            //            labels.Add("bib");

            m_CurRow = 0;
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

            m_TextWriter.Write("\"\"");  // RowCol Header

            // Col Headers
            for (int c = 0; c < nCols; c++)
            {
                m_TextWriter.Write(",\"{0}\"", chaccessor(c));
            }
            m_TextWriter.WriteLine();

            // データ行を出力
            for (int r = 0; r < nRows; r++)
            {
                // Row Header
                m_TextWriter.Write("\"{0}\"", rhaccessor(r));
                // Cells
                for (int c = 0; c < nCols; c++)
                {
                    m_TextWriter.Write(",");
                    object o = accessor(r, c);
                    if (o == null)
                    {
                        m_TextWriter.Write(string.Empty);
                    }
                    else
                    {
                        string s = o.ToString();
                        if (s.IndexOf(',') >= 0)
                        {
                            m_TextWriter.Write("\"{0}\"", s);
                        }
                        else
                        {
                            m_TextWriter.Write("{0}", s);
                        }
                    }
                }
                m_TextWriter.WriteLine();
            }
        }

        private void InitializeCSVFile()
        {
            // タイトル行を出力
            // cols = index corpusname position data_cols...
            bool firstcol = true;
            foreach (string label in m_Labels)
            {
                if (!firstcol)
                {
                    m_TextWriter.Write(",");
                }
                m_TextWriter.Write("\"{0}\"", label);
                firstcol = false;
            }
            m_TextWriter.WriteLine();
            m_CurRow++;
        }

        /// <summary>
        /// Export items to CSV File 
        /// (used instead of the default ExportItem, if Excel application is not available)
        /// </summary>
        /// <param name="ki"></param>
        /// <param name="wr"></param>
        public override void ExportItem(KwicItem ki)
        {
            if (m_TextWriter == null) throw new InvalidOperationException("TextWriter is null.");

            if (m_CurRow == 0)
            {
                InitializeCSVFile();
            }

            m_TextWriter.Write("{0},", ki.ID);
            m_TextWriter.Write("\"{0}\",", ki.Crps.Name);
            m_TextWriter.Write("{0},", ki.Document.ID);
            m_TextWriter.Write("{0},", ki.StartCharPos);
            m_TextWriter.Write("{0},", ki.SenID);

            switch (m_Setting.ExportFormat)
            {
                case ExportFormat.Sentence:
                    {
                        string[] portions = new string[3];
                        portions[0] = ki.Left.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        portions[1] = ki.Center.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        portions[2] = ki.Right.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        m_TextWriter.Write("\"{0}\"", string.Join((m_Setting.UseSpacing) ? " " : "", portions));
                    }
                    break;
                case ExportFormat.Portion:
                    {
                        string[] portions = new string[3];
                        portions[0] = ki.Left.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        portions[1] = ki.Center.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        portions[2] = ki.Right.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        m_TextWriter.Write("\"{0}\",", portions[0]);
                        m_TextWriter.Write("\"{0}\",", portions[1]);
                        m_TextWriter.Write("\"{0}\"", portions[2]);
                    }
                    break;
                case ExportFormat.Word:
                    {
                        List<string[]> words = new List<string[]>();
                        words.Add(ki.Left.WordsToStringArray(-10, m_Setting.UseSpacing, m_Setting.WordExportFormat));
                        words.Add(ki.Center.WordsToStringArray(1, m_Setting.UseSpacing, m_Setting.WordExportFormat));
                        words.Add(ki.Right.WordsToStringArray(10, m_Setting.UseSpacing, m_Setting.WordExportFormat));
                        for (int i = 0; i < 10; i++)
                        {
                            m_TextWriter.Write("\"{0}\",", words[0][i]);
                        }
                        m_TextWriter.Write("\"{0}\",", words[1][0]);
                        for (int i = 0; i < 10; i++)
                        {
                            if (i > 0) m_TextWriter.Write(",");
                            m_TextWriter.Write("\"{0}\"", words[2][i]);
                        }
                    }
                    break;
            }

            //          m_TextWriter.Write(",\"{0}\"", ki.Document.GetAttributeString());
            m_TextWriter.WriteLine();
            m_CurRow++;
        }
    }
}

