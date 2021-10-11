using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace ChaKiDump
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">args[0]: ダンプ元コーパスのdefファイル</param>
        static void Main(string[] args)
        {
            OldDbParameter dbParam = new OldDbParameter();
            try
            {
                dbParam.ParseDefFile(args[0]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to Read .def File.\n\n{0}", ex.ToString());
                return;
            }

            ChaKiDump dumper = null;
            try
            {
                dumper = new ChaKiDump(
                    string.Format("Server={0};User ID={1};Password={2}",
                        dbParam.Server,
                        dbParam.User,
                        dbParam.Password),
                    dbParam.Name);

                dumper.DumpLexicon();
                dumper.DumpBibs();
                dumper.DumpSentence();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("--- ERROR\n{0}", ex.ToString());
                return;
            }
            finally
            {
                if (dumper != null) dumper.Close();
            }
        }


    }
}
