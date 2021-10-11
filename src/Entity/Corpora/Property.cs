using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora
{
    /// <summary>
    /// Lexemeの各属性
    /// POS,CType,CFormの基底クラスであるが、それ以外の属性の場合のために、デフォルトでstring値を持つ。
    /// </summary>
    [XmlInclude(typeof(PartOfSpeech)), XmlInclude(typeof(CForm)), XmlInclude(typeof(CType))]
    public class Property : ICloneable
    {
        public Property()
        {
            this.val = "";
        }

        public Property(string s)
        {
            this.val = string.Copy(s);
        }

        public Property(Property src)
        {
            if (src.val != null)
            {
                val = string.Copy(src.val);
            }
            IsRegEx = src.IsRegEx;
            IsCaseSensitive = src.IsCaseSensitive;
        }

        public virtual object Clone()
        {
            return new Property(this);
        }

        public virtual string StrVal
        {
            get { return this.val; }
            set { this.val = value; }
        }

        public bool IsRegEx { get; set; }

        public bool IsCaseSensitive { get; set; }

        /// <summary>
        /// 検索の対象となるオブジェクトを返す。
        /// POS, CType, CFormでは自分自身を返すが、この基底実装ではstringを返す。
        /// </summary>
        public virtual object Target
        {
            get { return this.val; }
        }

        protected string val;
    }
}
