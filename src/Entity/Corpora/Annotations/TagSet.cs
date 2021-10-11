using System;
using System.Linq;
using System.Collections.Generic;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class TagSet
    {
        public virtual int ID { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<Tag> Tags { get; set; }
        public virtual IList<TagSetVersion> Versions { get; set; }
        public virtual TagSetVersion CurrentVersion { get; set; }

        public TagSet()
        {
            this.Name = string.Empty;
            this.Tags = new List<Tag>();
            this.Versions = new List<TagSetVersion>();
        }

        public TagSet(string name)
        {
            this.Name = name;
            this.Tags = new List<Tag>();
            this.Versions = new List<TagSetVersion>();
        }

        public TagSet(TagSet src)
        {
            this.CopyFrom(src);
        }

        public void CopyFrom(TagSet src)
        {
            this.Name = src.Name;
            this.Tags = new List<Tag>();
            foreach (Tag t in src.Tags)
            {
                this.Tags.Add(t);
            }
            this.Versions = new List<TagSetVersion>();
            foreach (TagSetVersion v in src.Versions)
            {
                this.Versions.Add(v);
            }
            this.CurrentVersion = src.CurrentVersion;
        }

        public void MergeWith(TagSet src)
        {
            if (this.Name.Length == 0)
            {
                this.Name = src.Name;
            }
            foreach (Tag t in src.Tags)
            {
                if (!this.Tags.Contains(t))
                {
                    this.Tags.Add(t);
                }
            }
            foreach (TagSetVersion v in src.Versions)
            {
                if (!this.Versions.Contains(v))
                {
                    this.Versions.Add(v);
                }
            }
            if (this.CurrentVersion == null)
            {
                this.CurrentVersion = src.CurrentVersion;
            }
        }

        public void AddTag(Tag tag)
        {
            this.Tags.Add(tag);
        }

        public void AddVersion(TagSetVersion version)
        {
            this.Versions.Add(version);
            if (version.IsCurrent)
            {
                this.CurrentVersion = version;
            }

        }

        public Tag FindTag(string type, string name)
        {
            foreach (Tag t in this.Tags)
            {
                if (t == null) continue;
                if (t.Type.Equals(type) && t.Name.Equals(name))
                {
                    return t;
                }
            }
            return null;
        }

        public Tag FindOrAddTag(string type, string name)
        {
            foreach (Tag t in this.Tags)
            {
                if (t == null) continue;
                if (t.Type.Equals(type) && t.Name.Equals(name))
                {
                    return t;
                }
            }
            Tag tag = new Tag(type, name);
            AddTag(tag);
            return tag;
        }
    }
}
