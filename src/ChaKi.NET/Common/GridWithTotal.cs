using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace ChaKi.GUICommon
{
    public class GridWithTotal : DataGridView
    {
        [Browsable(true)]
        public event EventHandler UpdatingTotal;

        private Timer timer1;
        private System.ComponentModel.IContainer components;
    
        public GridWithTotal()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // GridWithTotal
            // 
            this.AllowDrop = true;
            this.AllowUserToAddRows = false;
            this.AllowUserToDeleteRows = false;
            this.AllowUserToOrderColumns = true;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("MS UI Gothic", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.DefaultCellStyle = dataGridViewCellStyle2;
            this.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.GridColor = System.Drawing.Color.Snow;
            this.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.RowHeadersWidth = 60;
            this.RowTemplate.Height = 21;
            this.Sorted += new System.EventHandler(this.GridWithTotal_Sorted);
            this.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.GridWithTotal_RowPostPaint);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        public void InitializeGrid()
        {
            this.Rows.Clear();
            this.Columns.Clear();

            // TOTAL行を生成する
            if (this.ColumnCount == 0)
            {
                this.ColumnCount = 1; // dummy
            }
            this.Rows.Add();
            this.Rows[0].Tag = "TOTAL";   // TOTAL行であることをTagによって記憶させる。(cf. Sorted event)
            this.Rows[0].Frozen = true;
        }

        public void SuspendUpdatingTotal()
        {
            this.timer1.Stop();
        }

        public void ResumeUpdatingTotal()
        {
            this.timer1.Start();
        }

        /// <summary>
        /// 行データをクリアし、TOTAL行のみを作成する
        /// </summary>
        public void ResetRows()
        {
            this.Rows.Clear();
            // TOTAL行を生成する
            this.Rows.Add();
            this.Rows[0].Tag = "TOTAL";   // TOTAL行であることをTagによって記憶させる。(cf. Sorted event)
            this.Rows[0].Frozen = true;
        }


        public static string RowHeader(int r)
        {
            if (r == 0)
            {
                return "TOTAL";
            }
            else
            {
                return string.Format("{0}", r);
            }
        }

        /// <summary>
        /// ソート後処理：TOTAL行が末尾に移動されていたら先頭に戻す。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridWithTotal_Sorted(object sender, EventArgs e)
        {
            int nrows = this.Rows.Count;
            DataGridViewRow firstRow = this.Rows[0];
            DataGridViewRow lastRow = this.Rows[nrows - 1];
            // 1行目がTOTALになっていなければ、
            object rowtag = firstRow.Tag;
            if (!(rowtag is string && ((string)rowtag).Equals("TOTAL")))
            {
                // 末尾がTOTALであれば、それを先頭に移動
                rowtag = lastRow.Tag;
                if (rowtag is string && ((string)rowtag).Equals("TOTAL"))
                {
                    firstRow.Frozen = false;
                    this.Rows.InsertCopy(nrows - 1, 0);
                    for (int i = 0; i < this.Columns.Count; i++)
                    {
                        this[i, 0].Value = this[i, nrows].Value;
                    }
                    this.Rows.RemoveAt(nrows);
                    this.Rows[0].Frozen = true;
                }
            }
        }

        /// <summary>
        /// 行ヘッダ描画をカスタマイズする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridWithTotal_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            Rectangle rect = new Rectangle(
              e.RowBounds.Location.X,
              e.RowBounds.Location.Y,
              this.RowHeadersWidth - 4,
              e.RowBounds.Height);

            string text = RowHeader(e.RowIndex);
            TextRenderer.DrawText(
              e.Graphics,
              text,
              this.RowHeadersDefaultCellStyle.Font,
              rect,
              this.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (UpdatingTotal != null)
            {
                UpdatingTotal(this,e);
            }
        }
    }
}
