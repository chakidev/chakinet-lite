using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Entity.Corpora;
using ChaKi.GUICommon;
using ChaKi.Service.DependencyEdit;
using DependencyEditSLA.Widgets;
using MessageBox = ChaKi.Common.Widgets.MessageBox;
using System.Text;
using ChaKi.Service.Cabocha;
using System.IO;
using ChaKi.Service.Export;
using ChaKi.Service.Readers;
using ChaKi.Service.Database;

namespace DependencyEditSLA
{
    public partial class DepEditControl : UserControl
    {
        private SentenceStructureScrollPanel m_ContainerPanel;
        private SentenceStructure m_SentenceStructure;
        private Panel m_DummyPanel;
        private LexemeSelectionGrid m_LexemeSelector;
        private MWEUploadGrid m_MWEUploadSelector;
        private MWEDownloadGrid m_MWEDownloadSelector;

        private int m_SentenceID;
        private int m_CenterWordPos;
        private DepEditService m_Service;
        static private DepEditControl m_instance;
        private Corpus m_Corpus;
        private int m_CenterWordStartAt;
        private int m_TargetProject;

        /// <summary>
        /// 語の上にマウスを持ってきたときにGuide Panelの表示更新を通知するイベント
        /// </summary>
        public event UpdateGuidePanelDelegate UpdateGuidePanel;

        /// <summary>
        /// SegmentBoxの上にマウスを持ってきたときにAttribute Panelの表示更新を通知するイベント
        /// TODO: Link, Groupに対してもも追加する必要がある.
        /// </summary>
        public event UpdateAttributePanelDelegate UpdateAttributePanel;

        /// <summary>
        /// Goto Previous/Next Sentenceが指示されたことを通知するイベント
        /// </summary>
        public event ChangeSentenceDelegate ChangeSenenceRequested;

        /// <summary>
        /// DepEditにおける編集が開始・終了されたことを通知する
        /// (MainFormで受け、AttributeEditPanelの編集を開始させるのに使用)
        /// 引数がOpContextならそのコンテクストにて開始、nullなら終了を意味する。
        /// </summary>
        public event Action<OpContext> EditContextChanged;

        /// <summary>
        /// DepEditでSave操作が行われたことを通知する
        /// (MainFormで受け、AttributeEditPanelでもSaveを行うのに使用)
        /// </summary>
        public event Action Saving;

        public Func<bool> AttributeChanged;

        public DepEditControl()
        {
            m_ContainerPanel = new SentenceStructureScrollPanel();
            m_SentenceStructure = new DependencyEditSLA.SentenceStructure();
            m_DummyPanel = new Panel();

            InitializeComponent();

            m_SentenceStructure.BackColor = ColorTranslator.FromHtml(DepEditSettings.Current.Background);
            m_SentenceStructure.Location = new Point(0, 0);
            m_SentenceStructure.Margin = new Padding(3, 116, 3, 116);
            m_SentenceStructure.Name = "m_SentenceStructure";
            m_SentenceStructure.Size = new Size(732, 184);
            m_SentenceStructure.TabIndex = 1;
            m_DummyPanel.BackColor = Color.Linen;
            m_DummyPanel.Location = new Point(0, 0);
            m_DummyPanel.Size = m_SentenceStructure.Size;

            this.toolStripContainer2.ContentPanel.Controls.Add(m_ContainerPanel);
            m_ContainerPanel.Dock = DockStyle.Fill;
            m_ContainerPanel.Controls.Add(m_SentenceStructure);
            m_ContainerPanel.Controls.Add(m_DummyPanel);    // SentenceStrucureの再構成中のみ見せるパネルで、Containerのサイズを維持するためのもの.
            m_ContainerPanel.AutoScroll = true;
            m_SentenceStructure.OnLayoutChanged += new EventHandler(m_SentenceStructure_OnLayoutChanged);
            m_SentenceStructure.ScrollResetRequested += new EventHandler(m_SentenceStructure_ScrollResetRequested);

            m_SentenceStructure.DispMode = DepEditSettings.Current.DispMode;
            m_SentenceStructure.UpdateGuidePanel += new UpdateGuidePanelDelegate(m_SentenceStructure_UpdateGuidePanel);
            m_SentenceStructure.UpdateAttributePanel += new UpdateAttributePanelDelegate(m_SentenceStructure_UpdateAttributePanel);
            m_SentenceStructure.OnSentenceNoChanged += new EventHandler(m_SentenceStructure_OnSentenceNoChanged);
            m_SentenceStructure.SaveRequested += toolStripButton8_Click;
            m_SentenceStructure.CreateLinkModeChanged += m_SentenceStructure_CreateLinkModeChanged;
            FontDictionary.Current.FontChanged += new EventHandler(FontChangedHandler);
            m_CenterWordPos = -1;
            m_Service = new DepEditService();
            m_instance = this;
            m_Corpus = null;
            m_SentenceStructure.EditMode = false;
            m_IsEditMode = false;
        }

