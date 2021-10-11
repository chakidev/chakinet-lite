using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ChaKi.Common.Settings;
using ChaKi.Common.Widgets;
using ChaKi.Entity.Corpora;
using ChaKi.GUICommon;
using ChaKi.Service.DependencyEdit;
using PopupControl;
using ChaKi.Service.Lexicons;
using System.Linq;
using ChaKi.Common.SequenceMatcher;

namespace DependencyEditSLA.Widgets
{
    public partial class LexemeSelectionGrid : Form
    {
        private Corpus m_Corpus;
        private IList<LexemeCandidate> m_Candidates;
        private ILexiconService m_Service;
        private List<LP> m_PropertyColumns;
        private DataGridViewCell m_CurrentCell;
        private int m_PropertyColumnStart;

        private Dictionary<int, Popup> m_Popups;
        private PropertyTree m_POSPropTree;
        private PropertyTree m_CFormPropTree;
        private PropertyTree m_CTypePropTree;

        public static LexemeSelectionSettings Settings;

        static LexemeSelectionGrid()
        {
            Settings = DepEditSettings.Current.LexemeSelectionSettings;
        }

        /// <summary>
        /// 選択された結果のLexeme
        /// </summary>
        public LexemeCandidate Selection { get; set; }

        public event EventHandler LexemeSelected;

        public LexemeSelectionGrid(Corpus cps, ILexiconService svc)
        {
            m_Service = svc;
            m_Popups = new Dictionary<int, Popup>();
            m_Corpus = cps;
            m_POSPropTree = new PropertyTree();
            m_CFormPropTree = new PropertyTree();
            m_CTypePropTree = new PropertyTree();
            m_PropertyColumns = new List<LP>();

            InitializeComponent();

            DataGridView dg = this.dataGridView1;
            dg.Rows.Clear();
            dg.Columns.Clear();

            // カラム作成
            DataGridViewColumn col;
            col = new DataGridViewLinkColumn()
            {
                Name = "ID",
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle() { Alignment = DataGridViewContentAlignment.MiddleRight },
                LinkColor = Color.Blue,
                VisitedLinkColor = Color.DarkBlue
            };
            dg.Columns.Add(col);
            col = new DataGridViewTextBoxColumn() { Name = "Dictioanry", ReadOnly = true };
            dg.Columns.Add(col);

            m_PropertyColumnStart = dg.Columns.Count;

            m_Popups.Clear();
            int c = dg.Columns.Count;
            foreach (PropertyBoxItemSetting p in PropertyBoxSettings.Instance.Settings)
            {
                if (!p.IsVisible) continue;
                LP? lp = Lexeme.FindProperty(p.TagName);
                if (!lp.HasValue)
                {
                    continue;
                }
                m_PropertyColumns.Add(lp.Value);
                switch (lp.Value)
                {
                    case LP.PartOfSpeech:
                        m_Popups.Add(c, new Popup(m_POSPropTree) { Resizable = true, DropShadowEnabled = true, Size = Settings.POSPropTreeSize });
                        break;
                    case LP.CType:
                        m_Popups.Add(c, new Popup(m_CTypePropTree) { Resizable = true, DropShadowEnabled = true, Size = Settings.CTypePropTreeSize });
                        break;
                    case LP.CForm:
                        m_Popups.Add(c, new Popup(m_CFormPropTree) { Resizable = true, DropShadowEnabled = true, Size = Settings.CFormPropTreeSize });
                        break;
                }
                col = new DataGridViewTextBoxColumn() { Name = p.DisplayName };
                if (lp.Value == LP.Surface)
                {
                    col.ReadOnly = true;
                }
                dg.Columns.Add(col);
                c++;
            }
            col = new DataGridViewTextBoxColumn()
            {
                Name = "Frequency",
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle() { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dg.Columns.Add(col);

            // 初期値設定を反映
            for (int i = 0; i < Settings.ColumnWidths.Length; i++)
            {
                if (i < this.dataGridView1.Columns.Count)
                {
                    this.dataGridView1.Columns[i].Width = Math.Max(10, Settings.ColumnWidths[i]);
                }
            }
            Rectangle maxRect = Screen.GetWorkingArea(this);
            this.Location = new Point(
                Math.Min(maxRect.Width, Math.Max(Settings.InitialLocation.Location.X, 0)),
                Math.Min(maxRect.Height, Math.Max(Settings.InitialLocation.Location.Y, 0)));

            this.Size = new Size(
                Math.Min(maxRect.Width, Math.Max(Settings.InitialLocation.Size.Width, 100)),
                Math.Min(maxRect.Height, Math.Max(Settings.InitialLocation.Size.Height, 100)));

            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = false;
            this.dataGridView1.MultiSelect = true;
            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;

            m_CurrentCell = null;
        }

        public void BeginSelection(string surface, Lexeme currentLexeme, IList<Word> words = null)
        {
            DataGridView dg = this.dataGridView1;
            dg.Rows.Clear();
            try
            {
                m_Candidates = m_Service.FindAllLexemeCandidates(surface);
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Error while reading Lexicon. Please make sure DepEdit is in Edit Mode.", ex);
                dlg.ShowDialog();
                return;
            }

            this.Selection = (from c in this.m_Candidates where c.Lexeme == currentLexeme select c).FirstOrDefault();

            foreach (var cand in m_Candidates)
            {
                int r = dg.Rows.Add();

                DataGridViewRow row = dg.Rows[r];

                var lex = cand.Lexeme;
                var mwe_match = cand.Match;
                string idstr;
                if (lex.ID < 0)
                {
                    if (lex.SID != null)
                    {
                        idstr = string.Format("({0})", lex.SID);
                    }
                    else
                    {
                        idstr = "-";
                    }
                }
                else if (lex.Dictionary != null)
                {
                    idstr = string.Format("({0})", lex.ID);
                }
                else
                {
                    idstr = lex.ID.ToString();
                }
                int col = 0;
                row.Cells[col++].Value = idstr;            // "ID" Col.
                row.Cells[col++].Value = lex.Dictionary;   // "Dictionary" Col.
                for (int i = 0; i < m_PropertyColumns.Count; i++)
                {
                    row.Cells[col++].Value = lex.GetStringProperty(m_PropertyColumns[i]);
                }
                row.Cells[col++].Value = lex.Frequency;
                if (mwe_match != null)
                {
                    row.Cells[2].Value = MatchingResult.MWEToString(mwe_match.MWE, words, mwe_match);
                }

                if (!lex.CanEdit)
                {
                    row.ReadOnly = true;
                    if (mwe_match != null)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightYellow;
                    }
                    else if (lex.Dictionary != null)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightBlue;
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.LightGray;
                    }
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                }
            }
            this.dataGridView1.MouseClick += new MouseEventHandler(dataGridView1_MouseClick);
            this.dataGridView1.CellContentClick += dataGridView1_CellContentClick;

            // 参照用辞書を含め、全ての使用可能なPOS, CType, CFormタグのリストを得て、PropTreeにセットする.
            Dictionary<string, IList<PartOfSpeech>> pos;  // stringはDictionary名（カレントコーパスは"Default"）
            Dictionary<string, IList<CType>> ctypes;
            Dictionary<string, IList<CForm>> cforms;

            m_Service.GetLexiconTags(out pos, out ctypes, out cforms);

            m_POSPropTree.PopulateWithPOSSelections(pos);
            m_CTypePropTree.PopulateWithCTypeSelections(ctypes);
            m_CFormPropTree.PopulateWithCFormSelections(cforms);
        }

