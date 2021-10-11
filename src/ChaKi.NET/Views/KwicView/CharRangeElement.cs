using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Views.KwicView
{
    internal class CharRangeElement
    {
        public CharPosElement Start;
        public CharPosElement End;

        public CharRangeElement()
        {
        }

        public CharRangeElement(CharPosElement s, CharPosElement e)
        {
            Start = s;
            End = e;
        }

        public void Clear()
        {
            Start = new CharPosElement();
            End = new CharPosElement();
        }

        public int Length
        {
            get { return End.CharID - Start.CharID; }
        }

        public bool Valid
        {
            get
            {
                return (Start.IsValid && End.IsValid);
            }
        }

        public CharRangeElement ToAscendingRange()
        {
            if (!Valid)
            {
                CharRangeElement erange = new CharRangeElement();
                return erange;
            }
            if (Start.WordID > End.WordID)
            {
                // Swap Start/End
                CharRangeElement rrange = new CharRangeElement(End, Start);
                return rrange;
            }
            else if (Start.WordID == End.WordID)
            {
                if (Start.CharInWord > End.CharInWord)
                {
                    // Swap Start/End
                    CharRangeElement rrange = new CharRangeElement(End, Start);
                    return rrange;
                }
            }
            CharRangeElement range = new CharRangeElement(Start, End);
            return range;
        }

        public bool IsEmpty
        {
            get
            {
                if (!Start.IsValid || !End.IsValid) return true;
                return (Start.WordID == End.WordID && Start.CharInWord == End.CharInWord);
            }
        }
    }
}
