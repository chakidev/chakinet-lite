using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ChaKi.Common;
using ChaKi.Common.Settings;
using ChaKi.Entity.Corpora;
using ChaKi.GUICommon;
using ChaKi.Service.Lexicons;
using ChaKi.Properties;

namespace ChaKi.Panels
{
    public class WordAttributeListPanel : AttributeListPanel
    {
        private List<string> m_Keys;
        private Dictionary<int, LP> m_LPMapping;
        private ILexemeEditService m_Service;

        public WordAttributeListPanel()
            : base()
        {
            m_LPMapping = new Dictionary<int, LP>();
            m_Keys = new List<string>();
            m_Title = "LexemeEdit";
            m_Service = new LexemeEditService();

            UpdateGridSetting();

            // Grid状態の復元
            var gs = GUISetting.Instance.WordAttributePanelGridSettings;
            if (gs == null)
            {
                gs = GUISetting.Instance.WordAttributePanelGridSettings = new GridSettings();
            }
            this.AttributeGrid.Settings = gs;

            this.toolStripButton4.Visible = true;
            PropertyBoxSettings.Instance.SettingChanged += new EventHandler(Instance_SettingChanged);
        }

        protected override void HandleToggleEditMode(object sender, System.EventArgs e)
        {
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
        }

        void Instance_SettingChanged(object sender, EventArgs e)
        {
            UpdateGridSetting();
        }

        private void UpdateGridSetting()
        {
            m_LPMapping.Clear();
            m_Keys.Clear();
            // デフォルトのリスト項目（Corpusが決定されれば、その設定に従って再設定する）
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
                m_LPMapping.Add(row, lp.Value);
                m_Keys.Add(setting.DisplayName);
                row++;
            }
        }

        public override void SetSource(Corpus corpus, object source)
        {
            Lexeme lex = source as Lexeme;
            if (lex == null || m_Source == lex)
            {
                return;
            }
            if (this.AttributeGrid.IsEditing)
            {
                return;
            }
            m_Source = lex;
            m_Corpus = corpus;

            AttributeGridData model = new AttributeGridData();
            var rows = model.Rows;

            for (int i = 0; i < m_Keys.Count; i++)
            {
                string value = lex.GetStringProperty(m_LPMapping[i]);
                if (value == null)
                {
                    value = string.Empty;
                }
                var readOnly = (m_Keys[i] == Lexeme.PropertyName[LP.Surface] || m_Keys[i] == Lexeme.PropertyName[LP.BaseLexeme]);
                rows.Add(new AttributeData(m_Keys[i], value, readOnly? AttributeGridRowType.ReadOnly:AttributeGridRowType.ValueWritable));
            }
            // カスタム属性は常に最終行に表示
            string custom_val = lex.CustomProperty;
            if (custom_val == null)
            {
                custom_val = string.Empty;
            }
            rows.Add(new AttributeData("Custom", custom_val, AttributeGridRowType.ValueWritable));

            this.AttributeGrid.Model = model;
            this.AttributeGrid.IsEditing = false;
        }

        protected override void SetEditMode(bool f)
        {
            if (f)
            {
                try
                {
                    m_Service.Open(m_Corpus, m_Source as Lexeme, UnlockRequestCallback);
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Failed to open LexemeEditService:", ex);
                    dlg.ShowDialog();
                    return;
                }
                SetLexiconTags();
                this.AttributeGrid.SetWordAttributeMode();
                this.AttributeGrid.StartEditing();
            }
            else
            {
                try
                {
                    m_Service.Close();
                }
                catch (Exception ex)
                {
                    ErrorReportDialog dlg = new ErrorReportDialog("Failed to close LexemeEditService:", ex);
                    dlg.ShowDialog();
                }
                this.AttributeGrid.EndEditing();
            }
        }

        private bool UnlockRequestCallback(Type requestingService)
        {
            if (requestingService == typeof(ILexemeEditService))
            {
                // 自分自身からのUnlockリクエストは無視する.
                return true;
            }
            if (this.AttributeGrid.IsEditing)
            {
                if (this.AttributeGrid.CanUndo)
                {
                    return false;
                }
                SetEditMode(false);
            }
            return true;
        }

        private void SetLexiconTags()
        {
            Dictionary<string, IList<PartOfSpeech>> pos;
            Dictionary<string, IList<CType>> ctypes;
            Dictionary<string, IList<CForm>> cforms;

            m_Service.GetLexiconTags(out pos, out ctypes, out cforms);
            this.AttributeGrid.SetLexiconTags(pos, ctypes, cforms);
        }

        protected override void Save()
        {
            var viewModel = this.AttributeGrid.Model;

            // ( GroupName --> ( Key --> Value )* )*
            var updateDataTable = new Dictionary<string, string>();

            foreach (AttributeGridRowData rd in viewModel.Rows)
            {
                AttributeData ad = rd as AttributeData;
                if (ad == null) continue;
                if (ad.RowType == AttributeGridRowType.ReadOnly)
                {
                    continue;
                }
                if (updateDataTable.ContainsKey(ad.Key))
                {
                    throw new InvalidOperationException(string.Format("Duplicated Key found: {0}", ad.Key));
                }
                updateDataTable[ad.Key] = ad.Value;
            }

            m_Service.Save(updateDataTable);
        }

        protected override void Commit()
        {
            // Do Nothing （LexemeEditService.SaveがCommitを含むため）
        }
    }
}
