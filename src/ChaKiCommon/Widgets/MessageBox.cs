using System;
using System.Windows.Forms;
using System.Drawing;

namespace ChaKi.Common.Widgets
{
    public partial class MessageBox : Form
    {
        public MessageBox()
        {
            InitializeComponent();
        }

        static public DialogResult Show(string msg)
        {
            return Show(msg, "Message", MessageBoxButtons.OK, MessageBoxIcon.None, null);
        }

        static public DialogResult Show(string msg, string title)
        {
            return Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.None, null);
        }

        static public DialogResult Show(string msg, string title, MessageBoxButtons buttons)
        {
            return Show(msg, title, buttons, MessageBoxIcon.None, null);
        }

        static public DialogResult Show(string msg, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return Show(msg, title, buttons, icon, null);
        }

        static public DialogResult Show(string msg, string title, MessageBoxButtons buttons,  MessageBoxIcon icon, Control parent)
        {
            DialogResult result = DialogResult.Cancel;
            MessageBox mbox = new MessageBox();
            mbox.pictureBox1.Image = Bitmap.FromHicon(SystemIcons.Information.Handle);
            switch (icon)
            {
                case MessageBoxIcon.Information:
                    mbox.pictureBox1.Image = Bitmap.FromHicon(SystemIcons.Information.Handle);
                    break;
                case MessageBoxIcon.Error:
                    mbox.pictureBox1.Image = Bitmap.FromHicon(SystemIcons.Error.Handle);
                    break;
                case MessageBoxIcon.Exclamation:
                    mbox.pictureBox1.Image = Bitmap.FromHicon(SystemIcons.Exclamation.Handle);
                    break;
                case MessageBoxIcon.Question:
                    mbox.pictureBox1.Image = Bitmap.FromHicon(SystemIcons.Question.Handle);
                    break;
            }
            mbox.pictureBox1.Image = Bitmap.FromHicon(SystemIcons.Information.Handle);
            mbox.richTextBox1.Text = msg;
            mbox.richTextBox1.Height = Math.Max(50, mbox.button1.Top - mbox.richTextBox1.Top - 5);  // HighDPI対応
            mbox.Text = title;
            mbox.richTextBox1.Select(0, 0);
            switch (buttons)
            {
                case MessageBoxButtons.OKCancel:
                    mbox.ActiveControl = mbox.button1;
                    break;
                case MessageBoxButtons.OK:
                    mbox.button1.Visible = true;
                    mbox.button2.Visible = false;
                    mbox.ActiveControl = mbox.button1;
                    break;
                case MessageBoxButtons.YesNo:
                    mbox.button1.Visible = false;
                    mbox.button2.Visible = false;
                    mbox.button3.Visible = true;
                    mbox.button4.Visible = true;
                    mbox.button3.Location = mbox.button1.Location;
                    mbox.button4.Location = mbox.button2.Location;
                    mbox.AcceptButton = mbox.button3;
                    mbox.ActiveControl = mbox.button3;
                    mbox.button3.Focus();
                    break;
                case MessageBoxButtons.YesNoCancel:
                    mbox.button1.Visible = false;
                    mbox.button2.Visible = true;
                    mbox.button3.Visible = true;
                    mbox.button4.Visible = true;
                    mbox.button3.Location = mbox.button4.Location;
                    mbox.button4.Location = mbox.button1.Location;
                    mbox.AcceptButton = mbox.button3;
                    mbox.ActiveControl = mbox.button3;
                    mbox.button3.Focus();
                    break;
                default:
                    throw new NotImplementedException("Invalid option for the MessageBox.");
                // Other options are invalid
            }
            if (parent != null)
            {
                Rectangle r = parent.Bounds;
                Point lefttop = new Point((r.Left + r.Right) / 2 - mbox.Width / 2, (r.Top + r.Bottom) / 2 - mbox.Height / 2);
                mbox.Location = lefttop;
            }
            else
            {
                try
                {
                    // MainFormを探す
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f.GetType().FullName == "ChaKi.MainForm")
                        {
                            Rectangle r = f.Bounds;
                            Point lefttop = new Point((r.Left + r.Right) / 2 - mbox.Width / 2, (r.Top + r.Bottom) / 2 - mbox.Height / 2);
                            mbox.Location = lefttop;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            result = mbox.ShowDialog();
            return result;
        }
    }
}
