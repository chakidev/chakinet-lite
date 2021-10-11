using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyEdit
{
    public class MergeSplitEventArgs : EventArgs
    {
        public MergeSplitEventArgs(int bunsetsuPos, int wordPos)
        {
            this.bunsetsuPos = bunsetsuPos;
            this.wordPos = wordPos;
        }

        public int bunsetsuPos;
        public int wordPos;
    }
}
