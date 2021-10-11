using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ChaKi.Entity.Search
{
    public class LexemeCondition : ICloneable
    {
        /// <summary>
        /// Lexemeの属性に対する条件リスト.
        /// </summary>
        // 
        // Dictionaryにしたいが、XmlSerializerで扱えないのでListで我慢する
        //        public Dictionary<string, string> AttrTagPair { get; set; }
        public List<PropertyPair> PropertyPairs { get; set; }

        /// <summary>
        /// Range条件。DepSearchに含まれる場合は使用されない。
        /// </summary>
        public Range RelativePosition
        {
            get { return m_RelativePosition; }
            set { m_RelativePosition = value; }
        }
        private Range m_RelativePosition;

        /// <summary>
        /// DepSearchConditionに含まれる場合の、左Lexemeとの結合条件。TagSearchの場合は使用されない.
        /// 直前のLexemeConditionがあれば、そのRightConnectionと常に一致する。
        /// </summary>
        public char LeftConnection { get; set; }

        /// <summary>
        /// DepSearchConditionに含まれる場合の、右Lexemeとの結合条件。TagSearchの場合は使用されない.
        /// 直後のLexemeConditionがあれば、そのLeftConnectionと常に一致する。
        /// </summary>
        public char RightConnection { get; set; }

        /// <summary>
        /// 中心語に設定されていればtrue.
        /// </summary>
        public bool IsPivot { get; set; }

        public LexemeCondition()
        {
            //            AttrTagPair = new Dictionary<string, string>();
            PropertyPairs = new List<PropertyPair>();
            RelativePosition = new Range(0, 0);
            LeftConnection = ' ';
            RightConnection = ' ';
            IsPivot = false;
        }

        public LexemeCondition(LexemeCondition src)
        {
            m_RelativePosition = new Range(src.m_RelativePosition);
            IsPivot = src.IsPivot;
            PropertyPairs = new List<PropertyPair>();
            LeftConnection = src.LeftConnection;
            RightConnection = src.RightConnection;
            foreach (PropertyPair pp in src.PropertyPairs)
            {
                PropertyPairs.Add(new PropertyPair(pp));
            }
        }

        public object Clone()
        {
            return new LexemeCondition(this);
        }

        public PropertyPair FindProperty(string key)
        {
            foreach (PropertyPair pair in PropertyPairs)
            {
                if (pair.Key == key)
                {
                    return pair;
                }
            }
            return null;
        }

        public void RemoveProperty(string key)
        {
            PropertyPair p = FindProperty(key);
            if (p != null) {
                PropertyPairs.Remove(p);
            }
        }

        public void Add(string key, Property value)
        {
            PropertyPairs.Add(new PropertyPair(key, value));
        }

        public void Reset()
        {
            IsPivot = false;
            PropertyPairs.Clear();
            RelativePosition = new Range();
        }

        public void OffsetRange(int offset, int diff = 0)
        {
            m_RelativePosition = m_RelativePosition.Offset(offset, diff);
        }

        public bool Match(Lexeme lex)
        {
            if (lex == null)
            {
                return true;
            }
            foreach (PropertyPair p in this.PropertyPairs)
            {
                if (p.Key.Equals("Surface"))
                {
                    if (!lex.Surface.Equals(p.Value.StrVal))
                    {
                        return false;
                    }
                }
                else if (p.Key.Equals("Reading"))
                {
                    if (!lex.Reading.Equals(p.Value.StrVal))
                    {
                        return false;
                    }
                }
                else if (p.Key.Equals("Pronunciation"))
                {
                    if (!lex.Pronunciation.Equals(p.Value.StrVal))
                    {
                        return false;
                    }
                }
                else if (p.Key.Equals("BaseLexeme"))
                {
                    if (!lex.BaseLexeme.Surface.Equals(p.Value.StrVal))
                    {
                        return false;
                    }
                }
                if (p.Key.Equals("PartOfSpeech"))
                {
                    if (!lex.PartOfSpeech.Name.Equals(p.Value.StrVal))
                    {
                        return false;
                    }
                }
                else if (p.Key.Equals("CType"))
                {
                    if (!lex.CType.Name.Equals(p.Value.StrVal))
                    {
                        return false;
                    }
                }
                else if (p.Key.Equals("CForm"))
                {
                    if (!lex.CForm.Name.Equals(p.Value.StrVal))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool MatchRegexp(Lexeme lex)
        {
            if (lex == null)
            {
                return true;
            }
            foreach (PropertyPair p in this.PropertyPairs)
            {
                if (p.Key.Equals("Surface"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.Surface, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.Surface.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }

                }
                else if (p.Key.Equals("Reading"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.Reading, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.Reading.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                }
                else if (p.Key.Equals("Pronunciation"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.Pronunciation, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.Pronunciation.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                }
                else if (p.Key.Equals("BaseLexeme"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.BaseLexeme.Surface, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.BaseLexeme.Surface.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                }
                if (p.Key.Equals("PartOfSpeech"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.PartOfSpeech.Name, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.PartOfSpeech.Name.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                }
                else if (p.Key.Equals("CType"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.CType.Name, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.CType.Name.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                }
                else if (p.Key.Equals("CForm"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.CForm.Name, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.CForm.Name.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                }
                else if (p.Key.Equals("Lemma"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.Lemma, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.Lemma.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                }
                else if (p.Key.Equals("LemmaForm"))
                {
                    if (p.Value.IsRegEx)
                    {
                        if (!Regex.IsMatch(lex.LemmaForm, p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!lex.LemmaForm.Equals(p.Value.StrVal))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 別の条件condの内容から、PropertyPairsをマージする。
        /// 同じPropertyに対して双方が条件を持つ場合は、オリジナルを保持する
        /// </summary>
        /// <param name="cond"></param>
        public void Merge(LexemeCondition cond)
        {
            foreach (PropertyPair pair in cond.PropertyPairs)
            {
                if (FindProperty(pair.Key) == null)
                {
                    Add(pair.Key, pair.Value);
                }
            }
        }
    }
}