        // 親PopupをResizableとするのに必要
        protected override void WndProc(ref Message m)
        {
            Popup p = Parent as Popup;
            if (p != null && p.ProcessResizing(ref m))
            {
                return;
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //if (keyData == Keys.Enter)
            //{
            //    DetermineResult(true);
            //    return true;
            //}
            //else if (keyData == Keys.Escape)
            //{
            //    DetermineResult(false);
            //    return true;
            //}
            return false;
        }

        private void DetermineResult(bool OkCancel)
        {
            if (OkCancel)
            {
                int index = (this.dataGridView1.SelectedCells.Count > 0) ? this.dataGridView1.SelectedCells[0].RowIndex : -1;
                if (index >= 0 && index < m_Candidates.Count)
                {
                    try
                    {
                        var lex = m_Candidates[index].Lexeme;
                        var props = ToLexemePropertyArray(index, lex);
                        var customprop = lex.CustomProperty;
                        if (lex.Dictionary != null)
                        {
                            lex = null;
                        }
                        m_Service.CreateOrUpdateLexeme(ref lex, props, customprop);
                        m_Candidates[index].Lexeme = lex;

                        this.Selection = m_Candidates[index];
                        if (LexemeSelected != null)
                        {
                            LexemeSelected(this, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorReportDialog dlg = new ErrorReportDialog("Error while updating Lexicon.", ex);
                        dlg.ShowDialog();
                        return;
                    }
                }
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
            }
            Popup p = this.Parent as Popup;
            if (p != null)
            {
                p.Close(ToolStripDropDownCloseReason.ItemClicked);
            }
        }

        // Gridの行(row)の内容を元にLexemeの内容を変更する.
        private string[] ToLexemePropertyArray(int row, Lexeme lex)
        {
            this.dataGridView1.EndEdit();
            string[] props = new string[(int)LP.Max];
            for (int i = 0; i < (int)LP.Max; i++) props[i] = string.Empty;

            // Columnはm_PropertyColumns[]の順に並んでいる。最初のPropert ColumnのOffsetは2.
            for (int i = 0; i < m_PropertyColumns.Count; i++)
            {
                props[(int)m_PropertyColumns[i]] = (this.dataGridView1[i + 2, row].Value as string) ?? string.Empty;
            }
            return props;
        }

        private void dataGridView1_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < m_Candidates.Count)
            {
                foreach (DataGridViewCell cell in this.dataGridView1.SelectedCells)
                {
                    cell.Selected = false;
                }
                foreach (DataGridViewCell cell in this.dataGridView1.Rows[e.RowIndex].Cells)
                {
                    cell.Selected = true;
                }
                Application.DoEvents();
                Thread.Sleep(100);
                DetermineResult(true);
            }
        }

        private void LexemeSelectionGrid_Shown(object sender, EventArgs e)
        {
            foreach (DataGridViewCell cell in this.dataGridView1.SelectedCells)
            {
                cell.Selected = false;
            }
            if (m_Candidates == null)
            {
                return;
            }
            for (int i = 0; i < m_Candidates.Count; i++)
            {
                if (m_Candidates[i] == this.Selection)
                {
                    this.dataGridView1.Rows[i].Cells[0].Selected = true;
                    this.dataGridView1.CurrentCell = this.dataGridView1[0, i];
                    break;
                }
            }
        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = (DataGridView)sender;
            if (dgv.Columns[e.ColumnIndex].Name == "ID")
            {
                int index = (this.dataGridView1.SelectedCells.Count > 0) ? this.dataGridView1.SelectedCells[0].RowIndex : -1;
                if (index >= 0 && index < m_Candidates.Count)
                {
                    var cand = m_Candidates[index];
                    if (cand != null && cand.Url != null)
                    {
                        // Cradleの単語画面をブラウザで表示
                        var url = $"{cand.Url}word?id={cand.Lexeme.SID}";
                        System.Diagnostics.Process.Start(url);

                        // set "visited"
                        var cell = (DataGridViewLinkCell)dgv[e.ColumnIndex, e.RowIndex];
                        cell.LinkVisited = true;
                    }
                }
            }
        }

        // マウス右クリック→Tree Menu表示
        void dataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            Point p = e.Location;
            this.dataGridView1.PointToClient(p);
            DataGridView.HitTestInfo info = this.dataGridView1.HitTest(p.X, p.Y);
            int c = info.ColumnIndex;
            int r = info.RowIndex;
            if (c < 0 || r < 0) return;

            m_CurrentCell = this.dataGridView1[c, r];
            if (m_CurrentCell.ReadOnly) return;

            this.dataGridView1.Rows[r].Selected = true;

            Popup popup;
            if (m_Popups.TryGetValue(c, out popup))
            {
                ((PropertyTree)(popup.Content)).NodeHit += new EventHandler(LexemeSelectionGrid_NodeHit);
                popup.Show(Cursor.Position.X, Cursor.Position.Y);
            }
        }

