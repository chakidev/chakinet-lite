using System;
using System.IO;
using System.Text;
using System.Xml;
using ChaKi.Entity.Corpora.Annotations;
using Iesi.Collections.Generic;

namespace ChaKi.Entity.Corpora
{
    public class Document
    {
        public virtual int ID { get; set; }

        public virtual int Order { get; set; }

        public virtual string FileName { get; set; }

        /// <summary>
        /// パフォーマンス制御上、長大となりうるDocument.TextをHibernateで
        /// 自動マッピングすることは避けなければならないため、
        /// 明示的にQueryを行ってsetしない限りこのプロパティはnullであるので注意。
        /// </summary>
        public virtual string Text { get; set; }

        /// <summary>
        /// 2021.1.11仕様追加
        /// 従来より、DocumentAttributeの一部はSentenceAttributeとして使われている.
        /// （Sentence自体はDocumentAttributeへのIDしか持てない.）
        /// SentenceAttribute専用のAttributeを区別するため、Keyの先頭に"@"を付与するものとする.
        /// Export時に、
        ///   @付きのAttributeは、SENTENCETAGIDとしてのみ出力される.
        ///   @なしのAttributeは、DOCIDおよびDocumentの#ATTRタグに出力される.
        /// </summary>
        public virtual ISet<DocumentAttribute> Attributes { get; set; }

        public Document()
        {
            this.ID = 0;
            this.FileName = string.Empty;
            this.Attributes = new HashedSet<DocumentAttribute>();
            this.Text = string.Empty;
        }

        public virtual void AppendText(string s)
        {
            this.Text += s;
        }

        public virtual string GetAttribute(string key)
        {
            if (this.Attributes != null)
            {
                foreach (var a in this.Attributes)
                {
                    if (a.Key == key)
                    {
                        return a.Value;
                    }
                }
            }
            return null;
        }

        public virtual string GetAttributeString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (DocumentAttribute attr in this.Attributes)
            {
                if (sb.Length > 0)
                {
                    sb.Append(";");
                }
                sb.AppendFormat("{0}={1}", attr.Key, attr.Value);
            }
            return sb.ToString();
        }

        public virtual string GetAttributeStringAsXmlFragment()
        {
            try
            {
                using (TextWriter twr = new StringWriter())
                {
                    using (XmlWriter wr = new XmlTextWriter(twr))
                    {
                        foreach (DocumentAttribute attr in this.Attributes)
                        {
                            if (!attr.Key.StartsWith("@"))
                            {
                                wr.WriteElementString(attr.Key, attr.Value);
                            }
                        }
                        return twr.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
    }
}
