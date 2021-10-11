using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace ChaKi.Entity
{
    public class DBParameter
    {
        public string DBType { get; set; }
        public string Server { get; set; }
        public string Login { get; set; }
        [XmlIgnore]
        public string Password { get; set; }
        /// <summary>
        /// データベース名（SQLiteの場合は拡張子なしのファイル名）
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// データベースパス名（SQLiteのみ。DBファイルのフルパス名）
        /// </summary>
        public string DBPath { get; set; }

        public DBParameter()
        {
            DBType = "SQLite";
            Server = string.Empty;
            Login = string.Empty;
            Password = string.Empty;
            Name = string.Empty;
            DBPath = string.Empty;
        }

        public DBParameter(string dbms, string server, string login, string password)
        {
            this.DBType = dbms;
            this.Server = server;
            this.Login = login;
            this.Password = password;
            this.Name = string.Empty;
            this.DBPath = string.Empty;
        }


        /// <summary>
        /// defファイルを読んでパラメータをセットする
        /// </summary>
        /// <param name="path"></param>
        public void ParseDefFile(string path)
        {
            this.DBPath = path;
            using (StreamReader rd = new StreamReader(path))
            {
                string line;
                while ((line = rd.ReadLine()) != null)
                {
                    string[] fields = line.Split(new char[] { '=' });
                    if (fields.Length < 2) continue;
                    if (fields[0].Equals("db"))
                    {
                        DBType = fields[1];
                    }
                    else if (fields[0].Equals("corpusname"))
                    {
                        Name = fields[1];
                    }
                    else if (fields[0].Equals("server"))
                    {
                        Server = fields[1];
                    }
                    else if (fields[0].Equals("user"))
                    {
                        Login = fields[1];
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
