using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ChaKi.Common.Settings
{
    public class AttributeSelectorSettingItem : ICloneable
    {
        public Size Size { get; set; }
        public int Style { get; set; }

        public AttributeSelectorSettingItem()
        {
            Size = new Size(300, 100);
            Style = (int)System.Windows.Forms.View.Details;
        }

        public object Clone()
        {
            var ret = new AttributeSelectorSettingItem();
            ret.Size = this.Size;
            ret.Style = this.Style;
            return ret;
        }

        public void CopyFrom(AttributeSelectorSettingItem src)
        {
            this.Size = src.Size;
            this.Style = src.Style;
        }
    }

    public class AttributeSelectorSettings : ICloneable
    {
        public static AttributeSelectorSettings Current = new AttributeSelectorSettings();

        public AttributeSelectorSettingItem SentencePanel { get; set; }

        public int SentencePanelStyle { get; set; }

        private AttributeSelectorSettings()
        {
            this.SentencePanel = new AttributeSelectorSettingItem();
        }

        public AttributeSelectorSettings Copy()
        {
            var obj = new AttributeSelectorSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(AttributeSelectorSettings src)
        {
            this.SentencePanel = src.SentencePanel;
        }

        public object Clone()
        {
            var obj = new AttributeSelectorSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public AttributeSelectorSettingItem GetByType(string tagType)
        {
            if (tagType == "Sentence")
            {
                return this.SentencePanel;
            }
            return null;
        }

        public void SetByType(string tagType, AttributeSelectorSettingItem val)
        {
            if (tagType == "Sentence")
            {
                this.SentencePanel = val;
            }
        }
    }
}
