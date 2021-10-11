using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.GUICommon
{
    public class RelationCommandEventArgs : EventArgs
    {
        public RelationCommandEventArgs(char c)
        {
            this.Command = c;
        }

        public char Command;
    }

    public delegate void RelationCommandEventHandler(Object sender, RelationCommandEventArgs e);

}
