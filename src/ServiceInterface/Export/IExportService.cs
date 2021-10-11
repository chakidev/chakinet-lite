using System.Collections.Generic;
using System.IO;
using ChaKi.Entity.Kwic;
using System.Xml;
using System;
using ChaKi.Entity.Corpora;

namespace ChaKi.Service.Export
{
    public interface IExportService : IDisposable
    {
        void Export(List<KwicItem> items, int projid);
        void ExportCorpus(Corpus crps, int projid, ref bool cancelFlag, Action<int, object> progressCallback);

        void ExportGrid(int nRows, int nCols, GridAccessor accessor, GridHeaderAccessor rhaccessor, GridHeaderAccessor chaccessor, string[] chtags = null);
    }

    public delegate object GridAccessor(int r, int c);
    public delegate object GridHeaderAccessor(int i);
}
