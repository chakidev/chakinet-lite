
using System;
namespace ChaKi.Entity.Corpora
{
    public class CharRange
    {
        public int? Start { get; set; }
        public int? End { get; set; }

        public static CharRange Empty = new CharRange();

        public CharRange()
        {
            this.Start = null;
            this.End = null;
        }

        public CharRange(int? s, int? e)
        {
            this.Start = s;
            this.End = e;
        }

        public void Clear()
        {
            this.Start = null;
            this.End = null;
        }

        public bool IsSameStartEnd(CharRange y)
        {
            if (y == null) return false;
            if (!this.Start.HasValue && !this.End.HasValue && !y.Start.HasValue && !y.End.HasValue) return true;
            if (!this.Start.HasValue || !this.End.HasValue) return false;
            if (!y.Start.HasValue || !y.End.HasValue) return false;

            return (this.Start.Value == y.Start.Value && this.End.Value == y.End.Value);
        }

        public int Length
        {
            get { return (int)End - (int)Start; }
        }

        public bool Valid
        {
            get
            {
                return (Start != null && End != null);
            }
        }

        public CharRange ToAscendingRange()
        {
            if (!Valid)
            {
                CharRange erange = new CharRange();
                return erange;
            }
            if (this.End < this.Start) {
                // Swap Start/End
                return new CharRange(this.End, this.Start);
            }
            CharRange range = new CharRange(this.Start, this.End);
            return range;
        }

        public bool IsEmpty
        {
            get
            {
                if (this.Start == null || this.End == null) return true;
                if (this.Length == 0) return true;
                return false;
            }
        }

        public void Union(CharRange r)
        {
            if (!r.Start.HasValue || !r.End.HasValue) return;
            this.Start = (this.Start.HasValue) ? Math.Min(this.Start.Value, r.Start.Value) : r.Start;
            this.End = (this.End.HasValue) ? Math.Max(this.End.Value, r.End.Value) : r.End;
        }

        public bool IntersectsWith(CharRange r)
        {
            if (!this.Start.HasValue || !this.End.HasValue) return false;
            if (!r.Start.HasValue || !r.End.HasValue) return false;

            if (this.End.Value <= r.Start.Value) return false;
            if (r.End.Value <= this.Start.Value) return false;

            return true;
        }

        public bool Includes(CharRange r)
        {
            if (!this.Start.HasValue || !this.End.HasValue) return false;
            if (!r.Start.HasValue || !r.End.HasValue) return false;

            return (this.Start.Value <= r.Start.Value
             && r.End.Value <= this.End.Value);
        }

        public override string ToString()
        {
            return string.Format("CharRange[{0}:{1}]", this.Start, this.End);
        }
    }
}
