using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Text2Corpus
{
    internal class ThreadExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }

        public ThreadExceptionEventArgs(Exception ex)
        {
            this.Exception = ex;
        }
    }
}
