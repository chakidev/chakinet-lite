﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.GUICommon
{
    public class ErrorReportDialog : Form
    {
        public ErrorReportDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Exceptionの詳細をDetailにセットする
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="e"></param>
        public ErrorReportDialog(string msg, Exception e)
        {
            InitializeComponent();

            this.Message = msg;
            StringBuilder sb = new StringBuilder();
            Exception ee = e;
            string header = "";
            while (ee != null)
            {
                sb.Append(header);
                sb.Append(ee.Message);
                sb.AppendLine();
                ee = ee.InnerException;
                header += "> ";
            }
            sb.AppendLine();
            sb.Append("STACK TRACE:");
            sb.AppendLine();
            sb.Append(e.StackTrace);
            this.Detail = sb.ToString();
        }

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

        public string Message
        {
            set
            {
                this.message.Text = value;
            }
        }
        public string Detail
        {
            set
            {
                this.detail.Text = value;
            }
        }

        private Button button1;
        private TextBox message;
        private TextBox detail;
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.detail = new System.Windows.Forms.TextBox();
            this.message = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Location = new System.Drawing.Point(548, 386);
            this.button1.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(123, 41);
            this.button1.TabIndex = 0;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // detail
            // 
            this.detail.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.detail.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.detail.Location = new System.Drawing.Point(21, 85);
            this.detail.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.detail.Multiline = true;
            this.detail.Name = "detail";
            this.detail.ReadOnly = true;
            this.detail.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.detail.Size = new System.Drawing.Size(650, 285);
            this.detail.TabIndex = 2;
            this.detail.WordWrap = false;
            // 
            // message
            // 
            this.message.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.message.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.message.Location = new System.Drawing.Point(21, 21);
            this.message.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.message.Multiline = true;
            this.message.Name = "message";
            this.message.ReadOnly = true;
            this.message.Size = new System.Drawing.Size(650, 53);
            this.message.TabIndex = 1;
            // 
            // ErrorReportDialog
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(168F, 168F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.button1;
            this.ClientSize = new System.Drawing.Size(686, 442);
            this.Controls.Add(this.message);
            this.Controls.Add(this.detail);
            this.Controls.Add(this.button1);
            this.Font = new System.Drawing.Font("Lucida Sans Unicode", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ErrorReportDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Error";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
