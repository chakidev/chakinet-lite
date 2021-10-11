namespace ChaKi.Panels
{
    partial class CommandPanel
    {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CommandPanel));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.corpus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nd = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nhit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nhitp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nret = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nretp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.searchToolStripButton = new System.Windows.Forms.ToolStripSplitButton();
            this.newSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.narrowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.appendToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wordListToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.collocationToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.abortToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.filterToolStripButton1 = new FilterButton();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.LightGray;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            resources.ApplyResources(this.dataGridView1, "dataGridView1");
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlDark;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.Linen;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.Blue;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.corpus,
            this.nc,
            this.nd,
            this.nhit,
            this.nhitp,
            this.nret,
            this.nretp});
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowTemplate.Height = 21;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            // 
            // corpus
            // 
            resources.ApplyResources(this.corpus, "corpus");
            this.corpus.Name = "corpus";
            this.corpus.ReadOnly = true;
            // 
            // nc
            // 
            resources.ApplyResources(this.nc, "nc");
            this.nc.Name = "nc";
            this.nc.ReadOnly = true;
            // 
            // nd
            // 
            resources.ApplyResources(this.nd, "nd");
            this.nd.Name = "nd";
            this.nd.ReadOnly = true;
            // 
            // nhit
            // 
            resources.ApplyResources(this.nhit, "nhit");
            this.nhit.Name = "nhit";
            this.nhit.ReadOnly = true;
            // 
            // nhitp
            // 
            resources.ApplyResources(this.nhitp, "nhitp");
            this.nhitp.Name = "nhitp";
            this.nhitp.ReadOnly = true;
            // 
            // nret
            // 
            resources.ApplyResources(this.nret, "nret");
            this.nret.Name = "nret";
            this.nret.ReadOnly = true;
            // 
            // nretp
            // 
            resources.ApplyResources(this.nretp, "nretp");
            this.nretp.Name = "nretp";
            this.nretp.ReadOnly = true;
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // filterToolStripButton1
            // 
            this.filterToolStripButton1.Name = "filterToolStripButton1";
            // 
            // searchToolStripButton
            // 
            this.searchToolStripButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newSearchToolStripMenuItem,
            this.narrowToolStripMenuItem,
            this.appendToolStripMenuItem});
            this.searchToolStripButton.ForeColor = System.Drawing.SystemColors.ControlText;
            resources.ApplyResources(this.searchToolStripButton, "searchToolStripButton");
            this.searchToolStripButton.Name = "searchToolStripButton";
            this.searchToolStripButton.ButtonClick += new System.EventHandler(this.searchStripSplitButton_ButtonClick);
            // 
            // newSearchToolStripMenuItem
            // 
            this.newSearchToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlText;
            resources.ApplyResources(this.newSearchToolStripMenuItem, "newSearchToolStripMenuItem");
            this.newSearchToolStripMenuItem.Name = "newSearchToolStripMenuItem";
            this.newSearchToolStripMenuItem.Click += new System.EventHandler(this.newSearchToolStripMenuItem_Click);
            // 
            // narrowToolStripMenuItem
            // 
            this.narrowToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlText;
            resources.ApplyResources(this.narrowToolStripMenuItem, "narrowToolStripMenuItem");
            this.narrowToolStripMenuItem.Name = "narrowToolStripMenuItem";
            this.narrowToolStripMenuItem.Click += new System.EventHandler(this.narrowToolStripMenuItem_Click);
            // 
            // appendToolStripMenuItem
            // 
            resources.ApplyResources(this.appendToolStripMenuItem, "appendToolStripMenuItem");
            this.appendToolStripMenuItem.Name = "appendToolStripMenuItem";
            this.appendToolStripMenuItem.Click += new System.EventHandler(this.appendToolStripMenuItem_Click);
            // 
            // wordListToolStripButton
            // 
            resources.ApplyResources(this.wordListToolStripButton, "wordListToolStripButton");
            this.wordListToolStripButton.Name = "wordListToolStripButton";
            this.wordListToolStripButton.Click += new System.EventHandler(this.wordListToolStripButton_Click);
            // 
            // collocationToolStripButton
            // 
            resources.ApplyResources(this.collocationToolStripButton, "collocationToolStripButton");
            this.collocationToolStripButton.Name = "collocationToolStripButton";
            this.collocationToolStripButton.Click += new System.EventHandler(this.collocationToolStripButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // abortToolStripButton
            // 
            this.abortToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            resources.ApplyResources(this.abortToolStripButton, "abortToolStripButton");
            this.abortToolStripButton.Name = "abortToolStripButton";
            this.abortToolStripButton.Click += new System.EventHandler(this.abortToolStripButton_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.filterToolStripButton1,
            this.searchToolStripButton,
            this.wordListToolStripButton,
            this.collocationToolStripButton,
            this.toolStripSeparator1,
            this.abortToolStripButton});
            this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.toolStrip1.Name = "toolStrip1";            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            resources.ApplyResources(this.copyToolStripMenuItem, "copyToolStripMenuItem");
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // CommandPanel
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.BackColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CommandPanel";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.DataGridViewTextBoxColumn corpus;
        private System.Windows.Forms.DataGridViewTextBoxColumn nc;
        private System.Windows.Forms.DataGridViewTextBoxColumn nd;
        private System.Windows.Forms.DataGridViewTextBoxColumn nhit;
        private System.Windows.Forms.DataGridViewTextBoxColumn nhitp;
        private System.Windows.Forms.DataGridViewTextBoxColumn nret;
        private System.Windows.Forms.DataGridViewTextBoxColumn nretp;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSplitButton searchToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem newSearchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem narrowToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton wordListToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton abortToolStripButton;
        private System.Windows.Forms.ToolStripButton collocationToolStripButton;
        private System.Windows.Forms.ToolStripMenuItem appendToolStripMenuItem;
        private FilterButton filterToolStripButton1;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
    }
}