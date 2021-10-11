using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Options;
using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Service.SentenceEdit;
using ChaKi.GUICommon;
using ChaKi.Service.Search;
using MessageBox = ChaKi.Common.Widgets.MessageBox;
using ChaKi.Properties;

namespace ChaKi.Panels
{
    public partial class ContextPanel : Form, IChaKiView
    {
        public SentenceContext Model { get; set; }

        public bool IsEditing { get { return m_IsEditing; } }

        private bool m_IsEditing;
        private ISentenceEditService m_Service;

        public ContextPanel()
        {
            this.Model = new SentenceContext();
            m_IsEditing = false;
            m_Service = new SentenceEditService();

            InitializeComponent();
            this.richTextBox1.Font = GUISetting.Instance.GetBaseTextFont();
            try
            {
                this.richTextBox1.BackColor = Color.FromName(GUISetting.Instance.ContextPanelSettings.Background);
            }
            catch (Exception)
            {
                this.richTextBox1.BackColor = Color.Wheat;
            }

            FontDictionary.Current.FontChanged += new EventHandler(OnFontChanged);

            UpdateToolStrip();
        }

        public void UpdateView()
        {
            int currentPos = this.richTextBox1.SelectionStart;
            var lastReadOnly = this.richTextBox1.ReadOnly;
            this.richTextBox1.ReadOnly = false;
            Color fg = this.richTextBox1.ForeColor;
            Color bg = this.richTextBox1.BackColor;
            this.richTextBox1.Clear();

            this.richTextBox1.SelectionColor = fg;
            this.richTextBox1.SelectionBackColor = bg;
            foreach (SentenceContextItem s in Model.Items)
            {
                if (!s.IsCenter)
                {
                    this.richTextBox1.SelectedText = s.Text + "\n";
                }
                else
                {
                    string cen1 = null;
                    string cen2 = null;
                    string cen3 = null;
                    try
                    {
                        cen1 = s.Text.Substring(0, this.Model.CenterOffset);
                        cen2 = s.Text.Substring(this.Model.CenterOffset, this.Model.CenterLength);
                        cen3 = s.Text.Substring(this.Model.CenterOffset + this.Model.CenterLength);
                    }
                    catch
                    {
                        cen1 = null;
                        cen2 = s.Text;
                        cen3 = null;
                    }
                    if (cen1 != null)
                    {
                        this.richTextBox1.SelectionColor = Color.Black;
                        this.richTextBox1.SelectionBackColor = Color.Yellow;
                        this.richTextBox1.SelectedText = cen1;
                    }
                    if (cen2 != null)
                    {
                        this.richTextBox1.SelectionColor = Color.Yellow;
                        this.richTextBox1.SelectionBackColor = Color.Brown;
                        this.richTextBox1.SelectedText = cen2;
                    }
                    if (cen3 != null)
                    {
                        this.richTextBox1.SelectionColor = Color.Black;
                        this.richTextBox1.SelectionBackColor = Color.Yellow;
                        this.richTextBox1.SelectedText = cen3;
                    }
                    this.richTextBox1.SelectedText = "\n";

                    this.richTextBox1.SelectionColor = fg;
                    this.richTextBox1.SelectionBackColor = bg;
                }
            }

            this.richTextBox1.SelectionStart = currentPos;
            this.richTextBox1.Modified = false;
            this.richTextBox1.ReadOnly = lastReadOnly;
            while (this.richTextBox1.CanUndo)
            {
                this.richTextBox1.ClearUndo();
            }
        }

        private bool CheckHasEOS()
        {
            if (this.Model.Items.Count > 1)
            {
                if (this.Model.Items[0].Sen.EndChar != this.Model.Items[1].Sen.StartChar)
                {
                    return true;
                }
            }
            return false;
        }

