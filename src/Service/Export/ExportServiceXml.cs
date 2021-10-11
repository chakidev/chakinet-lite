using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using System.IO;
using ChaKi.Entity.Kwic;
using NHibernate;
using System.Xml.Serialization;
using System.Xml;

namespace ChaKi.Service.Export
{
    public class ExportServiceXml : ExportServiceBase
    {
        public ExportServiceXml(XmlWriter wr)
        {
            m_XmlWriter = wr;
        }

        public override void ExportItem(KwicItem ki)
        {
            if (m_XmlWriter == null) throw new InvalidOperationException("XmlWriter is null.");

            IQuery q = m_Session.CreateQuery(string.Format("from Sentence where ID={0}", ki.SenID));
            Sentence sen = q.UniqueResult<Sentence>();
            if (sen == null)
            {
                throw new Exception(string.Format("Sentence not found. Corpus={0}, senID={1}", ki.Crps.Name, ki.SenID));
            }
            var sen2 = new SentenceWrapped() { Sen = sen };
            XmlSerializer ser = new XmlSerializer(typeof(SentenceWrapped));
            ser.Serialize(m_XmlWriter, sen2);
        }


        // Sentence型をSerializeするための内部型
        // KWIC Export専用なので文節情報は出力しない.
        [XmlInclude(typeof(Word))]
        [XmlRoot("Sentence")]
        public class SentenceWrapped
        {
            public SentenceWrapped() { }

            private Sentence m_Sen;
            [XmlIgnore]
            public Sentence Sen
            {
                set {
                    m_Sen = value;
                    this.Words = m_Sen.GetWords(0).ToList();
                }
            }

            public int ID => m_Sen?.ID??-1;

            public List<Word> Words { get; private set; }
            public int StartChar => m_Sen?.StartChar ?? -1;
            public int EndChar => m_Sen?.EndChar ?? -1;
            public int Pos => m_Sen?.Pos ?? -1;
        }
    }
}
