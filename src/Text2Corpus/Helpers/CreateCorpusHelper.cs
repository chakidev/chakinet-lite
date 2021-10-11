using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using ChaKi.Text2Corpus.Properties;

namespace ChaKi.Text2Corpus.Helpers
{
    internal class CreateCorpusHelper
    {
        public static string CreateCorpusPath { get; private set; }

        public static void Setup()
        {
            CreateCorpusPath = Path.Combine(Program.ProgramDir, "CreateCorpusSLA.exe");
        }

        public static void InvokeCreateCorpus(string srcfile, string destfile)
        {
            if (CreateCorpusPath == null)
            {
                // "CreateCorpusが見つかりません."
                throw new Exception(Resources.S018);
            }
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = CreateCorpusPath;
            processStartInfo.WorkingDirectory = Program.ProgramDir;
            processStartInfo.Arguments = "-t=Auto -e=Auto -s";// "-t=Auto -e=Auto -s -C";
            processStartInfo.Arguments += string.Format(" \"{0}\" \"{1}\"", srcfile, destfile);
            Process.Start(processStartInfo).WaitForExit();
        }
    }
}
