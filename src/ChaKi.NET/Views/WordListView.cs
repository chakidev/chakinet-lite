using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;
using ChaKi.GUICommon;
using ChaKi.ToolDialogs;
using ChaKi.Common;
using ChaKi.Common.Settings;
using System.Threading;

namespace ChaKi.Views
{
    public partial class WordListView : UserControl, IChaKiView
    {
        private LexemeCountList m_Model;

        // m_Modelから変換により作成され、縮退・ソートなど変更を行うための内部データ
        // データの関連は以下の通り：
        //    m_Model(LexemeCountList) --> m_Table(DataTable) --> m_ActiveViewModel(DataView) --> dataGridView1(DataGridView)
        //                             縮退               ソート                      1:1
        //
        private DataTable m_Table;          // Total行を含まないDataTable. DataView経由で使う
        private DataView m_ActiveViewModel; // Sortを施したm_TableのView
        private DataTable m_TotalTable;     // Total行単独のDataTable
        private bool m_IsDirty;             // Totalの再計算が必要か

        private int m_FirstCountColIndex;   // corpusごとのカウント列の開始列index
        private int m_TotalColIndex;        // "All"列の列index
        private int m_RatioColIndex;        // "Ratio"列の列index
        public LexemeListFilter LexFilterList { get; private set; }

        private List<string> m_TagNames;
        private List<string> m_DisplayNames;
        private Dictionary<int, LP> m_ToLPMapping;

        //TODO: The followings should be part of Persistent Setting
        private int m_DefaultColWidth = 70;
        private int m_DefaultFiltetedColWidth = 20;
        private int m_DefaultCounterColWidth = 50;

        public DataGridView Grid { get { return this.dataGridView1; } }

        public event EventHandler OccurrenceRequested;   // Row Header右クリック→Show Occurenceコマンド実行依頼(to MainForm)
        
        public WordListView()
        {
            m_Model = null;
            LexFilterList = new LexemeListFilter(1); // default
            m_TagNames = new List<string>();
            m_DisplayNames = new List<string>();
            m_ToLPMapping = new Dictionary<int, LP>();
            UpdatePropertySetting();

            InitializeComponent();

            InitializeGrid();

            m_IsDirty = false;

            PropertyBoxSettings.Instance.SettingChanged += new EventHandler(Instance_SettingChanged);
            FontDictionary.Current.FontChanged += HandleFontChanged;
        }

        void Instance_SettingChanged(object sender, EventArgs e)
        {
            UpdatePropertySetting();
            Refresh();
        }

        private void UpdatePropertySetting()
        {
            m_TagNames.Clear();
            m_DisplayNames.Clear();
            m_ToLPMapping.Clear();
            int row = 0;
            foreach (PropertyBoxItemSetting setting in PropertyBoxSettings.Instance.Settings)
            {
                if (!setting.IsVisible)
                {
                    continue;
                }
                LP? lp = Lexeme.FindProperty(setting.TagName);
                if (!lp.HasValue)
                {
                    continue;
                }
                m_TagNames.Add(setting.TagName);
                m_DisplayNames.Add(setting.DisplayName);
                m_ToLPMapping.Add(row, lp.Value);
                row++;
            }
        }

        public void SetModel(LexemeCountList model, List<Corpus> corpusList, int lexCount, int pivotPos)
        {
            model.CorpusList = corpusList;
            model.LexSize = lexCount;
            model.PivotPos = pivotPos;
            SetModel(model);
        }

        public void SetModel(object model)
        {
            if (model != null && !(model is LexemeCountList))
            {
                throw new ArgumentException("Assigning invalid model to WordListView");
            }
            m_Model = (LexemeCountList)model;
            if (m_Model != null)
            {
                m_Model.OnLexemeCountAdded += new AddLexemeCountEventHandler(this.AddLexemeCountHandler);
                this.LexFilterList = m_Model.LexListFilter;
                InitializeGrid();
                UpdateViewModel();
            }
        }

        public void FinalizeDisplay()
        {
            this.LexFilterList.Default();
            UpdateViewModel();
        }

        public void SetVisible(bool f)
        {
            this.Visible = f;
        }