        public void SetEditMode(bool f)
        {
            if (f)
            {
                if (CheckHasEOS())
                {
                    MessageBox.Show(Resources.ContextPanel1);
                    return;
                }
                if (GUISetting.Instance.ContextPanelSettings.UseSpacing)
                {
                    MessageBox.Show("Option 'UseSpacing' must be off.");
                    return;
                }
                try
                {
                    m_Service.Open(this.Model.Corpus, UnlockRequestCallback);
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Failed to open Corpus:", ex);
                    dlg.ShowDialog();
                    return;
                }
                m_IsEditing = f;
                this.richTextBox1.BackColor = Color.White;
            }
            else
            {
                try
                {
                    m_Service.Close();
                    this.richTextBox1.BackColor = Color.FromName(GUISetting.Instance.ContextPanelSettings.Background);
                }
                catch (Exception)
                {
                    this.richTextBox1.BackColor = Color.Wheat;
                }
                m_IsEditing = f;
            }
            UpdateView();
            UpdateToolStrip();
        }

        bool UnlockRequestCallback(Type requestingService)
        {
            if (requestingService == typeof(ISentenceEditService))
            {
                // 自分自身からのUnlockリクエストは無視する.
                return true;
            }
            if (this.IsEditing)
            {
                if (!this.richTextBox1.CanUndo)
                {
                    return false;
                }
                SetEditMode(false);
            }
            return true;
        }

        /// <summary>
        /// アプリケーション終了前に呼ばれる、終了確認
        /// </summary>
        /// <returns></returns>
        public bool EndEditing()
        {
            if (IsEditing && this.richTextBox1.CanUndo)  //Saveすべきデータが残っている
            {
                DialogResult res = MessageBox.Show("Save Current Changes?", "SentenceEdit", MessageBoxButtons.YesNoCancel);
                if (res == DialogResult.Yes)
                {
                    Cursor oldCur = this.Cursor;
                    this.Cursor = Cursors.WaitCursor;
                    Save();
                    this.Cursor = oldCur;
                }
                else if (res == DialogResult.No)
                {
                }
                else if (res == DialogResult.Cancel)
                {
                    return false;
                }
            }
            m_Service.Close();
            return true;
        }

        void UpdateToolStrip()
        {
            this.toolStripButton1.Checked = IsEditing;
            this.editModeToolStripMenuItem.Checked = IsEditing;
            this.toolStripButton3.Enabled = 
                this.undoToolStripMenuItem.Enabled = (IsEditing && this.richTextBox1.CanUndo);
            this.toolStripButton4.Enabled = 
                this.redoToolStripMenuItem.Enabled = (IsEditing && this.richTextBox1.CanRedo);
            this.toolStripButton2.Enabled = 
                this.saveToolStripMenuItem.Enabled = (IsEditing && this.richTextBox1.CanUndo);
            this.splitSentenceToolStripMenuItem.Enabled = IsEditing;
            this.mergeSentenceToolStripMenuItem.Enabled = IsEditing;
        }

        private void Merge()
        {
            if (this.richTextBox1.SelectionStart < this.richTextBox1.Text.Length
             && this.richTextBox1.Text[this.richTextBox1.SelectionStart] == '\n')
            {
                this.richTextBox1.SelectionLength = 1;
                this.richTextBox1.ReadOnly = false;
                this.richTextBox1.SelectedText = string.Empty;
                this.richTextBox1.ReadOnly = true;
            }
        }

        private void Split()
        {
            this.richTextBox1.ReadOnly = false;
            this.richTextBox1.SelectedText = "\n";
            this.richTextBox1.ReadOnly = true;
        }

        private void Save()
        {
            List<int> editedLineLengths = new List<int>();
            for (int i = 0; i < this.richTextBox1.Lines.Length - 1; i++)  // 最後に空行があるのでそれのみ除く
            {
                string s = this.richTextBox1.Lines[i];
                string s2 = s.TrimEnd('\n');
                editedLineLengths.Add(s2.Length);
            }
            List<Sentence> sourceSentences = new List<Sentence>();
            foreach (SentenceContextItem item in this.Model.Items)
            {
                sourceSentences.Add(item.Sen);
            }
            // ModelとeditedLineLengthsが一致するまでSplit,Mergeを反復実行
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                m_Service.ChangeBoundaries(sourceSentences, editedLineLengths);
                m_Service.Commit();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Error while Saving", ex);
                dlg.ShowDialog();
            }
            SetEditMode(false);
            ReloadModel();
            this.Cursor = oldCur;
            UpdateToolStrip();
        }

