using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity.Search
{
    public class CommandProgress
    {
        public List<CommandProgressItem> Items { get; set; }
        public event EventHandler OnModelChanged;

        private CommandProgressItem m_Current;
        public CommandProgressItem Current
        {
            get
            {
                return m_Current;
            }
        }
        private CommandProgressItem m_Total;
        public CommandProgressItem Total
        {
            get
            {
                return m_Total;
            }
        }

        public CommandProgress()
        {
            Items = new List<CommandProgressItem>();
            m_Total = new CommandProgressItem();
            m_Total.Title = "TOTAL";
            m_Total.Row = 0;
            Reset();
        }


        public void Reset()
        {
            Items.Clear();
            m_Total.Reset();
            Items.Add(m_Total);
            m_Current = null;
            if (OnModelChanged != null) OnModelChanged(this, null);
        }

        public void StartOnItem(string name, int nc, int nd, int nhit)
        {
            m_Current = new CommandProgressItem();
            m_Current.Row = Items.Count - 1;
            m_Current.Title = name;
            m_Current.Nc = nc;
            m_Current.Nd = nd;
            m_Current.Nhit = nhit;
            Items.Insert(Items.Count - 1, m_Current);
            RecalcTotal();
            m_Total.Row++;
            if (OnModelChanged != null) OnModelChanged(this, null);
        }

        public void SetRange(int range)
        {
            if (m_Current != null)
            {
                m_Current.NhitIsUnknown = false;
                m_Current.Nhit = range;
                m_Total.NhitIsUnknown = false;
                m_Total.Nhit += range;
                if (OnModelChanged != null) OnModelChanged(this, null);
            }
        }

        /// <summary>
        /// カレントのヒット数を後から書き換える
        /// </summary>
        /// <param name="range"></param>
         public void AlterRange(int range)
        {
            if (m_Current != null)
            {
                m_Total.Nhit -= m_Current.Nhit;
                m_Current.Nhit = range;
                m_Total.Nhit += range;
                if (OnModelChanged != null) OnModelChanged(this, null);
            }
        }


        public void Increment()
        {
            if (m_Current != null)
            {
                m_Current.Increment();
                m_Total.Increment();
                if (OnModelChanged != null) OnModelChanged(this, null);
            }
        }

        private void RecalcTotal()
        {
            m_Total.Nc = 0;
            m_Total.Nhit = 0;
            m_Total.Nret = 0;
            for (int i = 0; i < Items.Count - 1; i++)
            {
                m_Total.Nc += Items[i].Nc;
                m_Total.Nhit += Items[i].Nhit;
                m_Total.Nret += Items[i].Nret;
            }
        }
    }
}
