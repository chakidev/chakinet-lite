using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using ChaKi.Entity.Corpora;

namespace ChaKi.Entity.Settings
{
    /// <summary>
    /// WordColorの設定をすべて管理するSingletonインスタンス
    /// "WordColorSettings.xml"ファイルに対応
    /// </summary>
    public class WordColorSettings
    {
        public static WordColorSettings GetInstance()
        {
            if (m_Instance == null)
            {
                m_Instance = new WordColorSettings();
            }
            return m_Instance;
        }

        private static WordColorSettings m_Instance;

        private WordColorSettings()
        {
            this.SettingDictionary = new SerializableDictionary<string, WordColorSetting[]>();
        }

        // コーパス名→WordColorSettingリスト
        // コーパス名の文字列で識別するので、コーパス名が重複しないよう注意！
        public SerializableDictionary<string, WordColorSetting[]> SettingDictionary { get; set; }

        public void Load(string file)
        {
            using (StreamReader rd = new StreamReader(file)) {
                XmlSerializer ser = new XmlSerializer(typeof(WordColorSettings));
                WordColorSettings.m_Instance = (WordColorSettings)ser.Deserialize(rd);
            }
        }

        public void Save(string file)
        {
            using (StreamWriter wr = new StreamWriter(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(WordColorSettings));
                ser.Serialize(wr, this);
            }
        }

        public WordColorSetting[] Find(string corpusName)
        {
            WordColorSetting[] result;
            if (this.SettingDictionary.TryGetValue(corpusName, out result) == false)
            {
                return null;
            }
            return result;
        }

        public WordColorSetting[] FindOrCreate(string corpusName)
        {
            WordColorSetting[] result;
            if (this.SettingDictionary.TryGetValue(corpusName, out result) == false)
            {
                result = AddNew(corpusName);
            }
            return result;
        }

        public WordColorSetting[] AddNew(string corpusName)
        {
            WordColorSetting[] wcs = new WordColorSetting[3] {
                new WordColorSetting(),
                new WordColorSetting(),
                new WordColorSetting()
            };
            this.SettingDictionary[corpusName] = wcs;
            return wcs;
        }

        public WordColorSetting Match(string corpusName, Lexeme lex)
        {
            WordColorSetting[] result = Find(corpusName);
            if (result == null)
            {
                return null;
            }

            foreach (WordColorSetting wcs in result)
            {
                if (wcs.Match(lex))
                {
                    return wcs;
                }
            }
            return null;
        }
    }
}
