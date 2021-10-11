using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Common.Widgets
{
    public partial class TransparentInputForm : Form
    {
        /// <summary>
        /// ���̓����w�i�̏�ɏ悹��Dialog(Form)
        /// </summary>
        private PropertyInputDialog popup;
        private PropertyTreeSelectionDialog popup_tree;
        private PropertyListSelectionDialog popup_list;

        /// <summary>
        /// ���̓����w�i�̐eControl
        /// </summary>
        private Control returnFocusTo = null;

        public TransparentInputForm()
        {
            InitializeComponent();

            popup = new PropertyInputDialog();
            popup_tree = new PropertyTreeSelectionDialog();
            popup_list = new PropertyListSelectionDialog();

            popup_tree.OnSelectionChanged += this.PopupTreeSelectionChangedHandler;
            popup_list.OnSelectionChanged += this.PopupListSelectionChangedHandler;

            AddOwnedForm(popup);
            AddOwnedForm(popup_tree);
            AddOwnedForm(popup_list);
        }

        private void TransparentBackForm_Click(object sender, EventArgs e)
        {
            popup.Done();
        }

        public PropertyInputDialog Popup
        {
            get { return popup; }
        }

        public PropertyTreeSelectionDialog TreePopup
        {
            get { return popup_tree; }
        }

        public PropertyListSelectionDialog ListPopup
        {
            get { return popup_list; }
        }

        public bool HasTreeSelection
        {
            get { return this.hasTreeSelection; }
            set
            {
                if ((this.hasTreeSelection = value) == true)
                {
                    this.HasListSelection = false;
                }
            }
        }
        private bool hasTreeSelection;

        public bool HasListSelection
        {
            get { return this.hasListSelection; }
            set
            {
                if ((this.hasListSelection = value) == true)
                {
                    this.HasTreeSelection = false;
                }
            }
        }
        private bool hasListSelection;

        public Control ReturnFocusTo
        {
            get { return returnFocusTo; }
            set { returnFocusTo = value; }
        }

        public void PopupTreeSelectionChangedHandler( object obj, EventArgs args )
        {
            string s = this.popup_tree.Selection;
            this.popup.EditText = s;
            if (s.IndexOf('*') >= 0)
            {
                // ���K�\��ON
                this.popup.IsRegEx = true;
            }
            else
            {
                this.popup.IsRegEx = false;
            }
        }

        public void PopupListSelectionChangedHandler(object obj, EventArgs args)
        {
            string s = this.popup_list.Selection;
            this.popup.EditText += s;
            if (s.IndexOf('*') >= 0)
            {
                // ���K�\��ON
                this.popup.IsRegEx = true;
            }
            else
            {
                this.popup.IsRegEx = false;
            }
        }

        public Point PopupLocation
        {
            set
            {
                Point p = value;
                popup.Location = p;
                popup.Show();
                p.Offset(0, popup.Height);
                popup_tree.Width = popup.Width;
                popup_tree.Location = p;
                popup_list.Location = p;
            }
        }

        public void ResetScrollState()
        {
            this.popup_list.ResetScrollState();
            this.popup_tree.ResetScrollState();
        }


        /// <summary>
        /// popup�������ɂ́A���̓����w�i��Visible=false�ɂ���B
        /// ���̌��ʂ��̃n���h�����Ă΂��popup��������邱�ƂɂȂ�B
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TransparentBackForm_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                if (popup.IsDisposed)
                {
                    popup = new PropertyInputDialog();
                    popup_tree = new PropertyTreeSelectionDialog();
                    popup_list = new PropertyListSelectionDialog();
                    AddOwnedForm(popup);
                    AddOwnedForm(popup_tree);
                    AddOwnedForm(popup_list);
                }
                popup.Show();
                if (HasTreeSelection)
                {
                    popup_tree.Show();
                }
                if (HasListSelection)
                {
                    popup_list.Show();
                }
                popup.Focus();
            }
            else
            {
                popup.Hide();
                popup_tree.Hide();
                popup_list.Hide();
                if (returnFocusTo != null)
                {
                    returnFocusTo.Focus(); // �|�b�v�A�b�v���B���ꂽ����Focus�������̂ŁA�eForm��Focus���ړ�������B
                }
            }
        }

    }
}