using System.Collections.Generic;
using ChaKi.Entity.Corpora.Annotations;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora
{
    public class Project
    {
        public virtual int ID { get; set; }

        [XmlIgnore]
        public virtual IList<TagSet> TagSetList { get; set; }

        [XmlIgnore]
        public virtual IList<User> Users { get; set; }

        [XmlIgnore]
        public virtual int DocumentPrivilege { get; set; }

        [XmlIgnore]
        public virtual int TagsetPrivilege { get; set; }

        [XmlIgnore]
        public virtual string Comments { get; set; }

        public Project()
        {
            this.ID = 0;
            this.TagSetList = new List<TagSet>();
            this.Users = new List<User>();
        }

        public void AddTagSet(TagSet ts)
        {
            this.TagSetList.Add(ts);
        }

        public void AddUser(User user)
        {
            this.Users.Add(user);
        }

        public Tag FindTag(string type, string name)
        {
            foreach (TagSet tset in this.TagSetList)
            {
                Tag t = tset.FindTag(type, name);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }
    }
}
