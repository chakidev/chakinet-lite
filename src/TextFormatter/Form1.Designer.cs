namespace TextFormatter
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.button_convert = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.label4 = new System.Windows.Forms.Label();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button_selectOpenFolder = new System.Windows.Forms.Button();
            this.button_selectOpenFile = new System.Windows.Forms.Button();
            this.textBox_infile = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button_selectSaveFolder = new System.Windows.Forms.Button();
            this.button_selectSaveFile = new System.Windows.Forms.Button();
            this.textBox_outfile = new System.Windows.Forms.Label();
            this.folderBrowserDialog2 = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.radioButton_ipadic = new System.Windows.Forms.RadioButton();
            this.radioButton_Unidic = new System.Windows.Forms.RadioButton();
            this.checkBox_doDepParse = new System.Windows.Forms.CheckBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.checkBox_doSeparateLine = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // button_convert
            // 
            this.button_convert.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button_convert.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.button_convert.Location = new System.Drawing.Point(70, 235);
            this.button_convert.Name = "button_convert";
            this.button_convert.Size = new System.Drawing.Size(293, 23);
            this.button_convert.TabIndex = 5;
            this.button_convert.Text = "変換";
            this.button_convert.UseVisualStyleBackColor = true;
            this.button_convert.Click += new System.EventHandler(this.button_convert_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "テキストファイル(*.txt)|*.txt|全てのファイル(*)|*";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "MeCab ファイル(*.mecab)|*.mecab|CaboCha ファイル(*.cabocha)|*.cabocha";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 190);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(0, 12);
            this.label4.TabIndex = 11;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(45, 229);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 12);
            this.label2.TabIndex = 16;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.button_selectOpenFolder);
            this.groupBox1.Controls.Add(this.button_selectOpenFile);
            this.groupBox1.Controls.Add(this.textBox_infile);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(441, 66);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "変換元ファイル/フォルダ";
            // 
            // button_selectOpenFolder
            // 
            this.button_selectOpenFolder.AutoSize = true;
            this.button_selectOpenFolder.Location = new System.Drawing.Point(125, 16);
            this.button_selectOpenFolder.Name = "button_selectOpenFolder";
            this.button_selectOpenFolder.Size = new System.Drawing.Size(104, 23);
            this.button_selectOpenFolder.TabIndex = 20;
            this.button_selectOpenFolder.Text = "フォルダを指定";
            this.button_selectOpenFolder.UseVisualStyleBackColor = true;
            this.button_selectOpenFolder.Click += new System.EventHandler(this.button_selectOpenFolder_Click);
            // 
            // button_selectOpenFile
            // 
            this.button_selectOpenFile.Location = new System.Drawing.Point(14, 16);
            this.button_selectOpenFile.Name = "button_selectOpenFile";
            this.button_selectOpenFile.Size = new System.Drawing.Size(104, 23);
            this.button_selectOpenFile.TabIndex = 19;
            this.button_selectOpenFile.Text = "ファイルを指定";
            this.button_selectOpenFile.UseVisualStyleBackColor = true;
            this.button_selectOpenFile.Click += new System.EventHandler(this.button_selectOpenFile_Click);
            // 
            // textBox_infile
            // 
            this.textBox_infile.AutoSize = true;
            this.textBox_infile.Location = new System.Drawing.Point(18, 46);
            this.textBox_infile.Name = "textBox_infile";
            this.textBox_infile.Size = new System.Drawing.Size(0, 12);
            this.textBox_infile.TabIndex = 18;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.button_selectSaveFolder);
            this.groupBox2.Controls.Add(this.button_selectSaveFile);
            this.groupBox2.Controls.Add(this.textBox_outfile);
            this.groupBox2.Location = new System.Drawing.Point(12, 149);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(441, 66);
            this.groupBox2.TabIndex = 19;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "変換結果格納先ファイル/フォルダ";
            // 
            // button_selectSaveFolder
            // 
            this.button_selectSaveFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.button_selectSaveFolder.Location = new System.Drawing.Point(125, 16);
            this.button_selectSaveFolder.Name = "button_selectSaveFolder";
            this.button_selectSaveFolder.Size = new System.Drawing.Size(102, 23);
            this.button_selectSaveFolder.TabIndex = 17;
            this.button_selectSaveFolder.Text = "フォルダを指定";
            this.button_selectSaveFolder.UseVisualStyleBackColor = true;
            this.button_selectSaveFolder.Click += new System.EventHandler(this.button_selectSaveFolder_Click);
            // 
            // button_selectSaveFile
            // 
            this.button_selectSaveFile.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.button_selectSaveFile.Location = new System.Drawing.Point(14, 16);
            this.button_selectSaveFile.Name = "button_selectSaveFile";
            this.button_selectSaveFile.Size = new System.Drawing.Size(102, 23);
            this.button_selectSaveFile.TabIndex = 16;
            this.button_selectSaveFile.Text = "ファイルを指定";
            this.button_selectSaveFile.UseVisualStyleBackColor = true;
            this.button_selectSaveFile.Click += new System.EventHandler(this.button_selectSaveFile_Click);
            // 
            // textBox_outfile
            // 
            this.textBox_outfile.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBox_outfile.AutoSize = true;
            this.textBox_outfile.Location = new System.Drawing.Point(17, 46);
            this.textBox_outfile.Name = "textBox_outfile";
            this.textBox_outfile.Size = new System.Drawing.Size(0, 12);
            this.textBox_outfile.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.radioButton_ipadic);
            this.groupBox3.Controls.Add(this.radioButton_Unidic);
            this.groupBox3.Location = new System.Drawing.Point(13, 85);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(115, 58);
            this.groupBox3.TabIndex = 20;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "使用する辞書";
            // 
            // radioButton_ipadic
            // 
            this.radioButton_ipadic.AutoSize = true;
            this.radioButton_ipadic.Location = new System.Drawing.Point(13, 36);
            this.radioButton_ipadic.Name = "radioButton_ipadic";
            this.radioButton_ipadic.Size = new System.Drawing.Size(60, 16);
            this.radioButton_ipadic.TabIndex = 1;
            this.radioButton_ipadic.Text = "IPADIC";
            this.radioButton_ipadic.UseVisualStyleBackColor = true;
            this.radioButton_ipadic.CheckedChanged += new System.EventHandler(this.radioButton_Unidic_CheckedChanged);
            // 
            // radioButton_Unidic
            // 
            this.radioButton_Unidic.AutoSize = true;
            this.radioButton_Unidic.Checked = true;
            this.radioButton_Unidic.Location = new System.Drawing.Point(13, 18);
            this.radioButton_Unidic.Name = "radioButton_Unidic";
            this.radioButton_Unidic.Size = new System.Drawing.Size(57, 16);
            this.radioButton_Unidic.TabIndex = 0;
            this.radioButton_Unidic.TabStop = true;
            this.radioButton_Unidic.Text = "UniDic";
            this.radioButton_Unidic.UseVisualStyleBackColor = true;
            this.radioButton_Unidic.CheckedChanged += new System.EventHandler(this.radioButton_Unidic_CheckedChanged);
            // 
            // checkBox_doDepParse
            // 
            this.checkBox_doDepParse.AutoSize = true;
            this.checkBox_doDepParse.Location = new System.Drawing.Point(137, 121);
            this.checkBox_doDepParse.Name = "checkBox_doDepParse";
            this.checkBox_doDepParse.Size = new System.Drawing.Size(118, 16);
            this.checkBox_doDepParse.TabIndex = 21;
            this.checkBox_doDepParse.Text = "係り受け解析も行う";
            this.checkBox_doDepParse.UseVisualStyleBackColor = true;
            this.checkBox_doDepParse.CheckedChanged += new System.EventHandler(this.checkBox_doDepParse_CheckedChanged);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 300;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // checkBox_doSeparateLine
            // 
            this.checkBox_doSeparateLine.AutoSize = true;
            this.checkBox_doSeparateLine.Checked = true;
            this.checkBox_doSeparateLine.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox_doSeparateLine.Location = new System.Drawing.Point(137, 85);
            this.checkBox_doSeparateLine.Name = "checkBox_doSeparateLine";
            this.checkBox_doSeparateLine.Size = new System.Drawing.Size(100, 16);
            this.checkBox_doSeparateLine.TabIndex = 22;
            this.checkBox_doSeparateLine.Text = "改行処理を行う";
            this.checkBox_doSeparateLine.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(465, 273);
            this.Controls.Add(this.checkBox_doSeparateLine);
            this.Controls.Add(this.checkBox_doDepParse);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.button_convert);
            this.Name = "Form1";
            this.Text = "ChaKi.NET インポート支援ツール rev. 2010/11/20";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBox_infile_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.textBox_infile_DragEnter);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_convert;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button_selectOpenFolder;
        private System.Windows.Forms.Button button_selectOpenFile;
        private System.Windows.Forms.Label textBox_infile;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button_selectSaveFolder;
        private System.Windows.Forms.Button button_selectSaveFile;
        private System.Windows.Forms.Label textBox_outfile;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton radioButton_ipadic;
        private System.Windows.Forms.RadioButton radioButton_Unidic;
        private System.Windows.Forms.CheckBox checkBox_doDepParse;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.CheckBox checkBox_doSeparateLine;
    }
}

