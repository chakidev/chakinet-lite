using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Collocation;
using ChaKi.Entity.Search;
using ChaKi.Entity.Kwic;
using System.Threading;

namespace ChaKi.Service.Collocation
{
    public class CollocationService : ICollocationService
    {
        private Counter m_Counter;

        private Thread m_Thread = null;
        private AsyncCallback m_Callback = null;
        public event ProgressEventHandler NotifyAsyncProgress;

        public CollocationService(KwicList src, CollocationList clist, SearchConditionsSequence condseq)
        {
            m_Counter = Counter.Create(src, clist, condseq.Last.CollCond);
        }

        public void Exec()
        {
            m_Counter.Count();
        }

        public void ExecAsync(AsyncCallback callback)
        {
            if (m_Thread == null)
            {
                m_Thread = new Thread(StartRoutine) { IsBackground = true };
            }
            if (m_Thread.IsAlive)
            {
                m_Thread.Abort();
            }
            m_Callback = callback;
            m_Thread.Start();
        }

        public void Abort()
        {
            if (m_Thread != null)
            {
                m_Thread.Abort();
            }
        }


        private void StartRoutine()
        {
            try
            {
                m_Counter.ProgressHandler = this.NotifyAsyncProgress;
                m_Counter.Count();
            }
            catch (ThreadAbortException)
            {
                ; // do nothing
            }
            catch (Exception ex)
            {
                if (m_Callback != null)
                {
                    m_Callback(new CollocationAsyncResult() { AsyncState = ex, IsCompleted = true });
                }
            }
            if (m_Callback != null)
            {
                m_Callback(new CollocationAsyncResult() { AsyncState = null, IsCompleted = true });
            }
        }
    }

    public class CollocationAsyncResult : IAsyncResult
    {
        public object AsyncState { get; set; }
        public WaitHandle AsyncWaitHandle { get; set; }
        public bool CompletedSynchronously { get; set; }
        public bool IsCompleted { get; set; }
    }
}
