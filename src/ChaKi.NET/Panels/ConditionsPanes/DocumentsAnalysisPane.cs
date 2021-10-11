using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.DocumentAnalysis;
using ChaKi.Entity.Corpora;
using ChaKi.Common.Settings;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class DocumentsAnalysisPane : UserControl
    {
        private DocumentsAnalysisCondition m_Model;
        private Dictionary<LP, int> m_FromLPMapping;

        public DocumentsAnalysisPane(DocumentsAnalysisCondition model)
        {
            InitializeComponent();

            UpdateListSetting();
            SetCondition(model);

            UpdateControlStates(model.AnalysisType);

            PropertyBoxSettings.Instance.SettingChanged += new EventHandler(Instance_SettingChanged);
        }

        public void SetCondition(DocumentsAnalysisCondition cond)
        {
            m_Model = cond;
            m_Model.ModelChanged += new EventHandler(this.OnModelChanged);
            UpdateView();
        }

        private void UpdateListSetting()
        {
            m_FromLPMapping = new Dictionary<LP, int>();
            // デフォルトのリスト項目（Corpusが決定されれば、その設定に従って再設定する）
            this.listBox1.Items.Clear();
            int row = 0;
            foreach (PropertyBoxItemSetting setting in PropertyBoxSettings.Instance.Settings)
            {
                if (!setting.IsVisible)
                {
                    continue;
                }
                this.listBox1.Items.Add(setting.DisplayName);
                LP? lp = Lexeme.FindProperty(setting.TagName);
                if (!lp.HasValue)
                {
                    continue;
                }
                m_FromLPMapping.Add(lp.Value, row);
                row++;
            }
            this.listBox1.TopIndex = 0;
        }

        void Instance_SettingChanged(object sender, EventArgs e)
        {
            UpdateListSetting();
            UpdateView();
        }


        public DocumentsAnalysisCondition GetCondition()
        {
            Synchronize();
            return m_Model;
        }

        /// <summary>
        /// 現在のModelの内容によってControl表示内容を更新する。
        /// </summary>
        public void UpdateView()
        {
            this.comboBox1.SelectedIndex = (int)m_Model.AnalysisType;
            this.numericUpDown1.Value = m_Model.N;

            this.listBox1.ClearSelected();
            for (int i = 0; i < Lexeme.PropertyName.Count; i++)
            {
                if (!m_Model.Filter.IsFiltered((LP)i))
                {
                    int row;
                    if (m_FromLPMapping.TryGetValue((LP)i, out row))
                    {
                        this.listBox1.SelectedIndices.Add(row);
                    }
                }
            }
            this.listBox1.TopIndex = 0;
        }

        /// <summary>
        /// 現在のControlの状態によってModel内容を更新する。
        /// </summary>
        private void Synchronize()
        {
            m_Model.AnalysisType = (DocumentsAnalysisTypes)this.comboBox1.SelectedIndex;
            m_Model.N = (int)this.numericUpDown1.Value;

            m_Model.Filter.Reset();
            for (int i = 0; i < Lexeme.PropertyName.Count; i++)
            {
                int row;
                if (m_FromLPMapping.TryGetValue((LP)i, out row))
                {
                    if (!this.listBox1.SelectedIndices.Contains(row))
                    {
                        m_Model.Filter.SetFiltered((LP)i);
                    }
                }
            }
        }

        void OnModelChanged(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Handler for "Select All"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.listBox1.Items.Count; i++)
            {
                this.listBox1.SelectedIndices.Add(i);
            }
        }

        /// <summary>
        /// Handler for "Clear All"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            this.listBox1.ClearSelected();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlStates((DocumentsAnalysisTypes)this.comboBox1.SelectedIndex);
        }

        private void UpdateControlStates(DocumentsAnalysisTypes t)
        {
            if (t == DocumentsAnalysisTypes.WordList)
            {
                this.numericUpDown1.Enabled = false;
            }
            else if (t == DocumentsAnalysisTypes.NgramList)            {
                // N-gramの場合、N を有効化
                this.numericUpDown1.Enabled = true;
            }

        }
    }
}
