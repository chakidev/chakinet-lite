using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora
{
    public class Bunsetsu
    {
        public Bunsetsu()
        {
            m_Sen = null;
            m_Pos = -1;
            m_DependsTo = null;
            m_DependsAs = "";
        }

        public virtual int ID
        {
            get { return m_id; }
            set { m_id = value; }
        }
        /// <summary>
        /// 文内の文節ID（0,1,...）
        /// </summary>
        public virtual int Pos
        {
            get { return m_Pos; }
            set { m_Pos = value; }
        }
        public virtual Sentence Sen
        {
            get { return m_Sen; }
            set { m_Sen = value; }
        }
        public virtual Bunsetsu DependsTo
        {
            get { return m_DependsTo; }
            set { m_DependsTo = value; }
        }
        public virtual string DependsAs
        {
            get { return m_DependsAs; }
            set { m_DependsAs = value; }
        }

        public override string ToString()
        {
#if true
            throw new NotImplementedException();    // This class is obsolete
#else
            string s = "";
            foreach (Word w in this.Sen.Words)
            {
                if (w.Bunsetsu != null && w.Bunsetsu.Pos == this.Pos)
                {
                    if (s.Length > 0)
                    {
                        s += " ";
                    }
                    s += w.Lex.Surface;
                }
            }
            return s;
#endif
        }

        private int m_id;
        private int m_Pos;
        private Sentence m_Sen;
        private Bunsetsu m_DependsTo;
        private string m_DependsAs;
    }
}
