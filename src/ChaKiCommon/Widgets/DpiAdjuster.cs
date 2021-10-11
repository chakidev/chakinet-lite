using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Common.Widgets
{
    public class DpiAdjuster
    {
        private SizeF? m_Scale = null;
        private Action<double, double> m_AdjustAction;

        public DpiAdjuster(Action<double, double> adjustAction)
        {
            m_AdjustAction = adjustAction;
        }

        public void Adjust(Graphics g)
        {
            if (m_Scale == null)
            {
                var scale = new SizeF(g.DpiX / 96.0F, g.DpiY / 96.0F);
                var xscale = scale.Width;
                var yscale = scale.Height;
                if (xscale > 0.5f && xscale < 6.0f && yscale > 0.5f && yscale < 6.0f)
                {
                    m_AdjustAction?.Invoke(xscale, yscale);
                }
                m_Scale = scale;
            }
        }
    }
}
