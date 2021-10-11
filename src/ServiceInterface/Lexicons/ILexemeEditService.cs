using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Common;

namespace ChaKi.Service.Lexicons
{
    public interface ILexemeEditService
    {
        // Lexeme修正インターフェイス
        void Open(Corpus corpus, Lexeme lex, UnlockRequestCallback callback);
        void Close();
        void Save(Dictionary<string, string> data);

        // Corpusと参照辞書の差分 -> ユーザー辞書のためのインターフェイス
        List<LexemeCorpusBoolLongTuple> ListLexemesNotInRefDic(List<Corpus> corpora, List<string> refdicpaths, IProgress progress);
        void GetLexiconTags(out Dictionary<string, IList<PartOfSpeech>> pos, out Dictionary<string, IList<CType>> ctypes, out Dictionary<string, IList<CForm>> cforms);
        void AddToUserDictionary(List<LexemeCorpusBoolLongTuple> list, string userdicpath);
        void UpdateCorpusInternalDictionaries(List<LexemeCorpusBoolLongTuple> list);

    }
}
