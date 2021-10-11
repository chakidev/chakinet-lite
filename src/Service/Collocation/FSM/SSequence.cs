using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChaKi.Service.Collocation.FSM
{
    internal class SSequence : ICloneable
    {
        public int sid;
        public List<uint> pos_seq;	// ひとつの文におけるposの列（つまり系列）を表す
        public int ngaps;		    // この系列におけるgapの数

        public SSequence(int _sid)
        {
            this.sid = _sid;
            this.ngaps = 0;
            this.pos_seq = new List<uint>();
        }

        public SSequence(SSequence org)
        {
            this.sid = org.sid;
            this.pos_seq = new List<uint>(org.pos_seq);
            this.ngaps = org.ngaps;
        }

        public object Clone()
        {
            return new SSequence(this);
        }

        public int GetLength()
        {
            return pos_seq.Count;
        }

        public void Add(uint pos)
        {
            if (pos_seq.Count > 0 && pos - pos_seq[pos_seq.Count - 1] > 1)
            {
                ngaps++;
            }
            pos_seq.Add(pos);
        }

        public void print()
        {
            Debug.Write(string.Format("  sid={0}    ngaps={1} {{ ", this.sid, this.ngaps));
            for (int i = 0; i < pos_seq.Count; i++)
            {
                Debug.Write(string.Format("{0} ", pos_seq[i]));
            }
            Debug.WriteLine("}");
        }
    }
}