        public void SuspendUpdatingTotal()
        {
            this.timer1.Stop();
        }

        public void ResumeUpdatingTotal()
        {
            this.timer1.Start();
        }

        protected void InitializeGrid()
        {
            this.dataGridView1.GridColor = Color.DarkGray;
            this.dataGridView1.RowHeadersWidth = 100;

            var defaultRowHeight = 18;
            var font = GUISetting.Instance.GetBaseAnsiFont();
            if (font != null)
            {
                defaultRowHeight = font.Height + 1;
            }
            this.dataGridView1.RowTemplate.Height = defaultRowHeight;

            m_Table = new DataTable();
            m_TotalTable = new DataTable();
            m_ActiveViewModel = m_Table.DefaultView;

            // 基本属性の表示列を生成する
            this.dataGridView1.Columns.Clear();
            int ncol = 0;

            if (m_Model == null)
            {
                return;
            }

            Color lexBackColor = Color.Azure;   // LexemeごとにIvoryと交代
            for (int i = 0; i < m_Model.LexSize; i++)
            {
                if (i == m_Model.PivotPos)
                {
                    lexBackColor = Color.DarkSalmon;
                }
                else
                {
                    lexBackColor = (lexBackColor != Color.Ivory) ? Color.Ivory : Color.Azure;
                }
                for (int j = 0; j < m_TagNames.Count; j++)
                {
                    DataGridViewColumn col = new DataGridViewTextBoxColumn();
                    col.Name = string.Format("{0}_{1}", m_DisplayNames[j], i);
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                    col.Width = m_DefaultColWidth;
                    col.DefaultCellStyle.BackColor = lexBackColor;
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                    col.Tag = string.Format("?{0}", i);
                    this.dataGridView1.Columns.Add(col);
                    ncol++;

                    m_Table.Columns.Add(col.Name, typeof(string));
                    m_TotalTable.Columns.Add(col.Name, typeof(string));
                }
            }
            m_FirstCountColIndex = ncol;
            // 検索するコーパスの数分だけ、カウント列を生成する
            var ncorpus = 0;
            foreach (Corpus c in m_Model.CorpusList)
            {
                DataGridViewColumn col = new DataGridViewTextBoxColumn();
                col.Name = c.Name;
                col.SortMode = DataGridViewColumnSortMode.Automatic;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.Width = m_DefaultCounterColWidth;
                col.DefaultCellStyle.BackColor = Color.LightGray;
                col.SortMode = DataGridViewColumnSortMode.Automatic;
                col.Tag = string.Format("#{0}", ncorpus++);
                this.dataGridView1.Columns.Add(col);
                ncol++;

                m_Table.Columns.Add(col.Name, typeof(long));
                m_TotalTable.Columns.Add(col.Name, typeof(long));
            }
            m_TotalColIndex = ncol;
            // TOTAL列を生成する
            {
                DataGridViewColumn col = new DataGridViewTextBoxColumn();
                col.Name = (m_Model.CorpusList.Count == 0)? "Frequency":"All";    // コーパス毎の集計がなければタイトルは単に"Frequency"とする
                col.SortMode = DataGridViewColumnSortMode.Automatic;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.Width = m_DefaultCounterColWidth;
                col.DefaultCellStyle.BackColor = Color.LightGray;
                col.SortMode = DataGridViewColumnSortMode.Automatic;
                this.dataGridView1.Columns.Add(col);
                ncol++;

                m_Table.Columns.Add(col.Name, typeof(long));
                m_TotalTable.Columns.Add(col.Name, typeof(long));
            }
            m_RatioColIndex = ncol;
            // TOTAL列を生成する
            {
                DataGridViewColumn col = new DataGridViewTextBoxColumn();
                col.Name = "Ratio(%)";
                col.SortMode = DataGridViewColumnSortMode.Automatic;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.Width = m_DefaultCounterColWidth;
                col.DefaultCellStyle.BackColor = Color.LightGray;
                col.SortMode = DataGridViewColumnSortMode.Automatic;
                this.dataGridView1.Columns.Add(col);
                ncol++;

                m_Table.Columns.Add(col.Name, typeof(double));
                m_TotalTable.Columns.Add(col.Name, typeof(double));
            }

            this.dataGridView1.Rows.Add();
            this.dataGridView1.Rows[0].Frozen = true;

            ApplyGridFont();

            ResumeUpdatingTotal();
        }

