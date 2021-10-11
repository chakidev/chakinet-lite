using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace ChaKi.Common.Settings
{
    public class DictionarySettings : List<DictionarySettingItem>
    {
        public static DictionarySettings Instance = new DictionarySettings();

        public void CopyFrom(DictionarySettings src)
        {
            Clear();
            foreach (DictionarySettingItem item in src)
            {
                Add(new DictionarySettingItem(item));
            }
        }

        /// <summary>
        /// 設定をXMLファイルから読み込む
        /// </summary>
        /// <param name="file"></param>
        public static void Load(string file)
        {
            using (StreamReader rd = new StreamReader(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(DictionarySettings));
                Instance = (DictionarySettings)ser.Deserialize(rd);
            }
        }

        /// <summary>
        /// 設定をXMLファイルへ書き出す
        /// </summary>
        /// <param name="file"></param>
        public static void Save(string file)
        {
            using (StreamWriter wr = new StreamWriter(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(DictionarySettings));
                ser.Serialize(wr, Instance);
            }
        }
    }

    public class DictionarySettingItem
    {
        public DictionarySettingItem()
        {
            this.Name = string.Empty;
            this.Path = string.Empty;
            this.ReadOnly = true;
            this.IsUserDic = false;
            this.IsCompoundWordDic = false;
        }

        public DictionarySettingItem(DictionarySettingItem src)
        {
            this.Name = src.Name;
            this.Path = src.Path;
            this.ReadOnly = src.ReadOnly;
            this.IsUserDic = src.IsUserDic;
            this.IsCompoundWordDic = src.IsCompoundWordDic;
        }

        public string Name { get; set; }
        public string Path { get; set; }
        public bool ReadOnly { get; set; }
        public bool IsUserDic { get; set; }
        public bool IsCompoundWordDic { get; set; }
    }
}
