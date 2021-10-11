using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Crownwood.DotNetMagic.Common;
using System.Drawing;
using ChaKi.Common.Settings;
using ChaKi.Common;

namespace ChaKi
{
    public class GUISetting
    {
        public static GUISetting Instance { get; private set; }

        public VisualStyle Visual { get; set; }
        public FontDictionary Fonts { get; set; }
        public KwicViewSettings KwicViewSettings { get; set; }
        public GridSettings CommandPanelGridSettings { get; set; }
        public GridSettings AttributePanelGridSettings { get; set; }
        public GridSettings WordAttributePanelGridSettings { get; set; }
        public DepEditSettings DepEditSettings { get; set; }
        public SearchSettings SearchSettings { get; set; }
        public bool AutoLogon { get; set; }
        public TagSelectorSettings TagSelectorSettings { get; set; }
        public AttributeSelectorSettings AttributeSelectorSettings { get; set; }
        public CollocationViewSettings CollocationViewSettings { get; set; }
        public ContextPanelSettings ContextPanelSettings { get; set; }
        public string UILocale { get; set; }
        public bool UseShortPOS { get; set; }
        public string ExternalEditorPath { get; set; }
        public bool SkipDbSchemaConversionDialog { get; set; }
        public GitSettings GitSettings { get; set; }

        public event EventHandler VisualStyleChanged;

        // Subitem Accessors
        [XmlIgnore]
        public string KwicViewBackground
        {
            get { return this.KwicViewSettings.Background; }
            set { this.KwicViewSettings.Background = value; }
        }
        [XmlIgnore]
        public string DepEditBackground
        {
            get { return this.DepEditSettings.Background; }
            set { this.DepEditSettings.Background = value; }
        }


        static GUISetting()
        {
            Instance = new GUISetting();
        }

        private GUISetting()
        {
            // Defaults
            this.Visual = VisualStyle.Office2007Silver;

            this.Fonts = FontDictionary.Current;
            this.KwicViewSettings = KwicViewSettings.Current;
            this.DepEditSettings = DepEditSettings.Current;
            this.SearchSettings = SearchSettings.Current;
            this.TagSelectorSettings = TagSelectorSettings.Current;
            this.AttributeSelectorSettings = AttributeSelectorSettings.Current;
            this.CollocationViewSettings = CollocationViewSettings.Current;
            this.ContextPanelSettings = ContextPanelSettings.Current;
            this.GitSettings = GitSettings.Current;
            this.AutoLogon = true;
            this.UILocale = string.Empty;
            this.UseShortPOS = true;
            this.ExternalEditorPath = "notepad.exe";
            this.SkipDbSchemaConversionDialog = false;
        }

        public GUISetting(GUISetting src)
            : this()
        {
            this.CopyFrom(src);
        }

        public void CopyFrom(GUISetting src)
        {
            this.Visual = src.Visual;
            this.Fonts = src.Fonts.Copy();
            this.SearchSettings = src.SearchSettings.Copy();
            this.AutoLogon = src.AutoLogon;
            this.TagSelectorSettings = src.TagSelectorSettings.Copy();
            this.AttributeSelectorSettings = src.AttributeSelectorSettings.Copy();
            this.KwicViewSettings = src.KwicViewSettings.Copy();
            this.DepEditSettings = src.DepEditSettings.Copy();
            this.CollocationViewSettings = src.CollocationViewSettings.Copy();
            this.ContextPanelSettings = src.ContextPanelSettings.Copy();
            this.UILocale = src.UILocale;
            this.UseShortPOS = src.UseShortPOS;
            this.ExternalEditorPath = src.ExternalEditorPath;
            this.SkipDbSchemaConversionDialog = src.SkipDbSchemaConversionDialog;
            this.GitSettings = src.GitSettings.Copy();
        }

        public void RaiseUpdate()
        {
            if (this.VisualStyleChanged != null)
            {
                this.VisualStyleChanged(this, null);
            }
            this.Fonts.RaiseUpdate();
        }

        public Font GetBaseTextFont()
        {
            return this.Fonts["BaseText"];
        }

        public Font GetBaseAnsiFont()
        {
            return this.Fonts["BaseAnsi"];
        }

        public void SetFont(string tag, Font font)
        {
            this.Fonts[tag] = font;
            this.Fonts.RaiseUpdate();
        }

        /// <summary>
        /// 設定をXMLファイルから読み込む
        /// </summary>
        /// <param name="file"></param>
        public static void Load(string file)
        {
            using (StreamReader rd = new StreamReader(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(GUISetting));
                Instance = (GUISetting)ser.Deserialize(rd);
            }
            FontDictionary.Current = Instance.Fonts;
            KwicViewSettings.Current = Instance.KwicViewSettings;
            DepEditSettings.Current = Instance.DepEditSettings;
            SearchSettings.Current = Instance.SearchSettings;
            TagSelectorSettings.Current = Instance.TagSelectorSettings;
            AttributeSelectorSettings.Current = Instance.AttributeSelectorSettings;
            CollocationViewSettings.Current = Instance.CollocationViewSettings;
            ContextPanelSettings.Current = Instance.ContextPanelSettings;
            GitSettings.Current = Instance.GitSettings;
        }

        /// <summary>
        /// 設定をXMLファイルへ書き出す
        /// </summary>
        /// <param name="file"></param>
        public static void Save(string file)
        {
            using (StreamWriter wr = new StreamWriter(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(GUISetting));
                ser.Serialize(wr, Instance);
            }
        }
    }
}
