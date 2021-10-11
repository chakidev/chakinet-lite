using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChaKi.Service.Collocation.FSM
{
    internal class Projection : ICloneable
    {
        public List<SSequence> nodes;	// 複数の系列
        public uint scnt;	// 異なるsidを持つnodeの数

        public Projection()
        {
            this.scnt = 0;
            this.nodes = new List<SSequence>();
        }

        public Projection(Projection org)
        {
            this.scnt = org.scnt;
            this.nodes = new List<SSequence>();
            foreach (SSequence s in org.nodes)
            {
                this.nodes.Add(new SSequence(s));
            }
        }

        public object Clone()
        {
            return new Projection(this);
        }

        public void Add(SSequence sseq)
        {
            if (nodes.Count == 0 || nodes[nodes.Count - 1].sid != sseq.sid)
            {
                scnt++;
            }
            nodes.Add(sseq);
        }

        public int Count
        {
            get { return nodes.Count; }
        }

        public int GetSid(int i)
        {
            return nodes[i].sid;
        }

        public int GetLastPos(int i)
        {
            List<uint> seq = nodes[i].pos_seq;
            if (seq.Count == 0)
            {
                return -1;
            }
            return (int)seq[seq.Count - 1];
        }

        public uint GetSentenceCount()
        {
            return scnt;
        }

        public void RecalcSentenceCount()
        {
            int cursid = -1;
            scnt = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (cursid != nodes[i].sid)
                {
                    scnt++;
                    cursid = nodes[i].sid;
                }
            }
        }

        public void print()
        {
            Debug.WriteLine(string.Format("scnt={0}", scnt));
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].print();
            }
        }
    }
}
