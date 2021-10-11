using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Readers;
using System.IO;
using ChaKi.Service.Export;
using ChaKi.Service.Readers;
using NDesk.Options;

namespace ExportCorpus
{
    class Program
    {
        private bool m_Verbose = true;
        private bool m_ShowHelp = false;
        private bool m_ForceOverwrite = false;
        private string m_OutputFormat = "Mecab|Cabocha|UniDic2";
        private int m_ProjId = 0;

        static void Main(string[] args)
        {
            new Program().DoExport(args);
        }

        private void DoExport(string[] args)
        {
            // コマンドライン解析
            OptionSet os = new OptionSet()
                .Add("v|verbose", "Show progress (true)", delegate(string v) { m_Verbose = (v != null); })
                .Add("t|output_format=", "Output format (Mecab|Cabocha|UniDic2)", delegate(string v) { m_OutputFormat = v; })
                .Add("f|force_overwrite", "Do not confirm when output file exists (false)", delegate(string v) { m_ForceOverwrite = (v != null); })
                .Add("p|project", "Target project ID (0)", delegate (int v) { m_ProjId = v; })
                .Add("h|?|help", "Show this help", delegate(string v) { m_ShowHelp = v != null; });
            List<string> parameters = os.Parse(args);
            if (parameters.Count < 1 || m_ShowHelp)
            {
                ShowHelp(os);
                return;
            }
            string filename = null;
            if (parameters.Count >= 2)
            {
                filename = parameters[1];
            }

            // Corpusオブジェクトを生成
            Corpus crps = Corpus.CreateFromFile(parameters[0]);

            if (!m_ForceOverwrite && File.Exists(filename))
            {
                while (true)
                {
                    Console.Write("File exists. Overwrite? (y/n) ");
                    char c = Console.ReadKey().KeyChar;
                    Console.WriteLine();
                    if (c == 'N' || c == 'n')
                    {
                        return;
                    }
                    if (c == 'Y' || c == 'y')
                    {
                        break;
                    }
                }
            }

            // ReaderDefを与えられた文字列より決定
            CorpusSourceReaderFactory factory = CorpusSourceReaderFactory.Instance;
            ReaderDef def = factory.ReaderDefs.Find(m_OutputFormat);
            if (def == null)
            {
                Console.Error.WriteLine(string.Format("Unrecognized output format: {0}", m_OutputFormat));
                return;
            }

            ExportCorpus(crps, filename, def);
        }

        private void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: ExportCorpus [OPTIONS]+ input-file [output-file]");
            Console.WriteLine("Reads a Corpus database and dumps its content as a single text file.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
            Console.WriteLine("For output format descriptors, see 'ReaderDefs.xml'.");
            Console.WriteLine("E.g.");
            Console.WriteLine(" > ExportCorpus -v- sanshiro.db");
            Console.WriteLine(" > ExportCorpus -t=\"ChaSen|Cabocha\" sanshiro.db sanshiro_out.cabocha");
            Console.WriteLine(" > ExportCorpus -t=\"Mecab|Cabocha\" sanshiro.def sanshiro_out.cabocha");
        }

        private void ExportCorpus(Corpus crps, string filename, ReaderDef def)
        {
            TextWriter wr;
            if (filename == null)
            {
                wr = Console.Out;
            }
            else
            {
                wr = new StreamWriter(filename, false);
            }
            try
            {
                IExportService svc = new ExportServiceCabocha(wr, def);
                bool dummy = false;
                svc.ExportCorpus(crps, m_ProjId, ref dummy, new Action<int, object>(
                    (int v, object o) => { if (m_Verbose) Console.Error.Write(string.Format("{0}% exported.\r", v)); }));
            }
            finally
            {
                wr.Close();
            }
            if (m_Verbose) Console.Error.WriteLine("Done.\n");
        }
    }
}
