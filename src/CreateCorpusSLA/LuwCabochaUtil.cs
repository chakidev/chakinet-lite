using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CreateCorpusSLA
{
    /// <summary>
    /// 長単位Cabocha fileを扱うためのユーティリティー
    /// </summary>
    static class LuwCabochaUtil
    {
        public static void Convert(string ifpath, string ofpath)
        {
            using (var istr = new FileStream(ifpath, FileMode.Open))
            using (var rdr = new StreamReader(istr))
            using (var ostr = new FileStream(ofpath, FileMode.Create))
            using (var wr = new StreamWriter(ostr))
            {
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    if (line.StartsWith("#") || line.StartsWith("*"))
                    {
                        wr.WriteLine(line);
                        continue;
                    }
                    var tokens = line.Split('\t');
                    if (tokens.Length < 5)
                    {
                        wr.WriteLine(line);
                        continue;
                    }
                    // Tab区切りで3,4カラムが長単位表層および品詞である.
                    if (tokens[2].Length > 0)
                    {
                        wr.WriteLine($"{tokens[2]}\t{tokens[3]},,,,,,,,,,,,,,,,,,,,,");
                    }
                }
            }
        }
    }
}
