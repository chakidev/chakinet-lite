using System;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using ChaKi.Entity.Corpora.Annotations;
using Iesi.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Collections.Generic;
using Iesi.Collections;

namespace ChaKi.Entity.Corpora
{
    [XmlInclude(typeof(Word))]
    public class Sentence
    {
        public Sentence()
        {
            this.Words = new HashedSet();
            // ����Word�C���X�^���X��Order��0�ɖ߂�
            // �iWord.Order�͕��P�ʂŕt�^�������̂ł���j
            Word.ResetOrder();
            this.Bunsetsus = new ArrayList();
            this.Attributes = new HashedSet<SentenceAttribute>();
        }

        public Sentence(Document parentDoc)
            : this()
        {
            this.ParentDoc = parentDoc;
        }

        public virtual int ID { get; set; }

        /// <summary>
        /// Set of Words
        /// </summary>
        public virtual ISet Words { get; set; }

        public virtual IList<Word> GetWords(int projid)
        {
            return (from w in this.Words.Cast<Word>() where w.Project.ID == projid orderby w.Pos select w).ToList<Word>();
        }

        /// <summary>
        /// Set of Bunsetsu
        /// </summary>
        public virtual IList Bunsetsus { get; set; }

        [XmlIgnore]
        public virtual Document ParentDoc { get; set; }

        /// <summary>
        /// �h�L�������g�����Έʒu�ɂ�邱�̕��̊J�n�����ʒu
        /// </summary>
        public virtual int StartChar { get; set; }

        /// <summary>
        /// �h�L�������g�����Έʒu�ɂ�邱�̕��̏I�������ʒu
        /// </summary>
        public virtual int EndChar { get; set; }

        /// <summary>
        /// �h�L�������g�ɂ����邱�̕��̕��ԍ�
        /// </summary>
        public virtual int Pos { get; set; }

        [XmlIgnore]
        public virtual Iesi.Collections.Generic.ISet<SentenceAttribute> Attributes { get; set; }

        public virtual Word AddWord(Lexeme m)
        {
            Word seq = new Word();
            seq.Lex = m;
            seq.Sen = this;
            this.Words.Add(seq);
            return seq;
        }

        /// <summary>
        /// Range [s,e]�ɑ��݂���Word�̃��X�g��Ԃ��B
        /// �[��Word���E��������s, e�Ɉ�v���Ă��Ȃ����empty�Ƃ���B
        /// </summary>
        /// <returns></returns>
        public virtual IList<Word> GetWordsInRange(int s, int e, int project_id = 0)
        {
            var result = new List<Word>();
            bool in_range = false;
            var words = GetWords(project_id);
            foreach (var w in words)
            {
                if (!in_range && w.StartChar == s)
                {
                    in_range = true;
                }
                if (in_range)
                {
                    if (w.EndChar < e)
                    {
                        result.Add(w);
                    }
                    else if (w.EndChar == e)
                    {
                        result.Add(w);
                        return result;
                    }
                    else
                    {
                        result.Clear();
                        return result;
                    }
                }
            }
            result.Clear();
            return result;
        }

        /// <summary>
        /// Range [s,e]�ɑ��݂���Word�̃��X�g��Ԃ��B
        /// �͈͂Ɉꕔ�̂݊܂܂��Word���܂߂�_��GetWordsInRange�ƈقȂ�B
        /// </summary>
        /// <returns></returns>
        public virtual IList<Word> GetWordsInRange2(int s, int e, int project_id = 0)
        {
            var result = new List<Word>();
            bool in_range = false;
            var words = GetWords(project_id);
            foreach (var w in words)
            {
                if (!in_range && w.EndChar > s)
                {
                    in_range = true;
                }
                if (in_range)
                {
                    if (w.StartChar < e)
                    {
                        result.Add(w);
                    }
                    else
                    {
                        return result;
                    }
                }
            }
            return result; // Range�������E�𒴂��Ă���ꍇ�͂����ɗ���.
        }

