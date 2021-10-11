using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Text2Corpus
{
    internal class ProgressEventArgs : EventArgs
    {
        public int Progress { get; private set; }

        public ProgressEventArgs(int progress)
        {
            this.Progress = progress;
        }
    }
}
