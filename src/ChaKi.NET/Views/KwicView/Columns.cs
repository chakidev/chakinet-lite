using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Common.Settings;

namespace ChaKi.Views.KwicView
{
    internal class Columns
    {
        public int[] Widths;

        public Columns()
        {
            this.Widths = new int[NColumns];
        }

        static Columns()
        {
            Default = new Columns();
            if (KwicView.Settings == null) KwicView.Settings = KwicViewSettings.Current;
            int[] warray = KwicView.Settings.DefaultHeaderWidthKwic;
            if (warray == null || warray.Length == 0 || warray.Length != NColumns)
            {
                KwicView.Settings.DefaultHeaderWidthKwic = new int[] { 37, 24, 80, 80, 44, 44, 250, 100, 250 };
            }
            warray = KwicView.Settings.DefaultHeaderWidthNonKwic;
            if (warray == null || warray.Length == 0 || warray.Length != NColumns)
            {
                KwicView.Settings.DefaultHeaderWidthNonKwic = new int[] { 37, 24, 80, 80, 44, 44, 0, 0, 600 };
            }

            Default.Widths = KwicView.Settings.DefaultHeaderWidthNonKwic;
        }

        public int GetSentenceOffset()
        {
            return Widths[0]+Widths[1]+Widths[2]+Widths[3]+Widths[4]+Widths[5];
        }

        public int GetSentenceEndOffset()
        {
            return Widths[0] + Widths[1] + Widths[2] + Widths[3] + Widths[4] + Widths[5] + Widths[6] + Widths[7] + Widths[8];
        }

        public int GetLeftWidth()
        {
            return this.Widths[6];
        }

        public int GetCenterOffset()
        {
            return Widths[0] + Widths[1] + Widths[2] + Widths[3] + Widths[4] + Widths[5] + Widths[6];
        }

        public int GetMaxSentenceWidth()
        {
            return this.Widths[6] + Widths[7] + Widths[8];
        }

        public bool IsInSentence(int x)
        {
            return (x >= GetSentenceOffset() && x < GetSentenceEndOffset());
        }

        public bool IsInCheckBox(int x)
        {
            if (x >= this.Widths[0])
            {
                if (x - this.Widths[0] < this.Widths[1])
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// centerize->5,7カラムの幅を調整することで全体がtotalWidth内に収まるようにする。
        /// !enterize->7カラムの幅を調整することで全体がtotalWidth内に収まるようにする。
        /// </summary>
        public void AutoAdjust(int totalWidth, bool centerize)
        {
            int rest = totalWidth;
            for (int i = 0; i < NColumns; i++)
            {
                if (i != 6 && i != 8)
                {
                    rest -= this.Widths[i];
                }
            }
            if (rest <= 0)
            {
                return;
            }
            if (centerize)
            {
                KwicView.Settings.DefaultHeaderWidthKwic[6] = KwicView.Settings.DefaultHeaderWidthKwic[8] = rest / 2;
            }
            else
            {
                KwicView.Settings.DefaultHeaderWidthNonKwic[8] = rest;
            }
        }

        #region Static definitions
        public static int NColumns = 9;
        public static Columns Default;

        public static string[] DefaultHeaderTextNonKwic =
        {
            "Index", "Check", "Corpus", "Doc", "Char", "Sen",
            "", "", "Text"
        };
        public static string[] DefaultHeaderTextKwic =
        {
            "Index", "Check", "Corpus", "Doc", "Char", "Sen",
            "Left", "Center", "Right"
        };
        #endregion
    }
}
