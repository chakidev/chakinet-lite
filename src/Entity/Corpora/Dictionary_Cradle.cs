using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class Dictionary_Cradle : Dictionary
    {
        private string m_Name;
        public override string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// サービスのベースURL
        /// </summary>
        public Uri Url { get; set; }

        public Dictionary_Cradle(string path, string name)
        {
            m_Name = name ?? path;
            this.Url = new Uri(path);
        }
    }
}
