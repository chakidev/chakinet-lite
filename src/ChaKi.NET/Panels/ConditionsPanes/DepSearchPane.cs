using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ChaKi.GUICommon;
using ChaKi.Entity.Search;
using PopupControl;
using ChaKi.Common;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.Common.Widgets;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class DepSearchPane : UserControl
    {
        private DepSearchCondition m_Model;
        private List<BunsetsuBox> m_Boxes;
        private List<RelationButton> m_Buttons;
        private List<LinkArrow> m_LinkArrows;

        private Point m_DragStartPoint;
        private BunsetsuBox m_DragStartBox;
        private Point m_DragEndPoint;
        private bool m_Dragging = false;
        private LinkArrow m_HitAt;
        private bool m_FreezeUpdate;

        private static Pen m_DragPen;

        private TagSelector m_LinkTagSource;

        private DpiAdjuster m_DpiAdjuster;
        private int m_XMargin = 5;

        static DepSearchPane()
        {
            var srcPen = LinkPens.Find("D");
            m_DragPen = (Pen)srcPen.Clone();
            m_DragPen.Width = 1.5F;
            m_DragPen.Color = Color.Red;
        }

        public DepSearchPane(DepSearchCondition model)
        {
            InitializeComponent();

            m_Boxes = new List<BunsetsuBox>();
            m_Buttons = new List<RelationButton>();
            m_LinkArrows = new List<LinkArrow>();

            m_Model = model;
            m_Model.OnModelChanged += new EventHandler(this.OnModelChanged);
            m_FreezeUpdate = false;

            this.UpdateView();

            m_LinkTagSource = TagSelector.PreparedSelectors[ChaKi.Entity.Corpora.Annotations.Tag.LINK];

            m_DpiAdjuster = new DpiAdjuster((xscale, yscale) => {
                m_XMargin = (int)(m_XMargin * xscale);
            });
            using (var g = this.CreateGraphics())
            {
                m_DpiAdjuster.Adjust(g);
            }
        }

        public void SetCondition(DepSearchCondition cond)
        {
            m_Model = cond;
            m_Model.OnModelChanged += new EventHandler(this.OnModelChanged);
            UpdateView();
        }

        public DepSearchCondition GetCondition()
        {
            return m_Model;
        }

        private void UpdateView()
        {
            Debug.WriteLine("DepSearchPane::UpdateView");

            //            this.Visible = false;
            this.SuspendLayout();

            m_Boxes.Clear();
            m_Buttons.Clear();
            m_LinkArrows.Clear();
            this.Controls.Clear();
            this.PerformLayout();

            int n = 0;
            foreach (TagSearchCondition bunsetsucond in m_Model.BunsetsuConds)
            {
                // ���̃{�^��
                RelationButton button = new RelationButton(-1, n);
                if (n == 0)
                {
                    button.Style = RelationButtonStyle.Leftmost;
                }
                button.Text = new String(bunsetsucond.LeftConnection, 1);
                button.OnCommandEvent += new RelationCommandEventHandler(OnRelationCommand);
                this.Controls.Add(button);
                m_Buttons.Add(button);

                // �O����Box
                BunsetsuBox bbox = new BunsetsuBox(n, bunsetsucond.SegmentTag, bunsetsucond.SegmentAttrs, bunsetsucond.LexemeConds);
                this.Controls.Add(bbox);
                this.m_Boxes.Add(bbox);
                bbox.MouseDownTransferring += new MouseEventHandler(OnMouseDownTransferring);
                bbox.CenterlizedButtonClicked += new EventHandler(OnCenterlizedButtonClicked);
                bbox.PropertyBoxDeleteClicked += new EventHandler(OnPropertyBoxDeleteClicked);
                bbox.DeleteClicked += new EventHandler(OnDeleteClicked);
                bbox.RelationCommandClicked += new RelationCommandEventHandler(OnRelationCommand);
                bbox.PropertyBoxSegmentTagChanged += new EventHandler(HandlePropertyBoxSegmentTagChanged);
                n++;
            }
            // �ł��E�̃{�^��
            RelationButton rightbutton = new RelationButton(-1, n);
            rightbutton.Style = RelationButtonStyle.Rightmost;
            if (n > 0 && n - 1 < m_Model.BunsetsuConds.Count)
            {
                rightbutton.Text = new String(m_Model.BunsetsuConds[n - 1].RightConnection, 1);
            }
            rightbutton.OnCommandEvent += new RelationCommandEventHandler(OnRelationCommand);
            this.Controls.Add(rightbutton);
            m_Buttons.Add(rightbutton);

            this.ResumeLayout();

            this.RecalcLayout();
            //            this.Visible = true;

            // �����N��o�^���A���̈ʒu���v�Z����
            foreach (LinkCondition link in m_Model.LinkConds)
            {
                AddLinkCondition(link);
            }
            Invalidate();
        }

        void bbox_DeleteClicked(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void AddLinkCondition(LinkCondition link)
        {
            LinkArrow la = new LinkArrow(this, link);
            la.From = link.SegidFrom;
            la.To = link.SegidTo;
            la.Text = link.Text;
            la.SetPosition(m_Boxes[link.SegidFrom].GetLinkPoint(), m_Boxes[link.SegidTo].GetLinkPoint());
            m_LinkArrows.Add(la);
        }

        private void RecalcLayout()
        {
            int x = 5;
            int y = 5;
            int height = 10;
            if (m_Boxes.Count > 0)
            {
                height += (m_Boxes[0].Height + 10);
            }
            int maxCount = Math.Min(m_Boxes.Count, m_Buttons.Count - 1);
            for (int i = 0; i < maxCount; i++)
            {
                RelationButton button = m_Buttons[i];
                button.Location = new Point(x, (height - button.Height) / 2);
                x += (button.Width + m_XMargin);

                BunsetsuBox bbox = m_Boxes[i];
                bbox.Location = new Point(x, y);
                x += (bbox.Width + m_XMargin);
            }
            RelationButton rbutton = m_Buttons[m_Buttons.Count - 1];
            rbutton.Location = new Point(x, (height - rbutton.Height) / 2);
            x += (rbutton.Width + m_XMargin);
            this.Width = x;
            this.Height = height;
        }

        /// <summary>
        /// Pivot��ύX����
        /// (�����̂��߁AModel->View update Mechanism�͎g�p�����A���ڗ����������ŕύX����)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnCenterlizedButtonClicked(object sender, EventArgs e)
        {
            PropertyBox pbox = sender as PropertyBox;
            for (int i = 0; i < m_Model.BunsetsuConds.Count; i++)
            {
                TagSearchCondition tagCond = m_Model.BunsetsuConds[i];
                BunsetsuBox bbox = m_Boxes[i];
                for (int j = 0; j < tagCond.LexemeConds.Count; j++)
                {
                    LexemeCondition lexCond = tagCond.LexemeConds[j];
                    PropertyBox box = bbox.Boxes[j];
                    if (box == pbox)
                    {
                        box.IsPivot = true;
                    }
                    else
                    {
                        box.IsPivot = false;
                    }
                }
            }
            Invalidate();
        }

        /// <summary>
        /// �R�}���h�{�^������
        /// �O��(BunsetsuBox)�E����(PropertyBox)�ǂ�������̃n���h���ŏ�������B
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnRelationCommand(object sender, RelationCommandEventArgs e)
        {
            RelationButton button = sender as RelationButton;

            char ch = e.Command;
            int bunsetsuPos = button.BunsetsuID;
            int buttonPos = button.ID;

            m_FreezeUpdate = true;

            // �v�f�̒ǉ�
            // + �������ꂽ�Ƃ��͖������A-, < �̂Ƃ��͒[�̈ʒu�ŉ����ꂽ�Ƃ��ɒǉ�����.
            if (bunsetsuPos < 0)
            {
                if (ch == '+'
                    || ((ch == '-' || ch == '<') && (buttonPos == 0 || buttonPos == m_Model.BunsetsuConds.Count)))
                {
                    TagSearchCondition newitem = m_Model.InsertBunsetsuCondAt(buttonPos);
                    if (newitem != null)
                    {
                        newitem.LexemeConds[0].IsPivot = false;
                    }
                    if (buttonPos == 0)
                    {
                        buttonPos++;    // ���[�ɒǉ������Ƃ��́AbuttonPos���E�ɃV�t�g����
                    }
                }
                // Link Position�̕ύX(from, to��button.ID�ȏ�̂��̂�+1����. button.ID�͍��[��0)
                if (ch == '+')
                {
                    foreach (var lnk in m_Model.LinkConds)
                    {
                        if (lnk.SegidFrom >= button.ID)
                        {
                            lnk.SegidFrom++;
                        }
                        if (lnk.SegidTo >= button.ID)
                        {
                            lnk.SegidTo++;
                        }
                    }
                }
            }
            else
            {
                if (ch == '+'
                    || ((ch == '-' || ch == '<') && (buttonPos == 0 || buttonPos == m_Model.BunsetsuConds[bunsetsuPos].LexemeConds.Count)))
                {
                    m_Model.InsertLexemeConditionAt(bunsetsuPos, buttonPos);
                    if (buttonPos == 0)
                    {
                        buttonPos++;    // ���[�ɒǉ������Ƃ��́AbuttonPos���E�ɃV�t�g����
                    }
                }
            }

            // �{�^�����x���̕ύX
            if (ch != '+')
            {
                if (bunsetsuPos < 0)
                {
                    m_Model.SetConnection(buttonPos, ch);
                }
                else
                {
                    m_Model.BunsetsuConds[bunsetsuPos].SetConnection(buttonPos, ch);
                }
            }
            m_FreezeUpdate = false;
            UpdateView();
        }

        void OnModelChanged(object sender, EventArgs e)
        {
            if (!m_FreezeUpdate)
            {
                UpdateView();
            }
        }

        /// <summary>
        /// PropertyBox��Centerized Button�i�Ԃ����������j�������ꂽ���̏����B
        /// ����Box��Pivot�\������B
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPropertyBoxCenterizedButtonClicked(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// PropertyBox����̃C�x���g�ʒm�ɂ��A����Box���폜����B
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPropertyBoxDeleteClicked(object sender, EventArgs e)
        {
            if (!(sender is PropertyBox))
            {
                return;
            }
            PropertyBox pb = (PropertyBox)sender;
            for (int iBunsetsu = 0; iBunsetsu < m_Boxes.Count; iBunsetsu++)
            {
                int iBox = m_Boxes[iBunsetsu].GetIndexOfPropertyBox((PropertyBox)sender);
                if (iBox >= 0)
                {
                    if (m_Boxes[iBunsetsu].Boxes.Count < 1)
                    {
                        return;
                    }
                    TagSearchCondition tcond = m_Model.BunsetsuConds[iBunsetsu];
                    tcond.RemoveAt(iBox);
                }
            }

            UpdateView();
        }

        private LinkArrowHitType HitTestLinkArrows(Point p)
        {
            m_HitAt = null;
            foreach (LinkArrow ar in m_LinkArrows)
            {
                LinkArrowHitType ht = ar.HitTest(p);
                if (ht != LinkArrowHitType.None)
                {
                    m_HitAt = ar;
                    return ht;
                }
            }
            return LinkArrowHitType.None;
        }

        /// <summary>
        /// BusnetsuBox�폜����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDeleteClicked(object sender, EventArgs e)
        {
            if (!(sender is BunsetsuBox))
            {
                return;
            }
            if (m_Boxes.Count < 2)
            {
                return;
            }
            int iBunsetsu = m_Boxes.IndexOf((BunsetsuBox)sender);
            if (iBunsetsu < 0)
            {
                return;
            }

            // Link Position�̕ύX(from, to�� >iBunsetsu �̂��̂�-1���A
            // =iBunsetsu�̂��͔͈̂͊O(-1)�Ƃ���).
            // from, to���͈͊O�ƂȂ������̂�RemoveBunsesuCondAt()�ŏ��O�����.
            // Link�̎n�I�_��Position�ŊǗ������ABox�ւ̃|�C���^�ŊǗ�����΂����Ƃ������肷�邪�A
            // Position�Ǘ���Model�܂œO�ꂵ�Ă��ĕύX�ł��Ȃ��̂ŁA���̂悤�ȃI�t�Z�b�g�v�Z�ŃJ�o�[����.
            foreach (var lnk in m_Model.LinkConds)
            {
                if (lnk.SegidFrom > iBunsetsu)
                {
                    lnk.SegidFrom--;
                }
                else if (lnk.SegidFrom == iBunsetsu)
                {
                    lnk.SegidFrom = -1;
                }
                if (lnk.SegidTo > iBunsetsu)
                {
                    lnk.SegidTo--;
                }
                else if (lnk.SegidTo == iBunsetsu)
                {
                    lnk.SegidTo = -1;
                }
            }

            // ����Box���폜���A�͈͊O�ƂȂ���Link���폜����B
            m_Model.RemoveBunsesuCondAt(iBunsetsu);

            UpdateView();
        }

        void HandlePropertyBoxSegmentTagChanged(object sender, EventArgs e)
        {
            BunsetsuBox bbox = sender as BunsetsuBox;
            if (bbox == null) return;
            int iBunsetsu = m_Boxes.IndexOf(bbox);
            if (iBunsetsu < 0)
            {
                return;
            }
            m_Model.BunsetsuConds[iBunsetsu].SegmentTag = bbox.SegmentTag;
            UpdateView();
        }



        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            LinkArrowHitType ht = HitTestLinkArrows(e.Location);
            if (ht == LinkArrowHitType.AtArrow)
            {
                this.contextMenuStrip2.Show(PointToScreen(e.Location));
            }
            else if (ht == LinkArrowHitType.AtText)
            {
#if false // TagSelector���ƃ��C���h�J�[�h�̓��͂Ŗ�肪����.
                Popup popup = TagSelector.PreparedPopups[ChaKi.Entity.Corpora.Annotations.Tag.LINK];
                //((TagSelector)popup.Content).TagSelected += new EventHandler(HandleLinkTagChanged);
                popup.Show(this, e.Location);
#endif
                var curcorpusname = (ChaKiModel.CurrentCorpus != null) ? (ChaKiModel.CurrentCorpus.Name) : string.Empty;
                List<Tag> tags = (m_LinkTagSource != null) ? m_LinkTagSource.GetTagsForCorpus(curcorpusname) : null;
                if (tags != null)
                {
                    this.contextMenuStrip1.Items.Clear();
                    this.contextMenuStrip1.Items.Add("*");
                    foreach (var tag in tags)
                    {
                        this.contextMenuStrip1.Items.Add(tag.Name);
                    }
                }
                else
                {
                    this.contextMenuStrip1.Items.Clear();
                    this.contextMenuStrip1.Items.Add("*");
                    this.contextMenuStrip1.Items.Add("D");
                }
                this.contextMenuStrip1.Show(PointToScreen(e.Location));
            }
        }

        // �O����Box�Ń}�E�X�������ꂽ���ˑ����h���b�O�J�n
        void OnMouseDownTransferring(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                BunsetsuBox box = sender as BunsetsuBox;
                m_DragStartPoint = box.GetLinkPoint();
                m_DragStartBox = box;
                m_DragEndPoint = e.Location;
                m_Dragging = true;
                this.Capture = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (m_Dragging)
            {
                m_DragEndPoint = e.Location;
                Rectangle r = new Rectangle(0, m_DragStartPoint.Y, this.Width, m_DragStartPoint.Y);
                Invalidate(r);
                return;
            }
            LinkArrowHitType ht = HitTestLinkArrows(e.Location);
            if (ht == LinkArrowHitType.AtArrow)
            {
                this.Cursor = Cursors.UpArrow;
            }
            else if (ht == LinkArrowHitType.AtText)
            {
                this.Cursor = Cursors.IBeam;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (!m_Dragging || m_DragStartBox == null)
            {
                return;
            }
            m_Dragging = false;
            m_DragEndPoint = e.Location;
            Rectangle r = new Rectangle(0, m_DragStartPoint.Y, this.Width, m_DragStartPoint.Y);
            Invalidate(r);
            this.Capture = false;
            Point p = e.Location;
            // MouseUp�ʒu�ɂ��镶��Box��T��
            BunsetsuBox hit = null;
            foreach (BunsetsuBox b in m_Boxes)
            {
                if (b.Bounds.Contains(p))
                {
                    hit = b;
                    break;
                }
            }
            if (hit != null)
            {
                if (m_DragStartBox.ID == hit.ID)
                {
                    return;
                }
                if (m_DragStartBox.ID >= 0 && m_DragStartBox.ID < m_Boxes.Count
                 && hit.ID >= 0 && hit.ID < m_Boxes.Count)
                {
                    LinkCondition lc = new LinkCondition(m_DragStartBox.ID, hit.ID, "*");
                    m_Model.AddLinkConstraint(lc);
                    // �����̂��߁AModel,View������������update����
                    AddLinkCondition(lc);
                    Refresh();
                    Invalidate();
                }
            }
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (m_Dragging)
            {
                Point[] pts = new Point[]
                {
                    m_DragStartPoint,
                    new Point(m_DragStartPoint.X,m_DragStartPoint.Y),
                    new Point(m_DragStartPoint.X,m_DragStartPoint.Y+20),
                    new Point(m_DragEndPoint.X,m_DragStartPoint.Y+20),
                    new Point(m_DragEndPoint.X,m_DragStartPoint.Y),
                };

                g.DrawLines(m_DragPen, pts);
            }

            int level = 0;
            foreach (LinkArrow la in m_LinkArrows)
            {
                la.Draw(g, level++, true);
            }
        }

        private void OnDeleteArrow(object sender, EventArgs e)
        {
            if (m_HitAt == null)
            {
                return;
            }
            m_Model.LinkConds.Remove(m_HitAt.Link);
            UpdateView();
            Invalidate();
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (m_HitAt != null)
            {
                string t = e.ClickedItem.Text;
                m_HitAt.Link.Text = t;
                // �����̂��߁A������View���X�V����
                m_HitAt.Text = t;
                Invalidate();
            }
        }

        /// <summary>
        /// Center Word��addCond�������}�[�W����.
        /// (Word Occurrence Search�Ō�̐��N���������邽�߂̏������쐬���邽�߂ɌĂ΂��j
        /// Center Word�̏������Ȃ���΍쐬����.
        /// </summary>
        /// <param name="addCond"></param>
        public void MergeSearchCondition(List<LexemeCondition> addCond)
        {
            List<LexemeCondition> orgLexConds = m_Model.GetLexemeCondList();
            if (addCond.Count != orgLexConds.Count)
            {
                throw new Exception("Mismatch in size of Lexeme Conditions.");
            }
            for (int i = 0; i < orgLexConds.Count; i++)
            {
                // �I���W�i���Ɏw��̂���Property�̓I���W�i�����g�p�A
                // ����ȊO��addCond�Ɏw��̂���Property�͂�����g�p����B
                // ���̏����̓I���W�i���̂܂܁B
                orgLexConds[i].Merge(addCond[i]);
            }
            UpdateView();
        }
    }
}
