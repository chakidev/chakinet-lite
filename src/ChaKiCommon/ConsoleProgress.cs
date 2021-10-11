using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChaKi.Common
{
    public class ConsoleProgress : IProgress
    {
        private int m_CurrentPercentage;

        public ConsoleProgress()
        {
            this.ProgressMax = 100;
            this.ProgressCount = 0;
            m_CurrentPercentage = 0;
        }
        
        public int ProgressMax { private get; set; }

        public int ProgressCount
        {
            set
            {
                int newPercentage = (int)(value*100/this.ProgressMax);
                if (m_CurrentPercentage != newPercentage)
                {
                    Console.Error.Write("{0}%\r", newPercentage);
                    m_CurrentPercentage = newPercentage;
                }
            }
        }

        public void ProgressReset()
        {
            ProgressCount = 0;
        }

        public bool Canceled
        {
            get { return false; }
        }

        public void EndWork()
        {
            Console.Error.WriteLine();
        }

        public event EventHandler WorkerCancelled;
    }
}
