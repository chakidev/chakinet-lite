using System.Collections.Generic;
using System.Drawing;
using ChaKi.Entity.Settings;

namespace ChaKi.Common
{
    public class SegmentPens
    {
        public static Dictionary<string, Pen> Pens;
        public static Pen DefaultPen;

        static SegmentPens()
        {
            Pens = new Dictionary<string, Pen>();
            Pens.Add("Bunsetsu", new Pen(Color.Plum, 1.0f));
            Pens.Add("Apposition", new Pen(Color.LimeGreen, 2.5f));
            Pens.Add("Parallel", new Pen(Color.MediumVioletRed, 2.5f));
            Pens.Add("Nest", new Pen(Color.DarkBlue, 2.5f));

            DefaultPen = new Pen(Color.Black, 1.0f);
        }

        public static void Clear()
        {
            foreach (Pen p in Pens.Values)
            {
                p.Dispose();
            }
            Pens.Clear();
        }

        public static void AddPen(string key, Color color, float width)
        {
            Pens.Add(key, new Pen(color, width));
        }

        public static void AddPens(IDictionary<string, TagSettingItem> settings)
        {
            Clear();
            foreach (KeyValuePair<string, TagSettingItem> item in settings)
            {
                Color c = ColorTranslator.FromHtml(item.Value.Color);
                AddPen(item.Key, Color.FromArgb(item.Value.Alpha, c.R, c.G, c.B), item.Value.Width);
            }
        }

        public static Pen Find(string key)
        {
            Pen p;
            if (Pens.TryGetValue(key, out p)) return p;
            return DefaultPen;
        }
    }
}
