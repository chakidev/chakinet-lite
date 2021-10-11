using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using System.Xml.Serialization;

namespace ChaKi.Entity.Kwic
{
    public class KwicWord
    {
        public const int KWA_NONE		= 0x00000000;
        public const int KWA_COMPWORD   = 0x00000001;	// 複合語・複合語の構成語
        public const int KWA_PIVOT      = 0x00000010;
        public const int KWA_HILIGHT    = 0x00000020;
        public const int KWA_SECOND     = 0x00000040;	// 絞込みマッチワード
        public const int KWA_DUMMY = 0x00000080;	// Shift時にできるDummy word


        public KwicWord()
            : this(" ", KWA_DUMMY)
        {
        }

        public KwicWord(Lexeme lex, Word word, int attr)
        {
            this.Lex = lex;
            this.Word = word;
            this.Text = null;
            this.ExtAttr = attr;
        }

        public KwicWord(string text, int attr)
        {
            this.Text = text;
            this.Lex = null;
            this.Word = null;
            this.ExtAttr = attr;
        }

        public int Length
        {
            get
            {
                if (this.Word != null)
                {
                    return this.Word.EndChar - this.Word.StartChar;
                }
                if (this.Lex != null)
                {
                    return this.Lex.Surface.Length;
                }
                if (this.Text != null)
                {
                    return this.Text.Length;
                }
                return 0;
            }
        }


        [XmlIgnore]
        public Lexeme Lex { get; set; }

        [XmlIgnore]
        public Word Word { get; set; }

        public string Text { get; set; }
        public int ExtAttr { get; set; }
    }
}
