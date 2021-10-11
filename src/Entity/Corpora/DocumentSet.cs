using System.Collections.Generic;
using System.Linq;

namespace ChaKi.Entity.Corpora
{
    public class DocumentSet
    {
        public virtual int ID { get; set; }

        public virtual string Name { get; set; }

        public virtual IList<Document> Documents { get; set; }

        public virtual IList<Project> Projects { get; set; }

        public virtual string Comments { get; set; }

        public DocumentSet()
        {
            this.Documents = new List<Document>();
            this.Projects = new List<Project>();
        }

        public void AddDocument(Document doc)
        {
            this.Documents.Add(doc);
        }

        public void AddProject(Project proj)
        {
            this.Projects.Add(proj);
        }

        public Document FindDocument(int id)
        {
            return this.Documents.FirstOrDefault<Document>(d => d.ID == id);
        }

        public int GetUnusedDocumentID()
        {
            if (this.Documents.Count == 0) return 0;
            return this.Documents.Max(d => d.ID) + 1;
        }
    }
}
