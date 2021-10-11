using System;
using System.Collections.Generic;
//using System.Linq;
using System.Windows.Forms;

namespace TextFormatter
{
    static class Program
    {
        static public string[] args;
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] argv)
        {
            args = argv;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
