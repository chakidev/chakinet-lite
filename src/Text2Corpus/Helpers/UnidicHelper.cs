using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Windows.Forms;
using ChaKi.Common.Settings;

namespace ChaKi.Text2Corpus.Helpers
{
    internal class UnidicHelper
    {
        private static Dictionary<int, List<BunruiData>> m_BunruiTable;

        public static void Setup()
        {
            ReadBunruiTable();
        }

        static public string GetUnidicPath()
        {
            RegistryKey rkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\unidic_win");
            if (rkey == null)
            {
                rkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\unidic_win");
            }
            if (rkey == null)
            {
                rkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\unidic_win");
            }
            if (rkey == null)
            {
                rkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\unidic_win");
            }
            if (rkey != null)
            {
                string unidicloc = (string)rkey.GetValue("InstallLocation");
                string dicpath = unidicloc + "\\dic\\unidic-mecab";
                string rcpath = dicpath + "\\dicrc";
                if (File.Exists(rcpath))
                {
                    return dicpath;
                }
            }

            string defaultPath = @"c:\Program Files\unidic\dic\unidic-mecab";
            string defrcpath = defaultPath + @"\dicrc";
            if (File.Exists(defrcpath)) return defaultPath;

            defaultPath = @"c:\Program Files (x86)\unidic\dic\unidic-mecab";
            defrcpath = defaultPath + @"\dicrc";
            if (File.Exists(defrcpath)) return defaultPath;

            return null;
        }

        public const string UNIDICCABOCHA_MODELDEP = "modeldep";
        public const string UNIDICCABOCHA_MODELCHUNK = "modelchunk";

        public static string GetCabochaUniDicPath()
        {
            if (File.Exists(Program.ProgramDir + "\\" + UNIDICCABOCHA_MODELDEP)
             && File.Exists(Program.ProgramDir + "\\" + UNIDICCABOCHA_MODELCHUNK))
            {
                return Program.ProgramDir;
            }
            return null;
        }

        //　分類語彙表のTSVファイルを読み込む
        public static void ReadBunruiTable()
        {
            m_BunruiTable = new Dictionary<int, List<BunruiData>>();

            using (var rdr = new StreamReader(Path.Combine(Program.ProgramDir, "BunruiNo_LemmaID.txt")))
            {
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    var tokens = line.Split(',', '\t');
                    if (tokens.Length != 4)
                    {
                        continue;
                    }
                    int lemmaid = -1;
                    if (Int32.TryParse(tokens[3], out lemmaid))
                    {
                        List<BunruiData> d;
                        m_BunruiTable.TryGetValue(lemmaid, out d);
                        if (d == null)
                        {
                            d = new List<BunruiData>();
                            m_BunruiTable[lemmaid] = d;
                        }
                        d.Add(new BunruiData(tokens[0], tokens[1], tokens[2]));

                    }
                }
            }
        }

        // Mecab出力に分類語彙表データを付加
        public static string AddBunrui(string input, BunruiOutputFormat bunruiOutputFormat)
        {
            var tokens = input.Split(',');
            int id;
            string bstr = string.Empty;
            if (bunruiOutputFormat != BunruiOutputFormat.None
             && tokens.Length > 0 && Int32.TryParse(tokens[tokens.Length - 1], out id))
            {
                List<BunruiData> bunrui;
                if (m_BunruiTable.TryGetValue(id, out bunrui))
                {
                    bstr = string.Join(";", (from b in bunrui
                                             select b.Format(bunruiOutputFormat)));
                }
            }
            return (bunruiOutputFormat == BunruiOutputFormat.None) ? input : $"{input}\t{bstr}";
        }
    }
}
