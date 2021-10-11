using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Settings;
using RDotNet;

namespace ChaKi.ToolDialogs
{
    public partial class ExportGridToExcelDialog : Form
    {
        private ExportSetting m_Model;

        public ExportGridToExcelDialog()
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
            this.radioButton3.Checked = (m_Model.ExportType == ExportType.R);
            this.radioButton4.Checked = (m_Model.ExportType == ExportType.Rda);
        }

        private void UpdateModel()
        {
            if (this.radioButton1.Checked) m_Model.ExportType = ExportType.Excel;
            if (this.radioButton2.Checked) m_Model.ExportType = ExportType.CSV;
            if (this.radioButton3.Checked) m_Model.ExportType = ExportType.R;
            if (this.radioButton4.Checked) m_Model.ExportType = ExportType.Rda;
        }

        private void ExportGridToExcelDialog_Load(object sender, EventArgs e)
        {
            // Excelが存在するかチェック
            if (!ExportToExcelDialog.CheckExcelExistence())
            {
                this.radioButton1.Enabled = false;
                if (m_Model.ExportType == ExportType.Excel)
                {
                    m_Model.ExportType = ExportType.CSV;
                }
            }
            // Rが存在するかチェック
            if (!CheckRExistence())
            {
                this.radioButton4.Enabled = false;
                if (m_Model.ExportType == ExportType.Rda)
                {
                    m_Model.ExportType = ExportType.R;
                }
            }
            UpdateView();
        }

        private bool CheckRExistence()
        {
            try {
                using (var engine = REngine.GetInstance())
                {
                    engine.SetSymbol("greetings", engine.CreateCharacterVector(new[] { "Hello!" }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }
    }
}
