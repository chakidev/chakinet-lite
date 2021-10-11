using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Settings;

namespace ChaKi.Common.Widgets
{
    public partial class PropertyListSelectionDialog : Form
    {
        public PropertyListSelectionDialog()
        {
            this.Selection = "";
            InitializeComponent();
        }

        public string Selection { get; private set; }

        public event EventHandler OnSelectionChanged;

        /// <summary>
        /// ListBoxにヒストリを表示する。
        /// ユーザ設定で保存されているヒストリからkey(Surface,Reading,etc.)に
        /// 対応するものを得る。
        /// </summary>
        /// <param name="key">対応するヒストリのプロパティ</param>
        public void PopulateWithUserHistory(string key)
        {
            this.listBox1.Items.Clear();
            List<string> history = UserSettings.GetInstance().LastKeywords;
            foreach (string s in history)
            {
                if (s.StartsWith(key))
                {
                    string[] fields = s.Split(new char[] { '=' });
                    if (fields.Length > 1)
                    {
                        this.listBox1.Items.Add(fields[1]);
                    }
                }
            }
            this.listBox1.SelectedIndex = -1;
            this.Selection = "";
        }

        /// <summary>
        /// スクロール位置を先頭にセットする
        /// </summary>
        public void ResetScrollState()
        {
            this.VerticalScroll.Value = 0;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Selection = (string)this.listBox1.SelectedItem;
            if (this.Selection == null)
            {
                this.Selection = "";
            }

            // イベント通知
            if (OnSelectionChanged != null)
            {
                OnSelectionChanged(this, null);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.listBox1.Items.Clear();
            UserSettings.GetInstance().ClearKeywordHistory();
        }
    }
}
