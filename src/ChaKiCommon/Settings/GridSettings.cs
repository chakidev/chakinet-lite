using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Common.Settings
{
    public class GridSettings
    {
        public List<int> ColWidths;
        private DataGridView m_DataGridView;

        public void BindTo(DataGridView dg)
        {
            m_DataGridView = dg;
            // 最初にBindされた時点で、状態を復元反映する。
            // （但し、ColWidthsの数が一致している場合のみ）
            if (this.ColWidths != null && dg.Columns.Count == this.ColWidths.Count)
            {
                for (int i = 0; i < this.ColWidths.Count; i++)
                {
                    dg.Columns[i].Width = this.ColWidths[i];
                }
            }
            else //一致していなければ、ColWidthsリストを新規作成し、現在の状態に合わせる。
            {
                this.ColWidths = new List<int>(dg.Columns.Count);
                foreach (DataGridViewColumn col in dg.Columns)
                {
                    this.ColWidths.Add(col.Width);
                }
            }
            // これ以降のDataGridViewのColWidthChangedイベントをListenし、
            // ColWidthsに即時反映するようにする。
            dg.ColumnWidthChanged += new DataGridViewColumnEventHandler(ColumnWidthChangedHandler);
        }

        void ColumnWidthChangedHandler(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column.Index < this.ColWidths.Count)
            {
                this.ColWidths[e.Column.Index] = e.Column.Width;
            }
            else
            {
                this.ColWidths = new List<int>(m_DataGridView.Columns.Count);
                foreach (DataGridViewColumn col in m_DataGridView.Columns)
                {
                    this.ColWidths.Add(col.Width);
                }
            }
        }
    }
}
