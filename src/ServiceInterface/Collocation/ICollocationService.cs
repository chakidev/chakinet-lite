using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Service.Collocation
{
    public interface ICollocationService
    {
        void Exec();
        void ExecAsync(AsyncCallback callback);
        void Abort();

        event ProgressEventHandler NotifyAsyncProgress;
    }

    public delegate void ProgressEventHandler(object src, ProgressEventArgs args);

    public class ProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }
    }
}
