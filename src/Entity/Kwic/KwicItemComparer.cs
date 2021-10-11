using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Kwic
{
    public class KwicItemComparer : IComparer<KwicItem>
    {
        private int col;
        private bool isAscending;

        public KwicItemComparer(int col, bool isAscending)
        {
            this.col = col;
            this.isAscending = isAscending;
        }

        public int Compare(KwicItem x, KwicItem y)
        {
//            "Index", "Check", "Corpus", "Document", "Char", "Sen",
//            "Left", "Center", "Right", "Bib" };
            int res = 0;

            switch (col)
            {
                case 0: // by Index
                    res = (x.ID - y.ID);
                    break;
                case 1: // by Check
                    if (x.Checked && !y.Checked) {
                        res = -1;
                    }
                    else if (!x.Checked && y.Checked)
                    {
                        res = 1;
                    }
                    break;
                case 2:
                    res = string.CompareOrdinal(x.Crps.Name, y.Crps.Name);
                    break;
                case 3:
                    res = (x.Document.ID - y.Document.ID);
                    break;
                case 4:
                    res = (x.StartCharPos - y.StartCharPos);
                    break;
                case 5:
                    res = (x.SenID - y.SenID);
                    break;
                case 6:
                    res = KwicPortion.Compare(x.Left, y.Left, true);
                    break;
                case 7:
                    res = KwicPortion.Compare(x.Center, y.Center, false);
                    break;
                case 8:
                    res = KwicPortion.Compare(x.Right, y.Right, false);
                    break;
                case 9:
                    res = 0;
                    break;
            }
            if (!isAscending)
            {
                return -res;
            }
            return res;
        }
    }
}
