using System.Collections.Generic;
using System.Drawing;
using ChaKi.Entity.Settings;
using System.Drawing.Drawing2D;

namespace ChaKi.Common
{
    public class LinkPens
    {
        public static CustomLineCap CustomArrowCap;
        public static Dictionary<string, Pen> Pens;
        public static Pen DefaultPen;

        static LinkPens()
        {
            // 大き目の矢印LineCap
            // Bezier曲線で使うと矢じりの中心が曲線とずれる.
            CustomArrowCap = new AdjustableArrowCap(5, 8);

            Pens = new Dictionary<string, Pen>();
            Pen p = new Pen(Color.Gray, 1.0f);
            p.SetLineCap(LineCap.Square, LineCap.Custom, DashCap.Flat);
            p.CustomEndCap = CustomArrowCap;
            Pens.Add("A", p);
            p = new Pen(Color.Gray, 1.0f);
            p.SetLineCap(LineCap.Square, LineCap.Custom, DashCap.Flat);
            p.CustomEndCap = CustomArrowCap;
            Pens.Add("O", new Pen(Color.Gray, 1.0f));
            p = new Pen(Color.Gray, 1.0f);
            p.SetLineCap(LineCap.Square, LineCap.Custom, DashCap.Flat);
            p.CustomEndCap = CustomArrowCap;
            Pens.Add("D", new Pen(Color.Gray, 1.0f));

            DefaultPen = new Pen(Color.Gray, 1.0f);
            DefaultPen.SetLineCap(LineCap.Square, LineCap.Custom, DashCap.Flat);
            DefaultPen.CustomEndCap = CustomArrowCap;
        }

        public static void Clear()
        {
            foreach (Pen p in Pens.Values)
            {
                p.Dispose();
            }
            Pens.Clear();
        }

        public static void AddPen(string key, Color color, float width, bool isDirected)
        {
            Pen p = new Pen(color, width);
            if (isDirected)
            {
                p.SetLineCap(LineCap.Square, LineCap.Custom, DashCap.Flat);
                p.CustomEndCap = CustomArrowCap;
            }
            else
            {
                p.SetLineCap(LineCap.Square, LineCap.Square, DashCap.Flat);
            }
            Pens.Add(key, p);
        }

        public static void AddPens(IDictionary<string, TagSettingItem> settings)
        {
            Clear();
            foreach (KeyValuePair<string, TagSettingItem> item in settings)
            {
                Color c = ColorTranslator.FromHtml(item.Value.Color);
                AddPen(item.Key, Color.FromArgb(item.Value.Alpha, c.R, c.G, c.B), item.Value.Width, true); //Todo: 本来はIsDirected判定が必要.
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
