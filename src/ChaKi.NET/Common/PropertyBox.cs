using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity;
using ChaKi.Entity.Search;
using ChaKi.Entity.Corpora;
using System.Drawing.Text;
using ChaKi.Entity.Settings;
using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Common.Widgets;

namespace ChaKi.GUICommon
{
    public partial class PropertyBox : UserControl
    {
        #region static fields
        private static Font m_font;
        private static Brush m_brush;
        private static Font m_font2;
        private static Brush m_brush2;
        private static Brush m_brush3;
        private static Brush m_brush4;
        private static Pen m_pen;
        private static TransparentInputForm m_inputDlg;
        #endregion

        private List<string> m_tags;
        private List<string> m_dispNames;
        private LexemeCondition m_Model;

        public event EventHandler CenterizedButtonClicked;
        public event EventHandler DeleteClicked;
        public event MouseEventHandler MouseDownTransferring;

        public bool CenterizedButtonVisible
        {
            get
            {
                return this.pictureBox1.Visible;
            }
            set
            {
                this.pictureBox1.Visible = value;
            }
        }

        public bool ShowRange
        {
            get
            {
                return this.textBox1.Visible;
            }
            set
            {
                this.textBox1.Visible = value;
                this.textBox2.Visible = value;
                this.label1.Visible = value;
            }
        }

        public bool IsPivot
        {
            get
            {
                if (m_Model != null)
                {
                    return m_Model.IsPivot;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (m_Model != null)
                {
                    m_Model.IsPivot = value;
                }
            }
        }

        public void SetModel(LexemeCondition model)
        {
            m_Model = model;
            this.Range = model.RelativePosition;
            Invalidate();
        }

        /// <summary>
        /// Rangeの設定・取得
        /// </summary>
        public Range Range
        {
            get
            {
                if (m_Model != null)
                {
                    SynchronizeRange();
                    return m_Model.RelativePosition;
                }
                return new Range();
            }
            set
            {
                if (m_Model != null)
                {
                    m_Model.RelativePosition = value;
                    this.textBox1.Text = string.Format("{0}", m_Model.RelativePosition.Start);
                    this.textBox2.Text = string.Format("{0}", m_Model.RelativePosition.End);
                }
            }
        }

        public void OffsetRange(int offset, int diff)
        {
            m_Model.OffsetRange(offset, diff);
            this.textBox1.Text = string.Format("{0}", m_Model.RelativePosition.Start);
            this.textBox2.Text = string.Format("{0}", m_Model.RelativePosition.End);
        }

        static PropertyBox()
        {
            m_font = new Font("ＭＳ Ｐゴシック", 9);
            m_font2 = new Font("Lucida Sans Unicode", 8, FontStyle.Italic);
            m_brush = new SolidBrush(Color.Black);
            m_brush2 = new SolidBrush(Color.MediumSlateBlue);
            m_brush3 = new SolidBrush(Color.White);
            m_brush4 = new SolidBrush(Color.Red);
            m_pen = new Pen(Color.Red, 2);

            // input用のポップアップを作成
            m_inputDlg = new TransparentInputForm();
        }

        private SizeF currentScaleFactor = new SizeF(1f, 1f);

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            //Record the running scale factor used
            this.currentScaleFactor = new SizeF(
               this.currentScaleFactor.Width * factor.Width,
               this.currentScaleFactor.Height * factor.Height);
        }

        public PropertyBox()
        {
            InitializeComponent();

            this.listBox1.ItemHeight = (int)(12 * currentScaleFactor.Height);

            this.CenterizedButtonVisible = true;

            AttachTo(Program.MainForm);

            UpdateSettings();
        }

        private void UpdateSettings()
        {
            m_tags = new List<string>();
            m_dispNames = new List<string>();
            this.listBox1.Items.Clear();
            foreach (PropertyBoxItemSetting setting in PropertyBoxSettings.Instance.Settings)
            {
                if (setting.IsVisible)
                {
                    m_tags.Add(setting.TagName);
                    m_dispNames.Add(setting.DisplayName);
                    this.listBox1.Items.Add(setting.TagName);
                }
            }
            PropertyBoxSettings.Instance.SettingChanged += new EventHandler(Instance_SettingChanged);

            // コントロールの高さを計算
            int height = this.listBox1.GetItemHeight(0) * m_tags.Count + 6;
            this.listBox1.Height = height;
            this.Height = this.listBox1.Height + 30;
        }

