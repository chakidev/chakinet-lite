using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Common
{
    public class TagChangedEventArgs : EventArgs
    {
        public TagChangedEventArgs(string text)
        {
            this.Text = text;
        }

        public string Text { get; private set; }
    }
}
