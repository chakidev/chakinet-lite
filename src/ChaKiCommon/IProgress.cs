using System;

namespace ChaKi.Common
{
    public interface IProgress
    {
        int ProgressMax { set; }
        int ProgressCount { set; }
        void ProgressReset();

        bool Canceled { get; }
        void EndWork();

        event EventHandler WorkerCancelled;
    }
}
