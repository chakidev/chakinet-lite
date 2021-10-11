using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Common.Widgets;
using System.Drawing;

namespace ChaKi.Views.KwicView
{
    class KwicViewTransparentPanel : TransparentPanel
    {
        public bool ShowVerticalSplitter = false;
        public int VerticalSplitterPos = 0;

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            if (this.ShowVerticalSplitter)
            {
                var g = e.Graphics;
                g.DrawLine(Pens.Gray, VerticalSplitterPos, 0, VerticalSplitterPos, this.Height);
            }
        }
    }
}
