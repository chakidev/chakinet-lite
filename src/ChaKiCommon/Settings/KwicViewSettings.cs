using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Common.Settings
{
    public class KwicViewSettings
    {
        public bool ShowSegments;
        public bool ShowLinks;
        public bool ShowGroups;
        public int LineMargin;
        public int WordMargin;
        public int[] DefaultHeaderWidthNonKwic;
        public int[] DefaultHeaderWidthKwic;
        public string Background;

        public static KwicViewSettings Current = new KwicViewSettings();

        private KwicViewSettings()
        {
            LineMargin = 5;
            WordMargin = 0;
            ShowSegments = false;
            ShowLinks = false;
            ShowGroups = false;
            Background = "Ivory";
        }

        public KwicViewSettings(KwicViewSettings src)
            : base()
        {
            this.CopyFrom(src);
        }

        public KwicViewSettings Copy()
        {
            KwicViewSettings obj = new KwicViewSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(KwicViewSettings src)
        {
            this.ShowSegments = src.ShowSegments;
            this.ShowLinks = src.ShowLinks;
            this.ShowGroups = src.ShowGroups;
            this.LineMargin = src.LineMargin;
            this.WordMargin = src.WordMargin;
            this.DefaultHeaderWidthKwic = src.DefaultHeaderWidthKwic;
            this.DefaultHeaderWidthNonKwic = src.DefaultHeaderWidthNonKwic;
            this.Background = src.Background;
        }
    }
}
