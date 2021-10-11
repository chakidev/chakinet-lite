using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Corpora;
using System.IO;
using ChaKi.Entity.Kwic;
using NHibernate;
using ChaKi.Service.Common;
using System.Collections;

namespace ChaKi.Service.Export
{
    public class ExportServiceConll : ExportServiceCabocha
    {
        public ExportServiceConll(TextWriter wr)
            : base(wr)
        {
        }

        protected long currentBunsetsuID;
        protected int bunsetsuPos;
        protected List<string> sb = new List<string>();  // Form
        protected List<string> sb2 = new List<string>();  // Base
        protected const string EMPTY_COL = "_";

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
                        pos.Name1,  // CPOSTAG
                        pos.Name2, // POSTAG
                        GetFeats(w),    // FEATS
                        depTo + 1,  // HEAD
                        depAs,  // DEPREL
                        EMPTY_COL,    // PHEAD
                        EMPTY_COL);   // PDEPREL
                    sb.Clear();
                    sb2.Clear();
                    currentBunsetsuID = buns;
                    bunsetsuPos++;
                }
            }
            t6 += (DateTime.Now.Ticks - t00);
        }

        protected override void WriteEos()
        {
            m_TextWriter.WriteLine();
        }

        protected int GetDependToIndex(long buns_id, out string dependAs)
        {
            long toSegID = -1;
            object[] link = null;
            if (!m_Links.TryGetValue(buns_id, out link))
            {
                dependAs = string.Empty;
                return -1;
            }
            toSegID = (long)link[1];
            dependAs = (string)link[2];
            int index = -1;
            if (!m_Segs.TryGetValue(toSegID, out index))
            {
                return -1;
            }
            return index;
        }

        protected virtual string GetFeats(Word w)
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
    }
}

