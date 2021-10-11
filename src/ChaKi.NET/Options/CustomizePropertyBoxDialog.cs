using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Common;
using ChaKi.Common.Settings;

namespace ChaKi.Options
{
    public partial class CustomizePropertyBoxDialog : Form
    {
        public CustomizePropertyBoxDialog()
        {
            InitializeComponent();

            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Columns[0].DataPropertyName = "TagName";
            this.dataGridView1.Columns[1].DataPropertyName = "DisplayName";
            this.dataGridView1.Columns[2].DataPropertyName = "IsVisible";
            this.dataGridView1.Columns[3].DataPropertyName = "IsKwicRow1";
            this.dataGridView1.Columns[4].DataPropertyName = "IsKwicRow2";
        }

        public IList<PropertyBoxItemSetting> Model
        {
            set
            {
                this.bindingSource1.DataSource = value;
            }
            get
            {
                return this.bindingSource1.DataSource as IList<PropertyBoxItemSetting>;
            }
        }

        public bool UseShortPOS
        {
            set
            {
                this.checkBox1.Checked = value;
            }
            get
            {
                return this.checkBox1.Checked;
            }
        }

        /// <summary>
        /// Move Up Current Row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection rows = this.dataGridView1.SelectedRows;
            if (rows.Count != 1 || rows[0].Index == 0)
            {
                return;
            }
            int row = rows[0].Index;
            PropertyBoxItemSetting item = Model[row];
            Model.RemoveAt(row);
            Model.Insert(row - 1, item);
            this.dataGridView1.Rows[row - 1].Selected = true;
        }

        /// <summary>
        /// Move Down current Row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection rows = this.dataGridView1.SelectedRows;
            if (rows.Count != 1 || rows[0].Index == this.Model.Count - 1)
            {
                return;
            }
            int row = rows[0].Index;
            PropertyBoxItemSetting item = Model[row];
            Model.RemoveAt(row);
            Model.Insert(row + 1, item);
            this.dataGridView1.Rows[row + 1].Selected = true;
        }

        /// <summary>
        /// Reset to Default
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            PropertyBoxSettings.Default(this.Model);
            this.checkBox1.Checked = true;
            this.dataGridView1.Refresh();
        }

        // KWIC Row1 & KWIC Row2カラムのラジオボタン動作
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //Console.WriteLine("{0},{1}", e.ColumnIndex, e.RowIndex);
            var model = this.Model;
            if (model == null)
            {
                return;
            }
            int r = e.RowIndex;
            int c = e.ColumnIndex;
            if (c != 3 && c != 4)
            {
                return;
            }
            if (c == 3)
            {
                for (int i = 0; i < model.Count; i++)
                {
                    model[i].IsKwicRow1 = (i == r);
                }
                this.bindingSource1.ResetBindings(false);
            }
            else if (c == 4)
            {
                for (int i = 0; i < model.Count; i++)
                {
                    model[i].IsKwicRow2 = (i == r);
                }
                this.bindingSource1.ResetBindings(false);
            }
        }
    }
}