        public virtual string GetText(bool insertSpaces, int project_id = 0)
        {
            var sb = new StringBuilder();
            var words = this.GetWords(project_id);
            for (int i = 0; i < words.Count; i++)
            {
                var w = words[i];
                sb.Append(w.Text);
                if (i != this.Words.Count - 1 && insertSpaces)
                {
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }

        public virtual string GetTextInRange(int s, int e, bool insertSpaces, int project_id = 0)
        {
            var result = string.Empty;
            bool in_range = false;
            var words = GetWords(project_id);
            foreach (var w in words)
            {
                if (!in_range && w.EndChar > s)
                {
                    in_range = true;
                }
                var spos = Math.Max(0, s - w.StartChar);
                if (in_range)
                {
                    var txt = w.Text;
                    if (w.StartChar < e)
                    {
                        var len = Math.Min(txt.Length - spos, e - w.StartChar);
                        if (result.Length > 0 && insertSpaces)
                        {
                            result += " ";
                        }
                        result += txt.Substring(spos, len);
                    }
                    else
                    {
                        return result;
                    }
                }
            }
            return result; // Range�������E�𒴂��Ă���ꍇ�͂����ɗ���.
        }

        public virtual string GetTrimmedText(int length, bool insertSpaces, int project_id = 0)
        {
            var sb = new StringBuilder();
            var words = this.GetWords(project_id);
            for (int i = 0; i < words.Count; i++)
            {
                var w = words[i];
                sb.Append(w.Text);
                if (i != this.Words.Count - 1 && insertSpaces)
                {
                    sb.Append(" ");
                }
                if (sb.Length > length)
                {
                    // length�őł��؂�.
                    return sb.ToString().Substring(0, length);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Cabocha�t�H�[�}�b�g�̕��߃^�O������Bunsetsu�I�u�W�F�N�g���쐬���A
        /// ����Sentence�ɓo�^����B
        /// ���ׂĂ̕��߂��������AEOS���������_��CheckBunsetsus���Ăяo���āA
        /// ���������K�v������B
        /// </summary>
        /// <param name="s"></param>
        public virtual Bunsetsu AddBunsetsu(string s)
        {
            char[] bunsetsuSplitPattern = new char[] { ' ' };
            char[] numberPattern = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

            // "* 0 -D 0/0 0.00000000"�̌`���̍s���p�[�X����
            string[] bunsetsuparams = s.Split(bunsetsuSplitPattern);
            if (bunsetsuparams.Length < 3)
            {
                return null;
            }
            int bunsetsuId = -1;
            int depBunsetsuId = -1;
            string depType = null;
            try
            {
                bunsetsuId = Int32.Parse(bunsetsuparams[1]);
                int pos = bunsetsuparams[2].LastIndexOfAny(numberPattern);
                if (pos < 0 || pos + 1 > bunsetsuparams[2].Length - 1)
                {
                    return null;
                }
                depBunsetsuId = Int32.Parse(bunsetsuparams[2].Substring(0, pos + 1));
                depType = bunsetsuparams[2].Substring(pos + 1, bunsetsuparams[2].Length - pos - 1);

                // �p�����[�^����������΁A���߃I�u�W�F�N�g���쐬���ASetnence�ɒǉ��o�^
                if (bunsetsuId >= 0 && depType != null)
                {
                    int newsize = Math.Max(bunsetsuId, depBunsetsuId) + 1;
                    if (this.Bunsetsus.Count < newsize)
                    {
                        // Bunsetsus�z����g��
                        for (int i = this.Bunsetsus.Count; i < newsize; i++)
                        {
                            this.Bunsetsus.Add(new Bunsetsu());
                        }
                    }
                    Bunsetsu ph = this.Bunsetsus[bunsetsuId] as Bunsetsu;
                    ph.Pos = bunsetsuId;
                    ph.Sen = this;
                    if (depBunsetsuId >= 0)
                    {
                        ph.DependsTo = this.Bunsetsus[depBunsetsuId] as Bunsetsu;
                    }
                    ph.DependsAs = depType;
                    return ph;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        /// <summary>
        /// ����Sentence�̓��e��Cabocha Format��Stream�ɏo�͂���
        /// </summary>
        /// <param name="wr"></param>
        public virtual void WriteCabocha(TextWriter wr)
        {
            Segment currentSeg = null;
            foreach (Word word in this.Words)
            {
                Segment seg = word.Bunsetsu;
                if (seg != currentSeg)
                {
                    // write segment tag


                    currentSeg = seg;
                }
                // write word
                Lexeme lex = word.Lex;
                if (lex != null)
                {

                }
            }
            wr.WriteLine("EOS");
        }

        public override string ToString()
        {
            return string.Format("[Sen {0},{1},{2},{3},{4}]",
                this.ID, this.StartChar, this.EndChar, this.ParentDoc.ID, this.Pos);
        }
    }
}
