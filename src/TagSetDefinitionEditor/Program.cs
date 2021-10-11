using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ChaKi.GUICommon;
using System.Diagnostics;
using System.IO;

namespace ChaKi.TagSetDefinitionEditor
{
    internal sealed class Program
    {
        private string[] args;
        static private Program instance;

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);
            try
            {
                if (args.Length < 2)
                {
                    ParamsDialog pdlg = new ParamsDialog();
                    if (pdlg.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    args = new string[2] { pdlg.DbFile, pdlg.TagSetName };
                }
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

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionDialogBox dlg = new ExceptionDialogBox();
            dlg.Text = e.ExceptionObject.ToString();
            dlg.ShowDialog();
            Process.GetCurrentProcess().Kill();
        }

        private Program(string[] args)
        {
            this.args = args;
        }

        public void Start()
        {
            // メイン画面の表示
            try
            {
                TagSetDialog dlg = new TagSetDialog(this.args[0], this.args[1]);
                Application.Run(dlg);
            }
            catch (FileNotFoundException e)
            {
                string msg = string.Format("File(s) not found.\nPlease check installation.\n\n{0}", e.Message);
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
    }
}
