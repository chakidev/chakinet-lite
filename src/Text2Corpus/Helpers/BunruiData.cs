using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Common.Settings;

namespace ChaKi.Text2Corpus.Helpers
{
    class BunruiData
    {
        public string Bunrui { get; set; }

        public string Label { get; set; }

        public string No { get; set; }

        public BunruiData(string bunrui, string label, string no)
        {
            this.Bunrui = bunrui;
            this.Label = label;
            this.No = no;
        }

        public override string ToString()
        {
            return $"{this.Bunrui},{this.Label},{this.No}";
        }

        public string Format(BunruiOutputFormat bunruiOutputFormat)
        {
            switch (bunruiOutputFormat)
            {
                case BunruiOutputFormat.None:
                    return string.Empty;
                case BunruiOutputFormat.Id:
                    return this.Bunrui;
                case BunruiOutputFormat.Label:
                    return this.Label;
            }
            return string.Empty;
        }
    }
}
