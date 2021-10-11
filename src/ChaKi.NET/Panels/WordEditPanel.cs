using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.GUICommon;
using ChaKi.Service.WordEdit;
using DependencyEditSLA.Widgets;

namespace ChaKi.Panels
{
    public partial class WordEditPanel : Form
    {
        private LexemeSelectionGrid m_LexSelector;
        private WordEditService m_Service;    // Service object
        public List<KwicPortion> Target;
        private Corpus m_Corpus;

        public WordEditPanel()
        {
            InitializeComponent();

            this.Target = new List<KwicPortion>();
            m_Service = new WordEditService();
        }

        public void AddTarget(KwicItem item)
        {
            if (this.Target.Count == 0)
            {
                m_Corpus = item.Crps;
            }
            else 
            {
                var lex = this.Target[0].Words[0].Lex;
                var add_lex = item.Center.Words[0].Lex;
                if (lex.Surface != add_lex.Surface)
                {
                    throw new Exception("WordEdit cannot target multiple lexemes.");
                }
                if (item.Crps != m_Corpus)
                {
                    throw new Exception("WordEdit cannot target multiple Corpora.");
                }
            }
            this.Target.Add(item.Center);
        }

        private void ChangeLexemeAssignment()
        {
            var orglex = this.Target[0].Words[0].Lex;
            m_LexSelector.BeginSelection(orglex.Surface, orglex);
            if (m_LexSelector.ShowDialog() == DialogResult.OK)
            {
                var lex = m_LexSelector.Selection?.Lexeme;
                var mwe_match = m_LexSelector.Selection?.Match;
                if (lex != null && mwe_match == null)
                {
                    var props = lex.ToPropertyArray();
                    var customprop = lex.CustomProperty;
                    if (lex.Dictionary != null) // 他のDB内のオブジェクト
                    {
                        lex = null;  // 一旦nullにしてCreateOrUpdateLexeme()で生成させる.
                    }
                    m_Service.CreateOrUpdateLexeme(ref lex, props, customprop);
                    foreach (var item in this.Target)
                    {
                        m_Service.ChangeLexeme(item.Parent.SenID, item.Parent.CenterWordID, lex);
                    }
                }
            }
        }

        private void WordEditPanel_Load(object sender, EventArgs e)
        {
            m_Service.Open(m_Corpus);
            try
            {
                m_LexSelector = new LexemeSelectionGrid(m_Corpus, m_Service);
                ChangeLexemeAssignment();
                m_Service.Commit();
            }
            catch (Exception ex)
            {
                ErrorReportDialog dlg = new ErrorReportDialog("Cannot Execute operation", ex);
                dlg.ShowDialog();
            }
            finally
            {
                m_Service.Close();
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
