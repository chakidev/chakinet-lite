using System;
using System.Collections.Generic;
using System.Text;
using ChaKi.Entity.Kwic;

namespace ChaKi.Views.KwicView
{
    internal class LineElementComparer : IComparer<LineElement>
    {
        private List<KwicItem> m_Source;

        public LineElementComparer(List<KwicItem> source)
        {
            m_Source = source;
        }

        public int Compare(LineElement x, LineElement y)
        {
            //TODO: IndexOfでは遅いので要改善
            int i1 = m_Source.IndexOf(x.KwicItem);
            int i2 = m_Source.IndexOf(y.KwicItem);
            return (i1-i2);
        }
    }
}
