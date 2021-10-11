using System;
using System.IO;

namespace CreateCorpusSLA
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateCorpus cc = new CreateCorpus();

            Console.WriteLine("args={0}\n", string.Join(",", args));

            if (!cc.ParseArguments(args))
            {
                PrintUsage();
                goto Exit;
            }
            if (cc.CreateSeparateDB)
            {
                // ターゲットが複数DBである場合は、入力フォルダの各ファイルについてループ
                // SQLiteの場合:
                //   出力としてフォルダパスを指定した場合にここに来る.
                //   出力は、指定されたフォルダの下、入力ファイルのファイル名部分+".db"という名前のファイル
                // MySQL等の場合:
                //   "Separate Database"チェックボックスをONにした場合にここに来る.
                //   出力は、入力ファイルのファイル名部分と同じ名前のデータベース
                string ipath = cc.InputPath;
                if (!CreateCorpus.PathIsFolder(ipath))
                {
                    Console.WriteLine("Input Path must be a folder name when -p is set.");
                    goto Exit;
                }
                string opath = cc.CorpusName;
                string folder = string.Empty;
                if (CreateCorpus.PathIsFolder(opath))
                {
                    folder = opath;
                }
                else
                {
                    folder = Path.GetDirectoryName(opath);
                }
                string[] files = Directory.GetFileSystemEntries(ipath);
                foreach (string s in files)
                {
                    if (CreateCorpus.PathIsFolder(s)) continue;

                    cc.InputPath = s;
                    cc.CorpusName = Path.GetFileNameWithoutExtension(s);
                    cc.ForceCheckInputExtension = true;
                    if (cc.DbType == "SQLite")
                    {
                        // フォルダと拡張子を追加
                        cc.CorpusName = string.Format("{0}\\{1}.db", folder, cc.CorpusName);
                    }
                    DoJobs(cc);
                }
            }
            else if (cc.ReaderType == "Mecab|Cabocha|UniDic2|LUW")
            {
                // LUW(長単位)アノテーション付きのCabochaファイルの処理
                // Proj 0に通常のCabochaインポートを行った後、
                // 長単位アノテーションをProj 1に追加する２段階のインポート処理になる.
                DoLUWJobs(cc);
            }
            else
            {
                DoJobs(cc);
            }
            if (cc.DoNotPauseOnExit)
            {
                return;
            }
        Exit:
            Console.WriteLine("\nPress Enter to exit.");
            Console.ReadLine();
        }

        static private void DoJobs(CreateCorpus cc)
        {
            DateTime t0 = DateTime.Now;
            Console.WriteLine("[{0}]: Started at {1}", cc.InputPath, t0.ToLocalTime());

            cc.InitializeDocumentSet();

            if (!cc.CheckInput()) return;

            if (cc.IsCreatingDictionary)
            {
                if (!cc.ParseDictionaryInput()) return;
                if (!cc.SaveLexicon()) return;
            }
            else
            {
                if (cc.ProjectId == 0)
                {
                    // 通常のコーパス作成
                    if (!cc.ParseBibFile()) return;
                    if (!cc.InitializeDB()) return;
                    if (!cc.InitializeLexiconBuilder()) return;
                    if (!cc.ParseInput()) return;
                    if (!cc.SaveProject()) return;
                    if (!cc.SaveLexicon()) return; 
                    if (!cc.SaveSentences()) return;
                    if (!cc.SaveSentenceTags()) return;
                    if (!cc.UpdateIndex()) return;
                }
                else
                {
                    // Project追加の場合
                    // ・既存Lexiconを読み込んでおく
                    // ・SaveSentenceの代わりにUpdateSentenceを呼ぶ
                    // ・Bib, SentenceTagやIndexなどの処理は行わない
                    if (!cc.InitializeDB(false)) return;  // Database, Tableの初期化は行わない.
                    if (!cc.InitializeLexiconBuilder(true)) return;
                    if (!cc.InitializeTagSet()) return;  // Seg, Link, Group TagをDBから取得する.
                    if (!cc.ParseInput()) return;
                    if (!cc.AddProject()) return;  // Project Tableのみ追加更新
                    if (!cc.SaveLexicon()) return;      // Lexicon追加は、新たに見つかった語のみ.
                    if (!cc.UpdateSentences()) return;
                }
            }
            DateTime t1 = DateTime.Now;
            TimeSpan elapsed = t1 - t0;
            Console.WriteLine("Done.");
            Console.WriteLine("Finished at {0}; Elapsed {1} minutes", t1.ToLocalTime(), elapsed.TotalMinutes);
        }

        static private void DoLUWJobs(CreateCorpus cc)
        {
            // 1. 通常のCabochaインポートを実行(Proj 0)
            Console.WriteLine("========================= SUW -> Proj0");
            cc.ReaderType = "Mecab|Cabocha|UniDic2";
            DoJobs(cc);
            // 2. LUWアノテーションから長単位Cabocha fileを一時的に作成
            Console.WriteLine("=========================");
            Console.WriteLine("Extracting LUW part to temporary cabocha file...");
            var path = Path.GetTempFileName();
            LuwCabochaUtil.Convert(cc.InputPath, path);
            Console.WriteLine($"Written to: {path}");
            // 3. 長単位cabocha fileをProj 1にインポート
            Console.WriteLine("========================= LUW -> Proj1");
            Console.WriteLine("Importing temporary cabocha file of LUWs...");
            cc.ResetInternals();
            cc.InputPath = path;
            cc.ProjectId = 1;
            DoJobs(cc);
            // 4. 一時ファイルを削除
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot delete temp file: {ex.Message}");
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: CreateCorpus [Options] <InputFile> <Output>");
            Console.WriteLine("Options:");
            Console.WriteLine("  [-e=<encoding>] Input Encoding (Auto)");
            Console.WriteLine("  [-t=<type>]     Reader Type; See ReaderDefs.xml (Auto)");
            Console.WriteLine("  [-b=<bibfile>]  Use external .bib file");
            Console.WriteLine("  [-l=<dicfile>]  Use external dictionary");
            Console.WriteLine("  [-d]            Create Dictionary instead of Corpus");
            Console.WriteLine("  [-s]            Set SQLite Synchronous Write mode (false)");
            Console.WriteLine("  [-p]            Make different database for each input when read from folder (false)");
            Console.WriteLine("  [-C]            Do not pause on exit (false)");
            Console.WriteLine("  [-P=<projid>]   Add as a new Project whose id is <projid> (projid > 0)");
            Console.WriteLine("InputFile         cabocha/chasen/mecab/text file");
            Console.WriteLine("Output            .db file for SQLite / .def file for Others / .ddb file for SQLite Dictionary");
        }
    }
}
