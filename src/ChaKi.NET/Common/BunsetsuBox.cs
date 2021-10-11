using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Search;
using ChaKi.Common;
using PopupControl;
using ChaKi.Common.Widgets;

namespace ChaKi.GUICommon
{
    public partial class BunsetsuBox : UserControl
    {
        // Model
        private List<LexemeCondition> m_Model;
        public string SegmentTag { get; set; }
        public Dictionary<string, string> SegmentAttributes { get; set;}

        private List<PropertyBox> m_PropertyBoxes;
        private List<RelationButton> m_Buttons;
        private int m_id;
        private EditAttributesDialog m_EditAttributesDlg;
        private DpiAdjuster m_DpiAdjuster;
        private int m_XMargin = 5;

        public event MouseEventHandler MouseDownTransferring;
        public event EventHandler CenterlizedButtonClicked;
        public event EventHandler DeleteClicked;
        public event EventHandler PropertyBoxDeleteClicked;
        public event RelationCommandEventHandler RelationCommandClicked;
        public event EventHandler PropertyBoxSegmentTagChanged;

        public List<PropertyBox> Boxes
        {
            get
            {
                return m_PropertyBoxes;
            }
        }

        public BunsetsuBox()
        {
            m_id = 0;
            m_PropertyBoxes = new List<PropertyBox>();
            m_Buttons = new List<RelationButton>();
            m_EditAttributesDlg = new EditAttributesDialog();
            InitializeComponent();

            m_DpiAdjuster = new DpiAdjuster((xscale, yscale) => {
                this.textBox1.Width = (int)((float)this.textBox1.Width * xscale);
                this.textBox1.Height = (int)((float)this.textBox1.Height * yscale);
                this.button1.Width = (int)((float)this.button1.Width * xscale);
                this.button1.Height = (int)((float)this.button1.Height * yscale);
                this.button1.Location = new System.Drawing.Point((int)(4 + 63 * xscale), 2);
                m_XMargin = (int)(m_XMargin * xscale);
            });
            using (var g = this.CreateGraphics())
            {
                m_DpiAdjuster.Adjust(g);
            }
        }


        public BunsetsuBox(int bunsetsuID, string segmentTag, Dictionary<string,string> segmentAttrs, List<LexemeCondition> model)
            :this()
        {
            m_id = bunsetsuID;
            m_Model = model;
            this.SegmentTag = segmentTag;
            this.SegmentAttributes = segmentAttrs;
            this.BackColor = CyclicColorTable.GetColor(bunsetsuID);

            UpdateView();
        }

        public int ID
        {
            get { return m_id; }
        }

        public void UpdateView()
        {
            this.Controls.Clear();
            m_PropertyBoxes.Clear();
            m_Buttons.Clear();

            if (m_Model == null)
            {
                return;
            }
            // Segment Tag Textboxを追加する
            this.Controls.Add(this.textBox1);
            this.textBox1.Text = this.SegmentTag;
            this.Controls.Add(this.button1);
            m_EditAttributesDlg.SetData(this.SegmentAttributes);
            this.button1.BackColor = (this.SegmentAttributes.Count > 0) ? Color.Crimson : Color.DodgerBlue;

            // PropertyBoxとボタンを追加する
            int n = 0;
            foreach (LexemeCondition lexcond in m_Model)
            {
                // Box
                PropertyBox box = new PropertyBox(lexcond);
                box.ShowRange = false;
                box.MouseDownTransferring += new MouseEventHandler(this.OnMouseDownTransferring);
                box.CenterizedButtonClicked += new EventHandler(OnCenterizedButtonClicked);
                box.DeleteClicked +=new EventHandler(OnPropertyBoxDeleteClicked);
                this.Controls.Add(box);
                m_PropertyBoxes.Add(box);
                // Button (Boxの左)
                RelationButton button = new RelationButton(m_id, n);
                if (n == 0)
                {
                    button.Style = RelationButtonStyle.Leftmost;
                }
                button.Text = new String(lexcond.LeftConnection, 1);
                button.OnCommandEvent += new RelationCommandEventHandler(OnRelationCommandClicked);
                this.Controls.Add(button);
                m_Buttons.Add(button);
                n++;
            }
            // 末尾のButton
            RelationButton rbutton = new RelationButton(m_id, n);
            if (n > 0 && n - 1 < m_Model.Count)
            {
                rbutton.Text = new String(m_Model[n-1].RightConnection, 1);
            }
            rbutton.Style = RelationButtonStyle.Rightmost;
            rbutton.OnCommandEvent += new RelationCommandEventHandler(OnRelationCommandClicked);
            this.Controls.Add(rbutton);
            m_Buttons.Add(rbutton);

            this.RecalcLayout();
        }

