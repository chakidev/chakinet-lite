using System.IO;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Readers;

namespace ChaKi.Service.Readers
{
    public interface CorpusSourceReader
    {
        Document ReadFromFile(string path, string encoding);
        Document ReadFromFileSLA(string path, string encoding);
        void ReadFromStreamSLA(TextReader rdr, int sentenceCount, Document doc);
        void ReadLexiconFromStream(TextReader rdr, bool baseOnly);
        void ReadLexiconFromStream(TextReader rdr);
        void SetFieldDefs(Field[] fieldDefs);


        LexiconBuilder LexiconBuilder { get; set; }

        string EncodingToUse { get; set; }
    }
}
