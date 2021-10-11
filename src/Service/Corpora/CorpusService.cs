using System;
using ChaKi.Entity.Corpora;
using ChaKi.Service.Database;
using NHibernate;

namespace ChaKi.Service.Corpora
{
    /// <summary>
    /// .dbファイルまたは.defファイルからCorpusを作成し、
    /// コーパスの種類に応じた適切なDBServiceを生成する.
    /// コーパス操作のためのファサードとなるサービス.
    /// </summary>
    public class CorpusService
    {
        public Corpus Corpus { get; private set; }
        public DBService DBService { get; private set; }

        public static CorpusService Create(string file)
        {
            CorpusService csvc = new CorpusService();
            csvc.Corpus = Corpus.CreateFromFile(file);
            csvc.DBService = DBService.Create(csvc.Corpus.DBParam);

            return csvc;
        }

        private CorpusService() {}

        public ISession OpenSession()
        {
            return this.DBService.OpenSession();
        }

        public void LoadSchemaVersion()
        {
            this.DBService.LoadSchemaVersion(this.Corpus);   
        }

        public bool ConvertSchema(Action<string> callback)
        {
            return this.DBService.ConvertSchema(this.Corpus, callback);
        }
    }
}
