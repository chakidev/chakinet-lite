using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora.Annotations
{
    public abstract class Annotation
    {
        /// <summary>
        /// このアノテーションの主値であるTag定義
        /// </summary>
        public virtual Tag Tag { get; set; }

        /// <summary>
        /// このアノテーションがどのVersionのTagSetによって付加されたか
        /// </summary>
        public virtual TagSetVersion Version { get; set; }

        /// <summary>
        /// このアノテーション付加作業を行ったProjectへの参照
        /// </summary>
        public virtual Project Proj { get; set; }

        /// <summary>
        /// このアノテーション付加作業を行ったUserへの参照
        /// </summary>
        public virtual User User { get; set; }

        /// <summary>
        /// 個別のアノテーションに対するコメント
        /// </summary>
        public virtual string Comment { get; set; }

        /// <summary>
        /// DoubleQuoteと改行をエスケープしたComment文字列を得る
        /// </summary>
        /// <param name="ann"></param>
        /// <returns></returns>
        public virtual string GetNormalizedCommentString()
        {
            return GetNormalizedCommentString(this.Comment);
        }

        public static string GetNormalizedCommentString(string str)
        {
            if (str == null)
            {
                return string.Empty;
            }
            return str.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
