using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Common;
using PopupControl;
using System.Drawing;

namespace DependencyEditSLA.Widgets
{
    public partial class LinkTagLabel : Label
    {
        private Popup popupMenu;

        public event EventHandler TagChanged;

        public Link Link { get; set; }

        public LinkTagLabel(Link link)
        {
            InitializeComponent();

            this.popupMenu = TagSelector.PreparedPopups[ChaKi.Entity.Corpora.Annotations.Tag.LINK];

            this.Link = link;
        }

        void selector_TagSelected(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (!(sender is TagSelector)) return;
                TagSelector selector = (TagSelector)sender;
                this.Text = selector.Selection.Name;
                if (TagChanged != null)
                {
                    TagChanged(this, null);
                }
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        public void DoEditTaglabel(Control sender, Point location)
        {
            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                this.popupMenu.Location = PointToScreen(location);
                TagSelector selector = (TagSelector)this.popupMenu.Content;
                selector.TagSelected += new EventHandler(selector_TagSelected);
                this.popupMenu.Closed += new ToolStripDropDownClosedEventHandler(popupMenu_Closed);
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
            this.popupMenu.Show(sender);
        }

        private void TagLabel_MouseUp(object sender, MouseEventArgs e)
        {
            DoEditTaglabel(sender as Control, e.Location);
        }

        void popupMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            if (!(sender is Popup)) return;
            TagSelector selector = (TagSelector)(((Popup)sender).Content);
            selector.TagSelected -= selector_TagSelected;
        }
    }
}
