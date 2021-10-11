namespace ChaKi.Options
{
    partial class WordColorSettingPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WordColorSettingPanel));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.colorPicker2 = new ChaKi.Common.ColorPickerButton();
            this.colorPicker1 = new ChaKi.Common.ColorPickerButton();
            this.propertyBox1 = new ChaKi.GUICommon.PropertyBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.labelSampleText = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.colorPicker2);
            this.groupBox1.Controls.Add(this.colorPicker1);
            this.groupBox1.Controls.Add(this.propertyBox1);
            this.groupBox1.Controls.Add(this.textBox2);
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.labelSampleText);
            this.groupBox1.Controls.Add(this.button1);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // checkBox1
            // 
            resources.ApplyResources(this.checkBox1, "checkBox1");
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // colorPicker2
            // 
            resources.ApplyResources(this.colorPicker2, "colorPicker2");
            this.colorPicker2.BackColor = System.Drawing.Color.White;
            this.colorPicker2.Color = System.Drawing.Color.Transparent;
            this.colorPicker2.Name = "colorPicker2";
            this.colorPicker2.UseVisualStyleBackColor = false;
            // 
            // colorPicker1
            // 
            resources.ApplyResources(this.colorPicker1, "colorPicker1");
            this.colorPicker1.BackColor = System.Drawing.Color.White;
            this.colorPicker1.Color = System.Drawing.Color.Transparent;
            this.colorPicker1.Name = "colorPicker1";
            this.colorPicker1.UseVisualStyleBackColor = false;
            // 
            // propertyBox1
            // 
            resources.ApplyResources(this.propertyBox1, "propertyBox1");
            this.propertyBox1.BackColor = System.Drawing.Color.Transparent;
            this.propertyBox1.CenterizedButtonVisible = true;
            this.propertyBox1.IsPivot = false;
            this.propertyBox1.Name = "propertyBox1";
            this.propertyBox1.Range = ((ChaKi.Entity.Search.Range)(resources.GetObject("propertyBox1.Range")));
            this.propertyBox1.ShowRange = false;
            // 
            // textBox2
            // 
            resources.ApplyResources(this.textBox2, "textBox2");
            this.textBox2.Name = "textBox2";
            // 
            // textBox1
            // 
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.Name = "textBox1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // labelSampleText
            // 
            resources.ApplyResources(this.labelSampleText, "labelSampleText");
            this.labelSampleText.Name = "labelSampleText";
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // WordColorSettingPanel
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.groupBox1);
            this.Name = "WordColorSettingPanel";
            resources.ApplyResources(this, "$this");
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label labelSampleText;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private ChaKi.GUICommon.PropertyBox propertyBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private ChaKi.Common.ColorPickerButton colorPicker1;
        private ChaKi.Common.ColorPickerButton colorPicker2;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}
