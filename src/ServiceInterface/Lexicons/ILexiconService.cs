using ChaKi.Common.SequenceMatcher;
using ChaKi.Entity.Corpora;
using System;
using System.Collections.Generic;

namespace ChaKi.Service.Lexicons
{
    public interface ILexiconService
    {
        /// <summary>
        /// Surfaceがstrに一致するLexemeをすべて得る。
        /// </summary>
        /// <param name="str"></param>
        IList<LexemeCandidate> FindAllLexemeCandidates(string str);

        /// <summary>
        /// propsで与えられた内容を元に既存のLexemeを更新または新たに生成してDBに登録する.
        /// Base, POS, CForm, CTypeも必要に応じて登録する.
        /// </summary>
        /// <param name="lex">既存ならID >= 0, 新語ならID &lt; 0</param>
        /// <param name="props">LPの順に並べられたProperty文字列の配列</param>
        /// <param name="customprops">新語の場合、CustomPropertyに設定する文字列</param>
        void CreateOrUpdateLexeme(ref Lexeme lex, string[] props, string customprop);

        /// <summary>
        /// 参照用辞書を含め、全ての使用可能なPOS, CType, CFormタグのリストを得る.
        /// stringキーは辞書名（カレントコーパスについては"Default"という名称とする）.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="ctypes"></param>
        /// <param name="cforms"></param>
        void GetLexiconTags(out Dictionary<string, IList<PartOfSpeech>> pos, out Dictionary<string, IList<CType>> ctypes, out Dictionary<string, IList<CForm>> cforms);


        /// <summary>
        /// strで開始されるMWEを複合語辞書(Cradle)から検索する.
        /// </summary>
        /// <param name="str"></param>
        List<MWE> FindMWECandidates(IList<Word> words, Action<string> showMessageCallback = null, Action<MWE, MatchingResult> foundMWECallback = null);

        /// <summary>
        /// surfaceを含み、word list（本文）の一部になりうるMWEをすべて辞書から検索し、
        /// word listとのマッチング結果を返す.
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        List<MatchingResult> FindMWECandidates2(string surface, IList<Word> words);
    }
}
