using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.GUICommon;
using ChaKi.Entity.Search;
using System.Diagnostics;
using ChaKi.Entity.Corpora;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class TagSearchPane : UserControl
    {
        private TagSearchCondition m_Model;
        private List<PropertyBox> m_Boxes;

        public TagSearchPane( TagSearchCondition model )
        {
            InitializeComponent();

            m_Boxes = new List<PropertyBox>();

            m_Model = model;
            m_Model.ModelChanged += new EventHandler( this.ModelChangedHandler );

            this.UpdateView();
        }

        public void SetCondition(TagSearchCondition cond)
        {
            m_Model = cond;
            m_Model.ModelChanged += new EventHandler(this.ModelChangedHandler);
            UpdateView();
        }

        public TagSearchCondition GetCondition()
        {
            foreach (PropertyBox b in m_Boxes)
            {
                b.SynchronizeRange();
            }
            //this.UpdateView();
            return m_Model;
        }

        private void UpdateView()
        {
            Debug.WriteLine("TagSearchPane::UpdateView");
            this.Visible = false;

//            this.Visible = false;
//            this.SuspendLayout();
            foreach (PropertyBox b in m_Boxes)
            {
                b.CenterizedButtonClicked -= this.OnPropertyBoxCenterizedButtonClicked;
                b.DeleteClicked -= this.OnPropertyBoxDeleteClicked;
            }
            m_Boxes.Clear();

            this.Controls.Clear();

            this.Controls.Add(leftButton);
            foreach (LexemeCondition conditem in m_Model.LexemeConds)
            {
                PropertyBox box = new PropertyBox(conditem);
                m_Boxes.Add(box);
                this.Controls.Add(box);
                box.CenterizedButtonClicked += new EventHandler(this.OnPropertyBoxCenterizedButtonClicked);
                box.DeleteClicked += new EventHandler(this.OnPropertyBoxDeleteClicked);
            }
            this.Controls.Add(rightButton);

            RecalcLayout();
            Invalidate();
            //           this.ResumeLayout();
            //            this.Visible = true;

            this.Visible = true;
        }

        private void RecalcLayout()
        {
            int x = 5;
            int height = 10;
            if (m_Boxes.Count > 0)
            {
                height += (m_Boxes[0].Height + 10);
            }
            this.leftButton.Location = new Point(x, (height - this.leftButton.Height) / 2);
            x += this.leftButton.Width + 5;
            for (int i = 0; i < m_Boxes.Count; i++)
            {
                m_Boxes[i].Location = new Point(x, (height - m_Boxes[i].Height) / 2);
                x += (m_Boxes[i].Width + 5);
            }
            this.rightButton.Location = new Point(x, (height - this.rightButton.Height) / 2);
            x += this.rightButton.Width + 5;

            this.Width = x;
            this.Height = height;
        }

        void ModelChangedHandler(object sender, EventArgs e)
        {
            UpdateView();
        }

        /// <summary>
        /// ���{�^���������ꂽ�Ƃ��̏���
        /// ���Ƀ{�b�N�X��ǉ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void leftButton_Click(object sender, EventArgs e)
        {
            m_Model.InsertLexemeConditionAtLeft();
        }

        private void rightButton_Click(object sender, EventArgs e)
        {
            m_Model.InsertLexemeConditionAtRight();
        }

        /// <summary>
        /// PropertyBox��Centerized Button�i�Ԃ����������j�������ꂽ���̏����B
        /// ����Box��Pivot�\������B
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPropertyBoxCenterizedButtonClicked(object sender, EventArgs e)
        {
            int offset = 0;
            int diff = 0;
            foreach (PropertyBox b in m_Boxes)
            {
                if (b == sender)
                {
                    b.IsPivot = true;
                    offset = b.Range.Start;
                    diff = b.Range.End - b.Range.Start; // Pivot���K��(0,0)�ɂȂ�悤��.
                }
                else
                {
                    b.IsPivot = false;
                }
            }
            if (offset != 0)
            {
                foreach (PropertyBox b in m_Boxes)
                {
                    b.OffsetRange(-offset, diff);
                }
            }
            Invalidate(true);
        }

        /// <summary>
        /// PropertyBox����̃C�x���g�ʒm�ɂ��A����Box���폜����B
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPropertyBoxDeleteClicked(object sender, EventArgs e)
        {
            if (m_Boxes.Count == 1)
            {
                return;
            }
            int index = m_Boxes.IndexOf((PropertyBox)sender);
            m_Model.RemoveAt(index);
        }

        /// <summary>
        /// LexemeCondition��addCond�������}�[�W����.
        /// (Word Occurrence Search�Ō�̐��N���������邽�߂̏������쐬���邽�߂ɌĂ΂��j
        /// </summary>
        /// <param name="addCond"></param>
        public void MergeSearchCondition(List<LexemeCondition> addCond)
        {
            if (addCond.Count != m_Model.LexemeConds.Count)
            {
                throw new Exception("Mismatch in size of Lexeme Conditions.");
            }
            for (int i = 0; i < m_Model.LexemeConds.Count; i++)
            {
                // �I���W�i���Ɏw��̂���Property�̓I���W�i�����g�p�A
                // ����ȊO��addCond�Ɏw��̂���Property�͂�����g�p����B
                // ���̏����̓I���W�i���̂܂܁B
                m_Model.LexemeConds[i].Merge(addCond[i]);
            }
            UpdateView();
        }

        /// <summary>
        /// �X�N���[���ʒu��ScrollableControl�ɂ���ď���ɒ��������̂�h��.
        /// cf. http://social.msdn.microsoft.com/Forums/ja-JP/winforms/thread/285b1a48-ce21-47ea-80bf-5601d6014cf7
        /// </summary>
        /// <param name="activeControl"></param>
        /// <returns></returns>
        protected override Point ScrollToControl(Control activeControl)
        {
            return this.AutoScrollPosition;
            //return base.ScrollToControl(activeControl);
        }
    }
}
