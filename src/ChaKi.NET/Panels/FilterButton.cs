using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Properties;
using System.Resources;

namespace ChaKi.Panels
{
    internal partial class FilterButton : ToolStripButton
    {
        public bool IsOn {
            get
            {
                return m_IsOn;
            }
            set
            {
                m_IsOn = value;
                if (IsOn)
                {
                    this.Image = FilterOn;
                }
                else
                {
                    this.Image = FilterOff;
                }
            }
        } private bool m_IsOn;
    }
}
