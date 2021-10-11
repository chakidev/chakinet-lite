using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Views
{
    public partial class FullScreenContainer : Form
    {
        private Action<Form> m_Callback;

        public FullScreenContainer(Action<Form> callback)
        {
            InitializeComponent();

            m_Callback = callback;
        }

        public void DoHide()
        {
            if (m_Callback != null)
            {
                m_Callback(this);
            }
            Hide();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DoHide();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