        private void m_SentenceStructure_CreateLinkModeChanged(object sender, EventArgs e)
        {
            this.toolStripButton15.Checked = m_SentenceStructure.CreateLinkMode;
        }

        void FontChangedHandler(object sender, EventArgs e)
        {
            m_SentenceStructure.BackColor = ColorTranslator.FromHtml(DepEditSettings.Current.Background);
            m_SentenceStructure.Refresh();
        }

        static public DepEditControl Instance
        {
            get { return m_instance; }
        }

        public Corpus Cps
        {
            get { return m_Corpus; }
        }

        private bool m_IsEditMode;
        public bool IsEditMode
        {
            get { return m_IsEditMode; }
            set
            {
                if (value)
                {
                    // ViewMode -> EditMode
                    BeginSentenceEdit(m_Corpus, m_SentenceID, m_CenterWordPos, m_TargetProject);
                }
                else
                {
                    // EditMode -> ViewMode
                    EndEditing();
                }
                m_IsEditMode = value;
            }
        }

        /// <summary>
        /// Dependency構造の編集を開始する
        /// </summary>
        /// <param name="cps">コーパス</param>
        /// <param name="sid">文番号</param>
        /// <param name="cid">中心語の位置（強調表示に使用. 無指定[-1]でも可）</param>
        public bool BeginSentenceEdit(Corpus cps, int sid, int cid, int projid)
        {
            // 現在の状態が編集中なら、まずその編集を終わらせるかどうかを確認する。
            EndEditing();

            m_SentenceID = sid;
            m_CenterWordPos = cid;
            m_Corpus = cps;
            if (!m_Corpus.Mutex.WaitOne(10, false))
            {
                MessageBox.Show("DepEditControl: Could not lock the Corpus.");
                return true;
            }
            try
            {
                // コーパスよりSentenceを取得する
                Sentence sen = m_Service.Open(m_Corpus, sid, HandleUnlockRequest);
                if (sen == null)
                {
                    MessageBox.Show("DepEditControl: No more sentence found, or sentence ID is invalid.");
                    Terminate();
                    return false;
                }

                // Projectを決める
                IList<Project> projs = m_Service.RetrieveProjects();
                if (projid < 0 && projs.Count > 0)
                {
                    projid = projs[0].ID;
                }
                if (projs.FirstOrDefault(p => p.ID == projid) == null)
                {
                    MessageBox.Show(string.Format("DepEditControl: Invalid Project ID: {0}.", projid));
                    Terminate();
                    return true;
                }
                m_Service.SetupProject(projid);
                m_TargetProject = projid;
                this.m_SentenceStructure.TargetProjectId = projid;

                // cidをDepEditServiceに伝える。
                var words = sen.GetWords(m_TargetProject);
                if (cid >= 0 && cid < words.Count)
                {
                    m_CenterWordStartAt = words[cid].StartChar;
                }
                else
                {
                    m_CenterWordStartAt = sen.StartChar;
                }
                m_Service.CenterWordStartAt = m_CenterWordStartAt;

                // 辞書設定をServiceに通知
                foreach (DictionarySettingItem item in DictionarySettings.Instance)
                {
                    try
                    {
                        Dictionary dict = Dictionary.Create(item.Path, item.Name, item.IsCompoundWordDic);
                        m_Service.AddReferenceDictionary(dict);
                    }
                    catch (Exception ex)
                    {
                        ErrorReportDialog dlg = new ErrorReportDialog("Failed to add Reference Dictionary. Please review your dictionary settings.", ex);
                        dlg.ShowDialog();
                    }
                }

                m_LexemeSelector = new LexemeSelectionGrid(m_Corpus, m_Service);
                m_MWEUploadSelector = new MWEUploadGrid(m_Corpus, m_Service, m_Service);
                m_MWEDownloadSelector = new MWEDownloadGrid(m_Corpus, m_Service, m_Service);

                m_SentenceStructure.SetSentence(sen, m_Service, m_LexemeSelector, m_MWEUploadSelector, m_MWEDownloadSelector);
                m_SentenceStructure.SetCenterWord(m_CenterWordPos);
                m_SentenceStructure.VerticalScroll.Value = m_SentenceStructure.VerticalScroll.Maximum;
                m_SentenceStructure.UpdateContents();
                m_SentenceStructure.EditMode = true;

                m_IsEditMode = true;

            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Starting Dependency Edit", ex);
                dlg.ShowDialog();
                Terminate();
            }
            finally
            {
                if (m_Corpus != null)
                {
                    m_Corpus.Mutex.ReleaseMutex();
                }
                var h = this.EditContextChanged;
                if (h != null)
                {
                    h(m_Service.OpContext);
                }
            }
            return true;
        }

