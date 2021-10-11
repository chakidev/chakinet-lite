using ChaKi.Common;
using ChaKi.Entity.Corpora;
using System.Threading.Tasks;

namespace ChaKi.Service.Import
{
    public interface IImportService
    {
        Task ImportAnnotationAsync(Corpus corpus, string file, int projid, IProgress progress);
    }
}
