using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Views.KwicView
{
    internal struct CharPosElement
    {
        public WordElement Word;
        public int CharInWord;

        public int WordID
        {
            get
            {
                if (this.Word == null)
                {
                    return -1;
                }
                return this.Word.Index;
            }
        }
        public int CharID
        {
            get
            {
                return this.Word.StartChar + this.CharInWord;
            }
        }

        public bool IsValid
        {
            get
            {
                return (this.Word != null);
            }
        }

        public CharPosElement(WordElement word, int ciw)
        {
            this.Word = word;
            this.CharInWord = ciw;
        }
    }
}
