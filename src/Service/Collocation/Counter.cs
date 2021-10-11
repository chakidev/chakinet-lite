using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Search;
using ChaKi.Entity.Collocation;
using ChaKi.Entity.Kwic;

namespace ChaKi.Service.Collocation
{
    internal abstract class Counter
    {
        protected KwicList m_Src;
        protected CollocationList m_Clist;
        protected CollocationCondition m_Cond;
        public ProgressEventHandler ProgressHandler;

        public static Counter Create(KwicList src, CollocationList result, CollocationCondition cond)
        {
            Counter obj = null;

            switch (cond.CollType)
            {
                case CollocationType.Raw:
                    obj = new CounterRaw(src, result, cond);
                    break;
                case CollocationType.MI:
                    obj = new CounterMI(src, result, cond);
                    break;
                case CollocationType.NgramL:
                    obj = new CounterNgram(src, result, cond, NgramType.Left);
                    break;
                case CollocationType.NgramR:
                    obj = new CounterNgram(src, result, cond, NgramType.Right);
                    break;
                case CollocationType.FSM:
                    obj = new CounterFSM(src, result, cond);
                    break;
            }
            return obj;
        }

        public Counter(KwicList src, CollocationList result, CollocationCondition cond)
        {
            m_Src = src;
            m_Clist = result;
            m_Cond = cond;
            this.ProgressHandler = null;
        }

        public abstract void Count();
    }
}
