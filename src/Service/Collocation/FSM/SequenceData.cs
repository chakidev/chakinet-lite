using System;

namespace ChaKi.Service.Collocation.FSM
{
    internal class SequenceData : ICloneable
    {
        public Projection spos;
        public int freq;
        public int ngaps;

        public SequenceData()
        {
            freq = 0;
        }

        public SequenceData(SequenceData org)
        {
            this.spos = new Projection(org.spos);
            this.freq = org.freq;
            this.ngaps = org.ngaps;
        }

        public object Clone()
        {
            return new SequenceData(this);
        }
    }
}