        /// <summary>
        /// ChaKiOptionのフォントが変更されたときのEvent Handler。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleFontChanged(object sender, EventArgs e)
        {
            ApplyGridFont();
        }

        void ApplyGridFont()
        {
            this.dataGridView1.Font = GUISetting.Instance.GetBaseAnsiFont();
        }

        private void AddLexemeCountHandler(object sender, AddLexemeCountEventArgs e)
        {
            this.Invoke(new AddLexemeCountDelegate(AddLexemeCount), new object[] { e });
        }

        delegate void AddLexemeCountDelegate( AddLexemeCountEventArgs e );

        private void AddLexemeCount( AddLexemeCountEventArgs e )
        {
            int rowindex = e.Index;
            if (rowindex < 0)
            {
                // 新しいLexemeList行を追加する必要がある場合
                rowindex = AddNewRow(e.Lex);
            }
            // 行のカウントを更新
            int corpusindex = m_Model.CorpusList.IndexOf(e.Cps);
            if (corpusindex < 0)
            {
                throw new Exception("Invalid Corpus in WordList Result.");
            }
            m_Table.Rows[rowindex][m_FirstCountColIndex + corpusindex] = e.Count;
            m_IsDirty = true;
        }


        /// <summary>
        /// Modelの内容からViewModelを生成しなおす。
        /// </summary>
        private void UpdateViewModel()
        {
            if (m_Model == null || m_Table == null)
            {
                return;
            }

            lock (m_Table)
            {
                var curSort = m_ActiveViewModel.Sort;

                m_Table.Rows.Clear();
                m_TotalTable.Rows.Clear();
                m_TotalTable.Rows.Add(m_TotalTable.NewRow());
                for (int i = 0; i < m_Model.CorpusList.Count; i++)
                {
                    m_TotalTable.Rows[0][m_FirstCountColIndex + i] = (long)0;
                }

                LexemeCountList tmpModel = new LexemeCountList(this.LexFilterList);  // カウント用

                foreach (LexemeCount lc in m_Model)
                {
                    LexemeCount lc0 = null;
                    if (tmpModel.TryGetValue(lc.LexList, out lc0))
                    {
                        int rowindex = tmpModel.IndexOf(lc0);
                        // 既に存在している行にカウントをマージする。
                        for (int i = 0; i < m_Model.CorpusList.Count; i++)
                        {
                            long c;
                            if (!lc.Counts.TryGetValue(m_Model.CorpusList[i], out c))
                            {
                                continue;
                            }
                            if (m_Table.Rows[rowindex][m_FirstCountColIndex + i] != null)
                            {
                                c += (long)m_Table.Rows[rowindex][m_FirstCountColIndex + i];
                            }
                            m_Table.Rows[rowindex][m_FirstCountColIndex + i] = c;
                        }
                    }
                    else
                    {
                        // 新しい行を追加する
                        tmpModel.Add(lc);
                        AddNewRow(lc.LexList);
                        int rowindex = m_Table.Rows.Count - 1;
                        for (int i = 0; i < m_Model.CorpusList.Count; i++)
                        {
                            long count;
                            if (lc.Counts.TryGetValue(m_Model.CorpusList[i], out count))
                            {
                                m_Table.Rows[rowindex][m_FirstCountColIndex + i] = count;
                            }
                        }
                    }
                }
                this.dataGridView1.RowCount = m_Table.Rows.Count + m_TotalTable.Rows.Count;

                // DataGridViewのカラム幅をフィルタに合わせる
                int col = 0;
                for (int i = 0; i < m_Model.LexSize; i++)
                {
                    for (int j = 0; j < m_TagNames.Count; j++)
                    {
                        LP lp;
                        if (!m_ToLPMapping.TryGetValue(j, out lp))
                        {
                            continue;
                        }
                        if (this.LexFilterList.IsFiltered(i, lp))
                        {
                            this.dataGridView1.Columns[col].Width = m_DefaultFiltetedColWidth;
                        }
                        else
                        {
                            this.dataGridView1.Columns[col].Width = m_DefaultColWidth;
                        }
                        col++;
                    }
                }

                m_ActiveViewModel.Sort = curSort;
                m_IsDirty = true;
            }
        }

