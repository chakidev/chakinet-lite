using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ChaKiDump
{
    class OldDbParameter
    {
        public string Server { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }

        public OldDbParameter()
        {
            Server = string.Empty;
            User = string.Empty;
            Password = string.Empty;
            Name = string.Empty;
        }

        /// <summary>
        /// defファイルを読んでパラメータをセットする
        /// </summary>
        /// <param name="path"></param>
        public void ParseDefFile(string path)
        {
            using (StreamReader rd = new StreamReader(path))
            {
                string line;
                while ((line = rd.ReadLine()) != null)
                {
                    string[] fields = line.Split(new char[] { '=' });
                    if (fields.Length < 2) continue;
                    if (fields[0].Equals("corpusname"))
                    {
                        Name = fields[1];
                    }
                    else if (fields[0].Equals("server"))
                    {
                        Server = fields[1];
                    }
                    else if (fields[0].Equals("user"))
                    {
                        User = fields[1];
                    }
                    else if (fields[0].Equals("password"))
                    {
                        Password = fields[1];
                    }
                }
            }
        }
    }
}
