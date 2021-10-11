using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ChaKi.Common.Settings
{
    public class TagSelectorSettingItem : ICloneable
    {
        public Size Size { get; set; }
        public int Style { get; set; }

        public TagSelectorSettingItem()
        {
            Size = new Size(300, 100);
            Style = (int)System.Windows.Forms.View.Details;
        }

        public object Clone()
        {
            TagSelectorSettingItem ret = new TagSelectorSettingItem();
            ret.Size = this.Size;
            ret.Style = this.Style;
            return ret;
        }

        public void CopyFrom(TagSelectorSettingItem src)
        {
            this.Size = src.Size;
            this.Style = src.Style;
        }
    }

    public class TagSelectorSettings : ICloneable
    {
        public static TagSelectorSettings Current = new TagSelectorSettings();

        public TagSelectorSettingItem SegmentPanel { get; set; }
        public TagSelectorSettingItem LinkPanel { get; set; }
        public TagSelectorSettingItem GroupPanel { get; set; }

        public int SegmentPanelStyle { get; set; }
        public int LinkPanelStyle { get; set; }
        public int GroupPanelStyle { get; set; }

        private TagSelectorSettings()
        {
            this.SegmentPanel = new TagSelectorSettingItem();
            this.LinkPanel = new TagSelectorSettingItem();
            this.GroupPanel = new TagSelectorSettingItem();
        }

        public TagSelectorSettings Copy()
        {
            TagSelectorSettings obj = new TagSelectorSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(TagSelectorSettings src)
        {
            this.SegmentPanel = src.SegmentPanel;
            this.LinkPanel = src.LinkPanel;
            this.GroupPanel = src.GroupPanel;
        }

        public object Clone()
        {
            TagSelectorSettings obj = new TagSelectorSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public TagSelectorSettingItem GetByType(string tagType)
        {
            if (tagType == ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT)
            {
                return this.SegmentPanel;
            }
            else if (tagType == ChaKi.Entity.Corpora.Annotations.Tag.LINK)
            {
                return this.LinkPanel;
            }
            else if (tagType == ChaKi.Entity.Corpora.Annotations.Tag.GROUP)
            {
                return this.GroupPanel;
            }
            return null;
        }

        public void SetByType(string tagType, TagSelectorSettingItem val)
        {
            if (tagType == ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT)
            {
                this.SegmentPanel = val;
            }
            else if (tagType == ChaKi.Entity.Corpora.Annotations.Tag.LINK)
            {
                this.LinkPanel = val;
            }
            else if (tagType == ChaKi.Entity.Corpora.Annotations.Tag.GROUP)
            {
                this.GroupPanel = val;
            }
        }
    }
}