        private int AddNewRow(LexemeList lexList)
        {
            m_Table.Rows.Add(m_Table.NewRow());
            int rowindex = m_Table.Rows.Count - 1;
            DataRow rowData = m_Table.Rows[rowindex];

            // 行にLexemeデータをセット
            int ncol = 0;
            for (int i = 0; i < lexList.Count; i++) {
                Lexeme lex = lexList[i];
                LexemeFilter filter = this.LexFilterList[i];
                for (int j = 0; j < m_TagNames.Count; j++)
                {
                    string val = "*";
                    LP lp;
                    if (!m_ToLPMapping.TryGetValue(j, out lp))
                    {
                        continue;
                    }
                    if (!filter.IsFiltered(lp))
                    {
                        val = lex.GetStringProperty(lp);
                    }
                    rowData[ncol++] = val;
                }
            }
//            this.dataGridView1.Rows[rowindex].HeaderCell.Value = rowindex;
            for (int i = 0; i < m_Model.CorpusList.Count; i++)
            {
                rowData[m_FirstCountColIndex + i] = (long)0;
            }

            rowData[m_TotalColIndex] = (long)0;
            rowData[m_RatioColIndex] = (double)0.0;

            // 最初のデータ追加時に、TOTAL行を初期化し、TOTALの更新を開始する
            if (rowindex == 0)
            {
                DataRow rowDataTotal = m_TotalTable.Rows[0];
                for (int i = 0; i < m_Model.CorpusList.Count; i++)
                {
                    rowData[m_FirstCountColIndex + i] = (long)0;
                }
                rowData[m_TotalColIndex] = (long)0;
                rowData[m_RatioColIndex] = (double)0.0;
                this.timer1.Start();
            }
            return rowindex;
        }

        /// <summary>
        /// TOTAL欄をupdateする
        /// </summary>
        private void UpdateTotal()
        {
            if (!m_IsDirty) return;
            if (m_Table == null || m_TotalTable == null) return;

            // dataGridViewの行数を更新
            int gridRowCount = m_Table.Rows.Count + m_TotalTable.Rows.Count;
            if (this.dataGridView1.RowCount != gridRowCount)
            {
                this.dataGridView1.RowCount = gridRowCount;
            }

            // 全体のTOTAL
            long total = 0;
            // 各コーパスごとの縦のTOTAL
            for (int i = 0; i < m_Model.CorpusList.Count; i++)
            {
                long ctotal = 0;
                foreach (DataRow rowData in m_Table.Rows)
                {
                    object val = rowData[m_FirstCountColIndex + i];
                    if (val != null && val is long)
                    {
                        ctotal += (long)val;
                    }
                }
                m_TotalTable.Rows[0][m_FirstCountColIndex + i] = ctotal;
                total += ctotal;
            }

            // 全体のTOTAL
            m_TotalTable.Rows[0][m_TotalColIndex] = total;
            m_TotalTable.Rows[0][m_RatioColIndex] = (double)100.0;

            // 各行横のTOTAL
            foreach (DataRow rowData in m_Table.Rows)
            {
                long sum = 0;
                for (int i = 0; i < m_Model.CorpusList.Count; i++)
                {
                    object val = rowData[m_FirstCountColIndex + i];
                    if (val != null && val is long)
                    {
                        sum += (long)val;
                    }
                }
                rowData[m_TotalColIndex] = sum;
                double ratio = (double)sum * 100.0 / (double)total;
                rowData[m_RatioColIndex] = ratio;//string.Format("{0:F1}", ratio);
            }
#if true
            m_IsDirty = false;
            for (int i = 0; i < m_Model.CorpusList.Count; i++)
            {
                this.dataGridView1.InvalidateColumn(m_FirstCountColIndex + i);
            }
            this.dataGridView1.InvalidateColumn(m_TotalColIndex);
            this.dataGridView1.InvalidateColumn(m_RatioColIndex);
            this.dataGridView1.InvalidateRow(0);
#endif
        }

