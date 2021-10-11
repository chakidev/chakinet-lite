using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public class CommandProgressItem
    {
        public CommandProgressItem()
        {
            Reset();
            Title = "";
        }

        public void Reset()
        {
            NhitIsUnknown = true;
            Row = 0;
            Nc = 0;
            Nd = 0;
            Nret = 0;
            Nhit = 0;
        }

        public bool NhitIsUnknown { get; set; } // Nhitがまだセットされておらず、未知の状態(Nhit欄, Nret欄に"--"を表示）
        public int Row { get; set; }
        public string Title { get; set; }
        public int Nc { get; set; }
        public int Nd { get; set; }
        public int Nret { get; set; }
        public int Nhit { get; set; }
        public double NhitP
        {
            get
            {
                return 100.0 * Nhit / Nc;
            }
        }
        public double NretP
        {
            get
            {
                return 100.0 * Nret / Nhit;
            }
        }

        public void Increment()
        {
            Nret++;
        }
    }
}
