using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace ChaKi.Entity.Corpora
{
    public class Lexicon : List<Lexeme>
    {
        private Dictionary<int, Lexeme> m_Dictionary;
        public List<PartOfSpeech> PartsOfSpeech { get; set; }
        public List<CType> CTypes { get; set; }
        public List<CForm> CForms { get; set; }
        public string UniqueID { get; set; }

        public Lexicon()
        {
            m_Dictionary = new Dictionary<int, Lexeme>();
            this.PartsOfSpeech = new List<PartOfSpeech>();
            this.CTypes = new List<CType>();
            this.CForms = new List<CForm>();
            this.UniqueID = string.Empty;
        }

        public void Reset()
        {
            Clear();
            m_Dictionary.Clear();
            this.PartsOfSpeech.Clear();
            this.CTypes.Clear();
            this.CForms.Clear();
        }

        /// <summary>
        /// LexemeÇí«â¡Ç∑ÇÈ
        /// </summary>
        /// <param name="item"></param>
        public new void Add(Lexeme item)
        {
            if (m_Dictionary.ContainsKey(item.ID))
            {
                return;
            }
            base.Add(item);
            m_Dictionary.Add(item.ID, item);
        }


        /// <summary>
        /// Index (Lex.ID) Ç©ÇÁLexemeÇíTÇ∑ÅB
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool TryGetLexeme(int id, out Lexeme lex)
        {
            lex = null;
            return m_Dictionary.TryGetValue(id, out lex);
        }

        /// <summary>
        /// å¥å`Ç©ÇÁLexemeÇíTÇ∑ÅBÇ»ÇØÇÍÇŒìoò^Ç∑ÇÈ(?)
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="pos"></param>
        /// <param name="ct"></param>
        /// <param name="cf"></param>
        /// <returns></returns>
        public Lexeme GetBaseLexeme(string s)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// EntriesÇëÆê´ÇÃÇ¢Ç∏ÇÍÇ©Ç…ÇÊÇ¡ÇƒÉ\Å[ÉgÇ∑ÇÈ
        /// </summary>
        /// <param name="lp"></param>
        /// <param name="isAscending"></param>
        public void Sort(int keyIndex, bool isAscending)
        {
            Lexeme.CompareKey = keyIndex;
            Lexeme.CompareAscending = isAscending;
            base.Sort();
        }
    }
}
