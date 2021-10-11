using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class SegmentAttribute : AttributeBase
    {
        public virtual Segment Target { get; set; }
    }
}
