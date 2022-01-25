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
        /// KwicItem���쐬����
        /// ID��KwicList�ɒǉ�(AddKwicItem)�����Ƃ��Ɏ����t�������B
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

            ID = 0; // KwicList�ɒǉ�(AddKwicItem)�����Ƃ��Ɏ����t�������B
            Crps = c;
            Document = doc;
            SenID = senID;
            SenPos = senPos;
            StartCharPos = startCharPos;
            EndCharPos = endCharPos;
            IsSimple = false;
        }

        /// <summary>
        /// ������
        /// </summary>
        public KwicPortion Left { get; set; }
        /// <summary>
        /// ���S��i������̎g�p�͖��Ή��j
        /// </summary>
        public KwicPortion Center { get; set; }
        /// <summary>
        /// �E����
        /// </summary>
        public KwicPortion Right { get; set; }

        /// <summary>
        /// �������ɕt�^����鍀�ڂ̒ʂ��ԍ�
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// ����KwicItem�̑�����Corpus
        /// </summary>
        public Corpora.Corpus Crps { get; set; }
        /// <summary>
        /// ����KwicItem�̑�����Document
        /// </summary>
        public Document Document { get; set; }
        /// <summary>
        /// Document���ł���KwicItem���n�܂镶���ʒu
        /// </summary>
        public int StartCharPos { get; set; }
        /// <summary>
        /// Document���ł���KwicItem���I��镶���ʒu
        /// </summary>
        public int EndCharPos { get; set; }
        /// <summary>
        /// �R�[�p�X���ł̂���KwicItem�̈�ӂ�ID
        /// </summary>
        public int SenID { get; set; }
        /// <summary>
        /// Document���ł̂���KwicItem�̕��ԍ�
        /// </summary>
        public int SenPos { get; set; }


        /// <summary>
        /// ���S��̊J�n�ʒu�����߂�
        /// </summary>
        public int CenterWordID
        {
            get
            {
                return this.Left.Count;
            }
        }

        /// <summary>
        /// ���S��̊J�n�����ʒu�i��������j�����߂�
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
        /// �I����Ԃɂ��邩�ۂ��������t���O
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// SimpleSearch�̌��ʂł�����Tag���(Lexeme)���܂܂Ȃ��f�[�^�ł��邩
        /// </summary>
        public bool IsSimple { get; set; }

        public int Offset
        {
            get { return m_Offset; }
        }

        /// <summary>
        /// Center����V�t�g����.
        /// �����A�����𒴂��ăV�t�g������Ƃ��̕��������KwicWord�����������.
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
                //  �E�V�t�g
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
                //  ���V�t�g
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
        /// �w��ʒu��KwicWord��Hilight�������Z�b�g����.
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
            // ������XmlSerializer�ɗ���Ȃ�: cf. Service.Readers.ChaKiReader.Read()
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
        /// �\��Shift���ɁA���݂�center�����̈ʒu����
        /// ���ꕪ�O��ɂ���Ă��邩�������l
        /// </summary>
        private int m_Offset;
        #endregion
    }
}
