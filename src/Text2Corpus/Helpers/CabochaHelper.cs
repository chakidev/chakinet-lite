using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ChaKi.Text2Corpus.Properties;
using System.IO;
using System.Text.RegularExpressions;

namespace ChaKi.Text2Corpus.Helpers
{
    internal class CabochaHelper
    {
        public static void Setup()
        {
            m_DictPaths.Clear();
            var unidicpath = UnidicHelper.GetCabochaUniDicPath();
            if (unidicpath != null)
            {
                m_DictPaths.Add("UniDic", unidicpath);
            }

            DetermineEncodingFromRc();
        }

        public static string CabochaPath = MecabHelper.GetSoftwarePath("cabocha");

        public static Encoding CabochaEncoding = SupportedEncodings.ShiftJIS;

        static private Dictionary<string, string> m_DictPaths = new Dictionary<string, string>();

        // cabocharcの"charset"または、"charset-file"定義（最も後置される定義が有効）に従ってEncodingを求める。
        private static void DetermineEncodingFromRc()
        {
            if (CabochaPath == null)
            {
                return;
            }
            var rcpath = Path.Combine(Path.GetDirectoryName(CabochaPath), @"..\etc\cabocharc");
            if (File.Exists(rcpath))
            {
                using (var rdr = new StreamReader(rcpath))
                {
                    string line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        var match = Regex.Match(line.Trim(), @"^charset\s*=\s*(\S+)$");
                        if (match.Success)
                        {
                            var encstr = match.Groups[1].Value;
                            CabochaEncoding = Encoding.GetEncoding(encstr);
                        }
                        var match2 = Regex.Match(line.Trim(), @"^charset-file\s*=\s*(\S+)$");
                        if (match2.Success)
                        {
                            var encfile = match2.Groups[1].Value;
                            encfile = encfile.Replace("$(rcpath)", Path.GetDirectoryName(rcpath));
                            if (File.Exists(encfile))
                            {
                                using (var rdr2 = new StreamReader(encfile))
                                {
                                    string line2;
                                    if ((line2 = rdr2.ReadLine()) != null)
                                    {
                                        var encstr = line2.Trim();
                                        CabochaEncoding = Encoding.GetEncoding(encstr);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void InvokeCabocha(string srcfile, string destfile, string dictname)
        {
            if (CabochaPath == null)
            {
                //"Cabochaが見つかりません."
                throw new Exception(Resources.S017);
            }
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = CabochaPath;
            processStartInfo.Arguments = "-I1 -O4 -f1 -n0";
            if (dictname != null && dictname != "default")
            {
                string dictpath;
                if (m_DictPaths.TryGetValue(dictname, out dictpath))
                {
                    processStartInfo.Arguments += string.Format(
                        " -P IPA -m \"{0}\\{1}\" -M \"{2}\\{3}\"",
                        dictpath,
                        UnidicHelper.UNIDICCABOCHA_MODELDEP,
                        dictpath,
                        UnidicHelper.UNIDICCABOCHA_MODELCHUNK);
                }
                else
                {
                    throw new Exception(string.Format(Resources.S022, dictname));
                }
            }
            processStartInfo.Arguments += " \"" + srcfile + "\" -o \"" + destfile + "\"";
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            var p = Process.Start(processStartInfo);
            var sb = new StringBuilder();
            while (!p.StandardOutput.EndOfStream)
            {
                var outs = p.StandardOutput.ReadLine();
                sb.AppendLine(outs);
            }
            p.WaitForExit();
            if (sb.Length > 0)
            {
                throw new Exception("Cabocha error: " + sb.ToString());
            }
        }
    }
}
