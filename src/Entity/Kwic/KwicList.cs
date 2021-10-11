using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Corpora;
using Wintellect.PowerCollections;

namespace ChaKi.Entity.Kwic
{
    public class KwicList
    {
        public List<KwicItem> Records { get; set; }
        public event UpdateKwicListEventHandler OnModelChanged;

        private int m_HilightPos;

        public KwicList()
        {
            Records = new List<KwicItem>();
            m_HilightPos = 0;
        }

        public void AddKwicItem(KwicItem item)
        {
            this.Records.Add(item);
            item.ID = this.Records.Count;

            // リスナにモデル更新を通知
            if (OnModelChanged != null) OnModelChanged(this, new UpdateKwicListEventArgs());
        }

        public void DeleteAll()
        {
            this.Records.Clear();
            if (OnModelChanged != null) OnModelChanged(this, new UpdateKwicListEventArgs(true,true,false));
        }

        public void Shift(int shift)
        {
            foreach (KwicItem k in this.Records)
            {
                k.Shift(shift);
            }
            if (OnModelChanged != null) OnModelChanged(this, new UpdateKwicListEventArgs(true,false,false));
        }

        public void SetHilight(int pos)
        {
            int oldHilightPos = m_HilightPos;
            m_HilightPos += pos;
            foreach (KwicItem k in this.Records)
            {
                k.SetHilight(oldHilightPos, false);
                k.SetHilight(m_HilightPos,true);
            }
            if (OnModelChanged != null) OnModelChanged(this, new UpdateKwicListEventArgs(true,false,false));
        }

        public void Sort(int col, bool isAscending)
        {
            KwicItemComparer cmp = new KwicItemComparer(col, isAscending);
            Algorithms.StableSortInPlace<KwicItem>(this.Records, cmp);
//            this.Records.Sort(cmp);   // Don't use unstable sort
            if (OnModelChanged != null) OnModelChanged(this, new UpdateKwicListEventArgs(true,false,true));
        }

        public void AlterCheckState(int index)
        {
            this.Records[index].Checked = !this.Records[index].Checked;
        }

        public List<int> MakeSentenceIDListOfCorpus(Corpus c)
        {
            List<int> res = new List<int>();
            foreach (KwicItem k in this.Records)
            {
                if (c.Name == k.Crps.Name && !res.Contains(k.SenID))
                {
                    res.Add(k.SenID);
                }
            }
            res.Sort();
            return res;
        }
    }
}
