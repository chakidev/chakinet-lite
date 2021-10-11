using System;
using System.Drawing;
using System.Windows.Forms;
using ChaKi.Common;

namespace ChaKi.Views.KwicView
{
    public class Caret
    {
        private IntPtr m_OwnerHandle;
        private bool m_Visible;

        public int Height { get; set; }

        public Caret(Control parent)
        {
            m_OwnerHandle = parent.Handle;
            this.Height = 0;
        }

        public void Create(int height)
        {
            NativeFunctions.CreateCaret(m_OwnerHandle, IntPtr.Zero, 1, height);
            this.Location = new Point(-100,-100);
            this.Visible = true;
            this.Height = height;
        }

        public void Delete()
        {
            NativeFunctions.DestroyCaret();
        }

        public Point Location
        {
            set {
                NativeFunctions.SetCaretPos(value.X, value.Y);
                m_Location = value;
            }
            get
            {
                return m_Location;
            }
        }

        private Point m_Location;

        public bool Visible
        {
            set
            {
                if (value)
                {
                    NativeFunctions.ShowCaret(m_OwnerHandle);
                }
                else
                {
                    NativeFunctions.HideCaret(m_OwnerHandle);
                }
                m_Visible = value;
            }
            get
            {
                return m_Visible;
            }
        }

    }
}
