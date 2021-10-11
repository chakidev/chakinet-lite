using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Readers
{
    public class ConllUReader : ConllReader
    {
        public ConllUReader(Corpus corpus, LexiconBuilder lb)
            : base(corpus, lb)
        {
        }

        // 各単語に１文節を割り当て、係り受けを付与する
        protected override void ProcessOneLine_1(string s, string[] fields)
        {
            var originalSurface = fields[1];
            Lexeme m = null;
            try
            {
                m = this.LexiconBuilder.AddEntryConllU(s, this.FromDictionary, false);
            }
            catch (Exception)
            {
                Console.WriteLine(string.Format("Lexeme parse error: {0}", s));
            }
            if (m != null)
            {
                try
                {
                    var f0 = Int32.Parse(fields[0]) - 1;
                    var f7 = (fields.Length > 7) ? fields[7] : string.Empty;
                    var f6 = f0 + 1;
                    if (fields.Length > 6 && fields[6] != "_")
                    {
                        f6 = Int32.Parse(fields[6]) - 1;
                    }
                    var buns = new CabochaBunsetsu(m_CurSen, m_CurDoc, m_CurCharPos, f0, f7, f6, 0.0);
                    buns.EndPos = buns.StartPos + originalSurface.Length;
                    if (fields.Length > 9 && fields[9] != "_")
                    {
                        foreach (var pairs in fields[9].Split('|'))
                        {
                            var pair = pairs.Split('=');
                            if (pair.Length == 2 && !buns.Attrs.ContainsKey(pair[0]))
                            {
                                if (pair[0] == "UniDicLemma")
                                {
                                    // UniDicLemma属性はlexemeに持たせる.
                                    m.CustomProperty = $"{pair[0]}\t{pair[1]}";
                                }
                                else
                                {
                                    // それ以外はBunsetsu SegmentのAttributeに持たせる.
                                    buns.Attrs.Add(pair[0], pair[1]);
                                }
                            }

                        }
                    }
                    m_CurBunsetsu = buns;
                    if (buns.DependsTo == -1)
                    {
                        m_CurTerminalBunsetsu.Add(buns);
                    }
                    m_BunsetsuList.Add(buns);
                }
                catch (Exception)
                {
                    Console.WriteLine(string.Format("Bunsetsu parse error: {0}", s));
                }
                // SentenceとBunsetsuにWordを追加.
                var w = m_CurSen.AddWord(m);
                w.StartChar = m_CurCharPos;
                w.EndChar = m_CurCharPos + originalSurface.Length; // Word.Lengthを使ってもよいが、空白を含む文字列長であることに注意.
                // Surfaceの末尾の空白をWordに記録
                if (!originalSurface.Equals(m.Surface))
                {
                    w.Extras = GetDiff(originalSurface, m.Surface);
                }
                m_DocumentTextBuilder.Append(originalSurface);

                // 文節にこの語を割り当てる
                if (m_CurBunsetsu != null)
                {
                    m_CurBunsetsu.Words.Add(w);
                }
                m_CurCharPos += (w.EndChar - w.StartChar);
            }
        }

        // CONLLUではFEATSフィールドの IOB2 タグは見ない.
        protected override void ProcessOneLine_2(string s, string[] fields)
        {
            ProcessOneLine_1(s, fields);
        }

        protected override void ProcessCommentLine(string s)
        {
            // コメント行にあるsent_idをSentence属性に格納
            if (s.StartsWith("# sent_id = "))
            {
                // SentenceAttrはDocumentAttrへのリンクになる.
                var attrid = m_CurDoc.Attributes.Count;
                m_CurDoc.Attributes.Add(new DocumentAttribute()
                {
                    ID = attrid,
                    Key = "@sent_id",
                    Value = s.Substring(13)
                });
                m_CurSen.Attributes.Add(new SentenceAttribute()
                {
                    ID = attrid
                });
            }
        }

    }
}
