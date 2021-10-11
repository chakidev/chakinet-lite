using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ChaKi.Entity.Corpora.Annotations;

namespace ChaKi.Entity.Settings
{
    public class TagSettingItem
    {
        public static TagSettingItem Default = new TagSettingItem();

        public TagSettingItem()
        {
            this.ShowInSelectorMenu = false;
            this.VisibleInKwicView = false;
            this.Color = "LightGray";
            this.Alpha = 255;
            this.Width = 1.0f;
            ShortcutKey = '\0';
        }

        public TagSettingItem(TagSettingItem src)
        {
            this.CopyFrom(src);
        }

        public void CopyFrom(TagSettingItem src)
        {
            this.ShowInSelectorMenu = src.ShowInSelectorMenu;
            this.VisibleInKwicView = src.VisibleInKwicView;
            this.Color = src.Color;
            this.Alpha = src.Alpha;
            this.Width = src.Width;
            this.ShortcutKey = src.ShortcutKey;
        }

        /// <summary>
        /// TagSelector PopupMenuにおいてトップレベル項目として表示するか.
        /// </summary>
        public bool ShowInSelectorMenu { get; set; }

        /// <summary>
        /// KwicViewにおいてAnnotation表示ONの場合にこのTagを表示するか.
        /// </summary>
        public bool VisibleInKwicView { get; set; }

        /// <summary>
        /// 描画色HTML Color値（Segment, Linkのみで有効, GroupはIndexに応じてCyclicColorが自動付与される）
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// 描画色のα値（透明度）:0-255
        /// </summary>
        public byte Alpha { get; set; }

        /// <summary>
        /// 描画線幅（Segment, Linkのみで有効）
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// ショートカットキー（無設定は'\0'）
        /// </summary>
        public char ShortcutKey { get; set; }

    }

    public class TagSetting
    {
        public static TagSetting Instance;

        public SerializableDictionary<string, TagSettingItem> Segment;
        public SerializableDictionary<string, TagSettingItem> Link;
        public SerializableDictionary<string, TagSettingItem> Group;

        static TagSetting()
        {
            Instance = new TagSetting();
            // Defaults
            Instance.Segment.Add("Bunsetsu", new TagSettingItem()
            {
                VisibleInKwicView = true,
                ShowInSelectorMenu = false,
                Color = "Plum",
                Alpha = 255,
                Width = 1.0f,
                ShortcutKey = '\0'
            });
            Instance.Segment.Add("Apposition", new TagSettingItem()
            {
                VisibleInKwicView = true,
                ShowInSelectorMenu = false,
                Color = "LimeGreen",
                Alpha = 255,
                Width = 2.5f,
                ShortcutKey = '\0'
            });
            Instance.Segment.Add("Parallel", new TagSettingItem()
            {
                VisibleInKwicView = true,
                ShowInSelectorMenu = false,
                Color = "MediumVioletRed",
                Alpha = 255,
                Width = 2.5f,
                ShortcutKey = '\0'
            });
            Instance.Segment.Add("Nest", new TagSettingItem()
            {
                VisibleInKwicView = true,
                ShowInSelectorMenu = true,
                Color = "DarkBlue",
                Alpha = 255,
                Width = 2.5f,
                ShortcutKey = '\0'
            });
            Instance.Link.Add("A", new TagSettingItem()
            {
                VisibleInKwicView = true,
                ShowInSelectorMenu = true,
                Color = "Gray",
                Alpha = 255,
                Width = 1.0f,
                ShortcutKey = '\0'
            });
            Instance.Link.Add("O", new TagSettingItem()
            {
                VisibleInKwicView = true,
                ShowInSelectorMenu = true,
                Color = "Gray",
                Alpha = 255,
                Width = 1.0f,
                ShortcutKey = '\0'
            });
            Instance.Link.Add("D", new TagSettingItem()
            {
                VisibleInKwicView = true,
                ShowInSelectorMenu = true,
                Color = "Gray",
                Alpha = 255,
                Width = 1.0f,
                ShortcutKey = '\0'
            });
            Instance.Group.Add("Apposition", new TagSettingItem()
            {
                VisibleInKwicView = false,
                ShowInSelectorMenu = true,
                ShortcutKey = '\0'
            });
            Instance.Group.Add("Parallel", new TagSettingItem()
            {
                VisibleInKwicView = false,
                ShowInSelectorMenu = true,
                ShortcutKey = '\0'
            });
        }

        public TagSetting()
        {
            this.Segment = new SerializableDictionary<string, TagSettingItem>();
            this.Link = new SerializableDictionary<string, TagSettingItem>();
            this.Group = new SerializableDictionary<string, TagSettingItem>();
        }

        public TagSetting(TagSetting src)
            : this()
        {
            this.CopyFrom(src);
        }

        public void CopyFrom(TagSetting src)
        {
            this.Segment.Clear();
            this.Link.Clear();
            this.Group.Clear();
            foreach (KeyValuePair<string, TagSettingItem> pair in src.Segment)
            {
                this.Segment.Add(pair.Key, new TagSettingItem(pair.Value));
            }
            foreach (KeyValuePair<string, TagSettingItem> pair in src.Link)
            {
                this.Link.Add(pair.Key, new TagSettingItem(pair.Value));
            }
            foreach (KeyValuePair<string, TagSettingItem> pair in src.Group)
            {
                this.Group.Add(pair.Key, new TagSettingItem(pair.Value));
            }
        }

        public static void Load(string file)
        {
            using (StreamReader rd = new StreamReader(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(TagSetting));
                Instance = (TagSetting)ser.Deserialize(rd);
            }
        }

        public static void Save(string file)
        {
            using (StreamWriter wr = new StreamWriter(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(TagSetting));
                ser.Serialize(wr, Instance);
            }
        }

        public List<string> GetVisibleNameList(string tagType)
        {
            Dictionary<string,TagSettingItem> list = null;
            if (tagType == Tag.SEGMENT)
            {
                list = this.Segment;
            }
            else if (tagType == Tag.LINK)
            {
                list = this.Link;
            }
            else if (tagType == Tag.GROUP)
            {
                list = this.Group;
            }
            if (list == null)
            {
                return null;
            }
            List<string> result = new List<string>();
            foreach (KeyValuePair<string, TagSettingItem> item in list)
            {
                if (item.Value.VisibleInKwicView)
                {
                    result.Add(item.Key);
                }
            }
            return result;
        }
    }
}
