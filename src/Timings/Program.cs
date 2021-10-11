using System;

namespace Timings
{
    class Program
    {
        static void Main(string[] args)
        {
            var tt = new Timings();

            Console.WriteLine("args={0}\n", string.Join(",", args));

            if (!tt.ParseArguments(args))
            {
                PrintUsage();
            }
            else
            {
                tt.Process();
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: Timings [Options] <InputFile> <Output>");
            Console.WriteLine("Options (default):");
            Console.WriteLine("  [-e=<encoding>] Input Encoding (SHIFT_JIS)");
            Console.WriteLine("  [-p=<projdct_id>] Project ID (0)");
            Console.WriteLine("  [-s] Provide word Surface as the first column, which is used to check correspondence with DB data. (false)");
            Console.WriteLine("  [-C] Do not pause on exit (false)");
            Console.WriteLine("InputFile - TSV File");
            Console.WriteLine("Output    - .db file for SQLite / .def file for Others");
        }
    }
}