        void LexemeSelectionGrid_NodeHit(object sender, EventArgs e)
        {
            if (!(sender is PropertyTree)) return;

            PropertyTree tree = (PropertyTree)sender;

            if (m_CurrentCell != null)
            {
                m_CurrentCell.Value = tree.Selection;
            }

            tree.NodeHit -= LexemeSelectionGrid_NodeHit;
            m_CurrentCell = null;
            Popup p = (Popup)(tree.Parent);
            p.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DetermineResult(true);
        }

        private void LexemeSelectionGrid_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 初期値設定をセーブ
            int cols = this.dataGridView1.Columns.Count;
            Settings.ColumnWidths = new int[cols];
            for (int i = 0; i < cols; i++)
            {
                Settings.ColumnWidths[i] = this.dataGridView1.Columns[i].Width;
            }
            Settings.InitialLocation = new Rectangle(this.Location, this.Size);
            for (int i = 0; i < m_PropertyColumns.Count; i++)
            {
                Popup p;
                if (!m_Popups.TryGetValue(m_PropertyColumnStart + i, out p)) continue;
                switch (m_PropertyColumns[i])
                {
                    case LP.PartOfSpeech:
                        Settings.POSPropTreeSize = p.Size; break;
                    case LP.CForm:
                        Settings.CFormPropTreeSize = p.Size; break;
                    case LP.CType:
                        Settings.CTypePropTreeSize = p.Size; break;
                }
            }
        }
    }
}
