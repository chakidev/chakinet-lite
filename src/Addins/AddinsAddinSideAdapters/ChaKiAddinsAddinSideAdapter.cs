using System.AddIn.Pipeline;
using System.IO;
using ChaKi.Addins.AddinViews;
using ChaKi.Addins.Contracts;
using System.Windows.Forms;

namespace ChaKi.Addins.HostSide
{
    [AddInAdapter]
    public class ChaKiAddinsAddinSideAdapter : ContractBase, IChaKiAddinContract
    {
        private ChaKiAddinView m_View;

        public ChaKiAddinsAddinSideAdapter(ChaKiAddinView view)
        {
            m_View = view;
        }

        public void Begin()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            m_View.Begin();
        }
    }
}
