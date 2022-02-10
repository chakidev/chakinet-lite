using System;

namespace ConvertUDCorpus
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new UDConverter().Convert(args[0], args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
