using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using System.IO;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Entity.Readers;

namespace ChaKi.Service.Readers
{
    public class PlainTextReader : CorpusSourceReader
    {
        private Corpus m_Corpus;
        private TagSet m_TagSet;
        public LexiconBuilder LexiconBuilder { get; set; }
        public string EncodingToUse { get; set; }

        public PlainTextReader(Corpus corpus)
        {
            m_Corpus = corpus;
            m_TagSet = new TagSet("Dummy");    // Empty TagSet
            this.LexiconBuilder = new LexiconBuilder();
        }

        public void SetFieldDefs(Field[] fieldDefs)
        {
        }

        public Document ReadFromFile(string path, string encoding)
        {
            Document newdoc = new Document();
            newdoc.FileName = path;
            using (TextReader streamReader = new StreamReader(path, Encoding.GetEncoding(encoding)))
            {
                ReadFromStreamSLA(streamReader, -1, newdoc);
            }
            return newdoc;
        }

        public Document ReadFromFileSLA(string path, string encoding)
        {
            return ReadFromFile(path, encoding);
        }

        public void ReadFromStreamSLA(TextReader rdr, int sentenceCount, Document doc)
        {
            int n = 0;
            int charPos = 0;
            string s;

            // TextはStringBuilderに生成しておいて、最後にDocumentに割り当てる.
            StringBuilder sb = new StringBuilder();

            while ((s = rdr.ReadLine()) != null)
            {
                s = s.Trim();  // ファイル途中のBOM（catした時に残っている場合がある）を削除する
                Sentence sen = new Sentence(doc);
                sb.Append(s);
                sen.StartChar = charPos;
                m_Corpus.AddSentence(sen);
                charPos += s.Length;
                sen.EndChar = charPos;

                Lexeme m = this.LexiconBuilder.AddEntry(s);
                if (m != null)
                {
                    Word w = sen.AddWord(m);
                    //TODO: 英語の場合：平文を再現するにはデリミタで単語を区切る必要がある

                    w.StartChar = sen.StartChar;
                    w.EndChar = sen.EndChar;
                }

                // 文全体に対して文節を追加
                Segment seg = new Segment();
                seg.StartChar = sen.StartChar;
                seg.EndChar = sen.EndChar;
                seg.Tag = new Tag(Tag.SEGMENT, "Bunsetsu"); //TODO: Tagは単一インスタンスを共用しなければならい。
                m_Corpus.AddSegment(seg);
                Segment dummy = new Segment();
                dummy.StartChar = sen.EndChar;
                dummy.EndChar = sen.EndChar;
                dummy.Tag = new Tag(Tag.SEGMENT, "Bunsetsu"); //TODO: Tagは単一インスタンスを共用しなければならい。
                m_Corpus.AddSegment(dummy);
                Link link = new Link();
                link.From = seg;
                link.To = dummy;
                link.Tag = new Tag(Tag.LINK, "D"); //TODO: Tagは単一インスタンスを共用しなければならい。
                m_Corpus.AddLink(link);

                Console.Write("> {0}\r", ++n);
            }
            doc.Text = sb.ToString();
        }


        public TagSet GetTagSet()
        {
            return m_TagSet;
        }

        //TODO 今のところダミー
        public void ReadLexiconFromStream(TextReader rdr)
        {
            ReadLexiconFromStream(rdr, false);
        }
        public void ReadLexiconFromStream(TextReader rdr, bool baseOnly)
        {
            throw new NotImplementedException("Dictionary is not applicable for a PlainText yet.");
        }
    }
}
