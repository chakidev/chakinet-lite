using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Common
{
    public static class ControlHelper
    {
        public static T FindAncestor<T>(Control root) where T:Control
        {
            if (root == null)
            {
                return null;
            }
            if (root.Parent is T)
            {
                return (T)(root.Parent);
            }
            return FindAncestor<T>(root.Parent);
        }
    }
}
