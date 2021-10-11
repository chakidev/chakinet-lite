using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DependencyEdit
{
    public partial class ConcatButton : Button
    {
        public ConcatButton(int pos)
        {
            InitializeComponent();

            this.Pos = pos;
        }

        public int Pos { get; set; }
    }
}