        public void CutToClipboard()
        {
            // intentionally left blank
        }

        public void CopyToClipboard()
        {
            var data = this.dataGridView1.GetClipboardContent();
            if (data != null)
            {
                Clipboard.SetDataObject(data);
            }
        }

        public void PasteFromClipboard()
        {
            // intentionally left blank
        }

        public bool CanCut { get { return false; } }    // always false

        public bool CanCopy
        {
            get
            {
                return true;
            }
        }

        public bool CanPaste { get { return false; } }    // always false

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (m_ActiveViewModel == null) return;
            int r = e.RowIndex;
            int c = e.ColumnIndex;
            //            if (c > m_ActiveViewModel.this.ViewModel.Columns.Count - 1) return;

            if (r == 0)
            {
                if (m_TotalTable.Rows.Count != 1) return;
                e.Value = m_TotalTable.Rows[0][c];
            }
            else
            {
                r--;
                if (r > m_ActiveViewModel.Count - 1) return;
                e.Value = m_ActiveViewModel[r][c];
            }
        }

        private bool m_UpdateFrozen = false;
        public bool UpdateFrozen
        {
            get { return m_UpdateFrozen; }
            set
            {
                if (m_UpdateFrozen != value)
                {
                    m_UpdateFrozen = value;
                    if (!m_UpdateFrozen)
                    {
                        // Freeze解除時にUpdateを行う。
                        UpdateViewModel();
                        for (int i = 0; i < 5; i++)      // DataViewのSortフィルタが適用されるのを待つ.
                        {
                            Application.DoEvents();
                            Thread.Sleep(100);
                            this.dataGridView1.Refresh();
                        }
                        UpdateTotal();
                    }
                }
            }
        }

