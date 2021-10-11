using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChaKi.Common
{
    public static class Utility
    {
        private static readonly string[] encodinglist = { "shift_jis", "EUC-JP", "utf-8", "utf-16" };

        /// <summary>
        /// Guess which encoding system the specified file uses. (Japanese only)
        /// </summary>
        /// <param name="filename">target text file</param>
        /// <returns></returns>
        public static string GuessTextFileEncoding(string filename)
        {
            var readsz = 500;
            var buf = new char[readsz];
            var h_scores = new Dictionary<string, int>();

            foreach (string senc in encodinglist)
            {
                var enc = ToEncoding(senc);
                using (var reader = new StreamReader(filename, enc))
                {
                    var nread = reader.ReadBlock(buf, 0, readsz);

                    var score = 0;
                    for (var i = 0; i < nread; i++)
                    {
                        if ('あ' <= buf[i] && buf[i] <= 'ン')
                        {
                            score++;
                        }
                    }

                    if (reader.CurrentEncoding != enc)
                    {
                        score -= 100; // encoding object was replaced with auto-detected encoding
                    }
                    h_scores.Add(senc, score);
                }
            }

            string bestenc = string.Empty;
            foreach (KeyValuePair<string, int> pair in h_scores)
            {
                if (bestenc == string.Empty || h_scores[bestenc] < pair.Value)
                {
                    bestenc = pair.Key;
                }
            }
            if (bestenc == string.Empty)
            {
                return null;
            }
            return bestenc;
            //return h_scores.Where(p1 => p1.Value == h_scores.Max(p2 => p2.Value)).First().Key;
        }

        private static Encoding ToEncoding(string encname)
        {
            if (encname == "utf-8")
            {
                return new UTF8Encoding(); // UTF-8N  //Encoding.GetEncoding(65001);
            }
            return Encoding.GetEncoding(encname);
        }

    }
}
