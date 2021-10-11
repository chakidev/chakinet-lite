using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using ChaKi.Common;
using ChaKi.Entity.Corpora;
using System.Drawing.Extended;
using ChaKi.Entity.Corpora.Annotations;
using System.Drawing.Drawing2D;

namespace DependencyEditSLA.Widgets
{
    internal class SegmentBoxGroup
    {
        public SegmentBoxGroup(int index, Group g)
        {
            this.Index = index;
            this.Model = g;
        }

        public Group Model { get; set; }
        public int Index { get; set; }
    }
}
