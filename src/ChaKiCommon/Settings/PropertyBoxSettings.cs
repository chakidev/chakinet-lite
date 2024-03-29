﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using ChaKi.Entity.Corpora;

namespace ChaKi.Common.Settings
{
    public class PropertyBoxSettings
    {
        public static PropertyBoxSettings Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new PropertyBoxSettings();
                }
                return m_Instance;
            }
        }
        private static PropertyBoxSettings m_Instance;

        public List<PropertyBoxItemSetting> Settings { get; set; }

        public LWP KwicRow1Property { get; set; }

        public LWP KwicRow2Property { get; set; }

        // Default-Visible properties for UD Treebank Corpus
        public static LWP[] DefaultVisibleProperty { get; private set; } =
            {LWP.Surface, LWP.LemmaForm, LWP.UPOS, LWP.XPOS, LWP.Custom };

        public event EventHandler SettingChanged;

        public PropertyBoxSettings()
        {
            this.Settings = new List<PropertyBoxItemSetting>();
            this.KwicRow1Property = LWP.Surface;
            this.KwicRow2Property = LWP.UPOS;
        }

        public PropertyBoxSettings(PropertyBoxSettings src)
            : this()
        {
            CopyFrom(src);
        }

        public void CopyFrom(PropertyBoxSettings src)
        {
            this.Settings.Clear();
            foreach (PropertyBoxItemSetting s in src.Settings)
            {
                this.Settings.Add(new PropertyBoxItemSetting(s));
            }
            if (SettingChanged != null)
            {
                SettingChanged(this, null);
            }
            this.KwicRow1Property = src.KwicRow1Property;
            this.KwicRow2Property = src.KwicRow2Property;
            UpdateKwicRows();
        }

        public static void Clear()
        {
            Instance.Settings.Clear();
        }

        public static void Default()
        {
            Default(Instance.Settings);
            Instance.KwicRow1Property = LWP.Surface;
            Instance.KwicRow2Property = LWP.UPOS;
        }

        public static void Default(IList<PropertyBoxItemSetting> list)
        {
            list.Clear();
            foreach (KeyValuePair<LWP, string> pair in Lexeme.LWPropertyName)
            {
                var val = new PropertyBoxItemSetting(pair.Value);
                if (pair.Key == LWP.Surface)
                {
                    val.IsKwicRow1 = true;
                    val.DisplayName = "FORM"; // UD Treebak Corpus Naming
                }
                else if (pair.Key == LWP.UPOS)
                {
                    val.IsKwicRow2 = true;
                }
                else if (pair.Key == LWP.LemmaForm)
                {
                    val.DisplayName = "LEMMA"; // UD Treebak Corpus Naming
                }
                else if (pair.Key == LWP.Custom)
                {
                    val.DisplayName = "FEATS"; // UD Treebak Corpus Naming
                }

                if (!DefaultVisibleProperty.Contains(pair.Key))
                {
                    val.IsVisible = false;
                }

                list.Add(val);
            }
        }

        public static void Load(string file)
        {
            Clear();
            using (StreamReader rd = new StreamReader(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(PropertyBoxSettings));
                m_Instance = (PropertyBoxSettings)ser.Deserialize(rd);
            }
            //SettingsにUPOSがない場合は古い設定と見なし、デフォルトで初期化する。
            if (m_Instance.Settings.FirstOrDefault(s => s.TagName == "UPOS") == null)
            {
               Default();
            }

            m_Instance.UpdateKwicRows();
        }

        public static void Save(string file)
        {
            using (StreamWriter wr = new StreamWriter(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(PropertyBoxSettings));
                ser.Serialize(wr, Instance);
            }
        }

        public void UpdateKwicRows()
        {
            for (int i = this.Settings.Count - 1; i >= 0; i--)
            {
                var item = this.Settings[i];
                if (item.IsKwicRow1)
                {
                    this.KwicRow1Property = (LWP)i;
                }
                if (item.IsKwicRow2)
                {
                    this.KwicRow2Property = (LWP)i;
                }
            }
        }
    }


    public class PropertyBoxItemSetting
    {
        public PropertyBoxItemSetting()
        {
            this.TagName = string.Empty;
            this.DisplayName = string.Empty;
            this.IsVisible = false;
            this.IsKwicRow1 = false;
            this.IsKwicRow2 = false;
        }

        public PropertyBoxItemSetting(string tagName)
        {
            this.TagName = tagName;
            this.DisplayName = tagName;
            this.IsVisible = true;
            this.IsKwicRow1 = false;
            this.IsKwicRow2 = false;
        }

        public PropertyBoxItemSetting(string tagName, string displayName, bool isVisible)
        {
            this.TagName = tagName;
            this.DisplayName = displayName;
            this.IsVisible = isVisible;
            this.IsKwicRow1 = false;
            this.IsKwicRow2 = false;
        }

        public PropertyBoxItemSetting(PropertyBoxItemSetting src)
        {
            this.TagName = src.TagName;
            this.DisplayName = src.DisplayName;
            this.IsVisible = src.IsVisible;
            this.IsKwicRow1 = src.IsKwicRow1;
            this.IsKwicRow2 = src.IsKwicRow2;
        }

        public string TagName { get; set; }
        public string DisplayName { get; set; }
        public bool IsVisible { get; set; }
        public bool IsKwicRow1 { get; set; }
        public bool IsKwicRow2 { get; set; }
    }
}
