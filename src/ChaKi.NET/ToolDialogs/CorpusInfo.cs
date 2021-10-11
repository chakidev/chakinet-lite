using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ChaKi.VirtualGrid;
using ChaKi.Entity.Corpora;
using System.Collections;
using ChaKi.GUICommon;
using ChaKi.Service.Database;
using ChaKi.Entity.Kwic;
using Crownwood.DotNetMagic.Controls;
using TabControl = Crownwood.DotNetMagic.Controls.TabControl;
using TabPage = Crownwood.DotNetMagic.Controls.TabPage;

namespace ChaKi.ToolDialogs
{
    public partial class CorpusInfo : Form
    {
        private Corpus m_Corpus;
        //private Cache m_MemoryCache;
        private bool m_Closed;

        public CorpusInfo()
        {
            InitializeComponent();
            m_Corpus = null;
            m_Closed = false;

            this.tabControl1.SelectedTab = this.tabPage1;
            this.tabControl1.SelectionChanged += new SelectTabHandler(this.tabControl1_SelectionChanged);
        }

        public void LoadCorpusInfo(Corpus c)
        {
            // コーパスの基本情報をロードする
            m_Corpus = c;
            Application.DoEvents();
            Cursor oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            DBService dbs = null;
            // Lexicon以外の情報→同期式
            try
            {
                dbs = DBService.Create(c.DBParam);
                dbs.LoadCorpusInfo(c);
            }
            catch (Exception ex)
            {
                ExceptionDialogBox dlg = new ExceptionDialogBox();
                dlg.Text = ex.ToString();
                dlg.ShowDialog();
                return;
            }
            Application.DoEvents();

            ShowSummary();
            if (dbs != null)
            {
                ShowDocuments(dbs);
                ShowTags(dbs);
            }
            this.Cursor = oldCur;
        }

        //  Lexiconタブに最初に移った時にLexiconのロードを開始
        private void tabControl1_SelectionChanged(TabControl sender, TabPage oldPage, TabPage newPage)
        {
            if (newPage.TabIndex == 2 && !this.tabControl2.Visible)
            {
                this.tabControl2.Visible = true;
                Application.DoEvents();
                StartLoadLexicon();
            }
        }
        
        private void StartLoadLexicon()
        {
            // Lexicon情報→非同期
            this.backgroundWorker1.RunWorkerAsync();
        }

        private void ShowSummary()
        {
            IList<CorpusProperty> cprops = m_Corpus.GetCorpusProperties();
            this.dataGridView1.DataSource = cprops;
            this.dataGridView1.Columns[0].Width = 100;
            this.dataGridView1.Columns[1].Width = 300;
        }

        private void ShowDocuments(DBService dbs)
        {
            this.dataGridView6.DataSource = dbs.LoadDocumentInfo();
        }

        private void ShowLexicon()
        {
            Lexicon lexicon = m_Corpus.Lex;
            LexemeListFilter filterList = new LexemeListFilter(1);
            filterList.Reset();
            LexemeCountList lexlist = new LexemeCountList(filterList);
            foreach (Lexeme lex in lexicon)
            {
                LexemeList llist = new LexemeList();
                llist.Add(lex);
                lexlist.Add(llist, m_Corpus, lex.Frequency);
            }
            this.wordListView1.SetModel(lexlist, new List<Corpus>() { m_Corpus }, 1, -1);
        }

        private void ShowPOS()
        {
            this.propertyTree1.PopulateWithPOSSelections(new List<Corpus> { m_Corpus }, m_Corpus);
        }
        private void ShowCForms()
        {
            this.propertyTree2.PopulateWithCFormSelections(new List<Corpus> { m_Corpus }, m_Corpus);
        }
        private void ShowCTypes()
        {
            this.propertyTree3.PopulateWithCTypeSelections(new List<Corpus> { m_Corpus }, m_Corpus);
        }
        private void ShowTags(DBService svc)
        {
            try
            {
                DataTable segs;
                DataTable links;
                DataTable groups;
                svc.LoadTags(out segs, out links, out groups);
                this.dataGridView3.DataSource = segs;
                this.dataGridView4.DataSource = links;
                this.dataGridView5.DataSource = groups;
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error: ", ex);
            }
        }

        delegate void ShowLexiconDele();

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                DBService dbs = DBService.Create(m_Corpus.DBParam);
                dbs.LoadLexicon(m_Corpus);
            }
            catch (Exception ex)
            {
                ExceptionDialogBox dlg = new ExceptionDialogBox();
                dlg.Text = ex.ToString();
                dlg.ShowDialog();
                return;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (m_Closed)
            {
                return;
            }
            this.Cursor = Cursors.WaitCursor;
            ShowLexicon();
            ShowPOS();
            ShowCForms();
            ShowCTypes();
            this.Cursor = Cursors.Arrow;
            this.label1.Visible = false;
        }

        private void CorpusInfo_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_Closed = true;
        }
    }
}
