using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;

namespace DependencyEdit
{
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
