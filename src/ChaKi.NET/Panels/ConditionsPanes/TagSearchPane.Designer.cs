using System.Drawing;
namespace ChaKi.Panels.ConditionsPanes
{
    partial class TagSearchPane
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
            this.leftButton = new System.Windows.Forms.Button();
            this.rightButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // leftButton
            // 
            this.leftButton.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.leftButton.Location = new System.Drawing.Point(0, 0);
            this.leftButton.Name = "leftButton";
            this.leftButton.Size = new System.Drawing.Size(18, 18);
            this.leftButton.TabIndex = 0;
            this.leftButton.Text = "+";
            this.leftButton.UseVisualStyleBackColor = true;
            this.leftButton.Click += new System.EventHandler(this.leftButton_Click);
            // 
            // rightButton
            // 
            this.rightButton.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rightButton.Location = new System.Drawing.Point(20, 0);
            this.rightButton.Name = "rightButton";
            this.rightButton.Size = new System.Drawing.Size(18, 18);
            this.rightButton.TabIndex = 0;
            this.rightButton.Text = "+";
            this.rightButton.UseVisualStyleBackColor = true;
            this.rightButton.Click += new System.EventHandler(this.rightButton_Click);
            // 
            // TagSearchPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.leftButton);
            this.Controls.Add(this.rightButton);
            this.Name = "TagSearchPane";
            this.Size = new System.Drawing.Size(613, 175);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button leftButton;
        private System.Windows.Forms.Button rightButton;
    }
}
