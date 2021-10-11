using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Service;
using ChaKi.Service.Search;
using ChaKi.Service.DependencyEdit;
using System.IO;

namespace DependencyEdit
{
    public partial class DepEditControl : UserControl
    {
        private int m_CenterWordPos;
        private DepEditService m_Service;
        static private DepEditControl m_instance;
        private Corpus m_Corpus;
        private float m_CurrentTextFontSize;

        public DepEditControl()
        {
            InitializeComponent();

            m_CurrentTextFontSize = WordBox.Font.Size;
            m_CenterWordPos = -1;
            m_Service = new DepEditService();
            m_instance = this;
            m_Corpus = null;
        }

        static public DepEditControl Instance
        {
            get { return m_instance; }
        }

        public Corpus Cps
        {
            get { return m_Corpus; }
        }

        /// <summary>
        /// Dependency構造の編集を開始する
        /// </summary>
        /// <param name="cps">コーパス</param>
        /// <param name="sid">文番号</param>
        /// <param name="cid">中心語の位置（強調表示に使用. 無指定[-1]でも可）</param>
        public void BeginSentenceEdit(Corpus cps, int sid, int cid)
        {
            // 現在の状態が編集中なら、まずその編集を終わらせるかどうかを確認する。
            if (m_Service.CanSave())
            {
                DialogResult res = MessageBox.Show("Save Current Changes?", "Save", MessageBoxButtons.YesNoCancel);
                if (res == DialogResult.Yes)
                {
                    m_Service.Commit();
                }
                else if (res == DialogResult.Cancel)
                {
                    return;
                }
            }
            m_Service.Close();
            m_CenterWordPos = cid;
            m_Corpus = cps;
            lock (m_Corpus)
            {
                // コーパスよりSentenceを取得する
                Sentence sen = m_Service.Open(m_Corpus, sid);
                m_SentenceStructure.SetSentence(sen, m_Service);
                m_SentenceStructure.SetCenterWord(m_CenterWordPos);
                m_SentenceStructure.VerticalScroll.Value = m_SentenceStructure.VerticalScroll.Maximum;
                m_SentenceStructure.UpdateContents();
            }
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
            if (m_instance == null) return;
            m_instance.toolStripButton1.Enabled = m_instance.m_SentenceStructure.CanUndo();
            m_instance.toolStripButton2.Enabled = m_instance.m_SentenceStructure.CanRedo();
            m_instance.toolStripButton8.Enabled = m_instance.m_SentenceStructure.CanSave();
        }

        /// <summary>
        /// Saveボタンが押されたときの処理。
        /// 編集内容をコミットし、同じ文の編集を続ける。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            m_Service.Commit();
            Sentence sen = m_Service.ReOpen();
            m_SentenceStructure.SetSentence(sen, m_Service);
            m_SentenceStructure.SetCenterWord(m_CenterWordPos);
            m_SentenceStructure.UpdateContents();
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

        /// <summary>
        /// １段階拡大して表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            m_CurrentTextFontSize = Math.Min( 20.0F, m_CurrentTextFontSize + 1.0F );
            WordBox.Font = new Font("Lucida Sans Unicode", m_CurrentTextFontSize, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            m_SentenceStructure.UpdateContents();
        }

        /// <summary>
        /// １段階縮小して表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            m_CurrentTextFontSize = Math.Max(5.0F, m_CurrentTextFontSize - 1.0F);
            WordBox.Font = new Font("Lucida Sans Unicode", m_CurrentTextFontSize, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            m_SentenceStructure.UpdateContents();
        }
    }
}

