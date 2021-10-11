using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class MWE
    {
        public MWE()
        {
            this.Items = new List<MWENode>();
        }

        public Lexeme Lex { get; set; }
        public List<MWENode> Items { get; set; }
        public string Dictionary { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var item in this.Items)
            {
                sb.AppendFormat("{0} ", item.ToString());
            }
            return sb.ToString().TrimEnd(' ');
        }
    }

    public class MWENode
    {
        public MWENode()
        {
        }

        public MWENodeType NodeType { get; set; }

        public string Label { get; set; }

        public Lexeme SrcLex { get; set; }

        public Lexeme LinkLex { get; set; }

        public string POS { get { return SrcLex?.PartOfSpeech.ToString(); } }
        public string CType { get { return SrcLex?.CType.ToString(); } }
        public string CForm { get { return SrcLex?.CForm.ToString(); } }

        public int DependsTo { get; set; }

        public string DependsAs { get; set; }

        public override string ToString()
        {
            return this.Label;
        }
    }

    public enum MWENodeType
    {
        Word,
        Dummy,
        Placeholder,
        Invalid
    }
}
