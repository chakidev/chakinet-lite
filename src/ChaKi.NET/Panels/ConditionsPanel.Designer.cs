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
            this.corpusTab = new System.Windows.Forms.TabPage();
            this.filterTab = new System.Windows.Forms.TabPage();
            this.stringSearchTab = new System.Windows.Forms.TabPage();
            this.tagSearchTab = new System.Windows.Forms.TabPage();
            this.depSearchTab = new System.Windows.Forms.TabPage();
            this.collocationTab = new System.Windows.Forms.TabPage();
            this.addinTab = new System.Windows.Forms.TabPage();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tabControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.TabPages.AddRange(new System.Windows.Forms.TabPage[] {
            this.corpusTab,
            this.filterTab,
            this.stringSearchTab,
            this.tagSearchTab,
            this.depSearchTab,
            this.collocationTab,
            this.addinTab});
            // 
            // corpusTab
            // 
            this.corpusTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.corpusTab, "corpusTab");
            this.corpusTab.Name = "corpusTab";
            this.corpusTab.Text = "Corpus";
            // 
            // filterTab
            // 
            this.filterTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.filterTab, "filterTab");
            this.filterTab.Name = "filterTab";
            this.filterTab.Text = "Filter";
            // 
            // stringSearchTab
            // 
            this.stringSearchTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.stringSearchTab, "stringSearchTab");
            this.stringSearchTab.Name = "stringSearchTab";
            this.stringSearchTab.Text = "String";
            // 
            // tagSearchTab
            // 
            this.tagSearchTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.tagSearchTab, "tagSearchTab");
            this.tagSearchTab.Name = "tagSearchTab";
            this.tagSearchTab.Text = "Tag";
            // 
            // depSearchTab
            // 
            this.depSearchTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.depSearchTab, "depSearchTab");
            this.depSearchTab.Name = "depSearchTab";
            this.depSearchTab.Text = "Dependency";
            // 
            // collocationTab
            // 
            this.collocationTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.collocationTab, "collocationTab");
            this.collocationTab.Name = "collocationTab";
            this.collocationTab.Text = "Collocation";
            // 
            // addinTab
            // 
            resources.ApplyResources(this.addinTab, "addinTab");
            this.addinTab.Name = "addinTab";
            this.addinTab.Text = "AddIn";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.LightGray;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // ConditionsPanel
            // 
            this.AllowDrop = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "ConditionsPanel";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.HandleDragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.HandleDragEnter);
            this.tabControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage corpusTab;
        private System.Windows.Forms.TabPage tagSearchTab;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TabPage filterTab;
        private System.Windows.Forms.TabPage depSearchTab;
        private System.Windows.Forms.TabPage collocationTab;
        private System.Windows.Forms.TabPage stringSearchTab;
        private System.Windows.Forms.TabPage addinTab;
    }
}