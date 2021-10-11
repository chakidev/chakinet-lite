using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    /// <summary>
    ///  Immutable Range Structure
    /// </summary>
    [Serializable]
    public struct Range
    {
        public Range(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }
        public Range(Range org)
        {
            this.Start = org.Start;
            this.End = org.End;
        }

        public int Start;
        public int End;

        public Range Offset(int offset, int diff = 0)
        {
            var s = this.Start + offset;
            var e = this.End + offset;
            e -= diff;
            return new Range(Math.Min(s, e), this.End = Math.Max(s, e));
        }

        public override string ToString()
        {
            return string.Format("({0} - {1}", this.Start, this.End);
        }
    }
}
