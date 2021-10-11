using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;

namespace CreateCorpusSLA
{
    internal class BibParser
    {
        public static Document Parse(string line, out int nSentence)
        {
            string[] fields = line.Split(new char[] { '\t' });
            if (fields.Length != 2)
            {
                throw new Exception(string.Format("Invalid Line in Bib file(Failed to Parse): {0}...", line.Substring(0, Math.Max(line.Length, 20))));
            }

            string[] subfields = fields[0].Split(new char[] { '-' });
            if (subfields.Length != 2)
            {
                throw new Exception(string.Format("Invalid Line in Bib file(Failed to parse Start-End): {0}...", line.Substring(0, Math.Max(line.Length, 20))));
            }
            int to, from;
            if (!Int32.TryParse(subfields[0], out from))
            {
                throw new Exception(string.Format("Invalid Line in Bib file(Failed to read StartSentenceID): {0}...", line.Substring(0, Math.Max(line.Length, 20))));
            }
            if (!Int32.TryParse(subfields[1], out to))
            {
                throw new Exception(string.Format("Invalid Line in Bib file(Failed to read EndSentenceID): {0}...", line.Substring(0, Math.Max(line.Length, 20))));
            }
            nSentence = to - from + 1;

            Document doc = new Document();

            string xmlstr = fields[1];

            string s;
            int sp = xmlstr.IndexOf('<');
            if (sp >= 0)
            {
                s = string.Format("<Root><FilePath>{0}</FilePath>{1}</Root>", xmlstr.Substring(0, sp), xmlstr.Substring(sp));
            }
            else
            {
                s = string.Format("<Root><FilePath>{0}</FilePath></Root>", xmlstr);
            }
            using (TextReader trdr = new StringReader(s))
            {
                XmlReader xrdr = XmlReader.Create(trdr);
                while (xrdr.Read())
                {
                    if (xrdr.Name.Equals("Root")) continue;
                    DocumentAttribute dt = new DocumentAttribute();
                    dt.ID = DocumentAttribute.UniqueID++;
                    dt.Key = xrdr.Name;
                    dt.Value = xrdr.ReadString();
                    if (dt.Key.Equals("FilePath"))
                    {
                        dt.Value = dt.Value.Replace(":", @"/");
                        dt.Value = dt.Value.Replace("//", @":/");
                        if (doc.FileName == null)
                        {
                            doc.FileName = dt.Value;        // BibIDがなければFilePathをDcument Filterに使用
                        }
                    }
                    else if (dt.Key.Equals("Bib_ID"))       // Document Filterではこのタグを優先して使用する
                    {
                        doc.FileName = dt.Value;
                    }
                    doc.Attributes.Add(dt);
                }
                xrdr.Close();
            }
            return doc;
        }
    }
}
