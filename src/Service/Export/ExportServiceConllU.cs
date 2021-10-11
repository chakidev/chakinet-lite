using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ChaKi.Service.Export
{
    public class ExportServiceConllU : ExportServiceConll
    {
        public ExportServiceConllU(TextWriter wr)
            : base(wr)
        {
            // SENTENCETAGは出力しない. 代わりに各Sentenceの冒頭でコメント行を出力.
            m_OutputSentencetagids = false;
        }

        protected override void WriteWords(IList<Word> words)
        {
            // ここで渡されるwordsには、Bunsetsu.ID, HeadInfo, Lex, Extras しかセットされていないtransient objectなので注意. 
            // ExportServiceCabocha.ExportItem()を参照.

            currentBunsetsuID = -1;
            bunsetsuPos = 0;
            sb.Clear();
            sb2.Clear();

            var t00 = DateTime.Now.Ticks;

            var wordsarray = words.Cast<Word>().ToArray();

            t5 += (DateTime.Now.Ticks - t00);
            t00 = DateTime.Now.Ticks;

            int n = wordsarray.Length;
            for (int i = 0; i < n; i++)
            {
                var t0 = DateTime.Now.Ticks;

                Word w = wordsarray[i];

                t1 += (DateTime.Now.Ticks - t0);
                t0 = DateTime.Now.Ticks;

                var lex = w.Lex;
                if (lex == null)
                {
                    continue;
                }
                sb.Add(lex.Surface);
                sb.Add(w.Extras);
                sb2.Add(lex.BaseLexeme.Surface);
                sb2.Add(w.Extras);

                var pos = lex.PartOfSpeech;
                t2 += (DateTime.Now.Ticks - t0);
                t0 = DateTime.Now.Ticks;

                var buns = w.Bunsetsu.ID;
                if (currentBunsetsuID != buns || i == n - 1)
                {
                    // Output word + dependency
                    // 自身
                    int buns_pos = -1;
                    m_Segs.TryGetValue(buns, out buns_pos);
                    // 係り先
                    string depAs;
                    var depTo = GetDependToIndex(buns, out depAs);

                    if (sb.Count > 0)
                    {
                        sb.RemoveAt(sb.Count - 1);  // 最後のExtraは出力しない.
                    }
                    if (sb2.Count > 0)
                    {
                        sb2.RemoveAt(sb2.Count - 1);
                    }
                    m_TextWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                        buns_pos + 1,  // ID
                        string.Join("", sb.ToArray()),  // FORM
                        string.Join("", sb2.ToArray()), // LEMMA
                        pos.Name1,  // UPOS
                        GetXPOS(pos), // XPOS
                        GetFeats(w),    // FEATS
                        depTo + 1,  // HEAD
                        depAs,  // DEPREL
                        EMPTY_COL,    // DEPS
                        GetMisc(buns, lex));   // MISC
                    sb.Clear();
                    sb2.Clear();
                    currentBunsetsuID = buns;
                    bunsetsuPos++;
                }
            }
            t6 += (DateTime.Now.Ticks - t00);
        }

        private string GetXPOS(PartOfSpeech pos)
        {
            // pos.Nameの'-'で区切られた最初の要素を除く部分を返す.
            var p = pos.Name.IndexOf('-');
            if (p >= 0 && p < (pos.Name.Length - 1))
            {
                return pos.Name.Substring(p + 1);
            }
            return string.Empty;
        }

        protected override string GetFeats(Word w)
        {
            var feats = new List<string>();
            if (w.Extras.Length > 0)
            {
                feats.Add("SP");
            }
            // 今のところ、Word Featureをこれ以外に保持していない。Segment Attributeをここに入れるか？

            var result = string.Join("|", feats.ToArray());
            if (result.Length > 0)
            {
                return result;
            }
            return EMPTY_COL;
        }

        // Segment AttributeとLexeme.CustomPropertyからCONLLU MISCフィールドを生成
        private string GetMisc(long segid, Lexeme lex)
        {
            var fields = new List<string>();

            // Segment Attributes
            using (var cmd = m_Session.Connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT attribute_key,attribute_value" +
                    " FROM segment_attribute" +
                    $" WHERE segment_id={segid} AND project_id={m_ProjId} ORDER BY id ASC";
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var key = rdr.GetString(0);
                    var val = rdr.GetString(1);
                    fields.Add($"{key}={val}");
                }
            }


            // Lexeme.CustomProperty
            var cust = lex.CustomProperty;
            foreach (var prop in cust.Split('\n'))
            {
                var pair = prop.Split('\t');
                if (pair.Length == 2)
                {
                    fields.Add($"{pair[0]}={pair[1]}");
                }
            }

            if (fields.Count > 0)
            {
                return string.Join("|", fields);
            }
            return EMPTY_COL;
        }

        protected override void WriteSentenceTag(string csa)
        {
            m_SentenceTags.TryGetValue(csa, out var seqtagpair);
            var t = seqtagpair.Tag.Split('\t');
            if (t.Length < 3) return;
            var s = $"<Root>{t[2]}</Root>";
            using (TextReader trdr = new StringReader(s))
            {
                XmlReader xrdr = XmlReader.Create(trdr);
                while (xrdr.Read())
                {
                    if (xrdr.Name.Equals("Root")) continue;
                    var key = xrdr.Name;
                    var val = xrdr.ReadString();
                    if (key.Length == 0) continue;
                    m_TextWriter.WriteLine($"# {key} = {val}");
                }
            }
        }
    }
}
