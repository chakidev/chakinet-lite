using System;
using System.Collections.Generic;

namespace ChaKi.Views.KwicView
{
    internal class Selection
    {
        private bool m_RangeSelection;  // Start-Endの指定ならtrue, List指定ならfalse
        public int Start;
        public int End;
        public List<int> Selections;

        public Selection()
        {
            this.Selections = new List<int>();
            Clear();
        }

        public void Clear()
        {
            m_RangeSelection = false;
            this.Selections.Clear();
            this.Start = this.End = -1;
        }

        public void SetRange(int start, int end)
        {
            this.Start = start;
            this.End = end;
            m_RangeSelection = true;
        }

        public void AddSelection(int index)
        {
            if (m_RangeSelection)
            {
                m_RangeSelection = false;
                this.Selections.Clear();
                for (int i = this.Start; i <= this.End; i++)
                {
                    this.Selections.Add(i);
                }
            }
            if (this.Selections.Contains(index))
            {
                this.Selections.Remove(index);
            }
            else
            {
                this.Selections.Add(index);
            }
        }

        public void RemoveSelection(int index)
        {
            if (m_RangeSelection)
            {
                throw new InvalidOperationException("Removing item from Range Selection");
            }
            this.Selections.Remove(index);
        }

        // indexまでSelectionを前後に拡張する（Shiftキー操作）
        public void ExtendSelectionTo(int index)
        {
            if (m_RangeSelection)
            {
                this.End = index;
            }
            else
            {
                if (this.Selections.Count == 0)
                {
                    this.Selections.Add(index);
                    return;
                }
                m_RangeSelection = true;
                this.Start = this.Selections[this.Selections.Count-1];
                this.End = index;
            }
        }

        public bool IsValid()
        {
            if (m_RangeSelection)
            {
                return (End >= 0 && Start >= 0);
            }
            return (Selections.Count > 0);
        }

        public bool Contains(int index)
        {
            if (m_RangeSelection)
            {
                if (Start <= End)
                {
                    return (index >= Start && index <= End);
                }
                else
                {
                    return (index >= End && index <= Start);
                }
            }
            return Selections.Contains(index);
        }

        /// <summary>
        /// 単一の要素が選択されている場合、その要素を返す。
        /// 選択がないか、複数要素が選択されている場合は-1を返す。
        /// 範囲指定の場合、範囲がStart==Endであっても-1を返す。
        /// </summary>
        /// <returns></returns>
        public int GetSingleSelection()
        {
            if (m_RangeSelection || Selections.Count != 1)
            {
                return -1;
            }
            return Selections[0];
        }

        public int Move(int minpos, int maxpos, int offset, bool bExtending)
        {
            if (!bExtending)
            {
                int i = GetSingleSelection();
                if (i >= 0)
                {
                    this.Selections[0] = Math.Max(minpos, Math.Min(maxpos, i + offset));
                    return this.Selections[0];
                }
            }
            else
            {
                if (!m_RangeSelection)
                {
                    if (Selections.Count == 0)
                    {
                        return -1;
                    }
                    m_RangeSelection = true;
                    this.Start = this.End = this.Selections[this.Selections.Count - 1];
                }
                this.End = Math.Max(minpos, Math.Min(maxpos, this.End + offset));
                return this.End;
            }
            return -1;
        }

        public int Forward(int maxpos)
        {
            int i = GetSingleSelection();
            if (i >= 0)
            {
                this.Selections[0] = Math.Min(maxpos, i + 1);
                return this.Selections[0];
            }
            return -1;
        }

        public int Backward(int minpos)
        {
            int i = GetSingleSelection();
            if (i >= 0)
            {
                this.Selections[0] = Math.Max(i - 1, minpos);
                return this.Selections[0];
            }
            return -1;
        }
    }
}
