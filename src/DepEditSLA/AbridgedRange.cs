using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora.Annotations;

namespace DependencyEditSLA
{
    /// <summary>
    /// 縮退表示領域を示す。Document内の絶対文字位置を使用する。
    /// </summary>
    internal struct AbridgedRange
    {
        public int Start;   //Document内の絶対文字位置
        public int End;     //Document内の絶対文字位置

        public static AbridgedRange Empty = new AbridgedRange(-1, -1);

        public AbridgedRange(int s, int e) { this.Start = s; this.End = e; }
        public int Length { get { return End - Start; } }
        public bool IsEmpty { get { return (this.Start == -1 && this.End == -1); } }
        public bool SegmentInRange(Segment seg) { return (seg.StartChar >= this.Start && seg.EndChar <= End); }
        public bool Intersects(int spos, int epos) { return (this.End > spos && this.Start < epos); }
        public bool Includes(int spos, int epos) { return (this.Start <= spos && epos <= this.End); }
        public void Union(int spos, int epos)
        {
            if (this.IsEmpty) { this.Start = spos; this.End = epos; }
            else { this.Start = Math.Min(this.Start, spos); this.End = Math.Max(this.End, epos); }
        }
    }
}
