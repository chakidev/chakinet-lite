using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Search;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class StringSearchPane : UserControl
    {
        private StringSearchCondition m_Model;

        public StringSearchPane(StringSearchCondition model)
        {
            InitializeComponent();

            SetCondition(model);
        }

        public void SetCondition(StringSearchCondition cond)
        {
            m_Model = cond;
            m_Model.OnModelChanged += new EventHandler(this.ModelChangedHandler);
            UpdateView();
        }

        public StringSearchCondition GetCondition()
        {
            Synchronize();
            return m_Model;
        }

        /// <summary>
        /// 現在のModelの内容によってControl表示内容を更新する。
        /// </summary>
        public void UpdateView()
        {
            this.textBox1.Text = m_Model.Pattern;
            this.checkBox1.Checked = m_Model.IsCaseSensitive;
            this.checkBox2.Checked = m_Model.IsRegexp;
        }

        /// <summary>
        /// 現在のControlの状態によってModel内容を更新する。
        /// </summary>
        private void Synchronize()
        {
            m_Model.Pattern = this.textBox1.Text;
            m_Model.IsCaseSensitive = this.checkBox1.Checked;
            m_Model.IsRegexp = this.checkBox2.Checked;
        }

        public void ModelChangedHandler(object sender, EventArgs e)
        {
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Enabled = !(checkBox2.Checked);
        }
    }
}