        /// <summary>
        /// 編集を終了する。セーブする必要がある場合はユーザーに問い合わせてCommitする。
        /// </summary>
        /// <returns>Cancelする場合はfalseを返す</returns>
        public bool EndEditing()
        {
            try
            {
                bool attributeUpdated = (m_instance.AttributeChanged != null) ? m_instance.AttributeChanged() : false;

                if (m_Service.CanSave() || attributeUpdated)
                {
                    DialogResult res = MessageBox.Show("Save Current Changes?", "DependencyEdit", MessageBoxButtons.YesNoCancel);
                    if (res == DialogResult.Yes)
                    {
                        if (!Save())
                        {
                            return false;
                        }
                        m_Service.CannotSave(); // CanSave状態をfalseにする
                    }
                    else if (res == DialogResult.No)
                    {
                        m_Service.CannotSave(); // CanSave状態をfalseにする
                    }
                    else if (res == DialogResult.Cancel)
                    {
                        return false;
                    }
                }
                m_Service.Close();
                if (this.InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (m_LexemeSelector != null) m_LexemeSelector.Dispose();
                        if (m_MWEUploadSelector != null) m_MWEUploadSelector.Dispose();
                    }));
                }
                else
                {
                    if (m_LexemeSelector != null) m_LexemeSelector.Dispose();
                    if (m_MWEUploadSelector != null) m_MWEUploadSelector.Dispose();
                }
                m_SentenceStructure.EditMode = false;
                m_IsEditMode = false;
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            finally
            {
                var h = this.EditContextChanged;
                if (h != null)
                {
                    if (this.InvokeRequired)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            h(null);
                        }));
                    }
                    else
                    {
                        h(null);
                    }
                }
            }
            return true;
        }

        public void Terminate()
        {
            m_Corpus = null;
            m_SentenceStructure.SetSentence(null, m_Service, m_LexemeSelector, m_MWEUploadSelector, m_MWEDownloadSelector);
            m_SentenceStructure.UpdateContents();
        }

        public bool HandleUnlockRequest(Type requestingService)
        {
            if (requestingService == typeof(IDepEditService))
            {
                // 自分自身からのUnlockリクエストは無視する.
                return true;
            }
            if (!m_Service.CanSave())
            {
                EndEditing();
                return true;
            }
            return false;
        }

        private bool Save()
        {
            if (!CheckForUnassignedLexeme())
            {
                MessageBox.Show("Cannot save: Incomplete word exists.", "DependencyEdit", MessageBoxButtons.OK);
                return false;
            }

            var h = this.Saving;
            if (h != null)
            {
                h();
            }

            m_Service.Commit();

            return true;
        }


        /// <summary>
        /// POSが"Unassigned"になっているWordが存在するかチェックする.
        /// 存在すればfalseを返す.
        /// </summary>
        /// <returns></returns>
        private bool CheckForUnassignedLexeme()
        {
            var words = m_SentenceStructure.Model.GetWords(m_TargetProject);
            foreach (var w in words)
            {
                if (w.Lex.PartOfSpeech == PartOfSpeech.Default)
                {
                    return false;
                }
            }
            return true;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            m_SentenceStructure.Undo();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            m_SentenceStructure.Redo();
        }

        /// <summary>
        /// アイドル時の処理として、UI状態を更新する。
        /// Programから呼び出す必要がある。
        /// </summary>
        public static void UIUpdate()
        {
            if (m_instance == null || m_instance.m_SentenceStructure == null) return;
            m_instance.toolStripButton1.Enabled = (m_instance.m_IsEditMode && m_instance.m_SentenceStructure.CanUndo());
            m_instance.toolStripButton2.Enabled = (m_instance.m_IsEditMode && m_instance.m_SentenceStructure.CanRedo());
            // AttributePanelに更新がある場合はFunc<bool>を通じてその状態を加味してSaveボタンの有効化を行う
            // (AttributePanelの編集とDepEditPanelの編集をUndo/Redo処理を含めて統合すれば不要となる)
            bool attributeUpdated = (m_instance.AttributeChanged != null) ? m_instance.AttributeChanged() : false;
            m_instance.toolStripButton8.Enabled = (m_instance.m_IsEditMode && (m_instance.m_SentenceStructure.CanSave() || attributeUpdated));
            m_instance.toolStripButton9.Enabled = (m_instance.ChangeSenenceRequested != null);
            m_instance.toolStripButton10.Enabled = (m_instance.ChangeSenenceRequested != null);
            m_instance.toolStripButton6.Checked = (m_instance.m_Corpus != null && m_instance.m_IsEditMode);
            m_instance.toolStripButton6.Enabled = (m_instance.m_Corpus != null);
            m_instance.toolStripButton14.Enabled = (m_instance.m_IsEditMode && m_instance.m_Corpus != null && m_instance.m_SentenceStructure.Model != null);
            m_instance.toolStripButton16.Enabled = (m_instance.m_Corpus != null && m_instance.m_SentenceStructure.CanAutoDetectMWE());
            m_instance.toolStripButton17.Enabled = (m_instance.m_Corpus != null && m_instance.m_SentenceStructure.CanAutoDetectMWE());
        }

        /// <summary>
        /// Saveボタンが押されたときの処理。
        /// 編集内容をコミットし、同じ文の編集を続ける。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Save())
                {
                    return;
                }
                Sentence sen = m_Service.ReOpen();
                m_SentenceStructure.SetSentence(sen, m_Service, m_LexemeSelector, m_MWEUploadSelector, m_MWEDownloadSelector);
                m_SentenceStructure.SetCenterWord(m_CenterWordPos);
                m_SentenceStructure.UpdateContents();
                // 辞書を再追加
                foreach (DictionarySettingItem item in DictionarySettings.Instance)
                {
                    Dictionary dict = Dictionary.Create(item.Path, item.Name, item.IsCompoundWordDic);
                    m_Service.AddReferenceDictionary(dict);
                }
                // cidをDepEditServiceに再設定
                m_Service.CenterWordStartAt = m_CenterWordStartAt;

                // ServiceをReOpenしたので、AttributePanelに新しいContextを通知
                var h = this.EditContextChanged;
                if (h != null)
                {
                    h(m_Service.OpContext);
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
        }

        /// <summary>
        /// EditModeボタンが押されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            this.IsEditMode = !this.IsEditMode;
        }

        /// <summary>
        /// GraphVizのDOTファイルへエクスポートする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            FileDialog fd = new SaveFileDialog();
            fd.Filter = "GraphViz DOT files (*.dot)|*.dot|All files (*.*)|*.* ";
            if (fd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            WriteToDotFile(fd.FileName);
        }

        public void WriteToDotFile(string filename)
        {
            m_SentenceStructure.WriteToDotFile(filename);
        }

        // "View Diagonal"
        private void verticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_SentenceStructure.DispMode = DispModes.Diagonal;
            DepEditSettings.Current.DispMode = m_SentenceStructure.DispMode;
        }

        // "View Horizontal"
        private void horizonalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_SentenceStructure.DispMode = DispModes.Horizontal;
            DepEditSettings.Current.DispMode = m_SentenceStructure.DispMode;
        }

        // View "Morphemes"
        private void morphemesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_SentenceStructure.DispMode = DispModes.Morphemes;
            DepEditSettings.Current.DispMode = m_SentenceStructure.DispMode;
        }

        // "Zoom"
        void toolStripButton3_Click(object sender, System.EventArgs e)
        {
            m_SentenceStructure.Zoom();
        }

        // "Pan"
        void toolStripButton4_Click(object sender, System.EventArgs e)
        {
            m_SentenceStructure.Pan();
        }

        // "1:1"
        void toolStripButton5_Click(object sender, System.EventArgs e)
        {
            m_SentenceStructure.ZoomToDefault();
        }

        void m_SentenceStructure_UpdateGuidePanel(Corpus corpus, Lexeme lex)
        {
            if (this.UpdateGuidePanel != null)
            {
                UpdateGuidePanel(corpus, lex);
            }
        }

        void m_SentenceStructure_UpdateAttributePanel(Corpus corpus, object source)
        {
            if (this.UpdateAttributePanel != null)
            {
                UpdateAttributePanel(corpus, source);
            }
        }

        void m_SentenceStructure_OnLayoutChanged(object sender, EventArgs e)
        {
            m_DummyPanel.Width = m_SentenceStructure.Width;
            m_DummyPanel.Height = m_SentenceStructure.Height;
        }

        void m_SentenceStructure_ScrollResetRequested(object sender, EventArgs e)
        {
            m_ContainerPanel.HorizontalScroll.Value = 0;
            m_ContainerPanel.VerticalScroll.Value = 0;
        }

        void m_SentenceStructure_OnSentenceNoChanged(object sender, EventArgs e)
        {
            if (m_Corpus != null && m_SentenceStructure.Model != null)
            {
                this.toolStripTextBox1.Text = m_SentenceStructure.Model.ID.ToString();
                this.toolStripTextBox2.Text = m_Corpus.Name;
            }
            else
            {
                this.toolStripTextBox1.Text = string.Empty;
                this.toolStripTextBox2.Text = string.Empty;
            }
        }

        // Goto Previous Sentenceの処理
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (m_Corpus == null || m_SentenceStructure.Model == null) return;
            if (this.ChangeSenenceRequested != null)
            {
                ChangeSenenceRequested(m_Corpus, m_SentenceStructure.Model.ID, -1, false);
            }
        }

        // Goto Next Sentenceの処理
        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (m_Corpus == null || m_SentenceStructure.Model == null) return;
            if (this.ChangeSenenceRequested != null)
            {
                ChangeSenenceRequested(m_Corpus, m_SentenceStructure.Model.ID, 1, false);
            }
        }

        // Goto Previous Sentence in KWIC Listの処理
        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            if (m_Corpus == null || m_SentenceStructure.Model == null) return;
            if (this.ChangeSenenceRequested != null)
            {
                ChangeSenenceRequested(m_Corpus, m_SentenceStructure.Model.ID, -1, true);
            }
        }

        // Goto Next Sentence in KWIC Listの処理
        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            if (m_Corpus == null || m_SentenceStructure.Model == null) return;
            if (this.ChangeSenenceRequested != null)
            {
                ChangeSenenceRequested(m_Corpus, m_SentenceStructure.Model.ID, 1, true);
            }
        }

        // Goto Line Number（EditBoxでMouseDown時）の処理
        private void toolStripTextBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (m_Corpus == null || m_SentenceStructure.Model == null) return;
            using (LineNumberInputForm dlg = new LineNumberInputForm())
            {
                Point loc = this.toolStrip1.PointToScreen(e.Location);
                loc.Y += 50;
                dlg.Location = loc;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    int ln = dlg.LineNumber;
                    if (ln >= 0 && this.ChangeSenenceRequested != null)
                    {
                        ChangeSenenceRequested(m_Corpus, -1, ln, false);
                    }
                }
            }
        }

        // Run cabocha to reanalyze the text
        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            if (m_Corpus == null || m_SentenceStructure.Model == null) return;

            try
            {
                var sen = m_SentenceStructure.Model;

                var cabocha = new CabochaRunner();

                var segs = m_Service.GetNestTags();
                string output = cabocha.ParseWithSegmentInfo(m_Corpus, sen, segs);

                m_Service.UpdateAllBunsetsu(sen, output);
                m_SentenceStructure.UpdateContents();
            }
            catch (Exception ex)
            {
                var edlg = new ErrorReportDialog("Command failed.", ex);
                edlg.ShowDialog();
            }
        }

        // MWE Upload
        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            m_SentenceStructure.UploadMWE();
        }

        // MWE Download
        private void toolStripButton17_Click(object sender, EventArgs e)
        {
            m_SentenceStructure.DownloadMWE();
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            var f = m_SentenceStructure.CreateLinkMode;
            m_SentenceStructure.CreateLinkMode = !f;
        }

        private SizeF? m_Scale = null;

        private void DepEditControl_Paint(object sender, PaintEventArgs e)
        {
            // High DPI対応
            if (m_Scale == null)
            {
                m_Scale = new SizeF(e.Graphics.DpiX / 96.0F, e.Graphics.DpiY / 96.0F);
                var xscale = m_Scale?.Width;
                if (xscale > 0.5f && xscale < 6.0f)
                {
                    this.toolStripTextBox1.Width = (int)((float)this.toolStripTextBox1.Width * xscale);
                }
            }
        }

        /// <summary>
        /// DependencyEdit Settings Dialog を表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton18_Click(object sender, EventArgs e)
        {
            m_SentenceStructure?.EditSettings(this.toolStrip1.PointToScreen(new Point(0, 0)));
        }
    }
}
