using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Settings;
using System.Runtime.InteropServices;

namespace ChaKi.ToolDialogs
{
    public partial class ExportToExcelDialog : Form
    {
        [DllImport("ole32.dll")]
        static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

        private ExportSetting m_Model;

        public ExportToExcelDialog()
        {
            InitializeComponent();
        }

        public void SetModel(ExportSetting model)
        {
            m_Model = new ExportSetting(model);
        }

        public ExportSetting GetModel()
        {
            UpdateModel();
            return new ExportSetting(m_Model);
        }

        private void UpdateView()
        {
            this.radioButton1.Checked = (m_Model.ExportType == ExportType.Excel);
            this.radioButton2.Checked = (m_Model.ExportType == ExportType.CSV);
            this.radioButton3.Checked = (m_Model.ExportFormat == ExportFormat.Sentence);
            this.radioButton4.Checked = (m_Model.ExportFormat == ExportFormat.Portion);
            this.radioButton5.Checked = (m_Model.ExportFormat == ExportFormat.Word);
            this.checkBox1.Checked = m_Model.UseSpacing;
            this.checkBox2.Checked = (m_Model.WordExportFormat == WordToStringConversionFormat.MixPOS);
        }

        private void UpdateModel()
        {
            if (this.radioButton1.Checked) m_Model.ExportType = ExportType.Excel;
            if (this.radioButton2.Checked) m_Model.ExportType = ExportType.CSV;
            if (this.radioButton3.Checked) m_Model.ExportFormat = ExportFormat.Sentence;
            if (this.radioButton4.Checked) m_Model.ExportFormat = ExportFormat.Portion;
            if (this.radioButton5.Checked) m_Model.ExportFormat = ExportFormat.Word;
            m_Model.UseSpacing = this.checkBox1.Checked;
            m_Model.WordExportFormat = this.checkBox2.Checked ?
                WordToStringConversionFormat.MixPOS : WordToStringConversionFormat.Short;
        }

        private void UpdateControlStates()
        {
            // Word mode以外でのみ、word spacingを選択可とする.
            this.checkBox1.Enabled = !(this.radioButton5.Checked);

            // Word modeまたはSpacing有効時のみ、Word/POS-mixを選択可とする.
            this.checkBox2.Enabled = (this.radioButton5.Checked || this.checkBox1.Checked);
        }

        public static bool CheckExcelExistence()
        {
            // Excelが存在するかチェック
            Guid clsid;
            if (CLSIDFromProgID("Excel.Application", out clsid) != 0) // S_OK(0)でないならエラー
            {
                return false;
            }
            return true;
        }

        private void ExportToExcelDialog_Load(object sender, EventArgs e)
        {
         	// Excelが存在するかチェック
            if (!CheckExcelExistence())
            {
                this.radioButton1.Enabled = false;
                m_Model.ExportType = ExportType.CSV;
            }
            else
            {
                m_Model.ExportType = ExportType.Excel;
            }

            UpdateView();
            UpdateControlStates();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStates();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStates();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStates();
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStates();
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStates();
        }
    }
}
