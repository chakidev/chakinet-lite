using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;

namespace ChaKi.Service.Readers
{
    /// <summary>
    /// Cabochaファイルを読みながら作成される一時オブジェクト
    /// StartPosによって一意に識別できる
    /// </summary>
    public class CabochaBunsetsu
    {
        public Sentence Sen { get; set; }
        public Document Doc { get; set; }
        public int StartPos { get; set; }       // Corpus内での文字位置
        public int EndPos { get; set; }         // Corpus内での文字位置
        public int BunsetsuPos { get; set; }    // 文内での文節番号
        public int DependsTo { get; set; }      // 文内での係り先文節番号
        public string DependsAs { get; set; }
        public List<Word> Words { get; set; }
        public Segment Seg { get; set; }        // 割り当てられたSegmentオブジェクトへの参照
        public double Score { get; set; }
        public int HeadInd { get; set; }        // 自立語主辞の文節内位置
        public int HeadAnc { get; set; }        // 付属語(Ancillary word)主辞の文節内位置
        public string Comment { get; set; }
        public Dictionary<string, string> Attrs { get; set; } = new Dictionary<string, string>();

        public CabochaBunsetsu(Sentence sen, Document doc, int startPos, int bunsetsuPos, string dependsAs, int dependsTo, double score)
            : this(sen, doc, startPos, startPos, bunsetsuPos, dependsAs, dependsTo, score)
        {
        }

        public CabochaBunsetsu(Sentence sen, Document doc, int startPos, int bunsetsuPos, string dependsAs, int dependsTo, double score, int headInd, int headAnc)
            : this(sen, doc, startPos, startPos, bunsetsuPos, dependsAs, dependsTo, score, headInd, headAnc)
        {
        }

        public CabochaBunsetsu(Sentence sen, Document doc, int startPos, int endPos, int bunsetsuPos, string dependsAs, int dependsTo, double score)
            : this(sen, doc, startPos, endPos, bunsetsuPos, dependsAs, dependsTo, score, -1, -1)
        {
        }

        public CabochaBunsetsu(Sentence sen, Document doc, int startPos, int endPos, int bunsetsuPos, string dependsAs, int dependsTo, double score, int headInd, int headAnc)
        {
            this.Sen = sen;
            this.Doc = doc;
            this.BunsetsuPos = bunsetsuPos;
            this.StartPos = startPos;
            this.EndPos = endPos;
            this.DependsAs = dependsAs;
            this.DependsTo = dependsTo;
            this.Score = score;
            this.HeadInd = headInd;
            this.HeadAnc = headAnc;
            Words = new List<Word>();
        }

        public void AddWord(Word w)
        {
            this.Words.Add(w);
            this.EndPos += w.CharLength;
        }

        public override string ToString()
        {
            return string.Format("CabochaBunsetsu{Pos={0}, [{1}-{2}], WordCount={3}}", 
                this.BunsetsuPos, this.StartPos, this.EndPos, this.Words.Count);
        }
    }
}
