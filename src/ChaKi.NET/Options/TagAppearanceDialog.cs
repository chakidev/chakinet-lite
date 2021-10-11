using System;
using System.Windows.Forms;
using ChaKi.Entity.Settings;
using System.Data;
using ChaKi.GUICommon;
using System.Collections.Generic;
using System.Drawing;
using ChaKi.Common;
using System.Globalization;

namespace ChaKi.Options
{
    public partial class TagAppearanceDialog : Form
    {
        public event EventHandler RefreshRequested;

        private TagSetting m_Original;

        public TagAppearanceDialog()
        {
            m_Original = new TagSetting(TagSetting.Instance);
            InitializeComponent();

            DataGridView dg = this.dataGridView1;
            dg.Columns.Clear();
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "TagName" });
            dg.Columns.Add(new ColorPickerColumn() { Name = "DrawColor" });
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Alpha" });
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DrawWidth" });
            dg.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "ShowInSelectorMenu" });
            dg.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "VisibleInKwicView" });
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ShortcutKey" });

            dg = this.dataGridView2;
            dg.Columns.Clear();
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "TagName" });
            dg.Columns.Add(new ColorPickerColumn() { Name = "DrawColor" });
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Alpha" });
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DrawWidth" });
            dg.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "ShowInSelectorMenu" });
            dg.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "VisibleInKwicView" });
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ShortcutKey" });

            dg = this.dataGridView3;
            dg.Columns.Clear();
            dg.Columns.Add(new DataGridViewTextBoxColumn() { Name = "TagName" });
            dg.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "ShowInSelectorMenu" });
            dg.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "VisibleInKwicView" });

            UpdateView();

            this.tabControl1.SelectedIndex = 0;
        }

        // Model -> View
        private void UpdateView()
        {
            try
            {
                Dictionary<string, TagSettingItem> table;
                DataGridView dg;
                table = TagSetting.Instance.Segment;
                dg = this.dataGridView1;
                dg.Rows.Clear();
                foreach (KeyValuePair<string, TagSettingItem> pair in table)
                {
                    Color c = ColorTranslator.FromHtml(pair.Value.Color);
                    //                    Color c = Color.FromArgb((int)(pair.Value.Color));
                    dg.Rows.Add(pair.Key, c, pair.Value.Alpha, pair.Value.Width, pair.Value.ShowInSelectorMenu, pair.Value.VisibleInKwicView, pair.Value.ShortcutKey);
                }

                table = TagSetting.Instance.Link;
                dg = this.dataGridView2;
                dg.Rows.Clear();
                foreach (KeyValuePair<string, TagSettingItem> pair in table)
                {
                    Color c = ColorTranslator.FromHtml(pair.Value.Color);
                    dg.Rows.Add(pair.Key, c, pair.Value.Alpha, pair.Value.Width, pair.Value.ShowInSelectorMenu, pair.Value.VisibleInKwicView, pair.Value.ShortcutKey);
                }

                table = TagSetting.Instance.Group;
                dg = this.dataGridView3;
                dg.Rows.Clear();
                foreach (KeyValuePair<string, TagSettingItem> pair in table)
                {
                    dg.Rows.Add(pair.Key, pair.Value.ShowInSelectorMenu, pair.Value.VisibleInKwicView);
                }
                CheckShortcuts();
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error: ", ex);
                edlg.ShowDialog();
            }
        }

        // View -> Model
        private bool Synchronize()
        {
            TagSetting rollback = new TagSetting(TagSetting.Instance);
            try
            {
                Dictionary<string, TagSettingItem> table = TagSetting.Instance.Segment;
                DataGridView dg = this.dataGridView1;

                table.Clear();
                foreach (DataGridViewRow row in dg.Rows)
                {
                    object val = row.Cells[0].Value;
                    if (val == null) continue;
                    string name = (string)val;
                    if (name.Length == 0) continue;
                    TagSettingItem item = new TagSettingItem();
                    val = row.Cells[1].Value;
                    if (val != null)
                    {
                        Color c = (Color)val;
                        item.Color = ColorTranslator.ToHtml(c);
                    }
                    val = row.Cells[2].Value;
                    if (val != null)
                    {
                        int alpha = int.Parse(val.ToString());
                        alpha = Math.Max(0, Math.Min(255, alpha));
                        item.Alpha = (byte)alpha;
                    }
                    val = row.Cells[3].Value;
                    if (val != null)
                    {
                        item.Width = float.Parse(val.ToString());
                    }
                    val = row.Cells[4].Value;
                    if (val != null)
                    {
                        item.ShowInSelectorMenu = (bool)val;
                    }
                    val = row.Cells[5].Value;
                    if (val != null)
                    {
                        item.VisibleInKwicView = (bool)val;
                    }
                    item.ShortcutKey = CellValueToChar(row.Cells[6]);
                    table.Add(name, item);
                }

                table = TagSetting.Instance.Link;
                dg = this.dataGridView2;
                table.Clear();
                foreach (DataGridViewRow row in dg.Rows)
                {
                    object val = row.Cells[0].Value;
                    if (val == null) continue;
                    string name = (string)val;
                    if (name.Length == 0) continue;
                    TagSettingItem item = new TagSettingItem();
                    val = row.Cells[1].Value;
                    if (val != null)
                    {
                        Color c = (Color)val;
                        item.Color = ColorTranslator.ToHtml(c);
                    }
                    val = row.Cells[2].Value;
                    if (val != null)
                    {
                        int alpha = int.Parse(val.ToString());
                        alpha = Math.Max(0, Math.Min(255, alpha));
                        item.Alpha = (byte)alpha;
                    }
                    val = row.Cells[3].Value;
                    if (val != null)
                    {
                        item.Width = float.Parse(val.ToString());
                    }
                    val = row.Cells[4].Value;
                    if (val != null)
                    {
                        item.ShowInSelectorMenu = (bool)val;
                    }
                    val = row.Cells[5].Value;
                    if (val != null)
                    {
                        item.VisibleInKwicView = (bool)val;
                    }
                    item.ShortcutKey = CellValueToChar(row.Cells[6]);
                    table.Add(name, item);
                }

                table = TagSetting.Instance.Group;
                dg = this.dataGridView3;
                table.Clear();
                foreach (DataGridViewRow row in dg.Rows)
                {
                    object val = row.Cells[0].Value;
                    if (val == null) continue;
                    string name = (string)val;
                    if (name.Length == 0) continue;
                    TagSettingItem item = new TagSettingItem();
                    val = row.Cells[1].Value;
                    if (val != null)
                    {
                        item.ShowInSelectorMenu = (bool)val;
                    }
                    val = row.Cells[2].Value;
                    if (val != null)
                    {
                        item.VisibleInKwicView = (bool)val;
                    }
                    table.Add(name, item);
                }
                CheckShortcuts();
            }
            catch (Exception ex)
            {
                ErrorReportDialog edlg = new ErrorReportDialog("Error: ", ex);
                edlg.ShowDialog();
                TagSetting.Instance.CopyFrom(rollback);
                return false;
            }
            return true;
        }

        // ShortcutのCell値を'a'~'z'に変換し、cellを書き換える（変換できないものは'\0'になる)
        private char CellValueToChar(DataGridViewCell cell)
        {
            var ch = '\0';
            var val = cell.Value;
            if (val is string)
            {
                var s = (string)val;
                ch = string.IsNullOrEmpty(s) ? '\0' : s[0];
            }
            else if (val is char)
            {
                ch = (char)val;
            }
            ch = char.ToLower(ch);
            if (!(ch >= 'a' && ch <= 'z'))
            {
                ch = '\0';
            }
            cell.Value = ch;
            return ch;
        }

        // Segment, LinkのShortcutの重複を検知し、エラーアイコン・テキストを付加する.
        private void CheckShortcuts()
        {
            var chars = new HashSet<char>();
            foreach (DataGridViewRow row in this.dataGridView1.Rows)
            {
                var ch = CellValueToChar(row.Cells[6]);
                if (ch != '\0' && chars.Contains(ch))
                {
                    row.Cells[6].ErrorText = "Duplicated";
                }
                else
                {
                    row.Cells[6].ErrorText = null;
                    chars.Add(ch);
                }
            }
            chars.Clear();
            foreach (DataGridViewRow row in this.dataGridView2.Rows)
            {
                var ch = CellValueToChar(row.Cells[6]);
                if (ch != '\0' && chars.Contains(ch))
                {
                    row.Cells[6].ErrorText = "Duplicated";
                }
                else
                {
                    row.Cells[6].ErrorText = null;
                    chars.Add(ch);
                }
            }
        }

        // Apply
        private void button1_Click(object sender, EventArgs e)
        {
            Synchronize();
            if (RefreshRequested != null)
            {
                RefreshRequested(this, null);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Synchronize();
            if (RefreshRequested != null)
            {
                RefreshRequested(this, null);
            }
        }

        private void TagAppearanceDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.DialogResult != DialogResult.OK)
            {
                TagSetting.Instance = new TagSetting(m_Original);
            }
            if (RefreshRequested != null)
            {
                RefreshRequested(this, null);
            }
        }
    }
}
