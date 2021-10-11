using System.AddIn.Contract;
using System.AddIn.Pipeline;
using System.IO;

namespace ChaKi.Addins.Contracts
{
    [AddInContract]
    public interface IChaKiAddinContract : IContract
    {
        void Begin();
    }
}
