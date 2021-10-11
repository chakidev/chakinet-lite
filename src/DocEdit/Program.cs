using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChaKi.DocEdit
{
    static class Program
    {
        static public string ProgramDir { get; private set; }
        static public string SettingDir { get; private set; }
        static private string m_SettingFileGUI;

        static private string CorpusFile = string.Empty;
        static private string ImportFile = string.Empty;
        static private bool ConsoleOnly = false;

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Parse Commandline
            if (!ParseArguments(args))
            {
                return;
            }

            // ユーザ設定のロード
            try
            {
                ProgramDir = Environment.CurrentDirectory;
                SettingDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                SettingDir += @"\ChaKi.NET";
                if (!Directory.Exists(SettingDir))
                {
                    Directory.CreateDirectory(SettingDir);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("{0}: {1}", e.Message, SettingDir));
            }

            m_SettingFileGUI = SettingDir + @"\DocEditSettingsGUI.xml";
            try
            {
                DocEditSettings.Load(m_SettingFileGUI);
                if (DocEditSettings.Instance.UILocale != null)
                {
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(DocEditSettings.Instance.UILocale);
                }
            }
            catch (FileNotFoundException)
            {
                // just reset content
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error reading user settings for GUI : {0}", e.Message));
            }

            Application.ApplicationExit += ApplicationExitHandler;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new DocEditView();
            if (CorpusFile.Length > 0)
            {
                form.OpenCorpus(CorpusFile);
            }
            if (ImportFile.Length > 0)
            {
                Console.Error.WriteLine("Importing from CSV...");
                form.ImportFromCSV(ImportFile);
            }
            if (ConsoleOnly)
            {
                Console.Error.WriteLine("Committing...");
                form.Commit();
                return;
            }

            // Processの親がCMD.EXEかどうかのチェック
            var process = Process.GetCurrentProcess();
            var pc = new PerformanceCounter("Process", "Creating Process Id", process.ProcessName);
            var pparent = Process.GetProcessById((int)pc.RawValue);
            if (pparent.MainModule.ModuleName.ToLower() != "cmd.exe")
            {
                // CMD.EXEから実行された場合以外はコンソールをここで隠し、Formをメインウィンドウとして見せる.
                ShowWindow(process.MainWindowHandle, 0/*=SW_HIDE*/);
            }

            // Main Formの表示
            Rectangle bounds = DocEditSettings.Instance.WindowLocation;
            if (bounds.Left < 0 || bounds.Left > Screen.PrimaryScreen.Bounds.Right)
            {
                bounds.Location = new Point(0,0);
            }
            if (bounds.Width < 200 || bounds.Width > Screen.PrimaryScreen.Bounds.Width
             || bounds.Height < 200 || bounds.Height > Screen.PrimaryScreen.Bounds.Height)
            {
                bounds.Width = 800;
                bounds.Height = 600;
            }
            form.Location = bounds.Location;
            form.Size = bounds.Size;
            Application.Run(form);
        }

        static void ApplicationExitHandler(object sender, EventArgs e)
        {
            DocEditSettings.Save(m_SettingFileGUI);
        }

        static bool ParseArguments(string[] args)
        {
            int n = 0;
            foreach (string arg in args)
            {
                if (arg.Length > 1 && arg.StartsWith("-"))
                {
                    string[] tokens = arg.Substring(1).Split(new char[] { '=' });
                    if (!(tokens.Length == 2 && (tokens[0].Equals("i")))
                     && !(tokens.Length == 1 && (tokens[0].Equals("c") || tokens[0].Equals("h"))))
                    {
                        Console.Error.WriteLine("Invalid option: {0}", arg);
                        PrintUsage();
                        return false;
                    }
                    if (tokens[0].Equals("i"))
                    {
                        ImportFile = tokens[1];
                    } else if (tokens[0].Equals("c"))
                    {
                        ConsoleOnly = true;
                    }
                    else if (tokens[0].Equals("h"))
                    {
                        PrintUsage();
                        return false;
                    }
                }
                else
                {
                    if (n == 0)
                    {
                        CorpusFile = arg;
                    }
                    n++;
                }
            }
            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("DocEdit (Sentence/Document attribute editor) Usage:");
            Console.WriteLine("> DocEdit [CorpusFileName] (-i=[ImportFileName]) (-c) (-h)");
            Console.WriteLine("    CorpusFileName: .db or .def file");
            Console.WriteLine("    -i: Import CSV data");
            Console.WriteLine("    ImportFileName: a CSV file containing sentence/document attributes");
            Console.WriteLine("    -c: Non-interactive, console-only mode.");
            Console.WriteLine("        Use with -i to auto-commit imported data.");
            Console.WriteLine("    -h: Show this help");
        }
    }
}
