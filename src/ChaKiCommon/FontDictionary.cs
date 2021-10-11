using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Drawing;
using System.Xml;

namespace ChaKi.Common
{
    [XmlRoot("FontDictionary")]
    public class FontDictionary : Dictionary<string, Font>, IXmlSerializable
    {
        public static FontDictionary Current = new FontDictionary();

        public event EventHandler FontChanged;

        static public string DefaultBaseTextFontName { get { return "ＭＳゴシック"; } }
        static public string DefaultAnsiTextFontName { get { return "Lucida Sans Unicode"; } }
        static public float DefaultBaseTextFontSize { get { return 11.0f; } }
        static public float DefaultAnsiTextFontSize { get { return 7.0f; } }

        private FontDictionary()
        {
            Add("BaseText", new Font(DefaultBaseTextFontName, DefaultBaseTextFontSize));
            Add("BaseAnsi", new Font(DefaultAnsiTextFontName, DefaultAnsiTextFontSize));
        }

        public FontDictionary Copy()
        {
            FontDictionary fd = new FontDictionary();
            fd.CopyFrom(this);
            return fd;
        }

        public void CopyFrom(FontDictionary src)
        {
            if (src == this) return;
            this.Clear();
            foreach (KeyValuePair<string, Font> pair in src)
            {
                this.Add(pair.Key, (Font)pair.Value.Clone());
            }
            this.FontChanged = src.FontChanged;
        }

        #region IXmlSerializable メンバ
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer guSerializer = new XmlSerializer(typeof(GraphicsUnit));
            XmlSerializer fsSerializer = new XmlSerializer(typeof(FontStyle));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
            {
                return;
            }

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");
                string key = (string)keySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement("value");
                string fontFamily = reader.ReadElementString("fontFamily");
                float fontSize = XmlConvert.ToSingle(reader.ReadElementString("fontSize"));
                GraphicsUnit graphicsUnit = (GraphicsUnit)guSerializer.Deserialize(reader);
                FontStyle fontStyle = (FontStyle)fsSerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadEndElement();
                this[key] = new Font(fontFamily, fontSize, fontStyle, graphicsUnit);
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer guSerializer = new XmlSerializer(typeof(GraphicsUnit));
            XmlSerializer fsSerializer = new XmlSerializer(typeof(FontStyle));

            foreach (string key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                // FontはそのままではXmlSerializeできない
                // --> <fontFamily/><fontSize/><GraphicsUnit/><FontStyle/>に分けて出力する
                Font font = this[key];
                writer.WriteElementString("fontFamily", font.FontFamily.Name);
                writer.WriteElementString("fontSize", XmlConvert.ToString(font.Size));
                guSerializer.Serialize(writer, font.Unit);
                fsSerializer.Serialize(writer, font.Style); 
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
        #endregion

        public void RaiseUpdate()
        {
            if (this.FontChanged != null)
            {
                this.FontChanged(this, EventArgs.Empty);
            }
        }
    }
}
