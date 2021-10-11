using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Settings
{
    public enum ExportType
    {
        Excel = 0,
        CSV = 1,
        R = 2,
        Rda = 3,
    }

    public enum ExportFormat
    {
        Sentence = 0,
        Portion = 1,
        Word = 2,
    }

    public enum WordToStringConversionFormat
    {
        Short = 0,
        MixPOS = 1,
        Full = 2,
    }

    public class ExportSetting
    {
        public ExportSetting()
        {
            this.ExportType = ExportType.Excel;
            this.ExportFormat = ExportFormat.Sentence;
            this.WordExportFormat = WordToStringConversionFormat.Short;
            this.UseSpacing = false;
        }

        public ExportSetting(ExportSetting src)
        {
            CopyFrom(src);
        }

        public void CopyFrom(ExportSetting src)
        {
            if (src == null) return;
            this.ExportType = src.ExportType;
            this.ExportFormat = src.ExportFormat;
            this.WordExportFormat = src.WordExportFormat;
            this.UseSpacing = src.UseSpacing;
        }

        public ExportType ExportType { get; set; }
        public ExportFormat ExportFormat { get; set; }
        public WordToStringConversionFormat WordExportFormat { get; set; }
        public bool UseSpacing { get; set; }
    }
}
