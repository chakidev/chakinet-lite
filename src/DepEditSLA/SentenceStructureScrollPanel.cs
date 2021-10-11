using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DependencyEditSLA
{
    public partial class SentenceStructureScrollPanel : UserControl
    {
        public SentenceStructureScrollPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// スクロール位置がScrollableControlによって勝手に調整されるのを防ぐ.
        /// cf. http://social.msdn.microsoft.com/Forums/ja-JP/winforms/thread/285b1a48-ce21-47ea-80bf-5601d6014cf7
        /// </summary>
        /// <param name="activeControl"></param>
        /// <returns></returns>
        protected override Point ScrollToControl(Control activeControl)
        {
            return this.AutoScrollPosition;
            //            return base.ScrollToControl(activeControl);
        }
    }
}
