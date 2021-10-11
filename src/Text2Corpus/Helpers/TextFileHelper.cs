using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using ChaKi.Common.Settings;

namespace ChaKi.Text2Corpus.Helpers
{
    public class TextFileHelper
    {
        public static void Setup(string separator, CRLFModes crlfmode)
        {
            SeparatorChars = separator;
            CRLFMode = crlfmode;
        }

        public static string SeparatorChars { get; private set; }

        public static CRLFModes CRLFMode { get; private set; }

        public static string SplitIntoLines(string s)
        {
            // remove single (CR)LFs
            switch (CRLFMode)
            {
                case CRLFModes.MultipleOnly:
                    break;
                case CRLFModes.SingleToSpace:
                    s = Regex.Replace(s, "([^\r\n])\r?\n([^\r\n])", "$1 $2");
                    break;
                case CRLFModes.Single:
                    s = Regex.Replace(s, "([^\r\n])\r?\n([^\r\n])", "$1$2");
                    break;
                case CRLFModes.All:
                    s = Regex.Replace(s, "\r?\n", "");
                    break;
            }

            if (SeparatorChars.Length > 0)
            {
                s = Regex.Replace(s, "([" + SeparatorChars + "]+)", "$1\n");
            }
            // multiple CR/LF => single LF
            s = Regex.Replace(s, "[\r\n]([\r\n]+)", "\n");
            return s;
        }

        public static void ChangeEncoding(string infile, string outfile, Encoding inenc, Encoding outenc)
        {
            using (var sr = new StreamReader(infile, inenc))
            using (var sw = new StreamWriter(outfile, false, outenc))
            {
                string l;
                while ((l = sr.ReadLine()) != null)
                {
                    sw.WriteLine(l);
                }
            }
        }

        public static string mecabPath;
        public static string cabochaPath;

         private const string UNIDICCABOCHA_MODELDEP = "modeldep";
        private const string UNIDICCABOCHA_MODELCHUNK = "modelchunk";

        public static void InvokeCaboCha(string srcfile, string destfile, string model)
        {
            var processStartInfo = new ProcessStartInfo();

            processStartInfo.FileName = cabochaPath;
            processStartInfo.Arguments = "-I1 -O4 -f1 -n0";
            if (model != null)
            {
                processStartInfo.Arguments += (" -P IPA -t SHIFT_JIS -m \"" + model
                    + "\\" + UNIDICCABOCHA_MODELDEP + "\" -M \"" + model
                    + "\\" + UNIDICCABOCHA_MODELCHUNK + "\"");
            }
            processStartInfo.Arguments += " \"" + srcfile + "\" -o \"" + destfile + "\"";

            Process.Start(processStartInfo).WaitForExit();
        }

        public static Encoding DetectEncoding(string filename)
        {
            var readsz = 500;
            var buf = new char[readsz];
            var h_scores = new Dictionary<Encoding, int>();

            foreach (var enc in SupportedEncodings.List)
            {
                using (var reader = new StreamReader(filename, enc))
                {
                    int nread = reader.ReadBlock(buf, 0, readsz);

                    int score = 0;
                    for (int i = 0; i < nread; i++)
                    {
                        if ('あ' <= buf[i] && buf[i] <= 'ン') score++;
                    }

                    if (reader.CurrentEncoding != enc) score -= 100; // encoding object was replaced with auto-detected encoding
                    h_scores.Add(enc, score);
                }
            }

            Encoding bestenc = null;
            foreach (var kvp in h_scores)
            {
                if (bestenc == null || h_scores[bestenc] < kvp.Value)
                {
                    bestenc = kvp.Key;
                }
            }
            return bestenc;
            //return h_scores.Where(p1 => p1.Value == h_scores.Max(p2 => p2.Value)).First().Key;
        }
    }
}
