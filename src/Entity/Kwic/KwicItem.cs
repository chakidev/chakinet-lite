using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using ChaKi.Entity.Search;

namespace ChaKi.Entity.Kwic
{
    public class KwicItem : IXmlSerializable
    {
        public KwicItem()
        {
            Left = new KwicPortion(this);
            Center = new KwicPortion(this);
            Right = new KwicPortion(this);

            ID = 0;
            Crps = null;
            SenID = 0;
            SenPos = 0;
            StartCharPos = 0;
            IsSimple = false;
        }

        /// <summary>
        /// KwicItemを作成する
        /// IDはKwicListに追加(AddKwicItem)されるときに自動付加される。
        /// </summary>
        /// <param name="c"></param>
        /// <param name="doc"></param>
        /// <param name="senPos"></param>
        /// <param name="charPos"></param>
        public KwicItem(Corpus c, Document doc, int senID, int startCharPos, int endCharPos, int senPos)
        {
            Left = new KwicPortion(this);
            Center = new KwicPortion(this);
            Right = new KwicPortion(this);

            ID = 0; // KwicListに追加(AddKwicItem)されるときに自動付加される。
            Crps = c;
            Document = doc;
            SenID = senID;
            SenPos = senPos;
            StartCharPos = startCharPos;
            EndCharPos = endCharPos;
            IsSimple = false;
        }

        /// <summary>
        /// 左文脈
        /// </summary>
        public KwicPortion Left { get; set; }
        /// <summary>
        /// 中心語（複数語の使用は未対応）
        /// </summary>
        public KwicPortion Center { get; set; }
        /// <summary>
        /// 右文脈
        /// </summary>
        public KwicPortion Right { get; set; }

        /// <summary>
        /// 検索時に付与される項目の通し番号
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// このKwicItemの属するCorpus
        /// </summary>
        public Corpora.Corpus Crps { get; set; }
        /// <summary>
        /// このKwicItemの属するDocument
        /// </summary>
        public Document Document { get; set; }
        /// <summary>
        /// Document内でこのKwicItemが始まる文字位置
        /// </summary>
        public int StartCharPos { get; set; }
        /// <summary>
        /// Document内でこのKwicItemが終わる文字位置
        /// </summary>
        public int EndCharPos { get; set; }
        /// <summary>
        /// コーパス内でのこのKwicItemの一意のID
        /// </summary>
        public int SenID { get; set; }
        /// <summary>
        /// Document内でのこのKwicItemの文番号
        /// </summary>
        public int SenPos { get; set; }


        /// <summary>
        /// 中心語の開始位置を求める
        /// </summary>
        public int CenterWordID
        {
            get
            {
                return this.Left.Count;
            }
        }

        /// <summary>
        /// 中心語の開始文字位置（文頭から）を求める
        /// </summary>
        public int GetCenterCharOffset()
        {
            return GetCenterCharOffset(false);
        }

        public int GetCenterCharOffset(bool useSpacing)
        {
            int i = 0;
            foreach (KwicWord w in this.Left.Words)
            {
                i += w.Length;
                if (useSpacing)
                {
                    i++;
                }
            }
            return i;

        }

        public int GetCenterCharLength()
        {
            int i = 0;
            foreach (KwicWord w in this.Center.Words)
            {
                i += w.Length;
            }
            return i;
        }

        public int WordCount
        {
            get
            {
                return this.Left.Count + this.Center.Count + this.Right.Count;
               
            }
        }

        /// <summary>
        /// 選択状態にあるか否かを示すフラグ
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// SimpleSearchの結果であってTag情報(Lexeme)を含まないデータであるか
        /// </summary>
        public bool IsSimple { get; set; }

        public int Offset
        {
            get { return m_Offset; }
        }

        /// <summary>
        /// Center語をシフトする.
        /// 文頭、文末を超えてシフトさせるとその分だけ空のKwicWordが生成される.
        /// </summary>
        /// <param name="shift"></param>
        public void Shift(int shift)
        {
            if (shift == 0)
            {
                return;
            }
            KwicWord w = null;
            if (shift > 0)
            {
                //  右シフト
                for (int i = 0; i < shift; i++)
                {
                    if (this.Left.Count > 0)
                    {
                        w = this.Left.PopBack();
                        m_Offset--;
                    }
                    else
                    {
                        w = new KwicWord();
                    }
                    this.Center.PushFront(w);
                    w = Center.PopBack();
                    this.Right.PushFront(w);
                }
            }
            else
            {
                //  左シフト
                for (int i = 0; i < -shift; i++)
                {
                    if (this.Right.Count > 0)
                    {
                        w = this.Right.PopFront();
                    }
                    else
                    {
                        w = new KwicWord();
                    }
                    this.Center.PushBack(w);
                    w = this.Center.PopFront();
                    m_Offset++;
                    this.Left.PushBack(w);
                }
            }
        }

        /// <summary>
        /// 指定位置のKwicWordのHilight属性をセットする.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="f"></param>
        public void SetHilight(int pos, bool f)
        {
            if (pos == 0)
            {
                foreach (KwicWord w in this.Center.Words)
                {
                    int attr = w.ExtAttr;
                    if (f)
                    {
                        attr |= KwicWord.KWA_HILIGHT;
                    }
                    else
                    {
                        attr &= (~KwicWord.KWA_HILIGHT);
                    }
                    w.ExtAttr = attr;
                }
            }
            if (pos < 0 && this.Left.Count + pos >= 0)
            {
                KwicWord w = this.Left.Words[this.Left.Count + pos];
                int attr = w.ExtAttr;
                if (f)
                {
                    attr |= KwicWord.KWA_HILIGHT;
                }
                else
                {
                    attr &= (~KwicWord.KWA_HILIGHT);
                }
                w.ExtAttr = attr;
            }
            else if (pos > 0 && pos - 1 < this.Right.Count)
            {
                KwicWord w = this.Right.Words[pos - 1];
                int attr = w.ExtAttr;
                if (f)
                {
                    attr |= KwicWord.KWA_HILIGHT;
                }
                else
                {
                    attr &= (~KwicWord.KWA_HILIGHT);
                }
                w.ExtAttr = attr;
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            string s = string.Format("{0},{1},{2},0,{3},{4},{5},{6},{7},{8},{9}",   // dummy field between {2} and {3}; for compatibility reason
                ID, Crps.Name, Document.ID, StartCharPos, SenID, SenPos, Checked, Left.ToString(), Center.ToString(), Right.ToString());
            writer.WriteCData(s);
        }

        public void ReadXml(XmlReader reader)
        {
            // 復元はXmlSerializerに頼らない: cf. Service.Readers.ChaKiReader.Read()
            reader.ReadStartElement("KwicItem");
            reader.Read();
            reader.ReadEndElement();
        }

        public XmlSchema GetSchema()
        {
            return (null);
        }


        #region Private Fields
        /// <summary>
        /// 逆順にした左文脈（ソートで使用）
        /// </summary>
        private KwicPortion Left_Rev;

        /// <summary>
        /// 表示Shift時に、現在のcenterが元の位置から
        /// 何語分前後にずれているかを示す値
        /// </summary>
        private int m_Offset;
        #endregion
    }
}
