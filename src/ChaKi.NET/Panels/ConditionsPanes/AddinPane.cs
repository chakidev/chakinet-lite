using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using ChaKi.Entity.Search;
using System.AddIn.Hosting;
using System.Collections.ObjectModel;
using ChaKi.Addins.HostSide;
using ChaKi.Common.Widgets;
using System.IO;
using ChaKi.GUICommon;

namespace ChaKi.Panels.ConditionsPanes
{
    public partial class AddinPane : UserControl
    {
        private MiningCondition m_Model;

        public AddinPane(MiningCondition cond)
        {
            m_Model = cond;

            InitializeComponent();

            FindAddins();
        }

        public void FindAddins()
        {
            try
            {
                string addinRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\ChaKi.NET\Addins";
                AddInStore.Rebuild(addinRoot);
                Collection<AddInToken> addins = AddInStore.FindAddIns(typeof(ChaKiAddin), addinRoot);
                foreach (AddInToken tk in addins)
                {
                    var lvi = new ListViewItem(
                        new string[] { tk.Name, tk.Description, tk.AddInFullName, tk.Publisher, tk.AssemblyName.FullName },
                        0);
                    lvi.Tag = tk;
                    lvi.ToolTipText = string.Format("{0}\n{1}\n{2}\n{3}", tk.AddInFullName, tk.Description, tk.Publisher, tk.AssemblyName.FullName);
                    this.listView1.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                ErrorReportDialog errdlg = new ErrorReportDialog("Error while searching for addins: ", ex);
                errdlg.ShowDialog();
            }
        }

        public void SetCondition(MiningCondition cond)
        {
            m_Model = cond;
        }

        public MiningCondition GetCondition()
        {
            return m_Model;
        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 0)
            {
                return;
            }
            var item = this.listView1.SelectedItems[0];
            var tk = (AddInToken)(item.Tag);
            var addinInstance = tk.Activate<ChaKiAddin>(AddInSecurityLevel.FullTrust);

            //var dlg = new AddinDialog(addinInstance.Begin);
            //dlg.Text = string.Format("AddIn ({0})", tk.AddInFullName);
            //dlg.ShowDialog();

            addinInstance.Begin();
        }
    }
}
