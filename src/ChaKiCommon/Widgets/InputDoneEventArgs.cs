using System;
using System.Windows.Forms;

namespace ChaKi.Common
{
    public delegate void PropertyInputDone(object sender, InputDoneEventArgs e);
    public delegate void PropertyInputCancel(object sender, KeyPressEventArgs e);
    public delegate void PropertyBoxCenterized(object sender, EventArgs e);

    public class InputDoneEventArgs
    {
        public InputDoneEventArgs()
        {
            Index = -1;
            Text = "";
            IsRegExp = false;
            IsCaseSensitive = false;
        }
        public InputDoneEventArgs( int index, string text, bool isRegExp, bool isCaseSensitive )
        {
            this.Index = index;
            this.Text = text;
            this.IsRegExp = isRegExp;
            this.IsCaseSensitive = isCaseSensitive;
        }

        public int Index { get; set; }
        public string Text { get; set; }
        public bool IsRegExp { get; set; }
        public bool IsCaseSensitive { get; set; }
    }
}
