using System;
using System.Collections.Generic;
using System.Text;

namespace AttachBib
{
    class Program
    {
        static void Main(string[] args)
        {
            AttachBib cc = new AttachBib();

            if (!cc.ParseArguments(args))
            {
                PrintUsage();
                Console.WriteLine("\nPress Enter to exit.");
                Console.ReadLine();
                return;
            }
            cc.Execute();
            Console.WriteLine("\nPress Enter to exit.");
            Console.ReadLine();
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: addbib <in:xml bib file> <out:db file (.db) for SQLite or (.def) for Others>");
        }
    }
}
