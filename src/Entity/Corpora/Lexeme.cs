using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Entity.Corpora
{
    public enum LP
    {
        // ToString()の順序と揃えること.
        Surface = 0,
        Reading = 1,
        LemmaForm = 2,
        Pronunciation = 3,
        BaseLexeme = 4,
        Lemma = 5,
        PartOfSpeech = 6,
        CType = 7,
        CForm = 8,
        Max = 9,
    }

    public enum LWP
    {
        // ToString()の順序と揃えること.
        Surface = 0,
        Reading = 1,
        LemmaForm = 2,
        Pronunciation = 3,
        BaseLexeme = 4,
        Lemma = 5,
        PartOfSpeech = 6,
        CType = 7,
        CForm = 8,
        // Word Properties
        StartTime = 9,
        EndTime = 10,
        Duration = 11,
        HeadInfo = 12,
        Max = 13,
    }

    public class Lexeme : IComparable, ICloneable
    {
        private Dictionary<LP, object> properties;
        private string customProperty;  // Tab区切りで並べた追加属性（ [KEY\tVALUE\t]* の形）
        private int frequency;
        private Dictionary<string, string> customPropertyMapping;

        /// <summary>
        /// MIスコアの算出で使用する一時的なマーキングフラグ
        /// 一時オブジェクトとしてのLexemeでのみ使用され、コーパスと結びついているLexemeでは常にfalse.
        /// </summary>
        public bool Marked { get; set; }

        /// <summary>
        /// TagEditで生成されたLexemeで、Lexiconへの追加をpendigされている新語の場合true
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// TagEditにおいて参照辞書からCopyしたLexemeの場合、元辞書名
        /// </summary>
        public string Dictionary { get; set; }

        public virtual int ID { get; set; }

        // Cradleを参照辞書として検索を行った場合のみ設定される、Cradle辞書の持つID
        [XmlIgnore]
        public string SID { get; set; }

        /// <summary>
        /// 語属性の名称（Lexemeクラスのプロパティ名と一致させること）
        /// </summary>
        public static readonly Dictionary<LP,string> PropertyName =
            new Dictionary<LP, string> {
                {LP.Surface, "Surface" },
                {LP.Reading, "Reading" },
                {LP.LemmaForm, "LemmaForm" },
                {LP.Pronunciation, "Pronunciation" },
                {LP.BaseLexeme, "BaseLexeme" },
                {LP.Lemma, "Lemma" },
                {LP.PartOfSpeech, "PartOfSpeech" },
                {LP.CType, "CType" },
                {LP.CForm, "CForm" },
            };

        /// <summary>
        /// Lexeme属性の名称（Lexemeクラスのプロパティ名と一致させること）
        /// + Wordクラスからのプロパティ
        /// </summary>
        public static readonly Dictionary<LWP, string> LWPropertyName =
            new Dictionary<LWP, string> {
                {LWP.Surface, "Surface" },
                {LWP.Reading, "Reading" },
                {LWP.LemmaForm, "LemmaForm" },
                {LWP.Pronunciation, "Pronunciation" },
                {LWP.BaseLexeme, "BaseLexeme" },
                {LWP.Lemma, "Lemma" },
                {LWP.PartOfSpeech, "PartOfSpeech" },
                {LWP.CType, "CType" },
                {LWP.CForm, "CForm" },
                {LWP.StartTime, "StartTime" },
                {LWP.EndTime, "EndTime" },
                {LWP.Duration, "Duration" },
                {LWP.HeadInfo, "HeadInfo" },
            };

        /// <summary>
        /// 語属性のテーブルカラム名称
        /// </summary>
        public static readonly Dictionary<LP, string> PropertyColumnName =
            new Dictionary<LP, string> {
                {LP.Surface, "surface" },
                {LP.Reading, "reading" },
                {LP.LemmaForm, "lemmaform" },
                {LP.Pronunciation, "pronunciation" },
                {LP.BaseLexeme, "base_lexeme_ref" },
                {LP.Lemma, "lemma" },
                {LP.PartOfSpeech, "part_of_speech_id" },
                {LP.CType, "ctype_id" },
                {LP.CForm, "cform_id" }
            };

        public static LP? FindProperty(string name)
        {
            foreach (KeyValuePair<LP, string> pair in PropertyName)
            {
                if (pair.Value == name)
                {
                    return pair.Key;
                }
            }
            return null;
        }

        public static int CompareKey = (int)LP.Surface;
        public static bool CompareAscending = true;

        public static Lexeme Empty = new Lexeme();

        public Lexeme() 
        {
            this.ID = -1;
            this.properties = new Dictionary<LP, object>();
            this.properties.Add(LP.Surface, string.Empty);
            this.properties.Add(LP.Reading, string.Empty);
            this.properties.Add(LP.LemmaForm, string.Empty);
            this.properties.Add(LP.Pronunciation, string.Empty);
            this.properties.Add(LP.BaseLexeme, null);
            this.properties.Add(LP.Lemma, string.Empty);
            this.properties.Add(LP.PartOfSpeech, null);
            this.properties.Add(LP.CType, null);
            this.properties.Add(LP.CForm, null);

            this.CustomProperty = string.Empty;
            this.customPropertyMapping = new Dictionary<string, string>();
            this.Marked = false;
            this.CanEdit = false;
        }

        public Lexeme(Lexeme org)
            : this()
        {
            if (org == null)
            {
                return;
            }
            this.properties = new Dictionary<LP, object>();
            foreach (KeyValuePair<LP, object> pair in org.properties)
            {
                this.properties.Add(pair.Key, pair.Value);
            }
            this.frequency = org.frequency;
            this.ID = org.ID;
            this.Marked = org.Marked;
            this.CanEdit = org.CanEdit;
            if (org.CustomProperty != null)
            {
                this.CustomProperty = string.Copy(org.CustomProperty);
                this.customPropertyMapping.Clear();
                foreach (KeyValuePair<string, string> pair in org.customPropertyMapping)
                {
                    this.customPropertyMapping.Add(pair.Key, pair.Value);
                }
            }
            else
            {
                this.CustomProperty = string.Empty;
                this.customPropertyMapping = new Dictionary<string, string>();
            }
        }

        public static Lexeme CreateDefaultUnknownLexeme(string surface)
        {
            Lexeme lex = new Lexeme();
            lex.Surface = surface;
            lex.BaseLexeme = lex;
            lex.PartOfSpeech = PartOfSpeech.Default;
            lex.CType = CType.Default;
            lex.CForm = CForm.Default;
            lex.CanEdit = true;

            return lex;
        }

        public object Clone()
        {
            return new Lexeme(this);
        }

        public int CharLength
        {
            get { return this.Surface.Length; }
        }

        public virtual string Surface
        {
            get { return properties[LP.Surface] as string; }
            set { properties[LP.Surface] = value; }
        }
        public virtual string Reading
        {
            get { return properties[LP.Reading] as string; }
            set { properties[LP.Reading] = value; }
        }
        public virtual string LemmaForm
        {
            get { return properties[LP.LemmaForm] as string; }
            set { properties[LP.LemmaForm] = value; }
        }
        public virtual string Pronunciation
        {
            get { return properties[LP.Pronunciation] as string; }
            set { properties[LP.Pronunciation] = value; }
        }
        [XmlIgnore]
        public virtual Lexeme BaseLexeme
        {
            get { return properties[LP.BaseLexeme] as Lexeme; }
            set { properties[LP.BaseLexeme] = value; }
        }
        public virtual string BaseLexemeSurface // for Serialization only
        {
            get { return this.BaseLexeme.Surface; }
            set { }
        }
        public virtual string Lemma
        {
            get { return properties[LP.Lemma] as string; }
            set { properties[LP.Lemma] = value; }
        }
        public virtual PartOfSpeech PartOfSpeech
        {
            get { return properties[LP.PartOfSpeech] as PartOfSpeech; }
            set { properties[LP.PartOfSpeech] = value; }
        }
        public virtual CType CType
        {
            get { return properties[LP.CType] as CType; }
            set { properties[LP.CType] = value; }
        }
        public virtual CForm CForm
        {
            get { return properties[LP.CForm] as CForm; }
            set { properties[LP.CForm] = value; }
        }
        public virtual int Frequency
        {
            get { return this.frequency; }
            set { this.frequency = value; }
        }
        public virtual string CustomProperty
        {
            get { return this.customProperty; }
            set
            {
                this.customProperty = value;
                if (this.customPropertyMapping == null) this.customPropertyMapping = new Dictionary<string,string>();
                this.customPropertyMapping.Clear();
                string[] pairs = value.Split('\n');
                foreach (string pair in pairs)
                {
                    string[] keyvalue = pair.Split('\t');
                    if (keyvalue.Length == 2)
                    {
                        this.customPropertyMapping.Add(keyvalue[0], keyvalue[1]);
                    }
                }
            }
        }

        public virtual object GetProperty( LP tag )
        {
            return properties[tag];
        }

        public virtual string GetStringProperty(LP tag)
        {
            string s = string.Empty;
            switch (tag)
            {
                case LP.Surface:
                    s = this.Surface;
                    break;
                case LP.Reading:
                    s = this.Reading;
                    break;
                case LP.LemmaForm:
                    s = this.LemmaForm;
                    break;
                case LP.Pronunciation:
                    s = this.Pronunciation;
                    break;
                case LP.BaseLexeme:
                    if (this.BaseLexeme != null) s = this.BaseLexeme.Surface;
                    break;
                case LP.Lemma:
                    s = this.Lemma;
                    break;
                case LP.PartOfSpeech:
                    if (this.PartOfSpeech != null) s = this.PartOfSpeech.Name;
                    break;
                case LP.CType:
                    if (this.CType != null) s = this.CType.Name;
                    break;
                case LP.CForm:
                    if (this.CForm != null) s = this.CForm.Name;
                    break;
            }
            if (this.Marked)
            {
                return ("<<" + s + ">>");
            }
            return (s ?? string.Empty);
        }

        public virtual string GetStringPropertyShort(LP tag)
        {
            string s = string.Empty;
            switch (tag)
            {
                case LP.Surface:
                    s = this.Surface;
                    break;
                case LP.Reading:
                    s = this.Reading;
                    break;
                case LP.LemmaForm:
                    s = this.LemmaForm;
                    break;
                case LP.Pronunciation:
                    s = this.Pronunciation;
                    break;
                case LP.BaseLexeme:
                    if (this.BaseLexeme != null) s = this.BaseLexeme.Surface;
                    break;
                case LP.Lemma:
                    s = this.Lemma;
                    break;
                case LP.PartOfSpeech:
                    if (this.PartOfSpeech != null) s = this.PartOfSpeech.Name1;   //短縮形
                    break;
                case LP.CType:
                    if (this.CType != null) s = this.CType.Name;
                    break;
                case LP.CForm:
                    if (this.CForm != null) s = this.CForm.Name;
                    break;
            }
            if (this.Marked)
            {
                return ("<<" + s + ">>");
            }
            return (s ?? string.Empty);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            // LPの順序と揃えること.
            sb.AppendFormat("{0},", GetStringProperty(LP.Surface));
            sb.AppendFormat("{0},", GetStringProperty(LP.Reading));
            sb.AppendFormat("{0},", GetStringProperty(LP.LemmaForm));
            sb.AppendFormat("{0},", GetStringProperty(LP.Pronunciation));
            sb.AppendFormat("{0},", GetStringProperty(LP.BaseLexeme));
            sb.AppendFormat("{0},", GetStringProperty(LP.Lemma));
            sb.AppendFormat("{0},", GetStringProperty(LP.PartOfSpeech));
            sb.AppendFormat("{0},", GetStringProperty(LP.CType));
            sb.AppendFormat("{0}", GetStringProperty(LP.CForm));
            return sb.ToString();
        }

        // Reading, Pronunciationを除く文字列表現
        // 基本形の一致比較のみに使用する
        public string ToString2()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0},,,", GetStringProperty(LP.Surface));
            sb.AppendFormat("{0},", GetStringProperty(LP.PartOfSpeech));
            sb.AppendFormat("{0},", GetStringProperty(LP.BaseLexeme));
            sb.AppendFormat("{0},", GetStringProperty(LP.CType));
            sb.AppendFormat("{0}", GetStringProperty(LP.CForm));
            return sb.ToString();
        }

        // ToString()に準ずるが、Compareする際にSurfaceの次にPOSを優先する.
        // DepEditService.FindAllLexemeCandidates()で使用.
        public string ToString3()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0},", GetStringProperty(LP.Surface));
            sb.AppendFormat("{0},", GetStringProperty(LP.PartOfSpeech));
            sb.AppendFormat("{0},", GetStringProperty(LP.Reading));
            sb.AppendFormat("{0},", GetStringProperty(LP.LemmaForm));
            sb.AppendFormat("{0},", GetStringProperty(LP.Pronunciation));
            sb.AppendFormat("{0},", GetStringProperty(LP.BaseLexeme));
            sb.AppendFormat("{0},", GetStringProperty(LP.Lemma));
            sb.AppendFormat("{0},", GetStringProperty(LP.CType));
            sb.AppendFormat("{0}", GetStringProperty(LP.CForm));
            return sb.ToString();
        }

        public string[] ToPropertyArray()
        {
            string[] ret = new string[(int)LP.Max];
            ret[(int)LP.Surface] = this.Surface;
            ret[(int)LP.Reading] = this.Reading;
            ret[(int)LP.LemmaForm] = this.LemmaForm;
            ret[(int)LP.Pronunciation] = this.Pronunciation;
            ret[(int)LP.BaseLexeme] = this.BaseLexeme.Surface;
            ret[(int)LP.Lemma] = this.Lemma;
            ret[(int)LP.PartOfSpeech] = this.PartOfSpeech.Name;
            ret[(int)LP.CType] = this.CType.Name;
            ret[(int)LP.CForm] = this.CForm.Name;
            return ret;
        }

        public int CompareTo(object obj)
        {
            Lexeme compareTo = (Lexeme)obj;
            if (CompareKey == (int)LP.Max)    // Compare by Frequency
            {
                if (CompareAscending)
                {
                    return this.Frequency - compareTo.Frequency;
                }
                else
                {
                    return compareTo.Frequency - this.Frequency;
                }
            }
            string s1 = GetStringProperty((LP)CompareKey);
            string s2 = compareTo.GetStringProperty((LP)CompareKey);
            if (CompareAscending)
            {
                return string.CompareOrdinal(s1, s2);
            }
            return string.CompareOrdinal(s2, s1);
        }

        /// <summary>
        /// Lexemeの一部属性を、Compare時にn/a扱いとなるように空にする。
        /// 一時的なLexemeにのみ使用する。
        /// </summary>
        /// <param name="filter"></param>
        public void ApplyFilter(LexemeFilter filter)
        {
            if (filter.IsFiltered(LP.Surface))
            {
                this.Surface = null;
            }
            if (filter.IsFiltered(LP.Reading))
            {
                this.Reading = null;
            }
            if (filter.IsFiltered(LP.LemmaForm))
            {
                this.LemmaForm = null;
            }
            if (filter.IsFiltered(LP.Pronunciation))
            {
                this.Pronunciation = null;
            }
            if (filter.IsFiltered(LP.BaseLexeme))
            {
                this.BaseLexeme = null;
            }
            if (filter.IsFiltered(LP.Lemma))
            {
                this.Lemma = null;
            }
            if (filter.IsFiltered(LP.PartOfSpeech))
            {
                this.PartOfSpeech = null;
            }
            if (filter.IsFiltered(LP.CType))
            {
                this.CType = null;
            }
            if (filter.IsFiltered(LP.CForm))
            {
                this.CForm = null;
            }
        }

        public string ToFilteredString(LexemeFilter filter)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < (int)LP.Max; i++) {
                if (!filter.IsFiltered((LP)i))
                {
                    if (sb.Length > 0) {
                        sb.Append( "/");
                    }
                    sb.Append(GetStringPropertyShort((LP)i));
                }
            }
            return sb.ToString();
        }

        public string GetCustomProperty(string customTagName)
        {
            string res;
            if (customPropertyMapping.TryGetValue(customTagName, out res))
            {
                return res;
            }
            return null;
        }

        public bool ReplaceReading(string value)
        {
            if (this.Reading != value)
            {
                this.Reading = value;
                return true;
            }
            return false;
        }

        public bool ReplacePronunciation(string value)
        {
            if (this.Pronunciation != value)
            {
                this.Pronunciation = value;
                return true;
            }
            return false;
        }

        public bool ReplaceLemma(string value)
        {
            if (this.Lemma != value)
            {
                this.Lemma = value;
                return true;
            }
            return false;
        }

        public bool ReplaceLemmaForm(string value)
        {
            if (this.LemmaForm != value)
            {
                this.LemmaForm = value;
                return true;
            }
            return false;
        }

        public bool ReplaceCustomProperty(string value)
        {
            if (this.CustomProperty != value)
            {
                this.CustomProperty = value;
                return true;
            }
            return false;
        }

        public Dictionary<string,string> GetPropertyAsDictionary()
        {
            var result = new Dictionary<string, string>();
            result.Add(Lexeme.PropertyName[LP.Surface], this.Surface);
            result.Add(Lexeme.PropertyName[LP.Reading], this.Reading);
            result.Add(Lexeme.PropertyName[LP.LemmaForm], this.LemmaForm);
            result.Add(Lexeme.PropertyName[LP.Pronunciation], this.Pronunciation);
            result.Add(Lexeme.PropertyName[LP.BaseLexeme], this.BaseLexeme.Surface);
            result.Add(Lexeme.PropertyName[LP.Lemma], this.Lemma);
            result.Add(Lexeme.PropertyName[LP.PartOfSpeech], this.PartOfSpeech.Name);
            result.Add(Lexeme.PropertyName[LP.CType], this.CType.Name);
            result.Add(Lexeme.PropertyName[LP.CForm], this.CForm.Name);
            result.Add("Custom", this.CustomProperty);

            return result;
        }
    }
}
