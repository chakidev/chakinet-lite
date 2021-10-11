using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Common
{
    public partial class EditAttributesDialog : Form
    {
        private DataTable m_DataTable;

        public EditAttributesDialog()
        {
            InitializeComponent();

            m_DataTable = new DataTable();
            m_DataTable.Columns.Add("Key");
            m_DataTable.Columns.Add("Value");
            this.dataGridView1.DataSource = m_DataTable;
        }

        public void SetData(Dictionary<string, string> data)
        {
            m_DataTable.Rows.Clear();
            foreach (var pair in data)
            {
                var row = m_DataTable.NewRow();
                row[0] = pair.Key;
                row[1] = pair.Value;
                m_DataTable.Rows.Add(row);
            }
        }

        public void GetData(Dictionary<string, string> data)
        {
            data.Clear();
            foreach (DataRow row in m_DataTable.Rows)
            {
                try
                {
                    var k = ((string)row[0]).Trim();
                    var v = (string)row[1];
                    if (k.Length > 0)
                    {
                        if (!data.ContainsKey(k))
                        {
                            data.Add(k, v);
                        }
                        else
                        {
                            MessageBox.Show("Duplicate Key - ignored.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // do nothing
                }
            }
        }
    }
}
