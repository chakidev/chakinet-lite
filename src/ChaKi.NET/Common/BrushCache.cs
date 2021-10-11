using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ChaKi.GUICommon
{
    internal class BrushCache
    {
        public static BrushCache Instance { get; set; }
        private Dictionary<uint, Brush> m_Repository;
        private int m_Capacity;

        static BrushCache()
        {
            Instance = new BrushCache(50);
        }

        private BrushCache(int capacity)
        {
            m_Repository = new Dictionary<uint, Brush>(capacity);
            m_Capacity = capacity;
        }

        public Brush Get(int argb)
        {
            return Get((uint)argb);
        }

        public Brush Get(uint argb)
        {
            Brush br;
            if (m_Repository.TryGetValue(argb, out br))
            {
                return br;
            }
            br = new SolidBrush(Color.FromArgb((int)argb));
            CheckVacancy();
            m_Repository[argb] = br;
            return br;
        }

        private void CheckVacancy()
        {
            if (m_Repository.Count < m_Capacity)
            {
                return;
            }
            foreach (KeyValuePair<uint, Brush> pair in m_Repository)
            {
                pair.Value.Dispose();
                m_Repository.Remove(pair.Key);
                break;
            }
        }
    }
}
