using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using ChaKi.Text2Corpus.Properties;
using System.Text.RegularExpressions;
using ChaKi.Common.Settings;

namespace ChaKi.Text2Corpus.Helpers
{
    internal class MecabHelper
    {
        public static void Setup(string dictname)
        {
            m_DictPaths.Clear();
            AddMecabDictionaries(m_DictPaths);
            var unidicpath = UnidicHelper.GetUnidicPath();
            if (unidicpath != null && !m_DictPaths.ContainsKey("UniDic"))
            {
                m_DictPaths.Add("UniDic", unidicpath);
            }

            MecabEncoding = DetectMecabEncoding(dictname);
        }

        public static string[] DictNames {get { return m_DictPaths.Keys.ToArray(); } }

        static public string MecabPath = GetSoftwarePath("mecab");
        static private Dictionary<string, string> m_DictPaths = new Dictionary<string, string>();

        static public Encoding MecabEncoding { get; private set; }

        public static string GetSoftwarePath(string name)
        {
            RegistryKey rkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\" + name);
            if (rkey == null)
            {
                rkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + name);
            }
            if (rkey != null)
            {
                string mecabrc = (string)rkey.GetValue(name + "rc");
                string exename = mecabrc.Replace(@"etc\" + name + "rc", @"bin\" + name + ".exe");
                if (File.Exists(exename))
                {
                    return exename;
                }
            }
            string defaultPath = @"c:\Program Files\" + name + @"\bin\" + name + ".exe";
            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }
            defaultPath = @"c:\Program Files (x86)\" + name + @"\bin\" + name + ".exe";
            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }

            return null;
        }

        public static void InvokeMecab(string srcfile, string destfile, string dictname, string outform)
        {
            if (MecabPath == null)
            {
                // "Mecabが見つかりません."
                throw new Exception(Resources.S019);
            }
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = MecabPath;
            processStartInfo.Arguments = "\"" + srcfile + "\" -o \"" + destfile + "\"";
            if (dictname != null && dictname != "default")
            {
                string dictpath;
                if (m_DictPaths.TryGetValue(dictname, out dictpath))
                {
                    processStartInfo.Arguments += (" -d \"" + dictpath + "\"");
                }
                else
                {
                    throw new Exception(string.Format(Resources.S021, dictname));
                }
            }
#if CHAMAME
            if (!string.IsNullOrEmpty(outform) && outform != "default")
            {
                processStartInfo.Arguments += (" -O \"" + outform + "\"");
            }
#endif
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
                throw new Exception("Mecab error: " + sb.ToString());
            }
        }


        public static Encoding DetectMecabEncoding(string dictname)
        {
            if (MecabPath == null)
            {
                // "Mecabが見つかりません."
                throw new Exception(Resources.S019);
            }
            string teststr = "これは辞書の文字コードを判定するためのテスト文です。\r\n";
            Encoding result = null;
            int maxscore = 0;

            var processStartInfo = new ProcessStartInfo();

            string srcfile = System.IO.Path.GetTempFileName();
            string destfile = System.IO.Path.GetTempFileName();
            try
            {
                foreach (var enc in SupportedEncodings.List)
                {
                    using (var writer = new StreamWriter(srcfile, false, enc))
                    {
                        writer.Write(teststr);
                    }

                    processStartInfo.FileName = MecabPath;
                    processStartInfo.Arguments = "\"" + srcfile + "\" -o \"" + destfile + "\"";
                    if (dictname != null && dictname != "default")
                    {
                        string dictpath;
                        if (m_DictPaths.TryGetValue(dictname, out dictpath))
                        {
                            processStartInfo.Arguments += (" -d \"" + dictpath + "\"");
                        }
                        else
                        {
                            throw new Exception(string.Format(Resources.S021, dictname));
                        }
                    }
                    processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    Process prc = Process.Start(processStartInfo);
                    prc.WaitForExit();

                    string readstr;
                    using (var reader = new StreamReader(destfile, enc))
                    {
                        readstr = reader.ReadToEnd();
                    }

                    int score = 0;
                    for (int i = 0; i < readstr.Length; i++)
                    {
                        if (readstr[i] == '詞') score++;
                    }
                    if (score > maxscore)
                    {
                        maxscore = score;
                        result = enc;
                    }

                }
            }
            finally
            {
                File.Delete(srcfile);
                File.Delete(destfile);
            }
            return result;
        }

        // Mecabフォルダ/dic/以下のサブフォルダのうち、dicrcを含むものをdictsにappendする.
        public static void AddMecabDictionaries(Dictionary<string, string> dicts)
        {
            var path = Path.GetDirectoryName(MecabPath);
            if (path == null)
            {
                // "Mecabが見つかりません."
                throw new Exception(Resources.S019);
            }
            var dpaths = Directory.EnumerateDirectories(Path.Combine(path, @"..\dic"));
            foreach (var dpath in dpaths)
            {
                var dname = Path.GetFileName(dpath);
                var dicrc = Path.Combine(dpath, "dicrc");
                if (File.Exists(dicrc))
                {
                    dicts.Add(dname, dpath);
                }
            }
        }

        public static string[] ListOutputFormats(string dicname)
        {
            var result = new List<string>();
            string dicpath;
            if (!m_DictPaths.TryGetValue(dicname, out dicpath))
            {
                dicpath = Path.Combine(Path.GetDirectoryName(MecabPath), @"..\dic\ipadic");
            }
            using (var rdr = new StreamReader(Path.Combine(dicpath, "dicrc")))
            {
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith(";"))
                    {
                        continue;
                    }
                    var match = Regex.Match(line, @"node-format-(\w+)");
                    if (match.Success)
                    {
                        var fname = match.Groups[1].Value;
                        result.Add(fname);
                    }
                }
            }
            return result.ToArray();
        }

        public static void AppendBunrui(BunruiOutputFormat bunruiOutputFormat, string infile, string outfile, Encoding enc)
        {
            using (var sr = new StreamReader(infile, enc))
            using (var sw = new StreamWriter(outfile, false, enc))
            {
                string l;
                while ((l = sr.ReadLine()) != null)
                {
                    l = UnidicHelper.AddBunrui(l, bunruiOutputFormat);
                    sw.WriteLine(l);
                }
            }

        }
    }
}
