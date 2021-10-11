using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKiDiag
{
    class Program
    {
        public void Execute()
        {
            AppDomain domain = AppDomain.CreateDomain("sandbox");
            try
            {
                domain.ExecuteAssembly("ChaKi.NET.exe");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            Program app = new Program();
            app.Execute();
        }
    }
}
