using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Search;
using ChaKi.GUICommon;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class FilterPane : UserControl
    {
        private FilterCondition m_Model;

        internal FilterButton FilterButton { get; set; }  // ����Pane�ŊǗ����ACommand Panel�ɕ\�������ԃ{�^��m_Model.AllEnabled�̒l�𔽉f����B

        internal ProjectSelector ProjectSelector { get; set; }    // MainForm��Toolbar�ɂ���ProjectSelector�̎Q��. MainForm�ɂ���Đڑ������.

        public FilterPane(FilterCondition model, SentenceSearchCondition corpusCond /* �Q�Ƃ̂� */)
        {
            m_Model = model;
            m_Model.OnModelChanged += new EventHandler( this.ModelChangedHandler );

            InitializeComponent();

            this.FilterButton = new FilterButton();
            this.FilterButton.IsOn = m_Model.AllEnabled;
            this.FilterButton.Click += new EventHandler(FilterButton_Click);

            this.filterControl.CorpusCond = corpusCond;

            UpdateView();
        }

        public void SetCondition(FilterCondition cond)
        {
            m_Model = cond;
            m_Model.OnModelChanged += new EventHandler(this.ModelChangedHandler);
            UpdateView();
        }

        public FilterCondition GetCondition()
        {
            Synchronize();
            return m_Model;
        }

        public void PerformFilterAutoIncrement()
        {
            if (m_Model.AllEnabled
                && m_Model.ResultsetFilter.FetchType == FetchType.Incremental
                && m_Model.ResultsetFilter.IsAutoIncrement)
            {
                m_Model.ResultsetFilter.StartAt += m_Model.ResultsetFilter.Max;
                UpdateView();
            }
        }

        private void UpdateView()
        {
            bool b = m_Model.AllEnabled;
            this.checkBox1.Checked = b;
            this.FilterButton.IsOn = b;
            this.filterControl.Enabled = b;

            this.filterControl.UpdateView(m_Model);

            if (this.ProjectSelector != null)
            {
                this.ProjectSelector.Text = m_Model.TargetProjectId.ToString();
            }
        }

        /// <summary>
        /// ���݂�Control�̏�Ԃɂ����Model���e���X�V����B
        /// </summary>
        private void Synchronize()
        {
            m_Model.AllEnabled = this.checkBox1.Checked;
            this.filterControl.Synchronize(m_Model);

            int id = 0;
            if (this.ProjectSelector != null)
            {
                Int32.TryParse(this.ProjectSelector.Text, out id);
            }
            m_Model.TargetProjectId = id;
        }

        public void ModelChangedHandler(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bool b = this.checkBox1.Checked;

            m_Model.AllEnabled = b;
            this.filterControl.Enabled = b;
            this.FilterButton.IsOn = b;
        }

        // Command Panel�ł�FilterButton�N���b�N�C�x���g�n���h��
        void FilterButton_Click(object sender, EventArgs e)
        {
            bool b = !this.FilterButton.IsOn;

            m_Model.AllEnabled = b;
            this.checkBox1.Checked = b;
            this.filterControl.Enabled = b;
            this.FilterButton.IsOn = b;
        }
    }
}