        private void RecalcLayout()
        {
            int x = 5;
            int y = 5;

            int height = 15;
            if (m_PropertyBoxes.Count > 0)
            {
                height += m_PropertyBoxes[0].Height;
            }
            int maxCount = Math.Max(m_PropertyBoxes.Count, m_Buttons.Count - 1);
            if (maxCount == 0)
            {
                x = 120;
                height = 34;
            }
            for (int i = 0; i < maxCount; i++)
            {
                RelationButton button = m_Buttons[i];
                button.Location = new Point(x, (height-button.Height) / 2);
                x += (button.Width + m_XMargin);
                PropertyBox box = m_PropertyBoxes[i];
                box.Location = new Point(x, y);
                x += (box.Width + m_XMargin);
            }
            RelationButton rbutton = m_Buttons[m_Buttons.Count-1];
            rbutton.Location = new Point(x, (height - rbutton.Height) / 2);
            x += (rbutton.Width + m_XMargin);
            this.Width = x;
            this.Height = height;
        }

        public Point GetLinkPoint()
        {
            return new Point((this.Left+this.Right)/2, this.Bottom);
        }

        public int GetIndexOfPropertyBox(PropertyBox box)
        {
            for (int i = 0; i < m_PropertyBoxes.Count; i++)
            {
                if (m_PropertyBoxes[i] == box)
                {
                    return i;
                }
            }
            return -1;
        }

        void OnCenterizedButtonClicked(object sender, EventArgs e)
        {
            // 上位(DepSearchPane)のハンドラに転送
            if (this.CenterlizedButtonClicked != null)
            {
                this.CenterlizedButtonClicked(sender, e);
            }
        }

        void OnPropertyBoxDeleteClicked(object sender, EventArgs e)
        {
            // 上位(DepSearchPane)のハンドラに転送
            if (this.PropertyBoxDeleteClicked != null)
            {
                this.PropertyBoxDeleteClicked(sender, e);
            }
        }

        private void BunsetsuBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = this.Bounds;
            g.FillRectangle(Brushes.White, 0, 0, r.Width, 2);
            g.FillRectangle(Brushes.White, 0, 0, 2, r.Height);
            g.FillRectangle(Brushes.Gray, 0, r.Height - 4, r.Width, 4);
            g.FillRectangle(Brushes.Gray, r.Width-4, 0, 4, r.Height);
        }

        void OnRelationCommandClicked(object sender, RelationCommandEventArgs e)
        {
            // コマンドは上位(DepSearchPane)のハンドラに転送する
            if (this.RelationCommandClicked != null)
            {
                this.RelationCommandClicked(sender, e);
            }
        }

        /// <summary>
        /// MouseDownイベントは、自身からのものだけでなく、
        /// PropertyBox経由のものもフックされて、ここに来る。
        /// (PropertyBoxの余白領域がクリックされる場合があるため）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseDownTransferring(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenu.Show(PointToScreen(e.Location));
                return;
            }
            // 上位のイベントリスナ（通常はDepSearchPanel）に転送する
            if (this.MouseDownTransferring != null)
            {
                this.MouseDownTransferring(this, e);
            }
            Console.WriteLine("MouseDown(2) id={0}", m_id);
        }

        /// <summary>
        /// Context Menu --> Delete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (this.DeleteClicked != null) this.DeleteClicked(this, e);
        }

        /// <summary>
        /// Segment Tag Boxがクリックされたら、Segment Selectorを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_Click(object sender, EventArgs e)
        {
            Popup popup = TagSelector.PreparedPopups[ChaKi.Entity.Corpora.Annotations.Tag.SEGMENT];
            ((TagSelector)popup.Content).TagSelected += new EventHandler(HandleSegmentTagChanged);
            popup.Show(this.textBox1);
        }

        private void HandleSegmentTagChanged(object sender, EventArgs e)
        {
            TagSelector selector = sender as TagSelector;
            if (selector == null) return;
            if (this.SegmentTag != selector.Selection.Name)
            {
                this.SegmentTag = selector.Selection.Name;
                this.textBox1.Text = this.SegmentTag;
                // 上位(DepSearchPane)のハンドラに転送
                if (this.PropertyBoxSegmentTagChanged != null)
                {
                    this.PropertyBoxSegmentTagChanged(this, e);
                }
            }
        }

        // 属性条件の編集
        private void button1_Click(object sender, EventArgs e)
        {
            var pos = PointToScreen(this.button1.Location);
            m_EditAttributesDlg.Location = pos;
            m_EditAttributesDlg.SetData(this.SegmentAttributes);
            if (m_EditAttributesDlg.ShowDialog() == DialogResult.OK)
            {
                m_EditAttributesDlg.GetData(this.SegmentAttributes);
                this.button1.BackColor = (this.SegmentAttributes.Count > 0) ? Color.Crimson : Color.DodgerBlue;
            }
        }
    }
}
