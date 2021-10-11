using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Corpora.Annotations;
using ChaKi.GUICommon;
using MessageBox = ChaKi.Common.Widgets.MessageBox;
using ChaKi.Service.AttributeEditService;
using ChaKi.Common;
using PopupControl;
using System.Drawing;
using ChaKi.Common.Settings;
using ChaKi.Properties;
using ChaKi.Service.Database;

namespace ChaKi.Panels
{
    public partial class AttributeListPanel : Form, IChaKiView
    {
        protected Corpus m_Corpus = null;
        protected object m_Source = null;
        protected AttributeListSelector m_Selector;
        protected string m_Title = "AttributeEdit";

        private AttributeEditService m_Service;

        public bool UpdatedSinceLastOpen { get; set; }

        protected AttributeGrid AttributeGrid
        {
            get { return this.attributeGrid1; }
        }

        public AttributeListPanel()
        {
            m_Service = new AttributeEditService();

            InitializeComponent();

            this.attributeGrid1.UpdateUIState += new System.EventHandler(attributeGrid1_UpdateUIState);
            this.toolStripButton1.Enabled = false;
            this.toolStripButton2.Checked = false;
            this.toolStripButton1.Click += new EventHandler(HandleToggleEditMode);
            this.toolStripButton2.Enabled = false;
            this.toolStripButton2.Click += new EventHandler(HandleUndo);
            this.toolStripButton3.Enabled = false;
            this.toolStripButton3.Click += new EventHandler(HandleRedo);
            this.toolStripButton4.Enabled = false;
            this.toolStripButton4.Click += new EventHandler(HandleSave);
            this.toolStripButton5.Enabled = false;
            this.toolStripButton5.Click += HandleAddRow;
            this.toolStripButton6.Enabled = false;
            this.toolStripButton6.Click += HandleRemoveRow;

            m_Selector = new AttributeListSelector("Sentence");
            m_Selector.Reset();
            SetupContextMenu(this.attributeGrid1.ContextMenu);

            // Grid状態の復元
            var gs = GUISetting.Instance.AttributePanelGridSettings;
            if (gs == null)
            {
                gs = GUISetting.Instance.AttributePanelGridSettings = new GridSettings();
            }
            this.AttributeGrid.Settings = gs;
            this.UpdatedSinceLastOpen = false;
        }

        /// <summary>
        /// </summary>
        /// <param name="source"></param>
        public virtual void SetSource(Corpus corpus, object source)
        {
            if (source == null || m_Source == source/* || this.attributeGrid1.IsEditing*/)
            {
                return;
            }

            if (source is object[] && m_Source is object[])
            {
                if (((object[])source).SequenceEqual((object[])m_Source))
                {
                    return;
                }
            }
            if (this.attributeGrid1.IsEditing)
            {
                if (CanSave)
                {
                    this.UpdatedSinceLastOpen = true;
                    // 編集対象オブジェクトが切り替わる時に変更をDBに書き込む（Commitは行わない）.
                    try
                    {
                        Save();
                        m_Service.Flush();
                    }
                    catch (Exception ex)
                    {
                        ErrorReportDialog dlg = new ErrorReportDialog("Failed to change target object of AttributeEdit.", ex);
                        dlg.ShowDialog();
                    }
                    finally
                    {
                        this.attributeGrid1.ClearUndoRedo();
                    }
                }
            }

            m_Source = source;
            m_Corpus = corpus;

            m_Service.ContinueWithDifferntTarget(m_Corpus, m_Source);

            AttributeGridData model = new AttributeGridData();
            var rows = model.Rows;

            try
            {
                if (source is object[])
                {
                    foreach (var obj in (object[])source)
                    {
                        AddSingleObject(obj, rows);
                        if (this.attributeGrid1.IsEditing)
                        {
                            rows.Add(new AttributeData(string.Empty, string.Empty, AttributeGridRowType.KeyValueWritable));
                        }
                    }
                }
                else
                {
                    AddSingleObject(source, rows);
                    if (this.attributeGrid1.IsEditing)
                    {
                        rows.Add(new AttributeData(string.Empty, string.Empty, AttributeGridRowType.KeyValueWritable));
                    }
                }
                model.Sort();
                model.RemoveExcessOfEmptyAttribute();
            }
            catch (Exception ex)
            {
                // AttributeはLazyなのでSessionがないときにはアクセスできない.
//                ErrorReportDialog dlg = new ErrorReportDialog("Failed to start AttributeEditService. Please make sure DepEdit is in Edit Mode.", ex);
//                dlg.ShowDialog();
            }

            this.attributeGrid1.Model = model;
            this.attributeGrid1.ClearUndoRedo();
            //this.attributeGrid1.IsEditing = false;
        }

