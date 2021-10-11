namespace ChaKi.Views.KwicView
{
    partial class KwicView
    {
        /// <summary> 
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナで生成されたコード

        /// <summary> 
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Index = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Check = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Corpus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Document = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Char = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Sen = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Left = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Center = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Right = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.kwicViewPanel1 = new ChaKi.Views.KwicView.KwicViewPanel();
            this.kwicViewPanel2 = new ChaKi.Views.KwicView.KwicViewPanel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.transparentPanel1 = new KwicViewTransparentPanel();
            //((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.transparentPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Index,
            this.Check,
            this.Corpus,
            this.Document,
            this.Char,
            this.Sen,
            this.Left,
            this.Center,
            this.Right});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowHeadersWidth = 4;
            this.dataGridView1.RowTemplate.Height = 21;
            this.dataGridView1.Size = new System.Drawing.Size(800, 27);
            this.dataGridView1.TabIndex = 3;
            this.dataGridView1.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView1_ColumnHeaderMouseClick);
            this.dataGridView1.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.dataGridView1_ColumnWidthChanged);
            this.dataGridView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataGridView1_MouseDown);
            this.dataGridView1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.dataGridView1_MouseMove);
            this.dataGridView1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dataGridView1_MouseUp);
             // 
            // Index
            // 
            this.Index.HeaderText = "Index";
            this.Index.Name = "Index";
            this.Index.ReadOnly = true;
            this.Index.ToolTipText = "Index";
            this.Index.Width = 37;
            // 
            // Check
            // 
            this.Check.HeaderText = "Check";
            this.Check.Name = "Check";
            this.Check.ReadOnly = true;
            this.Check.ToolTipText = "Check";
            this.Check.Width = 24;
            // 
            // Corpus
            // 
            this.Corpus.HeaderText = "Corpus";
            this.Corpus.Name = "Corpus";
            this.Corpus.ReadOnly = true;
            this.Corpus.ToolTipText = "Corpus";
            this.Corpus.Width = 80;
            // 
            // Document
            // 
            this.Document.HeaderText = "Doc";
            this.Document.Name = "Document";
            this.Document.ReadOnly = true;
            this.Document.Width = 80;
            // 
            // Char
            // 
            this.Char.HeaderText = "Char";
            this.Char.Name = "Char";
            this.Char.ReadOnly = true;
            this.Char.ToolTipText = "CharPos";
            this.Char.Width = 44;
            // 
            // Sen
            // 
            this.Sen.HeaderText = "Sen";
            this.Sen.Name = "Sen";
            this.Sen.ReadOnly = true;
            this.Sen.ToolTipText = "SentencePos";
            this.Sen.Width = 44;
            // 
            // Left
            // 
            this.Left.HeaderText = "Left";
            this.Left.Name = "Left";
            this.Left.ReadOnly = true;
            this.Left.ToolTipText = "LeftContext";
            this.Left.Width = 180;
            // 
            // Center
            // 
            this.Center.HeaderText = "Center";
            this.Center.MinimumWidth = 2;
            this.Center.Name = "Center";
            this.Center.ReadOnly = true;
            this.Center.ToolTipText = "Center";
            this.Center.Width = 80;
            // 
            // Right
            // 
            this.Right.HeaderText = "Right";
            this.Right.MinimumWidth = 2;
            this.Right.Name = "Right";
            this.Right.ReadOnly = true;
            this.Right.ToolTipText = "RightContext";
            this.Right.Width = 180;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.dataGridView1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 400);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 30);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(0);
            this.splitContainer1.Panel2.Padding = new System.Windows.Forms.Padding(3);
            this.splitContainer1.Panel2Collapsed = true;
            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.Transparent;
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.Transparent;
            this.splitContainer1.Panel1.Controls.Add(this.kwicViewPanel1);
            this.splitContainer1.Panel2.Controls.Add(this.kwicViewPanel2);
            this.splitContainer1.Size = new System.Drawing.Size(794, 367);
            this.splitContainer1.SplitterDistance = 210;
            this.splitContainer1.TabIndex = 6;
            //
            // kwicViewPanel1
            // 
            this.kwicViewPanel1.BackColor = System.Drawing.Color.Ivory;
            this.kwicViewPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kwicViewPanel1.DrawLinesFrom = 0;
            this.kwicViewPanel1.DrawLinesTo = 0;
            this.kwicViewPanel1.KwicMode = false;
            this.kwicViewPanel1.Location = new System.Drawing.Point(0, 0);
            this.kwicViewPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.kwicViewPanel1.Name = "kwicViewPanel1";
            this.kwicViewPanel1.SingleSelection = -1;
            this.kwicViewPanel1.Size = new System.Drawing.Size(800, 400);
            this.kwicViewPanel1.SuspendUpdateView = false;
            this.kwicViewPanel1.TabIndex = 4;
            this.kwicViewPanel1.TwoLineMode = true;
            this.kwicViewPanel1.UpdateFrozen = false;
            this.kwicViewPanel1.WordWrap = true;
            //
            // kwicViewPanel2
            // 
            this.kwicViewPanel2.BackColor = System.Drawing.Color.Ivory;
            this.kwicViewPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kwicViewPanel2.DrawLinesFrom = 0;
            this.kwicViewPanel2.DrawLinesTo = 0;
            this.kwicViewPanel2.KwicMode = false;
            this.kwicViewPanel2.Location = new System.Drawing.Point(0, 0);
            this.kwicViewPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.kwicViewPanel2.Name = "kwicViewPanel2";
            this.kwicViewPanel2.SingleSelection = -1;
            this.kwicViewPanel2.Size = new System.Drawing.Size(800, 400);
            this.kwicViewPanel2.SuspendUpdateView = false;
            this.kwicViewPanel2.TabIndex = 5;
            this.kwicViewPanel2.TwoLineMode = true;
            this.kwicViewPanel2.UpdateFrozen = false;
            this.kwicViewPanel2.WordWrap = true;
            //
            // transparentPanel1
            //
            this.transparentPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.transparentPanel1.Location = new System.Drawing.Point(3, 30);
            this.transparentPanel1.Size = new System.Drawing.Size(794, 367);
            this.transparentPanel1.BackColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);
            this.transparentPanel1.Visible = false;
            // 
            // KwicView
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.transparentPanel1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "KwicView";
            this.Size = new System.Drawing.Size(800, 400);
            //((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.transparentPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private KwicViewPanel kwicViewPanel1;
        private KwicViewPanel kwicViewPanel2;
        private KwicViewTransparentPanel transparentPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.SplitContainer splitContainer1;

        private System.Windows.Forms.DataGridViewTextBoxColumn KLeft;
        private System.Windows.Forms.DataGridViewTextBoxColumn KCenter;
        private System.Windows.Forms.DataGridViewTextBoxColumn KRight;
        private System.Windows.Forms.DataGridViewTextBoxColumn Index;
        private System.Windows.Forms.DataGridViewTextBoxColumn Check;
        private System.Windows.Forms.DataGridViewTextBoxColumn Corpus;
        private System.Windows.Forms.DataGridViewTextBoxColumn Document;
        private System.Windows.Forms.DataGridViewTextBoxColumn Char;
        private System.Windows.Forms.DataGridViewTextBoxColumn Sen;
        private System.Windows.Forms.DataGridViewTextBoxColumn LeftColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn CenterColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn RightColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn Left;
        private System.Windows.Forms.DataGridViewTextBoxColumn Center;
        private System.Windows.Forms.DataGridViewTextBoxColumn Right;

    }
}
