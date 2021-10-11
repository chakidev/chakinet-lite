using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace ChaKi.Service.Database
{
    [SQLiteFunction(Name = "regexp", FuncType = FunctionType.Scalar, Arguments = 2)]
    public class SQLiteFunc_RegExp : SQLiteFunction
    {
        private static Regex m_RegExp;
        private static string m_LastPattern = "";

        public override object Invoke(object[] args)
        {
            string pattern = args[0] as string;
            string text = args[1] as string;

            if (!pattern.Equals(m_LastPattern)) {
                m_RegExp = new Regex( pattern );
            }
            if (m_RegExp.IsMatch(text))
            {
                return 1;
            }
            return 0;
        }
    }
}
