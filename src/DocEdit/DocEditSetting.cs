using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Drawing;
using ChaKi.Common.Settings;
using ChaKi.Common;

namespace ChaKi.DocEdit
{
    public class DocEditSettings
    {
        public static DocEditSettings Instance { get; private set; }

        public DocumentSelectorSettings DocumentSelectorSettings { get; set; }
        public Rectangle WindowLocation { get; set; }
        public List<int> GridWidthSentenceList { get; set; }
        public List<int> GridWidthAttributeList { get; set; }
        public List<int> GridWidthDocumentSelector { get; set; }
        public int TextSnipLength { get; set; }
        public string UILocale { get; set; }

        static DocEditSettings()
        {
            Instance = new DocEditSettings();
            Instance.WindowLocation = new Rectangle(0, 0, 800, 600);
            Instance.GridWidthSentenceList = new List<int> { 80, 200, 80 };
            Instance.GridWidthAttributeList = new List<int> { 70, 70 };
            Instance.GridWidthDocumentSelector = new List<int> { 30 };
            Instance.TextSnipLength = 10;
            Instance.UILocale = string.Empty;
        }

        private DocEditSettings()
        {
            // Defaults
            this.DocumentSelectorSettings = DocumentSelectorSettings.Current;
            this.WindowLocation = new Rectangle(0, 0, 800, 600);
            this.GridWidthSentenceList = new List<int>();
            this.GridWidthAttributeList = new List<int>();
            this.GridWidthDocumentSelector = new List<int>();
        }

        public DocEditSettings(DocEditSettings src)
            : this()
        {
            this.CopyFrom(src);
        }

        public void CopyFrom(DocEditSettings src)
        {
            this.DocumentSelectorSettings = src.DocumentSelectorSettings.Copy();
            this.WindowLocation = src.WindowLocation;
            this.GridWidthSentenceList = new List<int>(src.GridWidthSentenceList);
            this.GridWidthAttributeList = new List<int>(src.GridWidthAttributeList);
            this.GridWidthDocumentSelector = new List<int>(src.GridWidthDocumentSelector);
            this.TextSnipLength = src.TextSnipLength;
            this.UILocale = src.UILocale;
        }

        /// <summary>
        /// 設定をXMLファイルから読み込む
        /// </summary>
        /// <param name="file"></param>
        public static void Load(string file)
        {
            using (StreamReader rd = new StreamReader(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(DocEditSettings));
                Instance = (DocEditSettings)ser.Deserialize(rd);
            }
            DocumentSelectorSettings.Current = Instance.DocumentSelectorSettings;
        }

        /// <summary>
        /// 設定をXMLファイルへ書き出す
        /// </summary>
        /// <param name="file"></param>
        public static void Save(string file)
        {
            using (StreamWriter wr = new StreamWriter(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(DocEditSettings));
                ser.Serialize(wr, Instance);
            }
        }
    }
}
