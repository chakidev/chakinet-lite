using System;
using System.Windows.Forms;
using ChaKi.Entity.Search;
using ChaKi.Entity.Corpora;
using System.Collections.Generic;
using ChaKi.Common;
using ChaKi.Common.Settings;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class CollocationPane : UserControl
    {
        private CollocationCondition m_Model;
        private Dictionary<LP, int> m_FromLPMapping;

        public CollocationPane(CollocationCondition model)
        {
            InitializeComponent();

            UpdateListSetting();
            SetCondition(model);

            UpdateControlStates(model.CollType);

            PropertyBoxSettings.Instance.SettingChanged += new EventHandler(Instance_SettingChanged);
        }

        public void SetCondition(CollocationCondition cond)
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


        public CollocationCondition GetCondition()
        {
            Synchronize();
            return m_Model;
        }

        /// <summary>
        /// 現在のModelの内容によってControl表示内容を更新する。
        /// </summary>
        public void UpdateView()
        {
            this.comboBox1.SelectedIndex = (int)m_Model.CollType;
            this.numericUpDown1.Value = m_Model.Lwsz;
            this.numericUpDown2.Value = m_Model.Rwsz;
            this.numericUpDown3.Value = m_Model.MinFrequency;
            this.numericUpDown4.Value = m_Model.MinLength;
            this.numericUpDown5.Value = m_Model.MaxGapLen;
            this.numericUpDown6.Value = m_Model.MaxGapCount;
            this.checkBox1.Checked = m_Model.ExactGC;
            this.textBox1.Text = string.Join(" ", m_Model.Stopwords);

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
            m_Model.CollType = (CollocationType)this.comboBox1.SelectedIndex;
            m_Model.Lwsz = (int)this.numericUpDown1.Value;
            m_Model.Rwsz = (int)this.numericUpDown2.Value;
            m_Model.MinFrequency = (uint)this.numericUpDown3.Value;
            m_Model.MinLength = (int)this.numericUpDown4.Value;
            m_Model.MaxGapLen = (int)this.numericUpDown5.Value;
            m_Model.MaxGapCount = (int)this.numericUpDown6.Value;
            m_Model.ExactGC = this.checkBox1.Checked;
            m_Model.Stopwords = this.textBox1.Text.Split(new char[] { ' ' });

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
            UpdateControlStates((CollocationType)this.comboBox1.SelectedIndex);
        }

        private void UpdateControlStates(CollocationType t)
        {
            if (t == CollocationType.NgramL || t == CollocationType.NgramR)
            {
                // N-gramの場合はProperty選択とGapパラメータ指定とWindowSizeを無効化
                this.listBox1.Enabled = false;
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.label2.Enabled = false;
                this.numericUpDown1.Enabled = false;
                this.label3.Enabled = false;
                this.numericUpDown2.Enabled = false;
                this.label4.Enabled = true;
                this.numericUpDown3.Enabled = true;
                this.label5.Enabled = true;
                this.numericUpDown4.Enabled = true;
                this.label6.Enabled = false;
                this.numericUpDown5.Enabled = false;
                this.label7.Enabled = false;
                this.numericUpDown6.Enabled = false;
                this.checkBox1.Enabled = false;
                this.label8.Enabled = false;
                this.textBox1.Enabled = false;
            }
            else if (t == CollocationType.FSM)
            {
                // FSMの場合はWindowSize以外全部有効
                this.listBox1.Enabled = true;
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.label2.Enabled = false;
                this.numericUpDown1.Enabled = false;
                this.label3.Enabled = false;
                this.numericUpDown2.Enabled = false;
                this.label4.Enabled = true;
                this.numericUpDown3.Enabled = true;
                this.label5.Enabled = true;
                this.numericUpDown4.Enabled = true;
                this.label6.Enabled = true;
                this.numericUpDown5.Enabled = true;
                this.label7.Enabled = true;
                this.numericUpDown6.Enabled = true;
                this.checkBox1.Enabled = true;
                this.label8.Enabled = true;
                this.textBox1.Enabled = true;
            }
            else
            {
                // Raw, MIの場合はWindowSize以外無効
                this.listBox1.Enabled = true;
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.label2.Enabled = true;
                this.numericUpDown1.Enabled = true;
                this.label3.Enabled = true;
                this.numericUpDown2.Enabled = true;
                this.label4.Enabled = false;
                this.numericUpDown3.Enabled = false;
                this.label5.Enabled = false;
                this.numericUpDown4.Enabled = false;
                this.label6.Enabled = false;
                this.numericUpDown5.Enabled = false;
                this.label7.Enabled = false;
                this.numericUpDown6.Enabled = false;
                this.checkBox1.Enabled = false;
                this.label8.Enabled = false;
                this.textBox1.Enabled = false;
            }
        }
    }
}
