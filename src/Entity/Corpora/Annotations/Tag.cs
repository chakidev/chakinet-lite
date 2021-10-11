
using System.Xml.Serialization;
namespace ChaKi.Entity.Corpora.Annotations
{
    public class Tag
    {
        public Tag()
        {
        }

        public Tag(string type, string name)
        {
            this.Type = type;
            this.Name = name;
            this.Description = string.Empty;
        }

        public virtual int ID { get; set; }

        [XmlIgnore]
        public virtual TagSet Parent { get; set; }

        public virtual string Type { get; set; }

        public virtual string Name { get; set; }

        public virtual string Description { get; set; }

        public virtual TagSetVersion Version { get; set; }

        // TagType
        public static string SEGMENT = "Segment";
        public static string LINK = "Link";
        public static string GROUP = "Group";
    }
}
