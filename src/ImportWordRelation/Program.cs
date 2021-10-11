using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImportWordRelation
{
    class Program
    {
        static void Main(string[] args)
        {
            var wr = new ImportWordRelation();

            Console.WriteLine("args={0}\n", string.Join(",", args));

            if (!wr.ParseArguments(args))
            {
                PrintUsage();
            }
            else
            {
                wr.Process();
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: ImportWordRelation [Options] <InputFile> <Output>");
            Console.WriteLine("Options (default):");
            Console.WriteLine("  [-C] Do not pause on exit (false)");
            Console.WriteLine("  [-b] Make relations bi-directional (false)");
            Console.WriteLine("  [-a] Do not clear the mapping table; append mode (false)");
            Console.WriteLine("InputFile - TSV File");
            Console.WriteLine("Output    - .db file for SQLite / .def file for Others");
        }
    }
}
