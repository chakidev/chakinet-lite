using ChaKi.Common.Settings;
using ChaKi.Common.Widgets;
using ChaKi.Text2Corpus.Helpers;
using ChaKi.Text2Corpus.Properties;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ChaKi.Text2Corpus
{
    internal class TextToCorpusWorker
    {
        private List<string> m_InFiles;
        private List<string> m_OutFiles;
        private string m_OutDirectory;
        private string m_OutDbPath;
        private bool m_DoLineSeparation;
        private bool m_DoChunking;
        private string m_DoneMessage;
        private string m_MecabDicPath;
        private string m_MecabOutputFormat;
        private string m_CabochaDicPath;
        private NormalizationForm? m_UnicodeNormalization;
        private bool m_DoZenkakuConversion;
        private BunruiOutputFormat m_BunruiOutputFormat;

        public Thread m_Thread = null;

        public event EventHandler<ThreadExceptionEventArgs> ExceptionOccurred;
        public event EventHandler<ProgressEventArgs> ProgressChanged;
        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<DetailedLogEventArgs> DetailedLog;
        public event EventHandler Aborted;
        public event EventHandler Done;

        public TextToCorpusWorker()
        {
            m_InFiles = new List<string>();
            m_OutFiles = new List<string>();
        }

        public void Convert(
            string inFile,
            string outPath,
            string outDbPath,
            Func<string, bool> confirmation,
            bool doLineSeparation,
            bool doChunking,
            NormalizationForm? unicodeNormalization,
            bool doZenkakuConversion,
            BunruiOutputFormat bunruiOutputFormat,
            string mecabDicPath,
            string mecabOutputFormat,
            string cabochaDicPath
            )
        {
            m_InFiles.Clear();
            m_OutFiles.Clear();
            string srcdir = string.Empty;
            m_OutDirectory = outPath;
            m_OutDbPath = outDbPath;
            m_DoLineSeparation = doLineSeparation;
            m_DoChunking = doChunking;
            m_UnicodeNormalization = unicodeNormalization;
            m_DoZenkakuConversion = doZenkakuConversion;
            m_MecabDicPath = mecabDicPath;
            m_MecabOutputFormat = mecabOutputFormat;
            m_CabochaDicPath = cabochaDicPath;
            m_BunruiOutputFormat = bunruiOutputFormat;

            // check existence of infile and outfile
            if (File.Exists(inFile))
            {
                srcdir = Path.GetDirectoryName(inFile);
                m_InFiles.Add(inFile);
            }
            else if (Directory.Exists(inFile))
            {
                srcdir = MakeSureEndsWithSlash(inFile);
                EnumerateFiles(srcdir, m_InFiles);
            }
            else
            {
                // "変換元ファイル・フォルダ {0} は存在しません。名前を確認してください。"
                throw new Exception(string.Format(Resources.S001, inFile));
            }

            // check dest folder whether files such that will be overwritten exists
            // get relative paths of files
            if (m_InFiles.Count == 0)
            {
                //"変換元フォルダ {0} には変換対象ファイルがありません。"
                throw new Exception(string.Format(Resources.S002, srcdir));
            }
            foreach (var fn in m_InFiles)
            {
                var name = Path.GetFileNameWithoutExtension(fn);
                name = name + (doChunking ? ".cabocha" : ".mecab");
                m_OutFiles.Add(Path.Combine(m_OutDirectory, name));
            }
            // "out"フォルダの中身があれば警告
            var filesToBeDeleted = new List<string>();
            if (Directory.Exists(m_OutDirectory))
            {
                filesToBeDeleted.AddRange(Directory.GetFiles(m_OutDirectory, "*.chasen", SearchOption.AllDirectories));
                filesToBeDeleted.AddRange(Directory.GetFiles(m_OutDirectory, "*.cabocha", SearchOption.AllDirectories));
                filesToBeDeleted.AddRange(Directory.GetFiles(m_OutDirectory, "*.mecab", SearchOption.AllDirectories));
                filesToBeDeleted.AddRange(Directory.GetFiles(m_OutDirectory, "*.txt", SearchOption.AllDirectories));
            }
            if (filesToBeDeleted.Count != 0)
            {
                var sb = new StringBuilder();
                // "変換結果格納フォルダにある {0} 個のファイルが削除されます。変換を続行してもよろしいですか？\n\n"
                sb.AppendFormat(Resources.S003, filesToBeDeleted.Count);
                //"--------------- 上書きされるファイル --------------\n"
                sb.Append(Resources.S004);
                for (int i = 0; i < 10 && i < filesToBeDeleted.Count; i++)
                {
                    sb.AppendFormat("{0}\n", filesToBeDeleted[i]);
                }
                if (filesToBeDeleted.Count > 10)
                {
                    sb.Append("...");
                }
                if (!confirmation(sb.ToString()))
                {
                    if (Aborted != null)
                    {
                        Aborted(this, EventArgs.Empty);
                    }
                    return;
                }
                foreach (var file in filesToBeDeleted)
                {
                    File.Delete(file);
                }
            }

            // "変換が完了しました。\n"
            m_DoneMessage = Resources.S006;
            if (doLineSeparation)
            {
                // "改行処理、"
                m_DoneMessage += Resources.S007;
            }
            // "MeCab処理(辞書文字コード:{0})"
//            m_DoneMessage += string.Format(Resources.S008, MecabHelper.MecabEncoding.EncodingName);
            if (doChunking)
            {
                // "、CaboCha処理を行いました。\n\n"
                m_DoneMessage += Resources.S009;
            }
            else
            {
                // "を行いました。\n\n"
                m_DoneMessage += Resources.S010;
            }

            m_Thread = new Thread(ConversionThread) { IsBackground = true };
            m_Thread.Start();
        }


        private void ConversionThread()
        {
            try
            {
                if (Program.Locale != null)
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(Program.Locale);
                }

                // "変換中..."
                ReportState(Resources.S011);
                ReportDetail(null);
                ReportDetail("Started.");
                var tmpfile1 = Path.GetTempFileName();
                var tmpfile2 = Path.GetTempFileName();
                var tmpfile3 = Path.GetTempFileName();
                Encoding inenc = null;
                string s = "";

                ProgressChanged?.Invoke(this, new ProgressEventArgs(0));
                for (int i_file = 0; i_file < m_InFiles.Count; i_file++)
                {
                    string fn = m_InFiles[i_file];
                    string outfilename = m_OutFiles[i_file];

                    ReportDetail($"Processing ({i_file + 1}/{m_InFiles.Count}): {fn}");

                    var dir = Path.GetDirectoryName(outfilename);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // preprocessing
                    inenc = TextFileHelper.DetectEncoding(fn);
                    s = File.ReadAllText(fn, inenc);

                    ReportDetail($" Estimated Character Encoding is {inenc.WebName}.");
                    ReportDetail($" Input content reads: {Abbrev(s)}...");

                    if (m_UnicodeNormalization != null)
                    {
                        s = s.Normalize(m_UnicodeNormalization.Value);
                    }
                    if (m_DoZenkakuConversion)
                    {
                        // 半角→全角
                        try
                        {
                            s = Strings.StrConv(s, VbStrConv.Wide, 0);
                        }
                        catch { /* 英語OSでErrorとなるが無視する */ }
                    }
                    if (m_DoLineSeparation)
                    {
                        ReportDetail(" Start LineSeparation.");
                        // 改行処理中... (ファイル {0}/{1})
                        ReportState(string.Format(Resources.S012, i_file + 1, m_InFiles.Count));
                        s = TextFileHelper.SplitIntoLines(s);
                        ReportDetail($" After LineSeparation: {Abbrev(s)}...");
                    } else
                    {
                        ReportDetail(" LineSeparation Skipped.");
                    }

                    using (var writer = new StreamWriter(tmpfile1, false, MecabHelper.MecabEncoding))
                    {
                        writer.Write(s);
                    }

                    // MeCab処理中... (ファイル {0}/{1})
                    ReportState(string.Format(Resources.S013, i_file + 1, m_InFiles.Count));
                    ReportDetail(" Start Mecab.");
                    ReportDetail($"  Mecab Path={MecabHelper.MecabPath}.");
                    ReportDetail($"  Mecab DicFile={m_MecabDicPath}");
                    ReportDetail($"  Mecab OutputFormat={m_MecabOutputFormat}");
                    ReportDetail($"  Mecab Encoding={MecabHelper.MecabEncoding.WebName}.");

                    MecabHelper.InvokeMecab(tmpfile1, tmpfile3, m_MecabDicPath, m_MecabOutputFormat);
                    string finalresult = tmpfile3;
                    if (!m_DoChunking)
                    {
                        MecabHelper.AppendBunrui(m_BunruiOutputFormat, tmpfile3, tmpfile2, MecabHelper.MecabEncoding);
                        finalresult = tmpfile2;
                    }
                    ReportDetail($" After Mecab: {Abbrev(ReadStartingText(finalresult, MecabHelper.MecabEncoding))}...");

                    if (m_CabochaDicPath != null)
                    {
                        var outenc = MecabHelper.MecabEncoding;
                        if (m_DoChunking)
                        {
                            TextFileHelper.ChangeEncoding(finalresult, tmpfile1, outenc, CabochaHelper.CabochaEncoding);
                            // CaboCha処理中... (ファイル {0}/{1})
                            ReportState(string.Format(Resources.S014, i_file + 1, m_InFiles.Count));
                            ReportDetail(" Start Cabocha.");
                            ReportDetail($"  Cabocha DicFile={m_CabochaDicPath}");
                            ReportDetail($"  Cabocha Encoding={CabochaHelper.CabochaEncoding.WebName}");
                            CabochaHelper.InvokeCabocha(tmpfile1, tmpfile2, m_CabochaDicPath);
                            outenc = CabochaHelper.CabochaEncoding;
                            MecabHelper.AppendBunrui(m_BunruiOutputFormat, tmpfile2, tmpfile3, outenc);
                            finalresult = tmpfile3;
                            ReportDetail($" After Cabocha: {Abbrev(ReadStartingText(finalresult, outenc))}...");
                        }
                        else
                        {
                            ReportDetail(" Cabocha (Chunking) Skipped.");
                            finalresult = tmpfile2;
                        }
                        TextFileHelper.ChangeEncoding(finalresult, outfilename, outenc, SupportedEncodings.UTF8);
                        ReportDetail(" Output converted to UTF-8");
                        if (i_file == m_InFiles.Count - 1)
                        {
                            s = File.ReadAllText(outfilename, SupportedEncodings.UTF8);
                        }
                    }
                    else // IPADIC
                    {
                        if (m_DoChunking)
                        {
                            //"CaboCha処理中... (ファイル {0}/{1})"
                            ReportState(string.Format(Resources.S014, i_file + 1, m_InFiles.Count));
                            ReportDetail(" Start Cabocha (IPADIC).");
                            ReportDetail($"  Cabocha Encoding={CabochaHelper.CabochaEncoding.WebName}");
                            CabochaHelper.InvokeCabocha(finalresult, tmpfile1, null);
                            MecabHelper.AppendBunrui(m_BunruiOutputFormat, tmpfile1, tmpfile3, CabochaHelper.CabochaEncoding);
                            finalresult = tmpfile3;
                            ReportDetail($" After Cabocha: {Abbrev(ReadStartingText(finalresult, CabochaHelper.CabochaEncoding))}...");
                        }
                        else
                        {
                            finalresult = tmpfile2;
                        }

                        // postprocessing (do nothing)
                        s = File.ReadAllText(finalresult, CabochaHelper.CabochaEncoding);
                        using (var writer = new StreamWriter(outfilename, false, SupportedEncodings.UTF8))
                        {
                            writer.Write(s);
                        }
                        ReportDetail(" Output converted to UTF-8");
                    }
                    ProgressChanged?.Invoke(this, new ProgressEventArgs((int)(50.0 * i_file / m_InFiles.Count)));
                }
                File.Delete(tmpfile1);
                File.Delete(tmpfile2);
                File.Delete(tmpfile3);

                // CreateCorpus処理
#if CHAMAME
                ProgressChanged?.Invoke(this, new ProgressEventArgs(100));
#else
                ProgressChanged?.Invoke(this, new ProgressEventArgs(50));
                // CreateCorpus処理中...
                ReportState(Resources.S015);
                CreateCorpusHelper.Setup();
                CreateCorpusHelper.InvokeCreateCorpus(m_OutDirectory, m_OutDbPath);
                ReportDetail(" Start CreateCorpus.");
                ReportDetail($"  CreateCorpus Path={CreateCorpusHelper.CreateCorpusPath}");
                ReportDetail("  CreateCorpus Encoding=Auto, Format=Auto");
                ProgressChanged?.Invoke(this, new ProgressEventArgs(100));
#endif

                // "==== 出力結果プレビュー (出力文字コード: utf-8) ====\n"
                m_DoneMessage += Resources.S016 + Abbrev(s);
                ReportState("Done.");
                ReportDetail(" Done.");
            }
            catch (Exception ex)
            {
                if (ExceptionOccurred != null)
                {
                    ExceptionOccurred(this, new ThreadExceptionEventArgs(ex));
                }
            }
            finally
            {
                if (this.Done != null)
                {
                    Done(this, EventArgs.Empty);
                }
            }
        }

        private static string Abbrev(string s)
        {
            return (s.Length < 300 ? s : s.Substring(0, 300)).Replace("\n", ".").Replace('\n', '.').Replace('\r', '.');
        }

        private static string ReadStartingText(string file, Encoding enc)
        {
            using (var rdr = new StreamReader(file, enc))
            {
                var sb = new StringBuilder();
                while (!rdr.EndOfStream)
                {
                    sb.Append(rdr.ReadLine());
                    if (sb.Length > 500)
                    {
                        break;
                    }
                }
                return sb.ToString();
            }
        }

        private void ReportState(string state)
        {
            var h = this.StateChanged;
            if (h != null)
            {
                h(this, new StateChangedEventArgs(state));
            }
        }

        private void ReportDetail(string message)
        {
            var h = this.DetailedLog;
            if (h != null)
            {
                h(this, new DetailedLogEventArgs(message));
            }
        }

        private static string MakeSureEndsWithSlash(string path)
        {
            return (path.EndsWith(@"\")) ? path : path + @"\";
        }

        private static void EnumerateFiles(string dir, List<string> result)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(dir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file);
                if (ext == ".txt" /*ext != ".mecab" && ext != ".cabocha" && ext != ".db" && ext != ".bib"*/)
                {
                    result.Add(file);
                }
            }
            foreach (string subdir in System.IO.Directory.GetDirectories(dir))
            {
                EnumerateFiles(subdir, result);
            }
        }
    }
}