        /// <summary>
        /// 表示カスタマイズ設定が更新された場合のイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_SettingChanged(object sender, EventArgs e)
        {
            UpdateSettings();
            Refresh();
        }

        public PropertyBox(LexemeCondition model)
            : this()
        {
            // Modelの初期化
            SetModel(model);
        }

        /// <summary>
        /// Popup Formを指定したFormの子Formとする
        /// </summary>
        /// <param name="form"></param>
        public void AttachTo(Form form)
        {
            if (form != null)
            {
                form.AddOwnedForm(m_inputDlg);
            }
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            int index = e.Index;
            if (index < 0 || index >= m_tags.Count)
            {
                return;
            }
            Rectangle r = e.Bounds;
            using (Graphics g = e.Graphics)
            {
                g.FillRectangle(m_brush3, e.Bounds);

                // テキストの平滑化を有効にする
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                PropertyPair pair = null;
                if (m_Model != null)
                {
                    pair = m_Model.FindProperty(m_tags[index]);
                }
                if (pair != null)
                {
                    if (pair.Value.IsRegEx)
                    {
                        g.DrawString(pair.Value.StrVal, m_font, m_brush4, r);
                    }
                    else
                    {
                        g.DrawString(pair.Value.StrVal, m_font, m_brush, r);
                    }
                }
                else
                {
                    g.DrawString("<" + m_dispNames[index] + ">", m_font2, m_brush2, r);
                }
            }
        }

        /// <summary>
        /// ListBoxの項目編集
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            // 入力ダイアログの初期値を決める
            string val = "";
            bool isRegex = false;
            bool isCaseSensitive = true;
            PropertyPair pair = m_Model.FindProperty(m_tags[index]);
            if (pair != null)
            {
                val = pair.Value.StrVal;
                isRegex = pair.Value.IsRegEx;
                isCaseSensitive = pair.Value.IsCaseSensitive;
            }

            Rectangle r = listBox1.GetItemRectangle(index);
            r = listBox1.RectangleToScreen(r);

            if (m_tags[index] == Lexeme.PropertyName[LP.PartOfSpeech])
            {
                m_inputDlg.HasTreeSelection = true;
                // Selection(Tree)にPOS候補を用意させる
                m_inputDlg.TreePopup.PopulateWithPOSSelections(ChaKiModel.CurrentCorpusList, ChaKiModel.CurrentCorpus);
            }
            else if (m_tags[index] == Lexeme.PropertyName[LP.CType])
            {
                m_inputDlg.HasTreeSelection = true;
                // Selection(Tree)にCType候補を用意させる
                m_inputDlg.TreePopup.PopulateWithCTypeSelections(ChaKiModel.CurrentCorpusList, ChaKiModel.CurrentCorpus);
            }
            else if (m_tags[index] == Lexeme.PropertyName[LP.CForm])
            {
                m_inputDlg.HasTreeSelection = true;
                // Selection(Tree)にCForm候補を用意させる
                m_inputDlg.TreePopup.PopulateWithCFormSelections(ChaKiModel.CurrentCorpusList, ChaKiModel.CurrentCorpus);
            }
            else
            {
                m_inputDlg.HasListSelection = true;
                m_inputDlg.ListPopup.PopulateWithUserHistory(m_tags[index]);
            }
            m_inputDlg.ReturnFocusTo = listBox1;
            m_inputDlg.PopupLocation = new Point(r.Left, r.Top);
            m_inputDlg.Bounds = Program.MainForm.Bounds;
            PropertyInputDialog popup = m_inputDlg.Popup;
            // 入力ダイアログの初期値を設定
            popup.Index = index;
            popup.EditText = val;
            popup.IsRegEx = isRegex;
            popup.IsCaseSensitive = isCaseSensitive;
            // input終了時のハンドラを設定する
            popup.OnInputDone += this.OnPropertyInputDone;
            popup.OnInputCancel += this.OnPropertyInputCancel;
            m_inputDlg.Show();
            m_inputDlg.Popup.Select();  // 透明背景が存在するため、このSelectがないとフォーカスがPopupに設定されない
            m_inputDlg.ResetScrollState(); // Tree/Listを最上部までスクロール（表示のたびにスクロール位置が変化しないよう）
        }

