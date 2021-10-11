using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Common.Settings
{
    public class CollocationViewSettings
    {
        public SerializableDictionary<string, int> ColWidthDictionary;

        public static CollocationViewSettings Current = new CollocationViewSettings();

        private CollocationViewSettings()
        {
            this.ColWidthDictionary = new SerializableDictionary<string, int>();
        }

        public CollocationViewSettings Copy()
        {
            CollocationViewSettings obj = new CollocationViewSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(CollocationViewSettings src)
        {
            this.ColWidthDictionary.Clear();
            foreach (KeyValuePair<string, int> item in src.ColWidthDictionary)
            {
                this.ColWidthDictionary.Add(item.Key, item.Value);
            }
        }

        public void FromGrid(DataGridView grid)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (!col.Visible) continue;
                this.ColWidthDictionary[col.Name] = col.Width;
            }
        }

        public void ApplyToGrid(DataGridView grid)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (!col.Visible) continue;

                int width;
                if (this.ColWidthDictionary.TryGetValue(col.Name, out width))
                {
                    col.Width = width;
                }
            }
        }
    }
}