        private void AddSingleObject(object source, List<AttributeGridRowData> rows)
        {
            if (source is Sentence)
            {
                rows.Add(new GroupData(GroupData.DOCUMENT));

                Sentence sen = source as Sentence;
                rows.Add(new AttributeData("ID", sen.ParentDoc.ID.ToString(), AttributeGridRowType.ReadOnly));
                foreach (DocumentAttribute a in sen.ParentDoc.Attributes)
                {
                    if (!a.Key.StartsWith("@"))
                    {
                        rows.Add(new AttributeData(a.Key, a.Value, AttributeGridRowType.KeyValueWritable));
                    }
                }
                // --> ここだけ変則的。呼び出し元のSetSource()に存在すべきものである。Sentence/Documentについても、sourceをobject[]にすればすっきりする(TODO)。
                if (this.attributeGrid1.IsEditing)
                {
                    rows.Add(new AttributeData(string.Empty, string.Empty, AttributeGridRowType.KeyValueWritable));
                }
                // <--
                rows.Add(new GroupData(GroupData.SENTENCE));
                rows.Add(new AttributeData("ID", sen.ID.ToString(), AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("CharBegin", sen.StartChar.ToString(), AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("CharEnd", sen.EndChar.ToString(), AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("Pos", sen.Pos.ToString(), AttributeGridRowType.ReadOnly));
                foreach (SentenceAttribute a in sen.Attributes)
                {
                    rows.Add(new AttributeData(a.Key, a.Value, AttributeGridRowType.KeyValueWritable));
                }
            }
            else if (source is Segment)
            {
                rows.Add(new GroupData(GroupData.SEGMENT));
                Segment seg = source as Segment;
                rows.Add(new AttributeData("ID", seg.ID.ToString(), AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("TagName", seg.Tag.Name, AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("Comment", seg.Comment, AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("CharBegin", seg.StartChar.ToString(), AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("CharEnd", seg.EndChar.ToString(), AttributeGridRowType.ReadOnly));
                if (seg.Sentence != null && seg.Sentence.Words != null)
                {
                    var segtext = seg.Sentence.GetTextInRange(seg.StartChar, seg.EndChar, GUISetting.Instance.ContextPanelSettings.UseSpacing);
                    rows.Add(new AttributeData("Text", segtext, AttributeGridRowType.ReadOnly));
                }
                foreach (SegmentAttribute a in seg.Attributes)
                {
                    rows.Add(new AttributeData(a.Key, a.Value, AttributeGridRowType.KeyValueWritable));
                }
            }
            else if (source is Link)
            {
                rows.Add(new GroupData(GroupData.LINK));
                Link lnk = source as Link;
                rows.Add(new AttributeData("ID", lnk.ID.ToString(), AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("TagName", lnk.Tag.Name, AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("Comment", lnk.Comment, AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("To", lnk.To.ID.ToString(), AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("From", lnk.From.ID.ToString(), AttributeGridRowType.ReadOnly));
                foreach (LinkAttribute a in lnk.Attributes)
                {
                    rows.Add(new AttributeData(a.Key, a.Value, AttributeGridRowType.KeyValueWritable));
                }
            }
            else if (source is Group)
            {
                rows.Add(new GroupData(GroupData.GROUP));
                Group grp = source as Group;
                rows.Add(new AttributeData("ID", grp.ID.ToString(), AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("TagName", grp.Tag.Name, AttributeGridRowType.ReadOnly));
                rows.Add(new AttributeData("Comment", grp.Comment, AttributeGridRowType.ReadOnly));
                foreach (GroupAttribute a in grp.Attributes)
                {
                    rows.Add(new AttributeData(a.Key, a.Value, AttributeGridRowType.KeyValueWritable));
                }
            }
        }

        public bool IsEditing
        {
            get
            {
                return this.attributeGrid1.IsEditing;
            }
        }

        protected virtual void HandleToggleEditMode(object sender, System.EventArgs e)
        {
            // 編集モード移行はDepEditPanelから行う
#if false
            if (!this.attributeGrid1.IsEditing)
            {
                // Enter Edit Mode
                SetEditMode(true);
            }
            else
            {
                // 1. Confirm to cancel current editing
                if (this.attributeGrid1.CanUndo)
                {
                    if (MessageBox.Show(Resources.Leave_Edit_Mode_without_Saving, Resources.Confirm, MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                    this.attributeGrid1.RewindUndo();  // 初期状態に戻す.
                }

                // 2. Reset EditMode and Retrieve Data from Grid.
                SetEditMode(false);
            }
#endif
        }

        public void StartEditMode(OpContext context)
        {
            try
            {
                m_Service.Open(m_Corpus, m_Source, context);
                SetTagDefinitions();
                this.UpdatedSinceLastOpen = false;
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Failed to open AttributeEditService:", ex);
                dlg.ShowDialog();
                return;
            }
            this.attributeGrid1.StartEditing();
        }

        public void EndEditMode()
        {
            this.attributeGrid1.EndEditing();
        }

        protected virtual void SetEditMode(bool f)
        {
            if (f)
            {
                try
                {
                    m_Service.Open(m_Corpus, m_Source, UnlockRequestCallback);
                    SetTagDefinitions();
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Failed to open AttributeEditService:", ex);
                    dlg.ShowDialog();
                    return;
                }
                this.attributeGrid1.StartEditing();
            }
            else
            {
                try
                {
                    m_Service.Close();
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Failed to close AttributeEditService:", ex);
                    dlg.ShowDialog();
                }
                this.attributeGrid1.EndEditing();
            }
        }

        private void SetTagDefinitions()
        {
            m_Selector.Reset();
            var tags = m_Service.GetSentenceTagList();
            foreach (AttributeBase32 tag in tags)
            {
                m_Selector.AddTag(tag);
            }
        }

        private void SetupContextMenu(ContextMenuStrip m)
        {
            foreach (ToolStripItem item in m.Items)
            {
                if (item.Text == "Add from List")
                {
                    var ti = item as ToolStripDropDownButton;
                    if (ti != null)
                    {
                        var popup = new Popup(m_Selector) { Resizable = true };
                        ti.DropDown = popup;
                        m_Selector.TagSelected += new EventHandler(AttributeListSelector_TagSelected);
                    }
                }
            }
        }

        // Add from List
        void AttributeListSelector_TagSelected(object sender, EventArgs e)
        {
            var sel = m_Selector.Selection;
            if (sel != null)
            {
                this.attributeGrid1.InsertItem(sel.Key, sel.Value);
            }
        }

        private bool UnlockRequestCallback(Type requestingService)
        {
            if (requestingService == typeof(IAttributeEditService))
            {
                // 自分自身からのUnlockリクエストは無視する.
                return true;
            }
            if (this.attributeGrid1.IsEditing)
            {
                if (this.attributeGrid1.CanUndo)
                {
                    return false;
                }
                SetEditMode(false);
            }
            return true;
        }


        private void HandleUndo(object sender, System.EventArgs e)
        {
            this.attributeGrid1.Undo();
        }

        private void HandleRedo(object sender, System.EventArgs e)
        {
            this.attributeGrid1.Redo();
        }

        private void HandleAddRow(object sender, System.EventArgs e)
        {
            this.attributeGrid1.InsertItem(string.Empty, string.Empty);
        }

        private void HandleRemoveRow(object sender, System.EventArgs e)
        {
            this.attributeGrid1.RemoveRow();
        }


        private void HandleSave(object sender, System.EventArgs e)
        {
            try
            {
                HandleSave();
                Commit();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot save: ", ex);
                dlg.ShowDialog();
            }
        }

        public void HandleSave()
        {
            Save();
            this.attributeGrid1.ClearUndoRedo();
            this.UpdatedSinceLastOpen = false;
        }

        protected virtual void Commit()
        {
            m_Service.Commit();
        }

        /// <summary>
        /// アプリケーション終了前に呼ばれる、終了確認
        /// </summary>
        /// <returns></returns>
        public bool EndEditing()
        {
            if (this.attributeGrid1.CanUndo)  //Saveすべきデータが残っている
            {
                DialogResult res = MessageBox.Show("Save Current Changes?", m_Title, MessageBoxButtons.YesNoCancel);
                if (res == DialogResult.Yes)
                {
                    Save();
                    m_Service.Commit();
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

        protected virtual void Save()
        {
            var viewModel = this.attributeGrid1.Model;

            // ( GroupName --> ( Key --> Value )* )*
            var updateDataTable = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> currentGroupData = null;
            foreach (AttributeGridRowData rd in viewModel.Rows)
            {
                if (rd is GroupData)
                {
                    GroupData gd = rd as GroupData;
                    currentGroupData = new Dictionary<string, string>();
                    updateDataTable[gd.Name] = currentGroupData;

                }
                else if (rd is AttributeData)
                {
                    AttributeData ad = rd as AttributeData;
                    if (ad.Key.Length == 0)
                    {
                        continue;
                    }
                    if (ad.RowType == AttributeGridRowType.ReadOnly)
                    {
                        continue;
                    }
                    if (currentGroupData.ContainsKey(ad.Key))
                    {
                        throw new InvalidOperationException(string.Format("Duplicated Key found: {0}", ad.Key));
                    }
                    currentGroupData[ad.Key] = ad.Value;
                }
            }
            if (m_Source is Sentence)
            {
                m_Service.UpdateAttributesForSentence(updateDataTable[GroupData.SENTENCE], updateDataTable[GroupData.DOCUMENT]);
            }
            else if (m_Source is Segment)
            {
                m_Service.UpdateAttributesForSegment(updateDataTable[GroupData.SEGMENT]);
            }
            else if (m_Source is Link)
            {
                m_Service.UpdateAttributesForLink(updateDataTable[GroupData.LINK]);
            }
            else if (m_Source is Group)
            {
                m_Service.UpdateAttributesForGroup(updateDataTable[GroupData.GROUP]);
            }
            else if (m_Source is object[])
            {
                foreach (var obj in (object[])m_Source)
                {
                    if (obj is Segment)
                    {
                        m_Service.UpdateAttributesForSegment(updateDataTable[GroupData.SEGMENT], (Segment)obj);
                    }
                    else if (obj is Link)
                    {
                        m_Service.UpdateAttributesForLink(updateDataTable[GroupData.LINK], (Link)obj);
                    }
                    else if (obj is Group)
                    {
                        m_Service.UpdateAttributesForGroup(updateDataTable[GroupData.GROUP], (Group)obj);
                    }
                }
            }
        }

        private void attributeGrid1_UpdateUIState(object sender, System.EventArgs e)
        {
            this.toolStripButton1.Enabled = (m_Service != null);
            this.toolStripButton1.Checked = this.attributeGrid1.IsEditing;
            this.toolStripButton2.Enabled = this.attributeGrid1.CanUndo;
            this.toolStripButton3.Enabled = this.attributeGrid1.CanRedo;
            this.toolStripButton4.Enabled = this.attributeGrid1.CanUndo;
            this.toolStripButton5.Enabled = this.attributeGrid1.IsEditing;
            this.toolStripButton6.Enabled = this.attributeGrid1.CanCut; // Cutできるなら選択行があるのでRemoveRowも可能
        }

        public void SetModel(object model)
        {
            throw new System.NotImplementedException();
        }

        public void SetVisible(bool f)
        {
            throw new System.NotImplementedException();
        }

        public void CutToClipboard()
        {
            this.attributeGrid1.Cut();
        }

        public void CopyToClipboard()
        {
            this.attributeGrid1.Copy();
        }

        public void PasteFromClipboard()
        {
            this.attributeGrid1.Paste();
        }

        public bool CanCut
        {
            get { return this.attributeGrid1.CanCut; }
        }

        public bool CanCopy
        {
            get { return this.attributeGrid1.CanCopy; }
        }

        public bool CanPaste
        {
            get { return this.attributeGrid1.CanPaste; }
        }

        public bool CanSave
        {
            get { return this.attributeGrid1.CanUndo; }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.S && (keyData & Keys.Modifiers) == Keys.Control)
            {
                if (CanSave)
                {
                    HandleSave(this, EventArgs.Empty);
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
