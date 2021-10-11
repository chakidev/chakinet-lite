using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Search;
using PopupControl;
using ChaKi.Common.Widgets;
using ChaKi.Service.Database;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class FilterControl : UserControl
    {
        private Popup popupForDocAttrKeys;
        private Popup popupForDocAttrValues;
        private ListSelector selectorDocAttrKeys;
        private ListSelector selectorDocAttrValues;

        public SentenceSearchCondition CorpusCond { get; set; }  // Corpus Tab Model への参照

        public FilterControl()
        {
            InitializeComponent();

            this.selectorDocAttrKeys = new ListSelector();
            this.popupForDocAttrKeys = new Popup(this.selectorDocAttrKeys) { Resizable = true };
            this.selectorDocAttrValues = new ListSelector();
            this.popupForDocAttrValues = new Popup(this.selectorDocAttrValues) { Resizable = true };
            this.selectorDocAttrKeys.TagSelected += new EventHandler(selectorForKeys_TagSelected);
            this.selectorDocAttrValues.TagSelected += new EventHandler(selectorForValues_TagSelected);
        }

        public void UpdateView(FilterCondition model)
        {
            this.textBox1.Text = model.DocumentFilterKey;
            this.textBox4.Text = model.DocumentFilterValue;
            this.radioButton1.Checked = (model.ResultsetFilter.FetchType == FetchType.Incremental);
            this.radioButton2.Checked = (model.ResultsetFilter.FetchType == FetchType.Decremental);
            this.radioButton3.Checked = (model.ResultsetFilter.FetchType == FetchType.Random);
            int val = model.ResultsetFilter.Max;
            this.textBox2.Text = (val >= 0) ? val.ToString() : string.Empty;
            val = model.ResultsetFilter.StartAt;
            this.textBox3.Text = (val >= 0) ? val.ToString() : string.Empty;
            this.checkBox1.Checked = model.ResultsetFilter.IsAutoIncrement;
        }

        public void Synchronize(FilterCondition model)
        {
            model.DocumentFilterKey = this.textBox1.Text;
            model.DocumentFilterValue = this.textBox4.Text;

            FetchType ftype = FetchType.Incremental;
            if (this.radioButton2.Checked) ftype = FetchType.Decremental;
            if (this.radioButton3.Checked) ftype = FetchType.Random;
            int max = -1;
            string s = this.textBox2.Text.Trim();
            if (s.Length > 0) Int32.TryParse(s, out max);
            int from = 0;
            s = this.textBox3.Text.Trim();
            if (s.Length > 0) Int32.TryParse(s, out from);
            bool isAutoIncrement = this.checkBox1.Checked;

            model.ResultsetFilter = new ResultsetFilter()
            {
                FetchType = ftype,
                StartAt = from,
                Max = max,
                IsAutoIncrement = isAutoIncrement
            };
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnableState();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnableState();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnableState();
        }

        private void UpdateEnableState()
        {
            checkBox1.Enabled = (radioButton1.Checked);
            label2.Enabled = !(radioButton3.Checked);
            textBox3.Enabled = !(radioButton3.Checked);
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            // DBからdocumenttagのリストを得る.
            var oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                this.selectorDocAttrKeys.Reset();
                foreach (var c in this.CorpusCond.Corpora)
                {
                    this.selectorDocAttrKeys.AddTab(c.Name);
                    var svc = DBService.Create(c.DBParam);
                    var list = svc.LoadDocumentAttributeKeys();
                    foreach (var s in list)
                    {
                        this.selectorDocAttrKeys.AddTag(c.Name, s);
                    }
                }
            }
            catch (Exception ex)
            {
                // 無視
                Console.WriteLine(ex);
            }
            finally
            {
                this.Cursor = oldCur;
            }
            this.popupForDocAttrKeys.Show(sender as Control);
        }

        private void textBox4_Click(object sender, EventArgs e)
        {
            var key = this.textBox1.Text;
            if (key.Length == 0)
            {
                return;
            }
            // DBからdocumenttagのリストを得る.
            var oldCur = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                this.selectorDocAttrValues.Reset();
                foreach (var c in this.CorpusCond.Corpora)
                {
                    var tabname = string.Format("{0}:{1}", c.Name, key);
                    this.selectorDocAttrValues.AddTab(tabname);
                    var svc = DBService.Create(c.DBParam);
                    var list = svc.LoadDocumentAttributeValues(key);
                    foreach (var s in list)
                    {
                        this.selectorDocAttrValues.AddTag(tabname, s);
                    }
                }
            }
            catch (Exception ex)
            {
                // 無視
                Console.WriteLine(ex);
            }
            finally
            {
                this.Cursor = oldCur;
            }
            this.popupForDocAttrValues.Show(sender as Control);
        }

        void selectorForKeys_TagSelected(object sender, EventArgs e)
        {
            if (!(sender is ListSelector)) return;
            var selector = (ListSelector)sender;
            this.textBox1.Text = selector.Selection;
        }

        void selectorForValues_TagSelected(object sender, EventArgs e)
        {
            if (!(sender is ListSelector)) return;
            var selector = (ListSelector)sender;
            this.textBox4.Text = selector.Selection;
        }
    }
}
