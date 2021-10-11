using System.Collections.Generic;
using System.Xml.Serialization;
using Iesi.Collections.Generic;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class Group : Annotation
    {
        public static readonly List<Group> EmptyList = new List<Group>();

        public Group()
        {
            this.Tags = new List<Annotation>();
            this.Attributes = new HashedSet<GroupAttribute>();
        }

        public virtual long ID { get; set; }

        public virtual IList<Annotation> Tags { get; set; }

        [XmlIgnore]
        public virtual Iesi.Collections.Generic.ISet<GroupAttribute> Attributes { get; set; }

    }
}
