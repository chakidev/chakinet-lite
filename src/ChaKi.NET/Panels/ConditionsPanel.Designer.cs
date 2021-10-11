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
            this.tabControl1 = new Crownwood.DotNetMagic.Controls.TabControl();
            this.corpusTab = new Crownwood.DotNetMagic.Controls.TabPage();
            this.filterTab = new Crownwood.DotNetMagic.Controls.TabPage();
            this.stringSearchTab = new Crownwood.DotNetMagic.Controls.TabPage();
            this.tagSearchTab = new Crownwood.DotNetMagic.Controls.TabPage();
            this.depSearchTab = new Crownwood.DotNetMagic.Controls.TabPage();
            this.collocationTab = new Crownwood.DotNetMagic.Controls.TabPage();
            this.addinTab = new Crownwood.DotNetMagic.Controls.TabPage();
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
            this.tabControl1.OfficeDockSides = false;
            this.tabControl1.PositionTop = true;
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.ShowDropSelect = false;
            this.tabControl1.TabPages.AddRange(new Crownwood.DotNetMagic.Controls.TabPage[] {
            this.corpusTab,
            this.filterTab,
            this.stringSearchTab,
            this.tagSearchTab,
            this.depSearchTab,
            this.collocationTab,
            this.addinTab});
            this.tabControl1.TextTips = true;
            this.tabControl1.SelectionChanged += new Crownwood.DotNetMagic.Controls.SelectTabHandler(this.tabControl1_SelectionChanged);
            // 
            // corpusTab
            // 
            this.corpusTab.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this.corpusTab, "corpusTab");
            this.corpusTab.InactiveBackColor = System.Drawing.Color.Empty;
            this.corpusTab.InactiveTextBackColor = System.Drawing.Color.Empty;
            this.corpusTab.InactiveTextColor = System.Drawing.Color.Empty;
            this.corpusTab.Name = "corpusTab";
            this.corpusTab.SelectBackColor = System.Drawing.Color.Empty;
            this.corpusTab.SelectTextBackColor = System.Drawing.Color.Empty;
            this.corpusTab.SelectTextColor = System.Drawing.Color.Empty;
            // 
            // filterTab
            // 
            this.filterTab.BackColor = System.Drawing.SystemColors.Control;
            this.filterTab.InactiveBackColor = System.Drawing.Color.Empty;
            this.filterTab.InactiveTextBackColor = System.Drawing.Color.Empty;
            this.filterTab.InactiveTextColor = System.Drawing.Color.Empty;
            resources.ApplyResources(this.filterTab, "filterTab");
            this.filterTab.Name = "filterTab";
            this.filterTab.SelectBackColor = System.Drawing.Color.Empty;
            this.filterTab.Selected = false;
            this.filterTab.SelectTextBackColor = System.Drawing.Color.Empty;
            this.filterTab.SelectTextColor = System.Drawing.Color.Empty;
            // 
            // stringSearchTab
            // 
            this.stringSearchTab.BackColor = System.Drawing.SystemColors.Control;
            this.stringSearchTab.InactiveBackColor = System.Drawing.Color.Empty;
            this.stringSearchTab.InactiveTextBackColor = System.Drawing.Color.Empty;
            this.stringSearchTab.InactiveTextColor = System.Drawing.Color.Empty;
            resources.ApplyResources(this.stringSearchTab, "stringSearchTab");
            this.stringSearchTab.Name = "stringSearchTab";
            this.stringSearchTab.SelectBackColor = System.Drawing.Color.Empty;
            this.stringSearchTab.Selected = false;
            this.stringSearchTab.SelectTextBackColor = System.Drawing.Color.Empty;
            this.stringSearchTab.SelectTextColor = System.Drawing.Color.Empty;
            // 
            // tagSearchTab
            // 
            this.tagSearchTab.BackColor = System.Drawing.SystemColors.Control;
            this.tagSearchTab.InactiveBackColor = System.Drawing.Color.Empty;
            this.tagSearchTab.InactiveTextBackColor = System.Drawing.Color.Empty;
            this.tagSearchTab.InactiveTextColor = System.Drawing.Color.Empty;
            resources.ApplyResources(this.tagSearchTab, "tagSearchTab");
            this.tagSearchTab.Name = "tagSearchTab";
            this.tagSearchTab.SelectBackColor = System.Drawing.Color.Empty;
            this.tagSearchTab.Selected = false;
            this.tagSearchTab.SelectTextBackColor = System.Drawing.Color.Empty;
            this.tagSearchTab.SelectTextColor = System.Drawing.Color.Empty;
            // 
            // depSearchTab
            // 
            this.depSearchTab.BackColor = System.Drawing.SystemColors.Control;
            this.depSearchTab.InactiveBackColor = System.Drawing.Color.Empty;
            this.depSearchTab.InactiveTextBackColor = System.Drawing.Color.Empty;
            this.depSearchTab.InactiveTextColor = System.Drawing.Color.Empty;
            resources.ApplyResources(this.depSearchTab, "depSearchTab");
            this.depSearchTab.Name = "depSearchTab";
            this.depSearchTab.SelectBackColor = System.Drawing.Color.Empty;
            this.depSearchTab.Selected = false;
            this.depSearchTab.SelectTextBackColor = System.Drawing.Color.Empty;
            this.depSearchTab.SelectTextColor = System.Drawing.Color.Empty;
            // 
            // collocationTab
            // 
            this.collocationTab.BackColor = System.Drawing.SystemColors.Control;
            this.collocationTab.InactiveBackColor = System.Drawing.Color.Empty;
            this.collocationTab.InactiveTextBackColor = System.Drawing.Color.Empty;
            this.collocationTab.InactiveTextColor = System.Drawing.Color.Empty;
            resources.ApplyResources(this.collocationTab, "collocationTab");
            this.collocationTab.Name = "collocationTab";
            this.collocationTab.SelectBackColor = System.Drawing.Color.Empty;
            this.collocationTab.Selected = false;
            this.collocationTab.SelectTextBackColor = System.Drawing.Color.Empty;
            this.collocationTab.SelectTextColor = System.Drawing.Color.Empty;
            // 
            // addinTab
            // 
            this.addinTab.InactiveBackColor = System.Drawing.Color.Empty;
            this.addinTab.InactiveTextBackColor = System.Drawing.Color.Empty;
            this.addinTab.InactiveTextColor = System.Drawing.Color.Empty;
            resources.ApplyResources(this.addinTab, "addinTab");
            this.addinTab.Name = "addinTab";
            this.addinTab.SelectBackColor = System.Drawing.Color.Empty;
            this.addinTab.Selected = false;
            this.addinTab.SelectTextBackColor = System.Drawing.Color.Empty;
            this.addinTab.SelectTextColor = System.Drawing.Color.Empty;
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
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ConditionsPanel";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.HandleDragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.HandleDragEnter);
            this.tabControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Crownwood.DotNetMagic.Controls.TabControl tabControl1;
        private Crownwood.DotNetMagic.Controls.TabPage corpusTab;
        private Crownwood.DotNetMagic.Controls.TabPage tagSearchTab;
        private System.Windows.Forms.PictureBox pictureBox1;
        private Crownwood.DotNetMagic.Controls.TabPage filterTab;
        private Crownwood.DotNetMagic.Controls.TabPage depSearchTab;
        private Crownwood.DotNetMagic.Controls.TabPage collocationTab;
        private Crownwood.DotNetMagic.Controls.TabPage stringSearchTab;
        private Crownwood.DotNetMagic.Controls.TabPage addinTab;
    }
}