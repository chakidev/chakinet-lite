using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace ChaKi.Common.Settings
{
    public class Text2CorpusSettings
    {
        public static Text2CorpusSettings Instance = new Text2CorpusSettings();

        public Text2CorpusSettings()
        {
            Default();
        }

        public static void Load(string file)
        {
            var ser = new XmlSerializer(typeof(Text2CorpusSettings));
            try
            {
                using (var str = new FileStream(file, FileMode.Open))
                {
                    Instance = ser.Deserialize(str) as Text2CorpusSettings;
                }
            }
            catch (Exception ex)
            {
                Instance.Default();
            }
        }

        public void Save(string file)
        {
            if (this.BunruiOutputFormat <= 0)
            {
                this.BunruiOutputFormat = BunruiOutputFormat.None;
            }
            var ser = new XmlSerializer(typeof(Text2CorpusSettings));
            using (var str = new FileStream(file, FileMode.Create))
            {
                ser.Serialize(str, Instance);
            }
        }

        public void Default()
        {
            this.DoSentenceSeparation = true;
            this.DoWordAnalysis = true;
            this.DoChunkAnalysis = true;
            this.MecabModel = "default";
            this.CabochaModel = "default";
            this.SeparatorChars = "。．！？」";
            this.CloseOnDone = false;
            this.CRLFMode = CRLFModes.MultipleOnly;
            this.UnicodeNormalization = null;
            this.DoZenkakuConversion = true;
            this.MecabOutputFormat = "default";
            this.WindowWidth = -1;
            this.BunruiOutputFormat = 0;
        }

        public bool DoSentenceSeparation;

        public bool DoWordAnalysis;

        public bool DoChunkAnalysis;

        public string SeparatorChars;

        public string MecabModel;

        public string CabochaModel;

        public string OutputFile;

        public bool CloseOnDone;

        public CRLFModes CRLFMode;

        public NormalizationForm? UnicodeNormalization;

        public bool DoZenkakuConversion;

        public string MecabOutputFormat;

        public int WindowWidth;

        public BunruiOutputFormat BunruiOutputFormat;
    }

    public enum CRLFModes
    {
        MultipleOnly = 0,   // 単独のCR/LFを保持（空行のみ削除）
        SingleToSpace = 1,  // 単独のCR/LFを空白に置き換え
        Single = 2,         // 単独のCR/LFを削除
        All = 3             // すべてのCR/LFを削除
    }

    public enum BunruiOutputFormat
    {
        None = 0,
        Id = 1,
        Label = 2,
    }
}
