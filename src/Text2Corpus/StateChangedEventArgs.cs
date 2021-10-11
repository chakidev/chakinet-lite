using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Text2Corpus
{
    internal class StateChangedEventArgs : EventArgs
    {
        public string State { get; private set; }

        public StateChangedEventArgs(string state)
        {
            this.State = state;
        }
    }
}
