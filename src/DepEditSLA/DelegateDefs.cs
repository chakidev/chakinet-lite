using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;

namespace DependencyEditSLA
{
    public delegate void MergeSplitEventHandler(object sender, MergeSplitEventArgs args);

    public class MergeSplitEventArgs : EventArgs
    {
        public MergeSplitEventArgs(int docid, int spos, int epos, int mpos)
        {
            DocID = docid;
            StartPos = spos;
            EndPos = epos;
            SplitPos = mpos;
        }

        public int DocID;
        public int StartPos;
        public int EndPos;
        public int SplitPos;
    }
    
    public delegate void ChangeLexemeEventHandler(object sender, ChangeLexemeEventArgs args);

    public class ChangeLexemeEventArgs : EventArgs
    {
        public ChangeLexemeEventArgs(int wpos, Lexeme newlex)
        {
            this.wordPos = wpos;
            this.newLex = newlex;
        }

        public int wordPos;
        public Lexeme newLex;
    }
}
