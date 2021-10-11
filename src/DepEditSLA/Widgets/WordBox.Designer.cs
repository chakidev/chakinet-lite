namespace DependencyEditSLA.Widgets
{
    partial class WordBox
    {
        /// <summary> 
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private ChaKi.Common.LexemeBox lexemeBox1;

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
            this.lexemeBox1 = new ChaKi.Common.LexemeBox();
            this.SuspendLayout();
            // 
            // lexemeBox1
            // 
            this.lexemeBox1.AutoSize = true;
            this.lexemeBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.lexemeBox1.BackColor = System.Drawing.Color.Transparent;
            this.lexemeBox1.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lexemeBox1.Location = new System.Drawing.Point(0, 0);
            this.lexemeBox1.Name = "lexemeBox1";
            this.lexemeBox1.Size = new System.Drawing.Size(0, 0);
            this.lexemeBox1.TabIndex = 0;
            this.lexemeBox1.Visible = false;
            // 
            // WordBox
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.BackColor = System.Drawing.Color.LightGray;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.lexemeBox1);
            this.Name = "WordBox";
            this.Size = new System.Drawing.Size(75, 20);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.WordBox_Paint);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.WordBox_MouseHover);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
