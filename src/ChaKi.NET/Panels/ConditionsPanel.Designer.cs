namespace ChaKi.Panels
{
    partial class ConditionsPanel
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

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConditionsPanel));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.filterTab = new System.Windows.Forms.TabPage();
            this.stringSearchTab = new System.Windows.Forms.TabPage();
            this.tagSearchTab = new System.Windows.Forms.TabPage();
            this.depSearchTab = new System.Windows.Forms.TabPage();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.stringSearchTab);
            this.tabControl1.Controls.Add(this.tagSearchTab);
            this.tabControl1.Controls.Add(this.depSearchTab);
            this.tabControl1.Controls.Add(this.filterTab);
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // filterTab
            // 
            this.filterTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.filterTab, "filterTab");
            this.filterTab.Name = "filterTab";
            // 
            // stringSearchTab
            // 
            this.stringSearchTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.stringSearchTab, "stringSearchTab");
            this.stringSearchTab.Name = "stringSearchTab";
            // 
            // tagSearchTab
            // 
            this.tagSearchTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.tagSearchTab, "tagSearchTab");
            this.tagSearchTab.Name = "tagSearchTab";
            // 
            // depSearchTab
            // 
            this.depSearchTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.depSearchTab, "depSearchTab");
            this.depSearchTab.Name = "depSearchTab";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.LightGray;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // ConditionsPanel
            // 
            this.AllowDrop = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.pictureBox1);
            resources.ApplyResources(this, "$this");
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.HandleDragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.HandleDragEnter);
            this.tabControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tagSearchTab;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TabPage filterTab;
        private System.Windows.Forms.TabPage depSearchTab;
        private System.Windows.Forms.TabPage stringSearchTab;
        private System.Windows.Forms.Button button1;
    }
}