using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ChaKi.Common.Settings
{
    public class ContextPanelSettings
    {
        public static ContextPanelSettings Current = new ContextPanelSettings();

        public bool UseSpacing { get; set; }
        public string Background { get; set; }

        private ContextPanelSettings()
        {
            this.UseSpacing = false;
            this.Background = "Wheat";
        }

        public ContextPanelSettings(ContextPanelSettings src)
            : base()
        {
            this.CopyFrom(src);
        }

        public ContextPanelSettings Copy()
        {
            ContextPanelSettings obj = new ContextPanelSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(ContextPanelSettings src)
        {
            this.UseSpacing = src.UseSpacing;
            this.Background = src.Background;
        }
    }
}
