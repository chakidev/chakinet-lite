namespace DependencyEditSLA.Widgets
{
    partial class LinkTagLabel
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
            this.SuspendLayout();
            // 
            // LinkTagLabel
            // 
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.Blue;
            this.Size = new System.Drawing.Size(16, 16);
            this.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TagLabel_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion

    }
}
