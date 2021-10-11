using Iesi.Collections.Generic;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora.Annotations
{
    /// <summary>
    /// Dependency検索専用のLinkクラス
    /// "Attributes2" propertyの存在以外はLinkとまったく同じ。
    /// AttributeをMapにMappingしているので、Key-Value形式（where Attributes2['key']='value'の形）でHQLに渡せる。
    /// なお、Link派生にすると通常のLinkの検索に影響を与える（重複してヒットする）ので無関係のクラスとする。
    /// </summary>
    public class Link2 : Annotation
    {
        // For Performance purpose:
        private Sentence m_FromSentence;
        private Sentence m_ToSentence;

        public Link2()
        {
            this.ID = 0;
            this.Attributes = new HashedSet<LinkAttribute>();
            this.Attributes2 = new NHibernate.Collection.Generic.PersistentGenericMap<string, string>();
        }

        public virtual long ID { get; set; }

        public virtual Segment From { get; set; }

        public virtual Segment To { get; set; }

        public virtual Sentence FromSentence { get; set; }

        public virtual Sentence ToSentence { get; set; }

        public virtual Lexeme Lex { get; set; }

        [XmlIgnore]
        public virtual Iesi.Collections.Generic.ISet<LinkAttribute> Attributes { get; set; }


        // MappingされないProperty: 常にtrue
        // SLATではConstraintによって表現され、個々のLinkインスタンスごとに指定することはできない.
        public bool IsDirected { get { return true; } }
        public bool IsTransitive { get { return true; } }

        public override string ToString()
        {
            return string.Format("[Link {0},{1},{2},{3},{4},{5},{6},{7},{8}]",
                this.ID, this.Tag.ID, this.Version.ID, this.From.ID, this.To.ID,
                this.Proj.ID, this.User.ID, this.FromSentence.ID, this.ToSentence.ID);
        }

        [XmlIgnore]
        public virtual IDictionary<string, string> Attributes2 { get; set; }
    }
}
