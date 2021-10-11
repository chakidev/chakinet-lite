using Iesi.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora.Annotations
{
    /// <summary>
    /// Dependency検索専用のSegmentクラス
    /// "Attributes2" propertyの存在以外はSegmentとまったく同じ。
    /// AttributeをMapにMappingしているので、Key-Value形式（where Attributes2['key']='value'の形）でHQLに渡せる。
    /// なお、Segment派生にすると通常のSegmentの検索に影響を与える（重複してヒットする）ので無関係のクラスとする。
    /// </summary>
    public class Segment2 : Annotation
    {
        public Segment2()
        {
            this.StartChar = -1;
            this.EndChar = -1;
            this.StringValue = string.Empty;
            this.Attributes = new HashedSet<SegmentAttribute>();
            this.Attributes2 = new NHibernate.Collection.Generic.PersistentGenericMap<string, string>();
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

        [XmlIgnore]
        public virtual IDictionary<string, string> Attributes2 { get; set; }
    }
}
