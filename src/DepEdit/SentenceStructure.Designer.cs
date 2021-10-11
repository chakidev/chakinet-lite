namespace DependencyEdit
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

        /// <summary> 
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SentenceStructure
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Linen;
            this.Name = "SentenceStructure";
            this.Size = new System.Drawing.Size(533, 251);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.SentenceStructure_Paint);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.SentenceStructure_MouseMove);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.SentenceStructure_MouseDown);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.SentenceStructure_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}

