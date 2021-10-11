using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Text2Corpus
{
    internal class DetailedLogEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public DetailedLogEventArgs(string message)
        {
            this.Message = message;
        }
    }
}
