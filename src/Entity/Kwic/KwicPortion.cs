using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Settings;

namespace ChaKi.Entity.Kwic
{
    public class KwicPortion
    {
        public KwicItem Parent;

        public KwicPortion(KwicItem parent)
        {
            this.Words = new List<KwicWord>();
            this.Parent = parent;
        }

        public List<KwicWord> Words { get; set; }

        public void AddLexeme(Lexeme lex, Word word, int attr)
        {
            this.Words.Add(new KwicWord(lex, word, attr));
        }

        public void AddText(string text, int attr)
        {
            this.Words.Add(new KwicWord(text, attr));
            this.Parent.IsSimple = true;
        }

        public static int Compare(KwicPortion x, KwicPortion y, bool isLeftPortion)
        {
            int n1 = x.Words.Count;
            int n2 = y.Words.Count;
            int n = Math.Min(n1, n2);

            if (!isLeftPortion)
            {
                for (int i = 0; i < n; i++)
                {
                    string s1 = null;
                    string s2 = null;
                    Lexeme w1 = x.Words[i].Lex;
                    if (w1 != null)
                    {
                        s1 = w1.GetStringProperty(LP.Surface);
                    }
                    else
                    {
                        s1 = x.Words[i].Text;
                    }
                    Lexeme w2 = y.Words[i].Lex;
                    if (w2 != null)
                    {
                        s2 = w2.GetStringProperty(LP.Surface);
                    }
                    else
                    {
                        s2 = y.Words[i].Text;
                    }
                    if (s1 == null || s2 == null)
                    {
                        throw new ArgumentNullException("Cannot compare KWIC Portions");
                    }
                    int c = string.CompareOrdinal(s1, s2);
                    if (c != 0)
                    {
                        return c;
                    }
                }
                // n�܂ł͂܂���������
                return (n1 - n2);   // n1�̕����������+, n2�̕����������-
            }
            else
            {
                // ��납���r����ȊO�͂܂���������
                for (int i = 0; i < n; i++)
                {
                    string s1 = null;
                    string s2 = null;
                    Lexeme w1 = x.Words[n1 - i - 1].Lex;
                    if (w1 != null)
                    {
                        s1 = w1.GetStringProperty(LP.Surface);
                    }
                    else
                    {
                        s1 = x.Words[n1 - i - 1].Text;
                    }
                    Lexeme w2 = y.Words[n2 - i - 1].Lex;
                    if (w2 != null)
                    {
                        s2 = w2.GetStringProperty(LP.Surface);
                    }
                    else
                    {
                        s2 = y.Words[n2 - i - 1].Text;
                    }
                    if (s1 == null || s2 == null)
                    {
                        throw new ArgumentNullException("Cannot compare KWIC Portions");
                    }
                    int c = string.CompareOrdinal(s1, s2);
                    if (c != 0)
                    {
                        return c;
                    }
                }
                return (n1 - n2);   // n1�̕����������+, n2�̕����������-
            }
            return 0;
        }

        public int Count
        {
            get { return this.Words.Count; }
        }

        public void PushFront( KwicWord w)
        {
            this.Words.Insert(0, w);
        }

        public void PushBack(KwicWord w)
        {
            this.Words.Add(w);
        }

        public KwicWord PopFront()
        {
            if (this.Words.Count > 0)
            {
                KwicWord w = this.Words[0];
                this.Words.RemoveAt(0);
                return w;
            }
            return null;
        }

        public KwicWord PopBack()
        {
            if (this.Words.Count > 0)
            {
                KwicWord w = this.Words[this.Words.Count - 1];
                this.Words.RemoveAt(this.Words.Count - 1);
                return w;
            }
            return null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KwicWord w in this.Words)
            {
                if (sb.Length > 0)
                {
                    sb.Append(";");
                }
                if (w.Lex != null)
                {
                    sb.Append(w.Lex.ID);
                }
                else
                {
                    sb.Append("\"");
                    sb.Append(w.Text);
                    sb.Append("\"");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Excel Export�p�̌`����KwicPortion��string������
        /// </summary>
        /// <param name="bUseSpacing">true �Ȃ�΁A�eword�̊Ԃ�space��}��</param>
        /// <param name="convType">true �Ȃ�΁Aword/POS �̌`���ŏo�́ibSpacing�͋����I��true�j</param>
        /// <returns></returns>
        public string ToString2(bool bUseSpacing, WordToStringConversionFormat convType)
        {
            if (convType != WordToStringConversionFormat.Short)
            {
                bUseSpacing = true;
            }
            StringBuilder sb = new StringBuilder();
            foreach (KwicWord w in this.Words)
            {
                sb.Append(WordToStringArray(w, convType));
                if (bUseSpacing)
                {
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Excel Export�p�̌`����KwicPortion�Ɋ܂܂��Word List��string[]�ɕϊ�����
        /// </summary>
        /// <param name="maxCount">�ϊ�����ő�J�E���g�i���̒l�ł���΁A�t���Ɏ��j</param>
        /// <param name="bUseSpacing">true �Ȃ�΁A�eword�̊Ԃ�space��}��</param>
        /// <param name="convType">true �Ȃ�΁Aword/POS �̌`���ŏo�́ibSpacing�͋����I��true�j</param>
        /// <returns></returns>
        public string[] WordsToStringArray(int maxCount, bool bUseSpacing, WordToStringConversionFormat convType)
        {
            if (convType != WordToStringConversionFormat.Short)
            {
                bUseSpacing = true;
            }
            if (maxCount == 0) {
                throw new Exception("KwicPortion.WordsToStringArray() - count must not be zero");
            }

            int aMax = Math.Abs(maxCount);
            string[] result = new string[aMax];
            if (maxCount > 0)
            {
                for (int i = 0; i < aMax; i++)
                {
                    if (i < this.Words.Count)
                    {
                        KwicWord w = this.Words[i];
                        result[i] = WordToStringArray(w, convType);
                    }
                    else
                    {
                        result[i] = string.Empty;
                    }
                }
            }
            else  // maxCount < 0
            {
                for (int i = 0; i < aMax; i++)
                {
                    if (i < this.Words.Count)
                    {
                        KwicWord w = this.Words[this.Words.Count - i - 1];
                        result[aMax - i - 1] = WordToStringArray(w, convType);
                    }
                    else
                    {
                        result[aMax - i - 1] = string.Empty;
                    }
                }
            }
            return result;
        }

        public static string WordToStringArray(KwicWord w, WordToStringConversionFormat convType)
        {
            if (w.Lex != null)
            {
                switch (convType)
                {
                    case WordToStringConversionFormat.MixPOS:
                        if (w.Word != null && w.Word.StartTime.HasValue && w.Word.EndTime.HasValue && w.Word.Duration.HasValue)
                        {
                            return string.Format("{0}/{1}/{2:F6}/{3:F6}/{4:F6}", w.Lex.Surface, w.Lex.PartOfSpeech.Name,
                                w.Word.StartTime, w.Word.EndTime, w.Word.Duration);
                        }
                        return string.Format("{0}/{1}", w.Lex.Surface, w.Lex.PartOfSpeech.Name);
                    case WordToStringConversionFormat.Full:
                        throw new NotImplementedException();
                    default:
                        return w.Lex.Surface;
                }
            }
            else
            {
                return w.Text;
            }
        }
    }
}
