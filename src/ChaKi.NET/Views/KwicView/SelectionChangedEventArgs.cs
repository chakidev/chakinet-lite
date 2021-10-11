using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Views.KwicView
{
    public class SelectionChangedEventArgs : EventArgs
    {
        public SelectionChangedEventArgs(int lineno, int sentenceid)
        {
            this.NewLineNo = lineno;
            this.NewSentenceID = sentenceid;
        }

        public int NewLineNo { get; set; }

        public int NewSentenceID { get; set; }
    }
}