        void ReloadModel()
        {
            if (this.Model == null)
            {
                return;
            }
            SentenceContextService svc = new SentenceContextService(this.Model);
            try
            {
                svc.Begin();
                this.UpdateView();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Error while Saving", ex);
                dlg.ShowDialog();
            }
        }

        void OnFontChanged(object sender, EventArgs e)
        {
            this.Font = GUISetting.Instance.GetBaseTextFont();
        }

        // Begin or Exit Edit Mode
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (IsEditing)
            {
                // Confirm to Cancel Editing
                this.richTextBox1.ReadOnly = false;
                if (this.richTextBox1.CanUndo)
                {
                    if (MessageBox.Show("Leave Edit Mode without Saving?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                    while (this.richTextBox1.CanUndo)
                    {
                        this.richTextBox1.Undo();
                        this.richTextBox1.ClearUndo();
                    }
                }
                this.richTextBox1.ReadOnly = true;
                SetEditMode(false);
            }
            else
            {
                // Enter Edit Mode
                SetEditMode(true);
            }
        }

        // Undo
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            this.richTextBox1.ReadOnly = false;
            if (this.richTextBox1.CanUndo)
            {
                this.richTextBox1.Undo();
//                this.richTextBox1.ClearUndo();
            }
            this.richTextBox1.SelectionLength = 0;
            this.richTextBox1.ReadOnly = true;
            UpdateToolStrip();
        }

        // Redo
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            this.richTextBox1.ReadOnly = false;
            if (this.richTextBox1.CanRedo)
            {
                this.richTextBox1.Redo();
            }
            this.richTextBox1.SelectionLength = 0;
            this.richTextBox1.ReadOnly = true;
            UpdateToolStrip();
        }

        // Save
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void richTextBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (m_IsEditing)
            {
                if (e.KeyCode == Keys.Delete)
                {
                    Merge();
                }
                else if (e.KeyCode == Keys.Return)
                {
                    Split();
                }
                else if (e.KeyCode == Keys.Back)
                {
                    this.richTextBox1.SelectionStart = Math.Max(0, this.richTextBox1.SelectionStart - 1);
                    Merge();
                }
            }
            UpdateToolStrip();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Copy();
        }

        private void editModeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            toolStripButton1_Click(sender, e);
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButton3_Click(sender, e);
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButton4_Click(sender, e);
        }

        private void splitSentenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_IsEditing)
            {
                Split();
            }
        }

        private void mergeSentenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_IsEditing)
            {
                Merge();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButton2_Click(sender, e);
        }

        private void richTextBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenuStrip1.Show(this.PointToScreen(e.Location));
            }
        }

        public void SetModel(object model)
        {
            // Intentionally blank
        }

        public void SetVisible(bool f)
        {
            // Intentionally blank
        }

        public void CutToClipboard()
        {
            // Intentionally blank
        }

        public void CopyToClipboard()
        {
            this.richTextBox1.Copy();
        }

        public void PasteFromClipboard()
        {
            // Intentionally blank
        }

        public bool CanCut
        {
            get { return false; }
        }

        public bool CanCopy
        {
            get { return this.richTextBox1.SelectionLength > 0; }
        }

        public bool CanPaste
        {
            get { return false; }
        }

        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            // 正しい操作において、Beep音を防ぐために必要 (編集モードでもRichTextBoxはReadonlyであるため、これらのキーが押されるとBeepが鳴る。)
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                e.Handled = true;
            }
        }
    }
}