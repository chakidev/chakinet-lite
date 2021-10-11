using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Search;

namespace ChaKi.Entity.Settings
{
    /// <summary>
    /// GUIよりも下の層に関係するセッティング
    /// （GUI設定はGUISettingクラスで管理する）
    /// </summary>
    public class UserSettings
    {
        public static UserSettings GetInstance()
        {
            if (m_Instance == null)
            {
                m_Instance = new UserSettings();
            }
            return m_Instance;
        }

        private static UserSettings m_Instance;

        private UserSettings()
        {
            LastCorpus = new List<Corpus>();
            LastKeywords = new List<string>();
            ExportSetting = new ExportSetting();
        }

        // 前回使用したコーパスのリスト
        public List<Corpus> LastCorpus { get; set; }

        // 検索履歴（Surface, Reading, Pronunciation） Queueにしたいが、XmlSerializeできない
        public List<string> LastKeywords { get; set; }

        // デフォルトDBアクセスパラメータ (DBLoginフォームで設定された最新のもの）
        public string DefaultDBServer { get; set; }
        public string DefaultDBUser { get; set; }
        public string DefaultDBPassword { get; set; }
        public string DefaultDBMS { get; set; }

        // CorpusSourceReaderのデフォルト入力ファイル文字コード
        public string DefaultCorpusSourceEncoding { get; set; }
        public string DefaultCorpusSourceType { get; set; }

        // Collocationのデフォルト設定
        public CollocationCondition DefaultCollCond { get; set; }
        public string[] DefaultStopwords { get; set; } // 上と重複してセーブされるがそのままとする.

        /// <summary>
        /// Excel Export設定
        /// </summary>
        /// <param name="file"></param>
        public ExportSetting ExportSetting { get; set; }

        public void Load(string file)
        {
            using (StreamReader rd = new StreamReader(file)) {
                XmlSerializer ser = new XmlSerializer(typeof(UserSettings));
                UserSettings.m_Instance = (UserSettings)ser.Deserialize(rd);
            }
        }

        public void Save(string file)
        {
            using (StreamWriter wr = new StreamWriter(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(UserSettings));
                ser.Serialize(wr, this);
            }
        }

        // 検索履歴に追加
        public void AddKeywordHistory(string tag, string value)
        {
            if (tag == Lexeme.PropertyName[LP.PartOfSpeech]
             || tag == Lexeme.PropertyName[LP.CType]
             || tag == Lexeme.PropertyName[LP.CForm])
            {
                // 上の３つのプロパティは履歴化しない
                return;
            }
            if (tag.Length == 0 || value.Length == 0)
            {
                return;
            }
            string s = string.Format("{0}={1}", tag, value);
            if (LastKeywords.Count > 100)
            {
                LastKeywords.RemoveAt(0);
            }
            if (!LastKeywords.Contains(s))
            {
                LastKeywords.Add(s);
            }
        }

        public void ClearKeywordHistory()
        {
            LastKeywords.Clear();
        }
    }
}
