using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Corpora
{
    public class CForm : Property
    {
        public CForm()
        {
        }

        public CForm(string name)
        {
            m_CForm = name;
        }

        public CForm(CForm src)
            : base(src)
        {
            m_ID = src.m_ID;
            if (src.m_CForm != null)
            {
                m_CForm = string.Copy(src.m_CForm);
            }
        }

        public override object Clone()
        {
            return new CForm(this);
        }

        public override string StrVal
        {
            get { return m_CForm; }
            set { m_CForm = value; }
        }

        public override string ToString()
        {
            return this.Name;
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
        public virtual string Name
        {
            get { return m_CForm; }
            set { m_CForm = value; }
        }

        public static CForm Default { get; set; }

        private int m_ID;
        private string m_CForm;

        public virtual bool IsBase()
        {
            return (m_CForm == "基本形" || m_CForm.StartsWith("終止形"));
        }
    }
}
