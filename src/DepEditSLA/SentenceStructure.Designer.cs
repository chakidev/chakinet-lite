using System.Windows.Forms;
namespace DependencyEditSLA
{
    partial class SentenceStructure
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
        private ContextMenuStrip contextMenuStrip1;
        private ContextMenuStrip groupingContextMenuStrip;
        private ToolStripMenuItem abridgeByLinkToolStripMenuItem;
        private ToolStripMenuItem deleteGroupToolStripMenuItem;
        private ContextMenuStrip contextMenuStrip2;

        /// <summary> 
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SentenceStructure));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteLinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modifyLinkTagToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.abridgeByLinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip2 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteGroupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupingContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextMenuStrip3 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.dependencyEditSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReturnToParentButton = new System.Windows.Forms.Button();
            this.linkContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextMenuStrip1.SuspendLayout();
            this.contextMenuStrip2.SuspendLayout();
            this.contextMenuStrip3.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteLinkToolStripMenuItem,
            this.modifyLinkTagToolStripMenuItem,
            this.abridgeByLinkToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.ShowImageMargin = false;
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            // 
            // deleteLinkToolStripMenuItem
            // 
            this.deleteLinkToolStripMenuItem.Name = "deleteLinkToolStripMenuItem";
            resources.ApplyResources(this.deleteLinkToolStripMenuItem, "deleteLinkToolStripMenuItem");
            this.deleteLinkToolStripMenuItem.Click += new System.EventHandler(this.deleteLinkToolStripMenuItem_Click);
            // 
            // modifyLinkTagToolStripMenuItem
            // 
            this.modifyLinkTagToolStripMenuItem.Name = "modifyLinkTagToolStripMenuItem";
            resources.ApplyResources(this.modifyLinkTagToolStripMenuItem, "modifyLinkTagToolStripMenuItem");
            this.modifyLinkTagToolStripMenuItem.Click += new System.EventHandler(this.modifyLinkTagToolStripMenuItem_Click);
            // 
            // abridgeByLinkToolStripMenuItem
            // 
            this.abridgeByLinkToolStripMenuItem.Name = "abridgeByLinkToolStripMenuItem";
            resources.ApplyResources(this.abridgeByLinkToolStripMenuItem, "abridgeByLinkToolStripMenuItem");
            this.abridgeByLinkToolStripMenuItem.Click += new System.EventHandler(this.abridgeToolStripMenuItem_Clicked);
            // 
            // contextMenuStrip2
            // 
            this.contextMenuStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteGroupToolStripMenuItem});
            this.contextMenuStrip2.Name = "contextMenuStrip2";
            this.contextMenuStrip2.ShowImageMargin = false;
            resources.ApplyResources(this.contextMenuStrip2, "contextMenuStrip2");
            this.contextMenuStrip2.Closed += new System.Windows.Forms.ToolStripDropDownClosedEventHandler(this.contextMenuStrip2_Closed);
            // 
            // deleteGroupToolStripMenuItem
            // 
            this.deleteGroupToolStripMenuItem.Name = "deleteGroupToolStripMenuItem";
            resources.ApplyResources(this.deleteGroupToolStripMenuItem, "deleteGroupToolStripMenuItem");
            // 
            // groupingContextMenuStrip
            // 
            this.groupingContextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.groupingContextMenuStrip.Name = "groupingcontextMenuStrip";
            this.groupingContextMenuStrip.ShowImageMargin = false;
            resources.ApplyResources(this.groupingContextMenuStrip, "groupingContextMenuStrip");
            // 
            // contextMenuStrip3
            // 
            this.contextMenuStrip3.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dependencyEditSettingsToolStripMenuItem});
            this.contextMenuStrip3.Name = "contextMenuStrip3";
            resources.ApplyResources(this.contextMenuStrip3, "contextMenuStrip3");
            // 
            // dependencyEditSettingsToolStripMenuItem
            // 
            this.dependencyEditSettingsToolStripMenuItem.Name = "dependencyEditSettingsToolStripMenuItem";
            resources.ApplyResources(this.dependencyEditSettingsToolStripMenuItem, "dependencyEditSettingsToolStripMenuItem");
            this.dependencyEditSettingsToolStripMenuItem.Click += new System.EventHandler(this.dependencyEditSettingsToolStripMenuItem_Click);
            // 
            // ReturnToParentButton
            // 
            resources.ApplyResources(this.ReturnToParentButton, "ReturnToParentButton");
            this.ReturnToParentButton.Name = "ReturnToParentButton";
            this.ReturnToParentButton.UseVisualStyleBackColor = true;
            this.ReturnToParentButton.Click += new System.EventHandler(this.ReturnToParentButton_Click);
            // 
            // linkContextMenuStrip
            // 
            this.linkContextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.linkContextMenuStrip.Name = "linkContextMenuStrip";
            this.linkContextMenuStrip.ShowImageMargin = false;
            resources.ApplyResources(this.linkContextMenuStrip, "linkContextMenuStrip");
            // 
            // SentenceStructure
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            resources.ApplyResources(this, "$this");
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.ReturnToParentButton);
            this.Name = "SentenceStructure";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.SentenceStructure_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.SentenceStructure_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.SentenceStructure_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.SentenceStructure_MouseUp);
            this.contextMenuStrip1.ResumeLayout(false);
            this.contextMenuStrip2.ResumeLayout(false);
            this.contextMenuStrip3.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private Button ReturnToParentButton;
        private ContextMenuStrip contextMenuStrip3;
        private ToolStripMenuItem dependencyEditSettingsToolStripMenuItem;
        private ToolStripMenuItem deleteLinkToolStripMenuItem;
        private ToolStripMenuItem modifyLinkTagToolStripMenuItem;
        private ContextMenuStrip linkContextMenuStrip;
    }
}