        public void OnPropertyInputDone(object sender, InputDoneEventArgs e)
        {
            // InputDialogからの通知を中止する
            PropertyInputDialog popup = m_inputDlg.Popup;
            popup.OnInputDone -= this.OnPropertyInputDone;
            popup.OnInputCancel -= this.OnPropertyInputCancel;

            string key = m_tags[e.Index];
            // 属性の種別に応じたPropertyを作成する
            Property p;
            if (m_tags[e.Index] == Lexeme.PropertyName[LP.PartOfSpeech])
            {
                p = new PartOfSpeech(e.Text);
            }
            else if (m_tags[e.Index] == Lexeme.PropertyName[LP.CType])
            {
                p = new CType(e.Text);
            }
            else if (m_tags[e.Index] == Lexeme.PropertyName[LP.CForm])
            {
                p = new CForm(e.Text);
            }
            else
            {
                p = new Property(e.Text);
            }
            p.IsRegEx = popup.IsRegEx;
            p.IsCaseSensitive = popup.IsCaseSensitive;

            // PropertyをLexemeConditionのPropertyPairに追加する（既存PropertyPairは置換）
            PropertyPair pair = m_Model.FindProperty(key);
            if (pair == null)
            {
                if (e.Text.Length > 0)
                {
                    pair = new PropertyPair(key, p);
                    m_Model.PropertyPairs.Add(pair);
                }
            }
            else
            {
                if (e.Text.Length == 0)
                {
                    m_Model.RemoveProperty(key);
                }
                else
                {
                    pair.Value = p;
                }
            }
            m_inputDlg.Hide();

            // 検索キーワードの履歴に追加
            UserSettings.GetInstance().AddKeywordHistory(key, e.Text);
        }

        public void OnPropertyInputCancel(object sender, KeyPressEventArgs e)
        {
            m_inputDlg.Hide();
            // InputDialogからの通知を中止する
            PropertyInputDialog popup = m_inputDlg.Popup;
            popup.OnInputDone -= this.OnPropertyInputDone;
            popup.OnInputCancel -= this.OnPropertyInputCancel;
        }

        public LexemeCondition GetCondition()
        {
            return m_Model;
        }

        public void SynchronizeRange()
        {
            int start = 0;
            int end = 0;
            Int32.TryParse(this.textBox1.Text, out start);
            Int32.TryParse(this.textBox2.Text, out end);
            m_Model.RelativePosition = new Range(start, end);
        }

        private void PropertyBox_Paint(object sender, PaintEventArgs e)
        {
            if (this.IsPivot)
            {
                Rectangle r = this.listBox1.Bounds;
                e.Graphics.DrawRectangle(m_pen, r);
            }
        }

        /// <summary>
        /// LexBoxのコンテクストメニューを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                listBox1.ContextMenuStrip.Show(listBox1, e.Location);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (this.CenterizedButtonClicked != null) this.CenterizedButtonClicked(this, e);
        }

        private void centerizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.CenterizedButtonClicked != null) this.CenterizedButtonClicked(this, e);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.DeleteClicked != null) this.DeleteClicked(this, e);
        }

        private void PropertyBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.MouseDownTransferring != null) this.MouseDownTransferring(this, e);
        }

        private Predicate<char> IsValidKey = delegate(char ch)
        {
            return ("0123456789-".IndexOf(ch) >= 0 || ch == (char)Keys.Back);
        };

        // 数字以外のキー入力を防ぐ
        private void HandleTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !IsValidKey(e.KeyChar);
        }
    }
}
