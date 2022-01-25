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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KwicView));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.kwicViewPanel1 = new ChaKi.Views.KwicView.KwicViewPanel();
            this.kwicViewPanel2 = new ChaKi.Views.KwicView.KwicViewPanel();
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.transparentPanel1 = new ChaKi.Views.KwicView.KwicViewTransparentPanel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.Transparent;
            this.splitContainer1.Panel1.Controls.Add(this.kwicViewPanel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.Transparent;
            this.splitContainer1.Panel2.Controls.Add(this.kwicViewPanel2);
            resources.ApplyResources(this.splitContainer1.Panel2, "splitContainer1.Panel2");
            this.splitContainer1.Panel2Collapsed = true;
            // 
            // kwicViewPanel1
            // 
            this.kwicViewPanel1.BackColor = System.Drawing.Color.Ivory;
            resources.ApplyResources(this.kwicViewPanel1, "kwicViewPanel1");
            this.kwicViewPanel1.DrawLinesFrom = 0;
            this.kwicViewPanel1.DrawLinesTo = 0;
            this.kwicViewPanel1.KwicMode = false;
            this.kwicViewPanel1.Name = "kwicViewPanel1";
            this.kwicViewPanel1.SingleSelection = -1;
            this.kwicViewPanel1.SuspendUpdateView = false;
            this.kwicViewPanel1.TwoLineMode = true;
            this.kwicViewPanel1.UpdateFrozen = false;
            this.kwicViewPanel1.WordWrap = true;
            // 
            // kwicViewPanel2
            // 
            this.kwicViewPanel2.BackColor = System.Drawing.Color.Ivory;
            resources.ApplyResources(this.kwicViewPanel2, "kwicViewPanel2");
            this.kwicViewPanel2.DrawLinesFrom = 0;
            this.kwicViewPanel2.DrawLinesTo = 0;
            this.kwicViewPanel2.KwicMode = false;
            this.kwicViewPanel2.Name = "kwicViewPanel2";
            this.kwicViewPanel2.SingleSelection = -1;
            this.kwicViewPanel2.SuspendUpdateView = false;
            this.kwicViewPanel2.TwoLineMode = true;
            this.kwicViewPanel2.UpdateFrozen = false;
            this.kwicViewPanel2.WordWrap = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
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
            resources.ApplyResources(this.dataGridView1, "dataGridView1");
            this.dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowTemplate.Height = 21;
            this.dataGridView1.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView1_ColumnHeaderMouseClick);
            this.dataGridView1.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.dataGridView1_ColumnWidthChanged);
            this.dataGridView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataGridView1_MouseDown);
            this.dataGridView1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.dataGridView1_MouseMove);
            this.dataGridView1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dataGridView1_MouseUp);
            // 
            // Index
            // 
            resources.ApplyResources(this.Index, "Index");
            this.Index.Name = "Index";
            this.Index.ReadOnly = true;
            // 
            // Check
            // 
            resources.ApplyResources(this.Check, "Check");
            this.Check.Name = "Check";
            this.Check.ReadOnly = true;
            // 
            // Corpus
            // 
            resources.ApplyResources(this.Corpus, "Corpus");
            this.Corpus.Name = "Corpus";
            this.Corpus.ReadOnly = true;
            // 
            // Document
            // 
            resources.ApplyResources(this.Document, "Document");
            this.Document.Name = "Document";
            this.Document.ReadOnly = true;
            // 
            // Char
            // 
            resources.ApplyResources(this.Char, "Char");
            this.Char.Name = "Char";
            this.Char.ReadOnly = true;
            // 
            // Sen
            // 
            resources.ApplyResources(this.Sen, "Sen");
            this.Sen.Name = "Sen";
            this.Sen.ReadOnly = true;
            // 
            // Left
            // 
            resources.ApplyResources(this.Left, "Left");
            this.Left.Name = "Left";
            this.Left.ReadOnly = true;
            // 
            // Center
            // 
            resources.ApplyResources(this.Center, "Center");
            this.Center.Name = "Center";
            this.Center.ReadOnly = true;
            // 
            // Right
            // 
            resources.ApplyResources(this.Right, "Right");
            this.Right.Name = "Right";
            this.Right.ReadOnly = true;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.dataGridView1, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // transparentPanel1
            // 
            resources.ApplyResources(this.transparentPanel1, "transparentPanel1");
            this.transparentPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.transparentPanel1.Name = "transparentPanel1";
            // 
            // KwicView
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.transparentPanel1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "KwicView";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private KwicViewPanel kwicViewPanel1;
        private KwicViewPanel kwicViewPanel2;
        private KwicViewTransparentPanel transparentPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.SplitContainer splitContainer1;

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
