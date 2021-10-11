using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora.Annotations
{
    public abstract class AttributeBase
    {
        public virtual Int64 ID { get; set; }
        public virtual string Key { get; set; }
        public virtual string Value { get; set; }
        public virtual string Comment { get; set; }
        public virtual Project Proj { get; set; }
        public virtual User User { get; set; }
        public virtual TagSetVersion Version { get; set; }
    }
}
