using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DependencyEditSLA.Widgets
{
    internal partial class ConcatButton : FunctionButton
    {
        public ConcatButton(int pos)
        {
            InitializeComponent();

            this.Pos = pos;
        }

        public int Pos { get; set; }
    }
}
