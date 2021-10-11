using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Service.Search;
using ChaKi.Service.Database;
using System.Data.Common;

namespace Timings
{
    public class WordEnumerator : IEnumerator<KeyValuePair<Int32,string>>
    {
        private DbDataReader m_Reader;

        public WordEnumerator(DbConnection conn, int projid)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = 600;
                cmd.CommandText = string.Format("SELECT w.id,l.surface from word w INNER JOIN lexeme l ON w.lexeme_id=l.id AND w.project_id={0} ORDER BY w. sentence_id ASC, w.position ASC", projid);
                m_Reader = cmd.ExecuteReader();
                m_Reader.Read();
            }
        }

        public KeyValuePair<Int32, string> Current
        {
            get
            {
                return new KeyValuePair<Int32, string>((Int32)m_Reader[0], (string)m_Reader[1]);
            }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            return m_Reader.Read();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            m_Reader.Close();
        }
    }
}
