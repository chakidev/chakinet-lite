using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKiDump
{
    /// <summary>
    /// 初回Getで空文字列、２回目以降のGetで指定文字列を返すような文字列結合子生成クラス。
    /// and条件やカンマ区切り列の生成に使用する。
    /// </summary>
    class StringConnector
    {
        string m_val;
        string m_cur;

        public StringConnector(string val)
        {
            m_val = val;
            m_cur = "";
        }

        public string Get()
        {
            string ret = string.Copy(m_cur);
            m_cur = m_val;
            return ret;
        }
    }
}
