using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ChaKi.Common.Settings;
using System.IO;
using System.Xml.Linq;
using System.Threading;
using System.Globalization;
using ChaKi.GUICommon;
using System.Diagnostics;

namespace ChaKi.Text2Corpus
{
    static class Program
    {
        public static string ProgramDir;
        public static string SettingDir;
        public static string SettingsFile;
        public static string Locale;

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

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
                var SettingFileGUI = SettingDir + @"\UserSettingsGUI.xml";
                var xdoc = XDocument.Load(SettingFileGUI);
                Locale = (string)(from el in xdoc.Descendants("UILocale") select el).FirstOrDefault();
                if (Locale != null)
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(Locale);
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(Locale);
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(string.Format("{0}: {1}", e.Message, SettingDir));
            }

            SettingsFile = SettingDir + @"\Text2CorpusSettings.xml";
            Text2CorpusSettings.Load(SettingsFile);

            TextToCorpus dlg = null;
            try
            {
                dlg = new TextToCorpus();
            }
            catch (Exception ex)
            {
                new ErrorReportDialog("Error", ex).ShowDialog();
                dlg = null;
            }
            if (dlg == null)
            {
                return 0;
            }
            if (args.Length > 0)
            {
                dlg.SetInputFile(args[0]);
            }
            Application.Run(dlg);

            return dlg.DoneSuccessfully? 0 : -1;
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var dlg = new ExceptionDialogBox();
            dlg.Text = e.ExceptionObject.ToString();
            dlg.ShowDialog();
            Process.GetCurrentProcess().Kill();
        }
    }
}
