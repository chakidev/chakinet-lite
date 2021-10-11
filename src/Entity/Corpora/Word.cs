using System;
using System.Collections;
using System.Text;
using System.Xml.Serialization;
using ChaKi.Entity.Corpora.Annotations;
using Iesi.Collections.Generic;
using System.Collections.Generic;

namespace ChaKi.Entity.Corpora
{
    public class Word : IComparable
    {
        private static int sequentialOrderID = 0;

        public Word()
        {
            this.pos = sequentialOrderID++;
            this.mappedTo = new HashedSet<int>();
        }

        public virtual int CompareTo(object obj)
        {
            if (obj is Word)
            {
                Word w = (Word)obj;
                return id.CompareTo(w.id);
            }
            throw new ArgumentException("object is not a Word");
        }


        private int id;
        private int pos;
        private int startChar;
        private int endChar;
        private DateTime timeStamp;

        private double? startTime;
        private double? endTime;
        private double? duration;

        private Sentence    sen;
        private Lexeme      lexeme;
        private Segment     bunsetsu;    // このWordの属するBunsetsu Segment
        private Project project;
        private HeadInfo headInfo;
        private Iesi.Collections.Generic.ISet<int> mappedTo;     // set of word ids

        private string extras;

        public static void ResetOrder()
        {
            sequentialOrderID = 0;
        }

        public virtual int ID
        {
            get { return id; }
            set { id = value; }
        }
        public virtual int Pos
        {
            get { return pos; }
            set { pos = value; }
        }
        public virtual int StartChar
        {
            get { return startChar; }
            set { startChar = value; }
        }
        public virtual int EndChar
        {
            get { return endChar; }
            set { endChar = value; }
        }

        public virtual double? StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }
        public virtual double? EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }
        public virtual double? Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        public virtual string Extras
        {
            get { return extras; }
            set { extras = value; }
        }

        public virtual Project Project
        {
            get { return project; }
            set { project = value; }
        }

        public virtual HeadInfo HeadInfo
        {
            get { return headInfo;  }
            set { headInfo = value; }
        }

        [XmlIgnore]
        // word_word table mapping (but not defined in Word.hbm.xml)
        public virtual Iesi.Collections.Generic.ISet<int> MappedTo
        {
            get { return mappedTo; }
            set { mappedTo = value; }
        }

        [XmlIgnore]
        public virtual Sentence Sen
        {
            get { return sen; }
            set { sen = value; }
        }
        public virtual Lexeme Lex
        {
            get { return lexeme; }
            set { lexeme = value; }
        }

        [XmlIgnore]
        public virtual Segment Bunsetsu
        {
            get { return bunsetsu; }
            set { bunsetsu = value; }
        }

        public virtual string Text
        {
            get
            {
                if (this.lexeme != null)
                {
                    var s = this.lexeme.Surface;
                    if (this.extras != null)
                    {
                        return s + extras;
                    }
                    return s;
                }
                return string.Empty;
            }
        }

        public virtual int CharLength
        {
            get
            {
                if (this.lexeme != null)
                {
                    return this.lexeme.CharLength;
                }
                return 0;
            }
        }

        public override string ToString()
        {
            return string.Format("[Word {0},{1},{2},{3},{4},{5},{6}]",
                this.ID, this.Sen.ID, this.StartChar, this.EndChar, this.Lex.ID, this.Bunsetsu.ID, this.Pos);
        }

        /// <summary>
        /// 語属性の名称（Wordクラスのプロパティ名と一致させること）
        /// </summary>
        public static readonly Dictionary<WP, string> PropertyName =
            new Dictionary<WP, string> {
                {WP.StartTime, "StartTime"},
                {WP.EndTime, "EndTime"},
                {WP.Duration, "Duration"},
                {WP.HeadInfo, "HeadInfo" },
            };

        /// <summary>
        /// 語属性のテーブルカラム名称
        /// </summary>
        public static readonly Dictionary<WP, string> PropertyColumnName =
            new Dictionary<WP, string> {
                {WP.StartTime, "start_time"},
                {WP.EndTime, "end_time"},
                {WP.Duration, "duration"},
                {WP.HeadInfo, "head_info" },
            };

        public static WP? FindProperty(string name)
        {
            foreach (KeyValuePair<WP, string> pair in PropertyName)
            {
                if (pair.Value == name)
                {
                    return pair.Key;
                }
            }
            return null;
        }
    }

    public enum WP
    {
        // Word Properties
        StartTime = 9,
        EndTime = 10,
        Duration = 11,
        HeadInfo = 12,
        Max = 13,
    }
}
