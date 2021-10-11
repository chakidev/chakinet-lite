namespace ChaKi.Entity.Corpora
{
    public class DocumentSetProjectMapping
    {
        public virtual int ID { get; set; }

        public virtual DocumentSet DocumentSet { get; set; }

        public virtual Project Proj { get; set; }
    }
}
