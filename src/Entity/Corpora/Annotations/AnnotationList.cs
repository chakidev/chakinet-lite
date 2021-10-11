using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora.Annotations
{
    public class AnnotationList
    {
        public List<Segment> Segments { get; set; }
        public List<Link> Links { get; set; }
        public List<Group> Groups { get; set; }

        public AnnotationList()
        {
            this.Segments = new List<Segment>();
            this.Links = new List<Link>();
            this.Groups = new List<Group>();
        }

        public void Clear()
        {
            this.Segments.Clear();
            this.Links.Clear();
            this.Groups.Clear();
        }
    }
}
