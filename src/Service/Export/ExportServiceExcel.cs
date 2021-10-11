using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Settings;
using Microsoft.Office.Interop.Excel;
using System.Threading;

namespace ChaKi.Service.Export
{
    public class ExportServiceExcel : ExportServiceBase
    {
        private _Application m_ExcelApp;
        Workbooks m_WorkBooks;
        _Workbook m_WorkBook;
        private ExportSetting m_Setting;
        private _Worksheet m_WorkSheet;
        private int m_CurRow;

        private List<string> m_Labels = new List<string>() { "Index", "Corpus", "Document", "CharPos", "SentenceID" };

        public ExportServiceExcel(ExportSetting settings)
        {
            m_Setting = settings;

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
            if (m_WorkSheet != null) Marshal.ReleaseComObject(m_WorkSheet);
            if (m_WorkBook != null) Marshal.ReleaseComObject(m_WorkBook);
            if (m_WorkBooks != null) Marshal.ReleaseComObject(m_WorkBooks);
            if (m_ExcelApp != null) Marshal.ReleaseComObject(m_ExcelApp);
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        private void InitializeExcelWorkSheet()
        {
            try
            {
                m_ExcelApp = (_Application)Marshal.GetActiveObject("Excel.Application");
            }
            catch
            {
            }
            if (m_ExcelApp == null)
            {
                m_ExcelApp = new Application();
            }
            m_ExcelApp.Visible = true;

            m_ExcelApp.SheetsInNewWorkbook = 1;
            m_WorkBooks = m_ExcelApp.Workbooks;
            m_WorkBook = m_WorkBooks.Add(Missing.Value);
            m_WorkSheet = (_Worksheet)m_WorkBook.ActiveSheet;

            // Worksheet内容の初期化
            // cols = index corpusname position data_cols...
            int c = 1;
            foreach (string label in m_Labels)
            {
                SetCellData(m_CurRow, c++, label);
            }
            m_CurRow++;
        }

        /// <summary>
        /// Export items to Excel by COM Automation (default)
        /// </summary>
        /// <param name="ki"></param>
        public override void ExportItem(KwicItem ki)
        {
            if (m_CurRow == 0)
            {
                m_CurRow = 1;
                InitializeExcelWorkSheet();
            }

            if (m_WorkSheet == null)
            {
                return;
            }

            int c = 1;
            SetCellData(m_CurRow, c++, string.Format("{0}", ki.ID));
            SetCellData(m_CurRow, c++, string.Format("{0}", ki.ID));
            SetCellData(m_CurRow, c++, ki.Crps.Name);
            SetCellData(m_CurRow, c++, string.Format("{0}", ki.Document.ID));
            SetCellData(m_CurRow, c++, string.Format("{0}", ki.StartCharPos));
            SetCellData(m_CurRow, c++, string.Format("{0}", ki.SenID));

            switch (m_Setting.ExportFormat)
            {
                case ExportFormat.Sentence:
                    {
                        string[] portions = new string[3];
                        portions[0] = ki.Left.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        portions[1] = ki.Center.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        portions[2] = ki.Right.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        SetCellData(m_CurRow, c++, string.Join((m_Setting.UseSpacing) ? " " : "", portions));
                    }
                    break;
                case ExportFormat.Portion:
                    {
                        string[] portions = new string[3];
                        portions[0] = ki.Left.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        portions[1] = ki.Center.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        portions[2] = ki.Right.ToString2(m_Setting.UseSpacing, m_Setting.WordExportFormat);
                        SetCellData(m_CurRow, c++, portions[0]);
                        SetCellData(m_CurRow, c++, portions[1]);
                        SetCellData(m_CurRow, c++, portions[2]);
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
                            SetCellData(m_CurRow, c++, words[0][i]);
                        }
                        SetCellData(m_CurRow, c++, words[1][0]);
                        for (int i = 0; i < 10; i++)
                        {
                            SetCellData(m_CurRow, c++, words[2][i]);
                        }
                    }
                    break;
            }

            //            m_WorkSheet.Cells[m_CurRow, c++] = ki.Document.GetAttributeString();

            m_CurRow++;
        }

        private void SetCellData(int r, int c, string data)
        {
            // ワークシートに書き込み中にExcel上でセルクリック等を行うとCOMExceptionとなるため、Retryを行う.
            for (int t = 0; t < 10; t++)
            {
                try
                {
                    m_WorkSheet.Cells[r, c] = data;
                    break;
                }
                catch (COMException)
                {
                    if (t == 9) throw;
                    Thread.Sleep(50);
                }
            }
        }

        /// <summary>
        /// Gridの内容をExcelにエクスポートする.
        /// </summary>
        /// <param name="nRows"></param>
        /// <param name="nCols"></param>
        /// <param name="accessor"></param>
        /// <param name="rhaccessor"></param>
        /// <param name="chaccessor"></param>
        public override void ExportGrid(int nRows, int nCols, GridAccessor accessor, GridHeaderAccessor rhaccessor, GridHeaderAccessor chaccessor, string[] chtags = null)
        {
            m_Labels.Clear();
            m_Labels.Add(string.Empty);
            for (int c = 0; c < nCols; c++)
            {
                m_Labels.Add((chaccessor(c) ?? string.Empty).ToString());
            }
            m_CurRow = 1;
            InitializeExcelWorkSheet();

            // データ行を出力
            for (int r = 0; r < nRows; r++)
            {
                SetCellData(m_CurRow, 1, (rhaccessor(r) ?? string.Empty).ToString());
                for (int c = 0; c < nCols; c++)
                {
                    SetCellData(m_CurRow, c + 2, (accessor(r, c) ?? string.Empty).ToString());
                }
                m_CurRow++;
            }
        }
    }
}
