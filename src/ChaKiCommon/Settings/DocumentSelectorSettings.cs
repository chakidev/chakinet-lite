using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ChaKi.Common.Settings
{
    public class DocumentSelectorSettings : ICloneable
    {
        public static DocumentSelectorSettings Current = new DocumentSelectorSettings();
        
        public Size Size { get; set; }

        private DocumentSelectorSettings()
        {
            Size = new Size(300, 100);
        }

        public DocumentSelectorSettings Copy()
        {
            var obj = new DocumentSelectorSettings();
            obj.CopyFrom(this);
            return obj;
        }

        public void CopyFrom(DocumentSelectorSettings src)
        {
            this.Size = src.Size;
        }

        public object Clone()
        {
            var obj = new DocumentSelectorSettings();
            obj.CopyFrom(this);
            return obj;
        }
    }
}
