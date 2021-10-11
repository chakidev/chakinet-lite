using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;

public static class UserDefinedFunctions
{
    public static readonly RegexOptions Options =
        RegexOptions.IgnorePatternWhitespace |
        RegexOptions.Singleline;

    [SqlFunction]
    public static SqlBoolean RegexMatch(SqlChars input, SqlString pattern)
    {
        Regex regex = new Regex(pattern.Value, Options);
        return regex.IsMatch(new string(input.Value));
    }
}
