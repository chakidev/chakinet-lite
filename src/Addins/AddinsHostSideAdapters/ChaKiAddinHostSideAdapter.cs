using System.AddIn.Pipeline;
using System.IO;
using ChaKi.Addins.Contracts;

namespace ChaKi.Addins.HostSide
{
    [HostAdapter]
    public class ChaKiAddinHostSideAdapter : ChaKiAddin
    {
        private IChaKiAddinContract m_Contract;
        private ContractHandle m_Handle;

        public ChaKiAddinHostSideAdapter(IChaKiAddinContract contract)
        {
            m_Contract = contract;
            m_Handle = new ContractHandle(contract);
        }

        public override void Begin()
        {
            m_Contract.Begin();
        }
    }
}
