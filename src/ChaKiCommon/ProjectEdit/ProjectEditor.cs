using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChaKi.Common.ProjectEdit
{
    public partial class ProjectEditor : Form
    {
        public ProjectEditor()
        {
            InitializeComponent();
        }

        private void ProjectEditor_Load(object sender, EventArgs e)
        {
            // for Test
            this.dataGridView1.Rows.Add();
            this.dataGridView1[0, 0].Value = "0";
            this.dataGridView1[1, 0].Value = "CabochaTagSet";
            this.dataGridView1[2, 0].Value = "1";
            this.dataGridView1[3, 0].Value = true;

            this.dataGridView1.Rows.Add();
            this.dataGridView1[0, 1].Value = "1";
            this.dataGridView1[1, 1].Value = "PredicateArgumentStructureTagSet";
            this.dataGridView1[2, 1].Value = "1";
            this.dataGridView1[3, 1].Value = false;


            this.dataGridView1.Rows.Add();
            this.dataGridView1[0, 2].Value = "2";
            this.dataGridView1[1, 2].Value = "ReferenceTagSet";
            this.dataGridView1[2, 2].Value = "1";
            this.dataGridView1[3, 2].Value = false;
        }
    }
}