        /// <summary>
        /// 行を逐次追加するとき、1行追加毎にTOTALを計算すると非効率なのでタイマによりUpdateする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.UpdateFrozen)
            {
                return;
            }
            if (!Monitor.TryEnter(m_Table, TimeSpan.FromMilliseconds(100)))
            {
                return;
            }
            try
            {
                UpdateTotal();
            }
            catch (Exception ex)
            {
                this.UpdateFrozen = true;
                ErrorReportDialog errdlg = new ErrorReportDialog("Error while executing redraw:", ex);
                errdlg.ShowDialog();
            }
            finally
            {
                Monitor.Exit(m_Table);
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Shrink/Expandのコンテクストメニュー表示
                if (e.ColumnIndex < m_FirstCountColIndex && dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.CurrentCell = dataGridView1[e.ColumnIndex, 0];
                    Point p = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, 0, true).Location;
                    this.contextMenuStrip1.Show(dataGridView1.PointToScreen(p));
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                // ソート
                int colno = e.ColumnIndex;
                SortOrder order = this.dataGridView1.Columns[colno].HeaderCell.SortGlyphDirection;
                switch (order)
                {
                    case SortOrder.None:
                        order = SortOrder.Ascending;
                        break;
                    case SortOrder.Ascending:
                        order = SortOrder.Descending;
                        break;
                    case SortOrder.Descending:
                        order = SortOrder.Ascending;
                        break;
                }
                m_ActiveViewModel = m_Table.DefaultView;
                m_ActiveViewModel.Sort = string.Format("{0} {1}", m_Table.Columns[colno].ColumnName, (order == SortOrder.Ascending) ? "ASC" : "DESC");
                // SortGlyphを更新
                for (int c = 0; c < this.dataGridView1.Columns.Count; c++)
                {
                    DataGridViewColumn col = this.dataGridView1.Columns[c];
                    if (colno == c)
                    {
                        col.HeaderCell.SortGlyphDirection = order;
                    }
                    else
                    {
                        col.HeaderCell.SortGlyphDirection = SortOrder.None;
                    }
                }
                this.dataGridView1.Refresh();
            }
        }

        /// <summary>
        /// 列の内容を縮退させる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compactRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int col = dataGridView1.CurrentCell.ColumnIndex;
            if (col < 0 || col >= m_TagNames.Count * m_Model.LexSize)
            {
                return;
            }
            LP lp;
            if (!m_ToLPMapping.TryGetValue(col % m_TagNames.Count, out lp))
            {
                return;
            }
            LexFilterList.SetFiltered(col / m_TagNames.Count, lp);
            UpdateViewModel();
            for (int i = 0; i < 5; i++)      // DataViewのSortフィルタが適用されるのを待つ.
            {
                Application.DoEvents();
                Thread.Sleep(100);
                this.dataGridView1.Refresh();
            }
        }

        /// <summary>
        /// 縮退していた列の内容を元に戻す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expandRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int col = dataGridView1.CurrentCell.ColumnIndex;
            if (col < 0 || col >= m_TagNames.Count * m_Model.LexSize)
            {
                return;
            }
            LP lp;
            if (!m_ToLPMapping.TryGetValue(col % m_TagNames.Count, out lp))
            {
                return;
            }
            LexFilterList.ResetFiltered(col / m_TagNames.Count, lp);
            UpdateViewModel();
            for (int i = 0; i < 5; i++)      // DataViewのSortフィルタが適用されるのを待つ.
            {
                Application.DoEvents();
                Thread.Sleep(100);
                this.dataGridView1.Refresh();
            }
        }

        private void editFilterSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditLexemeFilter dlg = new EditLexemeFilter(this.LexFilterList);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                UpdateViewModel();
                for (int i = 0; i < 5; i++)      // DataViewのSortフィルタが適用されるのを待つ.
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                    this.dataGridView1.Refresh();
                }
                this.dataGridView1.Refresh();
            }
        }

        private void dataGridView1_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (e.RowIndex > 0)
                {
                    dataGridView1.CurrentCell = dataGridView1[0, e.RowIndex];
                    Point p = dataGridView1.GetCellDisplayRectangle(0, e.RowIndex, true).Location;
                    this.contextMenuStrip2.Show(dataGridView1.PointToScreen(p));
                }
            }
        }

        private void listOccurrenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.OccurrenceRequested != null)
            {
                List<LexemeCondition> lcondList = new List<LexemeCondition>();
                int col = 0;
                for (int i = 0; i < m_Model.LexSize; i++)
                {
                    LexemeCondition lcond = new LexemeCondition();
                    int row = dataGridView1.CurrentCell.RowIndex;
                    for (int j = 0; j < m_TagNames.Count; j++)
                    {
                        LP lp;
                        if (!m_ToLPMapping.TryGetValue(j, out lp))
                        {
                            continue;
                        }
                        string v = (string)this.dataGridView1[col, row].Value;
                        if (v.Length > 0 && v != "*")
                        {
                            lcond.Add(Lexeme.PropertyName[lp], new Property(v));
                        }
                        col++;
                    }
                    lcondList.Add(lcond);
                }
                WordOccurrenceEventArgs ea = new WordOccurrenceEventArgs(lcondList);
                this.OccurrenceRequested(this, ea);
            }
        }

        public static string RowHeader(int r)
        {
            if (r == 0)
            {
                return "TOTAL";
            }
            else
            {
                return string.Format("{0}", r);
            }
        }

        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            Rectangle rect = new Rectangle(
              e.RowBounds.Location.X,
              e.RowBounds.Location.Y,
              this.dataGridView1.RowHeadersWidth - 4,
              e.RowBounds.Height);

            string text = RowHeader(e.RowIndex);
            TextRenderer.DrawText(
              e.Graphics,
              text,
              this.dataGridView1.RowHeadersDefaultCellStyle.Font,
              rect,
              this.dataGridView1.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private void WordListForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_Model.OnLexemeCountAdded -= new AddLexemeCountEventHandler(this.AddLexemeCountHandler);
        }

        private void dataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.contextMenuStrip3.Show(this.PointToScreen(e.Location));
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyToClipboard();
        }
    }

    internal class WordOccurrenceEventArgs : EventArgs
    {
        public WordOccurrenceEventArgs(List<LexemeCondition> lcondList)
        {
            this.AdditionalLexCondList = lcondList;
        }

        public List<LexemeCondition> AdditionalLexCondList { get; set; }
    }
}
