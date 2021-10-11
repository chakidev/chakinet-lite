using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Service.Readers
{
    internal class CompositeWordChunk
    {
        private List<CompositeWordChunkItem> m_Items;

        // IOB2タグから得られる、Chunkに与えるべきPOS
        public string ChunkPOS;

        public CompositeWordChunk()
        {
            m_Items = new List<CompositeWordChunkItem>();
            Clear();
        }

        public void Clear()
        {
            m_Items.Clear();
            this.ChunkPOS = string.Empty;
        }

        public bool IsEmpty()
        {
            return (m_Items.Count == 0);
        }

        public void Add(int id, string surface, string lemma, string features, int depid, string deptag)
        {
            m_Items.Add(new CompositeWordChunkItem(id, surface, lemma, features, depid, deptag));
        }

        public string ToConllSingleLine()
        {
            var head = FindHeadIndex();

            var sb = new StringBuilder();
            // 代表させるIDを選択（外に出るdepidを持つもののid)

            sb.AppendFormat("{0}\t", m_Items[head].Index);
            sb.AppendFormat("{0}\t", GetSurface());
            sb.AppendFormat("{0}\t", GetLemma());
            sb.AppendFormat("{0}\t", GetPOS());
            var headfeatures = m_Items[head].Features;
            headfeatures.Remove("SP");
            if (m_Items[m_Items.Count-1].HasSpace)
            {
                headfeatures.Add("SP");
            }
            sb.AppendFormat("{0}\t", string.Join("|", headfeatures.ToArray()));
            sb.AppendFormat("{0}\t{1}\t", m_Items[head].DepIndex, m_Items[head].DepTag);
            sb.Append("_\t_\n");
            return sb.ToString();
        }

        private int FindHeadIndex()
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                var k = m_Items[i].DepIndex;
                if (m_Items.Find(item => item.Index == k) == null)
                {
                    // 係り先が複合語外部なのでHeadであると考えられる.
                    return i;
                }
            }
            throw new Exception("Cannot determine the head of composite word.");
        }
        
        public string GetSurface()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].HasSpace && i != m_Items.Count - 1)
                {
                    sb.Append(m_Items[i].Surface + " ");
                }
                else
                {
                    sb.Append(m_Items[i].Surface);
                }
            }
            return sb.ToString();
        }

        public string GetLemma()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < m_Items.Count; i++)
            {
                var lemma = (m_Items[i].Lemma == "_") ? string.Empty : m_Items[i].Lemma;
                if (m_Items[i].HasSpace && i != m_Items.Count - 1)
                {
                    sb.Append(lemma + " ");
                }
                else
                {
                    sb.Append(lemma);
                }
            }
            var result = sb.ToString().Trim();
            return (result.Length == 0) ? "_" : result;
        }

        public string GetPOS()
        {
            if (this.ChunkPOS == "RB")
            {
                return "RB\tRB";
            }
            else if (this.ChunkPOS == "IN")
            {
                return "P\tIN";
            }
            else if (this.ChunkPOS == "DT")
            {
                return "DT\tDT";
            }
            else if (this.ChunkPOS == "JJ")
            {
                return "JJ\tJJ";
            }
            else if (this.ChunkPOS == "NN")
            {
                return "NN\tNN";
            }
            else if (this.ChunkPOS == "PP")
            {
                return "PP\tPP";
            }
            else if (this.ChunkPOS == "PRP" || this.ChunkPOS == "PRP-S")
            {
                return "PR\tPRP";
            }
            else if (this.ChunkPOS == "PRN")
            {
                return "PR\tPRP";
            }
            else if (this.ChunkPOS == "UH")
            {
                return "UH\tUH";
            }

            throw new Exception("Cannot determine POS of composite word.");
        }

    }

    internal class CompositeWordChunkItem
    {
        public CompositeWordChunkItem(int id, string surface, string lemma, string features, int depid, string deptag)
        {
            this.Index = id;
            this.Surface = surface;
            this.Lemma = lemma;
            this.DepIndex = depid;
            this.DepTag = deptag;
            if (features != "_")
            {
                this.Features = features.Split('|').ToList();
            }
            else
            {
                this.Features = new List<string>();
            }
            this.HasSpace = this.Features.Contains("SP");
        }

        public int Index;

        public string Surface;

        public List<string> Features;

        public bool HasSpace;

        public string Lemma;

        public int DepIndex;

        public string DepTag;
    }
}
