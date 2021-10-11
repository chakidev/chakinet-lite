using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ChaKi.Service.DependencyEdit
{
    internal class History
    {
        private List<IOperation> m_History;
        // HistPointer = 0: ヒストリの先頭までロールバックしている状態
        //             = 1～m_Hisotry.Count-1: ヒストリの中間にいる(Undo/Redoどちらもできる）状態
        //             = m_History.Count: ヒストリの最後にいる（最新の）状態
        private int m_HistPointer;

        public History()
        {
            m_History = new List<IOperation>();
            m_HistPointer = 0;
        }

        public void Reset()
        {
            m_History.Clear();
            m_HistPointer = 0;
        }

        public IList<IOperation> GetCurrentOperationChain()
        {
            var ret = new List<IOperation>();
            for (int i = 0; i < m_HistPointer; i++)
            {
                ret.Add(m_History[i]);
            }
            return ret;
        }

        public void Record(IOperation op)
        {
            if (m_History.Count - m_HistPointer > 0)
            {
                m_History.RemoveRange(m_HistPointer, m_History.Count - m_HistPointer);
            }
            Console.WriteLine("History Record @{0}: {1}", m_History.Count, op);
            m_History.Add(op);
            m_HistPointer = m_History.Count ;
        }

        public IOperation Back()
        {
            if (m_HistPointer == 0 || m_HistPointer - 1 >= m_History.Count)
            {
                return null;
            }
            m_HistPointer--;
            return m_History[m_HistPointer];
        }

        public IOperation Forward()
        {
            if (m_HistPointer >= m_History.Count)
            {
                return null;
            }
            var op = m_History[m_HistPointer];
            m_HistPointer++;
            return op;
        }

        public bool CanUndo()
        {
            return (m_HistPointer > 0);
        }

        public bool CanRedo()
        {
            return (m_HistPointer < m_History.Count);
        }

        public bool CanSave()
        {
            return CanUndo();
        }
    }
}
