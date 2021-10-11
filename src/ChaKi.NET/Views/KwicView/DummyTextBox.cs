using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Views.KwicView
{
    public partial class DummyTextBox : TextBox
    {
        public event KeyEventHandler GotKey;

        public DummyTextBox()
        {
            InitializeComponent();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (this.GotKey != null)
            {
                GotKey(this, new KeyEventArgs(keyData));
            }
            return true;
        }
    }
}
