using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ChaKi.GUICommon;
using ChaKi.Entity.Settings;
using ChaKi.Service.Search;
using DependencyEditSLA;
using ChaKi.Options;
using System.Threading;
using System.Globalization;
using ChaKi.Common;
using ChaKi.Views.KwicView;
using System.ComponentModel;
using ChaKi.Entity.Corpora;
using ChaKi.Common.Settings;

namespace ChaKi
{
    internal sealed class Program
    {
        static public MainForm MainForm;
        static public string ProgramDir { get; private set; }
        static public string SettingDir { get; private set; }
        static private string m_SettingFile;
        static private string m_SettingFileGUI;
        static private string m_SettingFileTagAppearance;
        static private string m_SettingFilePropertyBox;
        static private string m_SettingFileDictionary;
        static public string SettingFileWordColor { get; private set; }
        static private Program instance;
        private string[] args;

        public static bool IsIgnoreDockSetting = false;

        private Program(string[] args)
        {
            this.args = args;

            IsIgnoreDockSetting = args.Any(a => (a.ToLower() == "-r" || a.ToLower() == "/r"));
        }

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);
                instance = new Program(args);
                instance.Start();
            }
            catch (Exception ex)
            {
                ExceptionDialogBox dlg = new ExceptionDialogBox();
                dlg.Text = ex.ToString();
                dlg.ShowDialog();
                Process.GetCurrentProcess().Kill();
            }

        }

        public void Start()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // スプラッシュスクリーンを表示
            SplashScreen sscrn = new SplashScreen(this.InitializationProc);
            DialogResult res = sscrn.ShowDialog();
            switch (res)
            {
                case DialogResult.Cancel:
                    return;
                case DialogResult.OK:
                    User.Current = new User() { Name = sscrn.UserName, Password = sscrn.Password };
                    break;
                case DialogResult.Abort:  // Auto Logon
                    User.Current = new User() { Name = User.DefaultName, Password = string.Empty };
                    break;
            }
            sscrn.Dispose();

            Application.ApplicationExit += ApplicationExitHandler;
            Application.Idle += ApplicationIdleHandler;

            // メイン画面の表示
            try
            {
                if (GUISetting.Instance.UILocale != null)
                {
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(GUISetting.Instance.UILocale);
                }
                MainForm = new MainForm();
                Application.Run(MainForm);
            }
            catch (FileNotFoundException e)
            {
                string msg = string.Format("File(s) not found.\nPlease check installation.\n\n{0}", e.Message);
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
                return;
            }
        }

        public void InitializationProc(object sender, DoWorkEventArgs args)
        {
            SplashScreen sscrn = args.Argument as SplashScreen;

            sscrn.Invoke(new Action<SplashScreen>(delegate(SplashScreen s) { s.ProgressMsg = "Loading Entity Mappings..."; }), sscrn);

            // NHibernateの初期化（エンティティマッピングをコンパイルさせる）
            try
            {
                SearchConfiguration.GetInstance().Initialize();
            }
            catch (Exception e)
            {
                ExceptionDialogBox dlg = new ExceptionDialogBox();
                dlg.Text = e.ToString();
                dlg.ShowDialog();
                return;
            }

            sscrn.Invoke(new Action<SplashScreen>(delegate(SplashScreen s) { s.ProgressMsg = "Loading User Settings and Corpus Information..."; }), sscrn);

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

            m_SettingFilePropertyBox = SettingDir + @"\PropertyBoxSettings.xml";
            try
            {
                PropertyBoxSettings.Load(m_SettingFilePropertyBox);
            }
            catch (FileNotFoundException)
            {
                PropertyBoxSettings.Default();
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error reading user settings for PropertyBox : {0}", e.Message));
                PropertyBoxSettings.Default();
            }


            m_SettingFile = SettingDir + @"\UserSettings.xml";
            try
            {
                UserSettings.GetInstance().Load(m_SettingFile);
            }
            catch (FileNotFoundException)
            {
                // just reset content
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error reading user settings : {0}", e.Message));
            }

            m_SettingFileGUI = SettingDir + @"\UserSettingsGUI.xml";
            try
            {
                GUISetting.Load(m_SettingFileGUI);
                KwicView.Settings = GUISetting.Instance.KwicViewSettings;
            }
            catch (FileNotFoundException)
            {
                // just reset content
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error reading user settings for GUI : {0}", e.Message));
            }

            m_SettingFileTagAppearance = SettingDir + @"\UserSettingsTagAppearance.xml";
            try
            {
                TagSetting.Load(m_SettingFileTagAppearance);
            }
            catch (FileNotFoundException)
            {
                // just reset content
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error reading user settings for GUI : {0}", e.Message));
            }

            m_SettingFileDictionary = SettingDir + @"\DictionarySettings.xml";
            try
            {
                DictionarySettings.Load(m_SettingFileDictionary);
            }
            catch (FileNotFoundException)
            {
                // just reset content
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error reading dictionary settings : {0}", e.Message));
            }

            // 描画オブジェクトを初期化
            SegmentPens.AddPens(TagSetting.Instance.Segment);
            LinkPens.AddPens(TagSetting.Instance.Link);

            sscrn.Invoke(new Action<SplashScreen>(delegate(SplashScreen s) { s.Shrink = GUISetting.Instance.AutoLogon; }), sscrn);

            SettingFileWordColor = SettingDir + @"\WordColorSettings.xml";
            try
            {
                WordColorSettings.GetInstance().Load(SettingFileWordColor);
            }
            catch (FileNotFoundException)
            {
                // just reset content
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Error reading user settings for WordColors : {0}", e.Message));
            }

            sscrn.Invoke(new Action<SplashScreen>(delegate(SplashScreen s) { s.ProgressMsg = "Initialization Completed. Please Logon."; }), sscrn);
        }

        static void ApplicationExitHandler(object sender, EventArgs e)
        {
            Application.Idle -= ApplicationIdleHandler;

            UserSettings.GetInstance().Save(m_SettingFile);

            GUISetting.Instance.KwicViewSettings = KwicView.Settings;
            GUISetting.Save(m_SettingFileGUI);
            PropertyBoxSettings.Save(m_SettingFilePropertyBox);
            TagSetting.Save(m_SettingFileTagAppearance);
            DictionarySettings.Save(m_SettingFileDictionary);
        }

        static void ApplicationIdleHandler(object sender, EventArgs e)
        {
            if (MainForm != null)
            {
                MainForm.IdleUpdate();
            }
            DepEditControl.UIUpdate();
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionDialogBox dlg = new ExceptionDialogBox();
            dlg.Text = e.ExceptionObject.ToString();
            dlg.ShowDialog();
            Process.GetCurrentProcess().Kill();
        }
    }
}