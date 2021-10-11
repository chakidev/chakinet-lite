using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Text2Corpus
{
    internal class SupportedEncodings
    {
        static SupportedEncodings()
        {
            Encodings = new Dictionary<string,Encoding>();
            Encodings.Add("utf-8", new UTF8Encoding(false));  // Mecab/CabochaがBOMを受け付けないためBOMなしでwrite
            Encodings.Add("shift-jis", Encoding.GetEncoding("shift-jis"));
            Encodings.Add("EUC-JP", Encoding.GetEncoding("EUC-JP"));
            Encodings.Add("utf-16", Encoding.GetEncoding("utf-16"));
        }

        public static Encoding ShiftJIS { get { return Encodings["shift-jis"]; } }
        public static Encoding UTF8 { get { return Encodings["utf-8"]; } }
                
        private static Dictionary<string, Encoding> Encodings;

        public static Encoding Find(string name)
        {
            Encoding result = null;
            Encodings.TryGetValue(name, out result);
            return result;
        }

        public static List<Encoding> List
        {
            get
            { 
                return Encodings.Values.ToList();
            }
        }

        public static List<string> NameList
        {
            get
            {
                return Encodings.Keys.ToList();
            }
        }

    }
}
