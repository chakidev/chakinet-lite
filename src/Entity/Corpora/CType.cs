using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class CType : Property
    {
        public CType()
        {
            m_Names = new string[2];
        }

        public CType(string name)
        {
            FromString(name);
        }

        public CType(CType src)
            : base(src)
        {
            m_ID = src.m_ID;
            if (src.m_CType != null)
            {
                m_CType = string.Copy(src.m_CType);
            }
            m_Names = new string[2];
            for (int i = 0; i < 2; i++)
            {
                if (src.m_Names[i] != null)
                {
                    m_Names[i] = string.Copy(src.m_Names[i]);
                }
            }
        }
        
        public override object Clone()
        {
            return new CType(this);
        }

        private void FromString(string name)
        {
            m_Names = new string[2];
            string[] parts = name.Split(new char[] { '・' });
            m_CType = string.Join("-", parts);  // 区切り記号の「・」をDB/ChaKi内部では「-」に変更する
            for (int i = 0; i < parts.Length; i++)
            {
                if (i < 2)
                {
                    m_Names[i] = parts[i];
                }
            }
        }

        public override string ToString()
        {
            return this.Name;
        }

        public override string StrVal
        {
            get { return this.Name; }
            set { FromString( value ); }
        }

        public override object Target
        {
            get { return this; }
        }

        public virtual int ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }
        public virtual string Name1
        {
            get { return m_Names[0]; }
            set { m_Names[0] = value; }
        }
        public virtual string Name2
        {
            get { return m_Names[1]; }
            set { m_Names[1] = value; }
        }
        public virtual string Name
        {
            get { return m_CType; }
            set { m_CType = value; }
        }

        public static CType Default { get; set; }

        private int m_ID;
        private string[] m_Names;
        private string m_CType;
    }
}
