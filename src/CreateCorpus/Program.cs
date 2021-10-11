using System;

namespace CreateCorpus
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateCorpus    cc = new CreateCorpus();
            if (!cc.ParseArguments(args))
            {
                PrintUsage();
                return;
            }
            DateTime t0 = DateTime.Now;
            Console.WriteLine("Started at {0}", t0.ToLocalTime());
            cc.ParseInput();
            cc.SaveLexicon();
            cc.SaveSentences();
            cc.UpdateIndex();

            DateTime t1 = DateTime.Now;
            TimeSpan elapsed = t1 - t0;
            Console.WriteLine("Finished at {0}; Elapsed {1} minutes", t1.ToLocalTime(), elapsed.TotalMinutes);

            Console.WriteLine("\nPress Enter to exit.");
            Console.ReadLine();
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: CreateCorpus [-e=SHIFT_JIS|utf-8] [-t=Chasen|Mecab] <in:cabocha/chasen file> <out:db file (.db) for SQLite or (.def) for Others>");
        }
    }
}
