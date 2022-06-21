using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class PartOfSpeech : Property
    {
        public PartOfSpeech()
        {
            m_Names = new string[4];
        }

        public PartOfSpeech(PartOfSpeech src)
            : base(src)
        {
            m_ID = src.m_ID;
            if (src.m_PartOfSpeech != null)
            {
                m_PartOfSpeech = string.Copy(src.m_PartOfSpeech);
            }
            m_Names = new string[4];
            for (int i = 0; i < 4; i++)
            {
                if (src.m_Names[i] != null)
                {
                    m_Names[i] = string.Copy(src.m_Names[i]);
                }
            }
        }

        public override object Clone()
        {
            return new PartOfSpeech(this);
        }

        public PartOfSpeech(string name, bool isConllU = false)
         {
            if (isConllU)
            {
                FromStringConllU(name);
            }
            else
            {
                FromString(name);
            }
         }

        public override string StrVal
        {
            get { return this.Name; }
            set { this.FromString( value ); }
        }

        public override object Target
        {
            get { return this; }
        }

        public static PartOfSpeech Default { get; set; }


        private void FromString( string name )
        {
            m_PartOfSpeech = name;
            m_Names = new string[4];
            string[] parts = name.Split(new char[] { '-' });
            for (int i = 0; i < parts.Length; i++)
            {
                if (i < 4)
                {
                    m_Names[i] = parts[i];
                }
            }
        }

        // CONLL-U(UD)ではnameはUPOS-XPOSで、UPOSは'-'を含まないが、XPOSは'-'を含むことがある。
        // UPOSをname[0]に, XPOSをname[1]に入れる。
        private void FromStringConllU(string name)
        {
            m_PartOfSpeech = name;
            m_Names = new string[4];
            var idx = name.IndexOf('-');
            if (idx < 0)
            {
                m_Names[0] = name;
            }
            else
            {
                m_Names[0] = name.Substring(0, idx);
                m_Names[1] = name.Substring(idx + 1);
            }
        }

        public override string ToString()
        {
            return this.Name;
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
        public virtual string Name3
        {
            get { return m_Names[2]; }
            set { m_Names[2] = value; }
        }
        public virtual string Name4
        {
            get { return m_Names[3]; }
            set { m_Names[3] = value; }
        }
        public virtual string Name
        {
            get { return m_PartOfSpeech; }
            set { m_PartOfSpeech = value; }
        }

        private int m_ID;
        private string[] m_Names;
        private string m_PartOfSpeech;

    }
}
