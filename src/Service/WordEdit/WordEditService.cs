using ChaKi.Common.SequenceMatcher;
using ChaKi.Common.Settings;
using ChaKi.Entity.Corpora;
using ChaKi.Service.DependencyEdit;
using ChaKi.Service.Lexicons;
using System;
using System.Collections.Generic;

namespace ChaKi.Service.WordEdit
{
    public class WordEditService : IDisposable, ILexiconService
    {
        private DepEditContext m_Context;  // OpContextはDepEditService用を共用する.
        private List<DictionaryAccessor> m_RefDics;


        public WordEditService()
        {
            m_RefDics = new List<DictionaryAccessor>();
        }

        public void Open(Corpus cps)
        {
            // コーパスをオープンする.
            m_Context = DepEditContext.Create(cps.DBParam, HandleUnlockRequest, typeof(WordEditService));

            // 参照辞書の設定
            foreach (var item in DictionarySettings.Instance)
            {
                var dict = Dictionary.Create(item.Path, item.Name, item.IsCompoundWordDic);
                var ctx = DictionaryAccessor.Create(dict);
                m_RefDics.Add(ctx);
            }
        }

        public bool HandleUnlockRequest(Type requestingService)
        {
            if (requestingService == typeof(WordEditService))
            {
                // 自分自身からのUnlockリクエストは無視する.
                return true;
            }
            if (true/*!m_Service.CanSave()*/)
            {
                //EndEditing();
                return true;
            }
            //return false;
        }

        public void Commit()
        {
            m_Context.Trans.Commit();
            m_Context.Trans.Dispose();
            m_Context.Trans = null;
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Context != null)
            {
                m_Context.Dispose();
                m_Context = null;
            }
            foreach (var refdic in m_RefDics)
            {
                refdic.Dispose();
            }
            m_RefDics.Clear();
        }

        public void ChangeLexeme(int senid, int wordpos, Lexeme newlex)
        {
            var senMatching = m_Context.Session.CreateQuery(string.Format("from Sentence where ID={0}", senid));
            m_Context.Sen = senMatching.UniqueResult<Sentence>();

            var word = m_Context.Sen.GetWords(m_Context.Proj.ID)[wordpos];
            word.Lex = newlex;

            m_Context.SaveOrUpdate(word);
            m_Context.Flush();
        }

        public void CreateOrUpdateLexeme(ref Lexeme lex, string[] props, string customprop)
        {
            Operation.CreateOrUpdateLexeme(m_Context, ref lex, props, customprop);
        }

        public IList<LexemeCandidate> FindAllLexemeCandidates(string str)
        {
            throw new NotImplementedException();
        }

        public void GetLexiconTags(out Dictionary<string, IList<PartOfSpeech>> pos, out Dictionary<string, IList<CType>> ctypes, out Dictionary<string, IList<CForm>> cforms)
        {
            DepEditService.GetLexiconTags(m_Context.Session, m_RefDics, out pos, out ctypes, out cforms);
        }

        public List<MWE> FindMWECandidates(IList<Word> words, Action<string> showMessageCallback, Action<MWE, MatchingResult> foundMWECallback)
        {
            throw new NotImplementedException();
        }

        public List<MatchingResult> FindMWECandidates2(string surface, IList<Word> words)
        {
            throw new NotImplementedException();
        }
    }
}
