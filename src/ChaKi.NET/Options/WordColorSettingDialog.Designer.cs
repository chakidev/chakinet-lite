namespace ChaKi.Options
{
    partial class WordColorSettingDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WordColorSettingDialog));
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.wordAttributeColorSettingPanel1 = new ChaKi.Options.WordColorSettingPanel();
            this.wordAttributeColorSettingPanel2 = new ChaKi.Options.WordColorSettingPanel();
            this.wordAttributeColorSettingPanel3 = new ChaKi.Options.WordColorSettingPanel();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // textBox1
            // 
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            resources.ApplyResources(this.button2, "button2");
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Name = "button2";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // wordAttributeColorSettingPanel1
            // 
            resources.ApplyResources(this.wordAttributeColorSettingPanel1, "wordAttributeColorSettingPanel1");
            this.wordAttributeColorSettingPanel1.Data = null;
            this.wordAttributeColorSettingPanel1.Name = "wordAttributeColorSettingPanel1";
            // 
            // wordAttributeColorSettingPanel2
            // 
            resources.ApplyResources(this.wordAttributeColorSettingPanel2, "wordAttributeColorSettingPanel2");
            this.wordAttributeColorSettingPanel2.Data = null;
            this.wordAttributeColorSettingPanel2.Name = "wordAttributeColorSettingPanel2";
            // 
            // wordAttributeColorSettingPanel3
            // 
            resources.ApplyResources(this.wordAttributeColorSettingPanel3, "wordAttributeColorSettingPanel3");
            this.wordAttributeColorSettingPanel3.Data = null;
            this.wordAttributeColorSettingPanel3.Name = "wordAttributeColorSettingPanel3";
            // 
            // WordColorSettingDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.wordAttributeColorSettingPanel1);
            this.Controls.Add(this.wordAttributeColorSettingPanel2);
            this.Controls.Add(this.wordAttributeColorSettingPanel3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WordColorSettingDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private WordColorSettingPanel wordAttributeColorSettingPanel1;
        private WordColorSettingPanel wordAttributeColorSettingPanel2;
        private WordColorSettingPanel wordAttributeColorSettingPanel3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}