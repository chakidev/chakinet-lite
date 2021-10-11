using System.Collections.Generic;
using System.Xml.Serialization;
using Iesi.Collections.Generic;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class Link : Annotation
    {
        public static readonly List<Link> EmptyList = new List<Link>();

        // For Performance purpose:
        private Sentence m_FromSentence;
        private Sentence m_ToSentence;

        public Link()
        {
            this.ID = 0;
            this.Attributes = new HashedSet<LinkAttribute>();
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
                this.Proj.ID, this.User.ID, this.FromSentence?.ID, this.ToSentence?.ID);
        }
    }
}
