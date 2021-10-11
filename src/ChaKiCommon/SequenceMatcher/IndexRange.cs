using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Common.SequenceMatcher
{
    /// <summary>
    ///  Immutable Range Structure
    /// </summary>
    [Serializable]
    public struct IndexRange
    {
        public IndexRange(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }
        public IndexRange(IndexRange org)
        {
            this.Start = org.Start;
            this.End = org.End;
        }
        public IndexRange(int start)
        {
            this.Start = start;
            this.End = start + 1;
        }

        public int Start;
        public int End;

        public static IndexRange Invalid = new IndexRange(-1, -1);

        public IndexRange Offset(int offset, int diff = 0)
        {
            var s = this.Start + offset;
            var e = this.End + offset;
            e -= diff;
            return new IndexRange(Math.Min(s, e), this.End = Math.Max(s, e));
        }

        public override string ToString()
        {
            if (this.End == this.Start + 1 || (this.Start == -1 && this.End == -1))
            {
                return $"{this.Start}";
            }
            return $"[{this.Start},{this.End}]";
        }
    }
}
