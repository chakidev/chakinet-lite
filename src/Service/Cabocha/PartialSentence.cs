using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora;
using System.IO;

namespace ChaKi.Service.Cabocha
{
    class PartialSentence
    {
        public PartialSentence()
        {
            this.Words = null;
            this.WordStartPos = -1;
            this.IsMainSentence = false;
            this.CabochaResult = string.Empty;
        }

        public PartialSentence(IList<Word> words)
            : this()
        {
            this.Words = words;
            if (words.Count > 0)
            {
                this.WordStartPos = words[0].Pos;
            }
        }

        public PartialSentence(IList<Word> words, bool isMainSentence)
            : this()
        {
            this.Words = words;
            this.IsMainSentence = isMainSentence;
        }

        public IList<Word> Words { get; set; }

        public int WordStartPos { get; set; }

        public int BunsetsuCount { get; private set; }

        public bool IsMainSentence { get; set; }

        private string m_CabochaResult;
        public string CabochaResult
        {
            get
            {
                return m_CabochaResult;
            }
            set
            {
                m_CabochaResult = value;
                // Count bunsetsus
                this.BunsetsuCount = 0;
                using (var rdr = new StringReader(m_CabochaResult))
                {
                    string line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        if (line.StartsWith("*"))
                        {
                            this.BunsetsuCount++;
                        }
                    }
                }
            }
        }
    }
}
