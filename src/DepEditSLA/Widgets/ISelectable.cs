using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaKi.Entity.Corpora.Annotations;
using System.Windows.Forms;

namespace DependencyEditSLA.Widgets
{
    internal interface ISelectable
    {
        void InvalidateElement(Control container);

        bool Selected { get; set; }

        Annotation Model { get; }
    }
}
