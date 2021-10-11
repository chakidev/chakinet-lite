using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Corpora;
using ChaKi.Common.Settings;

namespace DependencyEditSLA
{
    public partial class LexemeListCheckDialog : Form
    {
        public List<LexemeCorpusBoolLongTuple> Model { get; set; }

        public static LexemeSelectionSettings Settings;

        static LexemeListCheckDialog()
        {
            Settings = DepEditSettings.Current.LexemeListCheckDialogSettings;
        }

        public LexemeListCheckDialog()
        {
            InitializeComponent();
        }

        // 全ての使用可能なPOS, CType, CFormタグのリストを得て、PropTreeにセットする.
        public void SetLexiconTags(IDictionary<string, IList<PartOfSpeech>> pos, IDictionary<string, IList<CType>> ctypes, IDictionary<string, IList<CForm>> cforms)
        {
            this.lexemeList1.SetLexiconTags(pos, ctypes, cforms);
        }


        private void LexemeListCheckDialog_Load(object sender, EventArgs e)
        {
            // 初期値設定を反映
            Rectangle maxRect = Screen.GetWorkingArea(this);
            this.Location = new Point(
                Math.Min(maxRect.Width, Math.Max(Settings.InitialLocation.Location.X, 0)),
                Math.Min(maxRect.Height, Math.Max(Settings.InitialLocation.Location.Y, 0)));

            this.Size = new Size(
                Math.Min(maxRect.Width, Math.Max(Settings.InitialLocation.Size.Width, 100)),
                Math.Min(maxRect.Height, Math.Max(Settings.InitialLocation.Size.Height, 100)));

            this.lexemeList1.Focus();

            this.lexemeList1.Model = this.Model;
        }

        private void LexemeListCheckDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.lexemeList1.EndEdit();

            // 初期値設定をセーブ
            Settings.InitialLocation = new Rectangle(this.Location, this.Size);
            this.lexemeList1.SaveSettings();
        }

        // "Check All" button
        private void button3_Click(object sender, EventArgs e)
        {
            this.lexemeList1.CheckAll();
        }

        // "Clear All" button
        private void button4_Click(object sender, EventArgs e)
        {
            this.lexemeList1.ClearAll();
        }
    }
}
