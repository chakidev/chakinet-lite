
using System.Xml.Serialization;
namespace ChaKi.Entity.Corpora.Annotations
{
    public class TagSetVersion
    {
        public virtual int ID { get; set; }

        public virtual string Version { get; set; }

        public virtual int Revision { get; set; }

        public bool IsCurrent { get; set; }

        [XmlIgnore]
        public virtual TagSet TagSet { get; set; }

        public TagSetVersion()
        {
        }

        public TagSetVersion(string version, int revision, bool isCurrent)
        {
            this.ID = 0;
            this.Version = version;
            this.Revision = revision;
            this.IsCurrent = isCurrent;
        }
    }
}
