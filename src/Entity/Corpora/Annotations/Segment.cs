using System.Collections.Generic;
using System.Xml.Serialization;
using Iesi.Collections.Generic;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class Segment : Annotation
    {
        public static readonly List<Segment> EmptyList = new List<Segment>();

        public Segment()
        {
            this.StartChar = -1;
            this.EndChar = -1;
            this.StringValue = string.Empty;
            this.Attributes = new HashedSet<SegmentAttribute>();
        }

        public virtual long ID { get; set; }

        /// <summary>
        /// セグメント開始位置番号（文字単位） 各Document先頭からの相対位置
        /// </summary>
        public virtual int StartChar { get; set; }

        /// <summary>
        /// セグメント開始位置番号（文字単位） 各Document先頭からの相対位置
        /// </summary>
        public virtual int EndChar { get; set; }

        public virtual Lexeme Lex { get; set; }

        //------------------------------------------------------------------------------------------------------
        // 以下はDB設計上は冗長なフィールドであり、検索速度向上の目的で追加されている。

        /// <summary>
        /// このSegmentの開始位置にあるSentence 
        /// </summary>
        [XmlIgnore]
        public virtual Sentence Sentence { get; set; }

        /// <summary>
        /// このSegmentを含むDocumentへの参照
        /// </summary>
        [XmlIgnore]
        public virtual Document Doc { get; set; }

        [XmlIgnore]
        public virtual string StringValue { get; set; }

        [XmlIgnore]
        public virtual Iesi.Collections.Generic.ISet<SegmentAttribute> Attributes { get; set; }

        public override string ToString()
        {
            return string.Format("[Seg {0},{1},{2},{3},{4},{5},{6},{7},{8}]",
                this.ID, this.Tag.ID, this.Version.ID, this.Doc.ID,
                this.StartChar, this.EndChar, this.Proj.ID, this.User.ID,
                this.Sentence.ID);
        }
    }
}
