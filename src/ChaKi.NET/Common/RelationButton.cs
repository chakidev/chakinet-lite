using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using ChaKi.Common.Widgets;

namespace ChaKi.GUICommon
{
    public enum RelationButtonStyle
    {
        Leftmost,
        Rightmost,
        Default
    }

    public class RelationButton : Button
    {
        /// <summary>
        /// どのBunsesuに属するか。Bunsetsuに属さないボタンの場合は、-1
        /// </summary>
        public int BunsetsuID;

        public RelationButtonStyle Style
        {
            set
            {
                if (value == RelationButtonStyle.Leftmost)
                {
                    this.menuItem_Leftmost.Visible = true;
                    this.menuItem_Rightmost.Visible = false;
                }
                else if (value == RelationButtonStyle.Rightmost)
                {
                    this.menuItem_Leftmost.Visible = false;
                    this.menuItem_Rightmost.Visible = true;
                }
                else
                {
                    this.menuItem_Leftmost.Visible = false;
                    this.menuItem_Rightmost.Visible = false;
                }
             
            }
        }

        public int ID;

        public ContextMenuStrip Menu
        {
            get
            {
                return this.contextMenu;
            }
        }

        public event RelationCommandEventHandler OnCommandEvent;

        private DpiAdjuster m_DpiAdjuster;

        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem menuItem_None;
        private ToolStripMenuItem menuItem_Greater;
        private ToolStripMenuItem menuItem_Leftmost;
        private ToolStripMenuItem menuItem_Rightmost;
        private ToolStripMenuItem menuItem_Plus;
        private ToolStripMenuItem menuItem_Consecutive;
        private System.ComponentModel.IContainer components;
    
        public RelationButton()
            : this(-1, 0)
        {
        }

        public RelationButton(int bunsetsuid, int id)
        {
            this.BunsetsuID = bunsetsuid;
            this.ID = id;
            InitializeComponent();
            this.Style = RelationButtonStyle.Default;

            m_DpiAdjuster = new DpiAdjuster((xscale, yscale) => {
                this.Width = (int)((float)this.Width * xscale);
                this.Height = (int)((float)this.Height * yscale);
            });
            using (var g = this.CreateGraphics())
            {
                m_DpiAdjuster.Adjust(g);
            }
        }


        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuItem_None = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItem_Plus = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItem_Greater = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItem_Consecutive = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItem_Leftmost = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItem_Rightmost = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenu
            // 
            this.contextMenu.Font = new System.Drawing.Font("Lucida Sans Unicode", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItem_None,
            this.menuItem_Plus,
            this.menuItem_Greater,
            this.menuItem_Consecutive,
            this.menuItem_Leftmost,
            this.menuItem_Rightmost});
            this.contextMenu.Name = "contextMenuStrip1";
            this.contextMenu.ShowImageMargin = false;
            this.contextMenu.Size = new System.Drawing.Size(62, 158);
            // 
            // menuItem_None
            // 
            this.menuItem_None.Name = "menuItem_None";
            this.menuItem_None.Size = new System.Drawing.Size(61, 22);
            this.menuItem_None.Text = " ";
            this.menuItem_None.Click += new System.EventHandler(this.menuItem_None_Click);
            // 
            // menuItem_Plus
            // 
            this.menuItem_Plus.Name = "menuItem_Plus";
            this.menuItem_Plus.Size = new System.Drawing.Size(61, 22);
            this.menuItem_Plus.Text = "+";
            this.menuItem_Plus.ToolTipText = "Insert New Item";
            this.menuItem_Plus.Click += new System.EventHandler(this.menuItem_Plus_Click);
            // 
            // menuItem_Greater
            // 
            this.menuItem_Greater.Name = "menuItem_Greater";
            this.menuItem_Greater.Size = new System.Drawing.Size(61, 22);
            this.menuItem_Greater.Text = "<";
            this.menuItem_Greater.ToolTipText = "Ascending Order";
            this.menuItem_Greater.Click += new System.EventHandler(this.menuItem_Greater_Click);
            // 
            // menuItem_Consecutive
            // 
            this.menuItem_Consecutive.Name = "menuItem_Consecutive";
            this.menuItem_Consecutive.Size = new System.Drawing.Size(61, 22);
            this.menuItem_Consecutive.Text = "-";
            this.menuItem_Consecutive.ToolTipText = "Consecutive Order";
            this.menuItem_Consecutive.Click += new System.EventHandler(this.menuItem_Consecutive_Click);
            // 
            // menuItem_Leftmost
            // 
            this.menuItem_Leftmost.Name = "menuItem_Leftmost";
            this.menuItem_Leftmost.Size = new System.Drawing.Size(61, 22);
            this.menuItem_Leftmost.Text = "^";
            this.menuItem_Leftmost.ToolTipText = "Leftmost Item";
            this.menuItem_Leftmost.Click += new System.EventHandler(this.menuItem_Leftmost_Click);
            // 
            // menuItem_Rightmost
            // 
            this.menuItem_Rightmost.Name = "menuItem_Rightmost";
            this.menuItem_Rightmost.Size = new System.Drawing.Size(61, 22);
            this.menuItem_Rightmost.Text = "$";
            this.menuItem_Rightmost.ToolTipText = "Rightmost Item";
            this.menuItem_Rightmost.Click += new System.EventHandler(this.menuItem_Rightmost_Click);
            // 
            // RelationButton
            // 
            this.Font = new System.Drawing.Font("Lucida Sans Unicode", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Size = new System.Drawing.Size(21, 21);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RelationButton_MouseDown);
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #region Button-menu Command Handlers
        void menuItem_None_Click(object sender, EventArgs e)
        {
            if (this.OnCommandEvent != null)
            {
                this.OnCommandEvent(this, new RelationCommandEventArgs(' '));
            }
        }
        void menuItem_Plus_Click(object sender, EventArgs e)
        {
            if (this.OnCommandEvent != null)
            {
                this.OnCommandEvent(this, new RelationCommandEventArgs('+'));
            }
        }
        void menuItem_Greater_Click(object sender, EventArgs e)
        {
            if (this.OnCommandEvent != null)
            {
                this.OnCommandEvent(this, new RelationCommandEventArgs('<'));
            }
        }
        void menuItem_Less_Click(object sender, EventArgs e)
        {
            if (this.OnCommandEvent != null)
            {
                this.OnCommandEvent(this, new RelationCommandEventArgs('>'));
            }
        }
        void menuItem_Consecutive_Click(object sender, EventArgs e)
        {
            if (this.OnCommandEvent != null)
            {
                this.OnCommandEvent(this, new RelationCommandEventArgs('-'));
            }
        }
        void menuItem_Leftmost_Click(object sender, EventArgs e)
        {
            if (this.OnCommandEvent != null)
            {
                this.OnCommandEvent(this, new RelationCommandEventArgs('^'));
            }
        }
        void menuItem_Rightmost_Click(object sender, EventArgs e)
        {
            if (this.OnCommandEvent != null)
            {
                this.OnCommandEvent(this, new RelationCommandEventArgs('$'));
            }
        }
        #endregion

        private void RelationButton_MouseDown(object sender, MouseEventArgs e)
        {
            this.contextMenu.Show(PointToScreen(e.Location));
        }

    }
}
